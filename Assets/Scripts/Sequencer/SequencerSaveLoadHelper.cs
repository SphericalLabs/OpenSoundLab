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

public class SequencerSaveLoadHelper
{
    sequencerDeviceInterface sequencer;

    public SequencerSaveLoadHelper(sequencerDeviceInterface sequencer)
    {
        this.sequencer = sequencer;
    }

    public SequencerData buildData()
    {
        SequencerData data = new SequencerData();
        data.deviceType = DeviceType.Sequencer;
        sequencer.GetTransformData(data);

        data.switchPlay = sequencer.running;
        data.resetJackID = sequencer.resetJack.transform.GetInstanceID();
        data.clockJackID = sequencer.clockJack.transform.GetInstanceID();
        data.phaseJackID = sequencer.phaseJack.transform.GetInstanceID();
        data.activePattern = sequencer.activePattern;
        int[] clampedDimensions = getClampedDimensions();
        data.dimensions = clampedDimensions;
        data.switchRange = sequencer.switchCVRange != null && sequencer.switchCVRange.switchVal;
        data.switchClockResetMode = sequencer.switchClockResetMode != null && sequencer.switchClockResetMode.switchVal;

        sequencer.readAllData();
        captureRowStates(data, clampedDimensions[0]);
        captureRowJackIds(data, clampedDimensions[0]);
        captureStepData(data, clampedDimensions[0], clampedDimensions[1]);

        return data;
    }

    public void loadData(SequencerData data, bool copyMode)
    {
        if (data == null) return;

        applyTransform(data);

        sequencer.activePattern = clampPattern(data.activePattern);

        applyStepData(data);

        int rows = getRowsFromData(data);
        int steps = getStepsFromData(data);
        sequencer.SetDimensions(rows, steps);

        if (sequencer.switchCVRange != null) sequencer.switchCVRange.setSwitch(data.switchRange, true);
        if (sequencer.switchClockResetMode != null)
        {
            sequencer.switchClockResetMode.setSwitch(data.switchClockResetMode, true);
        }

        sequencer.resetJack.SetID(data.resetJackID, copyMode);
        sequencer.clockJack.SetID(data.clockJackID, copyMode);
        sequencer.phaseJack.SetID(data.phaseJackID, copyMode);

        applyRowStates(data);
        applyRowJackIds(data, copyMode);
        applyStepUi();

        sequencer.forcePlay(data.switchPlay);
    }

    void applyTransform(SequencerData data)
    {
        sequencer.transform.localPosition = data.position;
        sequencer.transform.localRotation = data.rotation;
        sequencer.transform.localScale = data.scale;
    }

    void captureRowStates(SequencerData data, int rows)
    {
        button[] rowMutes = sequencer.getRowMutes();
        basicSwitch[] rowModes = sequencer.getRowModeSwitches();

        data.rowMutes = new bool[rows];
        data.rowModes = new bool[rows];

        for (int row = 0; row < rows; row++)
        {
            data.rowMutes[row] = rowMutes[row] != null && rowMutes[row].isHit;
            data.rowModes[row] = rowModes[row] != null && rowModes[row].switchVal;
        }
    }

    void captureRowJackIds(SequencerData data, int rows)
    {
        omniJack[] trigJacks = sequencer.getRowTriggerJacks();
        omniJack[] cvJacks = sequencer.getRowCvJacks();

        data.jackTriggerOutID = new int[rows];
        data.jackCvOutID = new int[rows];

        for (int row = 0; row < rows; row++)
        {
            if (trigJacks[row] != null) data.jackTriggerOutID[row] = trigJacks[row].transform.GetInstanceID();
            if (cvJacks[row] != null) data.jackCvOutID[row] = cvJacks[row].transform.GetInstanceID();
        }
    }

    void captureStepData(SequencerData data, int rows, int steps)
    {
        int maxPatterns = sequencer.maxPattern;
        basicSwitch[] rowModes = sequencer.getRowModeSwitches();

        data.stepBools = new bool[maxPatterns][][];
        data.stepFloats = new float[maxPatterns][][];

        for (int pattern = 0; pattern < maxPatterns; pattern++)
        {
            data.stepBools[pattern] = new bool[rows][];
            data.stepFloats[pattern] = new float[rows][];
            for (int row = 0; row < rows; row++)
            {
                bool isTriggerRow = rowModes != null
                    && row < rowModes.Length
                    && rowModes[row] != null
                    && rowModes[row].switchVal;
                if (isTriggerRow)
                {
                    data.stepBools[pattern][row] = new bool[steps];
                    for (int step = 0; step < steps; step++)
                    {
                        data.stepBools[pattern][row][step] = sequencer.stepBools[pattern, row, step];
                    }
                }
                else
                {
                    data.stepFloats[pattern][row] = new float[steps];
                    for (int step = 0; step < steps; step++)
                    {
                        data.stepFloats[pattern][row][step] = sequencer.stepFloats[pattern, row, step];
                    }
                }
            }
        }
    }

    void applyRowStates(SequencerData data)
    {
        button[] rowMutes = sequencer.getRowMutes();
        basicSwitch[] rowModes = sequencer.getRowModeSwitches();
        int maxRows = sequencer.getMaxRows();
        bool hasRowModes = data.rowModes != null;
        bool hasRowMutes = data.rowMutes != null;

        for (int row = 0; row < maxRows; row++)
        {
            if (row < rowModes.Length && rowModes[row] != null)
            {
                bool modeValue = hasRowModes && row < data.rowModes.Length ? data.rowModes[row] : rowModes[row].switchVal;
                rowModes[row].setSwitch(modeValue, true);
                sequencer.doModeSwitch(row);
            }

            if (row < rowMutes.Length && rowMutes[row] != null)
            {
                bool muteValue = hasRowMutes && row < data.rowMutes.Length ? data.rowMutes[row] : rowMutes[row].isHit;
                rowMutes[row].phantomHit(muteValue);
            }
        }
    }

    void applyRowJackIds(SequencerData data, bool copyMode)
    {
        omniJack[] trigJacks = sequencer.getRowTriggerJacks();
        omniJack[] cvJacks = sequencer.getRowCvJacks();
        int maxRows = sequencer.getMaxRows();
        bool hasTrigIds = data.jackTriggerOutID != null;
        bool hasCvIds = data.jackCvOutID != null;

        for (int row = 0; row < maxRows; row++)
        {
            if (trigJacks[row] != null && hasTrigIds && row < data.jackTriggerOutID.Length)
            {
                trigJacks[row].SetID(data.jackTriggerOutID[row], copyMode);
            }
            if (cvJacks[row] != null && hasCvIds && row < data.jackCvOutID.Length)
            {
                cvJacks[row].SetID(data.jackCvOutID[row], copyMode);
            }
        }
    }

    void applyStepData(SequencerData data)
    {
        int maxPatterns = sequencer.maxPattern;
        int maxRows = sequencer.getMaxRows();
        int maxSteps = sequencer.getMaxSteps();

        if (data.stepBools != null)
        {
            for (int pattern = 0; pattern < maxPatterns; pattern++)
            {
                bool[][] patternData = pattern < data.stepBools.Length ? data.stepBools[pattern] : null;
                for (int row = 0; row < maxRows; row++)
                {
                    bool[] rowData = (patternData != null && row < patternData.Length) ? patternData[row] : null;
                    for (int step = 0; step < maxSteps; step++)
                    {
                        if (rowData != null && step < rowData.Length)
                        {
                            sequencer.stepBools[pattern, row, step] = rowData[step];
                        }
                    }
                }
            }
        }

        if (data.stepFloats != null)
        {
            for (int pattern = 0; pattern < maxPatterns; pattern++)
            {
                float[][] patternData = pattern < data.stepFloats.Length ? data.stepFloats[pattern] : null;
                for (int row = 0; row < maxRows; row++)
                {
                    float[] rowData = (patternData != null && row < patternData.Length) ? patternData[row] : null;
                    for (int step = 0; step < maxSteps; step++)
                    {
                        if (rowData != null && step < rowData.Length)
                        {
                            sequencer.stepFloats[pattern, row, step] = rowData[step];
                        }
                    }
                }
            }
        }
    }

    void applyStepUi()
    {
        int[] curDimensions = sequencer.getCurrentDimensions();
        button[,] stepButtons = sequencer.getStepButtons();
        dial[,] stepDials = sequencer.getStepDials();

        for (int row = 0; row < curDimensions[0]; row++)
        {
            for (int step = 0; step < curDimensions[1]; step++)
            {
                if (stepButtons[row, step] != null)
                {
                    stepButtons[row, step].keyHit(sequencer.stepBools[sequencer.activePattern, row, step], false);
                }
                if (stepDials[row, step] != null)
                {
                    stepDials[row, step].setPercent(sequencer.stepFloats[sequencer.activePattern, row, step]);
                }
            }
        }
    }

    int clampPattern(int pattern)
    {
        if (sequencer.maxPattern < 1) return 0;
        return Mathf.Clamp(pattern, 0, sequencer.maxPattern - 1);
    }

    int getRowsFromData(SequencerData data)
    {
        if (data.dimensions == null || data.dimensions.Length < 2)
        {
            return Mathf.Clamp(sequencer.dimensions[0], 1, sequencer.getMaxRows());
        }
        return Mathf.Clamp(data.dimensions[0], 1, sequencer.getMaxRows());
    }

    int getStepsFromData(SequencerData data)
    {
        if (data.dimensions == null || data.dimensions.Length < 2)
        {
            return Mathf.Clamp(sequencer.dimensions[1], 1, sequencer.getMaxSteps());
        }
        return Mathf.Clamp(data.dimensions[1], 1, sequencer.getMaxSteps());
    }

    int[] getClampedDimensions()
    {
        int rows = 1;
        int steps = 1;
        int[] curDimensions = sequencer.getCurrentDimensions();
        if (curDimensions != null && curDimensions.Length >= 2)
        {
            rows = curDimensions[0];
            steps = curDimensions[1];
        }
        rows = Mathf.Clamp(rows, 1, sequencer.getMaxRows());
        steps = Mathf.Clamp(steps, 1, sequencer.getMaxSteps());
        return new int[] { rows, steps };
    }
}
