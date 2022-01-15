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

public class vcaSignalGenerator : signalGenerator {

  public signalGenerator incoming, controlSig;
  public bool active = true;
  public float amp = 1f;
  float sign;

  public override void processBuffer(float[] buffer, double dspTime, int channels) {


    float[] controlBuffer = new float[buffer.Length];
    if (controlSig != null) controlSig.processBuffer(controlBuffer, dspTime, channels);
    if (incoming != null) incoming.processBuffer(buffer, dspTime, channels);


    if (incoming != null) 
    { 
      if(controlSig != null) 
      {
        for (int i = 0; i < buffer.Length; i++)
        {
          buffer[i] = buffer[i] * controlBuffer[i] * Mathf.Pow(amp, 2); // RingMod, 0-1 fades normal, -1 inverts phase
        }
      } else {
        for (int i = 0; i < buffer.Length; i++)
        {
          buffer[i] = buffer[i] * Mathf.Pow(amp, 2); // dial acts as attenuator
        }
      }
    } else { // nothing plugged, no output
      //if(controlSig != null) {
      //  for (int i = 0; i < buffer.Length; i++)
      //  {
      //    buffer[i] = controlBuffer[i] * Mathf.Pow(amp, 2);
      //  }
      //} else {
        for (int i = 0; i < buffer.Length; i++)
        {
          buffer[i] = 0;
        }
      //}
    }

  }
}
