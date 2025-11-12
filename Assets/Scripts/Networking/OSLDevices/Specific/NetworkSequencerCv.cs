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
using Mirror;
using System;
public class NetworkSequencerCv : NetworkSyncListener
{
    protected sequencerCVDeviceInterface sequencerCvDeviceInterface;

    NetworkButtons networkButtons;
    NetworkDials networkDials;
    NetworkSwitchs networkSwitchs;
    NetworkJacks networkJacks;


    protected virtual void Awake()
    {
        sequencerCvDeviceInterface = GetComponent<sequencerCVDeviceInterface>();
        sequencerCvDeviceInterface.beatSlider.onEndGrabEvents.AddListener(OnSync);
        sequencerCvDeviceInterface.stepSelect.onEndGrabEvents.AddListener(OnSync);
        sequencerCvDeviceInterface.xyHandle.onEndGrabEvents.AddListener(OnSync);

        networkButtons = GetComponent<NetworkButtons>();
        networkDials = GetComponent<NetworkDials>();
        networkSwitchs = GetComponent<NetworkSwitchs>();
        networkJacks = GetComponent<NetworkJacks>();

        // make sure that nothing is added manually to these scripts, since otherwise it probably would end up doubled after these GetComponentsInChildren calls
        // todo: get rid of GetComponentsInChildren, this would save about 0.5ms of the init time according to profiler on Windows, so Quest savings will be higher
        networkButtons.buttons = Utils.AddElementsToArray(sequencerCvDeviceInterface.GetComponentsInChildren<button>(true), networkButtons.buttons);
        networkDials.dials = Utils.AddElementsToArray(sequencerCvDeviceInterface.GetComponentsInChildren<dial>(true), networkDials.dials);
        networkSwitchs.switchs = Utils.AddElementsToArray(sequencerCvDeviceInterface.GetComponentsInChildren<basicSwitch>(true), networkSwitchs.switchs);
        networkJacks.omniJacks = Utils.AddElementsToArray(sequencerCvDeviceInterface.GetComponentsInChildren<omniJack>(true), networkJacks.omniJacks);

    }
    #region Mirror

    public void Start()
    {
        GetComponent<NetworkXHandles>().xValues.Callback += OnHandleUpdated;
    }

    void OnHandleUpdated(SyncList<float>.Operation op, int index, float oldValue, float newValue)
    {
        switch (op)
        {
            case SyncList<float>.Operation.OP_ADD:
                break;
            case SyncList<float>.Operation.OP_INSERT:
                break;
            case SyncList<float>.Operation.OP_REMOVEAT:
                break;
            case SyncList<float>.Operation.OP_SET:
                // careful, this is hardwiring index 0.
                // this will break if there will be more xHandles on SequencerCV in the future
                // and the stepSelect handle would have another index because of that.
                if (index == 0) sequencerCvDeviceInterface.UpdateStepSelect(true);
                break;
            case SyncList<float>.Operation.OP_CLEAR:
                break;
        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            CmdRequestSync();
        }
    }

    protected override void OnSync()
    {
        base.OnSync();

        if (isServer)
        {
            RpcUpdateCurStep(sequencerCvDeviceInterface.TargetStep);
        }
        else
        {
            CmdRequestSync();
        }
    }

    protected override void OnIntervalSync()
    {
        base.OnIntervalSync();
        if (isServer)
        {
            RpcUpdateCurStep(sequencerCvDeviceInterface.TargetStep);
        }
    }

    [Command(requiresAuthority = false)]
    protected virtual void CmdRequestSync()
    {
        Debug.Log($"{gameObject.name} CmdRequestSync curStep {sequencerCvDeviceInterface.TargetStep}");
        RpcUpdateCurStep(sequencerCvDeviceInterface.TargetStep);
    }

    [ClientRpc]
    protected virtual void RpcUpdateCurStep(int targetStep)
    {
        if (isClient && !isServer)
        {
            Debug.Log($"{gameObject.name} old curStep: {sequencerCvDeviceInterface.TargetStep}, new curStep {targetStep}");
            sequencerCvDeviceInterface.TargetStep = targetStep;
        }
    }
    #endregion
}
