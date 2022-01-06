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

public class valveSignalGenerator : signalGenerator {

  public signalGenerator incoming, controlSig;
  public bool active = true;
  public float amp = 1f;


  public override void processBuffer(float[] buffer, double dspTime, int channels) {


    float[] controlBuffer = new float[buffer.Length];
    if (controlSig != null) controlSig.processBuffer(controlBuffer, dspTime, channels);
    if (incoming != null) incoming.processBuffer(buffer, dspTime, channels);


    if(incoming != null) 
    { 
      if(controlSig != null) 
      {
        for (int i = 0; i < buffer.Length; i++)
        {
          buffer[i] = buffer[i] * (controlBuffer[i] + 1) * 0.5f * Mathf.Pow(amp, 2); // map control buffer -1,1 -> 0,1
        }
      } else {
        for (int i = 0; i < buffer.Length; i++)
        {
          buffer[i] = buffer[i] * Mathf.Pow(amp, 2);
        }
      }
    } else {
      if(controlSig != null) {
        for (int i = 0; i < buffer.Length; i++)
        {
          buffer[i] = (controlBuffer[i] + 1) * 0.5f * Mathf.Pow(amp, 2);
        }
      } else {
        for (int i = 0; i < buffer.Length; i++)
        {
          buffer[i] = 0;
        }
      }
    }

  }
}
