// Copyright 2017-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
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

public class miniSamplerComponentInterface : componentInterface
{
    clipPlayerSimple player;
    public button muteButton;
    public omniJack jackSampleOut, jackPitch, jackAmp;
    public dial dialPitch, dialAmp;

    void Awake()
    {
        player = GetComponent<clipPlayerSimple>();
    }

    void Update()
    {
        player.playbackSpeed = 1f * Mathf.Pow(2, Utils.map(dialPitch.percent, 0f, 1f, -4f, 4f));
        player.amplitude = Mathf.Pow(dialAmp.percent, 2);

        if (player.freqExpGen != jackPitch.signal) player.freqExpGen = jackPitch.signal;
        if (player.ampGen != jackAmp.signal) player.ampGen = jackAmp.signal;

        player.seqMuted = muteButton.isHit;
    }

    public override void hit(bool on, int ID = -1)
    {
        player.amplitude = on ? 0 : 1;
    }
}
