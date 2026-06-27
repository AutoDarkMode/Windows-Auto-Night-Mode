extern crate winres;

fn main() {
    if cfg!(target_os = "windows") {
      let mut res = winres::WindowsResource::new();
      res.set_icon("adm_new.ico");
      // Embed an explicit asInvoker manifest. Without a requestedExecutionLevel,
      // Windows falls back to UAC Installer Detection, which heuristically flags
      // this binary as an installer (its name and version resources contain
      // "Updater"/"update") and prompts for elevation. PCA can also stamp a sticky
      // RUNASADMIN compat shim on a manifest-less exe. Declaring asInvoker disables
      // both: the updater simply runs with the token of whoever launched it.
      res.set_manifest(
        r#"<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v3">
    <security>
      <requestedPrivileges>
        <requestedExecutionLevel level="asInvoker" uiAccess="false" />
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>
"#,
      );
      res.compile().unwrap();
      static_vcruntime::metabuild();
    }
  }
