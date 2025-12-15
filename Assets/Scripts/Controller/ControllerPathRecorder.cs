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

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ControllerPathRecorder
{
    public Material pathMaterial;
    public Vector3 pathExtents = new Vector3(0.3f, 0.3f, 0.3f);
    public float minPathPointDistance = 0.003f;
    public float maxJumpDistance = 0.05f;
    public Color[] pathColors;

    const string PathsRootName = "paths";
    const float MinDistanceGuard = 0.0001f;
    const int HueCycle = 20;

    Transform ownerTransform;
    Transform pathsRoot;
    readonly List<PathData> paths = new List<PathData>();
    int currentPathIndex = -1;
    int colorIndex;
    int nextLocalPathId = -1;
    byte nextRequestId;
    bool isRecording;

    static Material fallbackMaterial;

    NetworkControllerManager networkManager;
    public NetworkControllerManager NetworkManager
    {
        get => networkManager;
        set => networkManager = value;
    }

    public void Initialize(Transform owner)
    {
        ownerTransform = owner;
        EnsurePathsRoot();
    }

    public void StartRecording(Vector3 percent)
    {
        EnsurePathsRoot();
        if (pathsRoot == null)
        {
            return;
        }

        isRecording = true;

        if (GetCurrentPath() == null)
        {
            BeginNewPath(percent);
        }
    }

    public void RecordPoint(Vector3 percent)
    {
        if (!isRecording)
        {
            return;
        }

        PathData path = GetCurrentPath();
        if (path == null)
        {
            BeginNewPath(percent);
            path = GetCurrentPath();
            if (path == null)
            {
                return;
            }
        }

        Vector3 localPoint = ConvertPercentToLocal(percent);
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
                BeginNewPath(percent);
                path = GetCurrentPath();
                if (path == null)
                {
                    return;
                }
                localPoint = ConvertPercentToLocal(percent);
            }
        }

        path.points.Add(localPoint);
        UpdateLine(path);
        SendPointToNetwork(path, localPoint, ControllerPathPointMarker.Continue);
    }

    public void StopRecording()
    {
        if (!isRecording)
        {
            return;
        }

        PathData path = GetCurrentPath();
        if (path != null && path.points.Count > 0)
        {
            FinishPath(path, path.points[path.points.Count - 1]);
        }

        currentPathIndex = -1;
        isRecording = false;
    }

    public void ClearPaths(bool notifyNetwork)
    {
        StopRecording();
        for (int i = 0; i < paths.Count; i++)
        {
            if (paths[i].line != null)
            {
                Object.Destroy(paths[i].line.gameObject);
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

    public void InitializeNetworkedPaths(IList<ControllerRecordedPathPoint> points)
    {
        ClearPaths(false);
        if (points == null)
        {
            return;
        }

        for (int i = 0; i < points.Count; i++)
        {
            ApplyNetworkPoint(points[i]);
        }
    }

    public void ApplyNetworkPoint(ControllerRecordedPathPoint point)
    {
        PathData path = FindPath(point.pathId);
        if (path != null && path.isLocal)
        {
            return;
        }

        switch (point.marker)
        {
            case ControllerPathPointMarker.Start:
                if (path == null)
                {
                    Color color = ResolveColor(point.color);
                    CreatePath(point.pathId, color, point.position, false);
                }
                break;

            case ControllerPathPointMarker.Continue:
                if (path == null)
                {
                    Color color = ResolveColor(point.color);
                    path = CreatePath(point.pathId, color, point.position, false);
                }
                AppendPoint(path, point.position);
                break;

            case ControllerPathPointMarker.End:
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

    void BeginNewPath(Vector3 percent)
    {
        Vector3 startPoint = ConvertPercentToLocal(percent);
        Color color = NextPathColor();
        bool isLocal = networkManager != null;

        PathData path = CreatePath(-1, color, startPoint, isLocal);
        if (path == null)
        {
            return;
        }

        currentPathIndex = paths.Count - 1;

        if (!isLocal)
        {
            path.id = nextLocalPathId--;
        }

        SendPointToNetwork(path, startPoint, ControllerPathPointMarker.Start);
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

        SendPointToNetwork(path, endPoint, ControllerPathPointMarker.End);
        currentPathIndex = -1;
    }

    void SendPointToNetwork(PathData path, Vector3 point, ControllerPathPointMarker marker)
    {
        if (networkManager == null || path == null)
        {
            return;
        }

        if (marker == ControllerPathPointMarker.Start)
        {
            BeginNetworkPath(path, point);
            return;
        }

        if (path.waitingForId)
        {
            path.pendingNetworkPoints.Add(point);
            if (marker == ControllerPathPointMarker.End)
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

        if (marker == ControllerPathPointMarker.Continue)
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
        if (pathsRoot == null)
        {
            return null;
        }

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

    void AppendPoint(PathData path, Vector3 point)
    {
        if (path == null)
        {
            return;
        }

        path.points.Add(point);
        UpdateLine(path);
    }

    void EnsurePathsRoot()
    {
        if (pathsRoot != null || ownerTransform == null)
        {
            return;
        }

        Transform existing = ownerTransform.Find(PathsRootName);
        if (existing != null)
        {
            pathsRoot = existing;
            return;
        }

        GameObject container = new GameObject(PathsRootName);
        container.transform.SetParent(ownerTransform, false);
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
            Color selected = pathColors[colorIndex % pathColors.Length];
            colorIndex = (colorIndex + 1) % pathColors.Length;
            return selected;
        }

        float hue = (colorIndex % HueCycle) / (float)HueCycle;
        colorIndex = (colorIndex + 1) % HueCycle;
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
            fallbackMaterial.name = "ControllerPathFallback";
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
