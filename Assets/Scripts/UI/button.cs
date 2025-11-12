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
using UnityEngine.Events;

public class button : manipObject
{
    public bool isSwitch = false;
    public bool toggleKey = false;
    public int buttonID;
    public int[] button2DID = new int[] { 0, 0 };
    public bool glowMatOnToggle = true;
    public Material onMat;
    public Material highlightMat;

    Renderer rend;
    Material offMat;
    Material glowMat;
    public componentInterface _componentInterface;
    public GameObject selectOverlay;
    public float glowHue = 0;
    Color glowColor = Color.HSVToRGB(0, .5f, .25f);
    Color offColor;

    public bool onlyOn = false;

    bool singleID = true;

    Renderer labelRend;
    public Color labelColor = new Color(0.75f, .75f, 1f);
    public float labelEmission = .0f;
    public float glowEmission = .3f;

    Queue<bool> hits = new Queue<bool>();
    public bool startToggled = false;
    public bool disregardStartToggled = false;

    public bool changeOverlayGlow = false;

    public UnityEvent onToggleChangedEvent;

    public override void Awake()
    {
        base.Awake();
        toggleKey = false;
        glowColor = Color.HSVToRGB(glowHue, .5f, .25f);

        if (_componentInterface == null)
        {
            if (transform.parent) _componentInterface = transform.parent.GetComponent<componentInterface>();
        }

        rend = GetComponent<Renderer>();
        offMat = rend.material;
        //offColor = offMat.GetColor("_Color");
        glowMat = new Material(onMat);
        glowMat.SetFloat("_EmissionGain", glowEmission);
        glowMat.SetColor("_TintColor", glowColor);
        selectOverlay.SetActive(false);

        if (changeOverlayGlow)
        {
            selectOverlay.GetComponent<Renderer>().material.SetColor("_TintColor", glowColor);
            selectOverlay.GetComponent<Renderer>().material.SetFloat("_EmissionGain", glowEmission);
        }

        if (GetComponentInChildren<TextMesh>() != null) labelRend = GetComponentInChildren<TextMesh>().transform.GetComponent<Renderer>();
        if (labelRend != null)
        {
            labelRend.material.SetFloat("_EmissionGain", .1f);
            labelRend.material.SetColor("_TintColor", labelColor);
        }

        if (disregardStartToggled) return;
        keyHit(startToggled, true);
    }

    void Start()
    {

    }

    // used for sequencer highlighting
    public void Highlight(bool on)
    {
        if (on)
        {
            rend.sharedMaterial = isHit ? glowMat : highlightMat;
            //glowMat.SetColor("_TintColor", new Color(1f, 1f, 1f));
            //offMat.SetColor("_Color", glowColor);
        }
        else
        {
            rend.sharedMaterial = isHit ? glowMat : offMat;
            //glowMat.SetColor("_TintColor", new Color(0.3f, 0.3f, 0.3f));
            //offMat.SetColor("_Color", offColor);
        }
    }

    public void Setup(int IDx, int IDy, bool on, Color c)
    {
        singleID = false;

        if (_componentInterface == null)
        {
            if (transform.parent.GetComponent<componentInterface>() != null) _componentInterface = transform.parent.GetComponent<componentInterface>();
            else _componentInterface = transform.parent.parent.GetComponent<componentInterface>();
        }
        button2DID[0] = IDx;
        button2DID[1] = IDy;
        glowColor = c;
        keyHit(on, true);
        startToggled = on;
        glowMat.SetColor("_TintColor", glowColor);
    }

    public void setOnAtStart(bool on)
    {
        keyHit(on, true);
        startToggled = on;
    }

    public void phantomHit(bool on)
    {
        hits.Enqueue(on);
    }

    bool flashOnQueued = false;
    bool flashOffQueued = false;
    int flashOffTimer = 0;
    private readonly object flashLock = new object(); // Lock object

    public void queueFlash()
    {
        lock (flashLock) // called from audio thread and then locks main thread
        {
            flashOnQueued = true;
            flashOffQueued = false;
            flashOffTimer = 0;
        }
    }

    void Update()
    {
        // this old shool mechanism allows for resetting in a better way then with coroutine
        if (flashOffQueued)
        {
            if (flashOffTimer > 4)
            {
                setDark();
                flashOffQueued = false;
                flashOnQueued = false;
                flashOffTimer = 0;
            }
            flashOffTimer++;
        }

        // single frame flashes queued from audio thread
        if (flashOnQueued)
        {
            setBright();
            flashOnQueued = false;
            flashOffQueued = true;
        }

        for (int i = 0; i < hits.Count; i++)
        {
            bool on = hits.Dequeue();
            isHit = on;
            toggled = on;
            if (on)
            {
                setBright();
            }
            else
            {
                setDark();
            }
        }
    }

    private void setBright()
    {
        if (glowMatOnToggle) rend.material = glowMat;
        if (labelRend != null) labelRend.material.SetFloat("_EmissionGain", labelEmission);
    }

    private void setDark()
    {
        if (glowMatOnToggle) rend.material = offMat;
        if (labelRend != null) labelRend.material.SetFloat("_EmissionGain", .1f);
    }

    public bool isHit = false;

    public void keyHit(bool on, bool invokeEvents = false)
    {
        bool oldValue = isHit;
        isHit = on;
        toggled = on;

        if (manipulatorObjScript != null) manipulatorObjScript.bigHaptic((ushort)3999, 0.015f);

        if (on)
        {
            if (singleID) _componentInterface?.hit(on, buttonID);
            else _componentInterface?.hit(on, button2DID[0], button2DID[1]);

            if (glowMatOnToggle) rend.material = glowMat;
            if (labelRend != null) labelRend.material.SetFloat("_EmissionGain", labelEmission);
            selectOverlay.GetComponent<Renderer>().material.SetColor("_TintColor", new Color(1f, 1f, 1f));
        }
        else
        {
            if (singleID) _componentInterface?.hit(on, buttonID);
            else _componentInterface?.hit(on, button2DID[0], button2DID[1]);

            if (glowMatOnToggle) rend.material = offMat;
            if (labelRend != null) labelRend.material.SetFloat("_EmissionGain", .1f);
            selectOverlay.GetComponent<Renderer>().material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f));
        }

        if (invokeEvents && oldValue != on)
        {
            onToggleChangedEvent.Invoke();
        }
        disregardStartToggled = true;
    }

    bool toggled = false;

    public override void setState(manipState state)
    {
        if (curState == manipState.grabbed && state != curState)
        {
            if (!isSwitch) keyHit(false, true);
            if (!glowMatOnToggle)
            {
                rend.material = offMat;
            }
        }
        curState = state;
        if (curState == manipState.none)
        {
            if (!singleID) _componentInterface?.onSelect(false, button2DID[0], button2DID[1]);
            selectOverlay.SetActive(false);
            selectOverlay.GetComponent<Renderer>().material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f));
        }
        else if (curState == manipState.selected)
        {
            if (!singleID) _componentInterface?.onSelect(true, button2DID[0], button2DID[1]);
            selectOverlay.GetComponent<Renderer>().material.SetColor("_TintColor", new Color(0.7f, 0.7f, 0.7f));
            selectOverlay.SetActive(true);
        }
        else if (curState == manipState.grabbed)
        {
            selectOverlay.GetComponent<Renderer>().material.SetColor("_TintColor", new Color(1f, 1f, 1f));
            if (!singleID) _componentInterface?.onSelect(true, button2DID[0], button2DID[1]);
            if (isSwitch)
            {
                toggled = !toggled;
                if (toggled) keyHit(true, true);
                else if (!onlyOn) keyHit(false, true);
            }
            else keyHit(true, true);

            if (!glowMatOnToggle)
            {
                rend.material = glowMat;
            }
        }
    }

    public override void onTouch(bool on, manipulator m)
    {
        if (m != null)
        {
            if (m.emptyGrab)
            {
                if (!on)
                {
                    if (!isSwitch) keyHit(false, true);
                    if (!glowMatOnToggle)
                    {
                        rend.material = offMat;
                    }
                }
                else
                {

                    m.hapticPulse();

                    if (isSwitch)
                    {
                        toggled = !toggled;
                        if (toggled) keyHit(true, true);
                        else if (!onlyOn) keyHit(false, true);
                    }
                    else keyHit(true, true);

                    if (!glowMatOnToggle)
                    {
                        rend.material = glowMat;
                    }
                }
            }
        }
    }
}
