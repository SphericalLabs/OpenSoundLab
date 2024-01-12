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
    float linearityA = 1f;
    float linearityD = 1f;

    int minAttack; // in samples
    int maxAttack;
    int minRelease;
    int maxRelease;

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
        minAttack = 1; // in samples
        maxAttack = Mathf.RoundToInt(5.000f * AudioSettings.outputSampleRate);
        minRelease = Mathf.RoundToInt(0.020f * AudioSettings.outputSampleRate);
        maxRelease = Mathf.RoundToInt(5.000f * AudioSettings.outputSampleRate);
  }

    public void setAttack(float val)
    {
        attackLength = Mathf.RoundToInt(Utils.map(val, 0f, 1f, minAttack, maxAttack));
    }

    public void setRelease(float val)
    {
        releaseLength = Mathf.RoundToInt(Utils.map(val, 0f, 1f, minRelease, maxRelease));
  }

    // c.f. https://www.reddit.com/r/modular/comments/ovh1b2/log_lin_exp_envelopes_and_which_is_correct/
    public void setLinearity(float val)
      {
          if (val >= 0.5)
          {
              linearityA = Utils.map(val, 0.5f, 1f, 1f, 20f);
              linearityD = Utils.map(val, 0.5f, 1f, 1f, 0.05f);
      }
          else
          {
              linearityD = Utils.map(val, 0f, 0.5f, 20f, 1f); 
              linearityA = Utils.map(val, 0f, 0.5f, 0.05f, 1f); 
          }
      }

    [DllImport("SoundStageNative")]
    public static extern bool GetBinaryState(float[] buffer, int length, int channels, ref float lastBuf);

    [DllImport("SoundStageNative")]
    public static extern bool IsPulse(float[] buffer, int length);

    [DllImport("SoundStageNative")]
    public static extern void SetArrayToSingleValue(float[] a, int length, float val);

    float prevLinearityA = 1f;
    float prevLinearityD = 1f;
    int prevAttackLengthFinal = 0; 
    int prevReleaseLengthFinal = 0; 

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
            attackLengthFinal = Mathf.RoundToInt(Mathf.Clamp(attackLength + attackBuffer[0] * maxAttack, minAttack, maxAttack)); // left only
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
            releaseLengthFinal = Mathf.RoundToInt(Mathf.Clamp(releaseLength + releaseBuffer[0] * maxRelease, minRelease, maxRelease)); // left only
        }
        else
        {
            releaseLengthFinal = releaseLength;
        }


        for (int n = 0; n < buffer.Length; n += 2)
        {

            // SHOULD BE NATIVE
            if (incoming != null){
                // this trigger implementation is good practice, carry over to other modules
                if (pulseBuffer[n] > 0f && lastPulseFloat <= 0f){ // left only
                    counter = 0;
                    stage = 0;
                    isRunning = true;
                }
                lastPulseFloat = pulseBuffer[n];
            } 

            // SHOULD BE NATIVE
            if (isRunning)
            {

                if (stage == 0)
                {
                    // need speed up with pre-calculated lookups at some point?
                    // PLEASE NOTE: this lerp interpolation here is also smoothing A and D changes that come via the CV inputs. Additionally, these inputs are only sampled on the first sample of the buffer, see above. But then again, audio rate modulation probably doesn't make much sense here anyways.
                    buffer[n] = buffer[n + 1] = Mathf.Pow((float)counter / ( prevAttackLengthFinal == attackLengthFinal ? (float)attackLengthFinal : Mathf.Lerp(prevAttackLengthFinal, attackLengthFinal, (float)n / buffer.Length) ), 
                    prevLinearityA == linearityA ? linearityA : Mathf.Lerp(prevLinearityA, linearityA, (float) n / buffer.Length));
                    
                    counter++;
                    if (counter > attackLengthFinal)
                    {
                        stage = 1;
                        counter = 0;
                    }
                }
                else if (stage == 1)
                {
                    buffer[n] = buffer[n + 1] = Mathf.Pow(1f - (float)counter / ( prevReleaseLengthFinal == releaseLengthFinal ? (float)releaseLengthFinal : Mathf.Lerp(prevReleaseLengthFinal, releaseLengthFinal, (float)n / buffer.Length) ), 
                    prevLinearityD == linearityD ? linearityD : Mathf.Lerp(prevLinearityD, linearityD, (float) n / buffer.Length));
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

            prevLinearityA = linearityA;
            prevLinearityD = linearityD;
            prevAttackLengthFinal = attackLengthFinal;
            prevReleaseLengthFinal = releaseLengthFinal;

        }
        recursionCheckPost();
    }

}

