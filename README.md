# SoundStage Pro

SoundStage Pro is a fork of Logan Olson's magnificent [SoundStage VR](https://github.com/googlearchive/soundstagevr). The intention of my project is to enhance the original version so that it is better suited for teaching and performing in the context of experimental electronic music, to make it feel less like a game and more like an actual sound laboratory. Feel free to get in touch if that sounds interesting to you. Binaries will soon be made available on SideQuest, Oculus App Lab and maybe the Oculus Quest Store as well. The project is currently funded by an educational grant of the University of Northwestern Switzerland, where I am a lecturer.

Initial Oculus Quest port by [James Surine](https://github.com/plaidpants/soundstagevr). 

Models: 'Rubber Med Gloves' by Komodoz, 'surgical mask' by F2A, 'Safety Goggles' by bariacg.

--

### Notes for making new MenuItems copiable and save/loadable
- add / copy yourDeviceInterface.cs 
- define YourData, implement (de)serialization 
- add YourData to xmlUpdate.cs
- add [XmlInclude(typeof(YourData))] to SaveLoadInterface.cs

### Notes for deploying with Oculus
- Project Settings -> Player -> Android -> Publishing Settings -> Keystore Manager
- Add keys to .gitignore
- Oculus -> Create store-compatible AndroidManifest.xml
- Project Settings -> Player -> Android -> Other Settings -> Identification -> Minimum SDK Version 23
- Add testing users via Oculus email to testing channel
- Basic info needs to be provided for app
- App will show up store of companion app and headset, i.e. by search for string
- Optional: Add an automatic sign-in, see https://forum.unity.com/threads/android-keystore-passwords-not-saved-between-sessions.235213/#post-1737809

### Samples loading ###
- On Quest filesystem: /Android/data/unity.HardLightLabsLLC.SoundStage/Samples
- You can upload samples there via USB (Windows Explorer, SideQuest, adb, etc)
- Place aiff and wav samples here, prefer 44.1/48khz, 16bit, mono/stereo
- Only one folder depth allowed, i.e. Samples/subfolders is ok
- Place preset samples in /Assets/StreamingAssets as Samples.tgz
- Will be unpacked on install

### Compile native ARM code for Android ###
- Download Android NDK from https://developer.android.com/ndk/downloads
- Update path in /Assets/SoundStageNative/SoundStageNative/build_plugin.sh
- Run build_plugin.sh
