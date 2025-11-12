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

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Switch360 : MonoBehaviour
{
    public List<Texture> imgs;
    public List<AudioClip> snds;
    //public float lastPointer = 0;
    //float newPointer = 0f;
    int imgPointer = 0;
    int sndPointer = 0;
    Material mat;
    AudioSource src;
    Vector2 leftStick, rightStick;
    Vector3 rotation;
    float srcVolume = 0.2f;

    void Start()
    {
        mat = GetComponent<Renderer>().materials[0];
        mat.mainTexture = imgs[imgPointer];
        rotation = new Vector3(0f, 0f, 0f);

        src = GetComponent<AudioSource>();
        src.clip = snds[sndPointer];
    }

    void Update()
    {
        leftStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);

        // rotate 360 sphere around y-axis
        if (leftStick.x != 0f)
        {
            rotation.y = -1 * Mathf.Sign(leftStick.x) * Mathf.Abs(Mathf.Pow(leftStick.x, 4));
            transform.Rotate(rotation);
        }

        // shuttle through 360 panos, allows for hi-speed fun...
        //if (leftStick.y != 0f) {
        //newPointer += Mathf.Sign(leftStick.y) * Mathf.Abs(Mathf.Pow(leftStick.y, 3) * 0.3f);
        //if (newPointer > environments.Count - 1)
        //{
        //  newPointer = 0;
        //}
        //else if (newPointer < 0)
        //{
        //  newPointer = environments.Count - 1;
        //}
        //if (Mathf.RoundToInt(lastPointer) != Mathf.RoundToInt(newPointer)) {
        //  mat.mainTexture = environments[Mathf.RoundToInt(newPointer)];
        //}
        //lastPointer = newPointer;

        //}

        // flip through equirectangulars
        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickUp, OVRInput.Controller.LTouch))
        {
            imgPointer = mod(imgPointer + 1, imgs.Count);
            mat.mainTexture = imgs[imgPointer];
        }
        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickDown, OVRInput.Controller.LTouch))
        {
            imgPointer = mod(imgPointer - 1, imgs.Count);
            mat.mainTexture = imgs[imgPointer];
        }

        rightStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);

        // adjust volume of ambisonics bus
        if (rightStick.x != 0f)
        {
            srcVolume += Mathf.Sign(rightStick.x) * Mathf.Abs(Mathf.Pow(rightStick.x, 4) * 0.005f);
            src.volume = Mathf.Pow(Mathf.Clamp01(srcVolume), 2);
        }

        // flip through ambisonics
        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickUp, OVRInput.Controller.RTouch))
        {
            sndPointer = mod(sndPointer + 1, snds.Count);
            src.clip = snds[sndPointer];
            src.Play();
        }
        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickDown, OVRInput.Controller.RTouch))
        {
            sndPointer = mod(sndPointer - 1, snds.Count);
            src.clip = snds[sndPointer];
            src.Play();
        }

    }
    int mod(int a, int b)
    {
        int c = a % b;
        return (c < 0) ? c + b : c;
    }
}
