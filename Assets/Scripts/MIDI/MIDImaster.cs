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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Midi; //kill and remove plugin if you go that route

public class MIDImaster : MonoBehaviour {

  public static MIDImaster instance;
  public List<MIDIdevice> inputDevices = new List<MIDIdevice>();
  public List<MIDIdevice> outputDevices = new List<MIDIdevice>();
  List<MIDIdevice> connectedInputDevices = new List<MIDIdevice>();
  List<MIDIdevice> connectedOutputDevices = new List<MIDIdevice>();

  void Awake() {
    instance = this;
  }

  public int GetIDbyName(bool input, string n) {
    if (input) {
      for (int i = 0; i < InputDevice.InstalledDevices.Count; i++) {
        if (InputDevice.InstalledDevices[i].Name == n) return i;
      }
    } else {
      for (int i = 0; i < OutputDevice.InstalledDevices.Count; i++) {
        if (OutputDevice.InstalledDevices[i].Name == n) return i;
      }
    }

    return -1;
  }

  public void RefreshDevices(bool input) {
    if (input) {
      inputDevices.Clear();
      for (int i = 0; i < InputDevice.InstalledDevices.Count; i++) {
        inputDevices.Add(new MIDIdevice(InputDevice.InstalledDevices[i], true));
      }
    } else {
      outputDevices.Clear();
      for (int i = 0; i < OutputDevice.InstalledDevices.Count; i++) {
        outputDevices.Add(new MIDIdevice(OutputDevice.InstalledDevices[i], false));
      }
    }
  }

  MIDIdevice getConnectedDevice(DeviceBase d) {
    for (int i = 0; i < connectedInputDevices.Count; i++) {
      if (connectedInputDevices[i].getDevice() == d) {
        return connectedInputDevices[i];
      }
    }

    for (int i = 0; i < connectedOutputDevices.Count; i++) {
      if (connectedOutputDevices[i].getDevice() == d) {
        return connectedOutputDevices[i];
      }
    }

    return null;
  }

  public void Disconnect(MIDIdevice m, bool input, midiComponentInterface _interface = null) {
    if (input) {
      if (!connectedInputDevices.Contains(m)) return;

      if (_interface != null) {
        m.removeInterface(_interface);
        (m.getDevice() as InputDevice).NoteOn -= new InputDevice.NoteOnHandler(_interface.InputNoteOn);
        (m.getDevice() as InputDevice).NoteOff -= new InputDevice.NoteOffHandler(_interface.InputNoteOff);
        (m.getDevice() as InputDevice).ControlChange -= new InputDevice.ControlChangeHandler(_interface.InputControlChange);
      } else {
        for (int i = 0; i < m._interfaceList.Count; i++) {
          (m.getDevice() as InputDevice).NoteOn -= new InputDevice.NoteOnHandler(m._interfaceList[i].InputNoteOn);
          (m.getDevice() as InputDevice).NoteOff -= new InputDevice.NoteOffHandler(m._interfaceList[i].InputNoteOff);
          (m.getDevice() as InputDevice).ControlChange -= new InputDevice.ControlChangeHandler(m._interfaceList[i].InputControlChange);
        }


            (m.getDevice() as InputDevice).StopReceiving();
        (m.getDevice() as InputDevice).Close();
        (m.getDevice() as InputDevice).RemoveAllEventHandlers();
        connectedInputDevices.Remove(m);
      }
    } else {
      if (!connectedOutputDevices.Contains(m)) return;

      if (_interface == null) {
        (m.getDevice() as OutputDevice).Close();
        connectedOutputDevices.Remove(m);
      }
    }
  }

  void OnApplicationQuit() {
    for (int i = connectedInputDevices.Count - 1; i >= 0; i--) {
      Disconnect(connectedInputDevices[i], true);
    }
    for (int i = connectedOutputDevices.Count - 1; i >= 0; i--) {
      Disconnect(connectedOutputDevices[i], false);
    }
  }

  public MIDIdevice Connect(midiComponentInterface _interface, int ID, bool input) {
    DeviceBase d;

    if (input) d = InputDevice.InstalledDevices[ID];
    else d = OutputDevice.InstalledDevices[ID];

    MIDIdevice midOut = getConnectedDevice(d);

    bool success = false;
    if (midOut == null) {
      try {
        if (input) {
          (d as InputDevice).Open();
          (d as InputDevice).StartReceiving(null);
          midOut = new MIDIdevice(d, true);
          connectedInputDevices.Add(midOut);
          success = true;
        } else {
          (d as OutputDevice).Open();
          midOut = new MIDIdevice(d, false);
          connectedOutputDevices.Add(midOut);
          success = true;
        }
      } catch {
        Debug.Log("FAIL...");
      }
    } else success = true;

    if (success) {
      if (input) {
        midOut.addInterface(_interface);
        (d as InputDevice).NoteOn += new InputDevice.NoteOnHandler(_interface.InputNoteOn);
        (d as InputDevice).NoteOff += new InputDevice.NoteOffHandler(_interface.InputNoteOff);
        (d as InputDevice).ControlChange += new InputDevice.ControlChangeHandler(_interface.InputControlChange);
      }
    }

    return midOut;
  }

  public void outputNote(MIDIdevice _dev, bool on, int ID, int channel) {
    if (_dev.input) {
      Debug.Log("problem");
      return;
    }

    if (on) {
      (_dev.getDevice() as OutputDevice).SendNoteOn((Channel)channel, (Pitch)ID, 127);
    } else {
      (_dev.getDevice() as OutputDevice).SendNoteOff((Channel)channel, (Pitch)ID, 127);
    }
  }

  public void outputCC(MIDIdevice _dev, int val, int ID, int channel) {
    if (_dev.input) {
      Debug.Log("problem");
      return;
    }
    Channel c1 = (Channel)channel;
    Control c2 = (Control)ID;
    string s = c2.ToString();
    if (c2.IsValid()) {
      (_dev.getDevice() as OutputDevice).SendControlChange(c1, c2, val);
    } else {
      Debug.Log("problem b");

    }
  }
}


public class MIDIdevice {
  public string name;
  private DeviceBase device;
  public bool input;
  public List<midiComponentInterface> _interfaceList = new List<midiComponentInterface>();

  public MIDIdevice(DeviceBase _dev, bool _input) {
    device = _dev;
    name = _dev.Name;
    input = _input;
  }

  public void addInterface(midiComponentInterface m) {
    if (!_interfaceList.Contains(m)) _interfaceList.Add(m);
  }

  public void removeInterface(midiComponentInterface m) {
    if (_interfaceList.Contains(m)) _interfaceList.Remove(m);
  }

  public DeviceBase getDevice() {
    return device;
  }
};



