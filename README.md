# OpenSoundLab

![OSL Logo](https://github.com/SphericalLabs/OpenSoundLab/blob/master/Assets/Textures/SplashScreens/CoverArt-Landscape-3-OSL.png?raw=true)

\[[Download](https://www.meta.com/en-gb/experiences/opensoundlab/5190305347733325/)\] \[[Trailer](https://www.youtube.com/watch?v=Q8GUHLsC1QI)\] \[[Paper](https://www.cambridge.org/core/journals/organised-sound/article/modular-observers-opensoundlab-and-patchworld-as-case-studies-for-emerging-practices-of-modular-synthesis-in-extended-realities/78178E780CB182CAD92F7987346222FC#article)\] \[[Newsletter](http://eepurl.com/h-9PsD)\] \[[Discord](https://discord.gg/Jrmg5na3Ru)\] \[[Quickstart](https://docs.google.com/document/d/1c9vt-wW-JnW9davSZ76r35cd4dE6xtnyzHEhdrbueOE/edit?usp=sharing)\] \[[Tutorials](https://www.youtube.com/playlist?list=PLSnuTstoP7nDSK8XqfTnln1v3dH0jACu7)\] \[[Developer](https://docs.google.com/document/d/1_EpWIVUfs2bxpx9QFacNEzsJZoaBOB2l7ooHdbXBrBM)\]

OpenSoundLab (OSL) makes modular sound patching three dimensional in a mixed-reality experience using Meta Quest's passthrough mode. Patch simple or complex sounds at home, in your studio or in the field. Learn the foundations of creative sound work through video tutorials that are placed right within your patch.

OSL is a fork of Logan Olson's magnificent [SoundStage VR](https://github.com/googlearchive/soundstagevr). The app significantly enhances the original version so that it is better suited for performing in the context of experimental electronic sound and music, to make it feel less like a game and more like an actual sound laboratory. We recreate the experience of working in spatial setups, but without being bound to mimicking the physical past. Where are the limits of the digital realm, and where are its sweet spots?

The project received generous funding by an educational grant of the University of Applied Sciences and Arts Northwestern Switzerland ([IDCE FHNW](https://www.fhnw.ch/en/about-fhnw/schools/academy-of-art-and-design/institute-digital-communication-environments)) and was further developed in the context of the SNSF Spark research project "Emerging Practices in Modular Synthesis: Towards a Virtual Ethnography for Mixed Realities".

This repository is work-in-progress. Please always link to this repository if you fork, deploy or otherwise redistribute it, in order to keep other users in sync with our ongoing development.

### Installation

OpenSoundLab is now available in the [Meta Quest app store](https://www.meta.com/en-gb/experiences/opensoundlab/5190305347733325/). We won't publish new apk binaries here from now on, but OpenSoundLab is open-source under the OSLLv1 license, so you can modify and build the app from the code here at GitHub.

### Build instructions

*   Clone the repository and check out the desired tag manually. Please note that the Releases page is outdated.
*   Install Unity Hub and register with Unity: https://unity.com/download
*   In Unity Hub click Installs > Install Editor > Archive > Download Archive and install Unity v2022.3.62f3
*   Add the downloaded repository (the folder containing Assets, OSLNative, etc.) to the Unity Hub: Projects > Add Project from Disk (click the triangle for that)
*   Click the editor version of the added repository and select Unity v2022.3.62f3 for Android
*   Open the project for the first time, this might take some time
*   Unity might ask you to restart when done importing, click Restart Editor
*   Build the native OSLNative plugins once before exporting: in Unity click OpenSoundLab > OSLNative > Rebuild. Alternatively enable OpenSoundLab > OSLNative > Rebuild on Build so Unity runs the native rebuild automatically before player builds.
*   On macOS, the OSLNative rebuild can build Android, Windows and macOS plugins. Install full Xcode, Homebrew and `mingw-w64` (`brew install mingw-w64`) first. The Android script can install the required Android command-line packages and NDK through Homebrew if they are missing.
*   On Windows, the OSLNative rebuild can build Windows and Android plugins. Install Visual Studio 2022 or Visual Studio Build Tools with the C++ workload, and install Android SDK/NDK r26b (`26.1.10909125`) through Android Studio or Unity's Android tooling. The Windows script looks for the SDK through `ANDROID_HOME`, `ANDROID_SDK_ROOT` or `ANDROID_NDK_ROOT`.
*   Make sure that the developer mode is activated on the headset
*   Connect your Meta Quest headset via USB
*   If connected for the first time, put on the headset and accept the connection to the computer
*   If you want to use Unity Relay, you have to set up a Unity Project ID and add that in Project Settings > Services
*   In Unity, click File > Build Settings
*   Check if your headset is listed at "Run Device"
*   Click Build and Run, set a destination for apk and wait for the build to complete
*   Put on the headset and check if the app was installed correctly
*   If you already had OpenSoundLab installed from the App Store you might have to deinstall the app first
*   Please note that the Tutorial videos are not included in the repository and thus your build

### Developer Notes

See the [Developer Notes](https://docs.google.com/document/d/1_EpWIVUfs2bxpx9QFacNEzsJZoaBOB2l7ooHdbXBrBM) for instructions on how to add new devices and more. These instructions are still work in progress.

### Mailing list

Please subscribe to our [newsletter](http://eepurl.com/h-9PsD) in order to get updates about new releases.

### Discord

Join the [OpenSoundLab channel](https://discord.com/channels/1020228980583976980) on Discord in order to stay up-to-date, receive/offer support, present your experiments and get to know other OSL users.

### Selected features

##### Mixed reality & collaboration

*   Mixed-reality passthrough without Guardian
*   Real-world depth occlusion via the Meta Depth API, so virtual modules are hidden behind physical objects in passthrough
*   Shared space colocation for local multi-user
*   Multi-user (LAN or Internet) via Mirror, Unity Relay and UniVoice
*   The patch is easily draggable and scalable

##### Sound modules

*   Oscillator: Antialiased oscillator with TZ FM, AM/RM, LFO mode, sync and PWM
*   FilterTwo: Clean, fully modulatable multimode filter (LP/BP/HP/notch) with a wide modulation range
*   AD: Two stage envelope generator with lin/exp control and CV modulation
*   VCA: Added amplifier with modulation and ring modulation
*   Delay: A highly flexible delay line, buffer can range from 1ms to 12.5s, CV modulation
*   Reverb: Classic Freeverb stereo reverb with CV modulation
*   Compressor: Dynamics processor with attack, release, threshold, ratio, bypass, gain and sidechaining
*   Artifact: Jitter, bit crush, downsample and noise
*   SampleHold: Sample-and-hold module

##### Sequencing & timing

*   Sequencer: Flexible sequencer for triggers and CV
*   PhaseGenerator and PhaseToClockDivider: a phase-accurate, resettable clock system with tempo nudging that drives the Sequencer in either phase or clock mode
*   Quantizer: Featuring Sem, Maj, Min, HMaj, HMin, PMaj, PMin, Oct scales and root key, octave dials
*   Eurorack-inspired 1V/Oct tracking scheme

##### Monitoring & control

*   Scope: Oscilloscope / spectral analyser with trigger on rise
*   Controller: record and replay gestural motion paths (hue-cycling trails)
*   Cables visualize the live signal flowing through them, with brightness scaling to signal level

##### Recording & learning

*   Master bus recorder in 48kHz, 24bit, WAV
*   Tutorials: Player console for video tutorials


### FAQ

**Q: There are already so many sound and music apps for VR, why should I consider OpenSoundLab?**

**A:** First of all, OpenSoundLab is among the very few sound apps for XR that are open-source and can thus be adapted, expanded and repaired as needed for your creative or academic needs. Apart from that, the vision for OpenSoundLab is to offer a "mixed-reality first" experience that blends effortlessly in your physical surrounding instead of teleporting you to a fancy (or goofy) virtual environment. Instead of gamification, expansive modular sound work is at the center of OpenSoundLab. OSL is also one of the few modular sound apps that allow you to collaborate with others or host virtual concerts, etc. In combination with the very low latency LAN mode and the shared space colocation functionality it contributes to the idea of a hybrid studio environment in a way that most other apps don't. OpenSoundLab has a rather reduced set of features or devices, but we want to get the user experience right in order to make sure that you flow in spatial way that other apps, or screen-based workflows or physical gear can not achieve. This goes in line with a "modular spirit" that puts creative improvisation from atomic modules at the core.

**Q: Does the OSLLv1 allow me to make my own app and publish it to an app store such as for Meta Quest or Apple Vision?**

**A:** The OSLLv1 license allows you to repair, adapt and port the app to your likings. However, you are not allowed to publish the app to any app store, be it commercially or for free, or offer any other commercial services around the app. You may however distribute your app e.g. via GitHub or send it e.g. to your band members for online collaboration and performances.

**Q: May I use OpenSoundLab to teach paid workshops or do paid gigs or produce and publish paid music?**

**A:** Yes, this is allowed. Please feel invited to share what you do with OpenSoundLab on the Discord server.

**Q: Can I use the trademarks OpenSoundLab and Spherical Labs or sphericals.io for my derivative?**

**A:** OpenSoundLab, Spherical Labs and sphericals.io are trademarks that you may not use for your derivatives. However, if you fork our repository on GitHub, it is ok to keep these trademarks visible in the repository and your app identifiers. As soon as you share your derivative outside of GitHub you should come up with your own names. If in doubt, please reach out.

**Q: Can I create my own devices/features for OSL and share them with others?**

**A:** You are cordially invited to add whatever feature you want to add to your derivative of OSL and you may also share these features as long as you do not publish them through an app store (free or commercially) or through other any means that are commercial by nature. Please feel free to propose your self-developed features for incorporation in OpenSoundLab.

**Q: I want to help to improve OSL. Are you accepting pull requests?**

**A:** Yes, but we have to talk about the license terms for your code contribution. The easiest way to help is by committing precise bug reports in the issues of the GitHub repository. You can also propose new features and improvements there.

**Q: I really want to make money with my OSL derivative, can we talk?**

**A:** Sure, feel free to reach out and discuss your idea.

### Project Team for OpenSoundLab

###### LEAD

Ludwig Zeller

###### MULTI-USER

Chris Elvis Leisi, Christoph Müller

###### DSP

Hannes Barfuss, Ludwig Zeller

###### MENU SYMBOLS

Iman Khoshniataram  
(The menu symbols of OpenSoundLab are licensed as [CC BY-NC-ND 4.0](https://creativecommons.org/licenses/by-nc-nd/4.0/))

###### TESTING & CONSULTING

Anselm Bauer

###### SPONSORING

IDCE FHNW

### Original Credits for SoundStage VR

###### CREATED BY

Logan Olson

###### SOUND DESIGNER (SAMPLES)

Reek Havok

###### PROGRAMMING CONSULTANT

Giray Ozil

###### MUSIC CONSULTANT

Ron Fish

### Other Credits

###### INITIAL QUEST PORT

James Surine

```
git checkout DESIRED_TAG
```

```
git clone https://github.com/SphericalLabs/OpenSoundLab
cd OpenSoundLab
git fetch --tags
git tags
```
