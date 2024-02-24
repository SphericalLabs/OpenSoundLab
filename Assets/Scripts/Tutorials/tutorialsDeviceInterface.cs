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
using UnityEngine.Video;



public class tutorialsDeviceInterface : deviceInterface
{

  public button playButton, resetButton, jumpBackButton, jumpForthButton, scrollUpButton, scrollDownButton, showOnStartup;
  public GameObject videoContainer;
  public tutorialPanel panelPrefab;
  public Transform tutorialHolder;

  public tutorialPanel selectedTutorial;
  public tutorialPanel[] tutorials;
  public TutorialRecord[] tutorialRecords;

  public VideoPlayer videoPlayer;

  public Transform progressTransform;
  private Vector3 maxProgressScale;
  //private float minProgressScale;

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


    triggerOpenTutorial(tutorials[0], true);

  }
  Vector3 scaleVector = new Vector3(0f, 1f, 1f);
  float minScale = 0f;
  void Update()
  {
    if (videoPlayer.isPrepared) {
      scaleVector.x = Utils.map((float)videoPlayer.frame, 0f, (float)videoPlayer.frameCount, minScale, 1f);

    } else {
      scaleVector.x = minScale;
    }
    progressTransform.localScale = Vector3.Scale(maxProgressScale, scaleVector);
  }


  public override void hit(bool on, int ID = -1)
  {
    switch (ID)
    {
      case 1:
        // rewind
        if (!on) return; // only on enter events
        videoPlayer.frame = 0; 
        break;
      case 2:
        // -10s
        if (!on) return; // only on enter events
        if (videoPlayer.frame - 10 * videoPlayer.frameRate >= 0)
        {
          videoPlayer.frame = Mathf.RoundToInt(videoPlayer.frame - 10 * videoPlayer.frameRate);
        }
        else
        {
          videoPlayer.frame = 0;
        }
        break;
      case 3:
        // play
        playPauseVideo(); // enter and release events
        break;
      case 4:
        // +10s
        if (!on) return; // only on enter events
        if (videoPlayer.frame + 10 * videoPlayer.frameRate >= videoPlayer.frameCount)
        {
          videoPlayer.frame = 0;
          videoPlayer.Pause();
          playButton.isHit = false; 
        }
        else
        {
          videoPlayer.frame = (int)(videoPlayer.frame + 10 * videoPlayer.frameRate);
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
    playButton.keyHit(false);
    StartCoroutine(openTutorial(tut, startPaused));
  }

  IEnumerator openTutorial(tutorialPanel tut, bool startPaused = false)
  {

    selectedTutorial = tut;

    // disable all other tutorials
    for (int i = 0; i < tutorials.Length; i++)
    {
      if (tutorials[i] != selectedTutorial)
        tutorials[i].setActivated(false);
    }

    selectedTutorial.setActivated(true, false); // do not notify manager, otherwise stackoverflow

    videoPlayer.url = Application.streamingAssetsPath + "/" + selectedTutorial.videoString;
    videoPlayer.Prepare();

    while (!videoPlayer.isPrepared)
    {
      yield return new WaitForSeconds(.1f);
    }
    videoPlayer.skipOnDrop = false;
    videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
    videoPlayer.targetMaterialRenderer = videoPlayer.GetComponentInChildren<Renderer>();
    videoPlayer.targetMaterialProperty = "_MainTex";
    if(!startPaused) forcePlay();
  }

  public void playPauseVideo()
  {
    if (playButton.isHit)
    {
      videoPlayer.Play();
    }
    else
    {
      videoPlayer.Pause();
    }

    //if(videoPlayer.isPlaying){
    //  videoPlayer.Pause();
    //} else if (videoPlayer.isPaused) {
    //  videoPlayer.Play();
    //}
  }

  public void forcePlay(){
    videoPlayer.frame = 0;
    videoPlayer.Play();
    //playButton.isHit = true;
    playButton.keyHit(true);
  }

  public override InstrumentData GetData()
  {
    TutorialsData data = new TutorialsData();
    data.deviceType = DeviceType.Tutorials;
    GetTransformData(data);
    return data;
  }

  public override void Load(InstrumentData d)
  {
    TutorialsData data = d as TutorialsData;
    base.Load(data);
  }

  }

public class TutorialsData : InstrumentData
{

}
