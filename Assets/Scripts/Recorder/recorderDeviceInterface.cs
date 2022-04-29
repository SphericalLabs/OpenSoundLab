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

public class recorderDeviceInterface : deviceInterface {
  public omniJack input, output, recordTrigger, playTrigger, backTrigger;
  public sliderNotched durSlider;
  waveTranscribeRecorder transcriber;
  public basicSwitch normalizeSwitch;
  public button[] buttons;
  public AudioSource babySpeakerRim;

  int[] durations = new int[] { 300, 150, 60, 30, 10 };//{ 10,30,60,150,300 };

  public override void Awake() {
    base.Awake();
    transcriber = GetComponent<waveTranscribeRecorder>();
    //durSlider = GetComponentInChildren<sliderNotched>();
  }

  void Update() {
    if (input.signal != transcriber.incoming) transcriber.incoming = input.signal;
    if (durations[durSlider.switchVal] != transcriber.duration) {
      transcriber.updateDuration(durations[durSlider.switchVal]);
    }
  }

  public override void hit(bool on, int ID = -1) {
    if (ID == 2 && on) transcriber.Back();
    if (ID == 3 && on) transcriber.Save();
    if (ID == 4 && on) transcriber.Flush();

    if (ID == 0) {
      if (on) buttons[1].keyHit(true);
      transcriber.recording = on;
    }
    if (ID == 1) {
      if (!on) buttons[0].keyHit(false);
      transcriber.playing = on;
    }
    if(ID == 5){
      babySpeakerRim.mute = on;
    }
  }

  public override InstrumentData GetData() {
    RecorderData data = new RecorderData();
    data.deviceType = menuItem.deviceType.Recorder;
    GetTransformData(data);

    data.jackInID = input.transform.GetInstanceID();
    data.jackOutID = output.transform.GetInstanceID();
    data.recordTriggerID = recordTrigger.transform.GetInstanceID();
    data.playTriggerID = playTrigger.transform.GetInstanceID();
    data.backTriggerID = backTrigger.transform.GetInstanceID();
    data.dur = durSlider.switchVal;
    data.normalize = normalizeSwitch.switchVal;
    return data;
  }

  public override void Load(InstrumentData d) {
    RecorderData data = d as RecorderData;
    base.Load(data);
    input.ID = data.jackInID;
    output.ID = data.jackOutID;
    recordTrigger.ID = data.recordTriggerID;
    playTrigger.ID = data.playTriggerID;
    backTrigger.ID = data.backTriggerID;
    durSlider.setVal(data.dur);
    normalizeSwitch.setSwitch(data.normalize, true);
  }
}

public class RecorderData : InstrumentData {
  public int jackOutID;
  public int jackInID;
  public int recordTriggerID;
  public int playTriggerID;
  public int backTriggerID;
  public int dur;
  public bool normalize;
  public string audioFilename;
}