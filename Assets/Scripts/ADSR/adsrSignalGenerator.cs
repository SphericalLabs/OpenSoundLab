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

﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class adsrSignalGenerator : signalGenerator {
  public float[] durations = new float[] { 1f, 1.4f, 1.2f };
  public float[] volumes = new float[] { 1, 0.8f };
  public bool active = false;

  int[] frames = new int[] { 1, 1, 1, 1 };

  float[] lastDur = new float[] { 0, 0, 0 };
  float[] lastVol = new float[] { 0, 0 };
  float ADSRvolume = -1;

  public bool sustaining = false;
  float sustainTime = 0;
  double _lastPhase = 0;

  float[] pulseBuffer;

  public signalGenerator incoming;
  public adsrDeviceInterface _devinterface;

  [DllImport("SoundStageNative", EntryPoint = "ADSRSignalGenerator")]
  public static extern void ADSRSignalGenerator(float[] buffer, int length, int channels, int[] frames, ref int frameCount, bool active, ref float ADSRvolume,
  float[] volumes, float startVal, ref int curFrame, bool sustaining);

  [DllImport("SoundStageNative")]
  public static extern bool GetBinaryState(float[] buffer, int length, int channels, ref float lastBuf);

  [DllImport("SoundStageNative")]
  public static extern void SetArrayToSingleValue(float[] a, int length, float val);

  public override void Awake() {
    base.Awake();
    pulseBuffer = new float[MAX_BUFFER_LENGTH];
  }

  public void hit(bool on) {
    if (on == sustaining) return;

    if (on) {
      active = true;
      _phase = 0;
      sustainTime = 0;

      startVal = 0;

      adsrValUpdate();

      curFrame = 0;
      frameCount = 0;
    } else if (curFrame == 0 && frameCount == 0) active = false;

    sustaining = on;
  }

  float startVal = 0;
  float lastPulseFloat = 0;

  void adsrValUpdate() {
    bool unchanged = true;
    for (int i = 0; i < 3; i++) {
      if (durations[i] != lastDur[i]) {
        unchanged = false;
        lastDur[i] = durations[i];
      }
    }

    for (int i = 0; i < 2; i++) {
      if (volumes[i] != lastVol[i]) {
        unchanged = false;
        lastVol[i] = volumes[i];
      }
    }

    if (!unchanged) {
      frames[0] = Mathf.RoundToInt(durations[0] * (float)_sampleRate);
      frames[1] = Mathf.RoundToInt(durations[1] * (float)_sampleRate);
      frames[3] = Mathf.RoundToInt(durations[2] * (float)_sampleRate);

      if (frames[0] == 0) frames[0] = 1;
      if (frames[1] == 0) frames[1] = 1;
      if (frames[3] == 0) frames[3] = 1;
    }
  }

  float getADSR() {
    switch (curFrame) {
      case 0:
        return startVal + (volumes[0] - startVal) * frameCount / frames[0];

      case 1:
        return volumes[0] + (volumes[1] - volumes[0]) * (float)frameCount / (float)frames[1];

      case 2:
        return volumes[1];

      case 3:
        return volumes[1] * (1f - (float)frameCount / (float)frames[3]);
      case 4:
        return 0;
      default:
        break;
    }
    return 0;
  }

  float lastBuffer = -1;
  int curFrame = 0;
  int frameCount = 0;
  bool markForReleaseInNextFrame = false; // workaround, ADSRSignalGenerator expects to be released not 1 sample, but one buffer later...
  public override void processBuffer(float[] buffer, double dspTime, int channels) {
    if (!recursionCheckPre()) return; // checks and avoids fatal recursions

    if (markForReleaseInNextFrame){
      markForReleaseInNextFrame = false;
      hit(false);
    }

    if (incoming != null) {
      if (pulseBuffer.Length != buffer.Length)
        System.Array.Resize(ref pulseBuffer, buffer.Length);

      SetArrayToSingleValue(pulseBuffer, pulseBuffer.Length, 0f);
      incoming.processBuffer(pulseBuffer, dspTime, channels);

      for (int n = 0; n < buffer.Length; n += 2){
          if (pulseBuffer[n] > 0f && lastPulseFloat <= 0f) // high
          { 
            hit(true);
          } else if (pulseBuffer[n] <= 0f && lastPulseFloat >= 0f) { // low
            markForReleaseInNextFrame = true;
          }
          lastPulseFloat = pulseBuffer[n];
        }
      
    }

    ADSRSignalGenerator(buffer, buffer.Length, channels, frames, ref frameCount, active, ref ADSRvolume, volumes, startVal, ref curFrame, sustaining);
    recursionCheckPost();
  }


}
