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
    [SerializeField] private float minPathPointDistance = 0.003f;
    [SerializeField] private float maxJumpDistance = 0.05f;

    [Header("Networking")]
    [SerializeField] private NetworkControlCubeManager networkManager;
    [SerializeField] private NetworkButtons buttonSync;

    [Header("Path Colors")]
    [SerializeField] private List<Color> pathColorPalette = new List<Color>();

    private const string PathsRootName = "paths";

    private Transform pathsRoot;
    private bool isCubeGrabbed;
    private bool isRecordingEnabled;
    private bool hasWarnedMissingMaterial;

    private static Material fallbackPathTemplate;

    private readonly Dictionary<int, PathRendererState> pathRenderers = new Dictionary<int, PathRendererState>();
    private readonly Dictionary<byte, PendingPathRequest> pendingPathRequests = new Dictionary<byte, PendingPathRequest>();
    private byte nextRequestId;
    private byte? activeRequestId;
    private bool suppressRecordButtonEvent;
    private int nextLocalPathId = -1;
    private const int PathColorCycleLength = 20;
    private const float PathColorHueStep = 1f / PathColorCycleLength;
    private const float PathColorAlignmentThreshold = 0.95f;
    private int nextPathColorIndex;

    private bool HasCustomPathPalette => pathColorPalette != null && pathColorPalette.Count > 0;

    private class PathRendererState
    {
        public readonly List<Vector3> points = new List<Vector3>();
        public LineRenderer lineRenderer;
        public bool isComplete;
    }

    private class PendingPathRequest
    {
        public int pathId = -1;
        public readonly List<Vector3> queuedPoints = new List<Vector3>();
        public bool endRequested;
        public Vector3 lastRecordedPoint;
    }

    public UnityEvent onPercentChangedEvent;

    public override void Awake()
    {
        base.Awake();
        if (networkManager == null)
        {
            networkManager = GetComponent<NetworkControlCubeManager>();
        }
        if (buttonSync == null)
        {
            buttonSync = GetComponent<NetworkButtons>();
            if (buttonSync == null)
            {
                Debug.LogWarning($"{nameof(ControlCubeDeviceInterface)} on {name} is missing a {nameof(NetworkButtons)} component for button syncing.", this);
            }
        }

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
        StopActiveRecording();
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

    public void AssignNetworkManager(NetworkControlCubeManager manager)
    {
        networkManager = manager;
    }

    public void InitializeNetworkedPaths(IList<ControlCubeRecordedPathPoint> points)
    {
        EnsurePathsRoot();
        DestroyAllPaths();
        nextLocalPathId = -1;
        pendingPathRequests.Clear();
        activeRequestId = null;

        if (points == null)
        {
            return;
        }

        foreach (ControlCubeRecordedPathPoint point in points)
        {
            ProcessRecordedPoint(point);
        }
    }

    public void HandleNetworkPathListChanged(SyncList<ControlCubeRecordedPathPoint>.Operation op, int index, ControlCubeRecordedPathPoint oldValue, ControlCubeRecordedPathPoint newValue)
    {
        switch (op)
        {
            case SyncList<ControlCubeRecordedPathPoint>.Operation.OP_ADD:
            case SyncList<ControlCubeRecordedPathPoint>.Operation.OP_INSERT:
            case SyncList<ControlCubeRecordedPathPoint>.Operation.OP_SET:
                ProcessRecordedPoint(newValue);
                break;
            case SyncList<ControlCubeRecordedPathPoint>.Operation.OP_REMOVEAT:
                RebuildAllPaths();
                break;
            case SyncList<ControlCubeRecordedPathPoint>.Operation.OP_CLEAR:
                DestroyAllPaths();
                break;
        }
    }

    public void ApplyNetworkRecordingState(bool newValue)
    {
        SetRecordingEnabled(newValue, fromNetwork: true, force: true);
    }

    public void HandleLocalPathConfirmed(byte requestId, int pathId)
    {
        OnPathConfirmed(requestId, pathId);
    }

    public void HandleRemotePathConfirmed(byte requestId, int pathId)
    {
        OnPathConfirmed(requestId, pathId);
    }

    private void OnPathConfirmed(byte requestId, int pathId)
    {
        if (!pendingPathRequests.TryGetValue(requestId, out PendingPathRequest request))
        {
            return;
        }

        request.pathId = pathId;

        if (request.queuedPoints.Count > 0 && networkManager != null)
        {
            foreach (Vector3 queuedPoint in request.queuedPoints)
            {
                networkManager.RequestAppendPathPoint(pathId, queuedPoint);
            }

            request.queuedPoints.Clear();
        }

        if (request.endRequested)
        {
            if (networkManager != null)
            {
                networkManager.RequestEndPath(pathId, request.lastRecordedPoint);
            }

            pendingPathRequests.Remove(requestId);

            if (activeRequestId.HasValue && activeRequestId.Value == requestId)
            {
                activeRequestId = null;
            }
        }
    }

    private void RebuildAllPaths()
    {
        DestroyAllPaths();
        nextLocalPathId = -1;

        if (networkManager == null)
        {
            return;
        }

        foreach (ControlCubeRecordedPathPoint point in networkManager.recordedPathPoints)
        {
            ProcessRecordedPoint(point);
        }
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
        StopActiveRecording();
    }

    private void OnRecordButtonToggleChanged()
    {
        if (suppressRecordButtonEvent)
        {
            return;
        }

        RequestRecordingToggle(recordButton != null && recordButton.isHit);
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
        SetRecordingEnabled(nextState, fromNetwork: false, force: force);
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

        if (!TryGetActiveRequest(out byte activeId, out PendingPathRequest activeRequest))
        {
            BeginNetworkedPath(localPoint);
            return;
        }

        if (force)
        {
            StopActiveRecording(activeId, activeRequest);
            BeginNetworkedPath(localPoint);
            return;
        }

        float minDistanceWorld = Mathf.Max(0.0001f, minPathPointDistance);
        float minDistanceSquared = minDistanceWorld * minDistanceWorld;
        bool jumpEnabled = maxJumpDistance > minDistanceWorld;
        float maxJumpSquared = jumpEnabled ? maxJumpDistance * maxJumpDistance : 0f;

        Vector3 lastWorldPoint = pathsRoot.TransformPoint(activeRequest.lastRecordedPoint);
        Vector3 currentWorldPoint = pathsRoot.TransformPoint(localPoint);
        float segmentDistanceSquared = Vector3.SqrMagnitude(lastWorldPoint - currentWorldPoint);

        if (jumpEnabled && segmentDistanceSquared >= maxJumpSquared)
        {
            StopActiveRecording(activeId, activeRequest);
            BeginNetworkedPath(localPoint);
            return;
        }

        if (segmentDistanceSquared < minDistanceSquared)
        {
            return;
        }

        AppendPointToNetwork(activeId, activeRequest, localPoint);
    }

    private bool TryGetActiveRequest(out byte requestId, out PendingPathRequest request)
    {
        requestId = default;
        request = null;

        if (activeRequestId.HasValue && pendingPathRequests.TryGetValue(activeRequestId.Value, out request))
        {
            requestId = activeRequestId.Value;
            return true;
        }

        return false;
    }

    private void BeginNetworkedPath(Vector3 startPoint)
    {
        EnsurePathsRoot();
        if (pathsRoot == null)
        {
            return;
        }

        Color pathColor = GeneratePathColor();
        Color32 color32 = (Color32)pathColor;

        byte requestId = nextRequestId++;
        PendingPathRequest request = new PendingPathRequest
        {
            lastRecordedPoint = startPoint
        };

        pendingPathRequests[requestId] = request;
        activeRequestId = requestId;

        if (networkManager == null)
        {
            int localPathId = nextLocalPathId--;
            request.pathId = localPathId;
            CreatePathRenderer(localPathId, pathColor, startPoint);
            return;
        }

        networkManager.CmdBeginPath(requestId, startPoint, color32);
    }

    private void AppendPointToNetwork(byte requestId, PendingPathRequest request, Vector3 point)
    {
        request.lastRecordedPoint = point;

        if (networkManager == null)
        {
            if (pathRenderers.TryGetValue(request.pathId, out PathRendererState state))
            {
                AppendPointToRenderer(state, point);
            }
            return;
        }

        if (request.pathId >= 0)
        {
            networkManager.RequestAppendPathPoint(request.pathId, point);
        }
        else
        {
            request.queuedPoints.Add(point);
        }
    }

    private void RequestRecordingToggle(bool desiredState)
    {
        SetRecordingEnabled(desiredState, fromNetwork: false);

        if (networkManager != null)
        {
            networkManager.CmdSetRecordingState(desiredState);
        }
    }

    private void SetRecordingEnabled(bool enabled, bool fromNetwork, bool force = false)
    {
        if (!force && isRecordingEnabled == enabled)
        {
            return;
        }

        isRecordingEnabled = enabled;

        if (fromNetwork && recordButton != null)
        {
            suppressRecordButtonEvent = true;
            recordButton.keyHit(enabled, false);
            suppressRecordButtonEvent = false;
        }

        if (!isRecordingEnabled)
        {
            StopActiveRecording();
        }
        else if (isCubeGrabbed)
        {
            TryRecordPoint(force: true);
        }
    }

    private void StopActiveRecording()
    {
        if (TryGetActiveRequest(out byte requestId, out PendingPathRequest request))
        {
            StopActiveRecording(requestId, request);
        }
    }

    private void StopActiveRecording(byte requestId, PendingPathRequest request)
    {
        if (networkManager != null)
        {
            if (request.pathId >= 0)
            {
                networkManager.RequestEndPath(request.pathId, request.lastRecordedPoint);
                pendingPathRequests.Remove(requestId);
            }
            else
            {
                request.endRequested = true;
            }
        }
        else
        {
            if (pathRenderers.TryGetValue(request.pathId, out PathRendererState state))
            {
                CompletePathRenderer(state, request.lastRecordedPoint);
            }
            pendingPathRequests.Remove(requestId);
        }

        if (activeRequestId.HasValue && activeRequestId.Value == requestId)
        {
            activeRequestId = null;
        }
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

    private void ClearPaths()
    {
        StopActiveRecording();
        DestroyAllPaths();
        pendingPathRequests.Clear();
        activeRequestId = null;

        if (networkManager != null)
        {
            networkManager.CmdClearPaths();
        }
    }

    private void DestroyAllPaths()
    {
        if (pathsRoot != null)
        {
            for (int i = pathsRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(pathsRoot.GetChild(i).gameObject);
            }
        }

        pathRenderers.Clear();
        nextLocalPathId = -1;
        nextPathColorIndex = 0;
    }

    private void ProcessRecordedPoint(ControlCubeRecordedPathPoint point)
    {
        switch (point.marker)
        {
            case ControlCubePathPointMarker.Start:
                {
                    Color pathColor;
                    if (IsColorUninitialized(point.color))
                    {
                        pathColor = GeneratePathColor();
                    }
                    else
                    {
                        pathColor = point.color;
                        AlignNextPathColorIndex(pathColor);
                    }

                    CreatePathRenderer(point.pathId, pathColor, point.position);
                    break;
                }
            case ControlCubePathPointMarker.Continue:
                {
                    if (!pathRenderers.TryGetValue(point.pathId, out PathRendererState state))
                    {
                        Color fallbackColor = GeneratePathColor();
                        state = CreatePathRenderer(point.pathId, fallbackColor, point.position);
                    }

                    AppendPointToRenderer(state, point.position);
                    break;
                }
            case ControlCubePathPointMarker.End:
                {
                    if (pathRenderers.TryGetValue(point.pathId, out PathRendererState state))
                    {
                        CompletePathRenderer(state, point.position);
                    }
                    break;
                }
        }
    }

    private PathRendererState CreatePathRenderer(int pathId, Color pathColor, Vector3 startPoint)
    {
        EnsurePathsRoot();
        if (pathsRoot == null)
        {
            return null;
        }

        if (pathRenderers.TryGetValue(pathId, out PathRendererState existing) && existing?.lineRenderer != null)
        {
            Destroy(existing.lineRenderer.gameObject);
            pathRenderers.Remove(pathId);
        }

        if (pathBaseMaterial != null)
        {
            hasWarnedMissingMaterial = false;
        }

        GameObject pathObject = new GameObject($"path_{pathId}");
        pathObject.transform.SetParent(pathsRoot, false);

        LineRenderer lineRenderer = pathObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = false;
        lineRenderer.widthMultiplier = 0.00225f;
        lineRenderer.numCapVertices = 2;
        lineRenderer.numCornerVertices = 2;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.allowOcclusionWhenDynamic = false;

        Material template = GetPathTemplateMaterial();
        Material materialInstance = new Material(template);
        ApplyColorToMaterial(materialInstance, pathColor);
        lineRenderer.material = materialInstance;
        lineRenderer.startColor = pathColor;
        lineRenderer.endColor = pathColor;

        ControlCubeRecordedPath lifecycle = pathObject.AddComponent<ControlCubeRecordedPath>();
        lifecycle.materialInstance = materialInstance;

        PathRendererState state = new PathRendererState
        {
            lineRenderer = lineRenderer
        };

        state.points.Add(startPoint);
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, startPoint);

        pathRenderers[pathId] = state;
        return state;
    }

    private void AppendPointToRenderer(PathRendererState state, Vector3 point)
    {
        if (state == null || state.lineRenderer == null)
        {
            return;
        }

        state.points.Add(point);
        state.lineRenderer.positionCount = state.points.Count;
        state.lineRenderer.SetPosition(state.points.Count - 1, point);
    }

    private void CompletePathRenderer(PathRendererState state, Vector3 point)
    {
        if (state == null || state.isComplete)
        {
            return;
        }

        if (state.points.Count == 0 || Vector3.SqrMagnitude(state.points[state.points.Count - 1] - point) > 1e-8f)
        {
            AppendPointToRenderer(state, point);
        }

        state.isComplete = true;
    }

    private Color GeneratePathColor()
    {
        if (HasCustomPathPalette)
        {
            if (nextPathColorIndex >= pathColorPalette.Count)
            {
                nextPathColorIndex = 0;
            }

            Color paletteColor = pathColorPalette[nextPathColorIndex];
            nextPathColorIndex = (nextPathColorIndex + 1) % pathColorPalette.Count;
            return paletteColor;
        }

        float hue = nextPathColorIndex * PathColorHueStep;
        nextPathColorIndex = (nextPathColorIndex + 1) % PathColorCycleLength;
        return Color.HSVToRGB(hue, 1f, 1f);
    }

    private void AlignNextPathColorIndex(Color color)
    {
        if (HasCustomPathPalette)
        {
            int paletteIndex = FindPaletteIndex((Color32)color);
            if (paletteIndex >= 0)
            {
                nextPathColorIndex = (paletteIndex + 1) % pathColorPalette.Count;
            }
            return;
        }

        Color.RGBToHSV(color, out float hue, out float saturation, out float value);

        if (saturation < PathColorAlignmentThreshold || value < PathColorAlignmentThreshold)
        {
            return;
        }

        float normalizedHue = Mathf.Repeat(hue, 1f);
        int derivedIndex = Mathf.RoundToInt(normalizedHue / PathColorHueStep) % PathColorCycleLength;
        nextPathColorIndex = (derivedIndex + 1) % PathColorCycleLength;
    }

    private int FindPaletteIndex(Color32 color)
    {
        if (!HasCustomPathPalette)
        {
            return -1;
        }

        for (int i = 0; i < pathColorPalette.Count; i++)
        {
            Color32 paletteColor = (Color32)pathColorPalette[i];
            if (paletteColor.r == color.r &&
                paletteColor.g == color.g &&
                paletteColor.b == color.b &&
                paletteColor.a == color.a)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsColorUninitialized(Color32 color)
    {
        return color.a == 0 && color.r == 0 && color.g == 0 && color.b == 0;
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
