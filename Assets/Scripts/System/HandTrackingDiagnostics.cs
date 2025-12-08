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

using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Runtime hand-tracking diagnostics. Attach this to the same rig object that owns HandInputAdapter.
/// Logs PASS/FAIL per test on a fixed interval so issues can be spotted in-HMD.
/// </summary>
public class HandTrackingDiagnostics : MonoBehaviour
{
    public HandInputAdapter handAdapter;
    public bool forceEnableUnityLogger = true;
    public bool runOnceOnStart = true;
    public bool autoRunOnStart = true;
    public float logIntervalSeconds = 3f;

    readonly string prefix = "[HandDiag]";
    float nextLogTime;

    void Awake()
    {
        if (forceEnableUnityLogger) Debug.unityLogger.logEnabled = true;
        if (handAdapter == null) handAdapter = HandInputAdapter.Instance ?? FindAnyObjectByType<HandInputAdapter>();
    }

    void Start()
    {
        if (runOnceOnStart) RunAllTests();
        if (autoRunOnStart && logIntervalSeconds > 0f) nextLogTime = Time.time + logIntervalSeconds;
    }

    void Update()
    {
        if (!autoRunOnStart) return;
        if (logIntervalSeconds <= 0f) return;
        if (Time.time < nextLogTime) return;
        RunAllTests();
        nextLogTime = Time.time + logIntervalSeconds;
    }

    public void RunAllTests()
    {
        logTest("SkeletonVersionMatchesRuntime", testSkeletonVersion(), "Requested vs runtime skeleton version");
        logTest("LeftSkeletonInitialized", testSkeletonInitialized(0), "Left hand skeleton has bones");
        logTest("RightSkeletonInitialized", testSkeletonInitialized(1), "Right hand skeleton has bones");
        logTest("LeftDataProviderAttached", testDataProviderMatch(0), "Left hand skeleton uses parent data provider");
        logTest("RightDataProviderAttached", testDataProviderMatch(1), "Right hand skeleton uses parent data provider");
        logTest("LeftPointerPoseValid", testPointerValid(0), "OVRHand PointerPose validity");
        logTest("RightPointerPoseValid", testPointerValid(1), "OVRHand PointerPose validity");
        logTest("LeftPointerAlignsMidpoint", testPointerMidpointAlignment(0, out float leftAngle), $"Angle {leftAngle:0.0}° (want <45°)");
        logTest("RightPointerAlignsMidpoint", testPointerMidpointAlignment(1, out float rightAngle), $"Angle {rightAngle:0.0}° (want <45°)");
    }

    void logTest(string name, bool pass, string detail)
    {
        string state = pass ? "PASS" : "FAIL";
        Debug.Log($"{prefix} {state} {name} - {detail}");
    }

    bool testSkeletonVersion()
    {
        // Desired version from project settings
        OVRHandSkeletonVersion desired = OVRHandSkeletonVersion.OpenXR;
        try
        {
            OVRRuntimeSettings settings = OVRRuntimeSettings.Instance;
            if (settings != null) desired = settings.HandSkeletonVersion;
        }
        catch (Exception) { }

        // Try to read runtime version via reflection (API differs across OVR releases)
        OVRHandSkeletonVersion runtimeVersion = desired;
        try
        {
            var prop = typeof(OVRPlugin).GetProperty("HandSkeletonVersion", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null)
            {
                object val = prop.GetValue(null, null);
                if (val is OVRHandSkeletonVersion v) runtimeVersion = v;
            }
        }
        catch (Exception) { }

        // Fallback: compare skeleton type on left hand to expected type; if mismatch, fail.
        OVRSkeleton left = getSkeleton(0);
        if (left != null)
        {
            OVRSkeleton.SkeletonType expectedType = desired == OVRHandSkeletonVersion.OpenXR
                ? OVRSkeleton.SkeletonType.XRHandLeft
                : OVRSkeleton.SkeletonType.HandLeft;
            OVRSkeleton.SkeletonType? actualType = getSkeletonType(left);
            if (actualType.HasValue && actualType.Value != expectedType) return false;
        }

        return runtimeVersion == desired;
    }

    bool testSkeletonInitialized(int controllerIndex)
    {
        OVRSkeleton skeleton = getSkeleton(controllerIndex);
        if (skeleton == null) return false;
        return skeleton.IsInitialized && skeleton.Bones != null && skeleton.Bones.Count > 0;
    }

    bool testDataProviderMatch(int controllerIndex)
    {
        OVRSkeleton skeleton = getSkeleton(controllerIndex);
        if (skeleton == null) return false;

        var provider = skeleton.GetComponentInParent<OVRSkeleton.IOVRSkeletonDataProvider>(true);
        if (provider == null) return false;

        // use private field via reflection (public accessor not available on older OVR versions)
        OVRSkeleton.IOVRSkeletonDataProvider activeProvider = null;
        FieldInfo field = typeof(OVRSkeleton).GetField("_dataProvider", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null) activeProvider = field.GetValue(skeleton) as OVRSkeleton.IOVRSkeletonDataProvider;

        return activeProvider == provider;
    }

    bool testPointerValid(int controllerIndex)
    {
        if (handAdapter == null) return false;
        return handAdapter.isPointerPoseValid(controllerIndex);
    }

    bool testPointerMidpointAlignment(int controllerIndex, out float angle)
    {
        angle = 999f;
        if (handAdapter == null) return false;

        Transform pointer = handAdapter.getPointerPose(controllerIndex);
        bool pointerValid = handAdapter.isPointerPoseValid(controllerIndex) && pointer != null;
        if (!pointerValid) return false;

        if (!handAdapter.tryGetThumbIndexMidpoint(controllerIndex, out _, out Quaternion midpointRot)) return false;

        angle = Quaternion.Angle(pointer.rotation, midpointRot);
        return angle < 45f;
    }

    OVRSkeleton getSkeleton(int controllerIndex)
    {
        if (handAdapter == null) return null;

        Transform anchor = controllerIndex == 0 ? handAdapter.leftHandAnchor : handAdapter.rightHandAnchor;
        if (anchor == null) return null;
        return anchor.GetComponent<OVRSkeleton>();
    }

    OVRSkeleton.SkeletonType? getSkeletonType(OVRSkeleton skeleton)
    {
        if (skeleton == null) return null;
        FieldInfo field = typeof(OVRSkeleton).GetField("_skeletonType", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) return null;
        object value = field.GetValue(skeleton);
        if (value is OVRSkeleton.SkeletonType typeValue) return typeValue;
        return null;
    }
}
