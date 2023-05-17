REM RUST BUILD AND PUBLISH
cargo build --release --manifest-path adm-updater-rs\Cargo.toml
if not exist bin\Publish\updater mkdir bin\Publish\adm-updater
copy adm-updater-rs\target\release\adm-updater-rs.exe bin\Publish\adm-updater\AutoDarkModeUpdater.exe
copy adm-updater-rs\license.html bin\Publish\adm-updater\license.html

REM DOTNET BUILD AND PUBLISH
call dotnet publish AutoDarkModeApp\AutoDarkModeApp.csproj /p:PublishProfile=$(SolutionDir)AutoDarkModeApp\Properties\PublishProfiles\AppPublish.pubxml
call dotnet publish AutoDarkModeSvc\AutoDarkModeSvc.csproj /p:PublishProfile=$(SolutionDir)\AutoDarkModeSvc\Properties\PublishProfiles\ServicePublish.pubxml
call dotnet publish AutoDarkModeShell\AutoDarkModeShell.csproj /p:PublishProfile=$(SolutionDir)\AutoDarkModeShell\Properties\PublishProfiles\FolderProfile.pubxml
REM call dotnet publish AutoDarkModeUpdater\AutoDarkModeupdater.csproj /p:PublishProfile=$(SolutionDir)\AutoDarkModeupdater\Properties\PublishProfiles\FolderProfile.pubxml