// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
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

using System.Runtime.InteropServices;

public class clipPlayerSimple : clipPlayer
{

    public float playbackSpeed = 1;
    public int ID = 0;

    public float amplitude = 1;
    public signalGenerator seqGen, freqExpGen, ampGen;
    public SingleDeviceInterface devInterface;

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

    void Start()
    {
        lastSeqGen = new float[] { 0, 0 };
        devInterface = GetComponent<SingleDeviceInterface>();
    }

    public void Play()
    {
        lock (lockObject)
        {
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

            SetArrayToSingleValue(trigBuffer, buffer.Length, 0f);
            SetArrayToSingleValue(freqExpBuffer, buffer.Length, 0f);
            SetArrayToSingleValue(ampBuffer, buffer.Length, 0f);

            if (seqGen != null) seqGen.processBuffer(trigBuffer, dspTime, channels);
            if (freqExpGen != null) freqExpGen.processBuffer(freqExpBuffer, dspTime, channels);
            if (ampGen != null) ampGen.processBuffer(ampBuffer, dspTime, channels);

            if (seqGen != null)
            {
                // Detect presence of at least one trigger pulse
                // Port to NATIVE at some point
                for (int n = 0; n < buffer.Length; n += channels) // left only
                {
                    if (trigBuffer[n] > 0f && lastTriggValue <= 0f)
                    {
                        devInterface.flashTriggerButton(); // was activated by trigger signal
                        lastTriggValue = trigBuffer[n];
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
