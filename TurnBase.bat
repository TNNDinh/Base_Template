@echo off
setlocal
cd /d "%~dp0"

set "UNITY_VERSION="
for /f "tokens=2" %%i in ('findstr /b "m_EditorVersion:" "ProjectSettings\ProjectVersion.txt"') do set "UNITY_VERSION=%%i"
if not defined UNITY_VERSION (
  echo Could not determine Unity version from ProjectSettings\ProjectVersion.txt
  pause
  exit /b 1
)
echo Detected Unity version: %UNITY_VERSION%

set "UNITY_EXE=%ProgramFiles%\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"
if not exist "%UNITY_EXE%" set "UNITY_EXE=%ProgramFiles(x86)%\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"
if not exist "%UNITY_EXE%" set "UNITY_EXE=%LOCALAPPDATA%\Programs\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"
if not exist "%UNITY_EXE%" (
  echo Unity application not found for version %UNITY_VERSION%.
  echo Please ensure Unity %UNITY_VERSION% is installed via Unity Hub.
  pause
  exit /b 1
)

echo Opening Unity Editor...
start "" "%UNITY_EXE%" -projectPath "%CD%" -buildTarget Android -accept-apiupdate -ignoreCompilerErrors
endlocal
