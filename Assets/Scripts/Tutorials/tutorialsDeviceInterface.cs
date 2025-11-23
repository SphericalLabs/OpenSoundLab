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
using UnityEngine.Video;
using System;



public class tutorialsDeviceInterface : deviceInterface
{

    public button playButton, resetButton, jumpBackButton, jumpForthButton, scrollUpButton, scrollDownButton, showOnStartup;
    public GameObject videoContainer;
    public tutorialPanel panelPrefab;
    public Transform tutorialHolder;

    public tutorialPanel selectedTutorial;
    public tutorialPanel[] tutorials;
    public TutorialRecord[] tutorialRecords;
    public bool openOnStartup = true;

    public VideoPlayer videoPlayer;

    public Transform progressTransform;
    private Vector3 maxProgressScale;
    //private float minProgressScale;
    private bool clipReady;
    private bool hasPendingSeek;
    private float pendingSeekTime;
    private bool desiredPlayState;

    public event Func<tutorialPanel, bool, bool> OnTriggerOpenTutorial;
    public bool IsReady => clipReady && videoPlayer != null && videoPlayer.isPrepared;

    public int skipSeconds = 15;

    [System.Serializable]
    public class TutorialRecord
    {
        public string labelString;
        public string videoString;
    }

    public override void Awake()
    {
        base.Awake();
    }

    void Start()
    {

        showOnStartup.keyHit(PlayerPrefs.GetInt("showTutorialsOnStartup") == 1);

        maxProgressScale = progressTransform.localScale;

        tutorials = new tutorialPanel[tutorialRecords.Length];
        for (int i = 0; i < tutorialRecords.Length; i++)
        {
            tutorials[i] = Instantiate<tutorialPanel>(panelPrefab, tutorialHolder);
            tutorials[i].transform.localPosition = new Vector3(0, -0.0321f * i, 0);
            tutorials[i].transform.localRotation = Quaternion.Euler(0, 90, 90);
            tutorials[i].setLabel(tutorialRecords[i].labelString);
            tutorials[i].setVideo(tutorialRecords[i].videoString);
        }


        if (openOnStartup && tutorials.Length > 0)
        {
            InternalOpenTutorial(tutorials[0], false);
        }

    }
    Vector3 scaleVector = new Vector3(0f, 1f, 1f);
    float minScale = 0f;
    void Update()
    {
        if (videoPlayer.isPrepared)
        {
            scaleVector.x = Utils.map((float)videoPlayer.frame, 0f, (float)videoPlayer.frameCount, minScale, 1f);

        }
        else
        {
            scaleVector.x = minScale;
        }
        progressTransform.localScale = Vector3.Scale(maxProgressScale, scaleVector);
    }

    public event Action OnPlay;
    public event Action OnPause;
    public event Action<float> OnSeek;

    public override void hit(bool on, int ID = -1)
    {
        switch (ID)
        {
            case 1:
                // rewind
                if (!on) return; // only on enter events
                videoPlayer.frame = 0;
                OnSeek?.Invoke(0f);
                break;
            case 2:
                // -10s
                if (!on) return; // only on enter events
                {
                    long targetFrame = 0;
                    if (videoPlayer.frame - skipSeconds * videoPlayer.frameRate >= 0)
                    {
                        targetFrame = Mathf.RoundToInt(videoPlayer.frame - skipSeconds * videoPlayer.frameRate);
                    }
                    else
                    {
                        targetFrame = 0;
                    }
                    videoPlayer.frame = targetFrame;
                    // Calculate time from frame to avoid stale videoPlayer.time
                    float targetTime = (float)targetFrame / videoPlayer.frameRate;
                    OnSeek?.Invoke(targetTime);
                }
                break;
            case 3:
                // play
                playPauseVideo(); // enter and release events
                break;
            case 4:
                // +10s
                if (!on) return; // only on enter events
                {
                    long targetFrame = 0;
                    if (videoPlayer.frame + skipSeconds * videoPlayer.frameRate >= videoPlayer.frameCount)
                    {
                        targetFrame = 0;
                        videoPlayer.frame = 0;
                        videoPlayer.Pause();
                        playButton.isHit = false;
                        OnPause?.Invoke();
                    }
                    else
                    {
                        targetFrame = (long)(videoPlayer.frame + skipSeconds * videoPlayer.frameRate);
                        videoPlayer.frame = targetFrame;
                        // Calculate time from frame to avoid stale videoPlayer.time
                        float targetTime = (float)targetFrame / videoPlayer.frameRate;
                        OnSeek?.Invoke(targetTime);
                    }
                }
                break;
            case 5:
                // show on startup
                PlayerPrefs.SetInt("showTutorialsOnStartup", on ? 1 : 0);
                PlayerPrefs.Save();
                break;
            default:
                break;
        }

    }
    public void triggerOpenTutorial(tutorialPanel tut, bool startPaused = false)
    {
        // playButton.keyHit(false); // Removed to prevent visual flicker and potential state confusion

        OnTriggerOpenTutorial?.Invoke(tut, startPaused);
    }

    // New method to be called directly without triggering the event
    public void InternalOpenTutorial(tutorialPanel tut, bool startPaused = false)
    {
        clipReady = false;
        hasPendingSeek = false;
        desiredPlayState = !startPaused;
        StartCoroutine(openTutorial(tut, startPaused));
    }

    IEnumerator openTutorial(tutorialPanel tut, bool startPaused = false)
    {

        selectedTutorial = tut;
        clipReady = false;
        hasPendingSeek = false;
        pendingSeekTime = 0f;
        desiredPlayState = !startPaused;

        // disable all other tutorials
        for (int i = 0; i < tutorials.Length; i++)
        {
            if (tutorials[i] != selectedTutorial)
                tutorials[i].setActivated(false);
        }

        selectedTutorial.setActivated(true, false); // do not notify manager, otherwise stackoverflow

        videoPlayer.Stop();
        // videoPlayer.time = 0f; // Removed to prevent Android crash (negative sample time)
        // videoPlayer.frame = 0; // Removed to prevent Android crash
        videoPlayer.url = ""; // Reset URL to ensure Prepare triggers correctly even if URL is same
        videoPlayer.url = Application.streamingAssetsPath + "/" + selectedTutorial.videoString;
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
        {
            yield return new WaitForSeconds(.01f);
        }
        yield return null; // Extra frame to ensure readiness
        videoPlayer.skipOnDrop = true;
        clipReady = true;

        if (hasPendingSeek)
        {
            videoPlayer.time = pendingSeekTime;
            hasPendingSeek = false;
        }
        else
        {
            // Explicitly reset to start if no seek is pending, NOW that we are prepared.
            videoPlayer.time = 0f;
            videoPlayer.frame = 0;
        }

        videoPlayer.playbackSpeed = 1f;

        if (desiredPlayState)
        {
            forcePlay();
        }
        else
        {
            InternalPause();
        }
    }

    public void playPauseVideo()
    {
        if (!clipReady) return;

        if (playButton.isHit)
        {
            videoPlayer.Play();
            OnPlay?.Invoke();
        }
        else
        {
            videoPlayer.Pause();
            OnPause?.Invoke();
        }
    }

    public void forcePlay()
    {
        if (!clipReady)
        {
            desiredPlayState = true;
            return;
        }

        videoPlayer.frame = 0;
        videoPlayer.Play();
        playButton.keyHit(true);
        OnPlay?.Invoke();
    }

    public void InternalPlay()
    {
        if (!clipReady)
        {
            desiredPlayState = true;
            return;
        }

        if (!videoPlayer.isPlaying)
        {
            videoPlayer.Play();
            playButton.keyHit(true);
        }
    }

    public void InternalPause()
    {
        if (!clipReady)
        {
            desiredPlayState = false;
            return;
        }

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            playButton.keyHit(false);
        }
    }

    public void InternalSeek(float time)
    {
        if (!clipReady)
        {
            pendingSeekTime = Mathf.Max(0f, time);
            hasPendingSeek = true;
            return;
        }

        float clampedTime = Mathf.Max(0f, time);
        videoPlayer.time = clampedTime;
    }

    public void InternalSetSpeed(float speed)
    {
        videoPlayer.playbackSpeed = speed;
    }

    public override InstrumentData GetData()
    {
        TutorialsData data = new TutorialsData();
        data.deviceType = DeviceType.Tutorials;
        GetTransformData(data);
        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        TutorialsData data = d as TutorialsData;
        base.Load(data, copyMode);
    }

}

public class TutorialsData : InstrumentData
{

}
