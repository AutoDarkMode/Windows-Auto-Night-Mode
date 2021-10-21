#![allow(dead_code)]

use log::error;
use std::path::PathBuf;

static SERVICE_EXE: &'static str = "AutoDarkModeSvc.exe";
static APP_EXE: &'static str = "AutoDarkModeApp.exe";
static SHELL_EXE: &'static str = "AutoDarkModeShell.exe";

/// Returns the execution directory the updater resides in
pub fn get_execution_dir() -> PathBuf {
    let dir = match std::env::current_dir() {
        Ok(path) => path,
        Err(e) => {
            error!("error getting current exe path: {}", e);
            panic!("{}", e)
        }
    };
    dir
    //let dir2 = PathBuf::from(r"D:\Code\Repos\AutoDarkMode\ADM-Test-Environment\App\Updater");
    //dir2
}

/// Returns the working directory of the updater, which is used for copying data INTO
pub fn get_working_dir() -> PathBuf {
    let pb = get_execution_dir();
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
    #[test]
    fn print_updater_paths() {
        use super::*;
        println!("exedir: {:?}", get_execution_dir());
        println!("cwd: {:?}", get_working_dir());
        println!("service: {:?}", get_service_path());
        println!("app: {:?}", get_app_path());
        println!("shell: {:?}", get_shell_path());
        println!("update_data: {:?}", get_update_data_dir());
    }
}
