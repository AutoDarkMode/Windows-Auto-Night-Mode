#![allow(dead_code)]

use std::path::PathBuf;

#[allow(unused_imports)]
use log::error;

static SERVICE_EXE: &'static str = "AutoDarkModeSvc.exe";
static APP_EXE: &'static str = "AutoDarkModeApp.exe";
static SHELL_EXE: &'static str = "AutoDarkModeShell.exe";

#[cfg(debug_assertions)]
/// Returns the execution directory the updater resides in
pub fn get_assembly_dir() -> PathBuf {
    PathBuf::from(r"D:\Code\Repos\AutoDarkMode\ADM-Test-Environment\App\Updater")
}

#[cfg(not(debug_assertions))]
/// Returns the execution directory the updater resides in
pub fn get_assembly_dir() -> PathBuf {
    let dir = match std::env::current_exe() {
        Ok(path) => {
            let parent = path.parent();
            if parent.is_none() {
                error!("adm updater must not be in root dir. this is really really dangerous, panicking!");
                panic!("adm executed in root");
            }
            parent.unwrap().into()
        }
        Err(e) => {
            error!("error getting current exe path: {}", e);
            panic!("{}", e)
        }
    };
    dir
}

/// Returns the working directory of the updater, which is used for copying data INTO
pub fn get_working_dir() -> PathBuf {
    let pb = get_assembly_dir();
    let parent = match pb.parent() {
        Some(p) => p.to_path_buf(),
        None => pb,
    };
    return parent;
}

/// Returns the path to the service executable, used for starting the service
pub fn get_service_path() -> PathBuf {
    let mut path = get_working_dir();
    path.push(SERVICE_EXE);
    path
}

/// Returns the path to the app executable, used for starting the app
pub fn get_app_path() -> PathBuf {
    let mut path = get_working_dir();
    path.push(APP_EXE);
    path
}

/// Returns the path to the shell executable, used for starting the shell
pub fn get_shell_path() -> PathBuf {
    let mut path = get_working_dir();
    path.push(SHELL_EXE);
    path
}

/// Returns the path the update directory, used for storing temp files and copying update data FROM
pub fn get_update_data_dir() -> PathBuf {
    let mut path = get_working_dir();
    path.push("UpdateData");
    path
}

#[cfg(test)]
mod tests {
    use bindings::Windows::Win32::System::Console::{ATTACH_PARENT_PROCESS, AttachConsole};
    #[test]
    fn print_updater_paths() {

        use super::*;
        unsafe { AttachConsole(ATTACH_PARENT_PROCESS); }
        println!("exedir: {:?}", get_assembly_dir());
        println!("cwd: {:?}", get_working_dir());
        println!("service: {:?}", get_service_path());
        println!("app: {:?}", get_app_path());
        println!("shell: {:?}", get_shell_path());
        println!("update_data: {:?}", get_update_data_dir());
    }
}
