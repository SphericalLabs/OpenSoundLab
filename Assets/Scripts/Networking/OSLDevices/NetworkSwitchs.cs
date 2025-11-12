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

using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkSwitchs : NetworkBehaviour
{
    public basicSwitch[] switchs;

    public readonly SyncList<bool> switchValues = new SyncList<bool>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (var s in switchs)
        {
            switchValues.Add(s.switchVal);
        }
    }

    private void Start()
    {
        //add dials on change callback event
        for (int i = 0; i < switchs.Length; i++)
        {
            int index = i;
            switchs[i].onSwitchChangedEvent.AddListener(delegate { UpdateSwitchValue(index); });
        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            switchValues.Callback += OnSwitchUpdated;

            // Process initial SyncList payload
            for (int i = 0; i < switchValues.Count; i++)
            {
                OnSwitchUpdated(SyncList<bool>.Operation.OP_ADD, i, switchs[i].switchVal, switchValues[i]);
            }
        }
    }

    void OnSwitchUpdated(SyncList<bool>.Operation op, int index, bool oldValue, bool newValue)
    {
        switch (op)
        {
            case SyncList<bool>.Operation.OP_ADD:
                switchs[index].setSwitch(newValue, true, true);
                break;
            case SyncList<bool>.Operation.OP_INSERT:
                break;
            case SyncList<bool>.Operation.OP_REMOVEAT:
                break;
            case SyncList<bool>.Operation.OP_SET:
                switchs[index].setSwitch(newValue, true, true);
                break;
            case SyncList<bool>.Operation.OP_CLEAR:
                break;
        }
    }

    public void UpdateSwitchValue(int index)
    {
        Debug.Log($"Update button hit of index: {index} to value: {switchs[index].switchVal}");
        if (isServer)
        {
            switchValues[index] = switchs[index].switchVal;
        }
        else
        {
            CmdUpdateSwitchIsChanged(index, switchs[index].switchVal);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateSwitchIsChanged(int index, bool value)
    {
        switchValues[index] = value;
        switchs[index].setSwitch(value, false, true);
    }
}

