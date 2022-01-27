// Copyright 2017 Google LLC
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



public class tutorialsDeviceInterface : deviceInterface {

  public button playButton, resetButton, jumpBackButton, jumpForthButton, scrollUpButton, scrollDownButton;
  public GameObject videoContainer;
  public tutorialPanel panelPrefab;
  public Transform tutorialHolder;

  public tutorialPanel selectedTutorial;
  public tutorialPanel[] tutorials;
  public TutorialRecord[] tutorialRecords;
  
  public VideoPlayer videoPlayer;

  [System.Serializable] 
  public class TutorialRecord
  {
    public string labelString;
    public string videoString;
  }

  public override void Awake() {
    base.Awake();
    tutorials = new tutorialPanel[tutorialRecords.Length];
    for(int i = 0; i < tutorialRecords.Length; i ++){
      tutorials[i] = Instantiate<tutorialPanel>(panelPrefab, tutorialHolder);
      tutorials[i].transform.localPosition = new Vector3(0, -0.0321f * i, 0);
      tutorials[i].transform.localRotation = Quaternion.Euler(0, 90, 90);
      tutorials[i].setLabel(tutorialRecords[i].labelString);
      tutorials[i].setVideo(tutorialRecords[i].videoString);
    }
  }


  void Update() {

  }


  public override void hit(bool on, int ID = -1)
  {
    if (ID == 0) /*player.togglePause(on)*/;
    if (ID == 1 && on) /*player.Back()*/;
    if (ID == 2)
    {

    }
  }

  public void triggerTutorial(tutorialPanel tut){
    // disable all tutorials
    for(int i = 0; i < tutorials.Length; i++){
      tutorials[i].setActive(false);
    }
    selectedTutorial = tut;
    selectedTutorial.setActive(true);
    playVideo(selectedTutorial.videoString);
  }

  public void playVideo(string videoString){

  }

}
