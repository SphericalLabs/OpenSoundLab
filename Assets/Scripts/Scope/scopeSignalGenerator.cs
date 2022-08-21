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

public class scopeSignalGenerator : signalGenerator
{

  public signalGenerator incoming;
  float[] bufferCopy; // for FFT
  waveViz displayOsc; // for OSC
  spectrumDisplay displayFft;
  public bool isMuted = false;

  [DllImport("SoundStageNative")]
  public static extern void CopyArray(float[] a, float[] b, int length);
  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);


  public override void Awake()
  {
    base.Awake();
    displayOsc = GetComponentInChildren<waveViz>();
    displayFft = GetComponentInChildren<spectrumDisplay>();

    bufferCopy = new float[MAX_BUFFER_LENGTH];
  }

  // needed for scope, feeds it in AudioSource on Scope for FFT
  private void OnAudioFilterRead(float[] buffer, int channels)
  {
    if (incoming == null || bufferCopy == null) return;
    CopyArray(bufferCopy, buffer, buffer.Length);
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels)
  {
    if (!recursionCheckPre()) return; // checks and avoids fatal recursions
    if (bufferCopy.Length != buffer.Length)
      System.Array.Resize(ref bufferCopy, buffer.Length);

    // if silent, 0 out and return
    if (!incoming)
    {
      SetArrayToSingleValue(buffer, buffer.Length, 0.0f);
      SetArrayToSingleValue(bufferCopy, bufferCopy.Length, 0.0f);
      return;
    }

    if (incoming != null){
      incoming.processBuffer(buffer, dspTime, channels);
    }

    /*if(displayFft.gameObject.activeSelf) */CopyArray(buffer, bufferCopy, buffer.Length); // copy over for FFT
    /*if(displayOsc.gameObject.activeSelf) */displayOsc.storeBuffer(buffer, channels); // copy over for OSC
    
    if(isMuted){
      SetArrayToSingleValue(buffer, buffer.Length, 0.0f); // clear after usage before passing on. todo: this could be made more efficient and click-free
    }
    recursionCheckPost();
  }
}
