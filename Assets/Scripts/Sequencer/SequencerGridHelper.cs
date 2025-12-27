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

using UnityEngine;

public class SequencerGridHelper
{
    sequencerDeviceInterface sequencer;

    public SequencerGridHelper(sequencerDeviceInterface sequencer)
    {
        this.sequencer = sequencer;
    }

    public void spawnInitialDimensions()
    {
        // Default dimensions are driven by the stretch handle in the prefab.
        sequencer.dimensions[0] = 4;
        sequencer.dimensions[1] = 8;
        setDimensions(sequencer.dimensions[0], sequencer.dimensions[1]);
    }

    public void setDimensions(int rows, int steps)
    {
        sequencer.dimensions[0] = rows;
        sequencer.dimensions[1] = steps;

        float cubeConst = sequencer.getCubeConst();
        Vector3 p = sequencer.stretchNode.localPosition;
        p.y = rows * -cubeConst - cubeConst * .75f;
        p.x = steps * -cubeConst - cubeConst * .75f;
        sequencer.stretchNode.localPosition = p;

        updateDimensions();
    }

    public void updateDimensions()
    {
        int[] curDimensions = sequencer.getCurrentDimensions();
        if (sequencer.dimensions[0] == curDimensions[0] && sequencer.dimensions[1] == curDimensions[1]) return;

        sequencer.stretchNode.GetComponent<xyHandle>().pulse();
        if (sequencer.dimensions[1] > curDimensions[1])
        {
            addColumns(sequencer.dimensions[1] - curDimensions[1]);
        }
        else if (sequencer.dimensions[1] < curDimensions[1])
        {
            removeColumns(curDimensions[1] - sequencer.dimensions[1]);
        }

        if (sequencer.dimensions[0] > curDimensions[0])
        {
            addRows(sequencer.dimensions[0] - curDimensions[0]);
        }
        else if (sequencer.dimensions[0] < curDimensions[0])
        {
            removeRows(curDimensions[0] - sequencer.dimensions[0]);
        }

        sequencer.dimensionDisplays[0].text = curDimensions[1] + " X " + curDimensions[0];
    }

    void addColumns(int count)
    {
        int[] curDimensions = sequencer.getCurrentDimensions();
        basicSwitch[] rowModes = sequencer.getRowModeSwitches();

        for (int i = 0; i < count; i++)
        {
            for (int row = 0; row < curDimensions[0]; row++)
            {
                ensureStepPrefabs(row, curDimensions[1]);
                if (rowModes[row].switchVal)
                {
                    sequencer.stepButtonTrans[row, curDimensions[1]].gameObject.SetActive(true);
                }
                else
                {
                    sequencer.stepDialTrans[row, curDimensions[1]].gameObject.SetActive(true);
                }
            }
            curDimensions[1]++;
        }

        float cubeConst = sequencer.getCubeConst();
        for (int row = 0; row < sequencer.getMaxRows(); row++)
        {
            if (sequencer.jackOutTrigTrans[row] != null) moveByOffset(sequencer.jackOutTrigTrans[row], -cubeConst * count);
            if (sequencer.jackOutCVTrans[row] != null) moveByOffset(sequencer.jackOutCVTrans[row], -cubeConst * count);
            if (sequencer.controlPanelTrans[row] != null) moveByOffset(sequencer.controlPanelTrans[row], -cubeConst * count);
        }

        sequencer.stepSelect.xBounds.x = -cubeConst * (curDimensions[1] - 1);
        sequencer.stepSelect.updatePos(sequencer.stepSelect.transform.localPosition.x);
    }

    void removeColumns(int count)
    {
        int[] curDimensions = sequencer.getCurrentDimensions();
        button[,] stepButtons = sequencer.getStepButtons();
        dial[,] stepDials = sequencer.getStepDials();

        for (int i = 0; i < count; i++)
        {
            for (int row = 0; row < curDimensions[0]; row++)
            {
                if (stepButtons[row, curDimensions[1] - 1] != null)
                {
                    stepButtons[row, curDimensions[1] - 1].Highlight(false);
                }
                if (sequencer.stepButtonTrans[row, curDimensions[1] - 1] != null)
                {
                    sequencer.stepButtonTrans[row, curDimensions[1] - 1].gameObject.SetActive(false);
                }

                if (sequencer.stepDialTrans[row, curDimensions[1] - 1] != null)
                {
                    sequencer.stepDialTrans[row, curDimensions[1] - 1].gameObject.SetActive(false);
                }
            }
            curDimensions[1]--;
        }

        float cubeConst = sequencer.getCubeConst();
        for (int row = 0; row < sequencer.getMaxRows(); row++)
        {
            if (sequencer.jackOutTrigTrans[row] != null) moveByOffset(sequencer.jackOutTrigTrans[row], cubeConst * count);
            if (sequencer.jackOutCVTrans[row] != null) moveByOffset(sequencer.jackOutCVTrans[row], cubeConst * count);
            if (sequencer.controlPanelTrans[row] != null) moveByOffset(sequencer.controlPanelTrans[row], cubeConst * count);
        }

        sequencer.stepSelect.xBounds.x = -cubeConst * (curDimensions[1] - 1);
        sequencer.stepSelect.updatePos(sequencer.stepSelect.transform.localPosition.x);
    }

    void addRows(int count)
    {
        int[] curDimensions = sequencer.getCurrentDimensions();
        basicSwitch[] rowModes = sequencer.getRowModeSwitches();

        for (int i = 0; i < count; i++)
        {
            int rowIndex = curDimensions[0];
            ensureRowPrefabs(rowIndex);
            for (int step = 0; step < curDimensions[1]; step++)
            {
                ensureStepPrefabs(rowIndex, step);
                if (rowModes[rowIndex].switchVal)
                {
                    sequencer.stepButtonTrans[rowIndex, step].gameObject.SetActive(true);
                    if (sequencer.jackOutTrigTrans[rowIndex] != null) sequencer.jackOutTrigTrans[rowIndex].gameObject.SetActive(true);
                }
                else
                {
                    sequencer.stepDialTrans[rowIndex, step].gameObject.SetActive(true);
                    if (sequencer.jackOutCVTrans[rowIndex] != null) sequencer.jackOutCVTrans[rowIndex].gameObject.SetActive(true);
                }

                if (sequencer.controlPanelTrans[rowIndex] != null) sequencer.controlPanelTrans[rowIndex].gameObject.SetActive(true);
            }
            curDimensions[0]++;
        }

        updateStepSelectVertical();
    }

    void removeRows(int rows)
    {
        int[] curDimensions = sequencer.getCurrentDimensions();
        button[,] stepButtons = sequencer.getStepButtons();
        dial[,] stepDials = sequencer.getStepDials();

        for (int row = 0; row < rows; row++)
        {
            for (int step = 0; step < curDimensions[1]; step++)
            {
                int rowIndex = curDimensions[0] - 1;
                if (rowIndex < 0) continue;

                if (stepButtons[rowIndex, step] != null)
                {
                    stepButtons[rowIndex, step].Highlight(false);
                    stepButtons[rowIndex, step].keyHit(false);
                }
                if (sequencer.stepButtonTrans[rowIndex, step] != null)
                {
                    sequencer.stepButtonTrans[rowIndex, step].gameObject.SetActive(false);
                }
                if (stepDials[rowIndex, step] != null)
                {
                    stepDials[rowIndex, step].setPercent(0.5f);
                }
                if (sequencer.stepDialTrans[rowIndex, step] != null)
                {
                    sequencer.stepDialTrans[rowIndex, step].gameObject.SetActive(false);
                }
            }

            int lastRow = curDimensions[0] - 1;
            if (lastRow >= 0)
            {
                if (sequencer.jackOutTrigTrans[lastRow] != null)
                {
                    sequencer.jackOutTrigTrans[lastRow].GetComponentInChildren<omniJack>().endConnection(true, true);
                    sequencer.jackOutTrigTrans[lastRow].gameObject.SetActive(false);
                }
                if (sequencer.jackOutCVTrans[lastRow] != null)
                {
                    sequencer.jackOutCVTrans[lastRow].GetComponentInChildren<omniJack>().endConnection(true, true);
                    sequencer.jackOutCVTrans[lastRow].gameObject.SetActive(false);
                }
                if (sequencer.controlPanelTrans[lastRow] != null) sequencer.controlPanelTrans[lastRow].gameObject.SetActive(false);
            }
            curDimensions[0]--;
        }

        updateStepSelectVertical();
    }

    void moveByOffset(Transform t, float offset)
    {
        Vector3 pJ = t.localPosition;
        pJ.x += offset;
        t.localPosition = pJ;
    }

    void updateStepSelectVertical()
    {
        int[] curDimensions = sequencer.getCurrentDimensions();
        Vector3 sPos = sequencer.stepSelect.transform.localPosition;
        sPos.y = -sequencer.getCubeConst() * (curDimensions[0]);
        sequencer.stepSelect.transform.localPosition = sPos;
    }

    void ensureRowPrefabs(int row)
    {
        bool even = row % 2 == 0;
        int[] curDimensions = sequencer.getCurrentDimensions();
        int stepIndex = Mathf.Max(0, curDimensions[1] - 1);
        float cubeConst = sequencer.getCubeConst();

        trigSignalGenerator[] trigGenerators = sequencer.getRowTriggerGenerators();
        cvSignalGenerator[] cvGenerators = sequencer.getRowCvGenerators();
        omniJack[] trigJacks = sequencer.getRowTriggerJacks();
        omniJack[] cvJacks = sequencer.getRowCvJacks();

        if (sequencer.jackOutTrigTrans[row] != null && trigJacks[row] == null)
        {
            trigJacks[row] = sequencer.jackOutTrigTrans[row].GetComponentInChildren<omniJack>();
            trigGenerators[row] = sequencer.jackOutTrigTrans[row].GetComponentInChildren<trigSignalGenerator>();
        }

        if (sequencer.jackOutCVTrans[row] != null && cvJacks[row] == null)
        {
            cvJacks[row] = sequencer.jackOutCVTrans[row].GetComponentInChildren<omniJack>();
            cvGenerators[row] = sequencer.jackOutCVTrans[row].GetComponentInChildren<cvSignalGenerator>();
        }

        if (sequencer.jackOutTrigTrans[row] == null)
        {
            Transform jackTrig = Object.Instantiate(sequencer.triggerJackOutPrefab, Vector3.zero, Quaternion.identity).transform;
            jackTrig.parent = sequencer.transform;
            jackTrig.localRotation = Quaternion.Euler(0, 0, -90);
            jackTrig.localScale = Vector3.one;
            jackTrig.localPosition = new Vector3(-cubeConst * (stepIndex + 2.5f), -cubeConst * row, -cubeConst * 0.5f);

            sequencer.jackOutTrigTrans[row] = jackTrig;
            trigGenerators[row] = jackTrig.GetComponentInChildren<trigSignalGenerator>();
            trigJacks[row] = jackTrig.GetComponentInChildren<omniJack>();
        }

        if (sequencer.jackOutCVTrans[row] == null)
        {
            Transform jackCV = Object.Instantiate(sequencer.cvJackOutPrefab, Vector3.zero, Quaternion.identity).transform;
            jackCV.parent = sequencer.transform;
            jackCV.localRotation = Quaternion.Euler(0, 0, -90);
            jackCV.localScale = Vector3.one;
            jackCV.localPosition = new Vector3(-cubeConst * (stepIndex + 2.5f), -cubeConst * row, -cubeConst * 0.5f);

            sequencer.jackOutCVTrans[row] = jackCV;
            cvGenerators[row] = jackCV.GetComponentInChildren<cvSignalGenerator>();
            cvJacks[row] = jackCV.GetComponentInChildren<omniJack>();
        }

        if (sequencer.controlPanelTrans[row] == null)
        {
            Transform ctrl = Object.Instantiate(sequencer.controlPrefab, Vector3.zero, Quaternion.identity).transform;
            ctrl.parent = sequencer.transform;
            ctrl.localRotation = Quaternion.Euler(0, 90, -90);
            ctrl.localScale = Vector3.one;
            ctrl.localPosition = new Vector3(-cubeConst * (stepIndex + 1), -cubeConst * row, cubeConst * 0.5f);

            sequencer.controlPanelTrans[row] = ctrl;
            sequencer.getRowMutes()[row] = ctrl.GetComponentInChildren<button>();
            sequencer.getRowModeSwitches()[row] = ctrl.GetComponentInChildren<basicSwitch>();
            sequencer.getRowModeSwitches()[row].setSwitch(even, true);
        }

        registerRowModeListener(row);
    }

    void ensureStepPrefabs(int row, int step)
    {
        if (sequencer.getRowModeSwitches()[row] == null) ensureRowPrefabs(row);
        bool isTrigger = sequencer.getRowModeSwitches()[row].switchVal;
        setupStepPrefabRow(sequencer.stepButtonPrefab, row, step, isTrigger);
        setupStepPrefabRow(sequencer.stepDialPrefab, row, step, !isTrigger);
    }

    void registerRowModeListener(int row)
    {
        bool[] listenerFlags = sequencer.getRowModeListenerFlags();
        basicSwitch[] rowModes = sequencer.getRowModeSwitches();
        if (listenerFlags[row]) return;
        if (rowModes[row] == null) return;

        int localRow = row;
        rowModes[row].onSwitchChangedEvent.AddListener(delegate { sequencer.doModeSwitch(localRow); });
        listenerFlags[row] = true;
    }

    void setupStepPrefabRow(GameObject prefab, int y, int x, bool activeOnSpawn)
    {
        button[,] stepButtons = sequencer.getStepButtons();
        dial[,] stepDials = sequencer.getStepDials();

        if (prefab == sequencer.stepButtonPrefab && sequencer.stepButtonTrans[y, x] != null)
        {
            sequencer.stepButtonTrans[y, x].gameObject.SetActive(activeOnSpawn);
            return;
        }

        if (prefab == sequencer.stepDialPrefab && sequencer.stepDialTrans[y, x] != null)
        {
            sequencer.stepDialTrans[y, x].gameObject.SetActive(activeOnSpawn);
            return;
        }

        Transform t = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity).transform;
        t.parent = sequencer.transform;
        t.localRotation = Quaternion.identity;
        t.localPosition = new Vector3(-sequencer.getCubeConst() * x, -sequencer.getCubeConst() * y, 0);
        t.localScale = Vector3.one;

        if (prefab == sequencer.stepButtonPrefab)
        {
            sequencer.stepButtonTrans[y, x] = t;
            stepButtons[y, x] = t.GetComponent<button>();
            float Hval = (float)y / sequencer.getMaxSteps();
            stepButtons[y, x].Setup(x, y, sequencer.stepBools[sequencer.activePattern, y, x], Color.HSVToRGB(Hval, .9f, .05f));
        }

        if (prefab == sequencer.stepDialPrefab)
        {
            sequencer.stepDialTrans[y, x] = t;
            stepDials[y, x] = t.GetComponentInChildren<dial>();
            stepDials[y, x].setPercent(sequencer.stepFloats[sequencer.activePattern, y, x]);
        }

        t.gameObject.SetActive(activeOnSpawn);
    }
}
