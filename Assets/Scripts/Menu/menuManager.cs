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
using UnityEngine.Analytics;
using System;
using System.Linq;
using OVR.OpenVR;

public class menuManager : MonoBehaviour
{
    public GameObject item;
    public GameObject rootNode;
    public GameObject trashNode;
    public GameObject settingsNode;
    public GameObject performanceNode;
    public GameObject recorderNode;

    public List<GameObject> menuItems;

    public Dictionary<string, GameObject> refObjects;

    public AudioSource _audioSource;
    public AudioClip openClip;
    public AudioClip closeClip;
    public AudioClip selectClip;
    public AudioClip grabClip;
    public AudioClip simpleOpenClip;

    List<menuItem> menuItemScripts;
    public static menuManager instance;

    bool active = false;
    int lastController = -1;

    public bool loaded = false;

    void Awake()
    {
        instance = this;
        OSLDeviceRegistry.GetAll();
        refObjects = new Dictionary<string, GameObject>();
        _audioSource = GetComponent<AudioSource>();
        loadMenu();
        loadNonMenuItems();
        cacheLegacyRefObjectAliases();
        loaded = true;
        Activate(false, transform);

        if (!PlayerPrefs.HasKey("midiOut")) PlayerPrefs.SetInt("midiOut", 0);
        if (PlayerPrefs.GetInt("midiOut") == 1)
        {
            toggleMidiOut(true);
        }
    }

    void Start()
    {
    }

    IEnumerator delayedActivate(bool on, Transform trans, float sec)
    {
        yield return new WaitForSecondsRealtime(sec);
        Activate(on, trans);
    }

    void loadNonMenuItems()
    {
        GameObject temp = Instantiate(item, Vector3.zero, Quaternion.identity) as GameObject;
        temp.transform.parent = rootNode.transform;
        menuItem m = temp.GetComponent<menuItem>();
        GameObject refObject = m.Setup(DeviceType.Get("io.sphericals.osl.core.various.TapeGroup"));
        if (refObject != null) refObjects[DeviceType.Get("io.sphericals.osl.core.various.TapeGroup")] = refObject;
        temp.SetActive(false);

        //temp = Instantiate(item, Vector3.zero, Quaternion.identity) as GameObject;
        //temp.transform.parent = rootNode.transform;
        //m = temp.GetComponent<menuItem>();
        //refObjects[deviceType.Pano] = m.Setup(deviceType.Pano);
        //temp.SetActive(false);
    }

    public void SetMenuActive(bool on)
    {
        active = on;
    }



    // this populates the menu on program start
    void loadMenu()
    {
        menuItems = new List<GameObject>();
        menuItemScripts = new List<menuItem>();

        int hElements = 8;
        float arc = -8.4f * hElements;
        float arcSegment = arc / hElements;

        float y;
        int x = 0;
        int tempCount = 0;

        IEnumerable<DeviceCategory> categories = Enum.GetValues(typeof(DeviceCategory)).OfType<DeviceCategory>();
        foreach (DeviceCategory category in categories)
        {

            y = 0;

            foreach (OSLDeviceRegistration registration in OSLDeviceRegistry.GetAllByCategory(category))
            {
                if (registration == null || !registration.showInMenu || !registration.IsAvailable) continue;

                GameObject tmpObj = Instantiate(item, Vector3.zero, Quaternion.identity) as GameObject;
                tmpObj.transform.parent = rootNode.transform;
                menuItems.Add(tmpObj);
                menuItem m = tmpObj.GetComponent<menuItem>();
                GameObject refObject = m.Setup(registration);
                if (refObject == null)
                {
                    menuItems.Remove(tmpObj);
                    Destroy(tmpObj);
                    continue;
                }

                cacheRefObject(registration, refObject);
                menuItemScripts.Add(m);



                float angle = arcSegment * (x - (categories.Count() - 1) / 2f);
                Quaternion rotation = Quaternion.Euler(0, angle, 0);
                Vector3 positionOffset = rotation * Vector3.forward * -0.5f - Vector3.forward * -0.5f;
                menuItems[tempCount].transform.localPosition = positionOffset + Vector3.up * y * 0.07f;
                menuItems[tempCount].transform.rotation = rotation;

                tempCount++;
                y++;

            }

            x++;

        }

        performanceNode.transform.localPosition = new Vector3(0.329f, 0.012f - 0.12f + 0.10f, 0.107f);
        performanceNode.transform.rotation = Quaternion.Euler(-0.529f, -40.157f, -0.7460001f);

        settingsNode.transform.localPosition = new Vector3(-0.344f, -0.001f, 0.171f);
        settingsNode.transform.rotation = Quaternion.Euler(-0.422f, 40.013f, 0.576f);

        recorderNode.transform.localPosition = new Vector3(0.344f, -0.001f, 0.171f);
        recorderNode.transform.rotation = Quaternion.Euler(-0.422f, -40.013f, -0.576f);
    }

    void cacheRefObject(OSLDeviceRegistration registration, GameObject refObject)
    {
        if (registration == null || refObject == null) return;

        if (!string.IsNullOrWhiteSpace(registration.deviceId)) refObjects[registration.deviceId] = refObject;
        if (!string.IsNullOrWhiteSpace(registration.deviceLocalId)) refObjects[registration.deviceLocalId] = refObject;
        if (!string.IsNullOrWhiteSpace(registration.canonicalDeviceId)) refObjects[registration.canonicalDeviceId] = refObject;
        if (registration.deviceType != null) refObjects[registration.deviceType] = refObject;
    }

    void cacheLegacyRefObjectAliases()
    {
        foreach (OSLDeviceRegistration registration in OSLDeviceRegistry.GetAll())
        {
            if (registration == null || registration.legacyDeviceIds == null) continue;
            if (!refObjects.TryGetValue(registration.deviceId, out GameObject refObject) &&
                !refObjects.TryGetValue(registration.canonicalDeviceId, out refObject)) continue;

            for (int i = 0; i < registration.legacyDeviceIds.Length; ++i)
            {
                string legacyDeviceId = registration.legacyDeviceIds[i];
                if (!string.IsNullOrWhiteSpace(legacyDeviceId)) refObjects[legacyDeviceId] = refObject;
            }
        }
    }

    public void SelectAudio()
    {
        //_audioSource.PlayOneShot(selectClip, .05f);
    }

    public void GrabAudio()
    {
        //_audioSource.PlayOneShot(grabClip, .75f);
    }

    public bool midiOutEnabled = false;
    float openSpeed = 3;
    public void toggleMidiOut(bool on)
    {
        PlayerPrefs.SetInt("midiOut", on ? 1 : 0);
        midiOutEnabled = on;
        openSpeed = on ? 2 : 3;
        menuItemScripts[menuItemScripts.Count - 1].Appear(on);
    }

    Coroutine activationCoroutine;
    IEnumerator activationRoutine(bool on, Transform pad)
    {

        if (on)
        {
            rootNode.SetActive(true);
            trashNode.SetActive(true);
            settingsNode.SetActive(true);
            recorderNode.SetActive(true);
            for (int i = 0; i < menuItems.Count; i++)
            {
                menuItemScripts[i].Appear(on);
                //menuItemScripts[i].transform.localScale = Vector3.one;
            }
            Transform manip = pad.Find("manipCollViz");
            transform.position = manip != null ? manip.position : Vector3.zero;

            faceCenterEye();

        }
        else
        {
            trashNode.SetActive(false);
            settingsNode.SetActive(false);
            recorderNode.SetActive(false);
            for (int i = 0; i < menuItems.Count; i++)
            {
                menuItemScripts[i].Appear(on);
            }
            rootNode.SetActive(false);
        }
        yield return null;
    }

    GameObject centerEyeAnchor;
    Vector3 centerEye, lookDirection;

    void faceCenterEye()
    {
        if (centerEyeAnchor == null) centerEyeAnchor = GameObject.Find("CenterEyeAnchor");

        centerEye = centerEyeAnchor.transform.position;
        centerEye.y -= 0.1f;

        //transform.LookAt(centerEye, Vector3.up);

        // needed Quaternion to avoid gimbal locks when spawning above or below
        lookDirection = centerEye - transform.position;
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }
    }

    void Activate(bool on, Transform pad)
    {
        active = on;
        if (activationCoroutine != null) StopCoroutine(activationCoroutine);
        activationCoroutine = StartCoroutine(activationRoutine(on, pad));
    }

    void SimpleActivate(bool on, Transform pad)
    {
        active = on;
        simpleMenu.toggleMenu();

        //if (on) _audioSource.PlayOneShot(simpleOpenClip);
        //else _audioSource.PlayOneShot(closeClip);
        if (!active) return;

        transform.position = pad.position;

        faceCenterEye();
    }

    public bool simple = false;
    public pauseMenu simpleMenu;

    public bool buttonEvent(int controller, Transform pad)
    {
        bool on = true;

        if (controller != lastController)
        { // this logic switches between the two controllers
            if (!simple) Activate(true, pad);
            else SimpleActivate(true, pad);
        }
        else
        {
            if (!simple) Activate(!active, pad);
            else SimpleActivate(!active, pad);
            on = active;
        }

        lastController = controller;
        return on;
    }
}
