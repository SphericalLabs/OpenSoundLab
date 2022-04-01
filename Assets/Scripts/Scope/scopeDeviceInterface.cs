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

public class scopeDeviceInterface : deviceInterface {

  int ID = 0;
  public omniJack input, output;

  spectrumDisplay displayFft;
  waveViz displayOsc;

  scopeSignalGenerator scopeSignal;

  public dial periodDial;
  public basicSwitch modeSelector;
  public button triggerButton;
  public button muteButton;

  public int bufferSize;
  float lastPeriodDialPercent = 0f;

  public override void Awake() {
    base.Awake();
    displayFft = GetComponentInChildren<spectrumDisplay>();
    displayOsc = GetComponentInChildren<waveViz>();
    scopeSignal = GetComponentInChildren<scopeSignalGenerator>();

    AudioConfiguration configuration = AudioSettings.GetConfiguration();
    bufferSize = configuration.dspBufferSize;
    periodDial.notchSteps = Mathf.RoundToInt(Mathf.Log(bufferSize, 2) + 1);

    displayOsc.sampleStep = calcSampleStep(periodDial.percent); // first init

  }

  void Update()
  {
    if(displayFft.gameObject.activeSelf != modeSelector.switchVal)
    {
      displayFft.gameObject.SetActive(modeSelector.switchVal);
    }
    if (displayOsc.gameObject.activeSelf == modeSelector.switchVal)
    {
      displayOsc.gameObject.SetActive(!modeSelector.switchVal);
    }

    if (periodDial.percent != lastPeriodDialPercent) {
      displayOsc.sampleStep = calcSampleStep(periodDial.percent);
      lastPeriodDialPercent = periodDial.percent;
    }

    if (scopeSignal.incoming != input.signal)
    {
      scopeSignal.incoming = input.signal;
      displayFft.toggleActive(scopeSignal.incoming != null);
    }

  }

  public override void hit(bool on, int ID = -1)
  {
    if(ID == 1){
      displayOsc.doTriggering = on;
    } else if(ID == 2){
      scopeSignal.isMuted = on;
    }
  }

  int calcSampleStep(float val){
    return Mathf.RoundToInt(Mathf.Pow(2, Mathf.Round(Utils.map(1-val, 0f, 1f, 0, Mathf.Log(bufferSize, 2))))); // 1 - 1024 in 2^ steps
  }
    
  public override InstrumentData GetData() {
    ScopeData data = new ScopeData();
    data.deviceType = menuItem.deviceType.Scope;
    GetTransformData(data);

    data.jackInID = input.transform.GetInstanceID();
    data.jackOutID = output.transform.GetInstanceID();
    data.periodState = periodDial.percent;
    data.modeState = modeSelector.switchVal;
    data.triggerState = triggerButton.isHit;
    data.muteState = muteButton.isHit;

    return data;
  }

  public override void Load(InstrumentData d) {
    ScopeData data = d as ScopeData;
    base.Load(data);

    ID = data.ID;
    input.ID = data.jackInID;
    output.ID = data.jackOutID;

    muteButton.setOnAtStart(data.muteState);
    triggerButton.setOnAtStart(data.triggerState);

    periodDial.setPercent(data.periodState);
    modeSelector.setSwitch(data.modeState, true);
  }

}


public class ScopeData : InstrumentData {
  public int jackOutID;
  public int jackInID;

  public float periodState;
  public bool modeState;
  public bool triggerState;
  public bool muteState;
}
