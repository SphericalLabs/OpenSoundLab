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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class ClientAudioTrigger : NetworkBehaviour
{
    private AudioSource audioSource;
    public int position = 0;
    public int samplerate = 44100;
    public float baseFrequency = 440f; // Default frequency for A4 (440 Hz)
    private float currentFrequency;
    public float amplitude = 0.5f;
    public float gain = 1f;
    private bool isPlaying = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (isLocalPlayer /*&& !isPlaying*/ && isClientOnly)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            //if (keyboard.aKey.wasPressedThisFrame) PlayNote(440f);
            //if (keyboard.sKey.wasPressedThisFrame) PlayNote(466.16f);
            //if (keyboard.dKey.wasPressedThisFrame) PlayNote(493.88f);
            if (keyboard.fKey.wasPressedThisFrame) PlayNote(523.25f);
            if (keyboard.gKey.wasPressedThisFrame) PlayNote(554.37f);
            if (keyboard.hKey.wasPressedThisFrame) PlayNote(587.33f);
            if (keyboard.jKey.wasPressedThisFrame) PlayNote(622.25f);
            if (keyboard.kKey.wasPressedThisFrame) PlayNote(659.26f);
            if (keyboard.lKey.wasPressedThisFrame) PlayNote(698.46f);
        }
    }

    [Command]
    void PlayNote(float frequency)
    {
        currentFrequency = frequency;
        // Set the audio source properties for the sine wave
        audioSource.pitch = 1f; // Set pitch to 1 (no change)
        audioSource.volume = amplitude;
        audioSource.clip = AudioClip.Create("Note", 44100 * 2, 1, 44100, true, OnAudioRead, OnAudioSetPosition);
        audioSource.Play();
    }

    void OnAudioRead(float[] data)
    {
        int count = 0;
        while (count < data.Length)
        {
            data[count] = Mathf.Sin(2 * Mathf.PI * currentFrequency * position / samplerate);
            position++;
            count++;
        }
    }

    void OnAudioSetPosition(int newPosition)
    {
        position = newPosition;
    }
}

