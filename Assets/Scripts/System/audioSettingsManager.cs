// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This file is part of OpenSoundLab, which is based on SoundStage VR.
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
using UnityEngine.UI;
using System.Collections;
using System.Linq;

public class audioSettingsManager : MonoBehaviour {
  public ToggleGroup qualityGroup, binauralGroup;
  public Toggle[] qualityToggles, binauralToggles;
  public masterControl MC;
  public GameObject menu;
  double initialDspDelta;

  void Start() {
    if (!PlayerPrefs.HasKey("audioQuality")) PlayerPrefs.SetInt("audioQuality", 1);
    if (!PlayerPrefs.HasKey("audioBinaural")) PlayerPrefs.SetInt("audioBinaural", 0);

    menu.SetActive(true);
    qualityToggles[PlayerPrefs.GetInt("audioQuality")].isOn = true;
    binauralToggles[PlayerPrefs.GetInt("audioBinaural")].isOn = true;
    menu.SetActive(false);
    initialDspDelta = AudioSettings.dspTime - Time.realtimeSinceStartup;

    setupAudioBuffer();
  }

  void setupAudioBuffer() {
    AudioConfiguration config = AudioSettings.GetConfiguration();

    switch (PlayerPrefs.GetInt("audioQuality")) {
      case 0:
        config.dspBufferSize = 256;
        break;
      case 1:
        config.dspBufferSize = 512;
        break;
      case 2:
        config.dspBufferSize = 1024;
        break;
      default:
        break;
    }

    AudioSource[] sources = FindObjectsOfType<AudioSource>();
    for (int i = 0; i < sources.Length; i++) sources[i].enabled = false;
    AudioSettings.Reset(config);
    for (int i = 0; i < sources.Length; i++) sources[i].enabled = true;
  }

  public void UpdateBinaural(bool on) {
    if (!on) return;
    int num = System.Int32.Parse(binauralGroup.ActiveToggles().First().transform.parent.name);
    PlayerPrefs.SetInt("audioBinaural", num);
    masterControl.updateBinaural(num);
  }

  public void UpdateQuality(bool on) {
    if (!on) return;
    int num = System.Int32.Parse(qualityGroup.ActiveToggles().First().transform.parent.name);
    PlayerPrefs.SetInt("audioQuality", num);
  }
}
