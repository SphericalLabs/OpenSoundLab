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

public class looperDeviceInterface : deviceInterface
{

    public omniJack input, output, recordTrigger, playTrigger;
    public sliderNotched durSlider;
    waveTranscribeLooper transcriber;
    public button[] buttons;
    public basicSwitch recordCvModeSwitch;

    public TextMesh countdownText;

    float[] durations = new float[] { 32, 16, 8, 4, 1 };

    public int curSliderVal = 0;
    public double period = .0625;

    beatTracker _beatManager;
    AudioSource audioSource;

    public override void Awake()
    {
        base.Awake();
        transcriber = GetComponent<waveTranscribeLooper>();
        audioSource = GetComponent<AudioSource>();
        durSlider = GetComponentInChildren<sliderNotched>();
        _beatManager = ScriptableObject.CreateInstance<beatTracker>();
    }

    void Start()
    {
        _beatManager.setTriggers(onBeatEvent, onResetEvent);
        _beatManager.updateBeatNoTriplets(2);
    }

    void OnDestroy()
    {
        Destroy(_beatManager);
    }

    public bool playClick = false;
    public bool RecordCvIsGate()
    {
        return recordCvModeSwitch == null || !recordCvModeSwitch.switchVal;
    }

    public void HandleCvTriggerPulse()
    {
        if (recordCountdown || transcriber.recording) return;
        RecordCountdown();
    }

    void Update()
    {
        if (input.signal != transcriber.incoming) transcriber.incoming = input.signal;

        if (playClick)
        {
            audioSource.Play();
            playClick = false;
        }
        if (curSliderVal != durSlider.switchVal || period != masterControl.instance.measurePeriod)
        {

            period = masterControl.instance.measurePeriod;
            curSliderVal = durSlider.switchVal;
            transcriber.updateDuration(durations[durSlider.switchVal], period);
        }

        countdownText.gameObject.SetActive(recordCountdown || playCountdown);
        if (recordCountdown || playCountdown)
        {
            countdownText.transform.localRotation = Quaternion.Euler(0, 180, countdownText.transform.parent.localRotation.eulerAngles.z);
            if (recordCountdown) countdownText.text = recCountdownRemaining.ToString();
            else countdownText.text = "";
        }
    }

    public void RecordCountdown()
    {
        recordCountdown = true;
        if (!transcriber.playing)
        {
            if (playCountdown) recCountdownRemaining = playCountdownRemaining;
            else recCountdownRemaining = 4;
        }
        else
        {
            transcriber.requestRecord(true);
        }
    }

    public bool recordCountdown = false;

    public int recCountdownRemaining = 0;

    int playCountdownRemaining = 0;
    public bool playCountdown = false;

    void PlayCountdown()
    {
        if (recordCountdown) playCountdownRemaining = recCountdownRemaining;
        else playCountdownRemaining = 1;
        playCountdown = true;
    }

    void onBeatEvent()
    {
        if (recordCountdown && !transcriber.playing)
        {
            transcriber.requestRecord(false);
            recCountdownRemaining--;
            playClick = true;
            if (recCountdownRemaining == 0)
            {
                recordCountdown = false;
                transcriber.Record();
                buttons[1].phantomHit(true);
            }
        }

        if (playCountdown)
        {
            playCountdownRemaining--;
            if (playCountdownRemaining == 0)
            {
                playCountdown = false;
                transcriber.Back();
                transcriber.playing = true;
            }
        }
    }

    void onResetEvent()
    {
        transcriber.Back();
    }

    void StartRecord(bool on)
    {
        if (on) buttons[1].keyHit(true);
        transcriber.recording = false;
    }

    public override void hit(bool on, int ID = -1)
    {
        if (ID == 3 && on) transcriber.Save();
        if (ID == 4 && on) transcriber.Flush();
        if (ID == 5) transcriber.cueLive = on;
        if (ID == 6) transcriber.overwrite = on;

        if (ID == 0)
        {
            if (on)
            {
                RecordCountdown();

            }
            else
            {
                transcriber.recording = false;
                recordCountdown = false;
                transcriber.requestRecord(false);
            }

        }
        if (ID == 1)
        {
            if (on) PlayCountdown();
            else
            {
                playCountdown = false;
                buttons[0].keyHit(false, false);
                transcriber.Back();
                transcriber.playing = false;
            }
        }
    }

    public override InstrumentData GetData()
    {
        LooperData data = new LooperData();
        data.deviceType = DeviceType.Looper;
        GetTransformData(data);

        data.jackInID = input.transform.GetInstanceID();
        data.jackOutID = output.transform.GetInstanceID();
        data.recordTriggerID = recordTrigger.transform.GetInstanceID();
        data.playTriggerID = playTrigger.transform.GetInstanceID();
        data.dur = durSlider.switchVal;
        data.cueLive = buttons[4].isHit;
        data.overwrite = buttons[5].isHit;
        data.recordCvTriggerMode = recordCvModeSwitch != null && recordCvModeSwitch.switchVal;
        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        LooperData data = d as LooperData;
        base.Load(data, true);
        input.SetID(data.jackInID, copyMode);
        output.SetID(data.jackOutID, copyMode);
        recordTrigger.SetID(data.recordTriggerID, copyMode);
        playTrigger.SetID(data.playTriggerID, copyMode);
        durSlider.setVal(data.dur);
        buttons[4].setOnAtStart(data.cueLive);
        buttons[5].setOnAtStart(data.overwrite);
        if (recordCvModeSwitch != null) recordCvModeSwitch.setSwitch(data.recordCvTriggerMode, true);
    }
}
public class LooperData : InstrumentData
{
    public int jackOutID;
    public int jackInID;
    public int recordTriggerID;
    public int playTriggerID;
    public int dur;
    public bool overwrite;
    public bool cueLive;
    public bool recordCvTriggerMode;
}
