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

public class embeddedSpeaker : MonoBehaviour {
  public omniJack speakerOut;
  signalGenerator signal;
  public speaker output;
  public AudioSource audio;
  public GameObject speakerRim;

  bool secondary = true;

  public bool activated = false;
  Material rimMat;
  void Awake() {
    signal = transform.parent.GetComponent<signalGenerator>();

    if (speakerRim != null) {
      rimMat = speakerRim.GetComponent<Renderer>().material;
      rimMat.SetFloat("_EmissionGain", .45f);
      speakerRim.SetActive(false);

    }
  }

  void Start() {
    audio.spatialize = (masterControl.instance.BinauralSetting == masterControl.BinauralMode.All);
  }

  public void updateSecondary(bool on) {
    secondary = on;
    updateSpeaker();
  }

  void updateSpeaker() {
    if (output.incoming == null || !secondary) {
      if (speakerRim != null) {
        activated = false;
        speakerRim.SetActive(false);
      }
    } else {
      if (speakerRim != null && secondary) {
        activated = true;
        speakerRim.SetActive(true);
      }
    }
  }

  float smoothedPeaks = 0;
  signalGenerator curSignal, prevSignal;
  void Update() {
    if (speakerOut.near == null) {
      curSignal = signal;
    } else {
      curSignal = null;
    }

    if (prevSignal != curSignal) {
      output.incoming = prevSignal = curSignal;
      updateSpeaker();
    }
  }
}
