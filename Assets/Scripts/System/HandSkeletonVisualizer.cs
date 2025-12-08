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
using System.Collections.Generic;

public class HandSkeletonVisualizer : MonoBehaviour
{
    public OVRSkeleton skeleton;
    public bool visibleByDefault = true;
    public float gizmoScale = 0.01f;
    public Color gizmoColor = Color.cyan;

    readonly List<GameObject> gizmos = new List<GameObject>();
    Material gizmoMaterial;

    void Start()
    {
        if (skeleton == null) skeleton = GetComponent<OVRSkeleton>();
        BuildMaterial();
        TryBuildGizmos();
    }

    void Update()
    {
        TryBuildGizmos();
        UpdateVisibility();
    }

    void BuildMaterial()
    {
        if (gizmoMaterial != null) return;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        gizmoMaterial = new Material(shader);
        gizmoMaterial.color = gizmoColor;
        gizmoMaterial.renderQueue = 3000; // render in front of most geometry
        gizmoMaterial.hideFlags = HideFlags.HideAndDontSave;
    }

    void TryBuildGizmos()
    {
        if (skeleton == null || !skeleton.IsInitialized || skeleton.Bones == null) return;

        if (gizmos.Count == skeleton.Bones.Count) return;

        ClearGizmos();

        for (int i = 0; i < skeleton.Bones.Count; i++)
        {
            var bone = skeleton.Bones[i];
            if (bone == null || bone.Transform == null) continue;
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = "BoneViz_" + bone.Id.ToString();
            g.transform.SetParent(bone.Transform, false);
            g.transform.localPosition = Vector3.zero;
            g.transform.localRotation = Quaternion.identity;
            g.transform.localScale = Vector3.one * gizmoScale;
            var renderer = g.GetComponent<Renderer>();
            if (renderer != null && gizmoMaterial != null)
            {
                renderer.sharedMaterial = gizmoMaterial;
            }
            var collider = g.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            gizmos.Add(g);
        }
    }

    void UpdateVisibility()
    {
        bool show = visibleByDefault && skeleton != null && skeleton.IsInitialized && skeleton.Bones != null;
        for (int i = 0; i < gizmos.Count; i++)
        {
            if (gizmos[i] != null && gizmos[i].activeSelf != show) gizmos[i].SetActive(show);
        }
    }

    void ClearGizmos()
    {
        for (int i = 0; i < gizmos.Count; i++)
        {
            if (gizmos[i] != null) Destroy(gizmos[i]);
        }
        gizmos.Clear();
    }

    void OnDestroy()
    {
        ClearGizmos();
        if (gizmoMaterial != null) Destroy(gizmoMaterial);
    }
}
