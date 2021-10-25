use bindings::Windows::Win32::Storage::FileSystem::{
    GetFileVersionInfoSizeW, GetFileVersionInfoW, VerQueryValueW, VS_FIXEDFILEINFO,
};
use log::{debug, error};
use std::{ffi::c_void, fmt::Formatter, fs::{self, File}, io::{self, BufRead}, path::{Path, PathBuf}};
use walkdir::WalkDir;

use crate::{
    extensions::{self, get_assembly_dir, get_update_data_dir, get_working_dir},
    OpError,
};

pub struct Version {
    major: i32,
    minor: i32,
    build: i32,
    revision: i32,
}

impl From<String> for Version {
    fn from(version: String) -> Self {
        let mut version_parts = version.split('.');
        let major = version_parts.next().unwrap_or_default().parse::<i32>().unwrap_or(-1);
        let minor = version_parts.next().unwrap_or_default().parse::<i32>().unwrap_or(-1);
        let build = version_parts.next().unwrap_or_default().parse::<i32>().unwrap_or(-1);
        let revision = version_parts.next().unwrap_or_default().parse::<i32>().unwrap_or(-1);
        Version {
            major,
            minor,
            build,
            revision,
        }
    }
}

impl std::fmt::Display for Version {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        write!(f, "{}.{}.{}.{}", self.major, self.minor, self.build, self.revision)
    }
}

lazy_static! {
    static ref WHITELIST: Result<Vec<String>, OpError> = {
        let mut v = Vec::new();
        let file = File::open(get_assembly_dir().join("whitelist.txt"))
            .map_err(|op| OpError::new(&format!("failed to open whitelist file: {}", op), true))?;
        let reader = io::BufReader::new(file);
        for line in reader.lines() {
            let line = line.map_err(|op| {
                OpError::new(&format!("failed to read whitelist file: {}", op), true)
            })?;
            v.push(line);
        }
        Ok(v)
    };
}

/// gets all files recursively that do not match the filter criteria
pub fn get_files_recurse(path: &PathBuf, filter_criteria: fn(&Path) -> bool) -> Vec<PathBuf> {
    let mut old_files: Vec<PathBuf> = Vec::new();
    WalkDir::new(path)
        .into_iter()
        .filter_entry(|e| filter_criteria(e.path()))
        .filter_map(|v| v.ok())
        .filter(|e| e.file_type().is_file())
        .for_each(|e| {
            old_files.push(PathBuf::from(e.path()));
        });
    old_files
}

/// gets all directories recursively not matching the filter criteria
pub fn get_dirs(path: &PathBuf, filter_criteria: fn(&Path) -> bool) -> Result<Vec<PathBuf>, OpError> {
    let entries = fs::read_dir(path)
        .map_err(|e| OpError::new(&format!("could not read root directory in get_dirs {:?}: {}", path, e), true))?;
    let mut old_dirs = entries
        .into_iter()
        .filter_map(|e| e.ok())
        .map(|e| e.path())
        .filter(|e| filter_criteria(e.as_path()))
        .filter(|e| e.is_dir())
        .collect::<Vec<PathBuf>>();
    let work_dir_str = get_working_dir();
    let filtered: Vec<PathBuf> = old_dirs.drain(..).filter(|e| !e.eq(&work_dir_str)).collect();
    Ok(filtered)
}

/// returns all files that belong to Auto Dark Mode, excluding installer files and update directories.
///
/// This is required for the update to complete, because the updater must not touch its own files
pub fn get_adm_files(path: &PathBuf) -> Result<Vec<PathBuf>, OpError> {
    let entries = fs::read_dir(path)
        .map_err(|e| OpError::new(&format!("could not read directory in get_files {:?}: {}", path, e), true))?;
    let result = entries
        .into_iter()
        .filter(|r| r.is_ok())
        .filter(|ent| match ent.as_ref() {
            Ok(e) => check_ignored(e.path().as_path()),
            Err(_) => false,
        })
        .map(|res| res.map(|e| e.path()))
        .collect::<Result<Vec<PathBuf>, io::Error>>()
        .map_err(|e| OpError::new(&format!("error mapping file in get_files: {}", e), true))?;
    Ok(result)
}

pub fn is_whitelisted(entry: &Path) -> bool {
    let whitelist = match WHITELIST.as_ref() {
        Ok(v) => v,
        Err(e) => {
            error!("{}", e);
            error!("aborting patch");
            panic!("{}", e);
        }
    };

    let entry_string = entry.display().to_string();
    return whitelist.iter().any(|e| entry_string.ends_with(e))
}

/// Checks if files should be ignored by the file collector
pub fn check_ignored(entry: &Path) -> bool {
    let execution_dir_str = get_assembly_dir().to_str().unwrap_or("").to_string();
    let update_data_dir_str = get_update_data_dir().to_str().unwrap_or("").to_string();
    //let work_dir_str = get_working_dir().to_str().unwrap_or("").to_string();
    let entry_str = entry.to_str().unwrap_or_default();
    if entry_str.contains("unins000.dat") {
        return false;
    } else if entry_str.contains("unins000.exe") {
        return false;
    } else if entry_str.contains("AutoDarkMode.VisualElementsManifest.xml") {
        return false;
    } else if execution_dir_str.len() > 0 && entry_str.contains(&execution_dir_str) {
        return false;
    } else if update_data_dir_str.len() > 0 && entry_str.contains(&update_data_dir_str) {
        return false;
    }
    is_whitelisted(entry)
}

pub fn clean_adm_dir() -> Result<(), OpError> {
    let files = get_files_recurse(&extensions::get_working_dir(), check_ignored);
    for file in files {
        debug!("removing file {}", file.display());
        std::fs::remove_file(file).map_err(|e| OpError::new(&format!("could not remove file: {}", e), true))?;
    }
    let dirs = get_dirs(&extensions::get_working_dir(), check_ignored)?;
    for dir in dirs {
        debug!("removing dir {}", dir.display());
        std::fs::remove_dir(dir).map_err(|e| OpError::new(&format!("could not remove directory: {}", e), true))?;
    }
    Ok(())
}

/// Moves files from source to destination
/// ### Returns
/// Ok if successful, an OpError with severity false if something went wrong
pub fn move_files(source: &PathBuf, target: &PathBuf, files: Vec<PathBuf>) -> Result<(), OpError> {
    for file in files {
        let relative_dir = file.strip_prefix(&source).unwrap();
        let out_file = target.join(relative_dir);
        let mut out_dir = out_file.clone();
        let success = out_dir.pop();
        if success && !Path::exists(&out_dir) {
            std::fs::create_dir_all(&out_dir).map_err(|e| {
                OpError::new(
                    &format!(
                        "could not create directory {} before moving files: {}",
                        out_dir.as_os_str().to_str().unwrap_or("undefined"),
                        e
                    ),
                    false,
                )
            })?;
        }
        std::fs::rename(&file, &out_file).map_err(|e| {
            OpError::new(
                &format!(
                    "could not move file {} to {} : {}",
                    file.as_os_str().to_str().unwrap_or("undefined"),
                    target.as_os_str().to_str().unwrap_or("undefined"),
                    e
                ),
                false,
            )
        })?;
    }
    Ok(())
}

/// Makes calls to the WinAPI, retrieving the file version of the given path
pub fn get_file_version(path: PathBuf) -> Result<Version, OpError> {
    let path = path.as_os_str();
    let mut handle: u32 = 2;
    let size = unsafe { GetFileVersionInfoSizeW(path, &mut handle) };
    if size == 0 {
        let msg = "failed to get file version size";
        debug!("{}", msg);
        return Err(OpError::new(msg, false));
    }

    let mut buffer: Vec<u8> = vec![0; size as usize];
    let result = unsafe { GetFileVersionInfoW(path, 0, size, buffer.as_mut_ptr() as *mut c_void) };
    if !result.as_bool() {
        let msg = "could not read file version";
        debug!("{}", msg);
        return Err(OpError {
            message: msg.into(),
            severe: false,
        });
    }
    //parse buffer using VerQueryValueW from the windows-rs api
    let mut value_len: u32 = 1;
    let mut pads: *mut c_void = std::ptr::null_mut();

    // this works if ppads as _, is casted below, or just using &mut pads
    //let ppads = &mut pads as *mut *mut c_void;
    let result = unsafe {
        VerQueryValueW(
            buffer.as_mut_ptr() as *mut c_void,
            "\\",
            &mut pads as _,
            //ppads as _,
            &mut value_len,
        )
    };
    if !result.as_bool() {
        let msg = "could not transform file version";
        debug!("{}", msg);
        return Err(OpError {
            message: msg.into(),
            severe: false,
        });
    }
    if value_len == 0 {
        let msg = "ver_query_value_w buffer length";
        debug!("{}", msg);
        return Err(OpError {
            message: msg.into(),
            severe: false,
        });
    }
    let version_info = unsafe { &*(pads as *const VS_FIXEDFILEINFO) };
    let version_info_string = format!(
        "{}.{}.{}.{}",
        (version_info.dwFileVersionMS >> 16) & 0xffff,
        (version_info.dwFileVersionMS >> 0) & 0xffff,
        (version_info.dwFileVersionLS >> 16) & 0xffff,
        (version_info.dwFileVersionLS >> 0) & 0xffff
    );
    Ok(version_info_string.into())
}

#[cfg(test)]
mod tests {
    use std::error::Error;

    use log::info;

    use crate::{
        extensions::{get_service_path, get_working_dir},
        setup_logger,
    };

    use super::*;

    #[test]
    fn get_service_version() -> Result<(), Box<dyn Error>> {
        let service_path = get_service_path();
        let result = get_file_version(service_path)?;
        println!("{}", result);
        Ok(())
    }

    #[test]
    fn test_dir_traverser() {
        setup_logger().unwrap();
        let files = get_adm_files(&get_working_dir()).unwrap();
        info!("{:?}", files);
        //get_working_dir_files(get_working_dir());
    }

    #[test]
    fn clean_adm_test() {
        setup_logger().unwrap();
        match clean_adm_dir() {
            Ok(_) => info!("clean adm dir successful"),
            Err(e) => info!("clean adm dir failed: {}", e),
        }
    }
}
