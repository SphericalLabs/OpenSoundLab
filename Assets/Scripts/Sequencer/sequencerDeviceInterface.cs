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

    SequencerGridHelper gridHelper;
    SequencerSaveLoadHelper saveLoadHelper;
    SequencerPlaybackHelper playbackHelper;

    // sequencer
    public bool running = true;

    // handles
    public xyHandle xyHandle;
    public Transform stretchNode;
    public xHandle stepSelect;
    public Vector2 stretchHandleOffset = new Vector2(0.01f, 0.02f);

    // row, step
    public int[] dimensions = new int[] { 1, 1 };
    int[] curDimensions = new int[] { 0, 0 };

    float cubeConst = .04f;

    int maxSteps = 64;
    int maxRows = 16;

    bool[] rowModeListenerAdded;

    public omniJack resetJack, clockJack, phaseJack;
    public button playButton;
    public basicSwitch switchCVRange;
    bool lastRangeLow = true;

    double _sampleDuration = 0;

    public TextMesh[] dimensionDisplays;

    public bool initialised = false;

    #endregion

    #region basics

    public override void Awake()
    {
        base.Awake();

        ensureHelpers();
        configureHandleNotches();

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

    void ensureHelpers()
    {
        if (gridHelper == null) gridHelper = new SequencerGridHelper(this);
        if (saveLoadHelper == null) saveLoadHelper = new SequencerSaveLoadHelper(this);
        if (playbackHelper == null) playbackHelper = new SequencerPlaybackHelper(this);
    }

    void configureHandleNotches()
    {
        xyHandle target = xyHandle;
        if (target == null && stretchNode != null) target = stretchNode.GetComponent<xyHandle>();
        if (target == null) return;
        target.useNotches = false;
        target.useGridSnap = true;
        target.xGridSize = -cubeConst;
        target.yGridSize = -cubeConst;
        target.xGridOffset = -cubeConst * .75f + stretchHandleOffset.x;
        target.yGridOffset = -cubeConst * .75f + stretchHandleOffset.y;
        target.xGridMin = 1;
        target.xGridMax = maxSteps;
        target.yGridMin = 1;
        target.yGridMax = maxRows;
    }

    void Start()
    {
    }

    void Update()
    {
        ensureHelpers();
        playbackHelper.selectStepUpdate();

        dimensions[1] = Mathf.CeilToInt((stretchNode.localPosition.x + cubeConst * .75f) / -cubeConst);
        dimensions[0] = Mathf.CeilToInt((stretchNode.localPosition.y + cubeConst * .75f) / -cubeConst);

        if (dimensions[1] < 1) dimensions[1] = 1;
        if (dimensions[0] < 1) dimensions[0] = 1;
        if (dimensions[1] > maxSteps) dimensions[1] = maxSteps;
        if (dimensions[0] > maxRows) dimensions[0] = maxRows;
        UpdateDimensions();
        playbackHelper.updateGenerators();

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

    public int CurStep
    {
        get
        {
            ensureHelpers();
            return playbackHelper.getCurStep();
        }
        set
        {
            ensureHelpers();
            playbackHelper.setCurStep(value);
        }
    }

    public int TargetStep
    {
        get
        {
            ensureHelpers();
            return playbackHelper.getTargetStep();
        }
        set
        {
            ensureHelpers();
            playbackHelper.setTargetStep(value);
        }
    }
    public bool silent = false;


    public void SelectStep(int s, bool silent = false)
    {
        ensureHelpers();
        playbackHelper.selectStep(s, silent);
    }


    // called from audio thread
    public void executeNextStep()
    {
        ensureHelpers();
        playbackHelper.executeNextStep();
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

    private void OnAudioFilterRead(float[] buffer, int channels)
    {
        ensureHelpers();
        playbackHelper.onAudioFilterRead(buffer, channels);
    }

    // called from Update loop
    public void UpdateStepSelect(bool forced = false)
    {
        ensureHelpers();
        playbackHelper.updateStepSelect(forced);
    }

    public void togglePlay(bool on)
    {
        ensureHelpers();
        playbackHelper.togglePlay(on);
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
        ensureHelpers();
        gridHelper.spawnInitialDimensions();
    }

    public void SetDimensions(int rows, int steps)
    {
        ensureHelpers();
        gridHelper.setDimensions(rows, steps);
    }

    void UpdateDimensions()
    {
        ensureHelpers();
        gridHelper.updateDimensions();
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

    public int[] getCurrentDimensions()
    {
        return curDimensions;
    }

    public float getCubeConst()
    {
        return cubeConst;
    }

    public bool[] getRowModeListenerFlags()
    {
        return rowModeListenerAdded;
    }

    public trigSignalGenerator[] getRowTriggerGenerators()
    {
        return jackOutTrigGenerators;
    }

    public cvSignalGenerator[] getRowCvGenerators()
    {
        return jackOutCVGenerators;
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
        ensureHelpers();
        return saveLoadHelper.buildData();
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        SequencerData data = d as SequencerData;
        ensureHelpers();
        saveLoadHelper.loadData(data, copyMode);
    }

    #endregion
}

public class SequencerData : InstrumentData
{
    public bool switchPlay;
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
