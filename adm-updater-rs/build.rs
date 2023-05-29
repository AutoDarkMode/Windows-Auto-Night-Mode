extern crate winres;

fn main() {
    if cfg!(target_os = "windows") {
      let mut res = winres::WindowsResource::new();
      res.set_icon("adm_new.ico");
      res.compile().unwrap();
      static_vcruntime::metabuild();
    }
  }
