// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright ? 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
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
// Copyright ? 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright ? 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright ? 2017 Apache 2.0 Google LLC SoundStage VR
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
using NAudio.Wave;

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

    // 2D arrays of prepoluated, networked interface elements
    public Transform[,] stepButtonTrans;
    public Transform[,] stepDialTrans;  

    // signalGenerators
    public List<sequencer> seqList; // trigger generators
    public List<sequencerCV> seqCVList; // cv generators

    public bool[] rowMute; // these should go to the right and be removed in basicSampler

    // prepopulated control panels
    public List<Transform> controlList;
    public List<button> controlMuteList;
    public List<basicSwitch> controlModeList;

    public List<Transform> trigJackOutList;
    public List<Transform> cvJackOutList;
    
    // todo: keep the convention: pattern, row / y, step / x
    // todo: make sure that they are easy to address for Mirror, regarding their sequence

    //public List<List<Transform>> cubeList;
    

    //void test(){
    //    cubeDials[0][0].GetComponentInChildren<dial>()
    //}

    // sequencer
    public bool running = true;
    float swingPercent = 0;
    int beatSpeed = 0;

    // handles
    public Transform stretchNode;
    public xHandle stepSelect;

    // x, y
    public int[] dimensions = new int[] { 1, 1 };
    int[] curDimensions = new int[] { 0, 0 };

    float cubeConst = .04f;

    int maxDim = 16; // limit for x and y

    public sliderNotched beatSlider;
    public omniJack playTriggerInputJack;
    public button playButton;
    dial swingDial;
    signalGenerator clockGenerator; 
    signalGenerator resetGenerator;
    beatTracker _beatManager; 
    public basicSwitch switchCVRange; // TODO: Consider using a notched dial, 1,2,3,4,5,6,7,8,9,10 octaves ranges
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
        //cubeList = new List<List<Transform>>();
        trigJackOutList = new List<Transform>();
        cvJackOutList = new List<Transform>();
        seqList = new List<sequencer>();
        seqCVList = new List<sequencerCV>();

        controlList = new List<Transform>();
        controlMuteList = new List<button>();
        controlModeList = new List<basicSwitch>();
        rowMute = new bool[maxDim];



        stepBools = new bool[maxPattern, maxDim, maxDim];    // 3D jagged array initialization
        stepFloats = new float[maxPattern, maxDim, maxDim];  // 3D jagged array initialization

        stepDialTrans = new Transform[maxDim, maxDim];
        stepButtonTrans = new Transform[maxDim, maxDim];


        for (int i = 0; i < maxDim; i++)
        {
            for (int i2 = 0; i2 < maxDim; i2++)
            {
                // this overwrites the init value of the touchDial prefab
                stepFloats[activePattern, i, i2] = 0.5f;
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
            //dimensionDisplays[i].gameObject.SetActive(false);
        }

        dimensionDisplays[0].GetComponent<Renderer>().material.SetFloat("_EmissionGain", .3f);
        dimensionDisplays[1].GetComponent<Renderer>().material.SetFloat("_EmissionGain", .3f);

        // init ranges
        for (int i = 0; i < curDimensions[1]; i++)
        {
            seqCVList[i].setRange(lastRangeLow ? sequencerCV.lowRange : sequencerCV.highRange);
        }
    }

    void Start()
    {
        _beatManager.setTriggers(executeNextStep, resetSteps); // TODO: these function must be triggered from signal generator then
        _beatManager.updateBeatNoTriplets(beatSpeed);
        _beatManager.updateSwing(swingPercent);

        spawnMaxDimensions();
    }

    void Update()
    {

        SelectStepUpdate();

        dimensions[0] = Mathf.CeilToInt((stretchNode.localPosition.x + cubeConst * .75f) / -cubeConst);
        dimensions[1] = Mathf.CeilToInt((stretchNode.localPosition.y + cubeConst * .75f) / -cubeConst);

        if (dimensions[0] < 1) dimensions[0] = 1;
        if (dimensions[1] < 1) dimensions[1] = 1;
        if (dimensions[0] > maxDim) dimensions[0] = maxDim;
        if (dimensions[1] > maxDim) dimensions[1] = maxDim;
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

        // update ranges of CVSequencers if changed
        if (switchCVRange.switchVal != lastRangeLow)
        {
            lastRangeLow = switchCVRange.switchVal;
            for (int i = 0; i < curDimensions[1]; i++)
            {
                seqCVList[i].setRange(lastRangeLow ? sequencerCV.lowRange : sequencerCV.highRange); // 0.1/Oct -> select between -1,1 and -4,4 ranges
            }
        }

        // read in all dials, because there is no hit-style update system for dials yet
        // the search in the scene graph might take too long!
        readAllDials();

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

        for (int i = 0; i < curDimensions[1]; i++)
        {
            
            if (rowMute[i]) continue; 
            
            seqList[i].setSignal(stepBools[activePattern, targetStep, i]);      
            seqCVList[i].setSignal(stepFloats[activePattern, targetStep, i] * 2f - 1f);   
        }
    }

    void SelectStepUpdate()
    {
        if (targetStep == curStep) return;
        if (curStep < dimensions[0]) stepOff(curStep);
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

        int next = (targetStep + s) % dimensions[0];

        if (next == 0 && clockGenerator != null && !minicheck) forcePlay(false);
        else SelectStep(next);


    }

    void stepOff(int step)
    {
        for (int i = 0; i < curDimensions[1]; i++)
        {
            if (stepButtonTrans[i, step] != null) stepButtonTrans[i, step].GetComponent<button>().Highlight(false);
        }
    }

    void stepOn(int step)
    {
        for (int i = 0; i < curDimensions[1]; i++)
        {
            if (stepButtonTrans[i, step] != null) stepButtonTrans[i, step].GetComponent<button>().Highlight(true);
        }
    }
        
    public void readAllDials(){
        for (int i = 0; i < dimensions[0]; i++)
        {
          for (int i2 = 0; i2 < dimensions[1]; i2++)
          {
            stepFloats[activePattern, i2, i] = stepDialTrans[i2, i].GetComponentInChildren<dial>().percent; 
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
        // bugfix for randomly skipped / missed steps in sequencer. 
        // this routine would fire even if the step selector handle was not touched or grabbed. 
        // this could be due to an multithread issue between main and audio thread, which is still unsolved.
        if (stepSelect.curState != manipObject.manipState.grabbed) return; 

        int s = (int)Mathf.Round(stepSelect.transform.localPosition.x / -cubeConst);
        if (s == selectedStep) return;
        //Debug.Log("step dragged");
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
        //dimensionDisplays[0].gameObject.SetActive(false);
    }

    #endregion

    #region size-management

    void spawnMaxDimensions(){

        for (int row = 0; row < maxDim; row++)
        {

            for (int step = 0; step < maxDim; step++)
            {
                setupStepPrefabRow(stepDialPrefab, row, step);
                setupStepPrefabRow(stepButtonPrefab, row, step);
            }


            // jacks for triggers
            Transform jack = (Instantiate(triggerJackOutPrefab, Vector3.zero, Quaternion.identity) as GameObject).transform;
            jack.parent = transform;
            jack.localRotation = Quaternion.Euler(0, 90, -90);
            jack.localScale = Vector3.one;
            //jack.localPosition = new Vector3(-cubeConst / 2f - .001f - cubeConst * (curDimensions[0] - 1), -cubeConst * curDimensions[1], 0);
            jack.localPosition = new Vector3(-cubeConst * (maxDim - 1 + 3), -cubeConst * row, cubeConst * 0.5f);

            trigJackOutList.Add(jack);
            seqList.Add(jack.GetComponent<sequencer>());

            // jacks for cvs
            Transform cvJack = (Instantiate(cvJackOutPrefab, Vector3.zero, Quaternion.identity) as GameObject).transform;
            cvJack.parent = transform;
            cvJack.localRotation = Quaternion.Euler(0, 90, -90);
            cvJack.localScale = Vector3.one;
            //cvJack.localPosition = new Vector3(-cubeConst / 2f - .001f - cubeConst * (curDimensions[0] - 1), -cubeConst * curDimensions[1], 0);
            cvJack.localPosition = new Vector3(-cubeConst * (maxDim - 1 + 3), -cubeConst * row, cubeConst * 0.5f);

            cvJackOutList.Add(cvJack);
            seqCVList.Add(cvJack.GetComponent<sequencerCV>());

            // controlPrefab
            Transform ctrl = (Instantiate(controlPrefab, Vector3.zero, Quaternion.identity) as GameObject).transform;
            ctrl.parent = transform;
            ctrl.localRotation = Quaternion.Euler(0, 90, -90);
            ctrl.localScale = Vector3.one;
            //cvJack.localPosition = new Vector3(-cubeConst / 2f - .001f - cubeConst * (curDimensions[0] - 1), -cubeConst * curDimensions[1], 0);
            ctrl.localPosition = new Vector3(-cubeConst * (maxDim - 1 + 1), -cubeConst * row, cubeConst * 0.5f);

            controlList.Add(ctrl);
            controlMuteList.Add(ctrl.GetComponentInChildren<button>());
            controlMuteList.Last()._componentInterface = (componentInterface)this;
            controlModeList.Add(ctrl.GetComponentInChildren<basicSwitch>());

        }

        curDimensions[0] = dimensions[0] = maxDim;
        curDimensions[1] = dimensions[1] = maxDim;
        dimensionDisplays[0].text = curDimensions[0] + " X " + curDimensions[1];
        SetDimensions(maxDim, maxDim); 

    }

    void setupStepPrefabRow(GameObject prefab, int y, int x)
    {
        Transform t = (Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject).transform;
        t.parent = transform;
        t.localRotation = Quaternion.identity;
        
        t.localPosition = new Vector3(-cubeConst * x, -cubeConst * y, 0);

        t.localScale = Vector3.one;
        if (prefab == stepButtonPrefab) stepButtonTrans[y, x] = t;
        if (prefab == stepDialPrefab) stepDialTrans[y, x] = t;

        float Hval = (float) y / maxDim; // todo: remove?

        if (prefab == stepButtonPrefab) t.GetComponent<button>().Setup(x, y, stepBools[activePattern, x, y], Color.HSVToRGB(Hval, .9f, .05f));
        if (prefab == stepDialPrefab) t.GetComponentInChildren<dial>().setPercent(stepFloats[activePattern, y, x]);
    }



    void UpdateDimensions()
    {
        if (dimensions[0] == curDimensions[0] && dimensions[1] == curDimensions[1]) return;

        stretchNode.GetComponent<xyHandle>().pulse();
        if (dimensions[0] > curDimensions[0])
        {
            addColumns(dimensions[0] - curDimensions[0]);
        }
        else if (dimensions[0] < curDimensions[0])
        {
            removeColumns(curDimensions[0] - dimensions[0]);
        }
        if (dimensions[1] > curDimensions[1])
        {
            addRows(dimensions[1] - curDimensions[1]);
        }
        else if (dimensions[1] < curDimensions[1])
        {
            removeRows(curDimensions[1] - dimensions[1]);
        }

        dimensionDisplays[0].text = curDimensions[0] + " X " + curDimensions[1];
    }


    public void SetDimensions(int rows, int steps)
    {
        dimensions[0] = rows;
        dimensions[1] = steps;
        Vector3 p = stretchNode.localPosition;
        p.x = rows * -cubeConst - cubeConst * .75f;
        p.y = steps * -cubeConst - cubeConst * .75f;

        stretchNode.localPosition = p;

        UpdateDimensions();
    }

    void addColumns(int c)
    {
        for (int i = 0; i < c; i++)
        {
            for (int i2 = 0; i2 < curDimensions[1]; i2++)
            {

                // todo: show column

                //// stepDialPrefab
                //setupStepPrefabColumn(stepDialPrefab, i2, true);

                //// stepCubePrefab 
                //setupStepPrefabColumn(stepButtonPrefab, i2, false);

                moveByOffset(trigJackOutList[i2], -cubeConst);
                moveByOffset(cvJackOutList[i2], -cubeConst);
                moveByOffset(controlList[i2], -cubeConst);

            }
            curDimensions[0]++;
        }
        stepSelect.xBounds.x = -cubeConst * (curDimensions[0] - 1);
        stepSelect.updatePos(stepSelect.transform.localPosition.x);
    }

    //void setupStepPrefabColumn(GameObject prefab, int i2, bool setupDial){
    //    Transform t = (Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject).transform;
    //    t.parent = transform;
    //    t.localRotation = Quaternion.identity;
    //    float xMult = curDimensions[0];
    //    float yMult = i2;
    //    t.localPosition = new Vector3(-cubeConst * xMult, -cubeConst * yMult, 0);

    //    t.localScale = Vector3.one;
    //    if(!setupDial) stepButtonTrans[i2].Add(t);
    //    if(setupDial) stepDialTrans[i2].Add(t);

    //    float Hval = (float)i2 / maxDim;
    //    if(!setupDial) t.GetComponent<button>().Setup(curDimensions[0], i2, stepBools[activePattern, curDimensions[0], i2], Color.HSVToRGB(Hval, .9f, .05f));
    //    if(setupDial) t.GetComponentInChildren<dial>().setPercent(stepFloats[activePattern, 0, i2]);
    //}

    void moveByOffset(Transform t, float offset){
        Vector3 pJ;
        pJ = t.localPosition;
        pJ.x += offset;
        t.localPosition = pJ;
    }

    void removeColumns(int c)
    {
        for (int i = 0; i < c; i++)
        {
            for (int i2 = 0; i2 < curDimensions[1]; i2++)
            {
                //todo: hide columns

                //Transform t = cubeList[i2].Last();
                //stepDialTrans[i2].RemoveAt(stepDialTrans[i2].Count - 1);
                //Destroy(t.gameObject);
                //cubeList[i2].RemoveAt(cubeList[i2].Count - 1);

                moveByOffset(trigJackOutList[i2], cubeConst);
                moveByOffset(cvJackOutList[i2], cubeConst);
                moveByOffset(controlList[i2], cubeConst);
            }
            curDimensions[0]--;
        }
        stepSelect.xBounds.x = -cubeConst * (curDimensions[0] - 1);
        stepSelect.updatePos(stepSelect.transform.localPosition.x);
    }

    void addRows(int c)
    {
        // toto: add show

        updateStepSelectVertical();
    }


    void updateStepSelectVertical()
    {
        Vector3 sPos = stepSelect.transform.localPosition;
        sPos.y = -cubeConst * (curDimensions[1]);
        stepSelect.transform.localPosition = sPos;
    }

    void removeRows(int c)
    {

        // todo: add hiding

        //for (int i = 0; i < c; i++)
        //{

        //    int z = cubeList.Count - 1;
        //    stepDialTrans.RemoveAt(z);
        //    for (int i2 = 0; i2 < cubeList[z].Count; i2++)
        //    {
        //        Transform t = cubeList[z, i2];
        //        Destroy(t.gameObject);
        //    }
        //    cubeList.RemoveAt(z);


        //    Transform j = trigJackOutList.Last();
        //    Destroy(j.gameObject);
        //    trigJackOutList.RemoveAt(trigJackOutList.Count - 1);
        //    seqList.RemoveAt(trigJackOutList.Count);

        //    j = cvJackOutList.Last();
        //    Destroy(j.gameObject);
        //    cvJackOutList.RemoveAt(cvJackOutList.Count - 1);
        //    seqCVList.RemoveAt(cvJackOutList.Count);

        //    j = controlList.Last();
        //    Destroy(j.gameObject);
        //    controlModeList.RemoveAt(controlModeList.Count - 1);
        //    controlMuteList.RemoveAt(controlModeList.Count);

        //    row[1]--;
        //}

        updateStepSelectVertical();
    }

    #endregion

    #region saveload

    public override InstrumentData GetData()
    {
        // TODO implement serialization for knobs, etc
        SequencerCVData data = new SequencerCVData();
        data.deviceType = DeviceType.SequencerCV;
        GetTransformData(data);
        data.sliderSpeed = beatSlider.switchVal;
        
        data.switchPlay = playButton.isHit;
        data.jackTriggerInID = playTriggerInputJack.transform.GetInstanceID();

        data.activePattern = activePattern;
        data.dimensions = dimensions;
        data.stepBools = stepBools;
        data.stepFloats = stepFloats;

        data.jackTriggerOutID = new int[trigJackOutList.Count];
        for (int i = 0; i < trigJackOutList.Count; i++)
        {
            data.jackTriggerOutID[i] = trigJackOutList[i].GetChild(0).GetInstanceID();
        }

        data.jackCvOutID = new int[cvJackOutList.Count];
        for (int i = 0; i < cvJackOutList.Count; i++)
        {
          data.jackCvOutID[i] = cvJackOutList[i].GetChild(0).GetInstanceID();
        }

        data.buttonMutes = rowMute;
        data.dialSwing = swingDial.percent;


        data.switchRange = switchCVRange.switchVal;

        return data;
    }

    public override void Load(InstrumentData d)
    {
        SequencerCVData data = d as SequencerCVData;
        base.Load(data);

        playButton.startToggled = data.switchPlay;

        //tapeList = data.rowSamples;
        rowMute = data.buttonMutes;

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

        for (int i = 0; i < data.dimensions[0]; i++)
        {
            for (int i2 = 0; i2 < data.dimensions[1]; i2++)
            {
                if (data.stepBools[data.activePattern,i,i2])
                {
                    stepButtonTrans[i2, i].GetComponentInChildren<button>().keyHit(true);                  
                }
                stepButtonTrans[i2, i].GetComponentInChildren<dial>().setPercent(data.stepFloats[data.activePattern, i, i2]);
            }
        }

        for (int i = 0; i < trigJackOutList.Count; i++)
        {
            trigJackOutList[i].GetComponentInChildren<omniJack>().ID = data.jackTriggerOutID[i];
        }

        for (int i = 0; i < cvJackOutList.Count; i++)
        {
            cvJackOutList[i].GetComponentInChildren<omniJack>().ID = data.jackCvOutID[i];
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
  
  public bool[] buttonMutes;
  public int[] dimensions;

  public int activePattern;
  public bool[,,] stepBools;
  public float[,,] stepFloats;

  public bool switchRange;

}