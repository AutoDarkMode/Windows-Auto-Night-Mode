call publish.bat

REM tar -cvzf AutoDarkModeInstaller/Setup/ADM.zip -C bin/Publish *
chdir /d bin
chdir /d Publish
7z a -mm=Deflate -mx7 ../../AutoDarkModeInstaller/Setup/ADM.zip *
chdir /d ..
chdir /d ..

pwsh -executionpolicy remotesigned -File generate_hash.ps1