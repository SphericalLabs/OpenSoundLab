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
using System.Runtime.InteropServices;

public class polarizerSignalGenerator : signalGenerator
{

  public signalGenerator incoming;
  public bool uniToBi = true;

  public override void processBuffer(float[] buffer, double dspTime, int channels)
  {
    if (!recursionCheckPre()) return; // checks and avoids fatal recursions 
    if (incoming == null){
      return;
    } else {
      incoming.processBuffer(buffer, dspTime, channels);
    }

    if (uniToBi) // 0,1 -> -1,1
    {
      for (int n = 0; n < buffer.Length; n++)
      {
        buffer[n] = buffer[n] * 2f - 1f;
      }
    }
    else // -1,1 -> 0,1
    {
      for (int n = 0; n < buffer.Length; n++)
      {
        buffer[n] = buffer[n] * 0.5f + 0.5f;
      }
    }
    recursionCheckPost();
  }
}
