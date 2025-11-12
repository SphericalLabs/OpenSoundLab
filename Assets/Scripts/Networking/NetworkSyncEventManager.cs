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
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class NetworkSyncEventManager : MonoBehaviour
{
    public static NetworkSyncEventManager Instance;
    public delegate void SyncHandler();
    public event SyncHandler OsSyncEvent;
    public event SyncHandler IntervalSyncEvent;

    [Header("Fixed Interval Sync")]
    private bool syncInFixedIntervals = false;
    private float syncTime = 60;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        SubscribeToEvents();
        if (syncInFixedIntervals)
        {
            StartCoroutine(IntervalSync());
        }
    }

    private void OnDestroy()
    {
        UnSubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        OVRManager.HMDAcquired += UpdateSync;
        OVRManager.HMDMounted += UpdateSync;
        OVRManager.VrFocusAcquired += UpdateSync;
        OVRManager.InputFocusAcquired += UpdateSync;
        OVRManager.TrackingAcquired += UpdateSync;
    }
    private void UnSubscribeToEvents()
    {
        OVRManager.HMDAcquired -= UpdateSync;
        OVRManager.HMDMounted -= UpdateSync;
        OVRManager.VrFocusAcquired -= UpdateSync;
        OVRManager.InputFocusAcquired -= UpdateSync;
        OVRManager.TrackingAcquired -= UpdateSync;
    }

    public void UpdateSync()
    {
        Debug.Log("Update Sync");
        OsSyncEvent?.Invoke();
    }

    private IEnumerator IntervalSync()
    {
        yield return new WaitForSeconds(syncTime);
        IntervalSyncEvent?.Invoke();
    }

}

#if UNITY_EDITOR
[CustomEditor(typeof(NetworkSyncEventManager))]
public class MyScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draws the default inspector layout

        NetworkSyncEventManager myScript = (NetworkSyncEventManager)target;
        if (GUILayout.Button("Sync Now"))
        {
            myScript.UpdateSync();
        }
    }
}
#endif
