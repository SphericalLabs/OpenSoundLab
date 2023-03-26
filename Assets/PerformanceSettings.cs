using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.Oculus;
using System;

public class PerformanceSettings : MonoBehaviour
{
    public dial cpuDial, gpuDial, fovDial, scaleDial, fpsDial;
    private float cpuDialVal, gpuDialVal, fovDialVal, scaleDialVal, fpsDialVal;
    UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urpa;

  // Start is called before the first frame update
  void Start()
    {
      float fps;
      if (Application.platform == RuntimePlatform.Android) // remove this init?
      {
        Debug.Log("Current cpuLevel: " + Stats.AdaptivePerformance.CPULevel + ", gpuLevel: " + Stats.AdaptivePerformance.GPULevel);
        Debug.Log("Trying to set levels to 4");
        Debug.Log("TrySetCPULevel returned " + Performance.TrySetCPULevel(3)); // from 0-3
        Debug.Log("TrySetGPULevel returned " + Performance.TrySetGPULevel(3)); // from 0-3
        Debug.Log("New cpuLevel: " + Stats.AdaptivePerformance.CPULevel + ", gpuLevel: " + Stats.AdaptivePerformance.GPULevel);
        Performance.TryGetDisplayRefreshRate(out fps);
        Debug.Log("Display refresh rate: " + fps);
        OVRManager.useDynamicFixedFoveatedRendering = false;
        Unity.XR.Oculus.Utils.EnableDynamicFFR(false);
        if (OVRManager.eyeTrackedFoveatedRenderingSupported)
        {
          OVRManager.eyeTrackedFoveatedRenderingEnabled = true;
        } 
        if (OVRManager.eyeTrackedFoveatedRenderingEnabled)
        {
          Debug.Log("eyeTrackedFoveatedRenderingEnabled is true");
        }
      //Unity.XR.Oculus.Utils.SetFoveationLevel(4);
    }

      urpa = (UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
      urpa.renderScale = 1.33f;
      
  }

  // Update is called once per frame
  void Update()
  {

    if (Application.platform != RuntimePlatform.Android) return;

    if (cpuDialVal != cpuDial.percent)
    {
      cpuDialVal = cpuDial.percent;
      Performance.TrySetCPULevel(Mathf.RoundToInt(cpuDialVal * (cpuDial.notchSteps - 1)));
    }

    if (gpuDialVal != gpuDial.percent)
    {
      gpuDialVal = gpuDial.percent;
      Performance.TrySetGPULevel(Mathf.RoundToInt(gpuDialVal * (gpuDial.notchSteps - 1)));
    }

    if (fovDialVal != fovDial.percent)
    {
      fovDialVal = fovDial.percent;
      //Unity.XR.Oculus.Utils.SetFoveationLevel(Mathf.FloorToInt(fovDialVal * fovDial.notchSteps));
      
      OVRManager.foveatedRenderingLevel = (OVRManager.FoveatedRenderingLevel) Enum.ToObject(typeof(OVRManager.FoveatedRenderingLevel), Mathf.RoundToInt(fovDialVal * (fovDial.notchSteps - 1)));

      Debug.Log(Mathf.RoundToInt(fovDialVal * (fovDial.notchSteps - 1)));
    }

    if (scaleDialVal != scaleDial.percent)
    {
      scaleDialVal = scaleDial.percent;
      urpa.renderScale = scaleDialVal * 2f;
    }

    if (fpsDialVal != fpsDial.percent)
    {
      fpsDialVal = fpsDial.percent;
      float newFps = 0f;
      int tmpFpsMode = Mathf.RoundToInt(fpsDialVal * (fpsDial.notchSteps - 1));
      if(tmpFpsMode == 0) {
        newFps = 60f;
      } else if (tmpFpsMode == 1){
        newFps = 72f;
      } else if (tmpFpsMode == 2){
        newFps = 80f;
      } else if (tmpFpsMode == 3){
        newFps = 90f;
      } else if (tmpFpsMode == 4){
        newFps = 120f;
      } 
      
      //OVRPlugin.systemDisplayFrequency = newFps;
      Performance.TrySetDisplayRefreshRate(newFps);
    }

    
    
  }
}
