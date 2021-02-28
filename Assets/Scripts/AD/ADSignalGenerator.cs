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
using System.Runtime.InteropServices;

public class ADSignalGenerator : signalGenerator
{

    public signalGenerator incoming;
    public float attackVal = 0; // 0,1
    public float releaseVal = 0;
    float attackLength = 0;
    float releaseLength = 0;

    int stage = 0;
    int counter = 0;

    bool isRunning = false;
    bool isTrigger = false;

    float[] pulseBuffer = new float[] { -1, -1 };
    bool lastTrigger = false;
    float lastBuffer = -1; // what is this for?

    public void Awake()
    {
    }

    [DllImport("SoundStageNative")]
    public static extern bool GetBinaryState(float[] buffer, int length, int channels, ref float lastBuf);

    [DllImport("SoundStageNative")]
    public static extern bool IsPulse(float[] buffer, int length);

    [DllImport("SoundStageNative")]
    public static extern void SetArrayToSingleValue(float[] a, int length, float val);

    public override void processBuffer(float[] buffer, double dspTime, int channels)
    {
        // wird zu oft aufgerufen!
        attackLength = attackVal * 44100 * 5; // 3 seconds
        releaseLength = releaseVal * 44100 * 5; // 3 seconds

        if (incoming != null)
        {
            if (pulseBuffer.Length != buffer.Length)
                System.Array.Resize(ref pulseBuffer, buffer.Length);

            incoming.processBuffer(pulseBuffer, dspTime, channels);

            isTrigger = ((pulseBuffer[0] > -1f) && (pulseBuffer[1] > -1f));
        } else {
            isTrigger = false;
        }

        // play, reset
        if (isTrigger)
        {
            counter = 0;
            stage = 0;
            isRunning = true;
        }

        //lastTrigger = isTrigger;

        for (int n = 0; n < buffer.Length; n += 2)
        {
            if (isRunning)
            {

                if (stage == 0)
                {
                    buffer[n] = buffer[n + 1] = Utils.lerp(-1f, 1f, counter / attackLength);
                    counter++;
                    if (counter > attackLength)
                    {
                        stage = 1;
                        counter = 0;
                    }
                }
                else if (stage == 1)
                {
                    buffer[n] = buffer[n + 1] = Utils.lerp(1f, -1f, counter / releaseLength);
                    counter++;
                    if (counter > releaseLength)
                    {
                        stage = 0;
                        counter = 0;
                        isRunning = false;
                        break;
                    }
                }

            } else {
                buffer[n] = buffer[n + 1] = -1f;
            }
        }

    }

}

