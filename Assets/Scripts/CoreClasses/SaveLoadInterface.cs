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
using System;
using System.Xml.Serialization;

public class SaveLoadInterface : MonoBehaviour {
  xmlSaveLoad synthSet;
  public GameObject plugPrefab;
  public static SaveLoadInterface instance;
  public metronome nome;
  Dictionary<menuItem.deviceType, GameObject> instrumentPrefabs;

  void Awake() {
    instance = this;
    instrumentPrefabs = new Dictionary<menuItem.deviceType, GameObject>();

    for (int i = 0; i < (int)menuItem.deviceType.Max; i++) {
      instrumentPrefabs[(menuItem.deviceType)i] = Resources.Load("Prefabs/" + ((menuItem.deviceType)i).ToString()) as GameObject;
    }

    instrumentPrefabs[menuItem.deviceType.TapeGroup] = Resources.Load("Prefabs/" + (menuItem.deviceType.TapeGroup).ToString()) as GameObject;
    instrumentPrefabs[menuItem.deviceType.Pano] = Resources.Load("Prefabs/" + (menuItem.deviceType.Pano).ToString()) as GameObject;
  }

  public void Load(string filename, bool tutorial = false) {
    synthSet = xmlSaveLoad.LoadFromFile(filename);
    masterControl.instance.currentScene = filename;

    float v = systemLoad(synthSet.SystemList[0]);

    if (v == 0) {
      xmlUpdate _xmlUpdate = new xmlUpdate();
      List<InstrumentData> dataB = _xmlUpdate.UpdateFile(filename);
      foreach (InstrumentData dB in dataB) {
        GameObject g = Instantiate(instrumentPrefabs[dB.deviceType], Vector3.zero, Quaternion.identity) as GameObject;
        g.GetComponent<deviceInterface>().Load(dB);
      }
    }
    Transform patchAnchor = GameObject.Find("PatchAnchor").transform;
    int c = synthSet.InstrumentList.Count;
    for (int i = 0; i < c; i++) {
      GameObject g = Instantiate(instrumentPrefabs[synthSet.InstrumentList[c - 1 - i].deviceType], patchAnchor /*Vector3.zero, Quaternion.identity*/) as GameObject;
      g.GetComponent<deviceInterface>().Load(synthSet.InstrumentList[c - 1 - i]);
    }

    LoadPlugs();
    ClearSynthSetList();
  }

  public bool PreviewLoad(string filename, Transform par) {
    synthSet = xmlSaveLoad.LoadFromFile(filename);
    float v = systemLoad(synthSet.SystemList[0], true);

    foreach (InstrumentData data in synthSet.InstrumentList) {
      Transform t = (Instantiate(menuManager.instance.refObjects[data.deviceType], par, false) as GameObject).transform;
      t.localPosition = data.position;
      t.localRotation = data.rotation;
      t.localScale = data.scale;
      t.gameObject.SetActive(true);
    }

    ClearSynthSetList();
    return (v != 0);
  }

  void ClearSynthSetList() {
    synthSet.InstrumentList.Clear();
    synthSet.PlugList.Clear();
    synthSet.SystemList.Clear();
  }

  void systemSave() {
    SystemData s = new SystemData();
    s.version = masterControl.versionNumber;
    s.BPM = masterControl.instance.bpm;
    s.binauralMode = (int)masterControl.BinauralSetting;
    synthSet.SystemList.Add(s);
  }

  float systemLoad(SystemData s, bool justpreview = false) {
    if (justpreview) return s.version;

    masterControl.instance.setBPM(s.BPM);
    masterControl.updateBinaural(s.binauralMode);
    nome.SetBPM(s.BPM);
    synthSet.SystemList.Add(s);
    return s.version;
  }

  public void Save(string filename) {
    masterControl.instance.currentScene = filename;

    if (synthSet == null) synthSet = new xmlSaveLoad();
    ClearSynthSetList();

    systemSave();
    deviceInterface[] devices = FindObjectsOfType(typeof(deviceInterface)) as deviceInterface[];
    foreach (deviceInterface d in devices)
    {
      if (d is tutorialsDeviceInterface) continue; // exclude Tutorials from saves
      synthSet.InstrumentList.Add(d.GetData());
    }

    omniPlug[] plugs = FindObjectsOfType(typeof(omniPlug)) as omniPlug[];
    foreach (omniPlug p in plugs) {
      synthSet.PlugList.Add(p.GetData());
    }

    synthSet.SaveToFile(filename);
  }

  void LoadPlugs() {
    Dictionary<int, omniPlug> temp = new Dictionary<int, omniPlug>();
    List<PlugData> ResortedPlugList = new List<PlugData>();

    for (int i = 0; i < synthSet.PlugList.Count; i++) {
      GameObject g = Instantiate(plugPrefab, synthSet.PlugList[i].position, synthSet.PlugList[i].rotation) as GameObject;
      omniPlug o = g.GetComponent<omniPlug>();
      o.ID = synthSet.PlugList[i].ID;
      o.outputPlug = synthSet.PlugList[i].outputPlug;
      temp[o.ID] = o;

      if (!o.outputPlug) ResortedPlugList.Insert(0, synthSet.PlugList[i]);
      else ResortedPlugList.Add(synthSet.PlugList[i]);
    }

    omniJack[] jacks = FindObjectsOfType(typeof(omniJack)) as omniJack[];

    for (int i = 0; i < ResortedPlugList.Count; i++) {
      omniJack targetJack = null;
      for (int i2 = 0; i2 < jacks.Length; i2++) {
        if (jacks[i2].ID == ResortedPlugList[i].connected) {
          targetJack = jacks[i2];
          break;
        }
      }
      if (targetJack == null) Debug.LogError("NO JACK FOR " + ResortedPlugList[i].connected);
      temp[ResortedPlugList[i].ID].Activate(temp[ResortedPlugList[i].otherPlug], targetJack, ResortedPlugList[i].plugPath, ResortedPlugList[i].cordColor);
    }
  }

  public void Copy(GameObject g, manipulator m) {
    if (g.GetComponent<deviceInterface>() != null) {
      InstrumentData data = g.GetComponent<deviceInterface>().GetData();

      GameObject g2 = Instantiate(instrumentPrefabs[data.deviceType], Vector3.zero, Quaternion.identity) as GameObject;
      deviceInterface device = g2.GetComponent<deviceInterface>();
      device.Load(data);

      if (device is oscillatorDeviceInterface) {
        oscillatorDeviceInterface osc = (oscillatorDeviceInterface) device;
        osc.ampDial.setPercent(0f);
      }

      if (device is samplerDeviceInterface)
      {
        samplerDeviceInterface sampler = (samplerDeviceInterface)device;
        sampler.volumeDial.setPercent(0f);
      }

      g2.transform.position = g.transform.position;
      g2.transform.rotation = g.transform.rotation;
      g2.transform.localScale = g.transform.localScale;

      Vector3 v = g.transform.localScale;
      if(v.x < 0) v.x *= -1; // needs mirroring if dragged while copyijg, because left controller was always mirrored and this copies down to the duplicate
      g2.transform.localScale = v;
      
      m.ForceGrab(g2.GetComponentInChildren<handle>());
    }
  }

}


[XmlInclude(typeof(DCData))]
[XmlInclude(typeof(TutorialsData))]
[XmlInclude(typeof(PolarizerData))]
[XmlInclude(typeof(ArtefactData))]
[XmlInclude(typeof(CompressorData))]
[XmlInclude(typeof(StereoVerbData))]
[XmlInclude(typeof(DelayData))]
[XmlInclude(typeof(ScopeData))]
[XmlInclude(typeof(QuantizerData))] 

[XmlInclude(typeof(ADData))]
  [XmlInclude(typeof(SequencerCVData))] // update?
  [XmlInclude(typeof(SampleHoldData))]

  
  [XmlInclude(typeof(GlideData))]
  [XmlInclude(typeof(GainData))]
  [XmlInclude(typeof(SARSCov2Data))]
  [XmlInclude(typeof(FunktionData))]

[XmlInclude(typeof(OscillatorData))]
[XmlInclude(typeof(SpeakerData))]
[XmlInclude(typeof(CameraData))]
[XmlInclude(typeof(ControlCubeData))]
[XmlInclude(typeof(DrumData))]
[XmlInclude(typeof(NoiseData))]
[XmlInclude(typeof(FilterData))]
[XmlInclude(typeof(MaracaData))]
[XmlInclude(typeof(MicrophoneData))]
[XmlInclude(typeof(TouchPadData))]
[XmlInclude(typeof(vcaData))]
[XmlInclude(typeof(ReverbData))]
[XmlInclude(typeof(TapeGroupData))]
[XmlInclude(typeof(MixerData))]
[XmlInclude(typeof(MultipleData))]

[XmlInclude(typeof(LooperData))]
[XmlInclude(typeof(RecorderData))]
[XmlInclude(typeof(SamplerData))]
[XmlInclude(typeof(KeyboardData))]
[XmlInclude(typeof(SequencerData))] // update?
[XmlInclude(typeof(XyloRollData))]
[XmlInclude(typeof(AirhornData))]
[XmlInclude(typeof(PanoData))]
[XmlInclude(typeof(MIDIinData))]
[XmlInclude(typeof(MIDIoutData))]
[XmlInclude(typeof(TimelineData))]
[XmlInclude(typeof(ADSRData))]

 


public class InstrumentData {
  public int ID;
  public menuItem.deviceType deviceType;
  public Vector3 position;
  public Vector3 scale = Vector3.one;
  public Quaternion rotation;
}

public class SystemData {
  public float BPM;
  public int binauralMode;
  public float version;
}


public class JackData : InstrumentData {
  public int connected;
  public int homePort;
  public Vector3[] jackPath;
  public Color cordColor;
  public int signalID;
  public int signalClass;
}

public class PlugData : InstrumentData {
  public bool outputPlug;
  public int connected;
  public int otherPlug;
  public Vector3[] plugPath;
  public Color cordColor;
}