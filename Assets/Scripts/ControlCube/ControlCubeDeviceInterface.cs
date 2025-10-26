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
using UnityEngine.Events;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

public class ControlCubeDeviceInterface : deviceInterface
{
    public Vector3 percent;
    public ControlCubeSingleSignalGenerator[] signals;
    public omniJack[] outputs;
    public cubeZone cubeManip;

    [Header("Recording")]
    [SerializeField] private button recordButton;
    [SerializeField] private button deleteButton;
    [SerializeField] private Material pathBaseMaterial;
    [SerializeField] private Vector3 pathExtents = new Vector3(0.3f, 0.3f, 0.3f);
    [SerializeField] private float minPathPointDistance = 0.005f;

    private const string PathsRootName = "paths";

    private Transform pathsRoot;
    private LineRenderer activeLineRenderer;
    private List<Vector3> activePathPoints;
    private Material activeMaterialInstance;
    private bool isCubeGrabbed;
    private bool isRecordingEnabled;
    private bool hasWarnedMissingMaterial;

    public UnityEvent onPercentChangedEvent;

    public override void Awake()
    {
        base.Awake();
        EnsurePathsRoot();
        Setup(percent);
    }

    private void OnEnable()
    {
        EnsurePathsRoot();
        SubscribeButtonEvents();
        SubscribeCubeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeButtonEvents();
        UnsubscribeCubeEvents();
        StopActivePath();
    }

    public void Setup(Vector3 p)
    {
        percent = p;
        cubeManip.updateLines(percent);
        updatePercent(percent);
        //toggleMute(true);
    }

    public override void hit(bool on, int ID = -1)
    {
    }

    private void SubscribeButtonEvents()
    {
        if (recordButton != null)
        {
            recordButton.onToggleChangedEvent.AddListener(OnRecordButtonToggled);
        }

        if (deleteButton != null)
        {
            deleteButton.onToggleChangedEvent.AddListener(OnDeleteButtonToggled);
        }
    }

    private void UnsubscribeButtonEvents()
    {
        if (recordButton != null)
        {
            recordButton.onToggleChangedEvent.RemoveListener(OnRecordButtonToggled);
        }

        if (deleteButton != null)
        {
            deleteButton.onToggleChangedEvent.RemoveListener(OnDeleteButtonToggled);
        }
    }

    private void SubscribeCubeEvents()
    {
        if (cubeManip != null)
        {
            cubeManip.onStartGrabEvents.AddListener(OnCubeGrabbed);
            cubeManip.onEndGrabEvents.AddListener(OnCubeReleased);
        }
    }

    private void UnsubscribeCubeEvents()
    {
        if (cubeManip != null)
        {
            cubeManip.onStartGrabEvents.RemoveListener(OnCubeGrabbed);
            cubeManip.onEndGrabEvents.RemoveListener(OnCubeReleased);
        }
    }

    private void OnCubeGrabbed()
    {
        isCubeGrabbed = true;
        TryRecordPoint(true);
    }

    private void OnCubeReleased()
    {
        isCubeGrabbed = false;
        StopActivePath();
    }

    private void OnRecordButtonToggled()
    {
        bool nextState = recordButton != null && recordButton.isHit;
        if (isRecordingEnabled == nextState)
        {
            return;
        }

        isRecordingEnabled = nextState;

        if (!isRecordingEnabled)
        {
            StopActivePath();
        }
        else
        {
            TryRecordPoint(true);
        }
    }

    private void OnDeleteButtonToggled()
    {
        if (deleteButton != null && deleteButton.isHit)
        {
            ClearPaths();
        }
    }

    void Update()
    {
    }

    public void updatePercent(Vector3 p, bool invokeChange = false)
    {
        percent = p;

        signals[0].value = (percent.x - .5f) * 2;
        signals[1].value = (percent.y - .5f) * 2;
        signals[2].value = (percent.z - .5f) * 2;

        if (invokeChange)
        {
            TryRecordPoint();
            onPercentChangedEvent.Invoke();
        }
    }

    private bool CanRecord => isRecordingEnabled && isCubeGrabbed && pathsRoot != null;

    private void TryRecordPoint(bool force = false)
    {
        if (!CanRecord)
        {
            return;
        }

        if (pathBaseMaterial == null)
        {
            WarnMissingMaterial();
            return;
        }

        Vector3 localPoint = ConvertPercentToLocal(percent);

        if (activePathPoints == null)
        {
            BeginNewPath(localPoint);
            return;
        }

        Vector3 lastPoint = activePathPoints[activePathPoints.Count - 1];
        float minDistance = Mathf.Max(0.0001f, minPathPointDistance);
        float minDistanceSquared = minDistance * minDistance;

        if (force || Vector3.SqrMagnitude(lastPoint - localPoint) >= minDistanceSquared)
        {
            AppendPoint(localPoint);
        }
    }

    private void BeginNewPath(Vector3 startPoint)
    {
        EnsurePathsRoot();

        if (pathsRoot == null)
        {
            return;
        }

        hasWarnedMissingMaterial = false;

        GameObject pathObject = new GameObject($"path_{pathsRoot.childCount}");
        pathObject.transform.SetParent(pathsRoot, false);

        activeLineRenderer = pathObject.AddComponent<LineRenderer>();
        activeLineRenderer.useWorldSpace = false;
        activeLineRenderer.loop = false;
        activeLineRenderer.widthMultiplier = 0.0025f;
        activeLineRenderer.numCapVertices = 2;
        activeLineRenderer.numCornerVertices = 2;
        activeLineRenderer.textureMode = LineTextureMode.Stretch;
        activeLineRenderer.alignment = LineAlignment.View;
        activeLineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        activeLineRenderer.receiveShadows = false;
        activeLineRenderer.allowOcclusionWhenDynamic = false;
        activeLineRenderer.positionCount = 0;

        activeMaterialInstance = new Material(pathBaseMaterial);
        Color pathColor = Random.ColorHSV(0f, 1f, 0.55f, 1f, 0.8f, 1f);
        ApplyColorToMaterial(activeMaterialInstance, pathColor);
        activeLineRenderer.material = activeMaterialInstance;
        activeLineRenderer.startColor = pathColor;
        activeLineRenderer.endColor = pathColor;

        ControlCubeRecordedPath pathLifecycle = pathObject.AddComponent<ControlCubeRecordedPath>();
        pathLifecycle.materialInstance = activeMaterialInstance;

        activePathPoints = new List<Vector3>();
        AppendPoint(startPoint);
    }

    private void AppendPoint(Vector3 point)
    {
        if (activeLineRenderer == null || activePathPoints == null)
        {
            return;
        }

        activePathPoints.Add(point);
        activeLineRenderer.positionCount = activePathPoints.Count;
        activeLineRenderer.SetPosition(activePathPoints.Count - 1, point);
    }

    private Vector3 ConvertPercentToLocal(Vector3 p)
    {
        return new Vector3(
            (p.x - 0.5f) * pathExtents.x,
            (p.y - 0.5f) * pathExtents.y,
            (p.z - 0.5f) * pathExtents.z
        );
    }

    private void StopActivePath()
    {
        activeLineRenderer = null;
        activePathPoints = null;
        activeMaterialInstance = null;
    }

    private void ClearPaths()
    {
        StopActivePath();

        if (pathsRoot == null)
        {
            return;
        }

        for (int i = pathsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(pathsRoot.GetChild(i).gameObject);
        }
    }

    private void EnsurePathsRoot()
    {
        if (pathsRoot != null)
        {
            return;
        }

        Transform existing = transform.Find(PathsRootName);
        if (existing != null)
        {
            pathsRoot = existing;
        }
        else
        {
            GameObject container = new GameObject(PathsRootName);
            container.transform.SetParent(transform, false);
            pathsRoot = container.transform;
        }
    }

    private void WarnMissingMaterial()
    {
        if (hasWarnedMissingMaterial)
        {
            return;
        }

        hasWarnedMissingMaterial = true;
        Debug.LogWarning($"{nameof(ControlCubeDeviceInterface)} on {name} is missing a Path Base Material reference.", this);
    }

    private static void ApplyColorToMaterial(Material material, Color color)
    {
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    public override InstrumentData GetData()
    {
        ControlCubeData data = new ControlCubeData();

        data.deviceType = DeviceType.ControlCube;
        GetTransformData(data);

        data.jackOutID = new int[4];
        for (int i = 0; i < 3; i++) data.jackOutID[i] = outputs[i].transform.GetInstanceID();

        data.dimensionValues = new float[3];
        for (int i = 0; i < 3; i++) data.dimensionValues[i] = percent[i];

        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        ControlCubeData data = d as ControlCubeData;

        base.Load(data, copyMode);

        for (int i = 0; i < 3; i++) outputs[i].SetID(data.jackOutID[i], copyMode);
        for (int i = 0; i < 3; i++) percent[i] = data.dimensionValues[i];

        Setup(percent);
    }
}

class ControlCubeRecordedPath : MonoBehaviour
{
    public Material materialInstance;

    private void OnDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}

public class ControlCubeData : InstrumentData
{
    public int[] jackOutID;
    public float[] dimensionValues;
}
