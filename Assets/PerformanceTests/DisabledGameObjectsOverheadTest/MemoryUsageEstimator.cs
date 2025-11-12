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

using UnityEngine;
using UnityEngine.Profiling;

public class MemoryUsageEstimator : MonoBehaviour
{
    public GameObject targetObject;

    void Update()
    {
        if (targetObject != null)
        {
            long memoryUsage = CalculateMemoryUsage(targetObject);
            Debug.Log($"Estimated memory usage for {targetObject.name}: {memoryUsage / 1024f} KB");
        }
        else
        {
            Debug.LogError("Target object is not assigned.");
        }
    }

    long CalculateMemoryUsage(GameObject obj)
    {
        long totalMemory = 0;

        // Add memory used by the GameObject itself
        totalMemory += Profiler.GetRuntimeMemorySizeLong(obj);

        // Add memory used by all components
        foreach (Component component in obj.GetComponents<Component>())
        {
            totalMemory += Profiler.GetRuntimeMemorySizeLong(component);
        }

        // Add memory used by all child GameObjects
        foreach (Transform child in obj.transform)
        {
            totalMemory += CalculateMemoryUsage(child.gameObject);
        }

        return totalMemory;
    }
}
