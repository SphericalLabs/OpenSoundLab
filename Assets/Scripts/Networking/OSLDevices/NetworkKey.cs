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

public class NetworkKey : NetworkBehaviour
{
    public key[] keys;

    public readonly SyncList<bool> keyValues = new SyncList<bool>();

    private float[] lastKeyHitTimes;

    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (var button in keys)
        {
            keyValues.Add(button.isHit);
        }
    }

    private void Start()
    {
        lastKeyHitTimes = new float[keys.Length];
        //add dials on change callback event
        for (int i = 0; i < keys.Length; i++)
        {
            int index = i;
            keys[i].onKeyChangedEvent.AddListener(delegate { UpdateKeyIsHit(index); });
            keys[i].onKeyChangedEvent.AddListener(delegate { UpdateLastKeyHitTime(index); });
        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            keyValues.Callback += OnKeyUpdated;

            // Process initial SyncList payload
            for (int i = 0; i < keyValues.Count; i++)
            {
                OnKeyUpdated(SyncList<bool>.Operation.OP_ADD, i, keys[i].isHit, keyValues[i]);
            }
        }
    }

    void OnKeyUpdated(SyncList<bool>.Operation op, int index, bool oldValue, bool newValue)
    {
        switch (op)
        {
            case SyncList<bool>.Operation.OP_ADD:
                keys[index].phantomHit(newValue);
                break;
            case SyncList<bool>.Operation.OP_INSERT:
                break;
            case SyncList<bool>.Operation.OP_REMOVEAT:
                break;
            case SyncList<bool>.Operation.OP_SET:
                if (IsKeyHitCooldownOver(index))
                {
                    keys[index].phantomHit(newValue, true);
                }
                break;
            case SyncList<bool>.Operation.OP_CLEAR:
                break;
        }
    }

    public void UpdateKeyIsHit(int index)
    {
        Debug.Log($"Update key hit of index: {index} to value: {keys[index].isHit}");
        if (isServer)
        {
            keyValues[index] = keys[index].isHit;
        }
        else
        {
            CmdKeyButtonIsHit(index, keys[index].isHit);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdKeyButtonIsHit(int index, bool value)
    {
        keyValues[index] = value;
        keys[index].phantomHit(value, true);
    }


    public void UpdateLastKeyHitTime(int index)
    {
        if (index >= 0 && index < lastKeyHitTimes.Length)
        {
            lastKeyHitTimes[index] = Time.time;
        }
    }

    private bool IsKeyHitCooldownOver(int index)
    {
        if (lastKeyHitTimes[index] + 0.5f < Time.time)
        {
            return true;
        }
        return false;
    }
}

