// Copyright � 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
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
// Copyright � 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright � 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright � 2017 Apache 2.0 Google LLC SoundStage VR
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class timelineTrackComponentInterface : componentInterface {

  public int ID = 0;

  clipPlayerSimple player;
  public button triggerButton;
  public omniJack jackIn, sampleOut, pulseOut;
  public timelineTrackSignalGenerator signal;
  timelineDeviceInterface _deviceInterface;
  public embeddedSpeaker embedSpeaker;

  public enum sigSource {
    button,
    signal,
    timeline
  };

  public struct trackComponent {
    public int jackInID;
    public int sampleOutID;
    public int pulseOutID;
    public string label;
    public string filename;

    public trackComponent(int idA, int idB, int idC, string l, string f) {
      jackInID = idA;
      sampleOutID = idB;
      pulseOutID = idC;
      label = l;
      filename = f;
    }
  };

  public void Load(trackComponent t) {
    jackIn.ID = t.jackInID;
    sampleOut.ID = t.sampleOutID;
    pulseOut.ID = t.pulseOutID;
    GetComponentInChildren<samplerLoad>().SetSample(t.label, t.filename);
  }

  void Awake() {
    player = GetComponent<clipPlayerSimple>();
    signal = GetComponent<timelineTrackSignalGenerator>();
    _deviceInterface = GetComponentInParent<timelineDeviceInterface>();
  }

  public trackComponent getTrackInfo() {
    string l = "";
    string f = "";
    GetComponentInChildren<samplerLoad>().getTapeInfo(out l, out f);
    return new trackComponent(
        jackIn.transform.GetInstanceID(),
        sampleOut.transform.GetInstanceID(),
        pulseOut.transform.GetInstanceID(),
        l, f);
  }

  void Update() {
    if (signal.incoming != jackIn.signal) {
      signal.incoming = jackIn.signal;
      if (signal.incoming == null) updateSignal(false, sigSource.signal);
    }
  }

  public bool isOutgoing() {
    bool a = (sampleOut.near != null);
    bool b = (pulseOut.near != null);
    bool c = embedSpeaker.activated;
    return a || b || c;
  }

  bool[] sourceState = new bool[] { false, false, false };
  public void updateSignal(bool on, sigSource source) {
    sourceState[(int)source] = on;
    setSignal(sourceState[0] || sourceState[1] || sourceState[2], sourceState[0] || sourceState[1]);
  }

  bool externalSignalOn = false;
  bool signalOn = false;
  public void setSignal(bool on, bool externOn) // from outside
  {
    if (signalOn != on) {
      signalOn = on;
      signal.setSignal(on);
    }

    if (externalSignalOn != externOn) {
      externalSignalOn = externOn;
      _deviceInterface.trackUpdate(ID, on);
    }
  }

  public void onTimelineEvent(bool on) {
    updateSignal(on, sigSource.timeline);
  }

  public override void hit(bool on, int ID = -1) {
    updateSignal(on, sigSource.button);
  }
}
