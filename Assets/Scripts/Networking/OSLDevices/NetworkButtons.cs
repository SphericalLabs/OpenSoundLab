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
using static UnityEngine.Rendering.DebugUI;

public class NetworkButtons : NetworkBehaviour
{
    public button[] buttons;

    public readonly SyncList<bool> buttonValues = new SyncList<bool>();

    private float[] lastToggeldTimes;

    public override void OnStartServer()
    {
        base.OnStartServer();
        buttonValues.Clear();
        foreach (var button in buttons)
        {
            buttonValues.Add(button.isHit);
        }
    }

    private void Start()
    {
        lastToggeldTimes = new float[buttons.Length];
        //add dials on change callback event
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onToggleChangedEvent.AddListener(delegate { UpdateButtonIsHit(index); });
            buttons[i].onToggleChangedEvent.AddListener(delegate { UpdateLastToggledTime(index); });
        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            buttonValues.Callback += OnButtonUpdated;

            // Process initial SyncList payload
            for (int i = 0; i < buttonValues.Count; i++)
            {
                OnButtonUpdated(SyncList<bool>.Operation.OP_ADD, i, buttons[i].isHit, buttonValues[i]);
            }
        }
    }

    void OnButtonUpdated(SyncList<bool>.Operation op, int index, bool oldValue, bool newValue)
    {
        switch (op)
        {
            case SyncList<bool>.Operation.OP_ADD:
                if (buttons[index].isHit != newValue)
                {
                    buttons[index].keyHit(newValue, false);
                }
                break;
            case SyncList<bool>.Operation.OP_INSERT:
                break;
            case SyncList<bool>.Operation.OP_REMOVEAT:
                break;
            case SyncList<bool>.Operation.OP_SET:
                if (IsToggleCooldownOver(index))
                {
                    buttons[index].keyHit(newValue, false);
                }
                break;
            case SyncList<bool>.Operation.OP_CLEAR:
                break;
        }
    }

    public void UpdateButtonIsHit(int index)
    {
        Debug.Log($"Update button hit of index: {index} to value: {buttons[index].isHit}");
        if (isServer)
        {
            buttonValues[index] = buttons[index].isHit;
        }
        else
        {
            CmdUpdateButtonIsHit(index, buttons[index].isHit);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateButtonIsHit(int index, bool value)
    {
        buttonValues[index] = value;
        buttons[index].keyHit(value, false);
    }

    public void UpdateLastToggledTime(int index)
    {
        if (index >= 0 && index < lastToggeldTimes.Length)
        {
            lastToggeldTimes[index] = Time.time;
        }
    }

    private bool IsToggleCooldownOver(int index)
    {
        if (lastToggeldTimes[index] + 0.5f < Time.time)
        {
            return true;
        }
        return false;
    }
}
