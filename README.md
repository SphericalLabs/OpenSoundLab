The original project is no longer actively developed or maintained.

Trying to port it to an updated Unity, SteamVR and Oculus Engines and get it to run on an Oculus Quest.

# SoundStage VR

SoundStage VR is a virtual reality music sandbox built specifically for room-scale VR. Whether you’re a professional DJ creating a new sound, or a hobbyist who wants to rock out on virtual drums, SoundStage gives you a diverse toolset to express yourself.

This is not an officially supported Google product.

## Requirements
* An SteamVR or Oculus running on a PC or Oculus Quest
* Unity 2019.2.13.f1 if you'd like to modify the project

## Setup
The Unity project can run without any additional components - just open the main scene to get started. 

That being said, the project is missing two third-party components available in the Unity Asset Store:

* [SE Natural Bloom & Dirty Lens] (https://github.com/sonicether/SE-Natural-Bloom-Dirty-Lens) creates the glow and bloom effects seen in the released software, no longer available on Unity (http://u3d.as/7v5)
* [Runtime AudioClip Loader](http://u3d.as/hEP) enables MP3 sample loading and improves the performance of all sample loading

To use each of these assets, add the full asset package to the *third_party* folder. If they are not automatically replaced, remove the corresponding placeholder scripts in that same folder.

A full build of the project with the third-party components is included in the *bin* folder.

### Original Credits
###### CREATED BY
Logan Olson

###### SOUND DESIGNER (SAMPLES)
Reek Havok

###### PROGRAMMING CONSULTANT
Giray Ozil <-iumlat

###### MUSIC CONSULTANT
Ron Fish

## Roadmap and Issues
A laundry list of things I could do, might do, maybe won't do, etc. and things that appear to be broken and need fixing.

* settings gear is not translucent like other icons, maybe use the other icons shader or material
* first save does not work
* when closing the menu and the cubes are disappering there is one extra cube in the upper left hand corner that is out of place
* add light objects from jarity fork
* add leap motion support on PC
* add chromecast support in addition to desktop display for PC or replacement on Android?
* Add expander for keyboard and xylo roll
* Add scale support for keyboard and xylo roll
* Add midi on Android
* Remove mountain background and replace with static skyboard, make skyboxes selectable
* Improve performance
* Improve graphics
* Improve sound
* Make sound 3D
* Make velocity drumpads selectable or improve velocity detection to be more reliable from Jarity fork
* Some combinations of connections result in no sound or garbled sound, figure out what those cases are and try and fix
* Fix camera display
* Multiplayer


