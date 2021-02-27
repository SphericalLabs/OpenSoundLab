// Copyright 2017 Google LLC
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
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class clipPlayerSimple : clipPlayer
{

    public float playbackSpeed = 1;
    public int ID = 0;

    public float amplitude = 1;
    public signalGenerator seqGen, speedGen, ampGen;

    public bool seqMuted = false; // electribe-ish midi sequence muting

    bool active = false;
    
    float _lastBuffer = 0;
    float[] lastSeqGen;

    [DllImport("SoundStageNative")]
    public static extern float ClipSignalGenerator(float[] buffer, float[] speedBuffer, float[] ampBuffer, float[] seqBuffer, int length, float[] lastSeqGen, int channels, bool speedGen, bool ampGen, bool seqGen, float floatingBufferCount
       , int[] sampleBounds, float playbackSpeed, System.IntPtr clip, int clipChannels, float amplitude, bool playdirection, bool looping, double _sampleDuration, int bufferCount, ref bool active);

    [DllImport("SoundStageNative")]
    public static extern void SetArrayToFixedValue(float[] buffer, int length, float value);

    float[] speedBuffer = new float[0];
    float[] ampBuffer = new float[0];
    float[] seqBuffer = new float[0];

    void Start()
    {
        lastSeqGen = new float[] { 0, 0 };
    }

    public void Play(float speed = 1)
    {
        playbackSpeed = speed;
        _lastBuffer = sampleBounds[0];
        active = true;
    }

    public void Stop()
    {
        active = false;
    }

    public override void processBuffer(float[] buffer, double dspTime, int channels)
    {
        if (!loaded) return;
        floatingBufferCount = _lastBuffer;

        if (seqBuffer.Length != buffer.Length)
            System.Array.Resize(ref seqBuffer, buffer.Length);
        if (speedBuffer.Length != buffer.Length)
            System.Array.Resize(ref speedBuffer, buffer.Length);
        if (ampBuffer.Length != buffer.Length)
            System.Array.Resize(ref ampBuffer, buffer.Length);

        if (!seqMuted)
        {
            if (seqGen != null) seqGen.processBuffer(seqBuffer, dspTime, channels);
        } else {
            SetArrayToFixedValue(seqBuffer, buffer.Length, -1f);
        }
        if (speedGen != null) speedGen.processBuffer(speedBuffer, dspTime, channels);
        if (ampGen != null) ampGen.processBuffer(ampBuffer, dspTime, channels);


        floatingBufferCount = ClipSignalGenerator(buffer, speedBuffer, ampBuffer, seqBuffer, buffer.Length, lastSeqGen, channels, speedGen != null, ampGen != null, seqGen != null, floatingBufferCount, sampleBounds,
       playbackSpeed, m_ClipHandle.AddrOfPinnedObject(), clipChannels, amplitude, true, false, _sampleDuration, bufferCount, ref active);

        _lastBuffer = floatingBufferCount;
    }
}
