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
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System.Collections;

public class platformSetup : MonoBehaviour
{
    public GameObject hmdPrefab, controllerPrefab;

    public Transform hmdTargetOculus, controllerLTargetOculus, controllerRTargetOculus;
    public GameObject hiresCamSelect;

    public bool usePersonalizedHands = false;
    public string personalizedHandsPrefabStr;

    manipulator[] manips = new manipulator[2];
    void Awake()
    {
        masterControl MC = GetComponent<masterControl>();

        if (MC.currentPlatform == masterControl.platform.Oculus)
        {

            Instantiate(hmdPrefab, hmdTargetOculus, false);
            manips[0] = (Instantiate(controllerPrefab, controllerLTargetOculus, false) as GameObject).GetComponentInChildren<manipulator>();
            manips[1] = (Instantiate(controllerPrefab, controllerRTargetOculus, false) as GameObject).GetComponentInChildren<manipulator>();

            manips[0].transform.parent.localPosition = Vector3.zero;
            manips[1].transform.parent.localPosition = Vector3.zero;

            /*if (UnityEngine.XR.XRSettings.loadedDeviceName == "Oculus")*/
            oculusSwitch();

            manips[0].SetDeviceIndex(0);
            manips[0].transform.parent.GetComponentInChildren<OVRControllerHelper>().m_controller = OVRInput.Controller.LTouch;
            manips[1].SetDeviceIndex(1);
            manips[1].transform.parent.GetComponentInChildren<OVRControllerHelper>().m_controller = OVRInput.Controller.RTouch;
        }
    }

    void Update()
    {
        OVRPlugin.Controller control = OVRPlugin.GetActiveController(); //get current controller scheme
        if ((OVRPlugin.Controller.Hands == control) || (OVRPlugin.Controller.LHand == control) || (OVRPlugin.Controller.RHand == control))
        { //if current controller is hands disable the controllers
            manips[0].gameObject.SetActive(false);
            manips[1].gameObject.SetActive(false);
        }
        else
        {
            manips[0].gameObject.SetActive(true);
            manips[1].gameObject.SetActive(true);
        }
    }

    void oculusSwitch()
    {
        //manips[0].invertScale(); // this actually makes the controller model L and R handed.
        manips[0].changeHW("oculus");
        manips[1].changeHW("oculus");
    }


    //public GameObject targetGameObject; // GameObject to attach the loaded prefab to.
    public GameObject OVRControllerPrefabL; // GameObject to be disabled.
    public GameObject OVRControllerPrefabR; // GameObject to be disabled.

    void Start()
    {

        if (usePersonalizedHands)
        {
            // Attempt to load the resource
            GameObject loadedPrefab = Resources.Load("Personalization/" + personalizedHandsPrefabStr) as GameObject;

            // Check if the resource is present
            if (loadedPrefab != null)
            {
                Debug.Log("Personalized hands found.");

                // Instantiate and attach it to the target GameObject
                Instantiate(loadedPrefab, manips[0].transform);
                Instantiate(loadedPrefab, manips[1].transform);

                if (OVRControllerPrefabL != null)
                    OVRControllerPrefabL.SetActive(false);
                if (OVRControllerPrefabR != null)
                    OVRControllerPrefabR.SetActive(false);

            }
            else
            {
                Debug.LogWarning("Personalized hands requested but not found. Add your hands prefab to Resources or disable this feature by setting usePersonalizedHands to false.");
            }
        }

    }

}
