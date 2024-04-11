using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class NetworkControlCubeManager : NetworkBehaviour
{
    public ControlCubeDeviceInterface controlCube;

    [SyncVar(hook = nameof(OnUpdateCubePercent))]
    public Vector3 syncPercent;

    public readonly SyncList<Vector2> xyValues = new SyncList<Vector2>();

    public override void OnStartServer()
    {
        base.OnStartServer();

        syncPercent = controlCube.percent;
    }

    private void Start()
    {
        controlCube.onPercentChangedEvent.AddListener(UpdateHandleValue);
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            controlCube.Setup(syncPercent);
        }
    }

    public void OnUpdateCubePercent(Vector3 oldValue, Vector3 newValue)
    {
        if (controlCube.cubeManip.curState != manipObject.manipState.grabbed)
        {
            Debug.Log($"Update control cube percent by sync value: {newValue}");
            controlCube.Setup(newValue);
        }
    }


    public void UpdateHandleValue()
    {
        Debug.Log($"Update control cube percent: {controlCube.percent}");
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
        controlCube.Setup(value);
        Debug.Log($"Update control cube percent on server value: {value}");
    }
}
