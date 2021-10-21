use std::{ffi::OsStr, path::PathBuf};

use windows_permissions::wrappers::LookupAccountName;
use winreg::{
    enums::{HKEY_USERS, KEY_SET_VALUE},
    RegKey,
};

use crate::OpError;

/// sets the innosetup version string that shows up in windows settings to the patched vesion
/// ### Returns
/// An op error with severity true if the updating failed, severity false if the key was not found.
///
/// It is then assumed that adm is a portable installation, as such no warning or error should be emitted.
pub fn update_inno_installer_string(username: &str, version_string: &str) -> Result<(), OpError> {
    let (sid, _, _) = LookupAccountName(Option::<&OsStr>::None, username)
        .map_err(|e| OpError::new(&format!("could not get user sid: {}", e), true))?;
    let sid_string = sid.to_string();
    let hku = RegKey::predef(HKEY_USERS);
    let path = PathBuf::from(format!(
        "{}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{{470BC918-3740-4A97-9797-8570A7961130}}_is1",
        sid_string
    ));
    let installer_key = hku.open_subkey_with_flags(path, KEY_SET_VALUE).map_err(|e| {
        OpError::new(
            &format!("inno installer key not detected, assuming portable adm installation: {}", e),
            false,
        )
    })?;
    installer_key
        .set_value("DisplayVersion", &version_string.to_string())
        .map_err(|e| OpError::new(&format!("could not update installer version string: {}", e), true))?;

    Ok(())
}

#[cfg(test)]
mod tests {
    use super::update_inno_installer_string;
    use crate::setup_logger;
    use log::debug;

    #[test]
    fn change_version_test() {
        setup_logger().unwrap();
        match update_inno_installer_string("sam", "10.0.1.10") {
            Ok(_) => debug!("test passed"),
            Err(e) => debug!("failed to test update inno installer: {}", e),
        }
    }
}
