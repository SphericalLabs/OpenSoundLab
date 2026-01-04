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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
public class NetworkSequencer : NetworkSyncListener
{
    protected sequencerDeviceInterface sequencerDeviceInterface;

    NetworkButtons networkButtons;
    NetworkDials networkDials;
    NetworkSwitchs networkSwitchs;
    NetworkJacks networkJacks;

    public readonly SyncList<bool> stepBoolValues = new SyncList<bool>();
    public readonly SyncList<float> stepFloatValues = new SyncList<float>();

    button[,] stepButtons;
    dial[,] stepDials;

    int maxRows;
    int maxSteps;
    int maxPatterns;
    int stepsPerPattern;
    int stepValueCount;

    float[] lastStepToggleTimes;
    float[] lastStepDialGrabTimes;

    bool suppressStepEvents;
    bool stepListenersReady;
    int lastObservedRows = -1;
    int lastObservedSteps = -1;

    bool[,] stepButtonListenerAdded;
    bool[,] stepDialListenerAdded;

    bool[] rowMuteRegistered;
    bool[] rowModeRegistered;
    bool[] rowTriggerJackRegistered;
    bool[] rowCvJackRegistered;

    protected virtual void Awake()
    {
        sequencerDeviceInterface = GetComponent<sequencerDeviceInterface>();
        // sequencerDeviceInterface.beatSlider.onEndGrabEvents.AddListener(OnSync);
        sequencerDeviceInterface.stepSelect.onEndGrabEvents.AddListener(OnSync);
        sequencerDeviceInterface.xyHandle.onEndGrabEvents.AddListener(OnSync);

        networkButtons = GetComponent<NetworkButtons>();
        networkDials = GetComponent<NetworkDials>();
        networkSwitchs = GetComponent<NetworkSwitchs>();
        networkJacks = GetComponent<NetworkJacks>();

        cacheStepDimensions();
        cacheStepGrid();
        initializeRowTracking();

        button[] stepButtonsFlat = flattenStepButtons(stepButtons);
        dial[] stepDialsFlat = flattenStepDials(stepDials);

        if (networkButtons != null)
        {
            button[] allButtons = sequencerDeviceInterface.GetComponentsInChildren<button>(true);
            if (stepButtonsFlat.Length > 0) allButtons = Utils.RemoveElementsFromArray(allButtons, stepButtonsFlat);
            networkButtons.buttons = allButtons;
        }

        if (networkDials != null)
        {
            dial[] allDials = sequencerDeviceInterface.GetComponentsInChildren<dial>(true);
            if (stepDialsFlat.Length > 0) allDials = Utils.RemoveElementsFromArray(allDials, stepDialsFlat);
            networkDials.dials = allDials;
        }

        if (networkSwitchs != null)
        {
            networkSwitchs.switchs = Utils.AddElementsToArray(sequencerDeviceInterface.GetComponentsInChildren<basicSwitch>(true), networkSwitchs.switchs);
        }

        if (networkJacks != null)
        {
            networkJacks.omniJacks = Utils.AddElementsToArray(sequencerDeviceInterface.GetComponentsInChildren<omniJack>(true), networkJacks.omniJacks);
        }

    }
    #region Mirror

    public void Start()
    {
        base.Start();
        GetComponent<NetworkXHandles>().xValues.Callback += OnHandleUpdated;
        registerStepListeners();
    }

    void Update()
    {
        updateStepListeners();
        updateRowNetworking();
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
                // this will break if there will be more xHandles on Sequencer in the future
                // and the stepSelect handle would have another index because of that.
                if (index == 0) sequencerDeviceInterface.UpdateStepSelect(true);
                break;
            case SyncList<float>.Operation.OP_CLEAR:
                break;
        }
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            stepBoolValues.Callback += OnStepBoolUpdated;
            stepFloatValues.Callback += OnStepFloatUpdated;

            for (int i = 0; i < stepBoolValues.Count; i++)
            {
                OnStepBoolUpdated(SyncList<bool>.Operation.OP_ADD, i, false, stepBoolValues[i]);
            }

            for (int i = 0; i < stepFloatValues.Count; i++)
            {
                OnStepFloatUpdated(SyncList<float>.Operation.OP_ADD, i, 0f, stepFloatValues[i]);
            }

            CmdRequestSync();
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        initializeStepValues();
    }

    protected override void OnSync()
    {
        base.OnSync();

        if (isServer)
        {
            RpcUpdateCurStep(sequencerDeviceInterface.TargetStep);
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
            RpcUpdateCurStep(sequencerDeviceInterface.TargetStep);
        }
    }

    [Command(requiresAuthority = false)]
    protected virtual void CmdRequestSync()
    {
        Debug.Log($"{gameObject.name} CmdRequestSync curStep {sequencerDeviceInterface.TargetStep}");
        RpcUpdateCurStep(sequencerDeviceInterface.TargetStep);
    }

    [ClientRpc]
    protected virtual void RpcUpdateCurStep(int targetStep)
    {
        if (isClient && !isServer)
        {
            Debug.Log($"{gameObject.name} old curStep: {sequencerDeviceInterface.TargetStep}, new curStep {targetStep}");
            sequencerDeviceInterface.TargetStep = targetStep;
        }
    }

    // SyncList -> sequencer step toggles.
    void OnStepBoolUpdated(SyncList<bool>.Operation op, int index, bool oldValue, bool newValue)
    {
        if (op != SyncList<bool>.Operation.OP_ADD && op != SyncList<bool>.Operation.OP_SET) return;
        if (!isToggleCooldownOver(index) && op == SyncList<bool>.Operation.OP_SET) return;
        if (!tryGetStepIndices(index, out int pattern, out int row, out int step)) return;

        suppressStepEvents = true;
        sequencerDeviceInterface.applyNetworkStepBool(pattern, row, step, newValue);
        suppressStepEvents = false;
    }

    // SyncList -> sequencer step dial values.
    void OnStepFloatUpdated(SyncList<float>.Operation op, int index, float oldValue, float newValue)
    {
        if (op != SyncList<float>.Operation.OP_ADD && op != SyncList<float>.Operation.OP_SET) return;
        if (op == SyncList<float>.Operation.OP_SET && !isDialCooldownOver(index)) return;
        if (!tryGetStepIndices(index, out int pattern, out int row, out int step)) return;

        if (op == SyncList<float>.Operation.OP_SET && isDialGrabbed(pattern, row, step)) return;

        suppressStepEvents = true;
        sequencerDeviceInterface.applyNetworkStepFloat(pattern, row, step, newValue);
        suppressStepEvents = false;
    }

    // Local step toggle -> SyncList update (current pattern only).
    void onStepButtonChanged(int row, int step)
    {
        if (suppressStepEvents) return;
        int pattern = Mathf.Clamp(sequencerDeviceInterface.activePattern, 0, maxPatterns - 1);
        int index = getStepIndex(pattern, row, step);
        if (index < 0 || index >= stepBoolValues.Count) return;

        lastStepToggleTimes[index] = Time.time;
        bool value = stepButtons[row, step].isHit;
        if (isServer)
        {
            stepBoolValues[index] = value;
        }
        else
        {
            CmdUpdateStepBool(index, value);
        }
    }

    // Local step dial -> SyncList update (current pattern only).
    void onStepDialChanged(int row, int step)
    {
        if (suppressStepEvents) return;
        int pattern = Mathf.Clamp(sequencerDeviceInterface.activePattern, 0, maxPatterns - 1);
        int index = getStepIndex(pattern, row, step);
        if (index < 0 || index >= stepFloatValues.Count) return;

        float value = stepDials[row, step].percent;
        if (isServer)
        {
            stepFloatValues[index] = value;
        }
        else
        {
            CmdUpdateStepFloat(index, value);
        }
    }

    [Command(requiresAuthority = false)]
    void CmdUpdateStepBool(int index, bool value)
    {
        if (index < 0 || index >= stepBoolValues.Count) return;
        stepBoolValues[index] = value;
        if (tryGetStepIndices(index, out int pattern, out int row, out int step))
        {
            sequencerDeviceInterface.applyNetworkStepBool(pattern, row, step, value);
        }
    }

    [Command(requiresAuthority = false)]
    void CmdUpdateStepFloat(int index, float value)
    {
        if (index < 0 || index >= stepFloatValues.Count) return;
        stepFloatValues[index] = value;
        if (tryGetStepIndices(index, out int pattern, out int row, out int step))
        {
            sequencerDeviceInterface.applyNetworkStepFloat(pattern, row, step, value);
        }
    }

    // Cooldown timer to avoid clobbering recent user edits.
    void updateLastDialGrabTime(int row, int step)
    {
        int pattern = Mathf.Clamp(sequencerDeviceInterface.activePattern, 0, maxPatterns - 1);
        int index = getStepIndex(pattern, row, step);
        if (index < 0 || index >= lastStepDialGrabTimes.Length) return;
        lastStepDialGrabTimes[index] = Time.time;
    }

    bool isToggleCooldownOver(int index)
    {
        if (index < 0 || index >= lastStepToggleTimes.Length) return true;
        return lastStepToggleTimes[index] + 0.5f < Time.time;
    }

    bool isDialCooldownOver(int index)
    {
        if (index < 0 || index >= lastStepDialGrabTimes.Length) return true;
        return lastStepDialGrabTimes[index] + 0.5f < Time.time;
    }

    bool isDialGrabbed(int pattern, int row, int step)
    {
        if (pattern != sequencerDeviceInterface.activePattern) return false;
        if (stepDials == null) return false;
        dial d = stepDials[row, step];
        if (d == null) return false;
        return d.curState == manipObject.manipState.grabbed;
    }

    // Server-side fill of full step state into SyncLists.
    void initializeStepValues()
    {
        cacheStepDimensions();
        if (stepValueCount <= 0) return;

        stepBoolValues.Clear();
        stepFloatValues.Clear();

        for (int p = 0; p < maxPatterns; p++)
        {
            for (int row = 0; row < maxRows; row++)
            {
                for (int step = 0; step < maxSteps; step++)
                {
                    stepBoolValues.Add(sequencerDeviceInterface.stepBools[p, row, step]);
                    stepFloatValues.Add(sequencerDeviceInterface.stepFloats[p, row, step]);
                }
            }
        }
    }

    // Initial listener hookup for any currently spawned steps.
    void registerStepListeners()
    {
        cacheStepDimensions();
        cacheStepGrid();
        initializeListenerBuffers();
        updateStepListeners();
        stepListenersReady = true;
    }

    // Register row mutes/mode switches/jacks as rows are created.
    void updateRowNetworking()
    {
        if (sequencerDeviceInterface == null) return;

        int rows = Mathf.Clamp(sequencerDeviceInterface.dimensions[0], 1, maxRows);
        if (rowMuteRegistered == null || rowMuteRegistered.Length != maxRows) initializeRowTracking();

        button[] rowMutes = sequencerDeviceInterface.getRowMutes();
        basicSwitch[] rowModes = sequencerDeviceInterface.getRowModeSwitches();
        omniJack[] rowTrigJacks = sequencerDeviceInterface.getRowTriggerJacks();
        omniJack[] rowCvJacks = sequencerDeviceInterface.getRowCvJacks();

        for (int row = 0; row < rows; row++)
        {
            if (networkButtons != null && !rowMuteRegistered[row] && rowMutes != null && rowMutes[row] != null)
            {
                networkButtons.registerButtons(new button[] { rowMutes[row] });
                rowMuteRegistered[row] = true;
            }

            if (networkSwitchs != null && !rowModeRegistered[row] && rowModes != null && rowModes[row] != null)
            {
                networkSwitchs.registerSwitches(new basicSwitch[] { rowModes[row] });
                rowModeRegistered[row] = true;
            }

            if (networkJacks != null && rowTrigJacks != null && rowCvJacks != null)
            {
                if (!rowTriggerJackRegistered[row] && rowTrigJacks[row] != null)
                {
                    networkJacks.registerJacks(new omniJack[] { rowTrigJacks[row] });
                    rowTriggerJackRegistered[row] = true;
                }

                if (!rowCvJackRegistered[row] && rowCvJacks[row] != null)
                {
                    networkJacks.registerJacks(new omniJack[] { rowCvJacks[row] });
                    rowCvJackRegistered[row] = true;
                }
            }
        }
    }

    // Register listeners for newly created step buttons/dials.
    void updateStepListeners()
    {
        if (sequencerDeviceInterface == null) return;
        if (stepButtons == null || stepDials == null) return;

        int rows = Mathf.Clamp(sequencerDeviceInterface.dimensions[0], 1, maxRows);
        int steps = Mathf.Clamp(sequencerDeviceInterface.dimensions[1], 1, maxSteps);
        if (rows == lastObservedRows && steps == lastObservedSteps && stepListenersReady) return;

        initializeListenerBuffers();

        for (int row = 0; row < rows; row++)
        {
            for (int step = 0; step < steps; step++)
            {
                if (!stepButtonListenerAdded[row, step])
                {
                    button b = stepButtons[row, step];
                    if (b != null)
                    {
                        int localRow = row;
                        int localStep = step;
                        b.onToggleChangedEvent.AddListener(delegate { onStepButtonChanged(localRow, localStep); });
                        stepButtonListenerAdded[row, step] = true;
                    }
                }

                if (!stepDialListenerAdded[row, step])
                {
                    dial d = stepDials[row, step];
                    if (d != null)
                    {
                        int localRow = row;
                        int localStep = step;
                        d.onPercentChangedEvent.AddListener(delegate { onStepDialChanged(localRow, localStep); });
                        d.onEndGrabEvents.AddListener(delegate { updateLastDialGrabTime(localRow, localStep); });
                        stepDialListenerAdded[row, step] = true;
                    }
                }
            }
        }

        lastObservedRows = rows;
        lastObservedSteps = steps;
    }

    // Row-major indexing for SyncList storage.
    int getStepIndex(int pattern, int row, int step)
    {
        if (stepsPerPattern <= 0) return -1;
        return pattern * stepsPerPattern + row * maxSteps + step;
    }

    // Reverse lookup for SyncList indices.
    bool tryGetStepIndices(int index, out int pattern, out int row, out int step)
    {
        pattern = 0;
        row = 0;
        step = 0;

        if (index < 0 || index >= stepValueCount) return false;
        if (stepsPerPattern <= 0 || maxSteps <= 0) return false;

        pattern = index / stepsPerPattern;
        int offset = index - pattern * stepsPerPattern;
        row = offset / maxSteps;
        step = offset - row * maxSteps;
        return true;
    }

    // Cache grid bounds and allocate cooldown buffers.
    void cacheStepDimensions()
    {
        maxRows = Mathf.Max(1, sequencerDeviceInterface.getMaxRows());
        maxSteps = Mathf.Max(1, sequencerDeviceInterface.getMaxSteps());
        maxPatterns = Mathf.Max(1, sequencerDeviceInterface.maxPattern);
        stepsPerPattern = maxRows * maxSteps;
        stepValueCount = stepsPerPattern * maxPatterns;

        if (lastStepToggleTimes == null || lastStepToggleTimes.Length != stepValueCount)
        {
            lastStepToggleTimes = new float[stepValueCount];
            for (int i = 0; i < lastStepToggleTimes.Length; i++)
            {
                lastStepToggleTimes[i] = -1f;
            }
        }

        if (lastStepDialGrabTimes == null || lastStepDialGrabTimes.Length != stepValueCount)
        {
            lastStepDialGrabTimes = new float[stepValueCount];
            for (int i = 0; i < lastStepDialGrabTimes.Length; i++)
            {
                lastStepDialGrabTimes[i] = -1f;
            }
        }
    }

    // Fetch current grid arrays from the sequencer.
    void cacheStepGrid()
    {
        stepButtons = sequencerDeviceInterface.getStepButtons();
        stepDials = sequencerDeviceInterface.getStepDials();
    }

    // Track which rows have been registered with the generic network scripts.
    void initializeRowTracking()
    {
        rowMuteRegistered = new bool[maxRows];
        rowModeRegistered = new bool[maxRows];
        rowTriggerJackRegistered = new bool[maxRows];
        rowCvJackRegistered = new bool[maxRows];
    }

    // Track which steps already have event listeners attached.
    void initializeListenerBuffers()
    {
        if (stepButtonListenerAdded == null || stepButtonListenerAdded.GetLength(0) != maxRows || stepButtonListenerAdded.GetLength(1) != maxSteps)
        {
            stepButtonListenerAdded = new bool[maxRows, maxSteps];
        }

        if (stepDialListenerAdded == null || stepDialListenerAdded.GetLength(0) != maxRows || stepDialListenerAdded.GetLength(1) != maxSteps)
        {
            stepDialListenerAdded = new bool[maxRows, maxSteps];
        }
    }

    // Helper: flatten step button grid for filtering.
    button[] flattenStepButtons(button[,] buttons)
    {
        if (buttons == null) return new button[0];
        int rows = buttons.GetLength(0);
        int steps = buttons.GetLength(1);
        List<button> list = new List<button>(rows * steps);
        for (int row = 0; row < rows; row++)
        {
            for (int step = 0; step < steps; step++)
            {
                button b = buttons[row, step];
                if (b != null) list.Add(b);
            }
        }
        return list.ToArray();
    }

    // Helper: flatten step dial grid for filtering.
    dial[] flattenStepDials(dial[,] dials)
    {
        if (dials == null) return new dial[0];
        int rows = dials.GetLength(0);
        int steps = dials.GetLength(1);
        List<dial> list = new List<dial>(rows * steps);
        for (int row = 0; row < rows; row++)
        {
            for (int step = 0; step < steps; step++)
            {
                dial d = dials[row, step];
                if (d != null) list.Add(d);
            }
        }
        return list.ToArray();
    }
    #endregion
}
