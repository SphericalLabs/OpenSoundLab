using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class HUD : MonoBehaviour
{

  [DllImport("OSLNative")]
  static extern float MasterBusRecorder_GetLevel_Lin();
  [DllImport("OSLNative")]
  static extern float MasterBusRecorder_GetLevel_dB();

  public GameObject overloadIndicator;
 
    void Awake()
    {
        overloadIndicator.SetActive(false);
    }

    void Update()
    {
        overloadIndicator.SetActive(MasterBusRecorder_GetLevel_dB() > -3f);
    }
}
