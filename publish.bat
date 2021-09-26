set digit1=%time:~0,1%
if "%digit1%"==" " set digit1=0
set timetext=%digit1%%time:~1,1%%time:~3,2%


call dotnet publish AutoDarkModeApp\AutoDarkModeApp.csproj /p:PublishProfile=$(SolutionDir)AutoDarkModeApp\Properties\PublishProfiles\AppPublish.pubxml
call dotnet publish AutoDarkModeSvc\AutoDarkModeSvc.csproj /p:PublishProfile=$(SolutionDir)\AutoDarkModeSvc\Properties\PublishProfiles\ServicePublish.pubxml
call dotnet publish AutoDarkModeShell\AutoDarkModeShell.csproj /p:PublishProfile=$(SolutionDir)\AutoDarkModeShell\Properties\PublishProfiles\FolderProfile.pubxml
call dotnet publish AutoDarkModeUpdater\AutoDarkModeUpdater.csproj /p:PublishProfile=$(SolutionDir)\AutoDarkModeUpdater\Properties\PublishProfiles\FolderProfile.pubxml

tar -cvzf AutoDarkModeInstaller/Setup/ADM.zip bin/Publish
pwsh -executionpolicy remotesigned -File generate_hash.ps1
chdir /d AutoDarkModeInstaller
chdir /d Setup
move ADM.zip AutoDarkModeX_Archive_%date:~-4,4%%date:~-7,2%%date:~-10,2%_%timetext%.zip
move ADM.sha256 AutoDarkModeX_Archive_%date:~-4,4%%date:~-7,2%%date:~-10,2%_%timetext%.sha256
