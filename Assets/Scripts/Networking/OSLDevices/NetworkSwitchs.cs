// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2026 OSLLv1 Sphericals OpenSoundLab
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
    bool clientReady;
    bool[] switchListenerAdded;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (switchs == null) switchs = new basicSwitch[0];
        foreach (var s in switchs)
        {
            switchValues.Add(s.switchVal);
        }
    }

    private void Start()
    {
        if (switchs == null) switchs = new basicSwitch[0];
        switchListenerAdded = new bool[switchs.Length];
        //add dials on change callback event
        for (int i = 0; i < switchs.Length; i++)
        {
            addSwitchListener(i);
        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            switchValues.Callback += OnSwitchUpdated;
            clientReady = true;

            // Process initial SyncList payload
            for (int i = 0; i < switchValues.Count; i++)
            {
                if (i >= switchs.Length) break;
                OnSwitchUpdated(SyncList<bool>.Operation.OP_ADD, i, switchs[i].switchVal, switchValues[i]);
            }
        }
    }

    void OnSwitchUpdated(SyncList<bool>.Operation op, int index, bool oldValue, bool newValue)
    {
        if (switchs == null || index < 0 || index >= switchs.Length) return;
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

    public void registerSwitches(basicSwitch[] newSwitches)
    {
        // Dynamic registration for runtime-created switches (e.g., Sequencer row mode switches).
        // Usage: call with newly spawned switches to add listeners + SyncList entries.
        // Note: this list is flat and ordered by append; no x/y scheme here.
        if (newSwitches == null || newSwitches.Length == 0) return;
        if (switchs == null) switchs = new basicSwitch[0];

        for (int i = 0; i < newSwitches.Length; i++)
        {
            basicSwitch newSwitch = newSwitches[i];
            if (newSwitch == null) continue;
            if (containsSwitch(newSwitch)) continue;

            int index = switchs.Length;
            switchs = Utils.AddElementsToArray(switchs, new basicSwitch[] { newSwitch });
            ensureListenerBuffers();
            addSwitchListener(index);

            if (isServer)
            {
                switchValues.Add(newSwitch.switchVal);
            }
            else if (clientReady && index < switchValues.Count)
            {
                OnSwitchUpdated(SyncList<bool>.Operation.OP_ADD, index, newSwitch.switchVal, switchValues[index]);
            }
        }
    }

    public void UpdateSwitchValue(int index)
    {
        if (switchs == null || index < 0 || index >= switchs.Length) return;
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
        if (switchs == null || index < 0 || index >= switchs.Length) return;
        switchValues[index] = value;
        switchs[index].setSwitch(value, false, true);
    }

    void addSwitchListener(int index)
    {
        if (switchs == null || index < 0 || index >= switchs.Length) return;
        if (switchListenerAdded != null && switchListenerAdded[index]) return;

        int localIndex = index;
        switchs[index].onSwitchChangedEvent.AddListener(delegate { UpdateSwitchValue(localIndex); });
        if (switchListenerAdded != null) switchListenerAdded[index] = true;
    }

    void ensureListenerBuffers()
    {
        if (switchListenerAdded == null || switchListenerAdded.Length != switchs.Length)
        {
            bool[] nextFlags = new bool[switchs.Length];
            if (switchListenerAdded != null)
            {
                int count = Mathf.Min(switchListenerAdded.Length, nextFlags.Length);
                for (int i = 0; i < count; i++) nextFlags[i] = switchListenerAdded[i];
            }
            switchListenerAdded = nextFlags;
        }
    }

    bool containsSwitch(basicSwitch candidate)
    {
        for (int i = 0; i < switchs.Length; i++)
        {
            if (switchs[i] == candidate) return true;
        }
        return false;
    }
}
