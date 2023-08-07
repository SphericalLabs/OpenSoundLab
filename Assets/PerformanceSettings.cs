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

    struct QualityStep
    {
      public OVRManager.FoveatedRenderingLevel fovLevel;
      public float renderScale;
    }

    QualityStep[] qualitySteps = new QualityStep[] {
      //new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.HighTop, renderScale = 0.1f },
      //new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.HighTop, renderScale = 0.2f },
      //new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.HighTop, renderScale = 0.3f },
      new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.HighTop, renderScale = 0.4f },
      new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.HighTop, renderScale = 0.5f },
      new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.HighTop, renderScale = 0.6f },
      new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.HighTop, renderScale = 0.7f },
      new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.HighTop, renderScale = 0.8f },
      new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.HighTop, renderScale = 0.9f },
      new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.HighTop, renderScale = 1f },
      new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.High, renderScale = 1f },
      new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.Medium, renderScale = 1f },
      new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.Low, renderScale = 1f },
      new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.Off, renderScale = 1f },
    };

    int currentQualityStep;

  // Start is called before the first frame update
  void Start()
    {
      if (Application.platform == RuntimePlatform.Android) // remove this init?
      {
        Debug.Log("Current cpuLevel: " + Stats.AdaptivePerformance.CPULevel + ", gpuLevel: " + Stats.AdaptivePerformance.GPULevel);
        Debug.Log("Trying to set levels to 4");
        Debug.Log("TrySetCPULevel returned " + Performance.TrySetCPULevel(3)); // from 0-3
        Debug.Log("TrySetGPULevel returned " + Performance.TrySetGPULevel(3)); // from 0-3
        Debug.Log("New cpuLevel: " + Stats.AdaptivePerformance.CPULevel + ", gpuLevel: " + Stats.AdaptivePerformance.GPULevel);
        Performance.TryGetDisplayRefreshRate(out f);
        Debug.Log("Display refresh rate: " + f);
        //OVRManager.useDynamicFoveatedRendering = true;
        if (OVRManager.eyeTrackedFoveatedRenderingSupported)
        {
          OVRManager.eyeTrackedFoveatedRenderingEnabled = true;
        }
        if (OVRManager.eyeTrackedFoveatedRenderingEnabled)
        {
          Debug.Log("eyeTrackedFoveatedRenderingEnabled is true");
        }
        //Unity.XR.Oculus.Utils.foveatedRenderingLevel = 0;
        //OVRManager.foveatedRenderingLevel = OVRManager.FoveatedRenderingLevel.Off;
    }

      //urpa = (UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
      //urpa.renderScale = 1.5f;  // this is the max oversampling quality, should not be changed during runtime, just for init
      // WARNING: do high settings result in warped visuals?!

      Performance.TrySetCPULevel(3);
      Performance.TrySetGPULevel(3);

      currentQualityStep = qualitySteps.Length - 1;
  }

  bool qualityChanged = false;
  float averageFrameTime = 0f;
  int fps;
  float renderBudget;
  float f;


  // Update is called once per frame
  void Update()
  {
    if (Application.platform != RuntimePlatform.Android) return;

    if (Time.frameCount % 72 == 0)
    {
      Performance.TryGetDisplayRefreshRate(out f); // Application.targetFrameRate would not capture fps changes from adb or SideQuest
      fps = (int) f;
      renderBudget = 1.0f / fps; // 1 : 72 = 13.8 ms    
      //Debug.Log(fps);
    }

    
    // always run averaging
    averageFrameTime += (Time.deltaTime - averageFrameTime) * 0.07f;

    // often run spike detection
    if (Time.frameCount % fps / 2 == 0)
    {
      if (Time.deltaTime >= renderBudget * 2.5f) 
      {
        currentQualityStep -= 2;
        Debug.Log("KICK DOWN");
        qualityChanged = true;
      }
    }

    // rarely run general rate detection
    // longer time step to let kick down settle in
    if (Time.frameCount % fps == 0) 
    {

      if (averageFrameTime >= renderBudget * 1.07f && currentQualityStep != 0) 
      {
        currentQualityStep--;
        qualityChanged = true;        
      }
      else if (averageFrameTime <= renderBudget * 0.99f && currentQualityStep != qualitySteps.Length - 1) 
      {
        currentQualityStep++;
        qualityChanged = true;
      }

    }

    // always clamp
    currentQualityStep = Math.Clamp(currentQualityStep, 0, qualitySteps.Length - 1);

    if (qualityChanged)
    {
      if (OVRManager.foveatedRenderingLevel != qualitySteps[currentQualityStep].fovLevel)
        OVRManager.foveatedRenderingLevel = qualitySteps[currentQualityStep].fovLevel;
      if (UnityEngine.XR.XRSettings.renderViewportScale != qualitySteps[currentQualityStep].renderScale)
        UnityEngine.XR.XRSettings.renderViewportScale = qualitySteps[currentQualityStep].renderScale;
      qualityChanged = false;
      Debug.Log("New step is " + currentQualityStep);
    }

    return;



    

    //if (cpuDialVal != cpuDial.percent)
    //{
    //  cpuDialVal = cpuDial.percent;
    //  Performance.TrySetCPULevel(Mathf.RoundToInt(cpuDialVal * (cpuDial.notchSteps - 1)));
    //}

    //if (gpuDialVal != gpuDial.percent)
    //{
    //  gpuDialVal = gpuDial.percent;
    //  Performance.TrySetGPULevel(Mathf.RoundToInt(gpuDialVal * (gpuDial.notchSteps - 1)));
    //}

    //if (fovDialVal != fovDial.percent)
    //{
    //  fovDialVal = fovDial.percent;
    //  //Unity.XR.Oculus.Utils.SetFoveationLevel(Mathf.FloorToInt(fovDialVal * fovDial.notchSteps));

    //  OVRManager.foveatedRenderingLevel = (OVRManager.FoveatedRenderingLevel)Enum.ToObject(typeof(OVRManager.FoveatedRenderingLevel), Mathf.RoundToInt(fovDialVal * (fovDial.notchSteps - 1)));

    //  Debug.Log(Mathf.RoundToInt(fovDialVal * (fovDial.notchSteps - 1)));
    //} 

    //if (scaleDialVal != scaleDial.percent)
    //{
    //  scaleDialVal = scaleDial.percent;
    //  //urpa.renderScale = scaleDialVal * 2f;
    //  //ScalableBufferManager.ResizeBuffers(Utils.map(scaleDial.percent, 0f, 1f, 0.25f, 1f), Utils.map(scaleDial.percent, 0f, 1f, 0.25f, 1f));
    //  UnityEngine.XR.XRSettings.renderViewportScale = Utils.map(scaleDial.percent, 0f, 1f, 0.25f, 1f);
      
      
    //}


    //if (fpsDialVal != fpsDial.percent)
    //{
    //  fpsDialVal = fpsDial.percent;
    //  float newFps = 0f;
    //  int tmpFpsMode = Mathf.RoundToInt(fpsDialVal * (fpsDial.notchSteps - 1));
    //  if(tmpFpsMode == 0) {
    //    newFps = 60f;
    //  } else if (tmpFpsMode == 1){
    //    newFps = 72f;
    //  } else if (tmpFpsMode == 2){
    //    newFps = 80f;
    //  } else if (tmpFpsMode == 3){
    //    newFps = 90f;
    //  } else if (tmpFpsMode == 4){
    //    newFps = 120f;
    //  } 
      
    //  //OVRPlugin.systemDisplayFrequency = newFps;
    //  Performance.TrySetDisplayRefreshRate(newFps);
    //}

    
  }
}
