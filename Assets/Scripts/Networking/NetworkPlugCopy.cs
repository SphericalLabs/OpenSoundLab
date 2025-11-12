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

public class NetworkPlugCopy : manipObject
{
    public Transform otherEnd;
    public LineRenderer lineRenderer;

    private NetworkPlayerPlugHand networkPlayerPlugHand;
    private omniJack targetJack;

    public void Initialize(NetworkPlayerPlugHand networkPlayerPlugHand, omniJack targetJack)
    {
        this.networkPlayerPlugHand = networkPlayerPlugHand;
        this.targetJack = targetJack;

        // this code here is for the preview of plugs that are currently being patched by another client
        // this code does not have the full wire width and plug size matching that omniPlug has, but it currently does the job
        // as soon as the client releases and fully patches the new cable omniplug.Activate() is called and properly sets everything up

        otherEnd.transform.parent = targetJack.gameObject.transform;
        otherEnd.transform.localScale = Vector3.one;
        otherEnd.localPosition = new Vector3(0, -0.03f, 0);
        otherEnd.localRotation = Quaternion.Euler(-90, 0, 0);

        onStartGrabEvents.AddListener(OnGrabbedByOther);
    }

    public void OnGrabbedByOther()
    {
        //delete this and send passing to network
        if (networkPlayerPlugHand != null)
        {
            networkPlayerPlugHand.PassToOtherHand();
            targetJack.CreatePlugInHand(manipulatorObjScript);
            //start new omnoPlug grab from target jack
            //copy this offset to hand to new jack
            Destroy(this.gameObject);
        }
    }

    private void Update()
    {
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, otherEnd.position);
    }

    private void OnDestroy()
    {
        Destroy(otherEnd.gameObject);
    }
}
