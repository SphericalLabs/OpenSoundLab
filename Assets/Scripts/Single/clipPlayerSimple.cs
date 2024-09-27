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

using Oculus.Interaction.PoseDetection;
using System.Runtime.InteropServices;
using UnityEngine;

public class clipPlayerSimple : clipPlayer
{

    public int ID = 0;

    public float playbackSpeed = 1;
    public float amplitude = 1;
    public float sampleStart = 0;

    public signalGenerator seqGen, freqExpGen, ampGen, startGen;
    public samplerOneDeviceInterface devInterface;

    bool active = false;
    
    //double _lastBuffer = 0;
    float[] lastSeqGen;

    float lastPlaybackSpeed = 0f;
    float lastAmplitude = 0f;

  [DllImport("OSLNative")]
    public static extern double ClipSignalGenerator(float[] buffer, float[] freqExpBuffer, float[] freqLinBuffer, float[] ampBuffer, float[] seqBuffer, int length, float[] lastSeqGen, int channels, bool freqExpGen, bool freqLinGen, bool ampGen, bool seqGen, double floatingBufferCount
, int[] sampleBounds, float playbackSpeed, float lastPlaybackSpeed, System.IntPtr clip, int clipChannels, float amplitude, float lastAmplitude, bool playdirection, bool looping, double _sampleDuration, int bufferCount, ref bool active, int windowLength);


    [DllImport("OSLNative")]
    public static extern void SetArrayToSingleValue(float[] buffer, int length, float value);

    float[] freqExpBuffer = new float[0];
    float[] ampBuffer = new float[0];
    float[] trigBuffer = new float[0];
    float[] startBuffer = new float[0];

    void Start()
    {
        lastSeqGen = new float[] { 0, 0 };
        devInterface = GetComponent<samplerOneDeviceInterface>();
    }



    public void Play()
    {
        lock (lockObject)
        {
            updateSampleBounds(startGen != null ? startBuffer[0] : 0f);
            floatingBufferCount = _lastBuffer = sampleBounds[0] + 1; // WARNING: Due to the code structure in the native ClipSignalGenerator function resetting to 0 (instead of 0 + 1) would mean that playback does not work anymore for speeds lower than 1f. It would always floor() to 0 and would not move through the file anymore.
            active = true;
        }
    }

    public void Play(float speed){
        playbackSpeed = speed;
        Play();
    }

    public void Stop()
    {
        active = false;
    }


    public int consolidatedSampleLength = -1; // sample length regardless of channel count
    public void updateSampleBounds(float externalStartOffset = 0f)
    {
        if (!loaded) return;

        // todo: only calc on sample load?
        // otherwise NaN errors at times
        consolidatedSampleLength = (int)(clipSamples.Length / clipChannels - 1);

        sampleBounds[0] = (int)(consolidatedSampleLength * (sampleStart + externalStartOffset) + 1);
        sampleBounds[0] = Mathf.Clamp(sampleBounds[0], 1, consolidatedSampleLength); // don't start from sample 0 because of native implementation
    }

    float lastTriggValue = 0f;

    private readonly object lockObject = new object(); // Lock object

    public override void processBuffer(float[] buffer, double dspTime, int channels)
    {
        if (!recursionCheckPre()) return; // checks and avoids fatal recursions
        
        lock (lockObject)
        {
            if (!loaded) return;

            floatingBufferCount = _lastBuffer;

            if (trigBuffer.Length != buffer.Length)
                System.Array.Resize(ref trigBuffer, buffer.Length);
            if (freqExpBuffer.Length != buffer.Length)
                System.Array.Resize(ref freqExpBuffer, buffer.Length);
            if (ampBuffer.Length != buffer.Length)
                System.Array.Resize(ref ampBuffer, buffer.Length);
            if (startBuffer.Length != buffer.Length)
                System.Array.Resize(ref startBuffer, buffer.Length);

            SetArrayToSingleValue(trigBuffer, buffer.Length, 0f);
            SetArrayToSingleValue(freqExpBuffer, buffer.Length, 0f);
            SetArrayToSingleValue(ampBuffer, buffer.Length, 0f);
            SetArrayToSingleValue(startBuffer, buffer.Length, 0f);

            if (seqGen != null) seqGen.processBuffer(trigBuffer, dspTime, channels);
            if (freqExpGen != null) freqExpGen.processBuffer(freqExpBuffer, dspTime, channels);
            if (ampGen != null) ampGen.processBuffer(ampBuffer, dspTime, channels);
            if (startGen != null) startGen.processBuffer(startBuffer, dspTime, channels);

            if (seqGen != null)
            {
                // Detect presence of at least one trigger pulse
                // Port to NATIVE at some point, since trigger signal is also being scanned there already
                for (int n = 0; n < buffer.Length; n += channels) // left only
                {
                    if (trigBuffer[n] > 0f && lastTriggValue <= 0f)
                    {
                        // careful, this is specific to SamplerOne but this code is also used on Drum:
                        devInterface?.flashTriggerButton(); // was activated by trigger signal

                        lastTriggValue = trigBuffer[n]; // read in current sample in startGen

                        updateSampleBounds(startGen != null ? startBuffer[n] : 0f); // sample and hold the current start dial and start signal value

                        break;
                    }
                    lastTriggValue = trigBuffer[n];
                }
            }

            floatingBufferCount = ClipSignalGenerator(buffer, freqExpBuffer, null, ampBuffer, trigBuffer, buffer.Length, lastSeqGen, channels, freqExpGen != null, false, ampGen != null, seqGen != null, floatingBufferCount, sampleBounds,
                  playbackSpeed, lastPlaybackSpeed, m_ClipHandle.AddrOfPinnedObject(), clipChannels, amplitude, lastAmplitude, true, false, _sampleDuration, bufferCount, ref active, 0);

            _lastBuffer = floatingBufferCount;

            lastAmplitude = amplitude;
            lastPlaybackSpeed = playbackSpeed;
        }
        recursionCheckPost();
  }
}
