# Build Instructions

OpenSoundLab is open-source under the OSLLv1 license, so you can modify and build the app from the code at GitHub. Please note that the Releases page is outdated.

## Get the source

Clone the repository and check out the desired tag manually:

```bash
git clone https://github.com/SphericalLabs/OpenSoundLab
cd OpenSoundLab
git fetch --tags
git tag
git checkout DESIRED_TAG
```

Replace `DESIRED_TAG` with the tag you want to build.

## Prepare Unity

*   Install Unity Hub and register with Unity: https://unity.com/download
*   In Unity Hub click Installs > Install Editor > Archive > Download Archive and install Unity v2022.3.62f3
*   Add the downloaded repository (the folder containing Assets, OSLNative and the other project folders) to Unity Hub: Projects > Add Project from Disk (click the triangle for that)
*   Click the editor version of the added repository and select Unity v2022.3.62f3 for Android
*   Open the project for the first time, this might take some time
*   Unity might ask you to restart when done importing, click Restart Editor

## Build OSLNative

Build the native OSLNative plugins once before exporting: in Unity click OpenSoundLab > OSLNative > Rebuild. Alternatively enable OpenSoundLab > OSLNative > Rebuild on Build so Unity runs the native rebuild automatically before player builds.

On macOS, the OSLNative rebuild can build Android, Windows and macOS plugins. Install full Xcode, Homebrew and `mingw-w64` (`brew install mingw-w64`) first. The Android script can install the required Android command-line packages and NDK through Homebrew if they are missing.

On Windows, the OSLNative rebuild can build Windows and Android plugins. Install Visual Studio 2022 or Visual Studio Build Tools with the C++ workload and install Android SDK/NDK r26b (`26.1.10909125`) through Android Studio or Unity's Android tooling. The Windows script looks for the SDK through `ANDROID_HOME`, `ANDROID_SDK_ROOT` or `ANDROID_NDK_ROOT`.

## Build And Run On Quest

*   Make sure that developer mode is activated on the headset
*   Connect your Meta Quest headset via USB
*   If connected for the first time, put on the headset and accept the connection to the computer
*   If you want to use Unity Relay, set up a Unity Project ID and add that in Project Settings > Services
*   In Unity, click File > Build Settings
*   Check if your headset is listed at "Run Device"
*   Click Build and Run, set a destination for the APK and wait for the build to complete
*   Put on the headset and check if the app was installed correctly
*   If you already had OpenSoundLab installed from the App Store you might have to deinstall the app first
*   Please note that the tutorial videos are not included in the repository and thus your build
