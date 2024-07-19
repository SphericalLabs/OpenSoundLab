// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
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
using System.Collections.Generic;
using System.Linq;
using static UnityEngine.Rendering.DebugUI.Table;

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
    public Transform stretchNode;
    public xHandle stepSelect;

    // row, step
    public int[] dimensions = new int[] { 1, 1 };
    int[] curDimensions = new int[] { 0, 0 };

    float cubeConst = .04f;

    //int maxDim = 16; // limit for x and y

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

        for (int i = 0; i < curDimensions[0]; i++)
        {
            jackOutCVGenerators[i].setRange(lastRangeLow ? cvSignalGenerator.lowRange : cvSignalGenerator.highRange);
        }

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
        if (dimensions[1] > maxRows) dimensions[1] = maxRows;
        if (dimensions[0] > maxSteps) dimensions[0] = maxSteps;
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

    int targetStep = 0;
    public void SelectStep(int s, bool silent = false)
    {
        selectedStep = targetStep = s;

        if (silent) return;

        // it is important that this is running here, since this is called from audio thread and directly manipulates the sequencer outputs
        // this is one aspect of making the sequencer sample accurate!
        for (int i = 0; i < curDimensions[0]; i++)
        {
            if (controlPanelMutes[i].isHit) continue;

            jackOutTrigGenerators[i].setSignal(stepBools[activePattern, targetStep, i]);
            jackOutCVGenerators[i].setSignal(stepFloats[activePattern, targetStep, i] * 2f - 1f);
        }
    }

    void SelectStepUpdate()
    {
        if (targetStep == curStep) return;
        if (curStep < dimensions[1]) stepOff(curStep);
        curStep = targetStep;
        stepOn(curStep);
        stepSelect.updatePos(-cubeConst * curStep);
    }

    int curStep = 0;
    public bool silent = false;

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

    int selectedStep = 0;
    void UpdateStepSelect()
    {
        if (stepSelect.curState != manipObject.manipState.grabbed) return;

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
        if (!on) SelectStep(0, true);
        else runningUpdated = true;
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
        bool oddSpawn;

        for (int row = 0; row < maxRows; row++)
        {
            oddSpawn = row % 2 == 1;

            for (int step = 0; step < maxSteps; step++)
            {
                // alternate between trigger and cv rows    
                setupStepPrefabRow(stepDialPrefab, row, step, oddSpawn); 
                setupStepPrefabRow(stepButtonPrefab, row, step, !oddSpawn);
            }

            // jacks for triggers
            Transform jackTrig = Instantiate(triggerJackOutPrefab, Vector3.zero, Quaternion.identity).transform;
            jackTrig.parent = transform;
            jackTrig.localRotation = Quaternion.Euler(0, 90, -90);
            jackTrig.localScale = Vector3.one;
            jackTrig.localPosition = new Vector3(-cubeConst * (maxSteps - 1 + 3), -cubeConst * row, cubeConst * 0.5f);

            jackOutTrigTrans[row] = jackTrig;
            jackOutTrigGenerators[row] = jackTrig.GetComponentInChildren<trigSignalGenerator>();

            // jacks for cvs
            Transform jackCV = Instantiate(cvJackOutPrefab, Vector3.zero, Quaternion.identity).transform;
            jackCV.parent = transform;
            jackCV.localRotation = Quaternion.Euler(0, 90, -90);
            jackCV.localScale = Vector3.one;
            jackCV.localPosition = new Vector3(-cubeConst * (maxSteps - 1 + 3), -cubeConst * row, cubeConst * 0.5f);

            jackOutCVTrans[row] = jackCV;
            jackOutCVGenerators[row] = jackCV.GetComponentInChildren<cvSignalGenerator>();

            // controlPrefab
            Transform ctrl = Instantiate(controlPrefab, Vector3.zero, Quaternion.identity).transform;
            ctrl.parent = transform;
            ctrl.localRotation = Quaternion.Euler(0, 90, -90);
            ctrl.localScale = Vector3.one;
            ctrl.localPosition = new Vector3(-cubeConst * (maxSteps - 1 + 1), -cubeConst * row, cubeConst * 0.5f);

            controlPanelTrans[row] = ctrl;
            controlPanelMutes[row] = ctrl.GetComponentInChildren<button>();
            controlPanelModes[row] = ctrl.GetComponentInChildren<basicSwitch>();
            controlPanelModes[row].setSwitch(oddSpawn); 
        }

        // shrink to start size
        curDimensions[0] = maxRows;
        curDimensions[1] = maxSteps;
        
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
                    stepButtonTrans[row, curDimensions[1] - 1].gameObject.SetActive(true);
                }
                else
                {
                    stepDialTrans[row, curDimensions[1] - 1].gameObject.SetActive(true);
                }

                moveByOffset(jackOutTrigTrans[row], -cubeConst);
                moveByOffset(jackOutCVTrans[row], -cubeConst);
                moveByOffset(controlPanelTrans[row], -cubeConst);
            }
            curDimensions[1]++;
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
                stepButtonTrans[row, curDimensions[1] - 1].gameObject.SetActive(false);
                stepDialTrans[row, curDimensions[1] - 1].gameObject.SetActive(false);

                moveByOffset(jackOutTrigTrans[row], cubeConst);
                moveByOffset(jackOutCVTrans[row], cubeConst);
                moveByOffset(controlPanelTrans[row], cubeConst);
            }
            curDimensions[1]--;
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
                if (controlPanelModes[curDimensions[0] - 1].switchVal)
                {
                    stepButtonTrans[curDimensions[0] - 1, step].gameObject.SetActive(true);
                    jackOutTrigTrans[curDimensions[0] - 1].gameObject.SetActive(true);
                }
                else
                {
                    stepDialTrans[curDimensions[0] - 1, step].gameObject.SetActive(true);
                    jackOutCVTrans[curDimensions[0] - 1].gameObject.SetActive(true);
                }

                controlPanelTrans[curDimensions[0] - 1].gameObject.SetActive(true);
            }
            curDimensions[0]++;
        }

        updateStepSelectVertical();
    }

    void removeRows(int c)
    {
        for (int i = 0; i < c; i++)
        {
            for (int step = 0; step < curDimensions[1]; step++)
            {
                stepButtonTrans[curDimensions[0] - 1, step].gameObject.SetActive(false);
                stepDialTrans[curDimensions[0] - 1, step].gameObject.SetActive(false);

                jackOutTrigTrans[curDimensions[0] - 1].gameObject.SetActive(false);
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
        data.stepBools = stepBools;
        data.stepFloats = stepFloats;

        data.jackTriggerOutID = new int[jackOutTrigTrans.Length];
        for (int i = 0; i < jackOutTrigTrans.Length; i++)
        {
            data.jackTriggerOutID[i] = jackOutTrigTrans[i].GetChild(0).GetInstanceID();
        }

        data.jackCvOutID = new int[jackOutCVTrans.Length];
        for (int i = 0; i < jackOutCVTrans.Length; i++)
        {
            data.jackCvOutID[i] = jackOutCVTrans[i].GetChild(0).GetInstanceID();
        }

        data.rowMutes = new bool[maxRows];
        for (int i = 0; i < maxRows; i++)
        {
            data.rowMutes[i] = controlPanelMutes[i].isHit;
        }

        data.rowModes = new bool[maxRows];
        for (int i = 0; i < maxRows; i++)
        {
            data.rowModes[i] = controlPanelModes[i].switchVal;
        }

        data.dialSwing = swingDial.percent;
        data.switchRange = switchCVRange.switchVal;

        return data;
    }

    public override void Load(InstrumentData d)
    {
        SequencerCVData data = d as SequencerCVData;
        base.Load(data);

        playButton.startToggled = data.switchPlay;

        for (int i = 0; i < maxRows; i++)
        {
            controlPanelMutes[i].phantomHit(data.rowMutes[i]);
        }

        for (int i = 0; i < maxRows; i++)
        {
            controlPanelModes[i].setSwitch(data.rowModes[i]);
        }

        playTriggerInputJack.ID = data.jackTriggerInID;
        SetDimensions(data.dimensions[0], data.dimensions[1]);

        for (int i = 0; i < data.stepBools.GetLength(0); i++)
        {
            for (int i2 = 0; i2 < data.stepBools.GetLength(1); i2++)
            {
                stepBools[data.activePattern, i, i2] = data.stepBools[activePattern, i, i2];
                stepFloats[data.activePattern, i, i2] = data.stepFloats[activePattern, i, i2];
            }
        }

        for (int step = 0; step < data.dimensions[1]; step++)
        {
            for (int row = 0; row < data.dimensions[0]; row++)
            {
                if (data.stepBools[data.activePattern, step, row])
                {
                    stepButtons[row, step].keyHit(true);
                }
                stepDials[row, step].setPercent(data.stepFloats[data.activePattern, step, row]);
            }
        }

        for (int i = 0; i < jackOutTrigTrans.Length; i++)
        {
            jackOutTrigTrans[i].GetComponentInChildren<omniJack>().ID = data.jackTriggerOutID[i];
        }

        for (int i = 0; i < jackOutCVTrans.Length; i++)
        {
            jackOutCVTrans[i].GetComponentInChildren<omniJack>().ID = data.jackCvOutID[i];
        }

        beatSlider.setVal(data.sliderSpeed);
        swingDial.setPercent(data.dialSwing);
        switchCVRange.setSwitch(data.switchRange, true);
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
    public bool[,,] stepBools;
    public float[,,] stepFloats;

    public bool switchRange;
}
