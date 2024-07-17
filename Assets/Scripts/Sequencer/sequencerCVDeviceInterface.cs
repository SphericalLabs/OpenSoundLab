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

public class sequencerCVDeviceInterface : deviceInterface
{

    #region fields

    // out generators are nested in the jackOutPrefabs
    public GameObject triggerJackOutPrefab, cvJackOutPrefab, controlPrefab;
    public GameObject stepDialPrefab, stepCubePrefab; 
     
    public Transform stretchNode;
    public xHandle stepSelect;
    public bool running = true;

    float swingPercent = 0;
    int beatSpeed = 0;

    public int[] dimensions = new int[] { 1, 1 };
    int[] curDimensions = new int[] { 0, 0 };

    public List<List<Transform>> cubeList;

    public bool[][] cubeBools;  // the step sequencer button values
    public List<sequencer> seqList; // trigger generators
    public List<Transform> trigJackOutList;

    public float[][] cubeFloats; // the step sequencer dial values
    public List<sequencerCV> seqCVList; // cv generators
    public List<List<dial>> cubeDials;  // the step sequencer dials, keep them so that you don't have to search them in the scene graph on Update()
    public List<Transform> cvJackOutList;

    public List<Transform> controlList;
    public List<button> controlMuteList;
    public List<basicSwitch> controlModeList;

    //public List<clipPlayerSimple> samplerList; // TODO: remove this, will be a seperate device

    public bool[] rowMute; // these should go to the right and be removed in basicSampler

    //public string[][] tapeList; // why 2-dimensional? TODO: remove

    float cubeConst = .04f;

    int max = 16; // limit for x and y

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
        cubeList = new List<List<Transform>>();
        trigJackOutList = new List<Transform>();
        cvJackOutList = new List<Transform>();
        seqList = new List<sequencer>();
        seqCVList = new List<sequencerCV>();

        controlList = new List<Transform>();
        controlMuteList = new List<button>();
        controlModeList = new List<basicSwitch>();

        cubeBools = new bool[max][]; // init cubes horizontally
        cubeFloats = new float[max][];

        cubeDials = new List<List<dial>>();

        rowMute = new bool[max];
        for (int i = 0; i < max; i++)
        {
            cubeBools[i] = new bool[max]; // init cubes vertically
            cubeFloats[i] = new float[max];
        }

        for (int i = 0; i < max; i++)
        {
          for (int i2 = 0; i2 < max; i2++)
          {
            cubeFloats[i][i2] = 0.5f; // this overwrites the init value of the touchDial prefab!
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
    }

    void Update()
    {

        SelectStepUpdate();

        dimensions[0] = Mathf.CeilToInt((stretchNode.localPosition.x + cubeConst * .75f) / -cubeConst);
        dimensions[1] = Mathf.CeilToInt((stretchNode.localPosition.y + cubeConst * .75f) / -cubeConst);

        if (dimensions[0] < 1) dimensions[0] = 1;
        if (dimensions[1] < 1) dimensions[1] = 1;
        if (dimensions[0] > max) dimensions[0] = max;
        if (dimensions[1] > max) dimensions[1] = max;
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
            
            seqList[i].setSignal(cubeBools[targetStep][i]);      
            seqCVList[i].setSignal(cubeFloats[targetStep][i] * 2f - 1f);   
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
            if (cubeList[i][step] != null) cubeList[i][step].GetComponent<button>().Highlight(false);
        }
    }

    void stepOn(int step)
    {
        for (int i = 0; i < curDimensions[1]; i++)
        {
            if (cubeList[i][step] != null) cubeList[i][step].GetComponent<button>().Highlight(true);
        }
    }
        
    
    public void readAllDials(){
        for (int i = 0; i < dimensions[0]; i++)
        {
          for (int i2 = 0; i2 < dimensions[1]; i2++)
          {
            //cubeFloats[i][i2] = cubeList[i2][i].GetComponentInChildren<dial>().percent;
            cubeFloats[i][i2] = cubeDials[i2][i].percent; // read from local registry of dials, avoid searching
            // please note: i2 and i are swapped for these cube lists, but works fine
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
        cubeBools[IDx][IDy] = on;
    }

    public override void onSelect(bool on, int IDx, int IDy)
    {
        if (!on) dimensionDisplays[1].gameObject.SetActive(false);
        else
        {
            dimensionDisplays[1].text = ((IDx + 1) + "X" + (IDy + 1)).ToString();
            dimensionDisplays[1].gameObject.SetActive(true);
            Vector3 pos = cubeList[IDy][IDx].localPosition;
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

    public void SetDimensions(int dimX, int dimY)
    {
        dimensions[0] = dimX;
        dimensions[1] = dimY;
        Vector3 p = stretchNode.localPosition;
        p.x = dimX * -cubeConst - cubeConst * .75f;
        p.y = dimY * -cubeConst - cubeConst * .75f;

        stretchNode.localPosition = p;

        UpdateDimensions();
    }

    void addColumns(int c)
    {
        for (int i = 0; i < c; i++)
        {
            for (int i2 = 0; i2 < curDimensions[1]; i2++)
            {
                // stepDialPrefab
                setupStepPrefabColumn(stepDialPrefab, i2);

                // stepCubePrefab 
                setupStepPrefabColumn(stepCubePrefab, i2);

                moveByOffset(trigJackOutList[i2], -cubeConst);
                moveByOffset(cvJackOutList[i2], -cubeConst);
                moveByOffset(controlList[i2], -cubeConst);

            }
            curDimensions[0]++;
        }
        stepSelect.xBounds.x = -cubeConst * (curDimensions[0] - 1);
        stepSelect.updatePos(stepSelect.transform.localPosition.x);
    }

    void setupStepPrefabColumn(GameObject prefab, int i2){
        Transform t = (Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject).transform;
        t.parent = transform;
        t.localRotation = Quaternion.identity;
        float xMult = curDimensions[0];
        float yMult = i2;
        t.localPosition = new Vector3(-cubeConst * xMult, -cubeConst * yMult, 0);

        t.localScale = Vector3.one;
        cubeList[i2].Add(t);
        cubeDials[i2].Add(t.GetComponentInChildren<dial>());

        float Hval = (float)i2 / max;
        t.GetComponent<button>()?.Setup(curDimensions[0], i2, cubeBools[curDimensions[0]][i2], Color.HSVToRGB(Hval, .9f, .05f));
        t.GetComponentInChildren<dial>()?.setPercent(cubeFloats[0][i2]);
    }

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
                Transform t = cubeList[i2].Last();
                cubeDials[i2].RemoveAt(cubeDials[i2].Count - 1);
                Destroy(t.gameObject);
                cubeList[i2].RemoveAt(cubeList[i2].Count - 1);

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
        for (int i = 0; i < c; i++)
        {
            cubeList.Add(new List<Transform>());
            cubeDials.Add(new List<dial>());

            for (int i2 = 0; i2 < curDimensions[0]; i2++)
            {
                setupStepPrefabRow(stepDialPrefab, i2);
                setupStepPrefabRow(stepCubePrefab, i2);
            }

            // TODO: add a third prefab for mute, switch between dials and buttons, 

            // jackOutPrefab for triggers
            Transform jack = (Instantiate(triggerJackOutPrefab, Vector3.zero, Quaternion.identity) as GameObject).transform;
            jack.parent = transform;
            jack.localRotation = Quaternion.Euler(0, 0, 0);
            jack.localScale = Vector3.one;
            //jack.localPosition = new Vector3(-cubeConst / 2f - .001f - cubeConst * (curDimensions[0] - 1), -cubeConst * curDimensions[1], 0);
            jack.localPosition = new Vector3(cubeConst * (curDimensions[0] - 1 + 2), -cubeConst * curDimensions[1], 0);

            trigJackOutList.Add(jack);
            seqList.Add(jack.GetComponent<sequencer>());

            // cvJackOutPrefab for cvs
            Transform cvJack = (Instantiate(cvJackOutPrefab, Vector3.zero, Quaternion.identity) as GameObject).transform;
            cvJack.parent = transform;
            cvJack.localRotation = Quaternion.Euler(0, 0, -90);
            cvJack.localScale = Vector3.one;
            //cvJack.localPosition = new Vector3(-cubeConst / 2f - .001f - cubeConst * (curDimensions[0] - 1), -cubeConst * curDimensions[1], 0);
            cvJack.localPosition = new Vector3(cubeConst * (curDimensions[0] - 1 + 2), -cubeConst * curDimensions[1], 0);

            cvJackOutList.Add(cvJack);
            seqCVList.Add(cvJack.GetComponent<sequencerCV>());

            // controlPrefab
            Transform ctrl = (Instantiate(controlPrefab, Vector3.zero, Quaternion.identity) as GameObject).transform;
            ctrl.parent = transform;
            ctrl.localRotation = Quaternion.Euler(0, 0, -90);
            ctrl.localScale = Vector3.one;
            //cvJack.localPosition = new Vector3(-cubeConst / 2f - .001f - cubeConst * (curDimensions[0] - 1), -cubeConst * curDimensions[1], 0);
            ctrl.localPosition = new Vector3(cubeConst * (curDimensions[0] - 1), -cubeConst * curDimensions[1], 0);

            controlList.Add(ctrl);
            controlMuteList.Add(ctrl.GetComponent<button>());
            controlModeList.Add(ctrl.GetComponent<basicSwitch>());



            //clipPlayerSimple samp = (Instantiate(samplerPrefab, Vector3.zero, Quaternion.identity, transform) as GameObject).GetComponent<clipPlayerSimple>();
            //samp.transform.localRotation = Quaternion.identity;
            //samp.transform.localScale = Vector3.one;
            //samp.transform.localPosition = new Vector3(.081f, -cubeConst * curDimensions[1], -.028f);
            //samp.seqGen = jack.GetComponent<sequencer>();

            //samp.gameObject.GetComponent<samplerLoad>().SetSample(tapeList[curDimensions[1]][0], tapeList[curDimensions[1]][1]);
            //samp.gameObject.GetComponent<miniSamplerComponentInterface>().muteButton.startToggled = rowMute[curDimensions[1]];

            //samplerList.Add(samp);
            curDimensions[1]++;
        }

        updateStepSelectVertical();
    }

    void setupStepPrefabRow(GameObject prefab, int i2){
        Transform t = (Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject).transform;
        t.parent = transform;
        t.localRotation = Quaternion.identity;
        float yMult = curDimensions[1];
        float xMult = i2;
        t.localPosition = new Vector3(-cubeConst * xMult, -cubeConst * yMult, 0);

        t.localScale = Vector3.one;
        cubeList.Last().Add(t);
        cubeDials.Last().Add(t.GetComponentInChildren<dial>());

        float Hval = (float)curDimensions[1] / max;

        t.GetComponent<button>()?.Setup(i2, curDimensions[1], cubeBools[i2][curDimensions[1]], Color.HSVToRGB(Hval, .9f, .05f));
        t.GetComponentInChildren<dial>()?.setPercent(cubeFloats[i2][curDimensions[1]]);
    }

    //public void LoadSamplerInfo()
    //{
    //    for (int i = 0; i < curDimensions[1]; i++)
    //    {
    //        samplerList[i].gameObject.GetComponent<samplerLoad>().SetSample(tapeList[curDimensions[i]][0], tapeList[curDimensions[i]][1]);
    //        samplerList[i].gameObject.GetComponent<miniSamplerComponentInterface>().muteButton.keyHit(rowMute[curDimensions[i]]);
    //    }
    //}

    void updateStepSelectVertical()
    {
        Vector3 sPos = stepSelect.transform.localPosition;
        sPos.y = -cubeConst * (curDimensions[1]);
        stepSelect.transform.localPosition = sPos;
    }

    void removeRows(int c)
    {
        for (int i = 0; i < c; i++)
        {

            int z = cubeList.Count - 1;
            cubeDials.RemoveAt(z);
            for (int i2 = 0; i2 < cubeList[z].Count; i2++)
            {
                Transform t = cubeList[z][i2];
                Destroy(t.gameObject);
            }
            cubeList.RemoveAt(z);


            Transform j = trigJackOutList.Last();
            Destroy(j.gameObject);
            trigJackOutList.RemoveAt(trigJackOutList.Count - 1);
            seqList.RemoveAt(trigJackOutList.Count);

            j = cvJackOutList.Last();
            Destroy(j.gameObject);
            cvJackOutList.RemoveAt(cvJackOutList.Count - 1);
            seqCVList.RemoveAt(cvJackOutList.Count);

            j = controlList.Last();
            Destroy(j.gameObject);
            controlModeList.RemoveAt(controlModeList.Count - 1);
            controlMuteList.RemoveAt(controlModeList.Count);

            //clipPlayerSimple clipTemp = samplerList.Last();
            //int tempIndex = samplerList.Count - 1;
            //clipTemp.gameObject.GetComponent<samplerLoad>().getTapeInfo(out tapeList[tempIndex][0], out tapeList[tempIndex][1]);
            //rowMute[tempIndex] = clipTemp.gameObject.GetComponent<miniSamplerComponentInterface>().muteButton.isHit;

            //Destroy(clipTemp.gameObject);
            //samplerList.RemoveAt(tempIndex);

            curDimensions[1]--;
        }

        updateStepSelectVertical();
    }

    #endregion

    #region saveload

    //public void saveRowSampler(int r)
    //{
    //    samplerList[r].gameObject.GetComponent<samplerLoad>().getTapeInfo(out tapeList[r][0], out tapeList[r][1]);
    //    rowMute[r] = samplerList[r].gameObject.GetComponent<miniSamplerComponentInterface>().muteButton.isHit;
    //}

    public override InstrumentData GetData()
    {
        // TODO implement serialization for knobs, etc
        SequencerCVData data = new SequencerCVData();
        data.deviceType = DeviceType.SequencerCV;
        GetTransformData(data);
        data.sliderSpeed = beatSlider.switchVal;

        data.switchPlay = playButton.isHit;
        data.jackTriggerInID = playTriggerInputJack.transform.GetInstanceID();

        data.dimensions = dimensions;
        data.cubeButtons = cubeBools;
        data.cubeDials = cubeFloats;

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

        //for (int i = 0; i < dimensions[1]; i++)
        //{
        //    saveRowSampler(i);
        //}

        //data.rowSamples = tapeList;
        data.buttonMutes = rowMute;
        data.dialSwing = swingDial.percent;

        //data.jackSampleOutIDs = new int[samplerList.Count];
        //for (int i = 0; i < samplerList.Count; i++)
        //{
        //    data.jackSampleOutIDs[i] = samplerList[i].gameObject.GetComponent<miniSamplerComponentInterface>().jackSampleOut.transform.GetInstanceID();
        //}

        //data.dialsPitch = new float[samplerList.Count];
        //for (int i = 0; i < samplerList.Count; i++){
        //    data.dialsPitch[i] = samplerList[i].gameObject.GetComponent<miniSamplerComponentInterface>().dialPitch.percent;
        //}

        //data.dialsAmp = new float[samplerList.Count];
        //for (int i = 0; i < samplerList.Count; i++)
        //{
        //    data.dialsAmp[i] = samplerList[i].gameObject.GetComponent<miniSamplerComponentInterface>().dialAmp.percent;
        //}

        //data.jackPitch = new int[samplerList.Count];
        //for (int i = 0; i < samplerList.Count; i++)
        //{
        //  data.jackPitch[i] = samplerList[i].gameObject.GetComponent<miniSamplerComponentInterface>().jackPitch.transform.GetInstanceID();
        //}

        //data.jackAmp = new int[samplerList.Count];
        //for (int i = 0; i < samplerList.Count; i++)
        //{
        //  data.jackAmp[i] = samplerList[i].gameObject.GetComponent<miniSamplerComponentInterface>().jackAmp.transform.GetInstanceID();
        //}

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

        for (int i = 0; i < data.cubeButtons.Length; i++)
        {
            for (int i2 = 0; i2 < data.cubeButtons[i].Length; i2++)
            {
                cubeBools[i][i2] = data.cubeButtons[i][i2];
                cubeFloats[i][i2] = data.cubeDials[i][i2];
            }
        }

        for (int i = 0; i < data.dimensions[0]; i++)
        {
            for (int i2 = 0; i2 < data.dimensions[1]; i2++)
            {
                if (data.cubeButtons[i][i2])
                {
                    cubeList[i2][i].GetComponent<button>().keyHit(true);                    
                }
                cubeList[i2][i].GetComponentInChildren<dial>().setPercent(data.cubeDials[i][i2]);
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

        //for (int i = 0; i < samplerList.Count; i++)
        //{
        //    samplerList[i].gameObject.GetComponent<miniSamplerComponentInterface>().jackSampleOut.ID = data.jackSampleOutIDs[i];
        //}

        //for (int i = 0; i < samplerList.Count; i++)
        //{
        //    samplerList[i].gameObject.GetComponent<miniSamplerComponentInterface>().dialPitch.setPercent(data.dialsPitch[i]);
        //}

        //for (int i = 0; i < samplerList.Count; i++)
        //{
        //    samplerList[i].gameObject.GetComponent<miniSamplerComponentInterface>().dialAmp.setPercent(data.dialsAmp[i]);
        //}

        //for (int i = 0; i < samplerList.Count; i++)
        //{
        //    samplerList[i].gameObject.GetComponent<miniSamplerComponentInterface>().jackPitch.ID = data.jackPitch[i];
        //}

        //for (int i = 0; i < samplerList.Count; i++)
        //{
        //    samplerList[i].gameObject.GetComponent<miniSamplerComponentInterface>().jackAmp.ID = data.jackAmp[i];
        //}

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
  
  //public string[][] rowSamples;
  //public int[] jackSampleOutIDs;
  public bool[] buttonMutes;
  public int[] dimensions;

  public bool[][] cubeButtons;
  public float[][] cubeDials;

  //public float[] dialsPitch;
  //public float[] dialsAmp;
  //public int[] jackPitch;
  //public int[] jackAmp;

  public bool switchRange;

}