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
      //new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.HighTop, renderScale = 0.6f },
      //new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.HighTop, renderScale = 0.7f },
      //new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.HighTop, renderScale = 0.8f },
      //new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.HighTop, renderScale = 0.9f },
      new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.HighTop, renderScale = 1f },
      new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.High, renderScale = 1f },
      new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.Medium, renderScale = 1f },
      new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.Low, renderScale = 1f },
      new QualityStep { fovLevel = OVRManager.FoveatedRenderingLevel.Off, renderScale = 1f },
    };

    float baseRenderScale = 1.40f;
    int currentQualityStep;

    // Start is called before the first frame update
    void Start()
    {
        if (Application.platform == RuntimePlatform.Android) // remove this init?
        {

            boostProcessors();

            if (OVRManager.eyeTrackedFoveatedRenderingSupported)
            {
                OVRManager.eyeTrackedFoveatedRenderingEnabled = true;
            }
            if (OVRManager.eyeTrackedFoveatedRenderingEnabled)
            {
                Debug.Log("eyeTrackedFoveatedRenderingEnabled is true");
                OVRManager.foveatedRenderingLevel = OVRManager.FoveatedRenderingLevel.Off;
            }
            //OVRManager.useDynamicFoveatedRendering = true;
            //OVRManager.SetDynamicFoveatedRenderingEnabled(true);

        }


        //UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1.5f; // does not work

        urpa = (UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        urpa.renderScale = baseRenderScale;  // this is the max oversampling quality, should not be changed during runtime, just for init

        //OVRManager.suggestedGpuPerfLevel = OVRManager.ProcessorPerformanceLevel.Boost;
        //OVRManager.gpuLevel = 2;
        //OVRManager.cpuLevel = 2;

        //if (Performance.TrySetCPULevel(3))
        //  Debug.Log("TrySetCPULevel(3) successful");
        //else
        //  Debug.Log("TrySetCPULevel(3) not successful");

        //if (Performance.TrySetGPULevel(3))
        //  Debug.Log("TrySetGPULevel(3) successful");
        //else
        //  Debug.Log("TrySetGPULevel(3) not successful");

        currentQualityStep = qualitySteps.Length - 1;
    }

    bool qualityChanged = false;
    float averageFrameTime = 0f;
    int targetFps = 72;
    float renderBudget;
    float f;
    float lastKickDownTime;

    void boostProcessors()
    {

        Debug.Log("Current cpuLevel: " + Stats.AdaptivePerformance.CPULevel + ", gpuLevel: " + Stats.AdaptivePerformance.GPULevel);
        Debug.Log("Trying to set levels to 4");
        Debug.Log("TrySetCPULevel returned " + Performance.TrySetCPULevel(4)); // from 0-4
        Debug.Log("TrySetGPULevel returned " + Performance.TrySetGPULevel(4)); // from 0-4
        Debug.Log("New cpuLevel: " + Stats.AdaptivePerformance.CPULevel + ", gpuLevel: " + Stats.AdaptivePerformance.GPULevel);
        Performance.TryGetDisplayRefreshRate(out f);
        Debug.Log("Display refresh rate: " + f);

        OVRPlugin.suggestedCpuPerfLevel = OVRPlugin.ProcessorPerformanceLevel.SustainedLow;
        OVRPlugin.suggestedGpuPerfLevel = OVRPlugin.ProcessorPerformanceLevel.Boost;
    }

    // Update is called once per frame
    void Update()
    {


        if (Application.platform != RuntimePlatform.Android) return;

        // if(Time.frameCount % 100 == 0) boostProcessors(); // keep spamming the metal

        if (Time.frameCount % (int)targetFps == 0)
        {
            Performance.TryGetDisplayRefreshRate(out f); // Application.targetFrameRate would not capture fps changes from adb or SideQuest
            targetFps = (int)f;
            renderBudget = 1.0f / targetFps; // 1 : 72 = 13.8 ms
                                             //Debug.Log(targetFps);
        }



        // always run averaging
        averageFrameTime += (Time.deltaTime - averageFrameTime) * 0.07f;

        // often run spike detection
        if (Time.frameCount % targetFps == 0 && lastKickDownTime + 1 < Time.time)
        {
            if (Time.deltaTime >= renderBudget * 1f)
            {
                currentQualityStep -= 1;
                qualityChanged = true;
                lastKickDownTime = Time.time;
                //Debug.Log("KICK DOWN");
            }
        }

        //if(Time.frameCount % 10 == 0)Debug.Log(averageFrameTime + "    " + currentQualityStep);

        // rarely run general rate detection
        // longer time step to let kick down settle in
        if (Time.frameCount % targetFps == 0)
        {

            //if (averageFrameTime >= renderBudget * 1.04f && currentQualityStep != 0)
            //{
            //  currentQualityStep--;
            //  qualityChanged = true;
            //}
            //else
            if (averageFrameTime <= renderBudget * 1.05f && currentQualityStep != qualitySteps.Length - 1)
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
