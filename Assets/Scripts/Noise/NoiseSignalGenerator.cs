// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 
// 
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
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
using System;

public class NoiseSignalGenerator : signalGenerator {

  float speedPercent = 1;
  int speedFrames = 1;

  int maxLength = 11025 * 16; //  max length of one random value in samples
  int counter = 0; // used for downsampling

  float curSample = -1.0f;

  int noiseStep = 0; // count how many samples have been calculated with thise noiseGen already, used for syncing with other clients
  int seed = 0; // select a specific noise pattern
  IntPtr noiseProcessorPointer; // used in OSLNative
  private readonly object lockObject = new object();

    [DllImport("OSLNative")]
    private static extern IntPtr CreateNoiseProcessor(int seed);

    [DllImport("OSLNative")]
    private static extern void DestroyNoiseProcessor(IntPtr processor);

    [DllImport("OSLNative")]
    private static extern void NoiseProcessBuffer(IntPtr processor, float[] buffer, int length, int channels, float frequency, ref int counter, int speedFrames, ref bool updated);

    [DllImport("OSLNative")]
    private static extern void SyncNoiseProcessor(IntPtr processor, int seed, int steps);

  public bool updated = false;

    public int NoiseStep { get => noiseStep; set => noiseStep = value; }
    public int Seed { get => seed; set => seed = value; }

    public void updatePercent(float per) {
    if (speedPercent == per) return;
    speedPercent = per;
    speedFrames = Mathf.RoundToInt(maxLength * Mathf.Pow(Mathf.Clamp01(1f - per / 0.95f), 4));
  }

  public override void Awake(){
    base.Awake();
    noiseProcessorPointer = CreateNoiseProcessor(Utils.GetNoiseSeed());
    //SyncNoiseProcessor(noiseProcessorPointer, noiseStep); // noiseStep should be synced via Mirror if necessary
    //// or call it sync and use to also set seed?
  }

  public void OnDestroy(){
    DestroyNoiseProcessor(noiseProcessorPointer);
  }

  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    lock (lockObject)
    {
        NoiseProcessBuffer(noiseProcessorPointer, buffer, buffer.Length, channels, speedPercent, ref counter, speedFrames, ref updated);
        noiseStep += buffer.Length;
    }
  }

  public void syncNoiseSignalGenerator(int seed, int steps){
    lock (lockObject)
    {
        SyncNoiseProcessor(noiseProcessorPointer, seed, steps);
        noiseStep = steps;
    }
  }
}
