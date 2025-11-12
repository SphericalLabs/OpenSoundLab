// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright � 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
//
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
//
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
//
//
// Copyright � 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright � 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright � 2017 Apache 2.0 Google LLC SoundStage VR
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

public class embeddedSpeaker : MonoBehaviour
{
    public omniJack speakerOut;
    signalGenerator signalGen;
    signalGenerator curGen, prevGen;
    public speaker output;
    public AudioSource audio;
    public GameObject speakerRim;

    bool secondary = true;

    public bool activated = false;
    Material rimMat;
    void Awake()
    {
        signalGen = transform.parent.GetComponent<signalGenerator>();

        if (speakerRim != null)
        {
            rimMat = speakerRim.GetComponent<Renderer>().material;
            rimMat.SetFloat("_EmissionGain", .45f);
            speakerRim.SetActive(false);

        }
    }

    void Start()
    {
        audio.spatialize = (masterControl.instance.BinauralSetting == BinauralMode.All);
    }

    public void updateSecondary(bool on)
    {
        secondary = on;
        updateSpeaker();
    }

    void updateSpeaker()
    {
        if (output.incoming == null || !secondary)
        {
            if (speakerRim != null)
            {
                activated = false;
                speakerRim.SetActive(false);
            }
        }
        else
        {
            if (speakerRim != null && secondary)
            {
                activated = true;
                speakerRim.SetActive(true);
            }
        }
    }


    void Update()
    {
        if (speakerOut.near == null && speakerOut.far == null)
        { // nothing patched, use embeddedSpeaker
            curGen = signalGen;
        }
        else if (speakerOut.near != null && speakerOut.far.signal == null)
        { // plug already patched at output jack, but not yet in another side, use embeddedSpeaker
            curGen = signalGen;
        }
        else
        { // active external patching, do not use embeddedSpeaker
            curGen = null;
        }

        if (prevGen != curGen)
        {
            output.incoming = prevGen = curGen;
            updateSpeaker();
        }
    }
}
