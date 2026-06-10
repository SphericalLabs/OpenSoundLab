@echo off
setlocal EnableExtensions

set "REQUIRED_NDK_VERSION=26.1.10909125"
set "SCRIPT_DIR=%~dp0"
set "OUTPUT_SO=%SCRIPT_DIR%libs\arm64-v8a\libOSLNative.so"
set "DEST_SO=%SCRIPT_DIR%..\Assets\OSLNative\arm64\Release\libOSLNative.so"

echo Build Android Plugin from Windows...

set "NDK_BUILD="

if defined ANDROID_NDK_ROOT (
    if exist "%ANDROID_NDK_ROOT%\ndk-build.cmd" set "NDK_BUILD=%ANDROID_NDK_ROOT%\ndk-build.cmd"
    if exist "%ANDROID_NDK_ROOT%\build\ndk-build.cmd" set "NDK_BUILD=%ANDROID_NDK_ROOT%\build\ndk-build.cmd"
)

if not defined NDK_BUILD if defined ANDROID_NDK_HOME (
    if exist "%ANDROID_NDK_HOME%\ndk-build.cmd" set "NDK_BUILD=%ANDROID_NDK_HOME%\ndk-build.cmd"
    if exist "%ANDROID_NDK_HOME%\build\ndk-build.cmd" set "NDK_BUILD=%ANDROID_NDK_HOME%\build\ndk-build.cmd"
)

if not defined NDK_BUILD if defined ANDROID_NDK (
    if exist "%ANDROID_NDK%\ndk-build.cmd" set "NDK_BUILD=%ANDROID_NDK%\ndk-build.cmd"
    if exist "%ANDROID_NDK%\build\ndk-build.cmd" set "NDK_BUILD=%ANDROID_NDK%\build\ndk-build.cmd"
)

if not defined NDK_BUILD if defined ANDROID_HOME call :FindNdkInSdk "%ANDROID_HOME%"
if not defined NDK_BUILD if defined ANDROID_SDK_ROOT call :FindNdkInSdk "%ANDROID_SDK_ROOT%"
if not defined NDK_BUILD call :FindNdkInSdk "%LOCALAPPDATA%\Android\Sdk"

if not defined NDK_BUILD (
    if exist "%USERPROFILE%\android-ndk-r26b\build\ndk-build.cmd" set "NDK_BUILD=%USERPROFILE%\android-ndk-r26b\build\ndk-build.cmd"
)

if not defined NDK_BUILD (
    echo Error: ndk-build.cmd could not be found.
    echo Expected Android NDK r26b at an Android SDK ndk\%REQUIRED_NDK_VERSION% folder, ANDROID_NDK_ROOT or %%USERPROFILE%%\android-ndk-r26b.
    exit /b 1
)

pushd "%SCRIPT_DIR%"
echo Using NDK build: %NDK_BUILD%
"%NDK_BUILD%" NDK_PROJECT_PATH=. NDK_APPLICATION_MK=./Application.mk %*
if errorlevel 1 (
    popd
    exit /b %ERRORLEVEL%
)

if not exist "%OUTPUT_SO%" (
    popd
    echo Error: Build finished but output file not found at %OUTPUT_SO%
    exit /b 1
)

if not exist "%SCRIPT_DIR%..\Assets\OSLNative\arm64\Release" mkdir "%SCRIPT_DIR%..\Assets\OSLNative\arm64\Release"
move /Y "%OUTPUT_SO%" "%DEST_SO%" >nul
if errorlevel 1 (
    popd
    exit /b %ERRORLEVEL%
)

rmdir /S /Q "%SCRIPT_DIR%libs" 2>nul
rmdir /S /Q "%SCRIPT_DIR%obj" 2>nul

popd
echo Success: Created %DEST_SO%
exit /b 0

:FindNdkInSdk
set "SDK_DIR=%~1"
if "%SDK_DIR%"=="" exit /b 0
if exist "%SDK_DIR%\ndk\%REQUIRED_NDK_VERSION%\ndk-build.cmd" set "NDK_BUILD=%SDK_DIR%\ndk\%REQUIRED_NDK_VERSION%\ndk-build.cmd"
exit /b 0

