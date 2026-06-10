# OpenResonance Build Chain

This folder contains OpenSoundLab-side build scripts for the `OpenResonance`
submodule. The scripts build the OpenResonance Unity plugin, stage the upstream
CMake install output and generate the Unity-facing local package used by
OpenSoundLab.

The build chain is separate from `OSLNative`. Running OSLNative builds will not
build OpenResonance.

## Quick Start

From the OpenSoundLab repository root:

```bash
git submodule update --init --recursive OpenResonance
./OpenResonanceBuild/build_all_from_macos.sh all
```

This builds the macOS-covered OpenResonance Unity targets and regenerates the
local Unity package:

- macOS `x86_64` with Embree-backed reverb baking enabled
- macOS `arm64` with Embree-backed reverb baking disabled
- Android `arm64-v8a` with reverb baking disabled
- iOS device `arm64` with reverb baking disabled

Windows is intentionally not part of this macOS build chain.

If you want macOS and Android only, without requiring the iOS/Xcode setup:

```bash
./OpenResonanceBuild/build_all_from_macos.sh macos-android
```

To regenerate only the Unity package from existing staged native binaries:

```bash
./OpenResonanceBuild/install_unity_package.sh
```

To include only specific staged native targets:

```bash
./OpenResonanceBuild/install_unity_package.sh macos-x86_64 macos-arm64
```

## Layout

```text
OpenResonanceBuild/
  build_all_from_macos.sh
  build_unity_for_macos_from_macos.sh
  build_unity_for_android_from_macos.sh
  build_unity_for_ios_from_macos.sh
  install_unity_package.sh
  restore_plugin_meta_templates.sh
  PluginMetaTemplates/
```

Generated package, build and staging output goes to:

```text
OpenResonanceBuild/Build/
OpenResonanceBuild/Package/
OpenResonanceBuild/Staging/
```

`Packages/manifest.json` statically references the generated package:

```text
io.sphericals.openresonance -> file:../OpenResonanceBuild/Package
```

Those generated paths are ignored by Git. Build or regenerate OpenResonance
before opening Unity so the local package exists.

## What Gets Packaged

`install_unity_package.sh` copies the upstream ResonanceAudio Unity assets from:

```text
OpenResonance/platforms/unity/UnityIntegration/Assets/ResonanceAudio
```

to:

```text
OpenResonanceBuild/Package
```

It generates `OpenResonanceBuild/Package/package.json`, adds runtime/editor
assembly definitions for UPM compilation, overlays native plugins from
`OpenResonanceBuild/Staging` and restores OpenSoundLab's plugin import metadata
from `PluginMetaTemplates`.

It excludes `Demos`, `Resources/ResonanceAudioMixer.mixer` and upstream
`ProjectSettings`. OpenSoundLab's own `ProjectSettings/AudioManager.asset`
already selects `Resonance Audio` as the spatializer and ambisonic decoder.

The package asset copy uses `rsync --delete`, so
`OpenResonanceBuild/Package` is treated as a generated mirror of the upstream
Unity assets plus package metadata, with demos and the upstream mixer excluded.
Do not keep manual local edits in that folder.

Older builds may leave a generated `Assets/ResonanceAudio` folder behind. Remove
that legacy folder before opening Unity; otherwise Unity imports duplicate
ResonanceAudio scripts and native plugins.

## macOS

```bash
./OpenResonanceBuild/build_unity_for_macos_from_macos.sh
```

This builds:

- `x86_64` with Embree-backed reverb baking enabled
- `arm64` with Embree-backed reverb baking disabled

You can build one architecture only:

```bash
./OpenResonanceBuild/build_unity_for_macos_from_macos.sh x86_64
./OpenResonanceBuild/build_unity_for_macos_from_macos.sh arm64
```

## Android

```bash
./OpenResonanceBuild/build_unity_for_android_from_macos.sh
```

This builds `arm64-v8a` with reverb baking disabled.

The script looks for Unity's Android NDK based on `ProjectSettings/ProjectVersion.txt`.
You can override it:

```bash
OPEN_RESONANCE_ANDROID_NDK=/path/to/ndk ./OpenResonanceBuild/build_unity_for_android_from_macos.sh
```

## iOS

```bash
./OpenResonanceBuild/build_unity_for_ios_from_macos.sh
```

This builds the iOS device static library with reverb baking disabled.
It requires full Xcode with the `iphoneos` SDK selected. If `xcode-select`
points at Command Line Tools, switch it before building:

```bash
sudo xcode-select -s /Applications/Xcode.app/Contents/Developer
```

## All macOS-Covered Targets

```bash
./OpenResonanceBuild/build_all_from_macos.sh all
```

Supported dispatcher targets are:

```text
macos
android
ios
macos-android
all
```

There is intentionally no Windows target in the macOS dispatcher. Windows should
be handled by a separate native Windows build path if needed later.

## CMake Use

These scripts call CMake directly instead of calling `OpenResonance/build.sh`.
That keeps each platform in its own build directory, avoids the upstream shared
`build` folder and lets OpenSoundLab pass explicit Unity plugin flags.

The scripts still use the upstream dependency helpers:

```text
OpenResonance/third_party/clone_core_deps.sh
OpenResonance/third_party/clone_build_install_unity_deps.sh
```

## Useful Environment Variables

```text
OPEN_RESONANCE_CMAKE_GENERATOR=Ninja
OPEN_RESONANCE_PROFILE=Release
OPEN_RESONANCE_CLEAN_BUILD=1
OPEN_RESONANCE_SKIP_INSTALL=1
OPEN_RESONANCE_MACOS_DEPLOYMENT_TARGET=11.5
OPEN_RESONANCE_ANDROID_PLATFORM=android-22
OPEN_RESONANCE_ANDROID_NDK=/path/to/ndk
OPEN_RESONANCE_CMAKE_POLICY_VERSION_MINIMUM=3.5
```

Use `OPEN_RESONANCE_SKIP_INSTALL=1` when you only want to build and stage
artifacts without regenerating `OpenResonanceBuild/Package`.

The policy minimum and CMP0074 default are passed to upstream dependency
configure steps so old third-party projects such as Embree v2 still configure
under CMake 4 and local dependency roots are preferred over Homebrew libraries.
Dependency tests are disabled because only the installed libraries are needed
for the Unity plugin.

For macOS `x86_64`, the wrapper also patches the ignored local Embree checkout
under `OpenResonance/third_party/embree` so CMake 4 and AppleClang can configure
Embree v2. This does not modify tracked OpenResonance submodule files.
