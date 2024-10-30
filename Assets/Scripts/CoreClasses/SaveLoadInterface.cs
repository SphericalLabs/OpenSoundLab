// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
// 
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at 
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
// 
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
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
using Mirror;
using System.Reflection;

public class SaveLoadInterface : MonoBehaviour
{
    xmlSaveLoad synthSet;
    public GameObject plugPrefab;
    public static SaveLoadInterface instance;    
    Dictionary<string, GameObject> instrumentPrefabs;

    void Awake()
    {
        instance = this;
        instrumentPrefabs = new Dictionary<string, GameObject>();

        foreach (var devType in DeviceType.GetAll())
        {
            instrumentPrefabs[devType] = Resources.Load("Prefabs/" + devType) as GameObject;
        }

        instrumentPrefabs[DeviceType.TapeGroup] = Resources.Load("Prefabs/" + DeviceType.TapeGroup) as GameObject;
        //instrumentPrefabs[deviceType.Pano] = Resources.Load("Prefabs/" + (deviceType.Pano).ToString()) as GameObject;
    }

    public void Load(string filename, bool tutorial = false)
    {
        synthSet = xmlSaveLoad.LoadFromFile(filename);
        masterControl.instance.currentScene = filename;

        float v = systemLoad(synthSet.SystemList[0]);

        if (v == 0)
        {
            xmlUpdate _xmlUpdate = new xmlUpdate();
            List<InstrumentData> dataB = _xmlUpdate.UpdateFile(filename);
            foreach (InstrumentData dB in dataB)
            {
                GameObject g = Instantiate(instrumentPrefabs[dB.deviceType], Vector3.zero, Quaternion.identity) as GameObject;
                g.GetComponent<deviceInterface>().Load(dB, false);
                //Debug.Log("load data");
                NetworkServer.Spawn(g);
            }
        }
        Transform patchAnchor = GameObject.Find("PatchAnchor").transform;
        int c = synthSet.InstrumentList.Count;
        for (int i = 0; i < c; i++)
        {
            GameObject g = Instantiate(instrumentPrefabs[synthSet.InstrumentList[c - 1 - i].deviceType], patchAnchor) as GameObject;
            g.GetComponent<deviceInterface>().Load(synthSet.InstrumentList[c - 1 - i], false);
            NetworkServer.Spawn(g);
        }

        StartCoroutine(LoadPlugs());
    }

    public bool PreviewLoad(string filename, Transform par)
    {
        synthSet = xmlSaveLoad.LoadFromFile(filename);
        float v = systemLoad(synthSet.SystemList[0], true);

        foreach (InstrumentData data in synthSet.InstrumentList)
        {
            Transform t = (Instantiate(menuManager.instance.refObjects[data.deviceType], par, false) as GameObject).transform;
            t.localPosition = data.position;
            t.localRotation = data.rotation;
            t.localScale = data.scale;
            t.gameObject.SetActive(true);
        }

        ClearSynthSetList();
        return (v != 0);
    }

    void ClearSynthSetList()
    {
        synthSet.InstrumentList.Clear();
        synthSet.PlugList.Clear();
        synthSet.SystemList.Clear();
    }

    void systemSave()
    {
        SystemData s = new SystemData();
        s.version = masterControl.versionNumber;
        s.BPM = masterControl.instance.bpm;
        s.binauralMode = (int)masterControl.instance.BinauralSetting;
        synthSet.SystemList.Add(s);
    }

    float systemLoad(SystemData s, bool justpreview = false)
    {
        if (justpreview) return s.version;

        masterControl.instance.setBPM(s.BPM);
        masterControl.instance.updateBinauralSetting(s.binauralMode);
        metronome.Instance.SetBPM(s.BPM);
        synthSet.SystemList.Add(s);
        return s.version;
    }

    public void Save(string filename)
    {
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
        foreach (omniPlug p in plugs)
        {
            synthSet.PlugList.Add(p.GetData());
        }

        synthSet.SaveToFile(filename);
    }

    IEnumerator LoadPlugs()
    {
        Debug.Log("Load Plugs");
        yield return new WaitForEndOfFrame();
        Dictionary<int, omniPlug> temp = new Dictionary<int, omniPlug>();
        List<PlugData> ResortedPlugList = new List<PlugData>();

        for (int i = 0; i < synthSet.PlugList.Count; i++)
        {
            GameObject g = Instantiate(plugPrefab, synthSet.PlugList[i].position, synthSet.PlugList[i].rotation) as GameObject;
            omniPlug o = g.GetComponent<omniPlug>();
            o.ID = synthSet.PlugList[i].ID;
            o.outputPlug = synthSet.PlugList[i].outputPlug;
            temp[o.ID] = o;

            if (!o.outputPlug) ResortedPlugList.Insert(0, synthSet.PlugList[i]); // sorts the non output plugs to the beginning
            else ResortedPlugList.Add(synthSet.PlugList[i]);
        }

        omniJack[] jacks = FindObjectsOfType(typeof(omniJack)) as omniJack[];

        for (int i = 0; i < ResortedPlugList.Count; i++)
        {
            omniJack targetJack = null;
            for (int i2 = 0; i2 < jacks.Length; i2++)
            {
                if (jacks[i2].ID == ResortedPlugList[i].connected)
                {
                    targetJack = jacks[i2];
                    break;
                }
            }
            if (targetJack == null) // jack not found
            {
                Debug.LogError("NO JACK FOR " + ResortedPlugList[i].connected);
                continue; // just skip invalid plugs and keep on loading the patch
            }

            temp[ResortedPlugList[i].ID].Activate(temp[ResortedPlugList[i].otherPlug], targetJack, ResortedPlugList[i].plugPath, ResortedPlugList[i].cordColor, true);
        }

        ClearSynthSetList();
    }

    public GameObject Copy(GameObject g, manipulator m)
    {
        if (g.GetComponent<deviceInterface>() != null)
        {

            InstrumentData data = g.GetComponent<deviceInterface>().GetData();

            GameObject g2 = Instantiate(instrumentPrefabs[data.deviceType], Vector3.zero, Quaternion.identity) as GameObject;
            deviceInterface device = g2.GetComponent<deviceInterface>();
            device.Load(data, true);

            // set volume to zero to avoid surprisingly loud sounds
            if (device is oscillatorDeviceInterface)
            {
                oscillatorDeviceInterface osc = (oscillatorDeviceInterface)device;
                osc.ampDial.setPercent(0f);
            }

            // set volume to zero to avoid surprisingly loud sounds
            if (device is samplerTwoDeviceInterface)
            {
                samplerTwoDeviceInterface sampler = (samplerTwoDeviceInterface)device;
                sampler.volumeDial.setPercent(0f);
            }

            g2.transform.position = g.transform.position;
            g2.transform.rotation = g.transform.rotation;
            g2.transform.localScale = g.transform.localScale;

            Vector3 v = g.transform.localScale;
            if (v.x < 0) v.x *= -1; // needs mirroring if dragged while copying, because left controller was always mirrored and this copies down to the duplicate
            g2.transform.localScale = v;

            if (m != null)
            {
                if (m.wasGazeBased)
                {
                    g2.transform.parent = GameObject.Find("PatchAnchor").transform;
                }
                m.ForceGrab(g2.GetComponentInChildren<handle>());
            }
            return g2;
        }
        return null;
    }

}


[XmlInclude(typeof(DCData))]
[XmlInclude(typeof(TutorialsData))]
[XmlInclude(typeof(PolarizerData))]
[XmlInclude(typeof(ArtefactData))]
[XmlInclude(typeof(CompressorData))]
[XmlInclude(typeof(FreeverbData))]
[XmlInclude(typeof(DelayData))]
[XmlInclude(typeof(ScopeData))]
[XmlInclude(typeof(QuantizerData))]

[XmlInclude(typeof(ADData))]
[XmlInclude(typeof(SequencerCVData))] // update?
[XmlInclude(typeof(SampleHoldData))]


[XmlInclude(typeof(GlideData))]
[XmlInclude(typeof(GainData))]

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
[XmlInclude(typeof(SamplerTwoData))]
[XmlInclude(typeof(SamplerOneData))]
[XmlInclude(typeof(KeyboardData))]
[XmlInclude(typeof(SequencerData))] // update?
[XmlInclude(typeof(XyloRollData))]
[XmlInclude(typeof(AirhornData))]
[XmlInclude(typeof(PanoData))]
[XmlInclude(typeof(MIDIinData))]
[XmlInclude(typeof(MIDIoutData))]
[XmlInclude(typeof(TimelineData))]
[XmlInclude(typeof(ADSRData))]




public class InstrumentData
{
    public int ID;
    public string deviceType;
    public Vector3 position;
    public Vector3 scale = Vector3.one;
    public Quaternion rotation;
}

public class SystemData
{
    public float BPM;
    public int binauralMode;
    public float version;
}


public class JackData : InstrumentData
{
    public int connected;
    public int homePort;
    public Vector3[] jackPath;
    public Color cordColor;
    public int signalID;
    public int signalClass;
}

public class PlugData : InstrumentData
{
    public bool outputPlug;
    public int connected;
    public int otherPlug;
    public Vector3[] plugPath;
    public Color cordColor;
}