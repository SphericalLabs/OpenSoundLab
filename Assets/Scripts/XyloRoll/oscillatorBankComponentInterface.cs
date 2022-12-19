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

public class oscillatorBankComponentInterface : componentInterface {

  public xylorollSignalGenerator signal;

  public dial[] ampDials;
  public dial[] freqDials;
  public slider[] waveSliders;

  public float[] ampPercent;
  public float[] freqPercent;
  public float[] wavePercent;

  void Start() {
    ampPercent = new float[2];
    freqPercent = new float[2];
    wavePercent = new float[2];

    updateOscillators();
  }

  public void setValues(float oscAamp, float oscAfreq, float oscAwave, float oscBamp, float oscBfreq, float oscBwave) {
    ampDials[0].setPercent(oscAamp);
    ampDials[1].setPercent(oscBamp);

    freqDials[0].setPercent(oscAfreq);
    freqDials[1].setPercent(oscBfreq);

    waveSliders[0].setPercent(oscAwave);
    waveSliders[1].setPercent(oscBwave);
  }

  void updateOscillators() {
    for (int i = 0; i < 2; i++) {
      ampPercent[i] = ampDials[i].percent;
      freqPercent[i] = freqDials[i].percent;
      wavePercent[i] = waveSliders[i].percent;
    }

    signal.updateOscAmp(ampPercent, freqPercent, wavePercent);
  }

  void Update() {

    bool needUpdate = false;
    for (int i = 0; i < 2; i++) {
      if (ampDials[i].percent != ampPercent[i]) needUpdate = true;
      else if (freqDials[i].percent != freqPercent[i]) needUpdate = true;
      else if (waveSliders[i].percent != wavePercent[i]) needUpdate = true;
    }
    if (needUpdate) updateOscillators();
  }
}
