using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.Oculus;
using System;

public class PerformanceSettings : MonoBehaviour
{
    public dial cpuDial, gpuDial, fovDial, scaleDial, fpsDial;
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
      //new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.HighTop, renderScale = 0.4f },
      //new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.HighTop, renderScale = 0.5f },
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

        if (OVRManager.eyeTrackedFoveatedRenderingSupported)
        {
          OVRManager.eyeTrackedFoveatedRenderingEnabled = true;
        }
        if (OVRManager.eyeTrackedFoveatedRenderingEnabled)
        {
          Debug.Log("eyeTrackedFoveatedRenderingEnabled is true");
          OVRManager.foveatedRenderingLevel = OVRManager.FoveatedRenderingLevel.Off;
        }
        
    }


    //UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.5f; // does not work

    // WARNING: setting this higher than 1.33f (tested on 1.5f) causes warped visuals when downscaling, but apparently only for Quest Pro. 
    urpa = (UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
    urpa.renderScale = 1.40f;  // this is the max oversampling quality, should not be changed during runtime, just for init
        

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

      if (averageFrameTime >= renderBudget * 1.08f && currentQualityStep != 0) 
      {
        currentQualityStep--;
        qualityChanged = true;        
      }
      else if (averageFrameTime <= renderBudget * 1.02f && currentQualityStep != qualitySteps.Length - 1) 
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
      //Debug.Log("New step is " + currentQualityStep);
    }

    return;

    
  }
}
