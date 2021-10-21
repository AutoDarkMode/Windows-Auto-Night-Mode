fn main() {
    windows::build! {
        Windows::Win32::UI::Shell::ShellExecuteW,
        Windows::Win32::Storage::FileSystem::GetFileVersionInfoW,
        Windows::Win32::Storage::FileSystem::GetFileVersionInfoSizeW,
        Windows::Win32::Storage::FileSystem::VerQueryValueW,
        Windows::Win32::Storage::FileSystem::VS_FIXEDFILEINFO,
        Windows::Win32::System::Console::AttachConsole,
        Windows::Win32::System::Console::ATTACH_PARENT_PROCESS
    };
}
