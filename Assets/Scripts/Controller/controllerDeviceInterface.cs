using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Xml.Serialization;

public class controllerDeviceInterface : deviceInterface
{
    public Vector3 percent;
    public controllerSingleSignalGenerator[] signals;
    public omniJack[] outputs;
    public controllerZone cubeManip;

    public button recordButton;
    public button deleteButton;
    public NetworkControllerManager networkManager;
    public controllerPathRecorder pathRecorder = new controllerPathRecorder();

    public UnityEvent onPercentChangedEvent;

    bool cubeGrabbed;
    bool deleteButtonWasPressed;
    manipulator currentCubeManipulator;

    public override void Awake()
    {
        base.Awake();
        pathRecorder.Initialize(transform);
        pathRecorder.NetworkManager = networkManager;
        Setup(percent);
        ConfigureButtons();
    }

    void OnDisable()
    {
        pathRecorder.StopRecording();
    }

    void Update()
    {
        bool grabbedNow = cubeManip != null && cubeManip.curState == manipObject.manipState.grabbed;
        if (grabbedNow != cubeGrabbed)
        {
            cubeGrabbed = grabbedNow;
            if (!cubeGrabbed)
            {
                currentCubeManipulator = null;
                pathRecorder.StopRecording();
            }
            else
            {
                ResolveCubeManipulator();
                if (ShouldRecord())
                {
                    pathRecorder.StartRecording(percent);
                }
            }
        }

        bool deletePressed = deleteButton != null && deleteButton.isHit;
        if (deletePressed && !deleteButtonWasPressed)
        {
            pathRecorder.ClearPaths(true);
        }
        deleteButtonWasPressed = deletePressed;
    }

    public override void hit(bool on, int ID = -1)
    {
        if (recordButton != null && ID == recordButton.buttonID)
        {
            HandleRecordHit(on);
            return;
        }
    }

    public void AssignNetworkManager(NetworkControllerManager manager)
    {
        networkManager = manager;
        pathRecorder.NetworkManager = manager;
    }

    public void InitializeNetworkedPaths(IList<ControllerRecordedPathPoint> list)
    {
        pathRecorder.InitializeNetworkedPaths(list);
    }

    public void ApplyNetworkPoint(ControllerRecordedPathPoint point)
    {
        pathRecorder.ApplyNetworkPoint(point);
    }

    public void HandlePathConfirmed(byte requestId, int pathId)
    {
        pathRecorder.HandlePathConfirmed(requestId, pathId);
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
            if (ShouldRecord())
            {
                pathRecorder.StartRecording(percent);
                pathRecorder.RecordPoint(percent);
            }
            onPercentChangedEvent.Invoke();
        }
    }

    public override InstrumentData GetData()
    {
        ControllerData data = new ControllerData();

        data.deviceType = DeviceType.Controller;
        GetTransformData(data);

        data.jackOutID = new int[4];
        for (int i = 0; i < 3; i++) data.jackOutID[i] = outputs[i].transform.GetInstanceID();

        data.dimensionValues = new float[3];
        for (int i = 0; i < 3; i++) data.dimensionValues[i] = percent[i];

        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        ControllerData data = d as ControllerData;

        base.Load(data, copyMode);

        for (int i = 0; i < 3; i++) outputs[i].SetID(data.jackOutID[i], copyMode);
        for (int i = 0; i < 3; i++) percent[i] = data.dimensionValues[i];

        Setup(percent);
    }

    void HandleRecordHit(bool on)
    {
        if (!on)
        {
            pathRecorder.StopRecording();
            return;
        }

        if (ShouldRecord())
        {
            pathRecorder.StartRecording(percent);
        }
    }

    bool ShouldRecord()
    {
        if (recordButton == null || !recordButton.isHit || !cubeGrabbed)
        {
            return false;
        }

        return IsGrabbingManipulatorSidePressed();
    }

    bool IsGrabbingManipulatorSidePressed()
    {
        manipulator manip = ResolveCubeManipulator();
        if (manip == null)
        {
            return false;
        }

        OSLInput input = OSLInput.getInstance();
        if (input == null)
        {
            return false;
        }

        return input.isSidePressed(manip.controllerIndex);
    }

    manipulator ResolveCubeManipulator()
    {
        if (cubeManip == null)
        {
            currentCubeManipulator = null;
            return null;
        }

        if (currentCubeManipulator != null && currentCubeManipulator.SelectedObject == cubeManip)
        {
            return currentCubeManipulator;
        }

        List<manipulator> instances = manipulator.GetInstances();
        if (instances == null)
        {
            currentCubeManipulator = null;
            return null;
        }

        for (int i = 0; i < instances.Count; i++)
        {
            manipulator candidate = instances[i];
            if (candidate != null && candidate.SelectedObject == cubeManip)
            {
                currentCubeManipulator = candidate;
                return currentCubeManipulator;
            }
        }

        currentCubeManipulator = null;
        return null;
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
}

[XmlInclude(typeof(ControlCubeData))] // Legacy handling
public class ControllerData : InstrumentData
{
    public int[] jackOutID;
    public float[] dimensionValues;
}

[XmlType("ControlCubeData")]
public class ControlCubeData : ControllerData
{
    // Legacy shell for old saves
}
