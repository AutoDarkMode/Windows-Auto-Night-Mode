call dotnet publish AutoDarkModeApp\AutoDarkModeApp.csproj /p:PublishProfile=$(SolutionDir)AutoDarkModeApp\Properties\PublishProfiles\AppPublish.pubxml
call dotnet publish AutoDarkModeSvc\AutoDarkModeSvc.csproj /p:PublishProfile=$(SolutionDir)\AutoDarkModeSvc\Properties\PublishProfiles\ServicePublish.pubxml
call dotnet publish AutoDarkModeShell\AutoDarkModeShell.csproj /p:PublishProfile=$(SolutionDir)\AutoDarkModeShell\Properties\PublishProfiles\FolderProfile.pubxml
call dotnet publish AutoDarkModeUpdater\AutoDarkModeUpdater.csproj /p:PublishProfile=$(SolutionDir)\AutoDarkModeUpdater\Properties\PublishProfiles\FolderProfile.pubxml

