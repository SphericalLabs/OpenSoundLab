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

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class ADSignalGenerator : signalGenerator
{

    public signalGenerator incoming, attackInput, releaseInput;

    int attackLength = 0;
    int releaseLength = 0;
    int attackLengthFinal = 0;
    int releaseLengthFinal = 0;
    float linearity = 1f;

    int length = 48000 * 5; // 5 seconds

    int stage = 0;
    int counter = 0;

    float glidedVal = 0f;

    bool isRunning = false;

    float[] pulseBuffer = new float[] { -1, -1 };
    float[] attackBuffer = new float[] { -1, -1 };
    float[] releaseBuffer = new float[] { -1, -1 };
    
    float lastPulseFloat = -1f;

    public void Awake()
    {
    }

    public void setAttack(float val)
    {
        attackLength = Mathf.RoundToInt(val * length) + 1;
    }

    public void setRelease(float val)
    {
        releaseLength = Mathf.RoundToInt(Utils.map(val, 0f, 1f, 0.01f, 1f) * length) + 1;
    }

    public void setLinearity(float val)
    {
        if (val >= 0.5)
        {
            linearity = Utils.map(val, 0.5f, 1f, 1f, 0.05f);
        }
        else
        {
            linearity = Utils.map(val, 0f, 0.5f, 20f, 1f);
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
        if (!recursionCheckPre()) return; // checks and avoids fatal recursions

        if (incoming != null)
        {
            if (pulseBuffer.Length != buffer.Length)
                System.Array.Resize(ref pulseBuffer, buffer.Length);

            SetArrayToSingleValue(pulseBuffer, pulseBuffer.Length, 0f);
            incoming.processBuffer(pulseBuffer, dspTime, channels);
        }

        if (attackInput != null)
        {
            if (attackBuffer.Length != buffer.Length)
                System.Array.Resize(ref attackBuffer, buffer.Length);

            SetArrayToSingleValue(attackBuffer, attackBuffer.Length, 0f);
            attackInput.processBuffer(attackBuffer, dspTime, channels);
            attackLengthFinal = Mathf.RoundToInt(Mathf.Clamp(attackLength + attackBuffer[0] * length, 0, length)); // left only
        }
        else
        {
            attackLengthFinal = attackLength;
        }

        if (releaseInput != null)
        {
            if (releaseBuffer.Length != buffer.Length)
                System.Array.Resize(ref releaseBuffer, buffer.Length);

            SetArrayToSingleValue(releaseBuffer, releaseBuffer.Length, 0f); 
            releaseInput.processBuffer(releaseBuffer, dspTime, channels);
            releaseLengthFinal = Mathf.RoundToInt(Mathf.Clamp(releaseLength + releaseBuffer[0] * length, 0, length)); // left only
        }
        else
        {
            releaseLengthFinal = releaseLength;
        }


        for (int n = 0; n < buffer.Length; n += 2)
        {

            // NATIVE
            if(incoming != null){
                // this trigger implementation is good practice, carry over to other modules
                if (pulseBuffer[n] > 0f && lastPulseFloat <= 0f){ // left only
                    counter = 0;
                    stage = 0;
                    isRunning = true;
                }
                lastPulseFloat = pulseBuffer[n];
            } 

            // NATIVE
            if (isRunning)
            {

                if (stage == 0)
                {
                    // need speed up with pre-calculated lookups at some point?

                    buffer[n] = buffer[n + 1] = Mathf.Pow((float)counter / (float)attackLengthFinal, linearity);
                    
                    counter++;
                    if (counter > attackLengthFinal)
                    {
                        stage = 1;
                        counter = 0;
                    }
                }
                else if (stage == 1)
                {
                    buffer[n] = buffer[n + 1] = Mathf.Pow(1f - (float)counter / (float) releaseLengthFinal, linearity);
                    counter++;
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
                SetArrayToSingleValue(buffer, buffer.Length, 0f);
            }

            // hotfix for occasional NaNs that crash other devices. occurs when linearity != 1
            if (System.Single.IsNaN(buffer[n]))
            {
                buffer[n] = buffer[n + 1] = 0f;
            }

            // hotfix for when attack is modulated and glidedVal is stalled as NaN
            if (System.Single.IsNaN(glidedVal))
            {
              glidedVal = 0f;
            }

            glidedVal += (buffer[n] - glidedVal) * 1f; // seems to smooth via float rounding error
            buffer[n] = buffer[n + 1] = glidedVal;

        }
        recursionCheckPost();
    }

}

