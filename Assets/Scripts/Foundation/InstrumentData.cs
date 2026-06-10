using UnityEngine;

public class InstrumentData
{
    public int ID;
    public string deviceType;
    public Vector3 position;
    public Vector3 scale = Vector3.one;
    public Quaternion rotation;
}

public class SystemData
{
    public int wireSetting;
    public int binauralMode;
    public float version;
    public string saveSchema;
    public string deviceIdentityFormat;
}

public class JackData : InstrumentData
{
    public int connected;
    public int homePort;
    public Vector3[] jackPath;
    public Color cordColor;
    public int signalID;
    public int signalClass;
}

public class PlugData : InstrumentData
{
    public bool outputPlug;
    public int connected;
    public int otherPlug;
    public Vector3[] plugPath;
    public Color cordColor;
}
