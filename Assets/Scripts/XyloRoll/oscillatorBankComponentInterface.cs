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
