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
using System.Collections;

public class looperDeviceInterface : deviceInterface {

  public omniJack input, output, recordTrigger, playTrigger;
  public sliderNotched durSlider;
  waveTranscribeLooper transcriber;
  public button[] buttons;

  public TextMesh countdownText;

  float[] durations = new float[] { 32, 16, 8, 4, 1 };

  public int curSliderVal = 0;
  public double period = .0625;

  beatTracker _beatManager;
  AudioSource audioSource;

  public override void Awake() {
    base.Awake();
    transcriber = GetComponent<waveTranscribeLooper>();
    audioSource = GetComponent<AudioSource>();
    durSlider = GetComponentInChildren<sliderNotched>();
    _beatManager = ScriptableObject.CreateInstance<beatTracker>();
  }

  void Start() {
    _beatManager.setTrigger(onBeatEvent);
    _beatManager.updateBeatNoTriplets(2);
  }

  void OnDestroy() {
    Destroy(_beatManager);
  }

  public bool playClick = false;
  void Update() {
    if (input.signal != transcriber.incoming) transcriber.incoming = input.signal;

    if (playClick) {
      audioSource.Play();
      playClick = false;
    }
    if (curSliderVal != durSlider.switchVal || period != masterControl.instance.measurePeriod) {

      period = masterControl.instance.measurePeriod;
      curSliderVal = durSlider.switchVal;
      transcriber.updateDuration(durations[durSlider.switchVal], period);
    }

    countdownText.gameObject.SetActive(recordCountdown || playCountdown);
    if (recordCountdown || playCountdown) {
      countdownText.transform.localRotation = Quaternion.Euler(0, 180, countdownText.transform.parent.localRotation.eulerAngles.z);
      if (recordCountdown) countdownText.text = recCountdownRemaining.ToString();
      else countdownText.text = "";
    }
  }

  void RecordCountdown() {
    recordCountdown = true;
    if (!transcriber.playing) {
      if (playCountdown) recCountdownRemaining = playCountdownRemaining;
      else recCountdownRemaining = 4;
    } else {
      transcriber.requestRecord(true);
    }
  }

  public bool recordCountdown = false;

  public int recCountdownRemaining = 0;

  int playCountdownRemaining = 0;
  public bool playCountdown = false;

  void PlayCountdown() {
    if (recordCountdown) playCountdownRemaining = recCountdownRemaining;
    else playCountdownRemaining = 1;
    playCountdown = true;
  }

  void onBeatEvent() {
    if (recordCountdown && !transcriber.playing) {
      transcriber.requestRecord(false);
      recCountdownRemaining--;
      playClick = true;
      if (recCountdownRemaining == 0) {
        recordCountdown = false;
        transcriber.Record();
        buttons[1].phantomHit(true);
      }
    }

    if (playCountdown) {
      playCountdownRemaining--;
      if (playCountdownRemaining == 0) {
        playCountdown = false;
        transcriber.Back();
        transcriber.playing = true;
      }
    }
  }

  void StartRecord(bool on) {
    if (on) buttons[1].keyHit(true);
    transcriber.recording = false;
  }

  public override void hit(bool on, int ID = -1) {
    if (ID == 3 && on) transcriber.Save();
    if (ID == 4 && on) transcriber.Flush();

    if (ID == 0) {
      if (on) {
        RecordCountdown();

      } else {
        transcriber.recording = false;
        recordCountdown = false;
        transcriber.requestRecord(false);
      }

    }
    if (ID == 1) {
      if (on) PlayCountdown();
      else {
        playCountdown = false;
        buttons[0].keyHit(false);
        transcriber.Back();
        transcriber.playing = false;
      }
    }
  }

  public override InstrumentData GetData() {
    LooperData data = new LooperData();
    data.deviceType = menuItem.deviceType.Looper;
    GetTransformData(data);

    data.jackInID = input.transform.GetInstanceID();
    data.jackOutID = output.transform.GetInstanceID();
    data.recordTriggerID = recordTrigger.transform.GetInstanceID();
    data.playTriggerID = playTrigger.transform.GetInstanceID();
    data.dur = durSlider.switchVal;
    return data;
  }

  public override void Load(InstrumentData d) {
    LooperData data = d as LooperData;
    base.Load(data);
    input.ID = data.jackInID;
    output.ID = data.jackOutID;
    recordTrigger.ID = data.recordTriggerID;
    playTrigger.ID = data.playTriggerID;
    durSlider.setVal(data.dur);
  }
}
public class LooperData : InstrumentData {
  public int jackOutID;
  public int jackInID;
  public int recordTriggerID;
  public int playTriggerID;
  public int dur;
}