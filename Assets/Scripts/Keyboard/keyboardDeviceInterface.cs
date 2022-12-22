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
using System.Collections.Generic;

public class keyboardDeviceInterface : deviceInterface {
  public timelineComponentInterface _timeline;
  public midiComponentInterface _midiIn, _midiOut;
  public GameObject whiteKeyPrefab, blackKeyPrefab;
  public omniJack freqOutput, gateOutput;
  public button midiInButton, midiOutButton, sequenceButton;

  public midiOutOfRange midiLow, midiHigh;

  int keyCount = 12 * 2 + 1; 
  key[] keys;

  adsrInterface _adsrInterface;
  
  keyFrequencySignalGenerator freqSignal;
  keyGateSignalGenerator gateSignal;

  int curKey;

  keyState[] keyStates;

  public override void Awake() {
    base.Awake();

    keyStates = new keyState[keyCount];

    curKey = -1;

    _adsrInterface = GetComponentInChildren<adsrInterface>();
    
    freqSignal = GetComponent<keyFrequencySignalGenerator>();
    gateSignal = GetComponent<keyGateSignalGenerator>();

    freqOutput.homesignal = freqSignal;
    gateOutput.homesignal = gateSignal;

    keys = new key[keyCount];
    SpawnKeys();

    for (int i = 0; i < keyCount; i++) keyStates[i] = new keyState(false);

  }

  void Start(){
    enableSequencer(false);
  }

  void SpawnKeys() {
    float separation = .05f;
    int whiteCount = 0;
    for (int i = 0; i < keyCount; i++) {
      GameObject g;
      if (i % 12 == 1 || i % 12 == 3 || i % 12 == 6 || i % 12 == 8 || i % 12 == 10) {
        g = Instantiate(blackKeyPrefab, transform, false) as GameObject;
        g.transform.localPosition = new Vector3(-separation * whiteCount + separation / 2 + .15f, .03f, -.025f);
      } else {
        g = Instantiate(whiteKeyPrefab, transform, false) as GameObject;
        g.transform.localPosition = new Vector3(-separation * whiteCount + .15f, -.007f, .005f);
        whiteCount++;
      }
      keys[i] = g.GetComponent<key>();
      keys[i].keyValue = i;
      keys[i].isKeyboard = true;
      keys[i].sticky = false;
    }
  }


  public override void onTimelineEvent(int track, bool on) {
    asynchKeyHit(on, track, keyInput.seq);
  }

  bool midiLowDesired = false;
  bool midiHighDesired = false;

  public override void OnMidiNote(int channel, bool on, int pitch) {
    int ID = pitch - 48;
    if (ID < 0) {
      if (on) midiLowDesired = true;
    } else if (ID > 24) {
      if (on) midiHighDesired = true;
    } else {
      asynchKeyHit(on, ID, keyInput.midi);
    }
  }

  public void asynchKeyHit(bool on, int ID, keyInput k) {
    if (k == keyInput.midi) keyStates[ID].midiState = on;
    else if (k == keyInput.seq) keyStates[ID].seqState = on;
    else if (k == keyInput.touch) keyStates[ID].touchState = on;

    if (keyStates[ID].nonSeqStateChange()) {
      keyStates[ID].currentNonSeqState = keyStates[ID].getNonSeqState();
      _timeline.onTimelineEvent(ID, keyStates[ID].currentNonSeqState);
    }

    if (keyStates[ID].stateChange()) {
      on = keyStates[ID].currentState = keyStates[ID].getState();
      keys[ID].phantomHit(on);
      keyHitEvent(on, ID);
    }
  }

  void keyHitEvent(bool on, int ID) {
    if (on) {
      if (curKey != ID) {
        int prev = curKey;
        curKey = ID;

        if (prev != -1) {
          gateSignal.isHigh = false;
          if (_midiOut != null) _midiOut.OutputNote(false, prev);
        }

        if (_midiOut != null) _midiOut.OutputNote(on, ID);
        freqSignal.UpdateKey(curKey);
        gateSignal.isHigh = true;
        gateSignal.newKeyWasPressed = true;
      }
    } else {
      if (curKey == ID) {
        _midiOut.OutputNote(false, ID);
        gateSignal.isHigh = false;
        curKey = -1;
      }
    }
  }

  void toggleMIDIin(bool on) {
    _midiIn.gameObject.SetActive(on);
  }

  void toggleMIDIout(bool on) {
    _midiOut.gameObject.SetActive(on);
  }

  void enableSequencer(bool on) {
    _timeline.gameObject.SetActive(on);
  }

  public override void hit(bool on, int ID = -1) {
    if (ID == -1) {
    } else if (ID == -2) {
      toggleMIDIin(on);
    } else if (ID == -3) {
      toggleMIDIout(on);
    } else if (ID == -4) {
      enableSequencer(on);
    } else {
      asynchKeyHit(on, ID, keyInput.touch);
    }
  }

  void Update() {
    
    if (midiLowDesired) {
      midiLowDesired = false;
      midiLow.gameObject.SetActive(true);
      midiLow.Activate();
    }

    if (midiHighDesired) {
      midiHighDesired = false;
      midiHigh.gameObject.SetActive(true);
      midiHigh.Activate();
    }
  }

  public override InstrumentData GetData() {
    KeyboardData data = new KeyboardData();
    data.deviceType = menuItem.deviceType.Keyboard;
    GetTransformData(data);

    
    data.freqOutID = freqOutput.transform.GetInstanceID();
    data.gateOutID = gateOutput.transform.GetInstanceID();

    data.midiInConnection = _midiIn.connectedDevice;
    data.midiOutConnection = _midiOut.connectedDevice;
    data.sequencerEnabled = _timeline.gameObject.activeSelf;

    data.timelinePresent = true;
    data.timelineData = _timeline.GetTimelineData();
    data.timelineHeight = _timeline.heightHandle.transform.localPosition.y;
    List<timelineEvent.eventData> tempevents = new List<timelineEvent.eventData>();
    for (int i = 0; i < _timeline._tlEvents.Count; i++) {
      if (_timeline._tlEvents[i] != null) tempevents.Add(_timeline._tlEvents[i].getEventInfo());
    }
    data.timelineEvents = tempevents.ToArray();

    return data;
  }

  public override void Load(InstrumentData d) {
    KeyboardData data = d as KeyboardData;
    base.Load(data);

    freqOutput.ID = data.freqOutID;
    gateOutput.ID = data.gateOutID;

    
    if (data.midiInConnection != null & data.midiInConnection != "") {
      midiInButton.startToggled = true;
      _midiIn.ConnectByName(data.midiInConnection);
    }
    if (data.midiOutConnection != null & data.midiOutConnection != "") {
      midiOutButton.startToggled = true;
      _midiOut.ConnectByName(data.midiOutConnection);
    }

    sequenceButton.startToggled = data.sequencerEnabled;

    if (data.timelinePresent) {
      _timeline.SetTimelineData(data.timelineData);

      Vector3 pos = _timeline.heightHandle.transform.localPosition;
      pos.y = data.timelineHeight;
      _timeline.heightHandle.transform.localPosition = pos;
      _timeline.setStartHeight(data.timelineHeight);

      for (int i = 0; i < data.timelineEvents.Length; i++) {
        _timeline.SpawnTimelineEvent(data.timelineEvents[i].track, data.timelineEvents[i].in_out);
      }
    }
  }

  public enum keyInput {
    seq,
    midi,
    touch
  }

  struct keyState {
    public bool seqState;
    public bool midiState;
    public bool touchState;

    public bool currentState;
    public bool currentNonSeqState;

    public keyState(bool on) {
      currentNonSeqState = currentState = seqState = midiState = touchState = on;
    }

    public bool getState() {
      return seqState || midiState || touchState;
    }

    public bool getNonSeqState() {
      return midiState || touchState;
    }

    public bool stateChange() {
      return getState() != currentState;
    }

    public bool nonSeqStateChange() {
      return getNonSeqState() != currentNonSeqState;
    }
  };
}


public class KeyboardData : InstrumentData {
  public bool sequencerEnabled;

  public int freqOutID;
  public int gateOutID;
  public string midiInConnection;
  public string midiOutConnection;

  public bool timelinePresent;
  public TimelineComponentData timelineData;
  public timelineEvent.eventData[] timelineEvents;
  public float timelineHeight;
}