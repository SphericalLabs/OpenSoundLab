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
        GetComponent<NetworkXHandles>().xValues.Callback += OnHandleUpdated;
        registerStepListeners();
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

    void OnStepBoolUpdated(SyncList<bool>.Operation op, int index, bool oldValue, bool newValue)
    {
        if (op != SyncList<bool>.Operation.OP_ADD && op != SyncList<bool>.Operation.OP_SET) return;
        if (!isToggleCooldownOver(index) && op == SyncList<bool>.Operation.OP_SET) return;
        if (!tryGetStepIndices(index, out int pattern, out int row, out int step)) return;

        suppressStepEvents = true;
        sequencerDeviceInterface.applyNetworkStepBool(pattern, row, step, newValue);
        suppressStepEvents = false;
    }

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

    void registerStepListeners()
    {
        if (stepListenersReady) return;
        cacheStepDimensions();
        cacheStepGrid();

        if (stepButtons == null || stepDials == null) return;

        int rows = stepButtons.GetLength(0);
        int steps = stepButtons.GetLength(1);
        for (int row = 0; row < rows; row++)
        {
            for (int step = 0; step < steps; step++)
            {
                button b = stepButtons[row, step];
                if (b != null)
                {
                    int localRow = row;
                    int localStep = step;
                    b.onToggleChangedEvent.AddListener(delegate { onStepButtonChanged(localRow, localStep); });
                }

                dial d = stepDials[row, step];
                if (d != null)
                {
                    int localRow = row;
                    int localStep = step;
                    d.onPercentChangedEvent.AddListener(delegate { onStepDialChanged(localRow, localStep); });
                    d.onEndGrabEvents.AddListener(delegate { updateLastDialGrabTime(localRow, localStep); });
                }
            }
        }

        stepListenersReady = true;
    }

    int getStepIndex(int pattern, int row, int step)
    {
        if (stepsPerPattern <= 0) return -1;
        return pattern * stepsPerPattern + row * maxSteps + step;
    }

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

    void cacheStepGrid()
    {
        stepButtons = sequencerDeviceInterface.getStepButtons();
        stepDials = sequencerDeviceInterface.getStepDials();
    }

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
