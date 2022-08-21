// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of OpenSoundLab.
//
// OpenSoundLab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenSoundLab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenSoundLab.  If not, see <http://www.gnu.org/licenses/>.

using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class vcaSignalGenerator : signalGenerator {

  public signalGenerator incoming, controlSig;
  public bool active = true;
  public float amp = 1f;
  float lastAmp = 0f;
  float sign;
  float[] controlBuffer = new float[1];


  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    if (!recursionCheckPre()) return; // checks and avoids fatal recursions
    if (controlBuffer.Length != buffer.Length)
      System.Array.Resize(ref controlBuffer, buffer.Length);

    SetArrayToSingleValue(controlBuffer, controlBuffer.Length, 0f);
    if (controlSig != null) controlSig.processBuffer(controlBuffer, dspTime, channels);
    if (incoming != null) incoming.processBuffer(buffer, dspTime, channels);

    if (incoming != null) 
    { 
      if(controlSig != null) 
      {
        for (int i = 0; i < buffer.Length; i++)
        {
          buffer[i] = buffer[i] * controlBuffer[i] * Mathf.Pow(Utils.lerp(lastAmp, amp, (float) i / (buffer.Length - 1)), 2); // RingMod, 0-1 fades normal, -1 inverts phase
        }
      } else {
        for (int i = 0; i < buffer.Length; i++)
        {
          buffer[i] = buffer[i] * Mathf.Pow(Utils.lerp(lastAmp, amp, (float) i / (buffer.Length - 1)), 2); // dial acts as attenuator
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

    lastAmp = amp;
    recursionCheckPost();
  }
}
