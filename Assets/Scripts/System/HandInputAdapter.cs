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
using System;
using System.Reflection;

public class HandInputAdapter : MonoBehaviour
{
    public Transform leftHandAnchor;
    public Transform rightHandAnchor;
    public bool hideHandMeshes = true;
    public float pinchThreshold = 0.05f;
    public float fullPinchThreshold = 0.7f;
    public float fistThreshold = 0.1f;

    static HandInputAdapter instance;
    public static HandInputAdapter Instance => instance;

    OVRHand leftHand;
    OVRHand rightHand;

    float[] indexPinch = new float[2];
    float[] thumbPinch = new float[2];
    float[] middlePinch = new float[2];
    float[] ringPinch = new float[2];
    float[] pinkyPinch = new float[2];
    float[] fistStrength = new float[2];

    bool handsActive;
    int cachedFrame = -1;

    void Awake()
    {
        instance = this;
        if (leftHandAnchor == null || rightHandAnchor == null) findAnchors();
        ensureHands();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Update()
    {
        cacheState();
    }

    public bool handsActiveThisFrame
    {
        get
        {
            cacheState();
            return handsActive;
        }
    }

    public float getIndexPinchStrength(int controllerIndex)
    {
        cacheState();
        return controllerIndex == 0 ? indexPinch[0] : indexPinch[1];
    }

    public float getThumbPinchStrength(int controllerIndex)
    {
        cacheState();
        return controllerIndex == 0 ? thumbPinch[0] : thumbPinch[1];
    }

    public float getMiddlePinchStrength(int controllerIndex)
    {
        cacheState();
        return controllerIndex == 0 ? middlePinch[0] : middlePinch[1];
    }

    public float getRingPinchStrength(int controllerIndex)
    {
        cacheState();
        return controllerIndex == 0 ? ringPinch[0] : ringPinch[1];
    }

    public float getPinkyPinchStrength(int controllerIndex)
    {
        cacheState();
        return controllerIndex == 0 ? pinkyPinch[0] : pinkyPinch[1];
    }

    public float getFistStrength(int controllerIndex)
    {
        cacheState();
        return controllerIndex == 0 ? fistStrength[0] : fistStrength[1];
    }

    void findAnchors()
    {
        Transform trackingSpace = transform.Find("TrackingSpace");
        if (trackingSpace == null) return;
        if (leftHandAnchor == null) leftHandAnchor = trackingSpace.Find("LeftHandAnchor");
        if (leftHandAnchor == null) leftHandAnchor = trackingSpace.Find("LeftHandAnchorDetached");
        if (rightHandAnchor == null) rightHandAnchor = trackingSpace.Find("RightHandAnchor");
        if (rightHandAnchor == null) rightHandAnchor = trackingSpace.Find("RightHandAnchorDetached");
    }

    void ensureHands()
    {
        if (leftHand == null && leftHandAnchor != null)
        {
            leftHand = leftHandAnchor.GetComponent<OVRHand>();
            if (leftHand == null) leftHand = leftHandAnchor.gameObject.AddComponent<OVRHand>();
            setHandType(leftHand, OVRHand.Hand.HandLeft);
            if (hideHandMeshes) disableRenderers(leftHandAnchor);
        }

        if (rightHand == null && rightHandAnchor != null)
        {
            rightHand = rightHandAnchor.GetComponent<OVRHand>();
            if (rightHand == null) rightHand = rightHandAnchor.gameObject.AddComponent<OVRHand>();
            setHandType(rightHand, OVRHand.Hand.HandRight);
            if (hideHandMeshes) disableRenderers(rightHandAnchor);
        }
    }

    void setHandType(OVRHand hand, OVRHand.Hand type)
    {
        if (hand == null) return;
        FieldInfo field = typeof(OVRHand).GetField("HandType", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field != null) field.SetValue(hand, type);
    }

    void cacheState()
    {
        if (cachedFrame == Time.frameCount) return;
        cachedFrame = Time.frameCount;

        ensureHands();

        OVRPlugin.Controller control = OVRPlugin.GetActiveController();
        bool pluginHands = (control == OVRPlugin.Controller.Hands) ||
                           (control == OVRPlugin.Controller.LHand) ||
                           (control == OVRPlugin.Controller.RHand);
        bool anyHandComponent = leftHand != null || rightHand != null;
        handsActive = pluginHands && anyHandComponent;

        if (!handsActive)
        {
            Array.Clear(indexPinch, 0, indexPinch.Length);
            Array.Clear(thumbPinch, 0, thumbPinch.Length);
            Array.Clear(middlePinch, 0, middlePinch.Length);
            Array.Clear(ringPinch, 0, ringPinch.Length);
            Array.Clear(pinkyPinch, 0, pinkyPinch.Length);
            Array.Clear(fistStrength, 0, fistStrength.Length);
            return;
        }

        indexPinch[0] = readPinch(leftHand, OVRHand.HandFinger.Index);
        thumbPinch[0] = readPinch(leftHand, OVRHand.HandFinger.Thumb);
        middlePinch[0] = readPinch(leftHand, OVRHand.HandFinger.Middle);
        ringPinch[0] = readPinch(leftHand, OVRHand.HandFinger.Ring);
        pinkyPinch[0] = readPinch(leftHand, OVRHand.HandFinger.Pinky);
        fistStrength[0] = Mathf.Min(middlePinch[0], Mathf.Min(ringPinch[0], pinkyPinch[0]));

        indexPinch[1] = readPinch(rightHand, OVRHand.HandFinger.Index);
        thumbPinch[1] = readPinch(rightHand, OVRHand.HandFinger.Thumb);
        middlePinch[1] = readPinch(rightHand, OVRHand.HandFinger.Middle);
        ringPinch[1] = readPinch(rightHand, OVRHand.HandFinger.Ring);
        pinkyPinch[1] = readPinch(rightHand, OVRHand.HandFinger.Pinky);
        fistStrength[1] = Mathf.Min(middlePinch[1], Mathf.Min(ringPinch[1], pinkyPinch[1]));
    }

    float readPinch(OVRHand hand, OVRHand.HandFinger finger)
    {
        if (hand == null) return 0f;
        if (hand.GetFingerConfidence(finger) != OVRHand.TrackingConfidence.High) return 0f;
        return Mathf.Clamp01(hand.GetFingerPinchStrength(finger));
    }

    void disableRenderers(Transform target)
    {
        if (target == null) return;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }
    }
}
