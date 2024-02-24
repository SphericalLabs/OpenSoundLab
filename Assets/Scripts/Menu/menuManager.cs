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
using UnityEngine.Analytics;
using System;
using System.Linq;

public class menuManager : MonoBehaviour {
  public GameObject item;
  public GameObject rootNode;
  public GameObject trashNode;
  public GameObject settingsNode;
  public GameObject metronomeNode;
  public GameObject performanceNode;

  public List<GameObject> menuItems;

  public Dictionary<DeviceType, GameObject> refObjects;

  public AudioSource _audioSource;
  public AudioClip openClip;
  public AudioClip closeClip;
  public AudioClip selectClip;
  public AudioClip grabClip;
  public AudioClip simpleOpenClip;

  List<menuItem> menuItemScripts;
  public static menuManager instance;

  bool active = false;
  int lastController = -1;

  public bool loaded = false;

  void Awake() {
    instance = this;
    refObjects = new Dictionary<DeviceType, GameObject>();
    _audioSource = GetComponent<AudioSource>();
    loadMenu();
    loadNonMenuItems();
    loaded = true;
    Activate(false, transform);

    if (!PlayerPrefs.HasKey("midiOut")) PlayerPrefs.SetInt("midiOut", 0);
    if (PlayerPrefs.GetInt("midiOut") == 1) {
      toggleMidiOut(true);
    }
  }

  void loadNonMenuItems() {
    GameObject temp = Instantiate(item, Vector3.zero, Quaternion.identity) as GameObject;
    temp.transform.parent = rootNode.transform;
    menuItem m = temp.GetComponent<menuItem>();
    refObjects[DeviceType.TapeGroup] = m.Setup(DeviceType.TapeGroup);
    temp.SetActive(false);

    //temp = Instantiate(item, Vector3.zero, Quaternion.identity) as GameObject;
    //temp.transform.parent = rootNode.transform;
    //m = temp.GetComponent<menuItem>();
    //refObjects[deviceType.Pano] = m.Setup(deviceType.Pano);
    //temp.SetActive(false);
  }

  public void SetMenuActive(bool on) {
    active = on;
  }

  

  // this populates the menu on program start
  void loadMenu() { 
    menuItems = new List<GameObject>();
    menuItemScripts = new List<menuItem>();

    int hElements = 8;
    float arc = -8.4f * hElements;
    float arcSegment = arc / hElements;

    float y;
    int x = 0;
    int tempCount = 0;

    IEnumerable<DeviceCategory> categories = Enum.GetValues(typeof(DeviceCategory)).OfType<DeviceCategory>();
    foreach (DeviceCategory category in categories)
    {

      y = 0;

      foreach (DeviceType devType in DeviceType.GetAllByCategory(category))
      {
        // skip incompatible devices
        if (Application.platform == RuntimePlatform.Android)
        {
          if (devType == DeviceType.Camera) continue;
        }

        if (devType == DeviceType.Sequencer) continue;
        if (devType == DeviceType.MIDIIN) continue;
        if (devType == DeviceType.MIDIOUT) continue;
        if (devType == DeviceType.Airhorn) continue;

        if (devType == DeviceType.Maracas) continue;
        if (devType == DeviceType.Timeline) continue;
        if (devType == DeviceType.Reverb) continue;

        // MultiMix and MultiSplit hack, want to have Multiple available for loading, but not in the menu palette
        if (devType == DeviceType.Multiple)
        {
          GameObject tmpObj2 = Instantiate(item, Vector3.zero, Quaternion.identity) as GameObject;
          tmpObj2.transform.parent = rootNode.transform;
          //menuItems.Add(tmpObj2);
          menuItem m2 = tmpObj2.GetComponent<menuItem>();
          refObjects[devType] = m2.Setup(devType);
          //menuItemScripts.Add(m); 
          tmpObj2.SetActive(false);
          continue;
        }


        if (devType == DeviceType.Camera) continue; // skip for windows, too, throws error otherwise
        if (devType == DeviceType.Pano) continue;
        if (devType == DeviceType.TapeGroup) continue;

        GameObject tmpObj = Instantiate(item, Vector3.zero, Quaternion.identity) as GameObject;
        tmpObj.transform.parent = rootNode.transform;
        menuItems.Add(tmpObj);
        menuItem m = tmpObj.GetComponent<menuItem>();
        refObjects[devType] = m.Setup(devType);
        menuItemScripts.Add(m);



        float angle = arcSegment * (x - hElements / 2) + arcSegment / 2f;
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        Vector3 positionOffset = rotation * Vector3.forward * -0.5f - Vector3.forward * -0.5f;
        menuItems[tempCount].transform.localPosition = positionOffset + Vector3.up * y * 0.07f;
        menuItems[tempCount].transform.rotation = rotation;

        tempCount++;
        y++;

      }

      x++;

    }


    metronomeNode.transform.localPosition = new Vector3(0.345f, 0.012f + 0.10f, 0.107f);
    metronomeNode.transform.rotation = Quaternion.Euler(-0.529f, -40.157f, -0.7460001f);   
    
    performanceNode.transform.localPosition = new Vector3(0.329f, 0.012f - 0.12f + 0.10f, 0.107f);
    performanceNode.transform.rotation = Quaternion.Euler(-0.529f, -40.157f, -0.7460001f);

    settingsNode.transform.localPosition = new Vector3(-0.344f, -0.001f, 0.171f);
    settingsNode.transform.rotation = Quaternion.Euler(-0.422f, 40.013f, 0.576f);

  }

  public void SelectAudio() {
    //_audioSource.PlayOneShot(selectClip, .05f);
  }

  public void GrabAudio() {
    //_audioSource.PlayOneShot(grabClip, .75f);
  }

  public bool midiOutEnabled = false;
  float openSpeed = 3;
  public void toggleMidiOut(bool on) {
    PlayerPrefs.SetInt("midiOut", on ? 1 : 0);
    midiOutEnabled = on;
    openSpeed = on ? 2 : 3;
    menuItemScripts[menuItemScripts.Count - 1].Appear(on);
  }

  Coroutine activationCoroutine;
  IEnumerator activationRoutine(bool on, Transform pad) {
    
    if(on){
      rootNode.SetActive(true);
      trashNode.SetActive(true);
      settingsNode.SetActive(true);
      metronomeNode.SetActive(true);
      for (int i = 0; i < menuItems.Count; i++)
      {
        menuItemScripts[i].Appear(on);
        //menuItemScripts[i].transform.localScale = Vector3.one;
      }
      transform.position = pad.Find("manipCollViz").position;
      transform.Translate(Vector3.left * -0.03f); // somehow this is only applied from the second menu spawn on

      faceCenterEye();

    }
    else {
      trashNode.SetActive(false);
      settingsNode.SetActive(false);
      metronomeNode.SetActive(false);
      for (int i = 0; i < menuItems.Count; i++)
      {
        menuItemScripts[i].Appear(on);
      }
      rootNode.SetActive(false);
    }
    yield return null;
  }

  GameObject centerEyeAnchor;
  Vector3 centerEye, lookDirection;

  void faceCenterEye(){
    if (centerEyeAnchor == null) centerEyeAnchor = GameObject.Find("CenterEyeAnchor");
    
    centerEye = centerEyeAnchor.transform.position;
    centerEye.y -= 0.1f;

    //transform.LookAt(centerEye, Vector3.up);

    // needed Quaternion to avoid gimbal locks when spawning above or below
    lookDirection = centerEye - transform.position;
    if (lookDirection != Vector3.zero)
    {
      transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
    }
  }

  void Activate(bool on, Transform pad) {
    active = on;
    if (activationCoroutine != null) StopCoroutine(activationCoroutine);
    activationCoroutine = StartCoroutine(activationRoutine(on, pad));
  }

  void SimpleActivate(bool on, Transform pad) {
    active = on;
    simpleMenu.toggleMenu();

    //if (on) _audioSource.PlayOneShot(simpleOpenClip);
    //else _audioSource.PlayOneShot(closeClip);
    if (!active) return;

    transform.position = pad.position;
    
    faceCenterEye();
  }

  public bool simple = false;
  public pauseMenu simpleMenu;

  public bool buttonEvent(int controller, Transform pad) {
    bool on = true;

    if (controller != lastController) { // this logic switches between the two controllers
      if (!simple) Activate(true, pad);
      else SimpleActivate(true, pad);
    } else {
      if (!simple) Activate(!active, pad);
      else SimpleActivate(!active, pad);
      on = active;
    }

    lastController = controller;
    return on;
  }
}
