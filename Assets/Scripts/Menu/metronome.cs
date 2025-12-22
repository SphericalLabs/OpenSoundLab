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
using System;


public class metronome : componentInterface
{
    public dial bpmDial, volumeDial;

    float bpm = 120f;
    float minBpm = 60f;
    float maxBpm = 180f;
    float pitchBendMult = 1f;
    public float PitchBendMult { get => pitchBendMult; set => pitchBendMult = value; }
    public delegate void PitchBendChangeHandler(float pitchBendMult);
    public event PitchBendChangeHandler PitchBendChange;

    public float bpmpercent = .1f;
    public float volumepercent = 0;

    public Transform rod;
    public TextMesh txt;

    public button recButton;
    public static metronome Instance;


    void Awake()
    {
        Instance = this;

        bpmDial = GetComponentInChildren<dial>();
        //TODO: This is not very pretty, but the masterBusRecorder needs a reference to the rec button in case a recording is stopped by the system (for example if the filesize limit is reached)!
        var buttons = GetComponentsInChildren<button>();
        foreach (button b in buttons)
        {
            if (b.buttonID == 5)
                recButton = b;
        }
    }

    private void Start()
    {
        readBpmDialAndBroadcast();
    }

    // this is called each time the menu is being activated
    void OnEnable()
    {
        //In case the recording has been stopped while the menu was disabled, we have to set the recButton toggle state to false:
        var recorderState = masterControl.instance.recorder.state;
        if (recorderState == masterBusRecorder.State.Idle)
        {
            recButton.phantomHit(false);
        }
    }

    bool rodDir = false;

        // Rod animation temporarily disabled until linked to Clock Phase output.

    public void readBpmDialAndBroadcast()
    {
        bpmpercent = bpmDial.percent;
        updateBpm();
    }

    public void updateBpm()
    {
        bpm = (float)Math.Round(Utils.map(bpmpercent, 0f, 1f, minBpm, maxBpm), 1) * pitchBendMult;
        masterControl.instance?.setBPM(bpm);
        txt.text = bpm.ToString("N1");
    }

    public void Reset()
    {
        SetBPM(bpm);
    }

    public void SetBPM(float targ)
    {
        bpmpercent = Utils.map(targ, minBpm, maxBpm, 0f, 1f);
        bpmDial.setPercent(bpmpercent, true);
        readBpmDialAndBroadcast();
    }

    public override void hit(bool on, int ID = -1)
    {
        // Legacy masterControl clock calls removed.
        // Clock controls now happen on spawnable Clock device.

        if (ID == 3 && on) pitchBendMult = 1 / 1.03f;
        if (ID == 3 && !on) pitchBendMult = 1;
        if (ID == 4 && on) pitchBendMult = 1 * 1.03f;
        if (ID == 4 && !on) pitchBendMult = 1;

        if (ID == 5) masterControl.instance.recorder.ToggleRec(on);

        updateBpm(); // temporary bpm nudging
    }



}
