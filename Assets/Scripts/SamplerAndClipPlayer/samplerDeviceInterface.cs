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
using System.Runtime;

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
  float tmp;
  
  void Update() {
    float mod = dirSwitch.switchVal ? 1 : -1;
    if (dirSwitch.switchVal != player.playdirection) player.playdirection = dirSwitch.switchVal;

    player.playbackSpeed = 1f * Mathf.Pow(2, Utils.map(speedDial.percent, 0f, 1f, -4f, 4f)) * mod; // base is 100 percent playback speed, can be altered over 8 octaves    
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
        player.updateSampleBounds();
      }
      headSlider.bounds.x = tailSlider.transform.localPosition.x;
    } else if (player.loaded) 
    {
      tmp = Mathf.Clamp01((float)player.sampleBounds[0] / player.consolidatedSampleLength);
      if(!double.IsNaN(tmp)) headSlider.setPercent(tmp); // map cv to slider 
    }

    if(tailInput.signal == null) { // tail cv not plugged in
      if (tailSlider.percent != player.trackBounds.y)
      {
        player.trackBounds.y = tailSlider.percent;
        player.updateSampleBounds();
      }
      tailSlider.bounds.y = headSlider.transform.localPosition.x;
    }
    else if(player.loaded) {
      tmp = Mathf.Clamp01((float)player.sampleBounds[1] / player.consolidatedSampleLength);
      if (!double.IsNaN(tmp)) tailSlider.setPercent(tmp); // map cv to slider 
    }
    
    if (headSlider.percent >= tailSlider.percent) 
      tailSlider.percent = Mathf.Clamp01(headSlider.percent + 0.04f);
    
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
    data.jackHeadID = headInput.transform.GetInstanceID();
    data.jackTailID = tailInput.transform.GetInstanceID();
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
    freqLinInput.ID = data.jackInFreqLinID;
    controlInput.ID = data.jackInSeqID;
    headInput.ID = data.jackHeadID; 
    tailInput.ID = data.jackTailID;    
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
  public int jackHeadID;
  public int jackTailID;
  public bool dirSwitch;

  public bool playToggle;

  public bool loopSwitch;
  public float headPos = 0;
  public float tailPos = 1;

  public bool turntable;
  public Vector3 turntablePos;
  public Quaternion turntableRot;
}
