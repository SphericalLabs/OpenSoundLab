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
    [SerializeField] private float maxJumpDistance = 0.05f;

    private const string PathsRootName = "paths";

    private Transform pathsRoot;
    private LineRenderer activeLineRenderer;
    private List<Vector3> activePathPoints;
    private Material activeMaterialInstance;
    private bool isCubeGrabbed;
    private bool isRecordingEnabled;
    private bool hasWarnedMissingMaterial;

    private static Material fallbackPathTemplate;

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
        RefreshRecordState(force: true);
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
            ConfigureRecordButton(recordButton);
            recordButton.onToggleChangedEvent.AddListener(OnRecordButtonToggleChanged);
        }

        if (deleteButton != null)
        {
            deleteButton.onStartGrabEvents.AddListener(OnDeleteButtonPressed);
        }
    }

    private void UnsubscribeButtonEvents()
    {
        if (recordButton != null && recordButton.onToggleChangedEvent != null)
        {
            recordButton.onToggleChangedEvent.RemoveListener(OnRecordButtonToggleChanged);
        }

        if (deleteButton != null)
        {
            deleteButton.onStartGrabEvents.RemoveListener(OnDeleteButtonPressed);
        }
    }

    private void ConfigureRecordButton(button target)
    {
        target.isSwitch = true;
        target.onlyOn = false;

        if (target.onToggleChangedEvent == null)
        {
            target.onToggleChangedEvent = new UnityEvent();
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
        TryRecordPoint(force: true);
    }

    private void OnCubeReleased()
    {
        isCubeGrabbed = false;
        StopActivePath();
    }

    private void OnRecordButtonToggleChanged()
    {
        RefreshRecordState(force: true);
    }

    private void OnDeleteButtonPressed()
    {
        if (deleteButton != null && deleteButton.isHit)
        {
            ClearPaths();
        }
    }

    private void RefreshRecordState(bool force = false)
    {
        bool nextState = recordButton != null && recordButton.isHit;
        if (!force && isRecordingEnabled == nextState)
        {
            return;
        }

        isRecordingEnabled = nextState;

        if (!isRecordingEnabled)
        {
            StopActivePath();
        }
        else if (isCubeGrabbed)
        {
            TryRecordPoint(force: true);
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

        Vector3 localPoint = ConvertPercentToLocal(percent);

        if (force || activePathPoints == null)
        {
            BeginNewPath(localPoint);
            return;
        }

        Vector3 lastPoint = activePathPoints[activePathPoints.Count - 1];
        float minDistance = Mathf.Max(0.0001f, minPathPointDistance);
        float minDistanceSquared = minDistance * minDistance;
        bool jumpEnabled = maxJumpDistance > minDistance;
        float maxJumpSquared = jumpEnabled ? maxJumpDistance * maxJumpDistance : 0f;
        float segmentDistanceSquared = Vector3.SqrMagnitude(lastPoint - localPoint);

        if (jumpEnabled && segmentDistanceSquared >= maxJumpSquared)
        {
            BeginNewPath(localPoint);
            return;
        }

        if (segmentDistanceSquared < minDistanceSquared)
        {
            return;
        }

        AppendPoint(localPoint);
    }

    private void BeginNewPath(Vector3 startPoint)
    {
        EnsurePathsRoot();

        if (pathsRoot == null)
        {
            return;
        }

        StopActivePath();
        if (pathBaseMaterial != null)
        {
            hasWarnedMissingMaterial = false;
        }

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

        Material template = GetPathTemplateMaterial();
        activeMaterialInstance = new Material(template);
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

    private Vector3 ConvertPercentToLocal(Vector3 value)
    {
        float xPercent = Mathf.Clamp01(value.x);
        float yPercent = Mathf.Clamp01(value.y);
        float zPercent = Mathf.Clamp01(value.z);

        float x = (0.5f - xPercent) * pathExtents.x;
        float y = yPercent * pathExtents.y;
        float z = (0.5f - zPercent) * pathExtents.z;

        return new Vector3(x, y, z);
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

    private Material GetPathTemplateMaterial()
    {
        if (pathBaseMaterial != null)
        {
            return pathBaseMaterial;
        }

        WarnMissingMaterial();

        if (fallbackPathTemplate == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Hidden/Internal-Colored");
            }

            fallbackPathTemplate = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
            fallbackPathTemplate.name = "ControlCubePathFallbackTemplate";
            fallbackPathTemplate.hideFlags = HideFlags.HideAndDontSave;
        }

        return fallbackPathTemplate;
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
