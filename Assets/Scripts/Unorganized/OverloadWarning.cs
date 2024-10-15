using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class OverloadWarning : MonoBehaviour
{

    [DllImport("OSLNative")]
    static extern float MasterBusRecorder_GetLevel_Lin();
    [DllImport("OSLNative")]
    static extern float MasterBusRecorder_GetLevel_dB();

    public GameObject overloadText;

    void Awake()
    {
        overloadText.SetActive(false);
    }

    void Update()
    {
        overloadText.SetActive(MasterBusRecorder_GetLevel_dB() > -3f);
    }
}
