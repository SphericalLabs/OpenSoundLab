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

public class timelineTrackComponentInterface : componentInterface
{

    public int ID = 0;

    clipPlayerSimple player;
    public button triggerButton;
    public omniJack jackIn, sampleOut, pulseOut;
    public timelineTrackSignalGenerator signal;
    timelineDeviceInterface _deviceInterface;
    public embeddedSpeaker embedSpeaker;

    public enum sigSource
    {
        button,
        signal,
        timeline
    };

    public struct trackComponent
    {
        public int jackInID;
        public int sampleOutID;
        public int pulseOutID;
        public string label;
        public string filename;

        public trackComponent(int idA, int idB, int idC, string l, string f)
        {
            jackInID = idA;
            sampleOutID = idB;
            pulseOutID = idC;
            label = l;
            filename = f;
        }
    };

    public void Load(trackComponent t)
    {
        jackIn.SetID(t.jackInID, false);
        sampleOut.SetID(t.sampleOutID, false);
        pulseOut.SetID(t.pulseOutID, false);
        GetComponentInChildren<samplerLoad>().SetSample(t.label, t.filename);
    }

    void Awake()
    {
        player = GetComponent<clipPlayerSimple>();
        signal = GetComponent<timelineTrackSignalGenerator>();
        _deviceInterface = GetComponentInParent<timelineDeviceInterface>();
    }

    public trackComponent getTrackInfo()
    {
        string l = "";
        string f = "";
        GetComponentInChildren<samplerLoad>().getTapeInfo(out l, out f);
        return new trackComponent(
            jackIn.transform.GetInstanceID(),
            sampleOut.transform.GetInstanceID(),
            pulseOut.transform.GetInstanceID(),
            l, f);
    }

    void Update()
    {
        if (signal.incoming != jackIn.signal)
        {
            signal.incoming = jackIn.signal;
            if (signal.incoming == null) updateSignal(false, sigSource.signal);
        }
    }

    public bool isOutgoing()
    {
        bool a = (sampleOut.near != null);
        bool b = (pulseOut.near != null);
        bool c = embedSpeaker.activated;
        return a || b || c;
    }

    bool[] sourceState = new bool[] { false, false, false };
    public void updateSignal(bool on, sigSource source)
    {
        sourceState[(int)source] = on;
        setSignal(sourceState[0] || sourceState[1] || sourceState[2], sourceState[0] || sourceState[1]);
    }

    bool externalSignalOn = false;
    bool signalOn = false;
    public void setSignal(bool on, bool externOn) // from outside
    {
        if (signalOn != on)
        {
            signalOn = on;
            signal.setSignal(on);
        }

        if (externalSignalOn != externOn)
        {
            externalSignalOn = externOn;
            _deviceInterface.trackUpdate(ID, on);
        }
    }

    public void onTimelineEvent(bool on)
    {
        updateSignal(on, sigSource.timeline);
    }

    public override void hit(bool on, int ID = -1)
    {
        updateSignal(on, sigSource.button);
    }
}
