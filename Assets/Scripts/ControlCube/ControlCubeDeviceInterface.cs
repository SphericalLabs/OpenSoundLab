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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ControlCubeDeviceInterface : deviceInterface
{
    public Vector3 percent;
    public ControlCubeSingleSignalGenerator[] signals;
    public omniJack[] outputs;
    public cubeZone cubeManip;

    public button recordButton;
    public button deleteButton;
    public Material pathMaterial;
    public Vector3 pathExtents = new Vector3(0.3f, 0.3f, 0.3f);
    public float minPathPointDistance = 0.003f;
    public float maxJumpDistance = 0.05f;
    public Color[] pathColors;

    public NetworkControlCubeManager networkManager;

    public UnityEvent onPercentChangedEvent;

    const string PathsRootName = "paths";
    const float MinDistanceGuard = 0.0001f;

    Transform pathsRoot;
    readonly List<PathData> paths = new List<PathData>();
    int currentPathIndex = -1;
    bool cubeGrabbed;
    int colorIndex;
    int nextLocalPathId = -1;
    byte nextRequestId;
    static Material fallbackMaterial;

    public override void Awake()
    {
        base.Awake();
        EnsurePathsRoot();
        Setup(percent);
        ConfigureButtons();
    }

    void OnDisable()
    {
        StopRecording();
    }

    void Update()
    {
        bool grabbedNow = cubeManip != null && cubeManip.curState == manipObject.manipState.grabbed;
        if (grabbedNow != cubeGrabbed)
        {
            cubeGrabbed = grabbedNow;
            if (!cubeGrabbed)
            {
                StopRecording();
            }
            else if (ShouldRecord())
            {
                BeginNewPath(percent);
            }
        }
    }

    public override void hit(bool on, int ID = -1)
    {
        if (recordButton != null && ID == recordButton.buttonID)
        {
            HandleRecordHit(on);
            return;
        }

        if (deleteButton != null && ID == deleteButton.buttonID && on)
        {
            ClearPaths(true);
        }
    }

    public void AssignNetworkManager(NetworkControlCubeManager manager)
    {
        networkManager = manager;
    }

    public void InitializeNetworkedPaths(IList<ControlCubeRecordedPathPoint> list)
    {
        ClearPaths(false);
        if (list == null)
        {
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            ApplyNetworkPoint(list[i]);
        }
    }

    public void ApplyNetworkPoint(ControlCubeRecordedPathPoint point)
    {
        PathData path = FindPath(point.pathId);
        if (path != null && path.isLocal)
        {
            return;
        }

        switch (point.marker)
        {
            case ControlCubePathPointMarker.Start:
                if (path == null)
                {
                    Color color = ResolveColor(point.color);
                    CreatePath(point.pathId, color, point.position, false);
                }
                break;

            case ControlCubePathPointMarker.Continue:
                if (path == null)
                {
                    Color color = ResolveColor(point.color);
                    path = CreatePath(point.pathId, color, point.position, false);
                }
                AppendPoint(path, point.position);
                break;

            case ControlCubePathPointMarker.End:
                if (path == null)
                {
                    Color color = ResolveColor(point.color);
                    path = CreatePath(point.pathId, color, point.position, false);
                }
                AppendPoint(path, point.position);
                path.isClosed = true;
                break;
        }
    }

    public void HandlePathConfirmed(byte requestId, int pathId)
    {
        if (networkManager == null)
        {
            return;
        }

        PathData path = FindPendingPath(requestId);
        if (path == null)
        {
            return;
        }

        path.id = pathId;
        path.waitingForId = false;

        for (int i = 0; i < path.pendingNetworkPoints.Count; i++)
        {
            networkManager.CmdAppendPathPoint(pathId, path.pendingNetworkPoints[i]);
        }

        if (path.pendingEnd)
        {
            networkManager.CmdEndPath(pathId, path.pendingEndPoint);
        }

        path.pendingNetworkPoints.Clear();
        path.pendingEnd = false;
    }

    public void Setup(Vector3 p)
    {
        percent = p;
        cubeManip.updateLines(percent);
        updatePercent(percent);
    }

    public void updatePercent(Vector3 p, bool invokeChange = false)
    {
        percent = p;

        signals[0].value = (percent.x - .5f) * 2;
        signals[1].value = (percent.y - .5f) * 2;
        signals[2].value = (percent.z - .5f) * 2;

        if (invokeChange)
        {
            RecordPoint(percent);
            onPercentChangedEvent.Invoke();
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

    void HandleRecordHit(bool on)
    {
        if (!on)
        {
            StopRecording();
            return;
        }

        if (cubeGrabbed)
        {
            BeginNewPath(percent);
        }
    }

    void RecordPoint(Vector3 value)
    {
        if (!ShouldRecord())
        {
            return;
        }

        PathData path = GetCurrentPath();
        if (path == null)
        {
            BeginNewPath(value);
            path = GetCurrentPath();
            if (path == null)
            {
                return;
            }
        }

        Vector3 localPoint = ConvertPercentToLocal(value);
        if (path.points.Count > 0)
        {
            Vector3 previous = path.points[path.points.Count - 1];
            float distance = (previous - localPoint).sqrMagnitude;
            float minDistance = Mathf.Max(MinDistanceGuard, minPathPointDistance);
            if (distance < minDistance * minDistance)
            {
                return;
            }

            float jumpLimit = maxJumpDistance > minDistance ? maxJumpDistance : 0f;
            if (jumpLimit > 0f && distance > jumpLimit * jumpLimit)
            {
                FinishPath(path, previous);
                BeginNewPath(value);
                path = GetCurrentPath();
                if (path == null)
                {
                    return;
                }
                localPoint = ConvertPercentToLocal(value);
            }
        }

        path.points.Add(localPoint);
        UpdateLine(path);
        SendPointToNetwork(path, localPoint, ControlCubePathPointMarker.Continue);
    }

    void BeginNewPath(Vector3 value)
    {
        Vector3 startPoint = ConvertPercentToLocal(value);
        Color color = NextPathColor();
        bool isLocal = networkManager != null;

        PathData path = CreatePath(-1, color, startPoint, isLocal);
        currentPathIndex = paths.Count - 1;

        if (!isLocal)
        {
            path.id = nextLocalPathId--;
        }

        SendPointToNetwork(path, startPoint, ControlCubePathPointMarker.Start);
    }

    void FinishPath(PathData path, Vector3 endPoint)
    {
        if (path == null || path.points.Count == 0)
        {
            return;
        }

        if ((path.points[path.points.Count - 1] - endPoint).sqrMagnitude > MinDistanceGuard)
        {
            path.points.Add(endPoint);
            UpdateLine(path);
        }

        SendPointToNetwork(path, endPoint, ControlCubePathPointMarker.End);
        currentPathIndex = -1;
    }

    void StopRecording()
    {
        PathData path = GetCurrentPath();
        if (path != null && path.points.Count > 0)
        {
            FinishPath(path, path.points[path.points.Count - 1]);
        }
        currentPathIndex = -1;
    }

    void ClearPaths(bool notifyNetwork)
    {
        StopRecording();
        for (int i = 0; i < paths.Count; i++)
        {
            if (paths[i].line != null)
            {
                Destroy(paths[i].line.gameObject);
            }
        }
        paths.Clear();
        currentPathIndex = -1;
        nextLocalPathId = -1;
        colorIndex = 0;

        if (notifyNetwork && networkManager != null)
        {
            networkManager.CmdClearPaths();
        }
    }

    void SendPointToNetwork(PathData path, Vector3 point, ControlCubePathPointMarker marker)
    {
        if (networkManager == null)
        {
            return;
        }

        if (marker == ControlCubePathPointMarker.Start)
        {
            BeginNetworkPath(path, point);
            return;
        }

        if (path.waitingForId)
        {
            path.pendingNetworkPoints.Add(point);
            if (marker == ControlCubePathPointMarker.End)
            {
                path.pendingEnd = true;
                path.pendingEndPoint = point;
            }
            return;
        }

        if (path.id < 0)
        {
            return;
        }

        if (marker == ControlCubePathPointMarker.Continue)
        {
            networkManager.CmdAppendPathPoint(path.id, point);
        }
        else
        {
            networkManager.CmdEndPath(path.id, point);
        }
    }

    void BeginNetworkPath(PathData path, Vector3 startPoint)
    {
        if (networkManager == null)
        {
            return;
        }

        path.waitingForId = true;
        path.requestId = ++nextRequestId;
        path.pendingNetworkPoints.Clear();
        path.pendingEnd = false;
        path.pendingEndPoint = startPoint;

        networkManager.CmdBeginPath(path.requestId, startPoint, (Color32)path.color);
    }

    PathData CreatePath(int pathId, Color color, Vector3 startPoint, bool localOwner)
    {
        EnsurePathsRoot();

        GameObject go = new GameObject(pathId >= 0 ? $"path_{pathId}" : "path_local");
        go.transform.SetParent(pathsRoot, false);

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = false;
        line.widthMultiplier = 0.0025f;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.textureMode = LineTextureMode.Stretch;
        line.alignment = LineAlignment.View;
        line.material = ResolveMaterial();
        line.startColor = color;
        line.endColor = color;

        PathData data = new PathData
        {
            id = pathId,
            line = line,
            color = color,
            isLocal = localOwner
        };

        data.points.Add(startPoint);
        UpdateLine(data);
        paths.Add(data);
        return data;
    }

    void UpdateLine(PathData path)
    {
        if (path == null || path.line == null)
        {
            return;
        }

        path.line.positionCount = path.points.Count;
        for (int i = 0; i < path.points.Count; i++)
        {
            path.line.SetPosition(i, path.points[i]);
        }
    }

    bool ShouldRecord()
    {
        return recordButton != null && recordButton.isHit && cubeGrabbed;
    }

    void ConfigureButtons()
    {
        ConfigureButton(recordButton, true);
        ConfigureButton(deleteButton, false);
    }

    static void ConfigureButton(button target, bool isSwitch)
    {
        if (target == null)
        {
            return;
        }

        target.isSwitch = isSwitch;
        target.onlyOn = false;
    }

    void EnsurePathsRoot()
    {
        if (pathsRoot != null)
        {
            return;
        }

        Transform existing = transform.Find(PathsRootName);
        if (existing != null)
        {
            pathsRoot = existing;
            return;
        }

        GameObject container = new GameObject(PathsRootName);
        container.transform.SetParent(transform, false);
        pathsRoot = container.transform;
    }

    Vector3 ConvertPercentToLocal(Vector3 value)
    {
        float x = (0.5f - Mathf.Clamp01(value.x)) * pathExtents.x;
        float y = Mathf.Clamp01(value.y) * pathExtents.y;
        float z = (0.5f - Mathf.Clamp01(value.z)) * pathExtents.z;
        return new Vector3(x, y, z);
    }

    Color NextPathColor()
    {
        if (pathColors != null && pathColors.Length > 0)
        {
            Color result = pathColors[colorIndex % pathColors.Length];
            colorIndex = (colorIndex + 1) % pathColors.Length;
            return result;
        }

        float hue = (colorIndex % 20) / 20f;
        colorIndex = (colorIndex + 1) % 20;
        return Color.HSVToRGB(hue, 0.85f, 1f);
    }

    Color ResolveColor(Color32 color)
    {
        if (color.a == 0 && color.r == 0 && color.g == 0 && color.b == 0)
        {
            return NextPathColor();
        }

        return color;
    }

    Material ResolveMaterial()
    {
        if (pathMaterial != null)
        {
            return pathMaterial;
        }

        if (fallbackMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            fallbackMaterial = new Material(shader);
            fallbackMaterial.name = "ControlCubePathFallback";
        }

        return fallbackMaterial;
    }

    PathData GetCurrentPath()
    {
        if (currentPathIndex < 0 || currentPathIndex >= paths.Count)
        {
            return null;
        }

        return paths[currentPathIndex];
    }

    PathData FindPath(int pathId)
    {
        for (int i = 0; i < paths.Count; i++)
        {
            if (paths[i].id == pathId)
            {
                return paths[i];
            }
        }
        return null;
    }

    PathData FindPendingPath(byte requestId)
    {
        for (int i = 0; i < paths.Count; i++)
        {
            if (paths[i].waitingForId && paths[i].requestId == requestId)
            {
                return paths[i];
            }
        }
        return null;
    }

    void AppendPoint(PathData path, Vector3 point)
    {
        if (path == null)
        {
            return;
        }

        path.points.Add(point);
        UpdateLine(path);
    }
}

public class ControlCubeData : InstrumentData
{
    public int[] jackOutID;
    public float[] dimensionValues;
}

class PathData
{
    public int id = -1;
    public LineRenderer line;
    public readonly List<Vector3> points = new List<Vector3>();
    public Color color = Color.white;
    public bool isLocal;
    public bool isClosed;

    public bool waitingForId;
    public byte requestId;
    public readonly List<Vector3> pendingNetworkPoints = new List<Vector3>();
    public bool pendingEnd;
    public Vector3 pendingEndPoint;
}
