REM RUST BUILD AND PUBLISH
cargo build --release --manifest-path adm-updater-rs\Cargo.toml
if not exist bin\Publish\Updater mkdir bin\Publish\Updater
copy adm-updater-rs\target\release\adm-updater-rs.exe bin\Publish\Updater\AutoDarkModeUpdater.exe
copy adm-updater-rs\license.html bin\Publish\Updater\license.html

REM DOTNET BUILD AND PUBLISH
call dotnet publish AutoDarkModeApp\AutoDarkModeApp.csproj /p:PublishProfile=$(SolutionDir)AutoDarkModeApp\Properties\PublishProfiles\AppPublish.pubxml
call dotnet publish AutoDarkModeSvc\AutoDarkModeSvc.csproj /p:PublishProfile=$(SolutionDir)\AutoDarkModeSvc\Properties\PublishProfiles\ServicePublish.pubxml
call dotnet publish AutoDarkModeShell\AutoDarkModeShell.csproj /p:PublishProfile=$(SolutionDir)\AutoDarkModeShell\Properties\PublishProfiles\FolderProfile.pubxml
call dotnet publish IThemeManager2Bridge\IThemeManager2Bridge.csproj /p:PublishProfile=$(SolutionDir)\IThemeManaber2Bridge\Properties\PublishProfiles\FolderProfile.pubxml
REM call dotnet publish AutoDarkModeUpdater\AutoDarkModeUpdater.csproj /p:PublishProfile=$(SolutionDir)\AutoDarkModeUpdater\Properties\PublishProfiles\FolderProfile.pubxml

REM Generate Updater Files whitelist
dir /b bin\Publish\ > bin\Publish\Updater\whitelist.txt

REM Custom old files
echo mscordaccore_amd64_amd64_5.0.1021.41214.dll>> bin\Publish\Updater\whitelist.txt
echo System.Diagnostics.EventLog.Messages.dll>> bin\Publish\Updater\whitelist.txt
echo clrcompression.dll>> bin\Publish\Updater\whitelist.txt
echo AutoDarkModeConfig.dll>> bin\Publish\Updater\whitelist.txt
echo AutoDarkModeConfig.pdb>> bin\Publish\Updater\whitelist.txt
echo mscordaccore_amd64_amd64_6.0.121.56705.dll>> bin\Publish\Updater\whitelist.txt
echo mscordaccore_amd64_amd64_6.0.21.52210.dll>> bin\Publish\Updater\whitelist.txt
echo zh-tw>> bin\Publish\Updater\whitelist.txt
echo overrides.json.dll>> bin\Publish\Updater\whitelist.txt