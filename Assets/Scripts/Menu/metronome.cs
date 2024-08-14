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

public class metronome : componentInterface {
  public dial bpmDial, volumeDial;

  float bpm = 120f;
  float minBpm = 60f;
  float maxBpm = 180f;
  float pitchBendMult = 1f;
    public float PitchBendMult { get => pitchBendMult; set => pitchBendMult = value; }
    public delegate void PitchBendChangeHandler(float pitchBendMult);
    public event PitchBendChangeHandler PitchBendChange;
  float bpmpercent = .1f;

  float volumepercent = 0;

  public Transform rod;
  public TextMesh txt;

  public button recButton;

  void Awake() {
    bpmDial = GetComponentInChildren<dial>();
    //TODO: This is not very pretty, but the masterBusRecorder needs a reference to the rec button in case a recording is stopped by the system (for example if the filesize limit is reached)!
    var buttons = GetComponentsInChildren<button>();
    foreach(button b in buttons)
    {
        if (b.buttonID == 5)
            recButton = b;
    }
  }

  public void Reset() {
    SetBPM(bpm);
  }

  public void SetBPM(float targ) {
    bpmpercent = Utils.map(targ, minBpm, maxBpm, 0f, 1f);
    bpmDial.setPercent(bpmpercent);
    readBpmDialAndBroadcast();
  }

  public override void hit(bool on, int ID = -1) {
    if (ID == 0) masterControl.instance.toggleBeatUpdate(on);
    if (ID == 1 && on) masterControl.instance.resetClock();

    if (ID == 3 && on) { pitchBendMult = 1 / 1.03f; PitchBendChange?.Invoke(pitchBendMult); }
    if (ID == 3 && !on) { pitchBendMult = 1; PitchBendChange?.Invoke(pitchBendMult); }
    if (ID == 4 && on) { pitchBendMult = 1 * 1.03f; PitchBendChange?.Invoke(pitchBendMult); }
    if (ID == 4 && !on) { pitchBendMult = 1; PitchBendChange?.Invoke(pitchBendMult); }

    if (ID == 5) masterControl.instance.recorder.ToggleRec(on);

    broadcastBpm(); // temporary bpm nudging
  }

  void OnEnable() {
    bpm = masterControl.instance.bpm;
    bpmpercent = Utils.map(bpm, minBpm, maxBpm, 0f, 1f);

    bpmDial.setPercent(bpmpercent);
    txt.text = bpm.ToString("N1");

    //In case the recording has been stopped while the menu was disabled, we have to set the recButton toggle state to false:
    var recorderState = masterControl.instance.recorder.state;
    if(recorderState == masterBusRecorder.State.Idle)
    {
        recButton.phantomHit(false);
    }
    }

    bool rodDir = false;



    void Update() {
    float cyc = Mathf.Repeat(masterControl.instance.curCycle * 4, 1);

    if (cyc < 0.5f) {
      rod.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-80, 80, cyc * 2));
      if (!rodDir) {
        rodDir = true;
      }
    } else {
      rod.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(80, -80, (cyc - .5f) * 2));
      if (rodDir) {
        rodDir = false;
      }
    }

    if (volumepercent != volumeDial.percent) {
      volumepercent = volumeDial.percent;
      masterControl.instance.metronomeClick.volume = Mathf.Clamp01(volumepercent - .1f);
    }

    if (bpmpercent != bpmDial.percent) readBpmDialAndBroadcast();
  }

  void readBpmDialAndBroadcast() {
    bpmpercent = bpmDial.percent;
    broadcastBpm();
  }

  public void broadcastBpm(){
    bpm = Utils.map(bpmpercent, 0f, 1f, minBpm, maxBpm) * pitchBendMult;
    masterControl.instance.setBPM(bpm);
    txt.text = bpm.ToString("N1");
  }
}
