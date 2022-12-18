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
        if (!recursionCheckPre()) return; // checks and avoids fatal recursions
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
        recursionCheckPost();
    }
}
