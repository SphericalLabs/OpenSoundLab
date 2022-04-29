// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of SoundStage Lab.
//
// SoundStage Lab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// SoundStage Lab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SoundStage Lab.  If not, see <http://www.gnu.org/licenses/>.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

public class sequencerCV : signalGenerator
{
    public static float lowRange = 0.2f; // central place to set these ranges!
    public static float highRange = 1f; // 


    float outVal = 0f;
    float rangeMultiplier = lowRange;

    [DllImport("SoundStageNative")]
    public static extern void SetArrayToSingleValue(float[] a, int length, float val);

    public void setRange(float f)
    {
        rangeMultiplier = f;
    }
    public void setSignal(float f)
    {
        outVal = f;
    }

    public override void processBuffer(float[] buffer, double dspTime, int channels)
    {
        SetArrayToSingleValue(buffer, buffer.Length, outVal * rangeMultiplier);
    }

}

