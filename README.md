# SoundStage Pro

Forked from Logan Olson's magnificent [SoundStage VR](https://github.com/googlearchive/soundstagevr).

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
- Oculus -> Create store-compativle AndroidManifest.xml
- Project Settings -> Player -> Android -> Other Settings -> Identification -> Minimum SDK Version 23
- Add testing users via Oculus email to testing channel
- Basic info needs to be provided for app
- App will show up store of companion app and headset, i.e. by search for string

### Samples loading ###
- \Android\data\unity.HardLightLabsLLC.SoundStage\Samples
- You can upload samples here via USB (Windows Explorer, SideQuest, adb, etc)
- Place aiff and wav samples here, prefer 44.1/48khz, 16bit, stereo!
- Only one folder depth allowed

- Place preset samples in /Assets/StreamingAssets as Samples.tgz
- Will be unpacked on install
