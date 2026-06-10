@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
set "NATIVE_DIR=%SCRIPT_DIR%.."
set "PROJECT_DIR=%SCRIPT_DIR%..\.."
set "TEMPLATE_DIR=%NATIVE_DIR%\PluginMetaTemplates"
set "PLUGIN_DIR=%PROJECT_DIR%\Assets\Plugins\OSLNative"

if "%~1"=="" (
    call :Restore "arm64\Release\libOSLNative.so.meta"
    if errorlevel 1 exit /b %ERRORLEVEL%
    call :Restore "macos\Release\libOSLNative.dylib.meta"
    if errorlevel 1 exit /b %ERRORLEVEL%
    call :Restore "x64\Release\OSLNative.dll.meta"
    if errorlevel 1 exit /b %ERRORLEVEL%
    exit /b 0
)

:Loop
if "%~1"=="" exit /b 0
call :Restore "%~1"
if errorlevel 1 exit /b %ERRORLEVEL%
shift
goto Loop

:Restore
set "RELATIVE_PATH=%~1"
set "SOURCE_PATH=%TEMPLATE_DIR%\%RELATIVE_PATH%"
set "DESTINATION_PATH=%PLUGIN_DIR%\%RELATIVE_PATH%"
set "PLUGIN_PATH=!DESTINATION_PATH:.meta=!"

if not exist "%SOURCE_PATH%" (
    echo Error: Missing OSLNative plugin meta template at %SOURCE_PATH%
    exit /b 1
)

if not exist "!PLUGIN_PATH!" (
    echo Skipping OSLNative plugin meta restore because plugin was not built: !PLUGIN_PATH!
    exit /b 0
)

for %%D in ("!DESTINATION_PATH!") do if not exist "%%~dpD" mkdir "%%~dpD"
copy /Y "%SOURCE_PATH%" "!DESTINATION_PATH!" >nul
if errorlevel 1 exit /b %ERRORLEVEL%
echo Restored OSLNative plugin meta: !DESTINATION_PATH!
exit /b 0
