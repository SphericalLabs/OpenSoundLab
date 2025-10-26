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

    [SyncVar(hook = nameof(OnUpdateCubePercent))]
    public Vector3 syncPercent;

    [SyncVar(hook = nameof(OnRecordingStateChanged))]
    private bool isRecordingActive;

    public readonly SyncList<Vector2> xyValues = new SyncList<Vector2>();
    public readonly SyncList<ControlCubeRecordedPathPoint> recordedPathPoints = new SyncList<ControlCubeRecordedPathPoint>();
    private readonly HashSet<int> activePathIds = new HashSet<int>();

    private bool callbacksRegistered;
    private int nextPathId = 1;

    public override void OnStartServer()
    {
        base.OnStartServer();
        RegisterCallbacks();

        if (controlCube != null)
        {
            syncPercent = controlCube.percent;
            controlCube.InitializeNetworkedPaths(recordedPathPoints);
            controlCube.ApplyNetworkRecordingState(isRecordingActive);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        RegisterCallbacks();

        if (!isServer && controlCube != null)
        {
            controlCube.Setup(syncPercent);
        }

        controlCube?.InitializeNetworkedPaths(recordedPathPoints);
        controlCube?.ApplyNetworkRecordingState(isRecordingActive);
    }

    private void Awake()
    {
        if (controlCube != null)
        {
            controlCube.onPercentChangedEvent.AddListener(UpdateHandleValue);
            controlCube.AssignNetworkManager(this);
        }

        RegisterCallbacks();
    }

    private void OnDestroy()
    {
        if (controlCube != null)
        {
            controlCube.onPercentChangedEvent.RemoveListener(UpdateHandleValue);
            controlCube.AssignNetworkManager(null);
        }

        if (callbacksRegistered)
        {
            recordedPathPoints.Callback -= OnRecordedPathPointChanged;
            callbacksRegistered = false;
        }
    }

    private void RegisterCallbacks()
    {
        if (callbacksRegistered)
        {
            return;
        }

        recordedPathPoints.Callback += OnRecordedPathPointChanged;
        callbacksRegistered = true;
    }

    public void OnUpdateCubePercent(Vector3 oldValue, Vector3 newValue)
    {
        if (controlCube == null)
        {
            return;
        }

        if (controlCube.cubeManip.curState != manipObject.manipState.grabbed)
        {
            controlCube.Setup(newValue);
        }
    }

    public void UpdateHandleValue()
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
            CmdUpdateHandleValue(controlCube.percent);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateHandleValue(Vector3 value)
    {
        syncPercent = value;
    }

    [Command(requiresAuthority = false)]
    public void CmdSetRecordingState(bool shouldRecord)
    {
        if (isRecordingActive == shouldRecord)
        {
            return;
        }

        isRecordingActive = shouldRecord;
    }

    private void OnRecordingStateChanged(bool oldValue, bool newValue)
    {
        controlCube?.ApplyNetworkRecordingState(newValue);
    }

    [Command(requiresAuthority = false)]
    public void CmdBeginPath(byte requestId, Vector3 startPoint, Color32 color, NetworkConnectionToClient sender = null)
    {
        int pathId = nextPathId++;
        // Mirror only populates connectionToClient when the object has client authority,
        // so fall back to the provided sender reference to acknowledge non-authoritative clients.
        NetworkConnectionToClient targetConnection = sender ?? connectionToClient;

        if (targetConnection != null)
        {
            TargetConfirmPath(targetConnection, requestId, pathId);
        }
        else
        {
            controlCube?.HandleLocalPathConfirmed(requestId, pathId);
        }

        AddRecordedPoint(new ControlCubeRecordedPathPoint(pathId, startPoint, ControlCubePathPointMarker.Start, color));
    }

    [Command(requiresAuthority = false)]
    public void CmdAppendPathPoint(int pathId, Vector3 point)
    {
        ServerAppendPathPoint(pathId, point);
    }

    [Command(requiresAuthority = false)]
    public void CmdEndPath(int pathId, Vector3 lastPoint)
    {
        ServerEndPath(pathId, lastPoint);
    }

    [Command(requiresAuthority = false)]
    public void CmdClearPaths()
    {
        recordedPathPoints.Clear();
        activePathIds.Clear();
        nextPathId = 1;
    }

    [TargetRpc]
    private void TargetConfirmPath(NetworkConnection target, byte requestId, int pathId)
    {
        controlCube?.HandleRemotePathConfirmed(requestId, pathId);
    }

    private void AddRecordedPoint(ControlCubeRecordedPathPoint point)
    {
        if (point.marker == ControlCubePathPointMarker.Start)
        {
            activePathIds.Add(point.pathId);
        }

        recordedPathPoints.Add(point);
    }

    private void OnRecordedPathPointChanged(SyncList<ControlCubeRecordedPathPoint>.Operation op, int index, ControlCubeRecordedPathPoint oldItem, ControlCubeRecordedPathPoint newItem)
    {
        controlCube?.HandleNetworkPathListChanged(op, index, oldItem, newItem);
    }

    [Server]
    private void ServerAppendPathPoint(int pathId, Vector3 point)
    {
        if (!activePathIds.Contains(pathId))
        {
            return;
        }

        AddRecordedPoint(new ControlCubeRecordedPathPoint(pathId, point, ControlCubePathPointMarker.Continue, default));
    }

    [Server]
    private void ServerEndPath(int pathId, Vector3 lastPoint)
    {
        if (!activePathIds.Contains(pathId))
        {
            return;
        }

        AddRecordedPoint(new ControlCubeRecordedPathPoint(pathId, lastPoint, ControlCubePathPointMarker.End, default));
        activePathIds.Remove(pathId);
    }

    public void RequestAppendPathPoint(int pathId, Vector3 point)
    {
        if (isServer)
        {
            ServerAppendPathPoint(pathId, point);
        }
        else
        {
            CmdAppendPathPoint(pathId, point);
        }
    }

    public void RequestEndPath(int pathId, Vector3 lastPoint)
    {
        if (isServer)
        {
            ServerEndPath(pathId, lastPoint);
        }
        else
        {
            CmdEndPath(pathId, lastPoint);
        }
    }
}
