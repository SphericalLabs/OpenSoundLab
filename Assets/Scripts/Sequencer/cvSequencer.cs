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
using System;

public class cvSequencer : signalGenerator {

  //bool newSignal = false;
  //bool signalOn = false;

  //[DllImport("SoundStageNative")]
  //public static extern void SetArrayToSingleValue(float[] a, int length, float val);

  public void setSignal(float f)
  {
    //newSignal = true;
    //signalOn = on;
    outVal = f * 3.5f - 0.5f; // same range as keyboard?
  }

  float outVal = 0f;

  double sigTime = -1;  // note: sigTime stuff will probably be necessary for tightness, also for cvSeq! Or not???

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    //float val = signalOn ? 1.0f : -1.0f;
    //SetArrayToSingleValue(buffer, buffer.Length, val);

    //if (newSignal) {
    //  if (sigTime == -1 || sigTime == dspTime) {
    //    sigTime = dspTime;
    //    buffer[0] = buffer[1] = -1;
    //  } else {
    //    newSignal = false;
    //    sigTime = -1;
    //  }
    //}
    for(int i = 0; i < buffer.Length; i++) 
    {
      buffer[i] = outVal;
    }
    
    

  }

}

