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
    MC.updateBinaural(num);
  }

  public void UpdateQuality(bool on) {
    if (!on) return;
    int num = System.Int32.Parse(qualityGroup.ActiveToggles().First().transform.parent.name);
    PlayerPrefs.SetInt("audioQuality", num);
  }
}
