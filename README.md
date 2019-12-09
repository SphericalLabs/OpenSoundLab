The original project is no longer actively developed or maintained.

This is a port of the original project to an updated Unity, SteamVR and Oculus Engines to get it to running on an Oculus Quest.

# SoundStage VR
SoundStage VR is a virtual reality music sandbox built specifically for room-scale VR. Whether you’re a professional DJ creating a new sound, or a hobbyist who wants to rock out on virtual drums, SoundStage gives you a diverse toolset to express yourself.

This is not an officially supported Google product.

## Requirements
* A SteamVR or Oculus setup running on a PC or an Oculus Quest
* Unity 2019.2.15.f1 if you'd like to modify the project

## Setup
The Unity project can run without any additional components - just open the main scene to get started. 

That being said, the project is missing one third-party components available in the Unity Asset Store:
* [Runtime AudioClip Loader](http://u3d.as/hEP) enables MP3 sample loading and improves the performance of all sample loading

To use this assets, add the full asset package to the *third_party* folder. If it is not automatically replaced, remove the corresponding placeholder scripts in that same folder.

## Included Dependencies
* [Sonic Ether Natural Bloom & Dirty Lens](https://github.com/sonicether/SE-Natural-Bloom-Dirty-Lens) creates the glow and bloom effects seen in the released software
* [SharpZipLib](https://github.com/icsharpcode/SharpZipLib) a compression library 
* [MIDI-dot-NET](https://github.com/jstnryan/midi-dot-net) a MIDI interface library for windows
* [SteamVR Unity Plugin](https://github.com/ValveSoftware/steamvr_unity_plugin) the Steam VR Plugin Also on Unity Asset Store [here](https://assetstore.unity.com/packages/tools/integration/steamvr-plugin-32647)
* [Oculus Platform SDK](https://developer.oculus.com/downloads/package/oculus-platform-sdk/) the Oculus SDK also on the Unity Asset Store [here](https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022)

### Original Credits
###### CREATED BY
Logan Olson

###### SOUND DESIGNER (SAMPLES)
Reek Havok

###### PROGRAMMING CONSULTANT
Giray Ozil <-iumlat

###### MUSIC CONSULTANT
Ron Fish
