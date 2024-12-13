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
using System.Runtime.InteropServices;

public class polarizerSignalGenerator : signalGenerator
{

  public signalGenerator incoming;
  public bool uniToBi = true;

  public override void processBufferImpl(float[] buffer, double dspTime, int channels)
  {
    if (!recursionCheckPre()) return; // checks and avoids fatal recursions 
    if (incoming == null){
      return;
    } else {
      incoming.processBuffer(buffer, dspTime, channels);
    }

    if (uniToBi) // 0,1 -> -1,1
    {
      for (int n = 0; n < buffer.Length; n++)
      {
        buffer[n] = buffer[n] * 2f - 1f;
      }
    }
    else // -1,1 -> 0,1
    {
      for (int n = 0; n < buffer.Length; n++)
      {
        buffer[n] = buffer[n] * 0.5f + 0.5f;
      }
    }
    recursionCheckPost();
  }
}
