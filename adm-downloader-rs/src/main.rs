#![windows_subsystem = "windows"]

use std::fs::{File, remove_file};
use std::io::{Read, copy};
use std::path::PathBuf;
use std::process::{Command, exit};

use windows::Win32::System::Console::{ATTACH_PARENT_PROCESS, AttachConsole};

use hex::FromHex;
use reqwest::blocking::Client;
use sha2::{Digest, Sha256};

// Explicit error codes for known failure modes.
const ERR_DOWNLOAD: i32 = 13370;
const ERR_VERIFY: i32 = 13371;
const ERR_INSTALL_SPAWN: i32 = 13372;
const ERR_CLEANUP: i32 = 13373;

fn main() -> anyhow::Result<()> {
    let result = unsafe { AttachConsole(ATTACH_PARENT_PROCESS) };
    if let Err(e) = result {
        eprintln!("error attaching to parent console: {}", e);
    }
    // detect runtime architecture to pick the correct asset (ARM64 or x86)
    let arch_env = std::env::var("PROCESSOR_ARCHITEW6432")
        .or_else(|_| std::env::var("PROCESSOR_ARCHITECTURE"))
        .unwrap_or_default();
    let arch_up = arch_env.to_uppercase();
    // if the machine is ARM-based, pick ARM64; otherwise fall back to x86 asset.
    let asset_arch = if arch_up.contains("ARM") {
        "ARM64"
    } else {
        "x86"
    };

    let base =
        "https://github.com/AutoDarkMode/Windows-Auto-Night-Mode/releases/download/11.0.0.54/";
    let filename = format!("AutoDarkMode_11.0.0.54_{}.exe", asset_arch);
    let url = format!("{}{}", base, filename);

    // prepare temp path early so we always attempt cleanup.
    let mut temp_path: PathBuf = std::env::temp_dir();
    temp_path.push(filename);

    println!("downloading to {:?}", temp_path);

    // track the installer's exit code (if run) and any mapped error code from this tool.
    let mut installer_code: Option<i32> = None;
    let mut program_error_code: Option<i32> = None;

    // run main flow and capture errors without skipping cleanup.
    if let Err(code) = run_install_flow(&url, &temp_path, &mut installer_code) {
        // capture mapped code.
        program_error_code = Some(code);
    }

    // always attempt to remove the downloaded file.
    if temp_path.exists() {
        match remove_file(&temp_path) {
            Ok(_) => println!("removed {:?}", temp_path),
            Err(rem_e) => {
                eprintln!("failed to remove temp file {:?}: {}", temp_path, rem_e);
                program_error_code = Some(ERR_CLEANUP);
            }
        }
    }

    // prefer the installer's code if present.
    if let Some(code) = installer_code {
        exit(code);
    }

    // otherwise if we mapped a specific error code, return that.
    if let Some(code) = program_error_code {
        exit(code);
    }

    println!("done.");
    Ok(())
}

fn download_file(url: &str, dest: &PathBuf) -> anyhow::Result<()> {
    let client = Client::new();
    let resp = client.get(url).send()?;
    if !resp.status().is_success() {
        anyhow::bail!("Failed to download: HTTP {}", resp.status());
    }

    let mut file = File::create(dest)?;
    let bytes = resp.bytes()?;
    let mut content = bytes.as_ref();
    copy(&mut content, &mut file)?;
    Ok(())
}

fn fetch_expected_sha256(url: &str) -> anyhow::Result<Vec<u8>> {
    // construct URL for the .sha256 file (assume same name + .sha256)
    let sha_url = format!("{}.sha256", url);
    let client = Client::new();
    let resp = client.get(&sha_url).send()?;
    if !resp.status().is_success() {
        anyhow::bail!("Failed to fetch sha256: HTTP {}", resp.status());
    }
    let text = resp.text()?;
    // file should contain the hex hash (optionally followed by filename)
    let hash_str = text
        .split_whitespace()
        .next()
        .ok_or_else(|| anyhow::anyhow!("Empty sha256 file"))?;
    let bytes = Vec::from_hex(hash_str)?;
    Ok(bytes)
}

fn compute_file_sha256(path: &PathBuf) -> anyhow::Result<Vec<u8>> {
    let mut f = File::open(path)?;
    let mut hasher = Sha256::new();
    let mut buf = [0u8; 8192];
    loop {
        let n = f.read(&mut buf)?;
        if n == 0 {
            break;
        }
        hasher.update(&buf[..n]);
    }
    Ok(hasher.finalize().to_vec())
}

fn verify_sha256(url: &str, path: &PathBuf) -> anyhow::Result<()> {
    let expected = fetch_expected_sha256(url)?;
    let actual = compute_file_sha256(path)?;
    if expected != actual {
        let _ = remove_file(path);
        anyhow::bail!(
            "sha256 mismatch: expected {:x?}, got {:x?}",
            expected,
            actual
        );
    }
    Ok(())
}

/// Run the main download/verify/install flow. The installer's exit code (if run)
/// is written into `installer_code`. On failure this returns one of the
/// explicit error codes (ERR_DOWNLOAD, ERR_VERIFY, ERR_INSTALL_SPAWN).
fn run_install_flow(
    url: &str,
    temp_path: &PathBuf,
    installer_code: &mut Option<i32>,
) -> Result<(), i32> {
    // Download
    if let Err(e) = download_file(url, temp_path) {
        eprintln!("download failed: {}", e);
        return Err(ERR_DOWNLOAD);
    }

    // Verify
    println!("verifying sha256 checksum...");
    if let Err(e) = verify_sha256(url, temp_path) {
        eprintln!("verify failed: {}", e);
        return Err(ERR_VERIFY);
    }

    // Spawn installer
    println!("running installer...");
    // collect CLI args passed to this program (skip argv[0]) and forward them to the installer
    let installer_args: Vec<String> = std::env::args().skip(1).collect();
    let arg_refs: Vec<&str> = installer_args.iter().map(|s| s.as_str()).collect();
    match build_command(temp_path, &arg_refs).status() {
        Err(e) => {
            eprintln!("failed to spawn installer: {}", e);
            return Err(ERR_INSTALL_SPAWN);
        }
        Ok(status) => {
            *installer_code = status.code();
            match status.code() {
                Some(code) => {
                    *installer_code = Some(code);
                    if code != 0 {
                        eprintln!("installer exited with code: {}", code);
                    }
                }
                None => {
                    // On Unix this usually means the process was terminated by a signal.
                    // On Windows it's uncommon; use a sentinel so main can still exit with a value.
                    eprintln!("installer terminated without an exit code (abnormal termination)");
                    *installer_code = Some(-99);
                }
            }
        }
    }

    Ok(())
}

/// Build a Command for the given path and arguments.
fn build_command(path: &PathBuf, args: &[&str]) -> Command {
    let mut cmd = Command::new(path);
    for a in args {
        cmd.arg(a);
    }
    cmd
}
