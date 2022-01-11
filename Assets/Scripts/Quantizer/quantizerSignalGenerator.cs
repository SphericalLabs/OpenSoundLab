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
using System.Linq;


public class quantizerSignalGenerator : signalGenerator {

  public signalGenerator incoming;
  public bool isOctave = false;
  public float transpose = 0f;

  List<float[]> scales = new List<float[]>();
  public int selectedScale = 2;

  float integerPart = 0f;
  float decimalPart = 0f;

  float semiMult = 1f / 12f;
    
  int i;

  public float output = 0f;

  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);

  public override void Awake()
  {
    base.Awake();

    // Noted in 1V/Oct, 0 is C
    scales.Add(new float[] {0f}); // Octaves
    scales.Add(new float[] {0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f, 11f} ); // Semitones
    scales.Add(new float[] {0f,     2f,     4f, 5f,     7f,     9f,      11f }); // Major
    scales.Add(new float[] {0f,     2f, 3f,     5f,     7f,     9f,      11f }); // Minor
    //Chrom  1 b2 2 b3 3 4 b5 5 b6 6 b7 7
    //Major  1    2    3 4    5    6    7
    //Minor  1    2 b3   4    5    6    7;
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels) {

    if (incoming != null)
    {
      incoming.processBuffer(buffer, dspTime, channels);
    } else {
      SetArrayToSingleValue(buffer, buffer.Length, 0f);
    }

    integerPart = Mathf.Floor((buffer[0] + transpose) * 10); // only first sample, 48000 / 512 => 93.75Hz 
    decimalPart = (buffer[0] + transpose) * 10 - integerPart; // upscale 

    // incoming signals and transpose dial need to be upscaled 0.1/Oct to 1/Oct
    // scales need to be adjusted for semi steps in 1/Oct by multiplying semiMult


    i = 0;
    while (i < scales[selectedScale].Length)
    {
      if (decimalPart < scales[selectedScale][i] * semiMult)
      {        
        break;
      }
      i++;
    }

    i--; // undo last increment, otherwise to high

    //if (i == 0)
    //{ // 
    //  output = integerPart;
    //}
    //else 
    if (i >= scales[selectedScale].Length - 1)
    { // higher than last value
      if(Mathf.Abs(decimalPart - scales[selectedScale][i] * semiMult) <= Mathf.Abs(decimalPart - scales[selectedScale][0] * semiMult)){  // optimise: premultilpy semiMult in List
        output = integerPart + scales[selectedScale][i] * semiMult;
      } else {
        output = integerPart + scales[selectedScale][0] * semiMult;
      }
    } else {
      if (Mathf.Abs(decimalPart - scales[selectedScale][i] * semiMult) <= Mathf.Abs(decimalPart - scales[selectedScale][i+1] * semiMult))
      {
        output = integerPart + scales[selectedScale][i] * semiMult;
      }
      else
      {
        output = integerPart + scales[selectedScale][i+1] * semiMult;
      }
    }

    SetArrayToSingleValue(buffer, buffer.Length, output * 0.1f); // downscale to 0.1/Oct

  }
}
