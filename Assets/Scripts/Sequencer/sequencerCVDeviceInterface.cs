// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright � 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
//
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
//
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
//
//
// Copyright � 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright � 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright � 2017 Apache 2.0 Google LLC SoundStage VR
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
using static OVRPlugin;
using static UnityEngine.Rendering.DebugUI.Table;
using System;

public class sequencerCVDeviceInterface : deviceInterface
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
    private dial[,] stepDials;
    private button[,] stepButtons;

    // sequencer
    public bool running = true;
    float swingPercent = 0;
    int beatSpeed = 0;

    // handles
    public xyHandle xyHandle;
    public Transform stretchNode;
    public xHandle stepSelect;

    // row, step
    public int[] dimensions = new int[] { 1, 1 };
    int[] curDimensions = new int[] { 0, 0 };

    float cubeConst = .04f;

    int maxSteps = 16;
    int maxRows = 8;

    public sliderNotched beatSlider;
    public omniJack playTriggerInputJack;
    public button playButton;
    dial swingDial;
    signalGenerator clockGenerator;
    signalGenerator resetGenerator;
    beatTracker _beatManager;
    public basicSwitch switchCVRange;
    bool lastRangeLow = true;

    double _phase = 0;
    double _sampleDuration = 0;
    float[] lastPlaySig = new float[] { 0, 0 };

    public TextMesh[] dimensionDisplays;

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

        beatSlider = GetComponentInChildren<sliderNotched>();
        swingDial = GetComponentInChildren<dial>();
        switchCVRange = GetComponentInChildren<basicSwitch>();

        _sampleDuration = 1.0 / AudioSettings.outputSampleRate;
        _beatManager = ScriptableObject.CreateInstance<beatTracker>();

        for (int i = 0; i < dimensionDisplays.Length; i++)
        {
            dimensionDisplays[i].GetComponent<Renderer>().material.SetColor("_TintColor", Color.white);
        }

        dimensionDisplays[0].GetComponent<Renderer>().material.SetFloat("_EmissionGain", .3f);
        dimensionDisplays[1].GetComponent<Renderer>().material.SetFloat("_EmissionGain", .3f);

        spawnMaxDimensions();

        // register switch events, only now possible after everything has spawned
        // but they will set they current value and thus make invisible rows visible again
        for (int row = 0; row < maxRows; row++)
        {
            int localRow = row;  // Create a local copy, since closures are passed by reference
            controlPanelModes[row].onSwitchChangedEvent.AddListener(delegate { doModeSwitch(localRow); });
        }

        for (int i = 0; i < curDimensions[0]; i++)
        {
            jackOutCVGenerators[i].setRange(lastRangeLow ? cvSignalGenerator.lowRange : cvSignalGenerator.highRange);
        }



        dimensionDisplays[1].text = "";
        dimensionDisplays[1].gameObject.SetActive(false);
    }

    void Start()
    {
        _beatManager.setTriggers(executeNextStep, resetSteps);
        _beatManager.updateBeatNoTriplets(beatSpeed);
        _beatManager.updateSwing(swingPercent);

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
        UpdateStepSelect();

        if (beatSpeed != beatSlider.switchVal)
        {
            beatSpeed = beatSlider.switchVal;
            _beatManager.updateBeatNoTriplets(beatSpeed);
        }
        if (swingPercent != swingDial.percent)
        {
            swingPercent = swingDial.percent;
            _beatManager.updateSwing(swingPercent);
        }

        if (clockGenerator != playTriggerInputJack.signal)
        {
            clockGenerator = playTriggerInputJack.signal;
            _beatManager.toggleMC(clockGenerator == null);
            if (clockGenerator != null) forcePlay(false);
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
        Destroy(_beatManager);
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

        if (next == 0 && clockGenerator != null && !minicheck) forcePlay(false);
        else SelectStep(next);
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
                stepFloats[activePattern, row, step] = stepDials[row, step].percent;
                stepBools[activePattern, row, step] = stepButtons[row, step].isHit;
            }
        }
    }

    public void doModeSwitch(int row)
    {

        if (row > curDimensions[0] - 1) return; // this might happen during initialisation when mode switch is synced

        if (controlPanelModes[row].switchVal) // trigger mode
        {
            // enable trig
            jackOutTrigTrans[row].gameObject.SetActive(true);
            for (int step = 0; step < curDimensions[1]; step++)
            {
                stepButtonTrans[row, step].gameObject.SetActive(true);
            }

            // disable cv
            jackOutCVTrans[row].GetComponentInChildren<omniJack>().endConnection(true, true);
            jackOutCVTrans[row].gameObject.SetActive(false);
            for (int step = 0; step < curDimensions[1]; step++)
            {
                stepDialTrans[row, step].gameObject.SetActive(false);
            }
        }
        else // cv mode
        {
            // disable trig
            jackOutTrigTrans[row].GetComponentInChildren<omniJack>().endConnection(true, true);
            jackOutTrigTrans[row].gameObject.SetActive(false);
            for (int step = 0; step < curDimensions[1]; step++)
            {
                stepButtonTrans[row, step].gameObject.SetActive(false);
            }

            // enable cv
            jackOutCVTrans[row].gameObject.SetActive(true);
            for (int step = 0; step < curDimensions[1]; step++)
            {
                stepDialTrans[row, step].gameObject.SetActive(true);
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
        SelectStep(0, true);
        runningUpdated = true;
    }

    private void OnAudioFilterRead(float[] buffer, int channels)
    {
        if (clockGenerator == null) return;

        double dspTime = AudioSettings.dspTime;

        float[] playBuffer = new float[buffer.Length];
        clockGenerator.processBuffer(playBuffer, dspTime, channels);

        for (int i = 0; i < playBuffer.Length; i += channels)
        {
            if (playBuffer[i] > lastPlaySig[1] && lastPlaySig[1] <= lastPlaySig[0])
            {
                _beatManager.beatResetEvent();
                _phase = 0;
                forcePlay(true);
            }
            lastPlaySig[0] = lastPlaySig[1];
            lastPlaySig[1] = playBuffer[i];
        }

        for (int i = 0; i < buffer.Length; i += channels)
        {
            _phase += _sampleDuration;

            if (_phase > masterControl.instance.measurePeriod) _phase -= masterControl.instance.measurePeriod;
            _beatManager.beatUpdateEvent((float)(_phase / masterControl.instance.measurePeriod));
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
    public void togglePlay(bool on)
    {
        _beatManager.toggle(on);
        if (on) runningUpdated = true;
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
        bool even;

        for (int row = 0; row < maxRows; row++)
        {
            even = row % 2 == 0;

            for (int step = 0; step < maxSteps; step++)
            {
                // alternate between trigger and cv rows
                setupStepPrefabRow(stepButtonPrefab, row, step, even);
                setupStepPrefabRow(stepDialPrefab, row, step, !even);
            }

            // jacks for triggers
            Transform jackTrig = Instantiate(triggerJackOutPrefab, Vector3.zero, Quaternion.identity).transform;
            jackTrig.parent = transform;
            jackTrig.localRotation = Quaternion.Euler(0, 0, -90);
            jackTrig.localScale = Vector3.one;
            jackTrig.localPosition = new Vector3(-cubeConst * (maxSteps - 1 + 2.5f), -cubeConst * row, -cubeConst * 0.5f);

            jackOutTrigTrans[row] = jackTrig;
            jackOutTrigGenerators[row] = jackTrig.GetComponentInChildren<trigSignalGenerator>();
            jackTrig.gameObject.SetActive(even);

            // jacks for cvs
            Transform jackCV = Instantiate(cvJackOutPrefab, Vector3.zero, Quaternion.identity).transform;
            jackCV.parent = transform;
            jackCV.localRotation = Quaternion.Euler(0, 0, -90);
            jackCV.localScale = Vector3.one;
            jackCV.localPosition = new Vector3(-cubeConst * (maxSteps - 1 + 2.5f), -cubeConst * row, -cubeConst * 0.5f);

            jackOutCVTrans[row] = jackCV;
            jackOutCVGenerators[row] = jackCV.GetComponentInChildren<cvSignalGenerator>();
            jackCV.gameObject.SetActive(!even);

            // controlPrefab
            Transform ctrl = Instantiate(controlPrefab, Vector3.zero, Quaternion.identity).transform;
            ctrl.parent = transform;
            ctrl.localRotation = Quaternion.Euler(0, 90, -90);
            ctrl.localScale = Vector3.one;
            ctrl.localPosition = new Vector3(-cubeConst * (maxSteps - 1 + 1), -cubeConst * row, cubeConst * 0.5f);

            controlPanelTrans[row] = ctrl;
            controlPanelMutes[row] = ctrl.GetComponentInChildren<button>();
            controlPanelModes[row] = ctrl.GetComponentInChildren<basicSwitch>();
            controlPanelModes[row].setSwitch(even);
        }

        // shrink to start size
        curDimensions[0] = maxRows;
        curDimensions[1] = maxSteps;

        // please note: don't set the default dimensions here, but position the stretchHandle in the prefab instead
        // still, the following two lines are necessary for shrinking down from maxDimensions
        // depending on your strechHandle settings you might have to update these dimensions here, too
        // todo: simplify default settings
        dimensions[0] = 4;
        dimensions[1] = 8;

        UpdateDimensions();
    }

    void setupStepPrefabRow(GameObject prefab, int y, int x, bool activeOnSpawn)
    {
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
            moveByOffset(jackOutTrigTrans[row], -cubeConst * c);
            moveByOffset(jackOutCVTrans[row], -cubeConst * c);
            moveByOffset(controlPanelTrans[row], -cubeConst * c);
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
                stepButtons[row, curDimensions[1] - 1].Highlight(false);
                //stepButtons[row, curDimensions[1] - 1].keyHit(false); // reset
                stepButtonTrans[row, curDimensions[1] - 1].gameObject.SetActive(false);

                //stepDials[row, curDimensions[1] - 1].setPercent(0.5f); // reset
                stepDialTrans[row, curDimensions[1] - 1].gameObject.SetActive(false);
            }
            curDimensions[1]--;
        }

        for (int row = 0; row < maxRows; row++)
        {
            moveByOffset(jackOutTrigTrans[row], cubeConst * c);
            moveByOffset(jackOutCVTrans[row], cubeConst * c);
            moveByOffset(controlPanelTrans[row], cubeConst * c);
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
            for (int step = 0; step < curDimensions[1]; step++)
            {
                if (controlPanelModes[curDimensions[0]].switchVal)
                {
                    stepButtonTrans[curDimensions[0], step].gameObject.SetActive(true);
                    jackOutTrigTrans[curDimensions[0]].gameObject.SetActive(true);
                }
                else
                {
                    stepDialTrans[curDimensions[0], step].gameObject.SetActive(true);
                    jackOutCVTrans[curDimensions[0]].gameObject.SetActive(true);
                }

                controlPanelTrans[curDimensions[0]].gameObject.SetActive(true);
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
                stepButtons[curDimensions[0] - 1, step].Highlight(false);
                stepButtons[curDimensions[0] - 1, step].keyHit(false); // reset
                stepButtonTrans[curDimensions[0] - 1, step].gameObject.SetActive(false);
                stepDials[curDimensions[0] - 1, step].setPercent(0.5f); // reset
                stepDialTrans[curDimensions[0] - 1, step].gameObject.SetActive(false);

                jackOutTrigTrans[curDimensions[0] - 1].GetComponentInChildren<omniJack>().endConnection(true, true);
                jackOutTrigTrans[curDimensions[0] - 1].gameObject.SetActive(false);
                jackOutCVTrans[curDimensions[0] - 1].GetComponentInChildren<omniJack>().endConnection(true, true);
                jackOutCVTrans[curDimensions[0] - 1].gameObject.SetActive(false);
                controlPanelTrans[curDimensions[0] - 1].gameObject.SetActive(false);
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

    #endregion

    #region saveload

    public override InstrumentData GetData()
    {
        SequencerCVData data = new SequencerCVData
        {
            deviceType = DeviceType.SequencerCV
        };
        GetTransformData(data);
        data.sliderSpeed = beatSlider.switchVal;

        data.switchPlay = playButton.isHit;
        data.jackTriggerInID = playTriggerInputJack.transform.GetInstanceID();

        data.activePattern = activePattern;
        data.dimensions = dimensions;

        // Initialize jagged arrays for stepBools and stepFloats
        data.stepBools = new bool[maxPattern][][];
        data.stepFloats = new float[maxPattern][][];

        for (int p = 0; p < maxPattern; p++)
        {
            data.stepBools[p] = new bool[maxRows][];
            data.stepFloats[p] = new float[maxRows][];
            for (int r = 0; r < maxRows; r++)
            {
                data.stepBools[p][r] = new bool[maxSteps];
                data.stepFloats[p][r] = new float[maxSteps];
                for (int s = 0; s < maxSteps; s++)
                {
                    data.stepBools[p][r][s] = stepBools[p, r, s];
                    data.stepFloats[p][r][s] = stepFloats[p, r, s];
                }
            }
        }

        data.jackTriggerOutID = new int[maxRows];
        for (int row = 0; row < jackOutTrigTrans.Length; row++)
        {
            data.jackTriggerOutID[row] = jackOutTrigTrans[row].GetChild(0).GetInstanceID();
        }

        data.jackCvOutID = new int[maxRows];
        for (int row = 0; row < maxRows; row++)
        {
            data.jackCvOutID[row] = jackOutCVTrans[row].GetChild(0).GetInstanceID();
        }

        data.rowMutes = new bool[maxRows];
        for (int row = 0; row < maxRows; row++)
        {
            data.rowMutes[row] = controlPanelMutes[row].isHit;
        }

        data.rowModes = new bool[maxRows];
        for (int row = 0; row < maxRows; row++)
        {
            data.rowModes[row] = controlPanelModes[row].switchVal;
        }

        data.dialSwing = swingDial.percent;
        data.switchRange = switchCVRange.switchVal;

        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        SequencerCVData data = d as SequencerCVData;
        base.Load(data, copyMode);

        // Grow to max size initially
        SetDimensions(maxRows, maxSteps);

        playButton.startToggled = data.switchPlay;

        for (int row = 0; row < maxRows; row++)
        {
            controlPanelMutes[row].phantomHit(data.rowMutes[row]);
        }

        for (int row = 0; row < maxRows; row++)
        {
            controlPanelModes[row].setSwitch(data.rowModes[row], true);
            doModeSwitch(row);
        }

        playTriggerInputJack.SetID(data.jackTriggerInID, copyMode);

        for (int p = 0; p < maxPattern; p++)
        {
            for (int r = 0; r < maxRows; r++)
            {
                for (int s = 0; s < maxSteps; s++)
                {
                    stepBools[p, r, s] = data.stepBools[p][r][s];
                    stepFloats[p, r, s] = data.stepFloats[p][r][s];
                }
            }
        }

        for (int step = 0; step < data.dimensions[1]; step++)
        {
            for (int row = 0; row < data.dimensions[0]; row++)
            {
                if (data.stepBools[data.activePattern][row][step])
                {
                    stepButtons[row, step].keyHit(true);
                }
                stepDials[row, step].setPercent(data.stepFloats[data.activePattern][row][step], true);
            }
        }

        for (int rows = 0; rows < maxRows; rows++)
        {
            jackOutTrigTrans[rows].GetComponentInChildren<omniJack>().SetID(data.jackTriggerOutID[rows], copyMode);
        }

        for (int row = 0; row < maxRows; row++)
        {
            jackOutCVTrans[row].GetComponentInChildren<omniJack>().SetID(data.jackCvOutID[row], copyMode);
        }

        beatSlider.setVal(data.sliderSpeed);
        swingDial.setPercent(data.dialSwing, true);
        switchCVRange.setSwitch(data.switchRange, true);

        // Shrink to desired size at the end
        SetDimensions(data.dimensions[0], data.dimensions[1]);
    }

    #endregion
}

public class SequencerCVData : InstrumentData
{
    public bool switchPlay;
    public int jackTriggerInID;

    public int sliderSpeed;
    public float dialSwing;

    public int[] jackTriggerOutID;
    public int[] jackCvOutID;

    public bool[] rowMutes;
    public bool[] rowModes;
    public int[] dimensions; // rows, steps

    public int activePattern;
    public bool[][][] stepBools; // Replaced bool[,,] with jagged array
    public float[][][] stepFloats; // Replaced float[,,] with jagged array

    public bool switchRange;
}

