@echo off
setlocal EnableExtensions

set "SCRIPT_DIR=%~dp0"
set "TARGET=%~1"

if "%TARGET%"=="" goto Usage

if /I "%TARGET%"=="windows" goto Windows
if /I "%TARGET%"=="android" goto Android
if /I "%TARGET%"=="all" goto All

echo Invalid target: %TARGET%
goto Usage

:Windows
echo --------------------------------------------------
echo Target: Windows
echo --------------------------------------------------
call "%SCRIPT_DIR%build_plugin_for_windows_from_windows.bat"
exit /b %ERRORLEVEL%

:Android
echo --------------------------------------------------
echo Target: Android
echo --------------------------------------------------
call "%SCRIPT_DIR%build_plugin_for_android_from_windows.bat"
exit /b %ERRORLEVEL%

:All
call "%SCRIPT_DIR%build_all_from_windows.bat" windows
if errorlevel 1 exit /b %ERRORLEVEL%
call "%SCRIPT_DIR%build_all_from_windows.bat" android
if errorlevel 1 exit /b %ERRORLEVEL%
echo --------------------------------------------------
echo Done.
exit /b 0

:Usage
echo Usage: build_all_from_windows.bat [android^|windows^|all]
exit /b 1

