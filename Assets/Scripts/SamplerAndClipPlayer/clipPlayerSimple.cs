// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This file is part of OpenSoundLab, which is based on SoundStage VR.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
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
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class clipPlayerSimple : clipPlayer
{

    public float playbackSpeed = 1;
    public int ID = 0;

    public float amplitude = 1;
    public signalGenerator seqGen, freqExpGen, ampGen;

    public bool seqMuted = false; // electribe-ish midi sequence muting

    bool active = false;
    
    float _lastBuffer = 0;
    float[] lastSeqGen;

    float lastPlaybackSpeed = 0f;
    float lastAmplitude = 0f;

  [DllImport("SoundStageNative")]
    public static extern float ClipSignalGenerator(float[] buffer, float[] freqExpBuffer, float[] freqLinBuffer, float[] ampBuffer, float[] seqBuffer, int length, float[] lastSeqGen, int channels, bool freqExpGen, bool freqLinGen, bool ampGen, bool seqGen, float floatingBufferCount
, int[] sampleBounds, float playbackSpeed, float lastPlaybackSpeed, System.IntPtr clip, int clipChannels, float amplitude, float lastAmplitude, bool playdirection, bool looping, double _sampleDuration, int bufferCount, ref bool active, int windowLength);


    [DllImport("SoundStageNative")]
    public static extern void SetArrayToSingleValue(float[] buffer, int length, float value);

    float[] freqExpBuffer = new float[0];
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
        if (!recursionCheckPre()) return; // checks and avoids fatal recursions
        if (!loaded) return;
        floatingBufferCount = _lastBuffer;

        if (seqBuffer.Length != buffer.Length)
            System.Array.Resize(ref seqBuffer, buffer.Length);
        if (freqExpBuffer.Length != buffer.Length)
            System.Array.Resize(ref freqExpBuffer, buffer.Length);
        if (ampBuffer.Length != buffer.Length)
            System.Array.Resize(ref ampBuffer, buffer.Length);

        SetArrayToSingleValue(seqBuffer, buffer.Length, 0f);
        SetArrayToSingleValue(freqExpBuffer, buffer.Length, 0f);
        SetArrayToSingleValue(ampBuffer, buffer.Length, 0f);

        if (seqGen != null) seqGen.processBuffer(seqBuffer, dspTime, channels);
        if (freqExpGen != null) freqExpGen.processBuffer(freqExpBuffer, dspTime, channels);
        if (ampGen != null) ampGen.processBuffer(ampBuffer, dspTime, channels);

        floatingBufferCount = ClipSignalGenerator(buffer, freqExpBuffer, null, ampBuffer, seqBuffer, buffer.Length, lastSeqGen, channels, freqExpGen != null, false, ampGen != null, seqGen != null, floatingBufferCount, sampleBounds,
          playbackSpeed, lastPlaybackSpeed, m_ClipHandle.AddrOfPinnedObject(), clipChannels, amplitude, lastAmplitude, true, false, _sampleDuration, bufferCount, ref active, 0);
                  
        _lastBuffer = floatingBufferCount;

        lastAmplitude = amplitude;
        lastPlaybackSpeed = playbackSpeed;
        recursionCheckPost();
  }
}
