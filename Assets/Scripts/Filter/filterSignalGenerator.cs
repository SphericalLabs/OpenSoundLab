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

public class filterSignalGenerator : signalGenerator
{

  public signalGenerator incoming, controlIncoming;

  //MonoFilter[] filters;

  public float cutoffFrequency = 0f;
  public float resonance = .5f;
  //public float[] frequency = new float[] { .3f,.6f};// cutoff frequency for LP and BP

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
  public static extern void processStereoFilter(float[] buffer, int length, ref mfValues mfL, ref mfValues mfR, float cutoffFrequencym, float[] frequencyBuffer, float resonance);
  // do these structs have to be created in native code?
   

  // create empty structs for passing to native code
  mfValues mfL = new mfValues();
  mfValues mfR = new mfValues();

  public override void Awake()
  {
    base.Awake();
    //filters = new MonoFilter[NUM_FILTERS];
    bufferCopy = new float[MAX_BUFFER_LENGTH];
    frequencyBuffer = new float[MAX_BUFFER_LENGTH];

    ////primary stereo filter
    //filters[0] = new MonoFilter(frequency[0], resonance);
    //filters[1] = new MonoFilter(frequency[0], resonance);

    ////secondary stereo filter (for BP/notch)
    //filters[2] = new MonoFilter(frequency[1], resonance);
    //filters[3] = new MonoFilter(frequency[1], resonance);
  }


  //  public void updateFilterType(filterType f)
  //  {
  //      curType = f;

  //      if(f == filterType.LP)
  //      {
  //          filters[0].mf.LP = true;
  //          filters[1].mf.LP = true;

  //          filters[0].SetFrequency(frequency[0]);
  //          filters[1].SetFrequency(frequency[0]);
  //      }
  //      else if(f == filterType.LP_long)
  //      {
  //          filters[0].mf.LP = true;
  //          filters[1].mf.LP = true;

  //          filters[0].SetFrequency(frequency[1]);
  //          filters[1].SetFrequency(frequency[1]);
  //      }
  //      else if (f == filterType.HP)
  //      {
  //          filters[0].mf.LP = false;
  //          filters[1].mf.LP = false;

  //          filters[0].SetFrequency(frequency[1]);
  //          filters[1].SetFrequency(frequency[1]);
  //      }
  //      else if (f == filterType.HP_long)
  //      {
  //          filters[0].mf.LP = false;
  //          filters[1].mf.LP = false;

  //          filters[0].SetFrequency(frequency[0]);
  //          filters[1].SetFrequency(frequency[0]);
  //      }
  //      else if (f == filterType.Notch)
  //      {
  //          filters[0].mf.LP = true;
  //          filters[1].mf.LP = true;

  //          filters[2].mf.LP = false;
  //          filters[3].mf.LP = false;

  //          filters[0].SetFrequency(frequency[0]);
  //          filters[1].SetFrequency(frequency[0]);

  //          filters[2].SetFrequency(frequency[1]);
  //          filters[3].SetFrequency(frequency[1]);
  //      }
  //      else if (f == filterType.BP)
  //      {
  //          filters[0].mf.LP = true;
  //          filters[1].mf.LP = true;

  //          filters[2].mf.LP = false;
  //          filters[3].mf.LP = false;

  //          filters[0].SetFrequency(frequency[1]);
  //          filters[1].SetFrequency(frequency[1]);

  //          filters[2].SetFrequency(frequency[0]);
  //          filters[3].SetFrequency(frequency[0]);
  //      }

  //      filters[0].SetResonance(resonance);
  //      filters[1].SetResonance(resonance);
  //      filters[2].SetResonance(resonance);
  //      filters[3].SetResonance(resonance);
  //}

  //  void Update()
  //  {
  //      if (curType == filterType.LP || curType == filterType.HP_long)
  //      {
  //          filters[0].SetFrequency(frequency[0]);
  //          filters[1].SetFrequency(frequency[0]);
  //      }
  //      else if (curType == filterType.LP_long || curType == filterType.HP)
  //      {
  //          filters[0].SetFrequency(frequency[1]);
  //          filters[1].SetFrequency(frequency[1]);
  //      }
  //      else if (curType == filterType.Notch)
  //      {

  //          filters[0].SetFrequency(frequency[0]);
  //          filters[1].SetFrequency(frequency[0]);

  //          filters[2].SetFrequency(frequency[1]);
  //          filters[3].SetFrequency(frequency[1]);
  //      }
  //      else if (curType == filterType.BP)
  //      {

  //          filters[0].SetFrequency(frequency[1]);
  //          filters[1].SetFrequency(frequency[1]);

  //          filters[2].SetFrequency(frequency[0]);
  //          filters[3].SetFrequency(frequency[0]);
  //      }
  //      filters[0].SetResonance(resonance);
  //      filters[1].SetResonance(resonance);
  //      filters[2].SetResonance(resonance);
  //      filters[3].SetResonance(resonance);
  //}

  //private void OnAudioFilterRead(float[] buffer, int channels)
  //{        
  //    if (incoming == null || bufferCopy == null) return;
  //    CopyArray(bufferCopy,buffer, buffer.Length);
  //}

  public override void processBuffer(float[] buffer, double dspTime, int channels)
  {
    if (bufferCopy.Length != buffer.Length)
      System.Array.Resize(ref bufferCopy, buffer.Length);

    if (frequencyBuffer.Length != buffer.Length)
      System.Array.Resize(ref frequencyBuffer, buffer.Length);

    if (controlIncoming != null)
    {
      controlIncoming.processBuffer(frequencyBuffer, dspTime, channels);
    } else {
      SetArrayToSingleValue(frequencyBuffer, buffer.Length, 0f);
    }

    // if silent, 0 out and return
    if (!incoming)
    {
      SetArrayToSingleValue(buffer, buffer.Length, 0.0f);
      SetArrayToSingleValue(bufferCopy, bufferCopy.Length, 0.0f);
      return;
    }
    incoming.processBuffer(buffer, dspTime, channels);


    if (curType != filterType.Notch && curType != filterType.BP) // not a double filter setup, either LP or HP
    {
      mfL = new mfValues();
      mfR = new mfValues();
      mfR.LP = mfL.LP = curType == filterType.LP;
      processStereoFilter(buffer, buffer.Length, ref mfL, ref mfR, cutoffFrequency, frequencyBuffer, resonance);
    }
    else if (curType == filterType.Notch) // duplicate buffer in order to process two filters in parallel
    {
      CopyArray(buffer, bufferCopy, buffer.Length);

      mfL = new mfValues();
      mfR = new mfValues();
      mfR.LP = mfL.LP = true;
      processStereoFilter(buffer, buffer.Length, ref mfL, ref mfR, cutoffFrequency - 0.1f, frequencyBuffer, resonance);

      mfL = new mfValues();
      mfR = new mfValues();
      mfR.LP = mfL.LP = false;
      processStereoFilter(bufferCopy, bufferCopy.Length, ref mfL, ref mfR, cutoffFrequency + 0.1f, frequencyBuffer, resonance);

      AddArrays(buffer, bufferCopy, buffer.Length);
    }

    else if (curType == filterType.BP) // process two filter in series
    {
      mfL = new mfValues();
      mfR = new mfValues();
      mfR.LP = mfL.LP = false;
      processStereoFilter(buffer, buffer.Length, ref mfL, ref mfR, cutoffFrequency - 0.1f, frequencyBuffer, resonance);

      mfL = new mfValues();
      mfR = new mfValues();
      mfR.LP = mfL.LP = true;
      processStereoFilter(buffer, buffer.Length, ref mfL, ref mfR, cutoffFrequency + 0.1f, frequencyBuffer, resonance);
    }

    CopyArray(buffer, bufferCopy, buffer.Length);
  }
}

public struct mfValues
{
  public float f, p, q; //filter coefficients
  public float b0, b1, b2, b3, b4; //filter buffers (beware denormals!)
  public bool LP; // needed?
};

//class MonoFilter
//{
//    public float frequency = .5f;
//    public float resonance = .5f;

//    public mfValues mf = new mfValues();
//    float t1, t2; 
//    public MonoFilter(float fre, float r)
//    {
//        mf.LP = true;
//        frequency = fre;
//        resonance = r;        
//        Update();
//    }

//    public void SetFrequency(float fre)
//    {
//        frequency = fre;
//        Update();
//    }

//    public void SetResonance(float r)
//    {
//        resonance = r;
//        Update();
//    }

//    public void Update()
//    {
//        mf.q = 1.0f - frequency;
//        mf.p = frequency + 0.8f * frequency * mf.q;
//        mf.f = mf.p + mf.p - 1.0f;
//        mf.q = resonance * (1.0f + 0.5f * mf.q * (1.0f - mf.q + 5.6f * mf.q * mf.q));
//    }
//}

