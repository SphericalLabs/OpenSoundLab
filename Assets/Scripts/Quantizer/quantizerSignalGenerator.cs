// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright � 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
// 
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at 
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
// 
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
// 
//
// Copyright � 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright � 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright � 2017 Apache 2.0 Google LLC SoundStage VR
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
  public float octave = 0f;

  public List<float[]> scales = new List<float[]>();
  public List<string> scalesStrings = new List<string>();
  public int selectedScale = 0;

  float integerPart = 0f;
  float decimalPart = 0f;
  readonly float semiMult = 1f / 12f;
    
  int i;

  public float output = 0f;

  //int counter = 0;

  [DllImport("OSLNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);

  public override void Awake()
  {
    base.Awake();

      // Keys          C  C# D  D# E  F  F# G  G# A  A# B
      // Index         0  1  2  3  4  5  6  7  8  9  10 11 
      // Semitone      x  x  x  x  x  x  x  x  x  x  x  x
    scalesStrings.Add("x  x  x  x  x  x  x  x  x  x  x  x");
      // Maj           x     x     x  x     x     x     x
    scalesStrings.Add("x     x     x  x     x     x     x");
      // Min           x     x  x     x     x  x     x
    scalesStrings.Add("x     x  x     x     x  x     x   ");
      // HarmonicMaj   x     x     x  x     x  x        x 
    scalesStrings.Add("x     x     x  x     x  x        x");
      // HarmonicMin   x     x  x     x     x  x        x
    scalesStrings.Add("x     x  x     x     x  x        x");
      // PentaMaj      x     x     x        x     x         
    scalesStrings.Add("x     x     x        x     x      ");
      // PentaMin      x        x     x     x        x      
    scalesStrings.Add("x        x     x     x        x   ");
      // Octave        x
    scalesStrings.Add("x");

    for (int i = 0; i < scalesStrings.Count; i++)
    {
      scales.Add(ParseStructuredString(scalesStrings[i]));
    }

    //pre-multiply semitone factor in order to comply 1/Oct
    for (int j = 0; j < scales.Count; j++){
      for(int k = 0; k < scales[j].Length; k++){
        scales[j][k] *= semiMult;
      }
    }
  }

  // Generated by ChatGPT:
  float[] ParseStructuredString(string input)
  {
    // Split the input string into an array of characters
    char[] chars = input.ToCharArray();

    // Use LINQ to select the indices of the 'x' characters
    int[] xIndices = chars.Select((c, i) => c == 'x' ? i : -1).Where(i => i != -1).ToArray();

    // Create a new float array with the same length as the xIndices array
    float[] result = new float[xIndices.Length];

    // Iterate over the xIndices array and set the corresponding element in the result array to the index
    for (int i = 0; i < xIndices.Length; i++)
    {
      result[i] = xIndices[i] / 3; // grid spacing is 3
    }

    return result;
  }

  public override void processBufferImpl(float[] buffer, double dspTime, int channels) {
    if (!recursionCheckPre()) return; // checks and avoids fatal recursions
    if (incoming != null)
    {
      incoming.processBuffer(buffer, dspTime, channels);
    } else {
      SetArrayToSingleValue(buffer, buffer.Length, 0f);
    }

    // hard coded octave, last element of the scale dial
    if (selectedScale == scales.Count)
    {
      SetArrayToSingleValue(buffer, buffer.Length, Mathf.Round((buffer[0]) * 10f) * 0.1f + transpose + octave); 
    }
    else // other quantisation modes
    {

      integerPart = Mathf.Floor(buffer[0] * 10); // transpose -1,1 octaves
      decimalPart = buffer[0] * 10 - integerPart;


      // incoming signals and transpose dial need to be upscaled 0.1/Oct to 1/Oct
      // scales need to be adjusted for semi steps in 1/Oct by multiplying semiMult


      i = 0;
      while (i < scales[selectedScale].Length)
      {
        if (decimalPart < scales[selectedScale][i])
        {
          break;
        }
        i++;
      }

      i--; // undo last increment, otherwise to high


      if (i == scales[selectedScale].Length - 1) // edge case: last value, need wrap around
      {

        if (Mathf.Abs(decimalPart - scales[selectedScale][i]) <= Mathf.Abs(1 - decimalPart)) // higher than last value
        {
          output = integerPart + scales[selectedScale][i];
        }
        else
        {
          output = integerPart + 1 + scales[selectedScale][0]; // last part is actually zero, no need to compute
        }
      }
      else // normal case
      {
        if (Mathf.Abs(decimalPart - scales[selectedScale][i]) <= Mathf.Abs(decimalPart - scales[selectedScale][i + 1]))
        {
          output = integerPart + scales[selectedScale][i];
        }
        else
        {
          output = integerPart + scales[selectedScale][i + 1];
        }
      }

      SetArrayToSingleValue(buffer, buffer.Length, output * 0.1f + transpose + octave); // downscale to 0.1/Oct

    }
    recursionCheckPost();
  }
}
