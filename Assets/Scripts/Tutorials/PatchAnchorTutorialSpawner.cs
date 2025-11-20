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
    public Vector3 tutorialLocalPosition = new Vector3(0f, 1.3f, 0.75f);
    public Vector3 tutorialLocalEuler = new Vector3(0f, -180f, 0f);
    public bool spawnOnStart = true;

    bool tutorialsSpawned;

    IEnumerator Start()
    {
        if (!spawnOnStart)
        {
            yield break;
        }

        if (isNetworkSessionActive())
        {
            yield return waitForNetworkSpawner();

            // Let the server/host perform the spawn; clients will receive it over the network.
            if (!NetworkServer.active)
            {
                yield break;
            }
        }

        spawnTutorials();
    }

    public void spawnTutorials()
    {
        if (tutorialsSpawned)
        {
            return;
        }

        if (isNetworkSessionActive() && !NetworkServer.active)
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

        Vector3 worldPosition = transform.TransformPoint(tutorialLocalPosition);
        Quaternion worldRotation = transform.rotation * Quaternion.Euler(tutorialLocalEuler);

        if (isNetworkSessionActive())
        {
            if (trySpawnNetworked(worldPosition, worldRotation))
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

    bool trySpawnNetworked(Vector3 worldPosition, Quaternion worldRotation)
    {
        if (NetworkSpawnManager.Instance == null || NetworkSpawnManager.Instance.netId == 0)
        {
            return false;
        }

        if (NetworkServer.active)
        {
            NetworkSpawnManager.Instance.CreateItem(tutorialsPrefab.name, worldPosition, worldRotation, Vector3.zero, Vector3.zero);
            return true;
        }

        return false;
    }

    bool isNetworkSessionActive()
    {
        if (NetworkManager.singleton == null)
        {
            return false;
        }

        return NetworkManager.singleton.isNetworkActive;
    }

    IEnumerator waitForNetworkSpawner()
    {
        while (isNetworkSessionActive())
        {
            if (NetworkSpawnManager.Instance != null && NetworkSpawnManager.Instance.netId != 0)
            {
                yield break;
            }

            yield return null;
        }
    }
}
