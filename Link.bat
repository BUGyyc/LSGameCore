cd /d %~dp0
echo "Start..."

cd "CopyUnityProject"

rd /s /Q Assets
mklink /J Assets "%~dp0/Unity/Assets"

rd /s /Q Packages
mklink /J Packages "%~dp0/Unity/Packages"

rd /s /Q ProjectSettings
mklink /J ProjectSettings "%~dp0/Unity/ProjectSettings"

rd /s /Q Library
mklink /J Library "%~dp0/Unity/Library"



echo "Successed..."

pause
