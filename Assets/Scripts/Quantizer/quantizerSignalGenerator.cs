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

public class quantizerSignalGenerator : signalGenerator {

  public signalGenerator incoming;
  public bool isOctave = false;
  public float transpose = 0f;

  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);


  public override void processBuffer(float[] buffer, double dspTime, int channels) {

    if (incoming != null)
    {
      incoming.processBuffer(buffer, dspTime, channels);
    } else {
      SetArrayToSingleValue(buffer, buffer.Length, 0f);
    }
    
    for (int n = 0; n < buffer.Length; n += 2) 
    {
      // Oct => round to 1 / 10 steps; Semi => round to 1 / 10 / 12
      if(isOctave){
        buffer[n] = buffer[n + 1] = Mathf.Round((buffer[n] + transpose) * 10f) / 10f; 
      } else {
        buffer[n] = buffer[n + 1] = Mathf.Round((buffer[n] + transpose) * 120f) / 120f; 
      }

      //if (n == 0) Debug.Log(buffer[n]);
      
    }

  }
}
