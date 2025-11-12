// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
//
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
//
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
//
//
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
//
// Licensed under the Apache License, Version 2.0 (the "License");
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;

// The code example shows how to implement a metronome that procedurally
// generates the click sounds via the OnAudioFilterRead callback.
// While the game is paused or suspended, this time will not be updated and sounds
// playing will be paused. Therefore developers of music scheduling routines do not have
// to do any rescheduling after the app is unpaused

[RequireComponent(typeof(AudioSource))]
public class AudioTest : MonoBehaviour
{
    AudioClip customAudioClip;
    int sampleRate;

    void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;
        // Create a new audio clip with a PCM read callback
        customAudioClip = AudioClip.Create("CustomAudio", sampleRate, 1, sampleRate, true, OnAudioRead);

        // Assign the custom audio clip to an AudioSource
        AudioSource audioSource = GetComponent<AudioSource>();
        audioSource.clip = customAudioClip;

        // Start playing the audio
        audioSource.Play();
    }

    // PCM read callback function
    void OnAudioRead(float[] data)
    {
        // Generate or provide custom audio data here
        // Populate the 'data' array with audio samples
        // ...

        // For simplicity, let's fill the array with a sine wave
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = Mathf.Sin(2 * Mathf.PI * 440 * i / (float)sampleRate);
        }
    }

}
