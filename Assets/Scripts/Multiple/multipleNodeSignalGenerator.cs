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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class multipleNodeSignalGenerator : signalGenerator
{
    public Renderer symbolA, symbolB;

    bool flow = true;
    public signalGenerator mainSig;
    public omniJack jack;
    signalGenerator sig;

    public Material mixerMaterial;
    public Material splitterMaterial;

    [DllImport("OSLNative")]
    public static extern void SetArrayToSingleValue(float[] a, int length, float val);

    public override void Awake()
    {
        sig = mainSig;
        jack = GetComponentInChildren<omniJack>();

        symbolA.sharedMaterial = mixerMaterial;
        symbolB.sharedMaterial = mixerMaterial;
    }

    public void setup(signalGenerator s, bool f)
    {
        sig = mainSig = s;
        setFlow(f);
    }

    public void setFlow(bool on)
    {
        flow = on;
        if (flow)
        {
            symbolA.transform.localPosition = new Vector3(.00075f, -.0016f, .0217f);
            symbolA.transform.localRotation = Quaternion.Euler(0, 180, 0);
            symbolA.sharedMaterial = mixerMaterial;

            symbolB.transform.localPosition = new Vector3(.00075f, -.0016f, -.0217f);
            symbolB.transform.localRotation = Quaternion.Euler(0, 180, 0);
            symbolB.sharedMaterial = mixerMaterial;
        }
        else
        {
            symbolA.transform.localPosition = new Vector3(.0025f, .0012f, .0217f);
            symbolA.transform.localRotation = Quaternion.Euler(0, 0, 90);
            symbolA.sharedMaterial = splitterMaterial;

            symbolB.transform.localPosition = new Vector3(.0025f, .0012f, -.0217f);
            symbolB.transform.localRotation = Quaternion.Euler(0, 0, 90);
            symbolB.sharedMaterial = splitterMaterial;
        }

        if (jack.near != null)
        {
            jack.near.Destruct();
            jack.signal = null;
        }
        jack.outgoing = flow;

        if (flow) sig = mainSig;
        else sig = jack.signal;
    }

    void Update()
    {
        if (flow) return;

        if (sig != jack.signal)
        {
            sig = jack.signal;
        }
    }

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (sig == null)
        {
            SetArrayToSingleValue(buffer, buffer.Length, 0.0f);
            return;
        }

        sig.processBuffer(buffer, dspTime, channels);
    }
}
