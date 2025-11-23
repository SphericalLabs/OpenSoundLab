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

using Mirror;
using UnityEngine;
using System.Collections;

public class PatchAnchorTutorialSpawner : MonoBehaviour
{
    public GameObject tutorialsPrefab;
    public Vector3 tutorialLocalPosition = new Vector3(0f, 1.3f, 0.5f);
    public Vector3 tutorialLocalEuler = new Vector3(0f, -180f, 0f);
    public bool spawnOnStart = true;
    public OVRCameraRig cameraRig;
    public float headsetReadyTimeout = 4f;
    public int trackedHeadsetFrames = 2;
    public float spawnForwardDistance = 0.8f;
    public float spawnHeightOffset = -0.45f;

    bool tutorialsSpawned;
    Transform headAnchor;

    IEnumerator Start()
    {
        if (!spawnOnStart)
        {
            yield break;
        }

        if (isNetworkSessionActive())
        {
            yield return waitForNetworkReady();
        }

        yield return waitForHeadsetReady();

        spawnTutorials();
    }

    public void spawnTutorials()
    {
        if (tutorialsSpawned)
        {
            return;
        }

        if (tutorialsPrefab == null)
        {
            Debug.LogWarning("PatchAnchorTutorialSpawner: tutorialsPrefab not assigned; cannot spawn tutorials.", this);
            return;
        }

        if (!shouldSpawnTutorials())
        {
            return;
        }

        if (isNetworkSessionActive())
        {
            if (trySpawnNetworked())
            {
                tutorialsSpawned = true;
            }
            return;
        }

        GameObject tutorialsRoot = Instantiate(tutorialsPrefab, transform, false);
        tutorialsRoot.transform.localPosition = tutorialLocalPosition;
        tutorialsRoot.transform.localRotation = Quaternion.Euler(tutorialLocalEuler);
        tutorialsSpawned = true;
    }

    bool shouldSpawnTutorials()
    {
        if (PlayerPrefs.GetInt("showTutorialsOnStartup", 1) != 1)
        {
            return false;
        }

        return FindObjectOfType<tutorialsDeviceInterface>() == null;
    }

    bool trySpawnNetworked()
    {
        if (NetworkSpawnManager.Instance == null)
        {
            return false;
        }

        updatePlacementFromHeadset();

        if (NetworkServer.active)
        {
            NetworkSpawnManager.Instance.CreateItem(tutorialsPrefab.name, transform.position, transform.rotation, Vector3.zero, Vector3.zero);
            return true;
        }

        if (NetworkClient.active)
        {
            NetworkSpawnManager.Instance.CmdCreateItem(tutorialsPrefab.name, transform.position, transform.rotation, Vector3.zero, Vector3.zero);
            return true;
        }

        return false;
    }

    IEnumerator waitForHeadsetReady()
    {
        float elapsed = 0f;
        headAnchor = resolveHeadAnchor();
        while (headAnchor == null && elapsed < headsetReadyTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
            headAnchor = resolveHeadAnchor();
        }

        int trackedFrames = 0;
        while (headAnchor != null && trackedFrames < trackedHeadsetFrames && elapsed < headsetReadyTimeout)
        {
            if (isHeadsetTracked())
            {
                trackedFrames++;
            }
            else
            {
                trackedFrames = 0;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    Transform resolveHeadAnchor()
    {
        if (headAnchor != null)
        {
            return headAnchor;
        }

        if (cameraRig != null && cameraRig.centerEyeAnchor != null)
        {
            headAnchor = cameraRig.centerEyeAnchor;
            return headAnchor;
        }

        OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
        if (rig != null)
        {
            cameraRig = rig;
            if (rig.centerEyeAnchor != null)
            {
                headAnchor = rig.centerEyeAnchor;
                return headAnchor;
            }

            headAnchor = rig.transform;
            return headAnchor;
        }

        if (Camera.main != null)
        {
            headAnchor = Camera.main.transform;
            return headAnchor;
        }

        return null;
    }

    bool isHeadsetTracked()
    {
        if (OVRManager.instance != null)
        {
            if (OVRManager.tracker != null)
            {
                return OVRManager.isHmdPresent && OVRManager.tracker.isPositionTracked;
            }

            return OVRManager.isHmdPresent;
        }

        return headAnchor != null;
    }

    bool updatePlacementFromHeadset()
    {
        Transform anchor = headAnchor != null ? headAnchor : resolveHeadAnchor();
        if (anchor == null)
        {
            return false;
        }

        Vector3 targetPos = anchor.position + anchor.forward * spawnForwardDistance;
        Vector3 lookPos = anchor.position;
        lookPos.y = targetPos.y;
        transform.position = targetPos;
        transform.LookAt(lookPos);

        transform.Translate(0f, spawnHeightOffset, 0f);

        return true;
    }

    bool isNetworkSessionActive()
    {
        if (NetworkManager.singleton == null)
        {
            return false;
        }

        return NetworkManager.singleton.isNetworkActive;
    }

    IEnumerator waitForNetworkReady()
    {
        // Wait until NetworkServer is active or NetworkClient is ready
        while (isNetworkSessionActive() && !NetworkServer.active &&
               (!NetworkClient.isConnected || !NetworkClient.ready || NetworkClient.connection == null || !NetworkClient.connection.isReady))
        {
            yield return null;
        }
    }
}
