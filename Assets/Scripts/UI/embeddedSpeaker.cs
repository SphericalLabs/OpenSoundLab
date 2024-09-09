// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
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
        audio.spatialize = (masterControl.instance.BinauralSetting == masterControl.BinauralMode.All);
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
