@echo off
setlocal EnableExtensions

set "SCRIPT_DIR=%~dp0"
set "PROJECT_PATH=%SCRIPT_DIR%OSLNative.vcxproj"
set "OUTPUT_FILE=%SCRIPT_DIR%..\Assets\Plugins\OSLNative\x64\Release\OSLNative.dll"

echo Build Windows Plugin from Windows...

if not exist "%PROJECT_PATH%" (
    echo Error: Could not find %PROJECT_PATH%
    exit /b 1
)

set "MSBUILD_EXE="

if defined VSINSTALLDIR (
    if exist "%VSINSTALLDIR%MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_EXE=%VSINSTALLDIR%MSBuild\Current\Bin\MSBuild.exe"
)

if not defined MSBUILD_EXE (
    set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
    if exist "%VSWHERE%" (
        for /f "usebackq delims=" %%I in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
            if not defined MSBUILD_EXE set "MSBUILD_EXE=%%I"
        )
    )
)

if not defined MSBUILD_EXE (
    where MSBuild.exe >nul 2>nul
    if not errorlevel 1 set "MSBUILD_EXE=MSBuild.exe"
)

if not defined MSBUILD_EXE (
    echo Error: MSBuild.exe could not be found. Install Visual Studio Build Tools with the C++ workload.
    exit /b 1
)

echo Using MSBuild: %MSBUILD_EXE%
"%MSBUILD_EXE%" "%PROJECT_PATH%" /t:Rebuild /p:Configuration=Release /p:Platform=x64 /m
if errorlevel 1 exit /b %ERRORLEVEL%

if exist "%OUTPUT_FILE%" (
    echo Success: Created %OUTPUT_FILE%
    call "%SCRIPT_DIR%Build\restore_plugin_meta_templates.bat" "x64\Release\OSLNative.dll.meta"
    if errorlevel 1 exit /b %ERRORLEVEL%
) else (
    echo Error: Build finished but output file not found at %OUTPUT_FILE%
    exit /b 1
)

