# Differences from SoundStage VR

OpenSoundLab (OSL) is a fork of Logan Olson's [SoundStage VR](https://github.com/googlearchive/soundstagevr). The main [README](../README.md) lists the new devices and headline features that OSL adds on top of SoundStage VR.

Besides those, the following changes have also been added to OSL.

#### New features

*   Master bus overload warning
*   Eurorack-inspired 1V/Oct tracking scheme was added (see the [README](../README.md))
*   Gain: 36db gain module
*   Polarizer: Convert between unipolar and bipolar signals
*   Knob: Bipolar constant / offset generator
*   Glide: Slope limiter

#### Improved features

*   Redesigned look-and-feel and new pictograms
*   Sequencer: Added CV sequencing and both dials and modulation inputs for volume and pitch
*   Filter: tracks at audio rate, allows more extreme resonances and modulations
*   Oscillator: Added linear through-zero FM, reset, PWM, triangle and anti-aliased VCO waveforms via PolyBLEP (Saw/Square/PWM/Sync)
*   Sampler: Added high-quality interpolation (Lagrange, plus Windowed-Sinc on SamplerTwo), threaded sample loading, linear through-zero FM, modulation for loop in/out, windowing
*   Optimized rendering performance for Meta Quest
*   Dynamic resolution scaling (including eye-tracked foveated rendering on Quest Pro)
*   Switched to ARM64 and recent versions of Unity, Oculus SDK and Vulkan
*   Improved performance of waveform displays by rendering them on the GPU
*   Button: Added latched mode
*   Keyboard: Added CV and Gate outputs
*   Multi-user: live client-authoritative world dragging, loading and clearing patches from clients, chunked patch upload (~16 MB) and an overhauled UniVoice voice chat with adaptive jitter buffering
*   Tutorials: now sync across multi-user sessions, spawn on startup and are seekable

#### Removed features

*   Removed default samples
*   Disabled timeline and MIDI for Xylophone and Keyboard until fixed
