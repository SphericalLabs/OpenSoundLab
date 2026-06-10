// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2026 OSLLv1 Sphericals OpenSoundLab
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
using Mirror;

public class SaveLoadInterface : MonoBehaviour
{
    const float patchLoadSettleSeconds = 0.6f;

    xmlSaveLoad synthSet;
    public GameObject plugPrefab;
    public static SaveLoadInterface instance;
    Dictionary<string, GameObject> instrumentPrefabs;
    int patchLoadSequence;

    void Awake()
    {
        instance = this;
        instrumentPrefabs = new Dictionary<string, GameObject>();

        foreach (OSLDeviceRegistration registration in OSLDeviceRegistry.GetAll())
        {
            cacheInstrumentPrefab(registration);
        }
    }

    void cacheInstrumentPrefab(OSLDeviceRegistration registration)
    {
        if (registration == null || registration.deviceType == null) return;
        if (!registration.IsAvailable) return;

        GameObject prefab = registration.LoadPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("OpenSoundLab: No prefab registered for device type " + registration.deviceId + ".");
            return;
        }

        instrumentPrefabs[registration.deviceId] = prefab;

        if (registration.legacyDeviceIds == null) return;
        for (int i = 0; i < registration.legacyDeviceIds.Length; ++i)
        {
            string legacyDeviceId = registration.legacyDeviceIds[i];
            if (!string.IsNullOrWhiteSpace(legacyDeviceId)) instrumentPrefabs[legacyDeviceId] = prefab;
        }
    }

    public void Load(string filename, bool tutorial = false)
    {
        preparePatchAnchorForPersistence();
        synthSet = xmlSaveLoad.LoadFromFile(filename);
        masterControl.instance.currentScene = filename;
        int loadSequence = ++patchLoadSequence;

        updatePatchAudioMute(true);
        try
        {
            float v = systemLoad(synthSet.SystemList[0]);
            bool usedLegacyLoad = false;

            if (v == 0)
            {
                xmlUpdate _xmlUpdate = new xmlUpdate();
                List<InstrumentData> dataB = _xmlUpdate.UpdateFile(filename);
                foreach (InstrumentData dB in dataB)
                {
                    dB.deviceType = normalizeDeviceTypeName(dB.deviceType);
                    if (!tryGetInstrumentPrefab(dB.deviceType, out GameObject prefab)) continue;
                    GameObject g = Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
                    g.GetComponent<deviceInterface>().Load(dB, false);
                    //Debug.Log("load data");
                    NetworkServer.Spawn(g);
                }
                usedLegacyLoad = dataB.Count > 0;
            }
            Transform patchAnchor = GameObject.Find("PatchAnchor").transform;
            int c = synthSet.InstrumentList.Count;
            for (int i = 0; i < c && !usedLegacyLoad; i++)
            {
                synthSet.InstrumentList[c - 1 - i].deviceType = normalizeDeviceTypeName(synthSet.InstrumentList[c - 1 - i].deviceType);
                if (!tryGetInstrumentPrefab(synthSet.InstrumentList[c - 1 - i].deviceType, out GameObject prefab)) continue;
                GameObject g = Instantiate(prefab, patchAnchor) as GameObject;
                g.GetComponent<deviceInterface>().Load(synthSet.InstrumentList[c - 1 - i], false);
                NetworkServer.Spawn(g);
            }

            StartCoroutine(LoadPlugs(loadSequence));
        }
        catch
        {
            if (loadSequence == patchLoadSequence)
            {
                updatePatchAudioMute(false);
            }
            throw;
        }
    }

    public bool PreviewLoad(string filename, Transform par)
    {
        synthSet = xmlSaveLoad.LoadFromFile(filename);
        float v = systemLoad(synthSet.SystemList[0], true);

        foreach (InstrumentData data in synthSet.InstrumentList)
        {
            data.deviceType = normalizeDeviceTypeName(data.deviceType);
            if (!tryGetPreviewPrefab(data.deviceType, out GameObject previewPrefab)) continue;

            Transform t = (Instantiate(previewPrefab, par, false) as GameObject).transform;
            t.localPosition = data.position;
            t.localRotation = data.rotation;
            t.localScale = data.scale;
            t.gameObject.SetActive(true);
        }

        ClearSynthSetList();
        return (v != 0);
    }

    bool tryGetPreviewPrefab(string deviceType, out GameObject previewPrefab)
    {
        if (menuManager.instance != null && menuManager.instance.refObjects != null && menuManager.instance.refObjects.TryGetValue(deviceType, out previewPrefab))
        {
            return true;
        }

        if (OSLDeviceRegistry.TryGet(deviceType, out OSLDeviceRegistration registration))
        {
            if (!registration.IsAvailable)
            {
                Debug.LogWarning("OpenSoundLab: Skipping preview for unavailable device type " + registration.deviceId + " (" + registration.Access.availability + ").");
                previewPrefab = null;
                return false;
            }

            Debug.LogWarning("OpenSoundLab: Skipping preview for device type " + registration.deviceId + " because no preview object is registered.");
            previewPrefab = null;
            return false;
        }

        Debug.LogWarning("OpenSoundLab: Skipping preview for missing device type " + deviceType + ".");
        previewPrefab = null;
        return false;
    }

    bool tryGetInstrumentPrefab(string deviceType, out GameObject prefab)
    {
        if (instrumentPrefabs.TryGetValue(deviceType, out prefab) && prefab != null) return true;

        if (OSLDeviceRegistry.TryGet(deviceType, out OSLDeviceRegistration registration) && !registration.IsAvailable)
        {
            prefab = null;
            Debug.LogWarning("OpenSoundLab: Skipping unavailable device type " + registration.deviceId + " (" + registration.Access.availability + "). Stage 8.5 will preserve it as a ghost.");
            return false;
        }

        prefab = OSLDeviceRegistry.GetPrefab(deviceType);
        if (prefab != null)
        {
            instrumentPrefabs[deviceType] = prefab;
            return true;
        }

        Debug.LogWarning("OpenSoundLab: Skipping missing device type " + deviceType + ". Install or enable its addon to load it.");
        return false;
    }

    public void StartNewPatch()
    {
        masterControl.instance.currentScene = "";
        ClearInstruments();
    }

    public void ClearInstruments()
    {
        preparePatchAnchorForPersistence();

        GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("instrument");
        for (int i = 0; i < gameObjects.Length; i++)
        {
            if (gameObjects[i].name == "Tutorials(Clone)") continue; // Tutorials stays persistent
            Destroy(gameObjects[i]);
        }

        if (masterControl.instance.examplesOn) masterControl.instance.toggleExamples();
    }

    void ClearSynthSetList()
    {
        synthSet.InstrumentList.Clear();
        synthSet.PlugList.Clear();
        synthSet.SystemList.Clear();
    }

    string normalizeDeviceTypeName(string deviceType)
    {
        if (OSLDeviceRegistry.TryNormalizeDeviceId(deviceType, out string normalizedDeviceId)) return normalizedDeviceId;
        return deviceType;
    }

    string getCanonicalDeviceIdForSave(string deviceType)
    {
        if (OSLDeviceRegistry.TryGetCanonicalDeviceId(deviceType, out string canonicalDeviceId)) return canonicalDeviceId;
        return deviceType;
    }

    void systemSave()
    {
        SystemData s = new SystemData();
        s.version = masterControl.versionNumber;
        s.saveSchema = "OSL-XML-1";
        s.deviceIdentityFormat = "canonical";
        s.binauralMode = (int)masterControl.instance.BinauralSetting;
        s.wireSetting = (int)masterControl.instance.WireSetting;
        synthSet.SystemList.Add(s);
    }

    float systemLoad(SystemData s, bool justpreview = false)
    {
        if (justpreview) return s.version;

        masterControl.instance.updateBinauralSetting(s.binauralMode);

        if (s.wireSetting == 0) s.wireSetting = (int)WireMode.Straight;
        masterControl.instance.updateWireSetting(s.wireSetting);

        synthSet.SystemList.Add(s);
        return s.version;
    }

    public void Save(string filename)
    {
        masterControl.instance.currentScene = filename;
        preparePatchAnchorForPersistence();

        if (synthSet == null) synthSet = new xmlSaveLoad();
        ClearSynthSetList();

        systemSave();
        deviceInterface[] devices = FindObjectsOfType(typeof(deviceInterface)) as deviceInterface[];
        foreach (deviceInterface d in devices)
        {
            if (d is tutorialsDeviceInterface) continue; // exclude Tutorials from saves
            InstrumentData data = d.GetData();
            data.deviceType = getCanonicalDeviceIdForSave(data.deviceType);
            synthSet.InstrumentList.Add(data);
        }

        omniPlug[] plugs = FindObjectsOfType(typeof(omniPlug)) as omniPlug[];
        foreach (omniPlug p in plugs)
        {
            synthSet.PlugList.Add(p.GetData());
        }

        synthSet.SaveToFile(filename);
    }

    void preparePatchAnchorForPersistence()
    {
        if (WorldDragController.Instance != null)
        {
            // World drag temporarily stores transform on PatchAnchor. Normalize
            // that first so save/load/new patch operate on baked child transforms.
            WorldDragController.Instance.PrepareForPersistence();
        }
    }

    IEnumerator LoadPlugs(int loadSequence)
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
        yield return new WaitForSecondsRealtime(patchLoadSettleSeconds);

        if (loadSequence == patchLoadSequence)
        {
            updatePatchAudioMute(false);
        }
    }

    void updatePatchAudioMute(bool muted)
    {
        if (NetworkServer.active && NetworkMasterControl.Instance != null)
        {
            NetworkMasterControl.Instance.SetPatchAudioMuted(muted);
            return;
        }

        speaker.SetPatchLoadMuted(muted);
    }

    public GameObject Copy(GameObject g, manipulator m)
    {
        if (g.GetComponent<deviceInterface>() != null)
        {

            InstrumentData data = g.GetComponent<deviceInterface>().GetData();

            string normalizedType = normalizeDeviceTypeName(data.deviceType);
            if (!tryGetInstrumentPrefab(normalizedType, out GameObject prefab)) return null;
            GameObject g2 = Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
            deviceInterface device = g2.GetComponent<deviceInterface>();
            device.Load(data, true);

            g2.transform.position = g.transform.position;
            g2.transform.rotation = g.transform.rotation;
            Vector3 v = g.transform.lossyScale;
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
