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
    public float gizmoScale = 0.0025f;
    public Color gizmoColor = Color.cyan;

    readonly List<LineRenderer> boneLines = new List<LineRenderer>();
    Material gizmoMaterial;
    int lastSkeletonChangedCount = -1;
    float lastReadyTime = -1f;

    void Start()
    {
        if (skeleton == null) skeleton = GetComponent<OVRSkeleton>();
        BuildMaterial();
        TryBuildGizmos();
    }

    void Update()
    {
        if (skeleton == null) skeleton = GetComponent<OVRSkeleton>();
        TryBuildGizmos();
        UpdateLinePositions();
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

        if (boneLines.Count == skeleton.Bones.Count && skeleton.SkeletonChangedCount == lastSkeletonChangedCount) return;

        ClearGizmos();
        lastSkeletonChangedCount = skeleton.SkeletonChangedCount;

        IList<OVRBone> bones = skeleton.Bones;
        boneLines.Capacity = bones.Count;
        for (int i = 0; i < bones.Count; i++) boneLines.Add(null);

        for (int i = 0; i < bones.Count; i++)
        {
            OVRBone bone = bones[i];
            if (bone == null || bone.Transform == null) continue;
            int parentIndex = bone.ParentBoneIndex;
            if (parentIndex < 0 || parentIndex >= bones.Count) continue;

            OVRBone parent = bones[parentIndex];
            if (parent == null || parent.Transform == null) continue;

            GameObject lineObj = new GameObject("BoneLine_" + bone.Id);
            lineObj.transform.SetParent(transform, false);
            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(line);
            boneLines[i] = line;
        }
    }

    void UpdateVisibility()
    {
        bool ready = skeleton != null && skeleton.IsInitialized && skeleton.Bones != null && skeleton.Bones.Count > 0;
        if (ready) lastReadyTime = Time.time;

        // keep the gizmos alive briefly through small tracking gaps so they don't disappear after the first pinch
        bool show = visibleByDefault && (ready || (lastReadyTime > 0f && Time.time - lastReadyTime < 0.4f));
        for (int i = 0; i < boneLines.Count; i++)
        {
            if (boneLines[i] != null) boneLines[i].enabled = show;
        }
    }

    void ClearGizmos()
    {
        for (int i = 0; i < boneLines.Count; i++)
        {
            if (boneLines[i] != null) Destroy(boneLines[i].gameObject);
        }
        boneLines.Clear();
    }

    void OnDestroy()
    {
        ClearGizmos();
        if (gizmoMaterial != null) Destroy(gizmoMaterial);
    }

    void UpdateLinePositions()
    {
        if (skeleton == null || !skeleton.IsInitialized || skeleton.Bones == null) return;
        IList<OVRBone> bones = skeleton.Bones;
        for (int i = 0; i < bones.Count; i++)
        {
            LineRenderer line = boneLines.Count > i ? boneLines[i] : null;
            if (line == null) continue;

            int parentIndex = bones[i].ParentBoneIndex;
            if (parentIndex < 0 || parentIndex >= bones.Count) continue;

            OVRBone parent = bones[parentIndex];
            if (parent == null || parent.Transform == null) continue;

            Transform childTransform = bones[i].Transform;
            if (childTransform == null) continue;

            line.positionCount = 2;
            line.SetPosition(0, parent.Transform.position);
            line.SetPosition(1, childTransform.position);
        }
    }

    void ConfigureLineRenderer(LineRenderer line)
    {
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.useWorldSpace = true;
        line.loop = false;
        line.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        line.widthMultiplier = gizmoScale;
        line.numCapVertices = 0;
        line.numCornerVertices = 0;
        line.material = gizmoMaterial;
    }
}
