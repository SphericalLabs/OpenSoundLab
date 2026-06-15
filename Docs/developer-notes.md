# Developer Notes

These notes collect app usage, Unity development and device integration notes for OpenSoundLab.

## General app usage

*   See the [OpenSoundLab Quickstart](quickstart.md)
*   Turn off Auto Sleep in the headset and the IR Distance Sensor via MQDH so the headset keeps running when you remove it from your head.
    *   Benefits:
        *   Sound can keep running continuously
        *   Network timeout kicks are less likely
        *   You avoid tracking offset glitches when the headset tracks continuously

## Setup Unity for building

*   See the [build instructions](build-instructions.md)
*   Install and use Git for cloning

## Meta Quest Link

### Setup Quest Link

*   Quest Link only works on x64 Windows systems
*   Download the Meta Quest Link app
*   Log into the Meta Quest Link app with your Meta user
*   Connect your Meta Quest headset

### Quest Link not working

*   If applicable, quit the currently running app on Quest
*   Make sure that only one headset is connected to the PC
*   Try Air Link. It is usually more robust when Wi-Fi is good.
    *   Please note: cable link will not work if Air Link is active.
*   Meta Quest Link > Settings > Beta > Restart Quest Link
*   Meta Quest Debug Tool > Restart Meta service
*   Kill all Meta-related processes in Task Manager
*   Meta Quest Developer Hub must show "Device Setup" and "USB Test" when clicking a headset
    *   If not, restart the PC
*   Try reinstalling the Link driver on your computer. You can find the installable Meta driver file at `c:\Program Files\Oculus\Support\oculus-drivers\oculus-driver.exe`.
*   If connection issues persist, follow these steps for purging rotten configs: https://www.reddit.com/r/OculusQuest/comments/uv5clc/completely_remove_all_oculus_software/

### Quest Link performance tuning

*   Apparently Quest framerate should be set to standard 72, otherwise tearing can occur.
*   Turning off USB power saving might help with connection problems.
*   Important: Visible Game tabs in Unity that are attached to the Layout currently (Unity 2022) produce significant frame drops with Quest Link. Therefore, hide the Game tab or detach it to a floating window.

## File Management

### Samples loading

*   See the [OpenSoundLab Quickstart](quickstart.md)

### Fix, purge and fully update samples and tutorials

*   Make sure you have your changes in Assets/StreamingAssetsPreZip
*   Do a full build export. Running in Editor or patching a build is not enough. This will overwrite the zips in Assets/StreamingAssets.
*   These zips are deployed to standalone builds, but they are only actually decompressed into your player preference folder if your OpenSoundLab/OpenMultiLab folder does not exist when opening the app. For example, this happens when it has been deleted by hand or by uninstalling the app first. Only then are the default files written to the headset player preference folder.

## Creating new devices

> **Outdated — these instructions will be updated soon.** OpenSoundLab now has a new system for structuring and registering devices: manifest-based collections that are discovered and normalized at runtime by `OSLDeviceRegistry`, instead of hardcoded prefab folders, `DeviceType` enums, spawnable-prefab lists and `[XmlInclude]` registrations. The steps below no longer reflect how devices are created and are struck through pending a rewrite. In the meantime, see [Assets/OSLDevices/README.md](../Assets/OSLDevices/README.md) and the `OpenSoundLab/Devices/Create Device` wizard.

### ~~Setup the prefabs~~

*   ~~Add your Prefab "YourDevice" to Resources/Prefabs, or duplicate a similar one~~
*   ~~Add or copy yourDeviceInterface.cs and yourSignalGenerator.cs in a new folder at /Assets/Scripts/YourDevice~~
*   ~~Add YourDeviceType to MenuItem.DeviceType and give it a label and category index~~
*   ~~Add yourDeviceInterface.cs and yourSignalGenerator.cs to your Prefab, or replace old scripts if the prefab was duplicated~~
*   ~~Add handle scripts, if not copied~~
*   ~~Also create a Prefab Variant of the Prefab in MenuPrefabs~~
    *   ~~Remove all scripts on the Prefab Variant~~
    *   ~~Define appearance in the menu~~
    *   ~~TODO: reactivate the auto stripping script?~~
        *   ~~Add Menu Variant to RemoveDeviceComponents.cs in RemoveDeviceComponents~~

### ~~Sync device for multi-user~~

*   ~~Required for spawning from menu: add to Registered Spawnable Prefabs in both LocalNetworkManager and OslRelayNetworkManager in NetworkManager, i.e. in both oslLocalNetworkScene and oslRelayNetworkScene. Do not press "Populate Spawnable Prefabs", since that adds too much undesired stuff.~~
*   ~~Add NetworkIdentity, NetworkAuthority, NetworkTransform and whatever else you want to sync on the network, if not copied~~

### ~~Make your device copyable and savable~~

*   ~~Define YourData in yourDeviceInterface.cs, implement GetData() and Load(InstrumentData d)~~
*   ~~Add YourData and YourDeviceType to xmlUpdate.cs. That is two separate switch statements, add them to both.~~
*   ~~Add [XmlInclude(typeof(YourData))] to SaveLoadInterface.cs~~

## Request integration in official repo and builds

*   Fork the main repository before coding on GitHub
*   Clone your fork to local
*   Create a feature branch for your developments
*   Push to your own fork on GitHub
*   Create and send a pull request for your commits

## Useful ADB Commands

You can run adb commands via PowerShell, Terminal or Meta Quest Developer Hub (MQDH). You can use Chocolatey on Windows or Homebrew on macOS to install ADB system-wide as a package.

### General cheatsheet

*   `adb shell`
*   `adb tcpip 5555` for wireless debugging
*   `adb connect 192.168.178.34:5555` with the IP address adapted
*   Record clean 60 fps video on Quest 2:

```bash
setprop debug.oculus.capture.bitrate 10000000; setprop debug.oculus.refreshRate 60; setprop debug.oculus.fullRateCapture 1; setprop debug.oculus.gpuLevel 4; setprop debug.oculus.cpuLevel 4
```

*   Record mostly clean 60 fps video on Quest 3 (macOS version):

```bash
adb shell setprop debug.oculus.refreshRate 120; adb shell setprop debug.oculus.fullRateCapture 0; adb shell setprop debug.oculus.swapInterval 2; adb shell setprop debug.oculus.drrForceDisable 1; adb shell setprop debug.oculus.cpuLevel 5; adb shell setprop debug.oculus.gpuLevel 5; adb shell setprop debug.oculus.capture.bitrate 10000000
```

*   Record mostly clean 60 fps video on Quest 3 (Git Bash on Windows, run this inside the scrcpy folder):

```bash
./adb.exe shell setprop debug.oculus.refreshRate 120; ./adb.exe shell setprop debug.oculus.fullRateCapture 0; ./adb.exe shell setprop debug.oculus.swapInterval 2; ./adb.exe shell setprop debug.oculus.drrForceDisable 1; ./adb.exe shell setprop debug.oculus.cpuLevel 5; ./adb.exe shell setprop debug.oculus.gpuLevel 5; ./adb.exe shell setprop debug.oculus.capture.bitrate 10000000
```

*   `"cat /proc/meminfo" | grep MemFree`
*   `adb logcat -s VrApi`
*   `adb logcat | grep Unity`

### Run scrcpy with Meta Quest

Please note: scrcpy sometimes shows flashing glitches on Meta Quest 3 when using `--crop`, `--angle` or interacting with the Meta menus. Consider doing your cropping and rotating in OBS instead, although it might work fine for you.

*   Install scrcpy. On macOS run:

```bash
brew install scrcpy
```

*   Connect the Quest headset via USB and accept ADB debugging in the headset.
    *   Sometimes this prompt does not appear. Either you already allow debugging connections and can proceed or you have to make that prompt reappear again. Instructions on the latter will follow.
*   Then open Terminal, assuming that you have ADB installed system-wide.
*   Enter this command. This will identify the IP address of the connected headset, open up a TCP/IP ADB server on the headset, connect to the headset via Wi-Fi and open a scrcpy connection via TCP/IP. Remove the cropping and rotating if you see flashes. You can remove the USB cable when you see the stream:

```bash
adb shell svc usb setFunctions adb; \
adb kill-server; \
sleep 1; \
ip=$(adb shell ip -o -4 addr show up | awk '!/ lo /{print $4}' | cut -d/ -f1 | grep -E '^(10\.|172\.(1[6-9]|2[0-9]|3[0-1])\.|192\.168\.)' | head -n1); \
sleep 1; \
adb kill-server; \
adb tcpip 5555; \
sleep 1; \
adb connect $ip:5555; \
sleep 1; \
scrcpy --select-tcpip -b 16M --crop=2064:2208:0:0 --angle=19
```

*   You can also do these steps separately:

```bash
adb shell "ip addr show wlan0 | grep 'inet ' | awk '{print \$2}' | cut -d/ -f1" | xargs -I {} sh -c "adb tcpip 5555 && adb connect {}"
scrcpy -b 16M --crop=2064:2208:0:0
```

*   For streaming:

```bash
scrcpy -b 20M --video-buffer=100 --no-audio --max-fps=60
```

*   With Git Bash on Windows:

```bash
./adb.exe shell "ip addr show wlan0 | grep 'inet ' | awk '{print \$2}' | cut -d/ -f1" | xargs -I {} sh -c "./adb.exe tcpip 5555 && ./adb.exe connect {}"
```
