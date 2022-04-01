// Copyright 2017-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
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

public class samplerDeviceInterface : deviceInterface {
  public dial speedDial, volumeDial, headTrimDial, tailTrimDial;
  public omniJack freqExpInput, freqLinInput, volumeInput, controlInput, output;
  public omniJack headInput, tailInput;
  public basicSwitch dirSwitch, loopSwitch;
  public button playButton, turntableButton;
  public sliderUneven headSlider, tailSlider;
  public GameObject turntableObject;
  clipPlayerComplex player;
  signalGenerator seq;

  bool turntableOn = false;
  public override void Awake() {
    base.Awake();
    player = GetComponent<clipPlayerComplex>();
  }

  public override void hit(bool on, int ID = -1) {
    if (ID == 0) player.togglePause(on);
    if (ID == 1 && on) player.Back();
    if (ID == 2) {
      turntableObject.SetActive(on);
      turntableOn = on;
    }
  }

  public void playEvent(bool on) {
    playButton.phantomHit(on);
  }

  int[] lastSignal = new int[] { 0, 0 };
  void Update() {
    float mod = dirSwitch.switchVal ? 1 : -1;
    if (dirSwitch.switchVal != player.playdirection) player.playdirection = dirSwitch.switchVal;

    player.playbackSpeed = 1f * Mathf.Pow(2, Utils.map(speedDial.percent, 0f, 1f, -4f, 4f)) * mod; // base is 100 percent playback speed, can be altered over 10 octaves    
    player.amplitude = Mathf.Pow(volumeDial.percent, 2);

    player.headTrim = headTrimDial.percent;
    player.tailTrim = tailTrimDial.percent;

    if (loopSwitch.switchVal != player.looping) player.looping = loopSwitch.switchVal;

    if (player.freqExpGen != freqExpInput.signal) player.freqExpGen = freqExpInput.signal;
    if (player.freqLinGen != freqLinInput.signal) player.freqLinGen = freqLinInput.signal;

    if (player.ampGen != volumeInput.signal) player.ampGen = volumeInput.signal;
    if (player.seqGen != controlInput.signal) player.seqGen = controlInput.signal;
    if (seq != controlInput.signal) seq = controlInput.signal;

    if (player.headGen != headInput.signal) player.headGen = headInput.signal;
    if (player.tailGen != tailInput.signal) player.tailGen = tailInput.signal;

    if(headInput.signal == null){ // head cv not plugged in
      if (headSlider.percent != player.trackBounds.x)
      {
        player.trackBounds.x = headSlider.percent;
        player.updateTrackBounds();
      }
      headSlider.bounds.x = tailSlider.transform.localPosition.x;
    } else {
      headSlider.setPercent(Mathf.Clamp01(player.headOffset)); // map cv to slider
    }

    if(tailInput.signal == null) { // tail cv not plugged in
      if (tailSlider.percent != player.trackBounds.y)
      {
        player.trackBounds.y = tailSlider.percent;
        player.updateTrackBounds();
      }
      tailSlider.bounds.y = headSlider.transform.localPosition.x;
    } else {
      tailSlider.setPercent(Mathf.Clamp01(player.tailOffset)); // map cv to slider
    }
    
    if (headSlider.percent >= tailSlider.percent) // maybe just suboptimal, how to deal with invalid head/tail values? seems to work for now
      tailSlider.percent = headSlider.percent + 0.02f;
    
    
  }

  public override InstrumentData GetData() {
    SamplerData data = new SamplerData();
    data.deviceType = menuItem.deviceType.Sampler;
    GetTransformData(data);
    data.speedDial = speedDial.percent;
    data.ampDial = volumeDial.percent;
    data.headTrimDial = headTrimDial.percent;
    data.tailTrimDial = tailTrimDial.percent;

    data.file = GetComponent<samplerLoad>().CurFile;
    data.label = GetComponent<samplerLoad>().CurTapeLabel;

    data.jackInAmpID = volumeInput.transform.GetInstanceID();
    data.jackInFreqExpID = freqExpInput.transform.GetInstanceID();
    data.jackInFreqLinID = freqLinInput.transform.GetInstanceID();
    data.jackInSeqID = controlInput.transform.GetInstanceID();
    data.jackOutID = output.transform.GetInstanceID();
    data.dirSwitch = dirSwitch.switchVal;
    data.loopSwitch = loopSwitch.switchVal;
    data.headPos = headSlider.percent;
    data.tailPos = tailSlider.percent;

    data.playToggle = playButton.isHit;

    data.turntable = turntableOn;
    data.turntablePos = turntableObject.transform.localPosition;
    data.turntableRot = turntableObject.transform.localRotation;

    return data;
  }

  public override void Load(InstrumentData d) {
    SamplerData data = d as SamplerData;
    base.Load(data);
    speedDial.setPercent(data.speedDial);
    volumeDial.setPercent(data.ampDial);
    headTrimDial.setPercent(data.headTrimDial);
    tailTrimDial.setPercent(data.tailTrimDial);
    GetComponent<samplerLoad>().SetSample(data.label, data.file);

    volumeInput.ID = data.jackInAmpID;
    freqExpInput.ID = data.jackInFreqExpID;
    controlInput.ID = data.jackInSeqID;
    output.ID = data.jackOutID;

    playButton.startToggled = data.playToggle;
    dirSwitch.setSwitch(data.dirSwitch);
    loopSwitch.setSwitch(data.loopSwitch);
    headSlider.setPercent(data.headPos);
    tailSlider.setPercent(data.tailPos);

    turntableButton.startToggled = data.turntable;
    if (data.turntablePos != Vector3.zero) turntableObject.transform.localPosition = data.turntablePos;
    if (data.turntableRot != Quaternion.identity) turntableObject.transform.localRotation = data.turntableRot;
  }
}

public class SamplerData : InstrumentData {
  public string label;
  public string file;
  public float ampDial;
  public float speedDial;
  public float headTrimDial;
  public float tailTrimDial;
  public int jackInAmpID;
  public int jackInFreqExpID;
  public int jackInFreqLinID;
  public int jackInSeqID;
  public int jackOutID;
  public bool dirSwitch;

  public bool playToggle;

  public bool loopSwitch;
  public float headPos = 0;
  public float tailPos = 1;

  public bool turntable;
  public Vector3 turntablePos;
  public Quaternion turntableRot;
}
