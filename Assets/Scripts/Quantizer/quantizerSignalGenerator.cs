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
using System.Linq;


public class quantizerSignalGenerator : signalGenerator {

  public signalGenerator incoming;
  public bool isOctave = false;
  public float transpose = 0f;

  List<float[]> scales = new List<float[]>();
  public int selectedScale = 0;

  float integerPart = 0f;
  float decimalPart = 0f;
  readonly float semiMult = 1f / 12f;
    
  int i;

  public float output = 0f;

  int counter = 0;

  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);

  public override void Awake()
  {
    base.Awake();

    // Noted in semitone steps, 0 is C
    scales.Add(new float[] {0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f, 11f}); // Semitones
    scales.Add(new float[] {0f,     2f,     4f, 5f,     7f,     9f,      11f}); // Major
    scales.Add(new float[] {0f,     2f, 3f,     5f,     7f,     9f,      11f}); // Minor
    scales.Add(new float[] {0f}); // Octaves -> please note, this is hardcoded to index 3 below!
    //Chrom  1 b2 2 b3 3 4 b5 5 b6 6 b7 7
    //Major  1    2    3 4    5    6    7
    //Minor  1    2 b3   4    5    6    7;

    //pre-multiply semitone factor in order to comply 1/Oct
    for(int j = 0; j < scales.Count; j++){
      for(int k = 0; k < scales[j].Length; k++){
        scales[j][k] *= semiMult;
      }
    }
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    if (!recursionCheckPre()) return; // checks and avoids fatal recursions
    if (incoming != null)
    {
      incoming.processBuffer(buffer, dspTime, channels);
    } else {
      SetArrayToSingleValue(buffer, buffer.Length, 0f);
    }

    // hard coded OCTAVE!
    if (selectedScale == 3)
    {
      SetArrayToSingleValue(buffer, buffer.Length, Mathf.Round((buffer[0] + transpose) * 10f) * 0.1f); // transpose -5,5 octaves

    }
    else // other quantisation modes
    {

      integerPart = Mathf.Floor(buffer[0] * 10 + transpose * 2); // transpose -1,1 octaves
      decimalPart = buffer[0] * 10 + transpose * 2 - integerPart;


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

      SetArrayToSingleValue(buffer, buffer.Length, output * 0.1f); // downscale to 0.1/Oct

    }
    recursionCheckPost();
  }
}
