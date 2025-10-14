#![windows_subsystem = "windows"]

use std::fs::{File, remove_file};
use std::io::{Read, copy};
use std::path::PathBuf;
use std::process::{Command, exit};

use windows::Win32::System::Console::{ATTACH_PARENT_PROCESS, AttachConsole};

use hex::FromHex;
use reqwest::blocking::Client;
use sha2::{Digest, Sha256};

// explicit error codes for known failure modes.
const ERR_DOWNLOAD: i32 = 13370;
const ERR_VERIFY: i32 = 13371;
const ERR_INSTALL_SPAWN: i32 = 13372;
const ERR_CLEANUP: i32 = 13373;

// IMAGE_FILE_MACHINE constants
const IMAGE_FILE_MACHINE_ARM64: u16 = 0xAA64;


use windows::Win32::System::SystemInformation::{
    GetNativeSystemInfo, SYSTEM_INFO,
    PROCESSOR_ARCHITECTURE_AMD64, PROCESSOR_ARCHITECTURE_INTEL, PROCESSOR_ARCHITECTURE_ARM64
};
use windows::Win32::System::Threading::{IsWow64Process2, GetCurrentProcess};

/// Returns "ARM64" or "x86" depending on the *native* system architecture.
fn detect_native_arch() -> &'static str {
    unsafe {
        let mut process_machine: u16 = 0;
        let mut native_machine: u16 = 0;

        if IsWow64Process2(
            GetCurrentProcess(),
            &mut process_machine as *mut u16 as *mut _,
            Some(&mut native_machine as *mut u16 as *mut _)
        ).is_ok() {
            if native_machine == IMAGE_FILE_MACHINE_ARM64 {
                return "ARM64";
            } else {
                return "x86";
            }
        }

        // Fallback for older Windows
        let mut sysinfo = SYSTEM_INFO::default();
        GetNativeSystemInfo(&mut sysinfo);

        match sysinfo.Anonymous.Anonymous.wProcessorArchitecture {
            PROCESSOR_ARCHITECTURE_ARM64 => "ARM64",
            PROCESSOR_ARCHITECTURE_AMD64 | PROCESSOR_ARCHITECTURE_INTEL => "x86", // or differentiate
            _ => "x86",
        }
    }
}
fn main() -> anyhow::Result<()> {
    let result = unsafe { AttachConsole(ATTACH_PARENT_PROCESS) };
    if let Err(e) = result {
        eprintln!("error attaching to parent console: {}", e);
    }

    // support a maintenance flag to print the embedded Cargo.lock packages used by this updater.
    // usage: adm-downloader-rs --updater-licenses
    let args: Vec<String> = std::env::args().collect();
    if args.iter().any(|a| a == "--updater-licenses") {
        print_updater_licenses();
        exit(0);
    }
    // detect runtime architecture to pick the correct asset (ARM64 or x86)
    let asset_arch = detect_native_arch();

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

/// print package name and version pairs from the embedded Cargo.lock.
fn print_updater_licenses() {
    // embed the prepared HTML at compile time and open it in the default browser.
    const HTML: &str = include_str!("../license.html");

    // write to a deterministic temp filename so it can be opened.
    let mut out = std::env::temp_dir();
    out.push("adm-updater-licenses.html");
    if let Err(e) = std::fs::write(&out, HTML) {
        eprintln!("failed to write embedded license HTML to {:?}: {}", out, e);
        return;
    }

    // use the Windows shell to open the file with the default application (browser).
    // `start` requires a title argument; pass an empty title string.
    let path_str = out.to_string_lossy().to_string();
    if let Err(e) = Command::new("cmd").args(["/C", "start", "", &path_str]).status() {
        eprintln!("failed to open license HTML in browser: {}", e);
    }
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

/// run the main download/verify/install flow. The installer's exit code (if run)
/// is written into `installer_code`. On failure this returns one of the
/// explicit error codes (ERR_DOWNLOAD, ERR_VERIFY, ERR_INSTALL_SPAWN).
fn run_install_flow(
    url: &str,
    temp_path: &PathBuf,
    installer_code: &mut Option<i32>,
) -> Result<(), i32> {
    // download
    if let Err(e) = download_file(url, temp_path) {
        eprintln!("download failed: {}", e);
        return Err(ERR_DOWNLOAD);
    }

    // verify
    println!("verifying sha256 checksum...");
    if let Err(e) = verify_sha256(url, temp_path) {
        eprintln!("verify failed: {}", e);
        return Err(ERR_VERIFY);
    }

    // spawn installer
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
