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
    public float pinchThreshold = 0.25f;
    public float fullPinchThreshold = 0.7f;
    public float fistThreshold = 0.1f;
    public bool drawBoneGizmos = true;
    public float boneGizmoScale = 0.01f;
    public Color boneGizmoColor = Color.cyan;

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
    OVRSkeleton leftSkeleton;
    OVRSkeleton rightSkeleton;

    bool handsActive;
    int cachedFrame = -1;
    bool skeletonVersionConfigured;

    void Awake()
    {
        instance = this;
        ensureHandSkeletonVersion();
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

    public bool isThumbIndexPinched(int controllerIndex)
    {
        cacheState();
        int idx = controllerIndex == 0 ? 0 : 1;
        return handsActive && indexPinch[idx] >= pinchThreshold && thumbPinch[idx] >= pinchThreshold;
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

    public bool isSkeletonReady(int controllerIndex)
    {
        OVRSkeleton skeleton = controllerIndex == 0 ? leftSkeleton : rightSkeleton;
        return skeleton != null && skeleton.IsInitialized && skeleton.Bones != null && skeleton.Bones.Count > 0;
    }

    public Transform getPointerPose(int controllerIndex)
    {
        cacheState();
        if (controllerIndex == 0) return leftHand != null ? leftHand.PointerPose : null;
        if (controllerIndex == 1) return rightHand != null ? rightHand.PointerPose : null;
        return null;
    }

    public bool isPointerPoseValid(int controllerIndex)
    {
        cacheState();
        if (controllerIndex == 0) return leftHand != null && leftHand.IsPointerPoseValid;
        if (controllerIndex == 1) return rightHand != null && rightHand.IsPointerPoseValid;
        return false;
    }

    public bool tryGetThumbIndexMidpoint(int controllerIndex, out Vector3 position, out Quaternion rotation)
    {
        cacheState();
        position = Vector3.zero;
        rotation = Quaternion.identity;

        OVRSkeleton skeleton = controllerIndex == 0 ? leftSkeleton : rightSkeleton;
        if (skeleton == null || !skeleton.IsInitialized || skeleton.Bones == null) return false;

        Transform thumbTip = getThumbTipTransform(skeleton);
        Transform indexTip = getIndexTipTransform(skeleton);
        if (thumbTip == null || indexTip == null) return false;

        position = (thumbTip.position + indexTip.position) * 0.5f;
        rotation = thumbTip.rotation;

        return true;
    }

    public bool tryGetThumbTipPose(int controllerIndex, out Vector3 position, out Quaternion rotation)
    {
        cacheState();
        position = Vector3.zero;
        rotation = Quaternion.identity;

        OVRSkeleton skeleton = controllerIndex == 0 ? leftSkeleton : rightSkeleton;
        if (skeleton == null || !skeleton.IsInitialized || skeleton.Bones == null) return false;

        Transform thumbTip = getThumbTipTransform(skeleton);
        if (thumbTip == null) return false;

        position = thumbTip.position;
        rotation = thumbTip.rotation;
        return true;
    }

    public bool tryGetIndexTipPose(int controllerIndex, out Vector3 position, out Quaternion rotation)
    {
        cacheState();
        position = Vector3.zero;
        rotation = Quaternion.identity;

        OVRSkeleton skeleton = controllerIndex == 0 ? leftSkeleton : rightSkeleton;
        if (skeleton == null || !skeleton.IsInitialized || skeleton.Bones == null) return false;

        Transform indexTip = getIndexTipTransform(skeleton);
        if (indexTip == null) return false;

        position = indexTip.position;
        rotation = indexTip.rotation;
        return true;
    }

    public bool tryGetWristPose(int controllerIndex, out Vector3 position, out Quaternion rotation)
    {
        cacheState();
        position = Vector3.zero;
        rotation = Quaternion.identity;

        OVRSkeleton skeleton = controllerIndex == 0 ? leftSkeleton : rightSkeleton;
        if (skeleton == null || !skeleton.IsInitialized || skeleton.Bones == null) return false;

        Transform wrist = getWristTransform(skeleton);
        if (wrist == null) return false;

        position = wrist.position;
        rotation = wrist.rotation;
        return true;
    }

    public bool tryGetMiddleMetacarpalPose(int controllerIndex, out Vector3 position, out Quaternion rotation)
    {
        cacheState();
        position = Vector3.zero;
        rotation = Quaternion.identity;

        OVRSkeleton skeleton = controllerIndex == 0 ? leftSkeleton : rightSkeleton;
        if (skeleton == null || !skeleton.IsInitialized || skeleton.Bones == null) return false;

        Transform meta = getMiddleMetacarpalTransform(skeleton);
        if (meta == null) return false;

        position = meta.position;
        rotation = meta.rotation;
        return true;
    }

    Transform getThumbTipTransform(OVRSkeleton skeleton)
    {
        Transform thumb = getBoneTransform(skeleton, OVRSkeleton.BoneId.Hand_ThumbTip);
        if (!isThumbTransform(thumb)) thumb = null;
        if (thumb == null) thumb = getBoneTransform(skeleton, OVRSkeleton.BoneId.XRHand_ThumbTip);
        if (!isThumbTransform(thumb)) thumb = null;
        if (thumb == null) thumb = getBoneTransformByName(skeleton, "thumb");
        if (!isThumbTransform(thumb)) thumb = null;
        if (thumb == null) thumb = getFurthestFingerBone(skeleton, "thumb");
        return thumb;
    }

    Transform getIndexTipTransform(OVRSkeleton skeleton)
    {
        Transform index = getBoneTransform(skeleton, OVRSkeleton.BoneId.Hand_IndexTip);
        if (!isIndexTransform(index)) index = null;
        if (index == null) index = getBoneTransform(skeleton, OVRSkeleton.BoneId.XRHand_IndexTip);
        if (!isIndexTransform(index)) index = null;
        if (index == null) index = getBoneTransformByName(skeleton, "index");
        if (!isIndexTransform(index)) index = null;
        if (index == null) index = getFurthestFingerBone(skeleton, "index");
        return index;
    }

    Transform getBoneTransform(OVRSkeleton skeleton, OVRSkeleton.BoneId id)
    {
        if (skeleton == null || skeleton.Bones == null) return null;
        var bones = skeleton.Bones;
        for (int i = 0; i < bones.Count; i++)
        {
            if (bones[i] != null && bones[i].Id == id) return bones[i].Transform;
        }
        return null;
    }

    Transform getBoneTransformByName(OVRSkeleton skeleton, string containsLower)
    {
        if (skeleton == null || skeleton.Bones == null) return null;
        for (int i = 0; i < skeleton.Bones.Count; i++)
        {
            var bone = skeleton.Bones[i];
            if (bone == null || bone.Transform == null) continue;
            string n = bone.Transform.name.ToLowerInvariant();
            if (n.Contains(containsLower)) return bone.Transform;
        }
        return null;
    }

    bool isThumbTransform(Transform t)
    {
        if (t == null) return false;
        string n = t.name.ToLowerInvariant();
        return n.Contains("thumb");
    }

    bool isIndexTransform(Transform t)
    {
        if (t == null) return false;
        string n = t.name.ToLowerInvariant();
        return n.Contains("index");
    }

    Transform getFurthestFingerBone(OVRSkeleton skeleton, string fingerKeyword)
    {
        if (skeleton == null || skeleton.Bones == null) return null;
        Transform wrist = getBoneTransform(skeleton, OVRSkeleton.BoneId.Hand_WristRoot);
        if (wrist == null) wrist = getBoneTransformByName(skeleton, "wrist");
        if (wrist == null) return null;

        float maxDist = -1f;
        Transform best = null;
        string keyword = fingerKeyword.ToLowerInvariant();

        for (int i = 0; i < skeleton.Bones.Count; i++)
        {
            var bone = skeleton.Bones[i];
            if (bone == null || bone.Transform == null) continue;
            string name = bone.Transform.name.ToLowerInvariant();
            if (!name.Contains(keyword)) continue;
            float dist = Vector3.Distance(wrist.position, bone.Transform.position);
            if (dist > maxDist)
            {
                maxDist = dist;
                best = bone.Transform;
            }
        }

        return best;
    }

    Transform getWristTransform(OVRSkeleton skeleton)
    {
        Transform wrist = getBoneTransform(skeleton, OVRSkeleton.BoneId.Hand_WristRoot);
        if (wrist == null) wrist = getBoneTransformByName(skeleton, "wrist");
        return wrist;
    }

    Transform getMiddleMetacarpalTransform(OVRSkeleton skeleton)
    {
        // Explicit metacarpal anchor; will surface a compile error if the enum is unavailable in this OVR version.
        return getBoneTransform(skeleton, OVRSkeleton.BoneId.XRHand_MiddleMetacarpal);
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
            ensureSkeleton(leftHandAnchor, ref leftSkeleton, OVRHand.Hand.HandLeft, OVRSkeleton.SkeletonType.HandLeft);
        }

        if (rightHand == null && rightHandAnchor != null)
        {
            rightHand = rightHandAnchor.GetComponent<OVRHand>();
            if (rightHand == null) rightHand = rightHandAnchor.gameObject.AddComponent<OVRHand>();
            setHandType(rightHand, OVRHand.Hand.HandRight);
            if (hideHandMeshes) disableRenderers(rightHandAnchor);
            ensureSkeleton(rightHandAnchor, ref rightSkeleton, OVRHand.Hand.HandRight, OVRSkeleton.SkeletonType.HandRight);
        }
    }

    void setHandType(OVRHand hand, OVRHand.Hand type)
    {
        if (hand == null) return;
        FieldInfo field = typeof(OVRHand).GetField("HandType", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (field != null) field.SetValue(hand, type);
    }

    void ensureSkeleton(Transform anchor, ref OVRSkeleton skeleton, OVRHand.Hand handType, OVRSkeleton.SkeletonType skeletonType)
    {
        if (anchor == null) return;
        if (skeleton == null) skeleton = anchor.GetComponent<OVRSkeleton>();
        if (skeleton == null) skeleton = anchor.gameObject.AddComponent<OVRSkeleton>();

        OVRSkeleton.SkeletonType configuredType = getConfiguredSkeletonType(handType, skeletonType);
        setSkeletonType(skeleton, configuredType);
        setSkeletonDataProvider(anchor, skeleton);
        skeleton.enabled = true;
        ensureVisualizer(anchor, skeleton);
    }

    OVRSkeleton.SkeletonType getConfiguredSkeletonType(OVRHand.Hand handType, OVRSkeleton.SkeletonType defaultType)
    {
        OVRHandSkeletonVersion version = OVRHandSkeletonVersion.OpenXR;
        try
        {
            OVRRuntimeSettings settings = OVRRuntimeSettings.Instance;
            if (settings != null) version = settings.HandSkeletonVersion;
        }
        catch (Exception) { }

        if (version == OVRHandSkeletonVersion.OpenXR)
        {
            return handType == OVRHand.Hand.HandLeft ? OVRSkeleton.SkeletonType.XRHandLeft : OVRSkeleton.SkeletonType.XRHandRight;
        }

        return defaultType;
    }

    void setSkeletonDataProvider(Transform anchor, OVRSkeleton skeleton)
    {
        if (anchor == null || skeleton == null) return;
        OVRSkeleton.IOVRSkeletonDataProvider provider = anchor.GetComponentInParent<OVRSkeleton.IOVRSkeletonDataProvider>(true);
        if (provider == null) return;

        try
        {
            FieldInfo field = typeof(OVRSkeleton).GetField("_dataProvider", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null) field.SetValue(skeleton, provider);
        }
        catch (Exception) { }
    }

    void ensureHandSkeletonVersion()
    {
        if (skeletonVersionConfigured) return;

        try
        {
            OVRHandSkeletonVersion version = OVRHandSkeletonVersion.OpenXR;
            OVRRuntimeSettings settings = OVRRuntimeSettings.Instance;
            if (settings != null) version = settings.HandSkeletonVersion;

            if (OVRPlugin.HandSkeletonVersion != version)
            {
                OVRPlugin.SetHandSkeletonVersion(version);
            }
        }
        catch (Exception) { }

        skeletonVersionConfigured = true;
    }

    void setSkeletonType(OVRSkeleton skeleton, OVRSkeleton.SkeletonType type)
    {
        if (skeleton == null) return;
        FieldInfo field = typeof(OVRSkeleton).GetField("_skeletonType", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null) field.SetValue(skeleton, type);
    }

    void ensureVisualizer(Transform anchor, OVRSkeleton skeleton)
    {
        if (!drawBoneGizmos) return;
        HandSkeletonVisualizer viz = anchor.GetComponent<HandSkeletonVisualizer>();
        if (viz == null) viz = anchor.gameObject.AddComponent<HandSkeletonVisualizer>();
        viz.skeleton = skeleton;
        viz.visibleByDefault = true;
        viz.gizmoScale = boneGizmoScale;
        viz.gizmoColor = boneGizmoColor;
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
        bool trackedHands = (leftHand != null && leftHand.IsTracked) || (rightHand != null && rightHand.IsTracked);
        handsActive = (pluginHands || trackedHands) && anyHandComponent;

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
        fistStrength[0] = Mathf.Max(middlePinch[0], Mathf.Max(ringPinch[0], pinkyPinch[0]));

        indexPinch[1] = readPinch(rightHand, OVRHand.HandFinger.Index);
        thumbPinch[1] = readPinch(rightHand, OVRHand.HandFinger.Thumb);
        middlePinch[1] = readPinch(rightHand, OVRHand.HandFinger.Middle);
        ringPinch[1] = readPinch(rightHand, OVRHand.HandFinger.Ring);
        pinkyPinch[1] = readPinch(rightHand, OVRHand.HandFinger.Pinky);
        fistStrength[1] = Mathf.Max(middlePinch[1], Mathf.Max(ringPinch[1], pinkyPinch[1]));
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
