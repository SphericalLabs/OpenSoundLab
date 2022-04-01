// Copyright 2017-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
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
using System.Runtime.InteropServices;

public class SampleHoldSignalGenerator : signalGenerator
{

    public signalGenerator incoming, trigSig;

    float[] trigBuffer = new float[] { 0f, 0f };
    float[] incomingBuffer = new float[] { 0f, 0f };
    float lastTrig = -1f;
    float holdVal = 0f;

    [DllImport("SoundStageNative")]
    public static extern void SetArrayToSingleValue(float[] a, int length, float val);

    [DllImport("SoundStageNative")]
    public static extern void CopyArray(float[] from, float[] to, int length);

    public override void processBuffer(float[] buffer, double dspTime, int channels)
    {

        if (incoming == null) // no signal, just 0
        {
            SetArrayToSingleValue(buffer, buffer.Length, 0f);
        }
        else
        {
            if (incomingBuffer.Length != buffer.Length)
                System.Array.Resize(ref incomingBuffer, buffer.Length);
            SetArrayToSingleValue(incomingBuffer, incomingBuffer.Length, 0f);
            incoming.processBuffer(incomingBuffer, dspTime, channels);

            if (trigSig == null) // signal, but no hold, just passthru
            {
                CopyArray(incomingBuffer, buffer, buffer.Length);
            }
            else // sample and hold mode
            {
                if (trigBuffer.Length != buffer.Length)
                    System.Array.Resize(ref trigBuffer, buffer.Length);
                SetArrayToSingleValue(trigBuffer, trigBuffer.Length, 0f);
                trigSig.processBuffer(trigBuffer, dspTime, channels);

                for (int n = 0; n < buffer.Length; n += 2)
                {
                    if(trigBuffer[n] > 0f && lastTrig <= 0f){
                        buffer[n] = buffer[n + 1] = holdVal = incomingBuffer[n]; // left only
                    } else {
                        buffer[n] = buffer[n + 1] = holdVal;
                    }

                    lastTrig = trigBuffer[n];
                }
            }
        }
    }
}
