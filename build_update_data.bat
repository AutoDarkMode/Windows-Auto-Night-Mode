call publish.bat

REM tar -cvzf AutoDarkModeInstaller/Setup/ADM.zip -C bin/Publish *
chdir /d bin
chdir /d Publish
7z a ../../AutoDarkModeInstaller/Setup/ADM.zip *
chdir /d ..
chdir /d ..

pwsh -executionpolicy remotesigned -File generate_hash.ps1