use std::thread;
use std::time::Duration;
use std::{fs, path::PathBuf};

use log::warn;
use log::{error, info};

use crate::{extensions, OpError};

pub fn rollback(temp_dir: &PathBuf) -> Result<(), OpError> {
    let adm_data_dir_pathbuf = extensions::get_adm_app_dir();
    fs::rename(temp_dir, adm_data_dir_pathbuf).map_err(|op| {
        let op_error = OpError::new(format!("{op}",).as_str(), true);
        error!("{}", op_error);
        op_error
    })?;
    Ok(())
}

pub fn move_to_temp(temp_dir: &PathBuf) -> Result<(), OpError> {
    let data_dir = extensions::get_adm_app_dir();
    if !data_dir.exists() {
        let msg = "update data directory not found, aborting patch";
        return Err(OpError::new(msg, false));
    }

    let retries = 3;
    let mut os_error_32 = None;
    for i in 0..retries {
        if let Err(e) = fs::rename(&data_dir, temp_dir) {
            let raw_os_err = e.raw_os_error().unwrap_or(-1);
            if raw_os_err == 32 {
                os_error_32 = Some(e);
                info!("waiting for os to release files, attempt {} of {}", i + 1, retries);
                thread::sleep(Duration::from_secs(1));
                continue;
            } else {
                let msg = "error moving current installation to temp directory, aborting patch";
                let op_error = OpError::new(format!("{msg}: {e}",).as_str(), false);
                return Err(op_error);
            }
        } else {
            os_error_32 = None;
            break;
        }
    }
    if let Some(e) = os_error_32 {
        let msg = "error moving current installation to temp directory, aborting patch";
        let op_error = OpError::new(format!("{msg}: {e}",).as_str(), false);
        return Err(op_error);
    }

    Ok(())
}

pub fn patch(update_dir: &PathBuf, adm_app_dir: &PathBuf) -> Result<(), OpError> {
    let patch_content_dir = update_dir.join("unpacked").join(extensions::APP_DIR);
    let retries = 3;
    let mut os_error_32 = None;
    for i in 0..retries {
        if let Err(e) = fs::rename(&patch_content_dir, adm_app_dir) {
            let raw_os_err = e.raw_os_error().unwrap_or(-1);
            if raw_os_err == 32 || raw_os_err == 5 {
                info!("waiting for os to release files, attempt {} of {}", i + 1, retries);
                os_error_32 = Some(e);
                thread::sleep(Duration::from_secs(1));
                continue;
            } else {
                let msg = "error patching auto dark mode, aborting patch";
                let op_error = OpError::new(format!("{msg}: {e}").as_str(), false);
                return Err(op_error);
            }
        } else {
            os_error_32 = None;
            break;
        }
    }
    if let Some(e) = os_error_32 {
        let msg = "error patching auto dark mode, aborting patch";
        let op_error = OpError::new(format!("{msg}: {e}").as_str(), false);
        return Err(op_error);
    }
    Ok(())
}

pub fn clean_update_files(update_dir: &PathBuf) {
    let previous_service = update_dir.join("tmp").join(extensions::SERVICE_EXE);
    if !previous_service.exists() {
        warn!("could not find valid tmp directory with previous service data, skipping update file removal");
    }
    if let Err(e) = fs::remove_dir_all(update_dir) {
        warn!("could not remove old update files, manual investigation required: {}", e);
    }
}
