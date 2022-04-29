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

public class DCSignalGenerator : signalGenerator {

  //public signalGenerator incoming;
  public bool active = true;
  bool isBipolar = true;
  public float attenDialValue = 0f;
  float lastAttenDialValue = 0.5f;
  
  float[] controlBuffer = new float[1];

  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);

  public override void processBuffer(float[] buffer, double dspTime, int channels) {

    //if(!recursionCheckPre()) return; // checks and avoids fatal recursions

    //if (incoming != null) // attennuverter mode
    //{
    //  incoming.processBuffer(buffer, dspTime, channels);
    //  if (isBipolar)
    //  {
    //    for (int i = 0; i < buffer.Length; i++)
    //      buffer[i] = buffer[i] * (Utils.lerp(lastAttenDialValue, attenDialValue, (float)i / (buffer.Length - 1)) * 2f - 1f); // linear attenuverter
    //  }
    //  else
    //  {
    //    for (int i = 0; i < buffer.Length; i++)
    //      buffer[i] = buffer[i] * Mathf.Pow(Utils.lerp(lastAttenDialValue, attenDialValue, (float)i / (buffer.Length - 1)), 2); // exponential attenuator
    //  }

    //} else { // nothing plugged, DC gen mode
      if(isBipolar){
        for(int i = 0; i < buffer.Length; i += channels)
          buffer[i] = buffer[i + 1] = Utils.lerp(lastAttenDialValue, attenDialValue, (float)i / (buffer.Length - 1)) * 2f - 1f;
      } else {
        for (int i = 0; i < buffer.Length; i += channels)
          buffer[i] = buffer[i + 1] = Utils.lerp(lastAttenDialValue, attenDialValue, (float)i / (buffer.Length - 1));
      }
    //}

    lastAttenDialValue = attenDialValue;
    //recursionCheckPost();
  }
}
