// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2026 OSLLv1 Sphericals OpenSoundLab
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
// you may not use this file except in compliance with the License.
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
using System.Collections;
using Mirror;

public class signalGenerator : MonoBehaviour
{
    protected bool hasAlreadyBeenCalledInThisBufferRun = false;

    public int index = 0;

    protected double _sampleRate;
    protected double _sampleDuration;
    public double _phase;

    public bool useNativeIfAvailable = true;

    float levelVal = 1;

    protected const int MAX_BUFFER_LENGTH = 2048; // Very important to enforce this

    public virtual void Awake()
    {
        _phase = 0;
        _sampleRate = AudioSettings.outputSampleRate;
        _sampleDuration = 1.0 / AudioSettings.outputSampleRate;
    }

    public virtual void updateTape(string s)
    {

    }

    public virtual void trigger(int c)
    {

    }

    public virtual void trigger(int c, float f)
    {

    }

    public virtual void modBuffer(double dspTime, int channels, float[] buffer)
    {

    }

    public float previousLastSample = 0f;
    public float vizSample = 0f;

    // Visualization uses a per-buffer trigger latch to avoid missed pulses.
    // The latch is consumed by the render thread to keep visuals stable.
    public bool vizTriggerLatched = false;

    public void processBuffer(float[] buffer, double dspTime, int channels)
    {
        processBufferImpl(buffer, dspTime, channels);

        if (masterControl.instance == null || masterControl.instance.WireSetting != WireMode.Visualized)
        {
            vizTriggerLatched = false;
            previousLastSample = buffer[buffer.Length - channels]; // last sample of stereo setup
            return;
        }

        if (!vizTriggerLatched)
        {
            vizSample = buffer[0];
            for (int i = 0; i < buffer.Length; i += channels)
            {
                if (i == 0)
                {
                    if (isRisingEdge(buffer[0], previousLastSample))
                    {
                        vizTriggerLatched = true;
                        //vizSample = buffer[0];
                        break;
                    }
                }
                else
                {
                    if (isRisingEdge(buffer[i], buffer[i - channels]))
                    {
                        vizTriggerLatched = true;
                        vizSample = buffer[i];
                        break;
                    }
                }
            }
        }

        previousLastSample = buffer[buffer.Length - channels]; // last sample of stereo setup
    }

    public bool consumeVizTrigger()
    {
        if (!vizTriggerLatched) return false;
        vizTriggerLatched = false;
        return true;
    }

    public static bool isRisingEdge(float sample, float lastSample)
    {
        return sample > 0f && lastSample <= 0f;
    }

    public static bool isFallingEdge(float sample, float lastSample)
    {
        return sample <= 0f && lastSample > 0f;
    }

    public static bool containsTrigger(float[] buffer, int channels, ref float lastSample)
    {
        for (int i = 0; i < buffer.Length; i += channels)
        {
            float sample = buffer[i];
            if (isRisingEdge(sample, lastSample))
            {
                lastSample = sample;
                return true;
            }
            lastSample = sample;
        }
        return false;
    }

    public virtual void processBufferImpl(float[] buffer, double dspTime, int channels)
    {

    }

    protected bool recursionCheckPre()
    {
        if (hasAlreadyBeenCalledInThisBufferRun)
        {
            hasAlreadyBeenCalledInThisBufferRun = false;
            return false;
        }
        hasAlreadyBeenCalledInThisBufferRun = true;
        return true;
    }

    protected void recursionCheckPost()
    {
        hasAlreadyBeenCalledInThisBufferRun = false;
    }

    public virtual float[] getBuffer(double dspTime, int channels, int bufferLength, bool modFreq = false, float requestedFreq = 440f, float detuneAmount = 0)
    {
        float[] buffer = new float[bufferLength];

        for (int i = 0; i < buffer.Length; i += channels)
        {
            double sample = Mathf.Sin((float)_phase * 2 * Mathf.PI);

            float frequency = 440;
            float amplitude = 0.5f;

            _phase += frequency * _sampleDuration;

            if (_phase > 1.0) _phase -= 1.0;

            buffer[i] = (float)sample * amplitude;
            buffer[i + 1] = (float)sample * amplitude;

            dspTime += _sampleDuration;
        }

        return buffer;
    }
}
