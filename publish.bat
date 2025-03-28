REM DOTNET BUILD AND PUBLISH x86
call dotnet publish AutoDarkModeApp\AutoDarkModeApp.csproj /p:PublishProfile=$(SolutionDir)AutoDarkModeApp\Properties\PublishProfiles\AppPublish.pubxml
call dotnet publish AutoDarkModeSvc\AutoDarkModeSvc.csproj /p:PublishProfile=$(SolutionDir)\AutoDarkModeSvc\Properties\PublishProfiles\ServicePublish.pubxml
call dotnet publish AutoDarkModeShell\AutoDarkModeShell.csproj /p:PublishProfile=$(SolutionDir)\AutoDarkModeShell\Properties\PublishProfiles\ShellPublish.pubxml
REM call dotnet publish AutoDarkModeUpdater\AutoDarkModeupdater.csproj /p:PublishProfile=$(SolutionDir)\AutoDarkModeupdater\Properties\PublishProfiles\LibPublish.pubxml

REM DOTNET BUILD AND PUBLISH ARM64
call dotnet publish AutoDarkModeApp\AutoDarkModeApp.csproj /p:PublishProfile=$(SolutionDir)AutoDarkModeApp\Properties\PublishProfiles\AppPublishARM64.pubxml
call dotnet publish AutoDarkModeSvc\AutoDarkModeSvc.csproj /p:PublishProfile=$(SolutionDir)\AutoDarkModeSvc\Properties\PublishProfiles\ServicePublishARM64.pubxml
call dotnet publish AutoDarkModeShell\AutoDarkModeShell.csproj /p:PublishProfile=$(SolutionDir)\AutoDarkModeShell\Properties\PublishProfiles\ShellPublishARM64.pubxml

REM RUST BUILD AND PUBLISH
cargo build --release --manifest-path adm-updater-rs\Cargo.toml
cargo build --release --manifest-path adm-updater-rs\Cargo.toml --target aarch64-pc-windows-msvc

if not exist bin\Publish\x86\adm-updater mkdir bin\Publish\x86\adm-updater
if not exist bin\Publish\ARM64\adm-updater mkdir bin\Publish\ARM64\adm-updater

copy adm-updater-rs\target\release\adm-updater-rs.exe bin\Publish\x86\adm-updater\AutoDarkModeUpdater.exe
copy adm-updater-rs\license.html bin\Publish\x86\adm-updater\license.html

copy adm-updater-rs\target\aarch64-pc-windows-msvc\release\adm-updater-rs.exe bin\Publish\ARM64\adm-updater\AutoDarkModeUpdater.exe
copy adm-updater-rs\license.html bin\Publish\ARM64\\adm-updater\license.html