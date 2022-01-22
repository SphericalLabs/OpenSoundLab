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

public class sequencer : signalGenerator
{

    bool newSignal = false;
    bool signalOn = false;

    [DllImport("SoundStageNative")]
    public static extern void SetArrayToSingleValue(float[] a, int length, float val);

    public void setSignal(bool on)
    {
        newSignal = true;
        signalOn = on;
    }

    double sigTime = -1;
    public override void processBuffer(float[] buffer, double dspTime, int channels)
    {

        SetArrayToSingleValue(buffer, buffer.Length, -1);

        if (newSignal)
        {
            if ((sigTime == -1 || sigTime == dspTime) && signalOn)
            {
                sigTime = dspTime;
                buffer[0] = buffer[1] = 1;
            }
            else
            {
                newSignal = false;
                sigTime = -1;
            }
        }
    }
}


