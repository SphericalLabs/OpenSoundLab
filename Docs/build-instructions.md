# Build Instructions

OpenSoundLab is open-source under the [OSLLv1 license](../LICENSE-OSLLv1.md), so you can modify and build the app from the code at GitHub. Please note that the Releases page is outdated. You can obtain the app binary through the [Meta Quest Store](https://www.meta.com/en-gb/experiences/opensoundlab/5190305347733325/) or build it yourself, following the OSLLv1 license agreement. Read the license first and make sure you understand it.

## Build Scope and Difficulty

A full source build is moderately difficult on macOS and currently not a complete clean Windows-only path. macOS is the recommended host for regenerating both OpenResonance and OSLNative from source. Windows can rebuild OSLNative, but the OpenResonance build scripts in this repository currently target macOS hosts.

Returning OpenSoundLab developers should note that OpenResonance is new in the build flow. OpenResonance is the new open-source spatializer for OpenSoundLab, which is good news: it moves an important part of the audio stack into source that developers can inspect and rebuild. It does mean that previous OSL build habits are no longer enough because OpenResonance is not shipped with generated Unity package contents or native binaries.

OSLNative binaries are also no longer included in the repository. The native plugin outputs and shared libraries for all platforms would add too much weight to an already large Git repository. Building them locally keeps the repository smaller and gives developers full control over large parts of the OpenSoundLab stack.

## Get the Source

Clone the repository and check out the desired tag manually:

```bash
git clone https://github.com/SphericalLabs/OpenSoundLab
cd OpenSoundLab
git fetch --tags
git tag
git checkout DESIRED_TAG
```

Replace `DESIRED_TAG` with the tag you want to build.

Then initialize the public submodules needed for a source build:

```bash
git submodule sync --recursive
git submodule update --init --recursive OpenResonance OSLNative/Core/External/AprilTag OSLNative/Core/External/link
```

The private paid add-ons submodule is not required for the public build. Initialize it separately only if you have access to the private add-ons repository.

## App Name and Package ID

The source project, documentation and codebase are OpenSoundLab, but homegrown developer builds use `OpenMultiLab` as the product name and `io.sphericals.OpenMultiLab` as the Android application ID.

Keep the OpenMultiLab ID for development and sideloaded Quest builds. The OpenSoundLab app identity is reserved for official signed builds. Android treats builds with the same package ID as the same app, so a sideloaded build signed by Unity's local debug key can fail to install over an official build signed with the official key. Keeping `io.sphericals.OpenMultiLab` lets the local build install separately. If Unity or ADB still reports an install/update conflict, uninstall the conflicting app from the headset first.

## Platform Prerequisites

On macOS:

*   Install Git, Homebrew and Xcode.app (not only the standalone Xcode Command Line Tools)
*   Run Xcode once and accept its license, or run `sudo xcodebuild -license`
*   Install CMake and Ninja for OpenResonance: `brew install cmake ninja`
*   Install `mingw-w64` only if you want to build the Windows OSLNative plugin from macOS: `brew install mingw-w64`
*   If iOS OpenResonance builds are needed, make sure `xcode-select -p` points inside `/Applications/Xcode.app`, not `/Library/Developer/CommandLineTools`. Switch it with `sudo xcode-select -s /Applications/Xcode.app/Contents/Developer`

On Windows:

*   Install Git
*   Install Visual Studio 2022 or Visual Studio Build Tools with the C++ workload if you want to rebuild OSLNative
*   Make sure Android NDK r26b (`26.1.10909125`) is installed and discoverable through `ANDROID_HOME`, `ANDROID_SDK_ROOT`, `ANDROID_NDK_ROOT` or `ANDROID_NDK_HOME` if you want to rebuild the Android OSLNative plugin from Windows
*   Install Meta Quest Developer Hub or Android platform-tools if you want standalone `adb`, logcat and manual uninstall/install commands outside Unity

## Build OpenResonance

Build OpenResonance before opening the project in Unity. `Packages/manifest.json` points to the generated local package at `OpenResonanceBuild/Package`, so Unity needs that package to exist when it resolves packages.

For Quest builds from macOS, build the macOS editor plugins and Android plugin:

```bash
./OpenResonanceBuild/build_all_from_macos.sh macos-android
```

This also regenerates the local Unity package. Use `./OpenResonanceBuild/build_all_from_macos.sh all` only when you also need the iOS plugin and `xcode-select` points at Xcode.app. See [OpenResonanceBuild/README.md](../OpenResonanceBuild/README.md) for target-specific details and environment overrides.

The OpenResonance build scripts in this repository currently target macOS hosts. For a clean Windows setup, generate `OpenResonanceBuild/Package` on macOS or add an equivalent Windows OpenResonance build path before opening the Unity project.

## Build OSLNative

Build the native OSLNative plugins before opening the project in Unity. Unity expects the editor-platform plugin to exist when it imports the project, and Quest builds also need the Android plugin.

On macOS, build the macOS editor plugin and the Android Quest plugin:

```bash
cd OSLNative
./build_all_from_macos.sh macos
./build_all_from_macos.sh android
cd ..
```

If you also need the Windows plugin from macOS, install `mingw-w64` and run:

```bash
cd OSLNative
./build_all_from_macos.sh windows
cd ..
```

On Windows, build the Windows editor plugin and the Android Quest plugin from Command Prompt or PowerShell:

```bat
cd OSLNative
build_all_from_windows.bat windows
build_all_from_windows.bat android
cd ..
```

The generated plugins are written under `Assets/Plugins/OSLNative`. If a script reports that the Android NDK is missing, install Android NDK r26b (`26.1.10909125`) through Unity, Android Studio or the Android SDK tools and make sure the relevant environment variable points to it.

## Prepare Unity

*   Install Unity Hub and register with Unity: https://unity.com/download
*   In Unity Hub click Installs > Install Editor > Archive > Download Archive and install Unity v2022.3.62f3
*   Include Android Build Support, Android SDK & NDK Tools and OpenJDK when installing the editor
*   Add the downloaded repository (the folder containing Assets, OSLNative and the other project folders) to Unity Hub: Projects > Add Project from Disk
*   Click the editor version of the added repository and select Unity v2022.3.62f3 for Android
*   Open the project for the first time; this might take some time
*   Unity might ask you to restart when done importing. Click Restart Editor

## Build and Run on Quest

*   Make sure that developer mode is enabled on the headset
*   Connect your Meta Quest headset via USB
*   If connected for the first time, put on the headset and accept the connection to the computer
*   In Unity, click File > Build Settings
*   Check if your headset is listed at "Run Device"
*   Click Build and Run, set a destination for the APK and wait for the build to complete
*   Put on the headset and check if the app was installed correctly
*   If you already had OpenSoundLab installed from the Meta Quest Store you might have to uninstall the app first
*   Please note that tutorial videos are not included in the repository, so your build will not include them

## Startup Wizards and Development Builds

A Unity Development Build does not automatically skip the startup flows.

The requirements wizard is controlled by the `requirements_consent_v1` PlayerPrefs key and the headset permissions for storage, scene depth access and microphone access. It appears until the required consent and permissions are complete.

Tutorial startup is controlled by the `showTutorialsOnStartup` PlayerPrefs key. Tutorials are suppressed in the Unity Editor and standalone desktop builds, but can open on Android builds when that key is enabled.

In the Unity Editor, use OpenSoundLab > PlayerPrefs > Show Requirements Wizard and OpenSoundLab > PlayerPrefs > Show Tutorials On Startup to toggle the editor-side test settings. PlayerPrefs on the headset are separate, so clear or change them on the device when testing first-run behavior.
