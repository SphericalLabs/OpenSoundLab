// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class midiDeviceInterface : deviceInterface {

  public GameObject midiCCprefab;
  public GameObject midiNoteprefab;
  midiComponentInterface _midiComponentInterface;

  Dictionary<int, midiCC>[] midiCCchannels = new Dictionary<int, midiCC>[17];
  Dictionary<int, midiNote>[] midiNotechannels = new Dictionary<int, midiNote>[17];
  List<simpleMIDI> midiToDo = new List<simpleMIDI>();

  int noteCount = 0;
  int ccCount = 0;

  public struct simpleMIDI {
    public int channel;
    public int ID;
    public bool CC;
    public int value;
    public int jackID;

    public simpleMIDI(int _channel, int _ID, int _value, bool _CC, int _jackID = -1) {
      channel = _channel;
      ID = _ID;
      value = _value;
      CC = _CC;
      jackID = _jackID;
    }
  }

  public override void Awake() {
    base.Awake();
    _midiComponentInterface = GetComponentInChildren<midiComponentInterface>();
    for (int i = 0; i < 17; i++) {
      midiCCchannels[i] = new Dictionary<int, midiCC>();
      midiNotechannels[i] = new Dictionary<int, midiNote>();
    }
  }

  void Update() {
    if (midiToDo.Count > 0) {
      for (int i = 0; i < midiToDo.Count; i++) {
        if (midiToDo[i].CC) {
          midiCC m = createMidiCC(midiToDo[i].channel, midiToDo[i].ID);
          m.UpdateValue(midiToDo[i].value);
          m.UpdateJackID(midiToDo[i].jackID);
        } else {
          midiNote m = createMidiNote(midiToDo[i].channel, midiToDo[i].ID);
          m.UpdateValue(midiToDo[i].value != 0);
          m.UpdateJackID(midiToDo[i].jackID);
        }
      }

      midiToDo.Clear();
    }
  }

  midiCC getMidiCC(int channel, int ID) {
    if (midiCCchannels[channel].ContainsKey(ID)) return midiCCchannels[channel][ID];
    else return null;
  }

  midiNote getMidiNote(int channel, int ID) {
    if (midiNotechannels[channel].ContainsKey(ID)) return midiNotechannels[channel][ID];
    else return null;
  }

  midiCC createMidiCC(int channel, int ID) {
    if (midiCCchannels[channel].ContainsKey(ID)) return midiCCchannels[channel][ID];

    ccCount++;
    midiCCchannels[channel][ID] = (Instantiate(midiCCprefab, transform, false) as GameObject).GetComponent<midiCC>();
    midiCCchannels[channel][ID].transform.localPosition = new Vector3(0.03f, ccCount * -.04f, 0);
    midiCCchannels[channel][ID].transform.localRotation = Quaternion.identity;
    string s = "CC" + ID + "\n(CH" + channel + ")";
    midiCCchannels[channel][ID].SetAppearance(s, 0.25f);
    return midiCCchannels[channel][ID];
  }

  midiNote createMidiNote(int channel, int ID) {
    if (midiNotechannels[channel].ContainsKey(ID)) return midiNotechannels[channel][ID];

    noteCount++;
    midiNotechannels[channel][ID] = (Instantiate(midiNoteprefab, transform, false) as GameObject).GetComponent<midiNote>();
    midiNotechannels[channel][ID].transform.localPosition = new Vector3(-.14f, .045f + noteCount * -.04f, 0);
    midiNotechannels[channel][ID].transform.localRotation = Quaternion.identity;

    string s = ID + " " + ((Midi.Pitch)ID).ToString().Replace("Sharp", "#") + "\n(CH" + channel + ")";
    midiNotechannels[channel][ID].SetAppearance(s, 0.25f);
    return midiNotechannels[channel][ID];
  }

  public override void OnMidiNote(int channel, bool on, int pitch) {
    midiNote m = getMidiNote(channel, pitch);
    if (m != null) m.UpdateValue(on);
    else midiToDo.Add(new simpleMIDI(channel, pitch, on ? 127 : 0, false));
  }

  public override void OnMidiCC(int channel, int ID, int value) {
    midiCC m = getMidiCC(channel, ID);
    if (m != null) m.UpdateValue(value);
    else midiToDo.Add(new simpleMIDI(channel, ID, value, true));
  }

  public override InstrumentData GetData() {
    MIDIinData data = new MIDIinData();
    data.deviceType = menuItem.deviceType.MIDIIN;
    GetTransformData(data);

    data.connection = _midiComponentInterface.connectedDevice;

    List<simpleMIDI> compiledInputs = new List<simpleMIDI>();

    for (int i = 0; i < 17; i++) {
      // midiCC
      foreach (KeyValuePair<int, midiCC> entry in midiCCchannels[i]) {
        int val = Mathf.FloorToInt((entry.Value.curValue + 1) / 2f * 127);
        compiledInputs.Add(new simpleMIDI(i, entry.Key, val, true, entry.Value.GetJackID()));
      }

      // midi note
      foreach (KeyValuePair<int, midiNote> entry in midiNotechannels[i]) {
        compiledInputs.Add(new simpleMIDI(i, entry.Key, entry.Value.noteOn ? 127 : 0, false, entry.Value.GetJackID()));
      }
    }

    data.outputs = compiledInputs.ToArray();

    return data;
  }

  public override void Load(InstrumentData d) {
    MIDIinData data = d as MIDIinData;
    base.Load(data);

    if (data.connection != "") _midiComponentInterface.ConnectByName(data.connection);

    for (int i = 0; i < data.outputs.Length; i++) {
      midiToDo.Add(new simpleMIDI(data.outputs[i].channel, data.outputs[i].ID, data.outputs[i].value, data.outputs[i].CC, data.outputs[i].jackID));
    }

    Update();
  }
}

public class MIDIinData : InstrumentData {
  public string connection;
  public midiDeviceInterface.simpleMIDI[] outputs;
}
