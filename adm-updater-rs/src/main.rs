#![windows_subsystem = "windows"]

#[macro_use]
extern crate lazy_static;

use crate::extensions::{get_service_path, get_update_data_dir};
use comms::send_message_and_get_reply;
use extensions::get_working_dir;
use log::{debug, warn};
use log::{error, info};
use std::error::Error;
use std::path::PathBuf;
use std::process::Command;
use std::rc::Rc;
use std::{env, fmt, fs};
use sysinfo::{System, UserExt};
use sysinfo::{ProcessExt, SystemExt};
use windows::core::PCWSTR;
use windows::w;
use windows::Win32::Foundation::HWND;
use windows::Win32::System::Console::{AttachConsole, ATTACH_PARENT_PROCESS};
use windows::Win32::UI::Shell::ShellExecuteW;
use windows::Win32::UI::WindowsAndMessaging::SHOW_WINDOW_CMD;

mod comms;
mod extensions;
mod io;
mod license;
mod regedit;

const VERSION: &'static str = env!("CARGO_PKG_VERSION");

#[derive(Debug, Clone)]
pub struct OpError {
    pub message: String,
    pub severe: bool,
}

impl Error for OpError {}

impl fmt::Display for OpError {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        write!(f, "{}", self.message)
    }
}

impl OpError {
    pub fn new(msg: &str, severe: bool) -> OpError {
        let message = msg.to_string();
        OpError { message, severe }
    }
}

trait LogExt {
    fn log(self) -> Self;
}

impl<T, E> LogExt for Result<T, E>
where
    E: std::fmt::Display,
{
    fn log(self) -> Self {
        if let Err(e) = &self {
            error!("An error happened: {}", e);
        }
        self
    }
}

fn main() -> Result<(), Box<dyn Error>> {
    unsafe {
        AttachConsole(ATTACH_PARENT_PROCESS);
    }
    if !setup_logger().is_ok() {
        print!("failed to setup logger");
    }

    let mut restart_app = false;
    let mut restart_shell = false;
    let args: Vec<String> = env::args().collect();
    if args.len() >= 2 {
        if args.contains(&"--notify".to_string()) {
            if args.len() >= 3 {
                if args[2] == "True" {
                    restart_shell = true;
                }
            }
            if args.len() >= 4 {
                if args[3] == "True" {
                    restart_app = true;
                }
            }
        }
        if args.contains(&"--info".to_string()) {
            license::display_license();
            return Ok(());
        }
    }
    info!("auto dark mode updater {}", VERSION);
    info!("cwd: {}", get_working_dir().display());
    info!("restart app: {}, restart shell: {}", restart_app, restart_shell);

    let username = whoami::username();
    let _curver = io::get_file_version(get_service_path())
        .and_then(|ver| {
            info!("currently installed version: {}", ver);
            Ok(ver)
        })
        .or_else(|e| {
            warn!("could not read installed version: {}", e);
            Err(e)
        });

    let temp_dir = get_update_data_dir().join("tmp");

    shutdown_service(&username).map_err(|op| {
        try_relaunch(restart_shell, restart_app, &username, false);
        op
    })?;

    move_to_temp(&temp_dir).map_err(|op| {
        if op.severe {
            std::process::exit(-1);
        }
        error!("moving files to temp failed, attempting rollback");
        if let Err(_) = rollback(&temp_dir) {
            error!("rollback failed, this is non-recoverable, please reinstall auto dark mode");
            std::process::exit(-1);
        } else {
            try_relaunch(restart_shell, restart_app, &username, false);
        }
        op
    })?;

    patch(&get_update_data_dir().join("unpacked")).map_err(|op| {
        error!("patching failed, attempting rollback with cleanup: {}", op);
        if let Err(e) = io::clean_adm_dir() {
            error!("{}", e);
            error!("preparing rollback failed, this is non-recoverable, please reinstall auto dark mode");
            std::process::exit(-1);
        }
        if let Err(_) = rollback(&temp_dir) {
            error!("rollback failed, this is non-recoverable, please reinstall auto dark mode");
            std::process::exit(-1);
        } else {
            try_relaunch(restart_shell, restart_app, &username, false);
        }
        op
    })?;

    let mut patch_success_msg = "patch_complete".to_string();
    if let Ok(current_version) = io::get_file_version(get_service_path()) {
        patch_success_msg.push_str(&format!(", installed version: {}", current_version).to_string());
        info!("updating setup version string");
        if let Err(e) = regedit::update_inno_installer_string(&username, &current_version.to_string()) {
            if e.severe {
                warn!("{}", e);
            } else {
                info!("{}", e);
            }
        };
    } else {
        warn!("could not read patched file version, skipping installer versin string update");
    };
    info!("{}", patch_success_msg);

    try_relaunch(restart_shell, restart_app, &username, true);
    Ok(())
}

fn move_to_temp(temp_dir: &PathBuf) -> Result<(), OpError> {
    info!("moving current installation to temp directory");
    let source = get_working_dir();
    let files = io::get_adm_files(&source)?;
    if !files.contains(&get_service_path()) {
        let msg = "service executable not found in working directory, aborting patch";
        error!("{}", msg);
        return Err(OpError::new(msg, true));
    }
    io::move_files(&source, temp_dir, files).map_err(|e| {
        error!("{}", e);
        e
    })?;
    Ok(())
}

fn rollback(temp_dir: &PathBuf) -> Result<(), OpError> {
    info!("rolling back files");
    let files = io::get_files_recurse(&temp_dir, |_| true);
    let target = get_working_dir();
    if let Err(mut e) = io::move_files(&temp_dir, &target, files) {
        error!("{}", e);
        e.severe = true;
        return Err(e);
    }

    match io::get_dirs(&temp_dir, |_| true) {
        Ok(dirs) => {
            let mut error = false;
            for dir in dirs {
                if let Err(e) = fs::remove_dir(&dir) {
                    warn!("could not remove temp subdirectory {}: {}", dir.display(), e);
                    error = true;
                }
            }
            if !error {
                if let Err(e) = fs::remove_dir(temp_dir) {
                    warn!("could not delete temp directory after rollback: {}", e);
                }
            }
        }
        Err(e) => {
            warn!("could not retrieve directories to clean after rollback {}", e);
        }
    }
    info!("rollback successful, no update has been performed, restarting auto dark mode");
    Ok(())
}

fn patch(update_dir: &PathBuf) -> Result<(), OpError> {
    info!("patching auto dark mode");
    let files = io::get_files_recurse(&update_dir, |_| true);
    if files.len() == 0 {
        return Err(OpError::new("no files found in update directory", true));
    }
    let target = get_working_dir();
    if let Err(mut e) = io::move_files(&update_dir, &target, files) {
        error!("{}", e);
        e.severe = true;
        return Err(e);
    }
    info!("removing old files");
    if let Err(e) = fs::remove_dir_all(get_update_data_dir()) {
        warn!("could not remove old update files, manual investigation required: {}", e);
    }
    Ok(())
}

fn shutdown_service(channel: &str) -> Result<(), Box<dyn Error>> {
    info!("shutting down service");
    let mut api_shutdown_confirmed = false;
    if let Err(e) = send_message_and_get_reply("--exit", 3000, channel) {
        if e.is_timeout {
            api_shutdown_confirmed = true;
        } else {
            warn!("could not cleanly shut down service");
            return Err(e.into());
        }
    }
    if !api_shutdown_confirmed {
        info!("waiting for service to stop");
        for _ in 0..5 {
            if let Err(e) = send_message_and_get_reply("--alive", 1000, channel) {
                if e.is_timeout {
                    break;
                }
            }
        }
    }

    let mut s = System::new();
    let username: String = whoami::username();
    s.refresh_processes();
    s.refresh_users_list();
    let mut p_service = s.processes_by_name("AutoDarkModeSvc");
    let mut p_app = s.processes_by_name("AutoDarkModeApp");
    let mut p_shell = s.processes_by_name("AutoDarkModeShell");
    let mut shutdown_failed = false;
    while let Some(p) = p_service.next() {
        info!("running adm service found");
        let user_id;
        match p.user_id() {
            Some(id) => user_id = id,
            None => continue,
        };
        if let Some(user) = s.get_user_by_id(user_id) {
            warn!("service still running, force stopping");
            if user.name() == username {
                info!("stopping service for current user");
                shutdown_failed = shutdown_failed || !p.kill();
            }
            else {
                info!("service found running for different user {}, no action required", user.name())
            }
        }
    }
    while let Some(p) = p_app.next() {
        info!("running adm app found");
        let user_id;
        match p.user_id() {
            Some(id) => user_id = id,
            None => continue,
        };
        if let Some(user) = s.get_user_by_id(user_id) {
            if user.name() == username {
                info!("stopping app for current user");
                shutdown_failed = shutdown_failed || !p.kill();
            } else {
                info!("app found running for different user {}, no action required", user.name())
            }
        }
    }
    while let Some(p) = p_shell.next() {
        info!("running adm shell found");
        let user_id;
        match p.user_id() {
            Some(id) => user_id = id,
            None => continue,
        };
        if let Some(user) = s.get_user_by_id(user_id) {
            if user.name() == username {
                info!("stopping shell for current user");
                shutdown_failed = shutdown_failed || !p.kill();
            }
            else {
                info!("shell found running for different user {}, no action required", user.name())
            }
        }
    }

    if shutdown_failed {
        let msg = "other auto dark mode components still running, skipping update".to_string();
        warn!("{}", &msg);
        return Err(Box::new(OpError {
            message: msg,
            severe: true,
        }));
    }
    info!("service shutdown confirmed");
    Ok(())
}

fn try_relaunch(restart_shell: bool, restart_app: bool, channel: &str, patch_success: bool) {
    match relaunch(restart_shell, restart_app, &channel, patch_success) {
        Ok(_) => {}
        Err(e) => {
            warn!("{}", e);
        }
    }
}

#[allow(non_snake_case)]
fn relaunch(restart_shell: bool, restart_app: bool, channel: &str, patch_success: bool) -> Result<(), Box<dyn Error>> {
    info!("starting service");
    let service_path = Rc::new(extensions::get_service_path());
    Command::new(&*Rc::clone(&service_path)).spawn().map_err(|e| {
        Box::new(OpError {
            message: format!(
                "could not relaunch service at path: {}: {}",
                service_path.to_str().unwrap_or_default(),
                e
            ),
            severe: false,
        })
    })?;
    if restart_app {
        let app_path = Rc::new(extensions::get_app_path());
        info!("relaunching app");
        debug!("app path {}", app_path.display());
        Command::new(&*Rc::clone(&app_path)).spawn().map_err(|e| {
            Box::new(OpError {
                message: format!(
                    "could not relaunch app at path: {}: {}",
                    app_path.to_str().unwrap_or_default(),
                    e
                ),
                severe: false,
            })
        })?;
    }
    if restart_shell {
        let shell_path_buf = extensions::get_shell_path();
        let shell_path = windows::core::HSTRING::from(shell_path_buf.as_os_str().to_os_string());
        info!("relaunching shell");
        debug!("shell path {}", shell_path_buf.display());
        let result = unsafe {
            ShellExecuteW(
                HWND(0),
                w!("open"),
                &shell_path,
                PCWSTR::null(),
                PCWSTR::null(),
                SHOW_WINDOW_CMD(5),
            )
        };
        if result.0 < 32 {
            return Err(Box::new(OpError {
                message: format!(
                    "could not relaunch shell at path: {}, (os_error: {})",
                    extensions::get_shell_path().to_str().unwrap_or_default(),
                    result.0
                ),
                severe: false,
            }));
        }
    }
    if !patch_success {
        if let Err(e) = send_message_and_get_reply("--update-failed", 5000, channel) {
            warn!("could not send update failed message: {}", e);
        }
    }
    Ok(())
}

#[cfg(debug_assertions)]
fn setup_logger() -> Result<(), fern::InitError> {
    use platform_dirs::AppDirs;
    let log_path = AppDirs::new(Some("AutoDarkMode"), false).map_or("updater.log".into(), |dirs| {
        dirs.config_dir
            .join("updater.log")
            .to_str()
            .unwrap_or("updater.log")
            .to_string()
    });
    fern::Dispatch::new()
        .format(|out, message, record| {
            out.finish(format_args!(
                "{} [{}] [{}] {}",
                chrono::Local::now().format("%Y-%m-%d %H:%M:%S"),
                record.level(),
                record.target(),
                message
            ))
        })
        .level(log::LevelFilter::Debug)
        .chain(std::io::stdout())
        .chain(fern::log_file(log_path)?)
        .apply()?;
    Ok(())
}

#[cfg(not(debug_assertions))]
fn setup_logger() -> Result<(), fern::InitError> {
    use platform_dirs::AppDirs;
    let log_path = AppDirs::new(Some("AutoDarkMode"), false).map_or("updater.log".into(), |dirs| {
        dirs.config_dir
            .join("updater.log")
            .to_str()
            .unwrap_or("updater.log")
            .to_string()
    });
    fern::Dispatch::new()
        .format(|out, message, record| {
            out.finish(format_args!(
                "{} [{}] [{}] {}",
                chrono::Local::now().format("%Y-%m-%d %H:%M:%S"),
                record.level(),
                record.target(),
                message
            ))
        })
        .level(log::LevelFilter::Info)
        .chain(std::io::stdout())
        .chain(fern::log_file(log_path)?)
        .apply()?;
    Ok(())
}

#[cfg(test)]
mod tests {
    use std::error::Error;

    use crate::{
        setup_logger,
    };

    use super::*;

    #[test]
    fn test_adm_shutdown() -> Result<(), Box<dyn Error>> {
        setup_logger()?;
        let username = whoami::username();
        shutdown_service(&username)?;
        Ok(())
    }
}
