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

using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class menuItem : manipObject
{
    public DeviceType item;
    public string canonicalDeviceId;
    public Material menuMat;
    public Renderer glowRend;
    menuManager manager;
    Material glowMat;

    Texture tex;
    GameObject itemPrefab;
    OSLDeviceRegistration registration;
    GameObject g; // the instance of itemPrefab
    public Renderer symbol;
    TextMesh label;
    Material labelMat;

    bool disabled = false;



    //public enum deviceType {
    //  // Please obey ascending alphabetical order, will define the order to the menuItems
    //  AD,
    //  ADSR,
    //  Airhorn,
    //  Artifact,
    //  DC,
    //  Camera,
    //  Compressor,
    //  ControlCube,
    //  Delay,
    //  Drum,
    //  Filter,
    //  Reverb,
    //  Gain,
    //  Glide,
    //  Keyboard,
    //  Looper,
    //  Maracas,
    //  Microphone,
    //  MIDIIN,
    //  MIDIOUT,
    //  Mixer,
    //  MultiMix,
    //  MultiSplit,
    //  Noise,
    //  Oscillator,
    //  Polarizer,
    //  Quantizer,
    //  Recorder,
    //  Reverb,
    //  SampleHold,
    //  Sampler,
    //  Scope,
    //  Sequencer,
    //  Speaker,
    //  Tapes,
    //  Timeline,
    //  TouchPad,
    //  Tutorials,
    //  VCA,
    //  Xylophone,
    //  // this is a stopper, items below won't appear in menu; alternatively you can skip items in menuManager.loadMenu()
    //  Max,
    //  TapeGroup,
    //  Pano
    //};

    //Color disabledColor;
    //Color normalColor;
    //Color selectColor;

    public void disable(bool on)
    {
        if (disabled == on) return;
        disabled = on;

        GetComponent<Collider>().enabled = !disabled;

    }

    public override void Awake()
    {
        base.Awake();
        //disabledColor = normalColor = selectColor = new Color(0.6f, 0.6f, 0.6f);

        label = GetComponentInChildren<TextMesh>();
        //labelMat = label.GetComponent<Renderer>().material;

        //labelMat.SetColor("_TintColor", normalColor);
        glowRend.gameObject.SetActive(false);
        glowMat = glowRend.material;
    }

    public GameObject Setup(DeviceType d)
    {
        if (!OSLDeviceRegistry.TryGet(d, out OSLDeviceRegistration deviceRegistration))
        {
            Debug.LogWarning("OpenSoundLab: Skipping unknown device " + d + ".");
            disable(true);
            return null;
        }

        return Setup(deviceRegistration);
    }

    public GameObject Setup(OSLDeviceRegistration deviceRegistration)
    {
        if (deviceRegistration == null || !deviceRegistration.IsAvailable)
        {
            disable(true);
            return null;
        }

        registration = deviceRegistration;
        item = registration.deviceType;
        canonicalDeviceId = registration.canonicalDeviceId;
        tex = registration.LoadSymbol();
        if (tex != null) tex.mipMapBias = -1f; // shift mipmap by one level, improves clarity of menu symbols
        symbol.material.SetTexture("_BaseMap", tex);
        itemPrefab = registration.LoadPrefab();
        GameObject menuPrefab = registration.LoadMenuPrefab();

        if (itemPrefab == null || menuPrefab == null)
        {
            Debug.LogWarning("OpenSoundLab: Skipping device " + registration.canonicalDeviceId + " because its prefab or menu prefab is missing.");
            disable(true);
            return null;
        }

        string displayName = registration.displayName;
        label.text = !string.IsNullOrWhiteSpace(displayName) ? displayName : registration.deviceId;

        if (label.text.Contains("\n")) label.fontSize = 52;

        label.gameObject.SetActive(true);
        symbol.gameObject.SetActive(true);
        g = Instantiate(menuPrefab, transform.position, transform.rotation) as GameObject;
        g.transform.parent = transform;


        manager = transform.parent.parent.GetComponent<menuManager>();

        Vector3 size = Vector3.zero;
        Vector3 center = Vector3.zero;
        Renderer[] r = g.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < r.Length; i++)
        {
            //r[i].material = menuMat;
            if (r[i].bounds.size.sqrMagnitude > size.sqrMagnitude)
            {
                size = r[i].bounds.size;
                center = r[i].bounds.center;
            }
        }


        g.tag = "Untagged";
        g.transform.localScale = g.transform.localScale / (size.magnitude * 20);
        g.transform.localPosition = g.transform.localPosition + Vector3.forward * .02f;
        applyPreviewTransform();

        g.SetActive(false);

        return g;
    }

    void applyPreviewTransform()
    {
        if (registration == null || g == null) return;

        g.transform.localPosition += registration.previewPositionOffset;
        if (registration.previewRotationOffset != Vector3.zero)
        {
            g.transform.localRotation = Quaternion.Euler(registration.previewRotationOffset);
        }
        if (registration.previewScale != Vector3.zero && registration.previewScale != Vector3.one)
        {
            g.transform.localScale = registration.previewScale;
        }
    }




    IEnumerator appearRoutine(bool on)
    {
        Vector3 destSize = Vector3.one;
        Vector3 startSize = Vector3.zero;
        glowRend.gameObject.SetActive(true);
        if (!on)
        {
            destSize = Vector3.zero;
            startSize = Vector3.one;
        }

        glowMat.SetColor("_EmissionColor", Color.white);
        float timer = 0;
        while (timer < 1)
        {
            timer = Mathf.Clamp01(timer + Time.deltaTime * 6);
            transform.localScale = Vector3.Lerp(startSize, destSize, timer);
            yield return null;
        }

        timer = 0;
        while (timer < 1)
        {
            timer = Mathf.Clamp01(timer + Time.deltaTime * 6);
            glowMat.SetColor("_EmissionColor", Color.Lerp(Color.white, Color.black, timer));
            yield return null;
        }
        glowRend.gameObject.SetActive(false);
    }

    Coroutine appearCoroutine;
    public void Appear(bool on)
    {

        if (appearCoroutine != null) StopCoroutine(appearCoroutine);
        appearCoroutine = StartCoroutine(appearRoutine(on));
    }

    void createItem()
    {
        // Get the direction from the object to the camera
        Vector3 direction = Camera.main.transform.position - transform.position /*+ Vector3.down * 0.10f*/;

        // Generate a quaternion that looks towards the camera
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        string spawnDeviceId = !string.IsNullOrWhiteSpace(canonicalDeviceId) ? canonicalDeviceId : itemPrefab.name;

        //spawn on network
        if (NetworkSpawnManager.Instance != null)
        {
            Vector3 localPositionOffset = Vector3.zero;
            Vector3 localRotationOffset = Vector3.zero;


            OSLDeviceRegistry.TryGetSpawnOffsets(spawnDeviceId, out localPositionOffset, out localRotationOffset);
            //todo send local player id with it
            if (NetworkManager.singleton.mode == NetworkManagerMode.Host)
            {
                Debug.Log("Spawn on host");
                manipulator manip = manipulatorObj.GetComponent<manipulator>();
                NetworkSpawnManager.Instance.CreateItem(spawnDeviceId, transform.position, lookRotation, localPositionOffset, localRotationOffset, manip);
            }
            else
            {
                Debug.Log("Spawn on client");
                manipulator manip = manipulatorObj.GetComponent<manipulator>();
                NetworkSpawnManager.Instance.CmdCreateItem(spawnDeviceId, transform.position, lookRotation, localPositionOffset, localRotationOffset, NetworkMenuManager.Instance.localPlayer.netIdentity, manip.isLeftController());
            }

        }
        else
        {
            GameObject g = Instantiate(itemPrefab, transform.position /*+ new Vector3(-0f, 0f, -0.04f)*/, lookRotation) as GameObject;

            manipulator manip = manipulatorObj.GetComponent<manipulator>();

            // spawn directly into the hand if selecting by gaze
            if (manip != null && manip.wasGazeBased)
            {
                g.transform.position = manip.transform.position + manip.transform.forward * 0.12f;
                manip.wasGazeBased = false; // treat that interaction as a physical one from now on, otherwise it would be handled in fine mode by default
            }

            Vector3 localPositionOffset;
            Vector3 localRotationOffset;
            if (OSLDeviceRegistry.TryGetSpawnOffsets(spawnDeviceId, out localPositionOffset, out localRotationOffset))
            {
                g.transform.Translate(localPositionOffset, Space.Self);
                g.transform.Rotate(localRotationOffset, Space.Self);
            }


            //else if (item != deviceType.Filter && item != deviceType.Scope && item != deviceType.Airhorn && item != deviceType.ADSR) /*g.transform.Rotate(0, 180, 0, Space.Self);*/


            if (manip != null && manip.wasGazeBased) g.transform.parent = GameObject.Find("PatchAnchor").transform;  // Directly inject PatchAnchor as parent, since the normal grab and then place back routine is skipped when spawn by gaze
            manip.ForceGrab(g.GetComponentInChildren<handle>());
        }

    }

    public override void setState(manipState state)
    {
        curState = state;

        if (disabled) return;

        if (curState == manipState.none)
        {
            label.gameObject.SetActive(true);
            symbol.gameObject.SetActive(true);
            g.SetActive(false);
        }
        else if (curState == manipState.selected)
        {
            //symbol.material.SetColor("_TintColor", normalColor);
            //labelMat.SetColor("_TintColor", normalColor);
            label.gameObject.SetActive(true);
            symbol.gameObject.SetActive(true);
            g.SetActive(true);
            manager.SelectAudio();
        }
        else if (curState == manipState.grabbed)
        {
            //symbol.material.SetColor("_TintColor", selectColor);
            label.gameObject.SetActive(true);
            symbol.gameObject.SetActive(true);
            g.SetActive(true);
            //labelMat.SetColor("_TintColor", selectColor);
            manager.GrabAudio();
            StartCoroutine(flash());
            createItem();
        }
    }

    IEnumerator flash()
    {
        float t = 0;
        glowRend.gameObject.SetActive(true);
        while (t < 1)
        {
            t = Mathf.Clamp01(t + Time.deltaTime * 3);
            glowMat.SetFloat("_EmissionGain", Mathf.Lerp(.6f, .2f, t));
            glowMat.SetColor("_EmissionColor", Color.Lerp(Color.white, Color.black, t));
            yield return null;
        }
        glowRend.gameObject.SetActive(false);
    }

}
