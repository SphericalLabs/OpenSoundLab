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
using System.Collections;
using System.Xml.Serialization;
using static OVRPlugin;
using static UnityEngine.Rendering.DebugUI.Table;
using System;
using System.Xml.Serialization;

public class sequencerDeviceInterface : deviceInterface
{
    #region fields

    // prefabs
    public GameObject triggerJackOutPrefab, cvJackOutPrefab, controlPrefab;
    public GameObject stepDialPrefab, stepButtonPrefab;

    public int activePattern = 0;
    public int maxPattern = 16;

    // 3D array of pattern states: pattern, row / y, step / x
    public bool[,,] stepBools;  // the step sequencer button values
    public float[,,] stepFloats; // the step sequencer dial values

    // 2D arrays of prepopulated, networked interface elements
    public Transform[,] stepButtonTrans;
    public Transform[,] stepDialTrans;

    // prepopulated control panels
    public Transform[] controlPanelTrans;
    public Transform[] jackOutTrigTrans;
    public Transform[] jackOutCVTrans;

    private basicSwitch[] controlPanelModes;
    private button[] controlPanelMutes;
    private trigSignalGenerator[] jackOutTrigGenerators;
    private cvSignalGenerator[] jackOutCVGenerators;
    private omniJack[] jackOutTrigJacks;
    private omniJack[] jackOutCVJacks;
    private dial[,] stepDials;
    private button[,] stepButtons;

    // sequencer
    public bool running = true;

    // handles
    public xyHandle xyHandle;
    public Transform stretchNode;
    public xHandle stepSelect;

    // row, step
    public int[] dimensions = new int[] { 1, 1 };
    int[] curDimensions = new int[] { 0, 0 };

    float cubeConst = .04f;

    int maxSteps = 64;
    int maxRows = 16;

    bool[] rowModeListenerAdded;

    public basicSwitch modeSwitch; // Switch between Clock (trigger) and Phase (ramp) modes
    public omniJack resetJack, clockJack, phaseJack;
    public button playButton;
    public basicSwitch switchCVRange;
    bool lastRangeLow = true;

    signalGenerator clockGenerator;
    signalGenerator resetGenerator;
    signalGenerator phaseGenerator;

    double _sampleDuration = 0;
    float[] lastClockSig = new float[] { 0, 0 };
    float[] lastResetSig = new float[] { 0, 0 };

    public TextMesh[] dimensionDisplays;

    float[] _audioPhaseBuffer = new float[2048];
    float[] _audioClockBuffer = new float[2048];
    float[] _audioResetBuffer = new float[2048];

    public bool initialised = false;

    #endregion

    #region basics

    public override void Awake()
    {
        base.Awake();

        jackOutTrigTrans = new Transform[maxRows];
        jackOutCVTrans = new Transform[maxRows];
        controlPanelTrans = new Transform[maxRows];

        controlPanelMutes = new button[maxRows];
        controlPanelModes = new basicSwitch[maxRows];

        jackOutTrigGenerators = new trigSignalGenerator[maxRows];
        jackOutCVGenerators = new cvSignalGenerator[maxRows];
        jackOutTrigJacks = new omniJack[maxRows];
        jackOutCVJacks = new omniJack[maxRows];
        stepDials = new dial[maxRows, maxSteps];
        stepButtons = new button[maxRows, maxSteps];

        stepBools = new bool[maxPattern, maxRows, maxSteps];
        stepFloats = new float[maxPattern, maxRows, maxSteps];

        stepDialTrans = new Transform[maxRows, maxSteps];
        stepButtonTrans = new Transform[maxRows, maxSteps];

        for (int i = 0; i < maxRows; i++)
        {
            for (int j = 0; j < maxSteps; j++)
            {
                stepFloats[activePattern, i, j] = 0.5f;
            }
        }

        playButton = GetComponentInChildren<button>();
        switchCVRange = GetComponentInChildren<basicSwitch>();

        _sampleDuration = 1.0 / AudioSettings.outputSampleRate;

        for (int i = 0; i < dimensionDisplays.Length; i++)
        {
            dimensionDisplays[i].GetComponent<Renderer>().material.SetColor("_TintColor", Color.white);
        }

        dimensionDisplays[0].GetComponent<Renderer>().material.SetFloat("_EmissionGain", .3f);
        dimensionDisplays[1].GetComponent<Renderer>().material.SetFloat("_EmissionGain", .3f);

        rowModeListenerAdded = new bool[maxRows];

        spawnMaxDimensions();

        for (int i = 0; i < curDimensions[0]; i++)
        {
            if (jackOutCVGenerators[i] != null)
                jackOutCVGenerators[i].setRange(lastRangeLow ? cvSignalGenerator.lowRange : cvSignalGenerator.highRange);
        }



        dimensionDisplays[1].text = "";
        dimensionDisplays[1].gameObject.SetActive(false);
    }

    void Start()
    {
    }

    void Update()
    {
        SelectStepUpdate();

        dimensions[1] = Mathf.CeilToInt((stretchNode.localPosition.x + cubeConst * .75f) / -cubeConst);
        dimensions[0] = Mathf.CeilToInt((stretchNode.localPosition.y + cubeConst * .75f) / -cubeConst);

        if (dimensions[1] < 1) dimensions[1] = 1;
        if (dimensions[0] < 1) dimensions[0] = 1;
        if (dimensions[1] > maxSteps) dimensions[1] = maxSteps;
        if (dimensions[0] > maxRows) dimensions[0] = maxRows;
        UpdateDimensions();
        if (clockGenerator != clockJack.signal)
        {
            clockGenerator = clockJack.signal;
        }

        if (resetGenerator != resetJack.signal)
        {
            resetGenerator = resetJack.signal;
        }

        if (phaseGenerator != phaseJack.signal)
        {
            phaseGenerator = phaseJack.signal;
        }

        if (switchCVRange.switchVal != lastRangeLow)
        {
            lastRangeLow = switchCVRange.switchVal;
            for (int i = 0; i < curDimensions[0]; i++)
            {
                jackOutCVGenerators[i].setRange(lastRangeLow ? cvSignalGenerator.lowRange : cvSignalGenerator.highRange);
            }
        }

        readAllData();

    }

    void OnDestroy()
    {
    }

    #endregion

    #region running

    int selectedStep = 0; // the currently highlighted one
    int targetStep = 0; // the step that now needs to be updated for
    int curStep = 0; // the actual step

    public int CurStep { get => curStep; set => curStep = value; }
    public int TargetStep { get => targetStep; set => targetStep = value; }
    public bool silent = false;


    public void SelectStep(int s, bool silent = false)
    {
        selectedStep = targetStep = s;

        if (silent) return;

        // it is important that this is running here, since this is called from audio thread and directly manipulates the sequencer outputs
        // this is one aspect of making the sequencer sample accurate!
        for (int row = 0; row < curDimensions[0]; row++)
        {
            if (controlPanelMutes[row].isHit) continue;

            jackOutTrigGenerators[row].setSignal(stepBools[activePattern, row, targetStep]);
            jackOutCVGenerators[row].setSignal(stepFloats[activePattern, row, targetStep] * 2f - 1f);
        }
    }

    // called from Update loop
    void SelectStepUpdate()
    {
        if (targetStep == curStep) return;
        if (curStep < dimensions[1]) stepOff(curStep);
        curStep = targetStep;
        stepOn(curStep);
        stepSelect.updatePos(-cubeConst * curStep);
    }


    // called from audio thread
    public void executeNextStep()
    {
        if (stepSelect.curState == manipObject.manipState.grabbed) return;

        int s = 1;

        bool minicheck = runningUpdated;
        if (runningUpdated)
        {
            s = 0;
            runningUpdated = false;
        }

        int next = (targetStep + s) % dimensions[1];
        SelectStep(next);
    }

    void stepOff(int step)
    {
        for (int i = 0; i < curDimensions[0]; i++)
        {
            if (stepButtons[i, step] != null) stepButtons[i, step].Highlight(false);
        }
    }

    void stepOn(int step)
    {
        for (int i = 0; i < curDimensions[0]; i++)
        {
            if (stepButtons[i, step] != null) stepButtons[i, step].Highlight(true);
        }
    }

    public void readAllData()
    {
        for (int step = 0; step < dimensions[1]; step++)
        {
            for (int row = 0; row < dimensions[0]; row++)
            {
                if (stepDials[row, step] != null)
                {
                    stepFloats[activePattern, row, step] = stepDials[row, step].percent;
                }
                if (stepButtons[row, step] != null)
                {
                    stepBools[activePattern, row, step] = stepButtons[row, step].isHit;
                }
            }
        }
    }

    public void doModeSwitch(int row)
    {

        if (row > curDimensions[0] - 1) return; // this might happen during initialisation when mode switch is synced

        if (controlPanelModes[row].switchVal) // trigger mode
        {
            // enable trig
            if (jackOutTrigTrans[row] != null) jackOutTrigTrans[row].gameObject.SetActive(true);
            for (int step = 0; step < curDimensions[1]; step++)
            {
                if (stepButtonTrans[row, step] != null) stepButtonTrans[row, step].gameObject.SetActive(true);
            }

            // disable cv
            if (jackOutCVTrans[row] != null)
            {
                jackOutCVTrans[row].GetComponentInChildren<omniJack>().endConnection(true, true);
                jackOutCVTrans[row].gameObject.SetActive(false);
            }
            for (int step = 0; step < curDimensions[1]; step++)
            {
                if (stepDialTrans[row, step] != null) stepDialTrans[row, step].gameObject.SetActive(false);
            }
        }
        else // cv mode
        {
            // disable trig
            if (jackOutTrigTrans[row] != null)
            {
                jackOutTrigTrans[row].GetComponentInChildren<omniJack>().endConnection(true, true);
                jackOutTrigTrans[row].gameObject.SetActive(false);
            }
            for (int step = 0; step < curDimensions[1]; step++)
            {
                if (stepButtonTrans[row, step] != null) stepButtonTrans[row, step].gameObject.SetActive(false);
            }

            // enable cv
            if (jackOutCVTrans[row] != null) jackOutCVTrans[row].gameObject.SetActive(true);
            for (int step = 0; step < curDimensions[1]; step++)
            {
                if (stepDialTrans[row, step] != null) stepDialTrans[row, step].gameObject.SetActive(true);
            }
        }

    }

    public void forcePlay(bool on)
    {
        togglePlay(on);
        playButton.phantomHit(on);
    }

    void resetSteps()
    {
        SelectStep(0);
    }

    private void OnAudioFilterRead(float[] buffer, int channels)
    {
        if (_audioPhaseBuffer.Length != buffer.Length)
        {
            System.Array.Resize(ref _audioPhaseBuffer, buffer.Length);
        }

        if (_audioClockBuffer.Length != buffer.Length)
        {
            System.Array.Resize(ref _audioClockBuffer, buffer.Length);
        }

        if (_audioResetBuffer.Length != buffer.Length)
        {
            System.Array.Resize(ref _audioResetBuffer, buffer.Length);
        }

        if (modeSwitch != null && modeSwitch.switchVal) // Phase mode
        {
            if (phaseGenerator == null) return;
            if (!running) return;

            phaseGenerator.processBuffer(_audioPhaseBuffer, AudioSettings.dspTime, channels);

            // Map phase directly to step
            float latestPhase = _audioPhaseBuffer[buffer.Length - channels];
            int s = Mathf.FloorToInt(latestPhase * dimensions[1]);
            s = Mathf.Clamp(s, 0, dimensions[1] - 1);
            if (phaseSyncPending || s != targetStep)
            {
                 // We don't call SelectStep here because it's for the audio thread state usually
                 // Actually SelectStep handles the signal generator updates which IS what we want
                 SelectStep(s);
                 runningUpdated = true;
                 phaseSyncPending = false;
            }
        }
        else // Clock (Trigger) mode
        {
            if (resetGenerator != null)
            {
                resetGenerator.processBuffer(_audioResetBuffer, AudioSettings.dspTime, channels);

                for (int i = 0; i < buffer.Length; i += channels)
                {
                    if (signalGenerator.isRisingEdge(_audioResetBuffer[i], lastResetSig[1]))
                    {
                        resetSteps();
                    }
                    lastResetSig[0] = lastResetSig[1];
                    lastResetSig[1] = _audioResetBuffer[i];
                }
            }

            if (!running) return;

            if (clockGenerator != null)
            {
                clockGenerator.processBuffer(_audioClockBuffer, AudioSettings.dspTime, channels);

                for (int i = 0; i < buffer.Length; i += channels)
                {
                    if (signalGenerator.isRisingEdge(_audioClockBuffer[i], lastClockSig[1]))
                    {
                        executeNextStep();
                    }
                    lastClockSig[0] = lastClockSig[1];
                    lastClockSig[1] = _audioClockBuffer[i];
                }
            }
        }
    }

    // called from Update loop
    public void UpdateStepSelect(bool forced = false)
    {
        // bugfix for randomly skipped / missed steps in sequencer.
        // this routine would fire even if the step selector handle was not touched or grabbed.
        // this could be due to an multithread issue between main and audio thread, which is still unsolved.
        if (!forced && stepSelect.curState != manipObject.manipState.grabbed) return;

        int s = (int)Mathf.Round(stepSelect.transform.localPosition.x / -cubeConst);
        if (s == selectedStep) return;
        stepSelect.pulse();
        selectedStep = s;
        SelectStep(s);
    }

    bool runningUpdated = false;
    bool phaseSyncPending = false;
    public void togglePlay(bool on)
    {
        running = on;
        if (on)
        {
            runningUpdated = true;
            phaseSyncPending = true;
        }
        else
        {
            phaseSyncPending = false;
        }
    }

    public override void hit(bool on, int ID = -1)
    {
        togglePlay(on);
    }

    public override void hit(bool on, int IDx, int IDy)
    {
        stepBools[activePattern, IDy, IDx] = on;
    }

    public override void onSelect(bool on, int IDx, int IDy)
    {
        if (!on) dimensionDisplays[1].gameObject.SetActive(false);
        else
        {
            dimensionDisplays[1].text = ((IDx + 1) + "X" + (IDy + 1)).ToString();
            dimensionDisplays[1].gameObject.SetActive(true);
            Vector3 pos = stepButtonTrans[IDy, IDx].localPosition;
            pos.z = .021f;
            dimensionDisplays[1].transform.localPosition = pos;
        }
    }

    Coroutine _rowDisplayFadeRoutine;
    public override void onSelect(bool on, int ID = -1)
    {
        if (_rowDisplayFadeRoutine != null) StopCoroutine(_rowDisplayFadeRoutine);

        if (on)
        {
            dimensionDisplays[0].GetComponent<Renderer>().material.SetColor("_TintColor", Color.white);
            dimensionDisplays[0].gameObject.SetActive(true);
        }
        else
        {
            _rowDisplayFadeRoutine = StartCoroutine(rowDisplayFadeRoutine());
        }
    }

    IEnumerator rowDisplayFadeRoutine()
    {
        float t = 0;
        while (t < 1)
        {
            t = Mathf.Clamp01(t + Time.deltaTime);
            dimensionDisplays[0].GetComponent<Renderer>().material.SetColor("_TintColor", Color.Lerp(Color.white, Color.black, t));
            yield return null;
        }
    }

    #endregion

    #region size-management

    void spawnMaxDimensions()
    {
        // please note: don't set the default dimensions here, but position the stretchHandle in the prefab instead
        // still, the following two lines are necessary for shrinking down from maxDimensions
        // depending on your strechHandle settings you might have to update these dimensions here, too
        // todo: simplify default settings
        dimensions[0] = 4;
        dimensions[1] = 8;

        SetDimensions(dimensions[0], dimensions[1]);
    }

    void setupStepPrefabRow(GameObject prefab, int y, int x, bool activeOnSpawn)
    {
        if (prefab == stepButtonPrefab && stepButtonTrans[y, x] != null)
        {
            stepButtonTrans[y, x].gameObject.SetActive(activeOnSpawn);
            return;
        }

        if (prefab == stepDialPrefab && stepDialTrans[y, x] != null)
        {
            stepDialTrans[y, x].gameObject.SetActive(activeOnSpawn);
            return;
        }

        Transform t = Instantiate(prefab, Vector3.zero, Quaternion.identity).transform;
        t.parent = transform;
        t.localRotation = Quaternion.identity;
        t.localPosition = new Vector3(-cubeConst * x, -cubeConst * y, 0);
        t.localScale = Vector3.one;

        if (prefab == stepButtonPrefab)
        {
            stepButtonTrans[y, x] = t;
            stepButtons[y, x] = t.GetComponent<button>();
            float Hval = (float)y / maxSteps;
            stepButtons[y, x].Setup(x, y, stepBools[activePattern, y, x], Color.HSVToRGB(Hval, .9f, .05f));
        }

        if (prefab == stepDialPrefab)
        {
            stepDialTrans[y, x] = t;
            stepDials[y, x] = t.GetComponentInChildren<dial>();
            stepDials[y, x].setPercent(stepFloats[activePattern, y, x]);
        }

        t.gameObject.SetActive(activeOnSpawn);
    }

    public void SetDimensions(int rows, int steps)
    {
        dimensions[0] = rows;
        dimensions[1] = steps;
        Vector3 p = stretchNode.localPosition;
        p.y = rows * -cubeConst - cubeConst * .75f;
        p.x = steps * -cubeConst - cubeConst * .75f;

        stretchNode.localPosition = p;

        UpdateDimensions();
    }

    void UpdateDimensions()
    {
        if (dimensions[0] == curDimensions[0] && dimensions[1] == curDimensions[1]) return;

        stretchNode.GetComponent<xyHandle>().pulse();
        if (dimensions[1] > curDimensions[1])
        {
            addColumns(dimensions[1] - curDimensions[1]);
        }
        else if (dimensions[1] < curDimensions[1])
        {
            removeColumns(curDimensions[1] - dimensions[1]);
        }
        if (dimensions[0] > curDimensions[0])
        {
            addRows(dimensions[0] - curDimensions[0]);
        }
        else if (dimensions[0] < curDimensions[0])
        {
            removeRows(curDimensions[0] - dimensions[0]);
        }

        dimensionDisplays[0].text = curDimensions[1] + " X " + curDimensions[0];
    }

    void addColumns(int c)
    {
        for (int i = 0; i < c; i++)
        {
            for (int row = 0; row < curDimensions[0]; row++)
            {
                ensureStepPrefabs(row, curDimensions[1]);
                if (controlPanelModes[row].switchVal)
                {
                    stepButtonTrans[row, curDimensions[1]].gameObject.SetActive(true);
                }
                else
                {
                    stepDialTrans[row, curDimensions[1]].gameObject.SetActive(true);
                }


            }
            curDimensions[1]++;
        }

        for (int row = 0; row < maxRows; row++)
        {
            if (jackOutTrigTrans[row] != null) moveByOffset(jackOutTrigTrans[row], -cubeConst * c);
            if (jackOutCVTrans[row] != null) moveByOffset(jackOutCVTrans[row], -cubeConst * c);
            if (controlPanelTrans[row] != null) moveByOffset(controlPanelTrans[row], -cubeConst * c);
        }

        stepSelect.xBounds.x = -cubeConst * (curDimensions[1] - 1);
        stepSelect.updatePos(stepSelect.transform.localPosition.x);
    }

    void removeColumns(int c)
    {
        for (int i = 0; i < c; i++)
        {
            for (int row = 0; row < curDimensions[0]; row++)
            {
                if (stepButtons[row, curDimensions[1] - 1] != null)
                {
                    stepButtons[row, curDimensions[1] - 1].Highlight(false);
                }
                if (stepButtonTrans[row, curDimensions[1] - 1] != null)
                {
                    stepButtonTrans[row, curDimensions[1] - 1].gameObject.SetActive(false);
                }

                if (stepDialTrans[row, curDimensions[1] - 1] != null)
                {
                    stepDialTrans[row, curDimensions[1] - 1].gameObject.SetActive(false);
                }
            }
            curDimensions[1]--;
        }

        for (int row = 0; row < maxRows; row++)
        {
            if (jackOutTrigTrans[row] != null) moveByOffset(jackOutTrigTrans[row], cubeConst * c);
            if (jackOutCVTrans[row] != null) moveByOffset(jackOutCVTrans[row], cubeConst * c);
            if (controlPanelTrans[row] != null) moveByOffset(controlPanelTrans[row], cubeConst * c);
        }

        stepSelect.xBounds.x = -cubeConst * (curDimensions[1] - 1);
        stepSelect.updatePos(stepSelect.transform.localPosition.x);
    }

    void moveByOffset(Transform t, float offset)
    {
        Vector3 pJ = t.localPosition;
        pJ.x += offset;
        t.localPosition = pJ;
    }

    void addRows(int c)
    {
        for (int i = 0; i < c; i++)
        {
            int rowIndex = curDimensions[0];
            ensureRowPrefabs(rowIndex);
            for (int step = 0; step < curDimensions[1]; step++)
            {
                ensureStepPrefabs(rowIndex, step);
                if (controlPanelModes[rowIndex].switchVal)
                {
                    stepButtonTrans[rowIndex, step].gameObject.SetActive(true);
                    if (jackOutTrigTrans[rowIndex] != null) jackOutTrigTrans[rowIndex].gameObject.SetActive(true);
                }
                else
                {
                    stepDialTrans[rowIndex, step].gameObject.SetActive(true);
                    if (jackOutCVTrans[rowIndex] != null) jackOutCVTrans[rowIndex].gameObject.SetActive(true);
                }

                if (controlPanelTrans[rowIndex] != null) controlPanelTrans[rowIndex].gameObject.SetActive(true);
            }
            curDimensions[0]++;
        }

        updateStepSelectVertical();
    }

    void removeRows(int rows)
    {
        for (int row = 0; row < rows; row++)
        {
            // controlPanelModes[curDimensions[0] - 1].setSwitch((curDimensions[0] - 1) % 2 == 0 ? true : false); // reset mode

            for (int step = 0; step < curDimensions[1]; step++)
            {
                int rowIndex = curDimensions[0] - 1;
                if (rowIndex < 0) continue;

                if (stepButtons[rowIndex, step] != null)
                {
                    stepButtons[rowIndex, step].Highlight(false);
                    stepButtons[rowIndex, step].keyHit(false);
                }
                if (stepButtonTrans[rowIndex, step] != null)
                {
                    stepButtonTrans[rowIndex, step].gameObject.SetActive(false);
                }
                if (stepDials[rowIndex, step] != null)
                {
                    stepDials[rowIndex, step].setPercent(0.5f);
                }
                if (stepDialTrans[rowIndex, step] != null)
                {
                    stepDialTrans[rowIndex, step].gameObject.SetActive(false);
                }
            }

            int lastRow = curDimensions[0] - 1;
            if (lastRow >= 0)
            {
                if (jackOutTrigTrans[lastRow] != null)
                {
                    jackOutTrigTrans[lastRow].GetComponentInChildren<omniJack>().endConnection(true, true);
                    jackOutTrigTrans[lastRow].gameObject.SetActive(false);
                }
                if (jackOutCVTrans[lastRow] != null)
                {
                    jackOutCVTrans[lastRow].GetComponentInChildren<omniJack>().endConnection(true, true);
                    jackOutCVTrans[lastRow].gameObject.SetActive(false);
                }
                if (controlPanelTrans[lastRow] != null) controlPanelTrans[lastRow].gameObject.SetActive(false);
            }
            curDimensions[0]--;
        }

        updateStepSelectVertical();
    }

    void updateStepSelectVertical()
    {
        Vector3 sPos = stepSelect.transform.localPosition;
        sPos.y = -cubeConst * (curDimensions[0]);
        stepSelect.transform.localPosition = sPos;
    }

    // Ensure row-level prefabs exist when rows are created dynamically.
    void ensureRowPrefabs(int row)
    {
        bool even = row % 2 == 0;
        int stepIndex = Mathf.Max(0, curDimensions[1] - 1);

        if (jackOutTrigTrans[row] != null && jackOutTrigJacks[row] == null)
        {
            jackOutTrigJacks[row] = jackOutTrigTrans[row].GetComponentInChildren<omniJack>();
            jackOutTrigGenerators[row] = jackOutTrigTrans[row].GetComponentInChildren<trigSignalGenerator>();
        }

        if (jackOutCVTrans[row] != null && jackOutCVJacks[row] == null)
        {
            jackOutCVJacks[row] = jackOutCVTrans[row].GetComponentInChildren<omniJack>();
            jackOutCVGenerators[row] = jackOutCVTrans[row].GetComponentInChildren<cvSignalGenerator>();
        }

        if (jackOutTrigTrans[row] == null)
        {
            Transform jackTrig = Instantiate(triggerJackOutPrefab, Vector3.zero, Quaternion.identity).transform;
            jackTrig.parent = transform;
            jackTrig.localRotation = Quaternion.Euler(0, 0, -90);
            jackTrig.localScale = Vector3.one;
            jackTrig.localPosition = new Vector3(-cubeConst * (stepIndex + 2.5f), -cubeConst * row, -cubeConst * 0.5f);

            jackOutTrigTrans[row] = jackTrig;
            jackOutTrigGenerators[row] = jackTrig.GetComponentInChildren<trigSignalGenerator>();
            jackOutTrigJacks[row] = jackTrig.GetComponentInChildren<omniJack>();
        }

        if (jackOutCVTrans[row] == null)
        {
            Transform jackCV = Instantiate(cvJackOutPrefab, Vector3.zero, Quaternion.identity).transform;
            jackCV.parent = transform;
            jackCV.localRotation = Quaternion.Euler(0, 0, -90);
            jackCV.localScale = Vector3.one;
            jackCV.localPosition = new Vector3(-cubeConst * (stepIndex + 2.5f), -cubeConst * row, -cubeConst * 0.5f);

            jackOutCVTrans[row] = jackCV;
            jackOutCVGenerators[row] = jackCV.GetComponentInChildren<cvSignalGenerator>();
            jackOutCVJacks[row] = jackCV.GetComponentInChildren<omniJack>();
        }

        if (controlPanelTrans[row] == null)
        {
            Transform ctrl = Instantiate(controlPrefab, Vector3.zero, Quaternion.identity).transform;
            ctrl.parent = transform;
            ctrl.localRotation = Quaternion.Euler(0, 90, -90);
            ctrl.localScale = Vector3.one;
            ctrl.localPosition = new Vector3(-cubeConst * (stepIndex + 1), -cubeConst * row, cubeConst * 0.5f);

            controlPanelTrans[row] = ctrl;
            controlPanelMutes[row] = ctrl.GetComponentInChildren<button>();
            controlPanelModes[row] = ctrl.GetComponentInChildren<basicSwitch>();
            controlPanelModes[row].setSwitch(even, true);
        }

        registerRowModeListener(row);
    }

    // Ensure step prefabs exist for the given row/step.
    void ensureStepPrefabs(int row, int step)
    {
        if (controlPanelModes[row] == null) ensureRowPrefabs(row);
        bool isTrigger = controlPanelModes[row].switchVal;
        setupStepPrefabRow(stepButtonPrefab, row, step, isTrigger);
        setupStepPrefabRow(stepDialPrefab, row, step, !isTrigger);
    }

    // Hook row mode switch to update row visibility.
    void registerRowModeListener(int row)
    {
        if (rowModeListenerAdded[row]) return;
        if (controlPanelModes[row] == null) return;

        int localRow = row;
        controlPanelModes[row].onSwitchChangedEvent.AddListener(delegate { doModeSwitch(localRow); });
        rowModeListenerAdded[row] = true;
    }

    // Expose grid bounds to networking helpers.
    public int getMaxRows()
    {
        return maxRows;
    }

    // Expose grid bounds to networking helpers.
    public int getMaxSteps()
    {
        return maxSteps;
    }

    // Expose step buttons for dynamic listener registration.
    public button[,] getStepButtons()
    {
        return stepButtons;
    }

    // Expose step dials for dynamic listener registration.
    public dial[,] getStepDials()
    {
        return stepDials;
    }

    // Expose row mute buttons for dynamic network registration.
    public button[] getRowMutes()
    {
        return controlPanelMutes;
    }

    // Expose row mode switches for dynamic network registration.
    public basicSwitch[] getRowModeSwitches()
    {
        return controlPanelModes;
    }

    // Expose row trigger jacks for dynamic network registration.
    public omniJack[] getRowTriggerJacks()
    {
        return jackOutTrigJacks;
    }

    // Expose row CV jacks for dynamic network registration.
    public omniJack[] getRowCvJacks()
    {
        return jackOutCVJacks;
    }

    // Apply networked step toggles into local state and UI.
    public void applyNetworkStepBool(int pattern, int row, int step, bool value)
    {
        if (pattern < 0 || pattern >= maxPattern) return;
        if (row < 0 || row >= maxRows) return;
        if (step < 0 || step >= maxSteps) return;

        stepBools[pattern, row, step] = value;

        if (pattern != activePattern) return;
        if (stepButtons == null) return;
        button b = stepButtons[row, step];
        if (b == null) return;
        b.keyHit(value, false);
    }

    // Apply networked step dial values into local state and UI.
    public void applyNetworkStepFloat(int pattern, int row, int step, float value)
    {
        if (pattern < 0 || pattern >= maxPattern) return;
        if (row < 0 || row >= maxRows) return;
        if (step < 0 || step >= maxSteps) return;

        stepFloats[pattern, row, step] = value;

        if (pattern != activePattern) return;
        if (stepDials == null) return;
        dial d = stepDials[row, step];
        if (d == null) return;
        d.setPercent(value);
    }

    #endregion

    #region saveload

    public override InstrumentData GetData()
    {
        SequencerData data = new SequencerData
        {
            deviceType = DeviceType.Sequencer
        };
        GetTransformData(data);

        data.switchPlay = playButton.isHit;
        data.modeSwitch = modeSwitch != null && modeSwitch.switchVal;

        data.resetJackID = resetJack.transform.GetInstanceID();
        data.clockJackID = clockJack.transform.GetInstanceID();
        data.phaseJackID = phaseJack.transform.GetInstanceID();

        data.activePattern = activePattern;
        int rows = Mathf.Clamp(dimensions[0], 1, maxRows);
        int steps = Mathf.Clamp(dimensions[1], 1, maxSteps);
        data.dimensions = new int[] { rows, steps };

        // Store only visible dimensions to keep saves compact.
        data.stepBools = new bool[maxPattern][][];
        data.stepFloats = new float[maxPattern][][];

        for (int p = 0; p < maxPattern; p++)
        {
            data.stepBools[p] = new bool[rows][];
            data.stepFloats[p] = new float[rows][];
            for (int r = 0; r < rows; r++)
            {
                bool isTriggerRow = controlPanelModes[r].switchVal;
                if (isTriggerRow)
                {
                    data.stepBools[p][r] = new bool[steps];
                    for (int s = 0; s < steps; s++)
                    {
                        data.stepBools[p][r][s] = stepBools[p, r, s];
                    }
                }
                else
                {
                    data.stepFloats[p][r] = new float[steps];
                    for (int s = 0; s < steps; s++)
                    {
                        data.stepFloats[p][r][s] = stepFloats[p, r, s];
                    }
                }
            }
        }

        data.jackTriggerOutID = new int[rows];
        for (int row = 0; row < rows; row++)
        {
            if (jackOutTrigTrans[row] != null)
                data.jackTriggerOutID[row] = jackOutTrigTrans[row].GetChild(0).GetInstanceID();
        }

        data.jackCvOutID = new int[rows];
        for (int row = 0; row < rows; row++)
        {
            if (jackOutCVTrans[row] != null)
                data.jackCvOutID[row] = jackOutCVTrans[row].GetChild(0).GetInstanceID();
        }

        data.rowMutes = new bool[rows];
        for (int row = 0; row < rows; row++)
        {
            if (controlPanelMutes[row] != null)
                data.rowMutes[row] = controlPanelMutes[row].isHit;
        }

        data.rowModes = new bool[rows];
        for (int row = 0; row < rows; row++)
        {
            if (controlPanelModes[row] != null)
                data.rowModes[row] = controlPanelModes[row].switchVal;
        }

        data.switchRange = switchCVRange.switchVal;

        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        SequencerData data = d as SequencerData;
        base.Load(data, copyMode);

        int rows = maxRows;
        int steps = maxSteps;
        if (data.dimensions != null && data.dimensions.Length >= 2)
        {
            rows = Mathf.Clamp(data.dimensions[0], 1, maxRows);
            steps = Mathf.Clamp(data.dimensions[1], 1, maxSteps);
        }

        SetDimensions(rows, steps);

        togglePlay(data.switchPlay);
        playButton.phantomHit(data.switchPlay);

        if (modeSwitch != null) modeSwitch.setSwitch(data.modeSwitch, true);

        if (data.rowMutes != null)
        {
            int rowCount = Mathf.Min(curDimensions[0], data.rowMutes.Length);
            for (int row = 0; row < rowCount; row++)
            {
                if (controlPanelMutes[row] != null)
                    controlPanelMutes[row].phantomHit(data.rowMutes[row]);
            }
        }

        if (data.rowModes != null)
        {
            int rowCount = Mathf.Min(curDimensions[0], data.rowModes.Length);
            for (int row = 0; row < rowCount; row++)
            {
                if (controlPanelModes[row] == null) continue;
                controlPanelModes[row].setSwitch(data.rowModes[row], true);
                doModeSwitch(row);
            }
        }

        resetJack.SetID(data.resetJackID, copyMode);
        clockJack.SetID(data.clockJackID, copyMode);
        phaseJack.SetID(data.phaseJackID, copyMode);

        applyStepData(data);

        int activePatternIndex = Mathf.Clamp(data.activePattern, 0, maxPattern - 1);
        for (int step = 0; step < steps; step++)
        {
            for (int row = 0; row < rows; row++)
            {
                if (stepBools[activePatternIndex, row, step])
                {
                    stepButtons[row, step].keyHit(true);
                }
                stepDials[row, step].setPercent(stepFloats[activePatternIndex, row, step], true);
            }
        }

        if (data.jackTriggerOutID != null)
        {
            int rowCount = Mathf.Min(curDimensions[0], data.jackTriggerOutID.Length);
            for (int row = 0; row < rowCount; row++)
            {
                if (jackOutTrigTrans[row] != null)
                    jackOutTrigTrans[row].GetComponentInChildren<omniJack>().SetID(data.jackTriggerOutID[row], copyMode);
            }
        }

        if (data.jackCvOutID != null)
        {
            int rowCount = Mathf.Min(curDimensions[0], data.jackCvOutID.Length);
            for (int row = 0; row < rowCount; row++)
            {
                if (jackOutCVTrans[row] != null)
                    jackOutCVTrans[row].GetComponentInChildren<omniJack>().SetID(data.jackCvOutID[row], copyMode);
            }
        }

        switchCVRange.setSwitch(data.switchRange, true);

        // Ensure final size
        SetDimensions(rows, steps);
    }

    #endregion

    void applyStepData(SequencerData data)
    {
        if (data.stepBools == null && data.stepFloats == null) return;

        int patternCount = maxPattern;
        if (data.stepBools != null) patternCount = Mathf.Min(patternCount, data.stepBools.Length);
        if (data.stepFloats != null) patternCount = Mathf.Min(patternCount, data.stepFloats.Length);

        for (int p = 0; p < patternCount; p++)
        {
            bool[][] boolRows = data.stepBools != null && p < data.stepBools.Length ? data.stepBools[p] : null;
            float[][] floatRows = data.stepFloats != null && p < data.stepFloats.Length ? data.stepFloats[p] : null;

            int rowCount = maxRows;
            if (boolRows != null) rowCount = Mathf.Min(rowCount, boolRows.Length);
            if (floatRows != null) rowCount = Mathf.Min(rowCount, floatRows.Length);

            for (int r = 0; r < rowCount; r++)
            {
                if (controlPanelModes[r] == null) continue;
                bool isTriggerRow = controlPanelModes[r].switchVal;
                if (isTriggerRow)
                {
                    if (boolRows == null || r >= boolRows.Length || boolRows[r] == null) continue;
                    int stepCount = Mathf.Min(maxSteps, boolRows[r].Length);
                    for (int s = 0; s < stepCount; s++)
                    {
                        stepBools[p, r, s] = boolRows[r][s];
                    }
                }
                else
                {
                    if (floatRows == null || r >= floatRows.Length || floatRows[r] == null) continue;
                    int stepCount = Mathf.Min(maxSteps, floatRows[r].Length);
                    for (int s = 0; s < stepCount; s++)
                    {
                        stepFloats[p, r, s] = floatRows[r][s];
                    }
                }
            }
        }
    }
}

public class SequencerData : InstrumentData
{
    public bool switchPlay;
    public bool modeSwitch;
    public int resetJackID, clockJackID, phaseJackID;
    public int activePattern;
    public int[] dimensions;
    public bool[][][] stepBools;
    public float[][][] stepFloats;
    public int[] jackTriggerOutID;
    public int[] jackCvOutID;
    public bool[] rowMutes;
    public bool[] rowModes;
    public bool switchRange;
}

[XmlType("SequencerCVData")]
public class SequencerCVData : SequencerData
{
} // legacy alias, remove when old saves are dropped.
