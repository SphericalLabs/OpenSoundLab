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

using Mirror;
using System.Collections.Generic;
using UnityEngine;

public enum ControlCubePathPointMarker : byte
{
    Start = 0,
    Continue = 1,
    End = 2
}

[System.Serializable]
public struct ControlCubeRecordedPathPoint
{
    public int pathId;
    public Vector3 position;
    public ControlCubePathPointMarker marker;
    public Color32 color;

    public ControlCubeRecordedPathPoint(int pathId, Vector3 position, ControlCubePathPointMarker marker, Color32 color)
    {
        this.pathId = pathId;
        this.position = position;
        this.marker = marker;
        this.color = color;
    }
}

public class NetworkControlCubeManager : NetworkBehaviour
{
    public ControlCubeDeviceInterface controlCube;

    [SyncVar(hook = nameof(OnSyncPercentChanged))]
    public Vector3 syncPercent;

    public readonly SyncList<ControlCubeRecordedPathPoint> recordedPathPoints = new SyncList<ControlCubeRecordedPathPoint>();

    readonly HashSet<int> activePathIds = new HashSet<int>();
    int nextPathId = 1;
    bool callbackRegistered;

    void Awake()
    {
        if (controlCube != null)
        {
            controlCube.onPercentChangedEvent.AddListener(OnLocalPercentChanged);
            controlCube.AssignNetworkManager(this);
        }

        RegisterCallbacks();
    }

    void OnDestroy()
    {
        if (controlCube != null)
        {
            controlCube.onPercentChangedEvent.RemoveListener(OnLocalPercentChanged);
            controlCube.AssignNetworkManager(null);
        }

        if (callbackRegistered)
        {
            recordedPathPoints.Callback -= OnRecordedPathChanged;
            callbackRegistered = false;
        }
    }

    void RegisterCallbacks()
    {
        if (callbackRegistered)
        {
            return;
        }

        recordedPathPoints.Callback += OnRecordedPathChanged;
        callbackRegistered = true;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (controlCube != null)
        {
            syncPercent = controlCube.percent;
            controlCube.InitializeNetworkedPaths(recordedPathPoints);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (controlCube != null)
        {
            controlCube.Setup(syncPercent);
            controlCube.InitializeNetworkedPaths(recordedPathPoints);
        }
    }

    void OnLocalPercentChanged()
    {
        if (controlCube == null)
        {
            return;
        }

        if (isServer)
        {
            syncPercent = controlCube.percent;
        }
        else
        {
            CmdUpdateHandle(controlCube.percent);
        }
    }

    void OnSyncPercentChanged(Vector3 oldValue, Vector3 newValue)
    {
        if (controlCube == null)
        {
            return;
        }

        if (controlCube.cubeManip != null && controlCube.cubeManip.curState == manipObject.manipState.grabbed)
        {
            return;
        }

        controlCube.Setup(newValue);
    }

    [Command(requiresAuthority = false)]
    void CmdUpdateHandle(Vector3 value)
    {
        syncPercent = value;
    }

    [Command(requiresAuthority = false)]
    public void CmdBeginPath(byte requestId, Vector3 startPoint, Color32 color, NetworkConnectionToClient sender = null)
    {
        int pathId = nextPathId++;
        activePathIds.Add(pathId);
        recordedPathPoints.Add(new ControlCubeRecordedPathPoint(pathId, startPoint, ControlCubePathPointMarker.Start, color));

        if (sender != null)
        {
            TargetConfirmPath(sender, requestId, pathId);
        }
        else
        {
            controlCube?.HandlePathConfirmed(requestId, pathId);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdAppendPathPoint(int pathId, Vector3 point)
    {
        if (!activePathIds.Contains(pathId))
        {
            return;
        }

        recordedPathPoints.Add(new ControlCubeRecordedPathPoint(pathId, point, ControlCubePathPointMarker.Continue, default));
    }

    [Command(requiresAuthority = false)]
    public void CmdEndPath(int pathId, Vector3 lastPoint)
    {
        if (!activePathIds.Contains(pathId))
        {
            return;
        }

        recordedPathPoints.Add(new ControlCubeRecordedPathPoint(pathId, lastPoint, ControlCubePathPointMarker.End, default));
        activePathIds.Remove(pathId);
    }

    [Command(requiresAuthority = false)]
    public void CmdClearPaths()
    {
        recordedPathPoints.Clear();
        activePathIds.Clear();
        nextPathId = 1;
    }

    [TargetRpc]
    void TargetConfirmPath(NetworkConnection target, byte requestId, int pathId)
    {
        controlCube?.HandlePathConfirmed(requestId, pathId);
    }

    void OnRecordedPathChanged(SyncList<ControlCubeRecordedPathPoint>.Operation op, int index, ControlCubeRecordedPathPoint oldItem, ControlCubeRecordedPathPoint newItem)
    {
        if (controlCube == null)
        {
            return;
        }

        switch (op)
        {
            case SyncList<ControlCubeRecordedPathPoint>.Operation.OP_CLEAR:
            case SyncList<ControlCubeRecordedPathPoint>.Operation.OP_REMOVEAT:
                controlCube.InitializeNetworkedPaths(recordedPathPoints);
                break;
            default:
                controlCube.ApplyNetworkPoint(newItem);
                break;
        }
    }
}
