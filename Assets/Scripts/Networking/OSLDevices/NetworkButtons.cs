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
using static UnityEngine.Rendering.DebugUI;

public class NetworkButtons : NetworkBehaviour
{
    public button[] buttons;

    public readonly SyncList<bool> buttonValues = new SyncList<bool>();

    private float[] lastToggeldTimes;
    private bool[] buttonListenerAdded;
    bool clientReady;

    public override void OnStartServer()
    {
        base.OnStartServer();
        buttonValues.Clear();
        if (buttons == null) buttons = new button[0];
        foreach (var button in buttons)
        {
            buttonValues.Add(button.isHit);
        }
    }

    private void Start()
    {
        if (buttons == null) buttons = new button[0];
        lastToggeldTimes = new float[buttons.Length];
        buttonListenerAdded = new bool[buttons.Length];
        //add dials on change callback event
        for (int i = 0; i < buttons.Length; i++)
        {
            addButtonListener(i);
        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            buttonValues.Callback += OnButtonUpdated;
            clientReady = true;

            // Process initial SyncList payload
            for (int i = 0; i < buttonValues.Count; i++)
            {
                if (i >= buttons.Length) break;
                OnButtonUpdated(SyncList<bool>.Operation.OP_ADD, i, buttons[i].isHit, buttonValues[i]);
            }
        }
    }

    void OnButtonUpdated(SyncList<bool>.Operation op, int index, bool oldValue, bool newValue)
    {
        if (buttons == null || index < 0 || index >= buttons.Length) return;
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

    public void registerButtons(button[] newButtons)
    {
        // Dynamic registration for runtime-created buttons (e.g., Sequencer row mutes).
        // Usage: call with newly spawned buttons so they get listeners + SyncList entries.
        // Note: this list is flat and ordered by append; no x/y scheme here.
        // Sequencer step buttons are intentionally excluded and synced elsewhere.
        if (newButtons == null || newButtons.Length == 0) return;
        if (buttons == null) buttons = new button[0];

        for (int i = 0; i < newButtons.Length; i++)
        {
            button newButton = newButtons[i];
            if (newButton == null) continue;
            if (containsButton(newButton)) continue;

            int index = buttons.Length;
            buttons = Utils.AddElementsToArray(buttons, new button[] { newButton });
            ensureListenerBuffers();
            addButtonListener(index);

            if (isServer)
            {
                buttonValues.Add(newButton.isHit);
            }
            else if (clientReady && index < buttonValues.Count)
            {
                OnButtonUpdated(SyncList<bool>.Operation.OP_ADD, index, newButton.isHit, buttonValues[index]);
            }
        }
    }

    public void UpdateButtonIsHit(int index)
    {
        if (buttons == null || index < 0 || index >= buttons.Length) return;
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
        if (buttons == null || index < 0 || index >= buttons.Length) return;
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
        if (lastToggeldTimes == null || index < 0 || index >= lastToggeldTimes.Length) return true;
        if (lastToggeldTimes[index] + 0.5f < Time.time)
        {
            return true;
        }
        return false;
    }

    void addButtonListener(int index)
    {
        if (buttons == null || index < 0 || index >= buttons.Length) return;
        if (buttonListenerAdded != null && buttonListenerAdded[index]) return;

        int localIndex = index;
        buttons[index].onToggleChangedEvent.AddListener(delegate { UpdateButtonIsHit(localIndex); });
        buttons[index].onToggleChangedEvent.AddListener(delegate { UpdateLastToggledTime(localIndex); });
        if (buttonListenerAdded != null) buttonListenerAdded[index] = true;
    }

    void ensureListenerBuffers()
    {
        if (lastToggeldTimes == null || lastToggeldTimes.Length != buttons.Length)
        {
            float[] nextTimes = new float[buttons.Length];
            if (lastToggeldTimes != null)
            {
                int count = Mathf.Min(lastToggeldTimes.Length, nextTimes.Length);
                for (int i = 0; i < count; i++) nextTimes[i] = lastToggeldTimes[i];
            }
            lastToggeldTimes = nextTimes;
        }

        if (buttonListenerAdded == null || buttonListenerAdded.Length != buttons.Length)
        {
            bool[] nextFlags = new bool[buttons.Length];
            if (buttonListenerAdded != null)
            {
                int count = Mathf.Min(buttonListenerAdded.Length, nextFlags.Length);
                for (int i = 0; i < count; i++) nextFlags[i] = buttonListenerAdded[i];
            }
            buttonListenerAdded = nextFlags;
        }
    }

    bool containsButton(button candidate)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == candidate) return true;
        }
        return false;
    }
}
