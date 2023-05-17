#![allow(dead_code)]

use std::path::PathBuf;

#[allow(unused_imports)]
use log::error;

pub static SERVICE_EXE: &'static str = "AutoDarkModeSvc.exe";
pub static APP_EXE: &'static str = "AutoDarkModeApp.exe";
pub static SHELL_EXE: &'static str = "AutoDarkModeShell.exe";
pub static APP_DIR: &'static str = "adm-app";

#[cfg(debug_assertions)]
/// Returns the execution directory the updater resides in
pub fn get_assembly_dir() -> PathBuf {
    let path = PathBuf::from(r"D:\Code\Repos\AutoDarkMode\ADM-Test-Environment\adm-updater");
    //let path = PathBuf::from(r"F:\\");
    let parent = path.parent();
    if parent.is_none() {
        error!("adm updater must not be in root dir. this is forbidden, panicking!");
        panic!("adm executed in root");
    }
    path
}

#[cfg(not(debug_assertions))]
/// Returns the execution directory the updater resides in
pub fn get_assembly_dir() -> PathBuf {
    let dir = match std::env::current_exe() {
        Ok(path) => {
            let exec_dir = path.parent();

            if exec_dir.is_none() {
                error!("exec dir is none, panicking");
                panic!("adm exec dir none");
            }
            let parent_dir = exec_dir.unwrap().parent();
            if parent_dir.is_none() {
                error!("adm updater must not be in root dir. this is forbidden, panicking!");
                panic!("adm executed in root");
            }
            exec_dir.unwrap().into()
        }
        Err(e) => {
            error!("error getting current exe path: {}", e);
            panic!("{}", e)
        }
    };
    dir
}

/// Returns the parent directory of the updater
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
    let mut path = get_adm_app_dir();
    path.push(SERVICE_EXE);
    path
}

/// Returns the path to the app executable, used for starting the app
pub fn get_app_path() -> PathBuf {
    let mut path = get_adm_app_dir();
    path.push(APP_EXE);
    path
}

/// Returns the path to the shell executable, used for starting the shell
pub fn get_shell_path() -> PathBuf {
    let mut path = get_adm_app_dir();
    path.push(SHELL_EXE);
    path
}

/// Returns the path the update directory, used for storing temp files and copying update data FROM
pub fn get_update_data_dir() -> PathBuf {
    let mut path = get_working_dir();
    path.push("adm-update-data");
    path
}

pub fn get_adm_app_dir() -> PathBuf {
    let mut path = get_working_dir();
    path.push(APP_DIR);
    path
}

#[cfg(test)]
mod tests {
    use windows::Win32::System::Console::{AttachConsole, ATTACH_PARENT_PROCESS};
    #[test]
    fn print_updater_paths() {
        use super::*;
        unsafe {
            AttachConsole(ATTACH_PARENT_PROCESS);
        }
        println!("exedir: {:?}", get_assembly_dir());
        println!("cwd: {:?}", get_working_dir());
        println!("service: {:?}", get_service_path());
        println!("app: {:?}", get_app_path());
        println!("shell: {:?}", get_shell_path());
        println!("update_data: {:?}", get_update_data_dir());
        println!("adm data directory: {:?}", get_adm_app_dir());
        println!(
            "unpacked directory: {:?}",
            get_update_data_dir().join("unpacked").join(APP_DIR)
        );
    }
}
