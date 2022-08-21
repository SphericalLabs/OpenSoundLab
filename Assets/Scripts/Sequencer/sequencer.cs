// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of OpenSoundLab.
//
// OpenSoundLab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenSoundLab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenSoundLab.  If not, see <http://www.gnu.org/licenses/>.

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

        SetArrayToSingleValue(buffer, buffer.Length, 0f);

        if (newSignal)
        {
            if ((sigTime == -1 || sigTime == dspTime) && signalOn)
            {
                sigTime = dspTime;
                buffer[0] = buffer[1] = 1f;
            }
            else
            {
                newSignal = false;
                sigTime = -1;
            }
        }
    }
}


