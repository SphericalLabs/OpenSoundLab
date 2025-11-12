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
using System.Linq;
using UnityEngine;

public class RemoveDeviceComponents : MonoBehaviour
{
    public bool searchInChildren = true;
    public GameObject[] targets;
    public Material newMaterial;

    [ContextMenu("Remove Components")]
    public void RemoveComponents()
    {
        foreach (GameObject g in targets)
        {
            if (searchInChildren)
            {
                tooltips t = g.GetComponentInChildren<tooltips>(true);
                if (t != null) DestroyImmediate(t.gameObject, true);

                MonoBehaviour[] m = g.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < m.Length; i++) DestroyImmediate(m[i], true);

                AudioSource[] audios = g.GetComponentsInChildren<AudioSource>(true);
                for (int i = 0; i < audios.Length; i++) DestroyImmediate(audios[i], true);

                Rigidbody[] rig = g.GetComponentsInChildren<Rigidbody>(true);
                for (int i = 0; i < rig.Length; i++) DestroyImmediate(rig[i], true);

                Collider[] col = g.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < col.Length; i++) DestroyImmediate(col[i], true);

                TextMesh[] tm = g.GetComponentsInChildren<TextMesh>(true);
                for (int i = 0; i < tm.Length; i++) DestroyImmediate(tm[i].gameObject, true);

                if (newMaterial != null)
                {
                    Renderer[] r = g.GetComponentsInChildren<Renderer>();
                    for (int i = 0; i < r.Length; i++)
                    {
                        r[i].material = newMaterial;
                    }
                }
            }
            else
            {
                MonoBehaviour[] m = g.GetComponents<MonoBehaviour>();
                for (int i = 0; i < m.Length; i++) DestroyImmediate(m[i], true);

                AudioSource[] audios = g.GetComponents<AudioSource>();
                for (int i = 0; i < audios.Length; i++) DestroyImmediate(audios[i], true);

                Rigidbody[] rig = g.GetComponents<Rigidbody>();
                for (int i = 0; i < rig.Length; i++) DestroyImmediate(rig[i], true);

                Collider[] col = g.GetComponents<Collider>();
                for (int i = 0; i < col.Length; i++) DestroyImmediate(col[i], true);

                TextMesh[] tm = g.GetComponents<TextMesh>();
                for (int i = 0; i < tm.Length; i++) DestroyImmediate(tm[i].gameObject, true);

                if (newMaterial != null)
                {
                    Renderer[] r = g.GetComponents<Renderer>();
                    for (int i = 0; i < r.Length; i++)
                    {
                        r[i].material = newMaterial;
                    }
                }
            }

        }

    }
}
