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
using System.Runtime.InteropServices;
using System;

public class filterSignalGenerator : signalGenerator
{

  public signalGenerator incoming, freqIncoming;

  //MonoFilter[] filters;

  public float cutoffFrequency = 0f;
  float lastCutoffFrequency = 0f;
  public float bandWidthHalfed = 0.05f;
  public float resonance = .5f;

  float[] bufferCopy;
  float[] frequencyBuffer;

  // Changing this number requires changing native code.
  //const int NUM_FILTERS = 4;

  // Changing this enum requires changing the mirrored native enum.
  public enum filterType
  {
    none,
    LP, // x
    HP, // x
    LP_long,
    HP_long,
    BP, // x
    Notch, // x
    pass
  };

  public filterType curType = filterType.LP;

  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);
  [DllImport("SoundStageNative")]
  public static extern void CopyArray(float[] a, float[] b, int length);
  [DllImport("SoundStageNative")]
  public static extern void AddArrays(float[] a, float[] b, int length);

  [DllImport("SoundStageNative")]
  public static extern void processStereoFilter(float[] buffer, int length, ref mfValues mfL, ref mfValues mfR, float cutoffFrequency, float lastCutoffFrequency, bool freqGen, float[] frequencyBuffer, float resonance/*, IntPtr logger*/);
   
    // create structs for passing to native code
  mfValues mf1L = new mfValues();
  mfValues mf1R = new mfValues();

  mfValues mf2L = new mfValues();
  mfValues mf2R = new mfValues();

  //IntPtr delegatePtr;

  public override void Awake()
  {
    base.Awake();
    bufferCopy = new float[MAX_BUFFER_LENGTH];
    frequencyBuffer = new float[MAX_BUFFER_LENGTH];

    //LogDelegate callback_delegate = new LogDelegate(LogCallback);
    //delegatePtr = Marshal.GetFunctionPointerForDelegate(callback_delegate);
  }

  //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  //public delegate void LogDelegate(int level, string str);

  static void LogCallback(int level, string msg)
  {
    if (level == 0)
      Debug.Log(msg);
    else if (level == 1)
      Debug.LogWarning(msg);
    else if (level == 2)
      Debug.LogError(msg);
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels)
  {
    if (!recursionCheckPre()) return; // checks and avoids fatal recursions
    if (bufferCopy.Length != buffer.Length)
      System.Array.Resize(ref bufferCopy, buffer.Length);

    if (frequencyBuffer.Length != buffer.Length)
      System.Array.Resize(ref frequencyBuffer, buffer.Length);

    SetArrayToSingleValue(frequencyBuffer, frequencyBuffer.Length, 0f);
    if (freqIncoming != null) 
      freqIncoming.processBuffer(frequencyBuffer, dspTime, channels);

    // if silent, 0 out and return
    //if (!incoming)
    //{
    //  SetArrayToSingleValue(buffer, buffer.Length, 0.0f);
    //  SetArrayToSingleValue(bufferCopy, bufferCopy.Length, 0.0f);
    //  return;
    //}
    if(incoming != null) incoming.processBuffer(buffer, dspTime, channels);


    if (curType != filterType.Notch && curType != filterType.BP) // not a double filter setup, either LP or HP
    {
      mf1R.LP = mf1L.LP = curType == filterType.LP;
      processStereoFilter(buffer, buffer.Length, ref mf1L, ref mf1R, cutoffFrequency, lastCutoffFrequency, freqIncoming != null,  frequencyBuffer, resonance/*, delegatePtr*/);
    }
    else if (curType == filterType.Notch) // duplicate buffer in order to process two filters in parallel
    {
      CopyArray(buffer, bufferCopy, buffer.Length);

      mf1R.LP = mf1L.LP = true;
      processStereoFilter(buffer, buffer.Length, ref mf1L, ref mf1R, cutoffFrequency - bandWidthHalfed, lastCutoffFrequency - bandWidthHalfed, freqIncoming != null, frequencyBuffer, resonance * 0.7f/*, delegatePtr*/); // less resonance for double filter mode

      mf2R.LP = mf2L.LP = false;
      processStereoFilter(bufferCopy, bufferCopy.Length, ref mf2L, ref mf2R, cutoffFrequency + bandWidthHalfed, lastCutoffFrequency + bandWidthHalfed, freqIncoming != null, frequencyBuffer, resonance * 0.7f/*, delegatePtr*/);

      AddArrays(buffer, bufferCopy, buffer.Length);
    }

    else if (curType == filterType.BP) // process two filter in series
    {

      mf1R.LP = mf1L.LP = false;
      processStereoFilter(buffer, buffer.Length, ref mf1L, ref mf1R, cutoffFrequency - bandWidthHalfed, lastCutoffFrequency - bandWidthHalfed, freqIncoming != null, frequencyBuffer, resonance * 0.7f/*, delegatePtr*/);

      mf2R.LP = mf2L.LP = true;
      processStereoFilter(buffer, buffer.Length, ref mf2L, ref mf2R, cutoffFrequency + bandWidthHalfed, lastCutoffFrequency + bandWidthHalfed, freqIncoming != null, frequencyBuffer, resonance * 0.7f/*, delegatePtr*/);
    }

    CopyArray(buffer, bufferCopy, buffer.Length);

    lastCutoffFrequency = cutoffFrequency; // for slope limiting in native code
    recursionCheckPost();
  }
}

public struct mfValues
{
  public float f, p, q; // filter coefficients
  public float b0, b1, b2, b3, b4; // filter buffers (beware denormals!)
  public bool LP; 
};


