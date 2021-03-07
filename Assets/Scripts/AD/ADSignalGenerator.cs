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

    public signalGenerator incoming, attackInput, releaseInput;
    //float attackVal = 0; // 0,1
    //float releaseVal = 0;
    int attackLength = 0;
    int releaseLength = 0;
    int attackLengthFinal = 0;
    int releaseLengthFinal = 0;
    float linearity = 1f;

    int length = 44100 * 5; // 5 seconds

    int stage = 0;
    int counter = 0;

    bool isRunning = false;
    bool isTrigger = false;

    float[] pulseBuffer = new float[] { -1, -1 };
    float[] attackBuffer = new float[] { -1, -1 };
    float[] releaseBuffer = new float[] { -1, -1 };

    //bool lastTrigger = false;
    //float lastBuffer = -1; // what is this for?

    public void Awake()
    {
    }

    public void setAttack(float val)
    {
        attackLength = Mathf.RoundToInt(val * length) + 1;
    }

    public void setRelease(float val)
    {
        releaseLength = Mathf.RoundToInt(val * length) + 1;
    }

    public void setLinearity(float val)
    {
        if (val >= 0.5)
        {
            linearity = Utils.map(val, 0.5f, 1f, 1f, 0.1f);
        }
        else
        {
            linearity = Utils.map(val, 0f, 0.5f, 10f, 1f);
        }
    }

    [DllImport("SoundStageNative")]
    public static extern bool GetBinaryState(float[] buffer, int length, int channels, ref float lastBuf);

    [DllImport("SoundStageNative")]
    public static extern bool IsPulse(float[] buffer, int length);

    [DllImport("SoundStageNative")]
    public static extern void SetArrayToSingleValue(float[] a, int length, float val);

    public override void processBuffer(float[] buffer, double dspTime, int channels)
    {
        if (incoming != null)
        {
            if (pulseBuffer.Length != buffer.Length)
                System.Array.Resize(ref pulseBuffer, buffer.Length);

            incoming.processBuffer(pulseBuffer, dspTime, channels);

            isTrigger = ((pulseBuffer[0] > -1f) && (pulseBuffer[1] > -1f));
        }
        else
        {
            isTrigger = false;
        }

        if (attackInput != null)
        {
            if (attackBuffer.Length != buffer.Length)
                System.Array.Resize(ref attackBuffer, buffer.Length);

            attackInput.processBuffer(attackBuffer, dspTime, channels);
            attackLengthFinal = attackLength + Mathf.RoundToInt(attackBuffer[0] * length); // left only
        }
        else
        {
            attackLengthFinal = attackLength;
        }

        if (releaseInput != null)
        {
            if (releaseBuffer.Length != buffer.Length)
                System.Array.Resize(ref releaseBuffer, buffer.Length);

            releaseInput.processBuffer(releaseBuffer, dspTime, channels);
            releaseLengthFinal = releaseLength + Mathf.RoundToInt(releaseBuffer[0] * length); // left only
        }
        else
        {
            releaseLengthFinal = releaseLength;
        }

        // play, reset
        if (isTrigger)
        {
            counter = 0;
            stage = 0;
            isRunning = true;
        }

        for (int n = 0; n < buffer.Length; n += 2)
        {
            if (isRunning)
            {

                if (stage == 0)
                {
                    buffer[n] = buffer[n + 1] = Mathf.Pow((float)counter / (float)attackLengthFinal, linearity) * 2f - 1f;
                    //buffer[n] = buffer[n + 1] = Utils.lerp(-1f, 1f, counter / attackLengthFinal);
                    counter++;
                    //Debug.Log(buffer[n] + " in stage 0");
                    if (counter > attackLengthFinal)
                    {
                        stage = 1;
                        counter = 0;
                    }
                }
                else if (stage == 1)
                {
                    buffer[n] = buffer[n + 1] = Mathf.Pow(1f - (float)counter / (float)releaseLengthFinal, linearity) * 2f - 1f;
                    //buffer[n] = buffer[n + 1] = Utils.lerp(1f, -1f, counter / releaseLengthFinal);
                    counter++;
                    //Debug.Log(buffer[n] + " in stage 1");
                    if (counter > releaseLengthFinal)
                    {
                        stage = 0;
                        counter = 0;
                        isRunning = false;
                    }
                }

            }
            else
            {
                buffer[n] = buffer[n + 1] = -1f;
                //Debug.Log(buffer[n] + " in stage 2");
            }

            // hotfix for occasional NaNs that crash other devices. occur when linearity != 1
            if (System.Single.IsNaN(buffer[n]))
            {
                buffer[n] = buffer[n + 1] = -1f;
                //Debug.Log("NaN in stage: " + stage);
                //Debug.Log("counter: " + counter);
                //Debug.Log("attack: " + attackLengthFinal);
                //Debug.Log("release: " + releaseLengthFinal);
            }

        }

        //for(int i = 0; i < buffer.Length; i += 2){
        //    Debug.Log(buffer[n]);
        //}

    }

}

