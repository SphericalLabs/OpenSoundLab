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

public class metronome : componentInterface {
  public dial bpmDial, volumeDial;

  float bpm = 120f;
  float minBpm = 60f;
  float maxBpm = 180f;
  float pitchBendMult = 1f;

  float bpmpercent = .1f;

  float volumepercent = 0;

  public masterBusRecorder recorder;

  public Transform rod;
  public TextMesh txt;

  void Awake() {
    bpmDial = GetComponentInChildren<dial>();
    recorder = GetComponentInChildren<masterBusRecorder>();
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

    if (ID == 3 && on) pitchBendMult = 1 / 1.03f;
    if (ID == 3 && !on) pitchBendMult = 1;
    if (ID == 4 && on) pitchBendMult = 1 * 1.03f;
    if (ID == 4 && !on) pitchBendMult = 1;

    if (ID == 5) recorder.ToggleRec(on);

    broadcastBpm(); // temporary bpm nudging
  }

  void OnEnable() {
    bpm = masterControl.instance.bpm;
    bpmpercent = Utils.map(bpm, minBpm, maxBpm, 0f, 1f);

    bpmDial.setPercent(bpmpercent);
    txt.text = bpm.ToString("N1");
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

  void broadcastBpm(){
    bpm = Utils.map(bpmpercent, 0f, 1f, minBpm, maxBpm) * pitchBendMult;
    masterControl.instance.setBPM(bpm);
    txt.text = bpm.ToString("N1");
  }
}
