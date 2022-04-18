// Copyright 2017-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
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
