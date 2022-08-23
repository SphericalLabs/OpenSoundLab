// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of OpenSoundLab.
//
// OpenSoundLab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenSoundLab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenSoundLab.  If not, see <http://www.gnu.org/licenses/>.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class menuManager : MonoBehaviour {
  public GameObject item;
  public GameObject rootNode;
  public GameObject trashNode;
  public GameObject settingsNode;
  public GameObject metronomeNode;

  public List<GameObject> menuItems;

  public Dictionary<menuItem.deviceType, GameObject> refObjects;

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
    refObjects = new Dictionary<menuItem.deviceType, GameObject>();
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
    refObjects[menuItem.deviceType.TapeGroup] = m.Setup(menuItem.deviceType.TapeGroup);
    temp.SetActive(false);

    temp = Instantiate(item, Vector3.zero, Quaternion.identity) as GameObject;
    temp.transform.parent = rootNode.transform;
    m = temp.GetComponent<menuItem>();
    refObjects[menuItem.deviceType.Pano] = m.Setup(menuItem.deviceType.Pano);
    temp.SetActive(false);
  }

  public void SetMenuActive(bool on) {
    active = on;
  }

  int rowLength = 8;

  // this populates the menu on program start
  void loadMenu() { 
    menuItems = new List<GameObject>();
    menuItemScripts = new List<menuItem>();
    for (int i = (int)menuItem.deviceType.Max - 1; i >= 0; i--) {
      // skip incompatible devices
      if (Application.platform == RuntimePlatform.Android)
      {
        if ((menuItem.deviceType)i == menuItem.deviceType.Camera) continue;
      }
      
      if ((menuItem.deviceType)i == menuItem.deviceType.Sequencer) continue; 
      if ((menuItem.deviceType)i == menuItem.deviceType.MIDIIN) continue;
      if ((menuItem.deviceType)i == menuItem.deviceType.MIDIOUT) continue;
      if ((menuItem.deviceType)i == menuItem.deviceType.Airhorn) continue;
      if ((menuItem.deviceType)i == menuItem.deviceType.Stereo) continue; // remove completely?
      //if ((menuItem.deviceType)i == menuItem.deviceType.ADSR) continue;
      if ((menuItem.deviceType)i == menuItem.deviceType.Maracas) continue;
      //if ((menuItem.deviceType)i == menuItem.deviceType.Drum) continue;
      if ((menuItem.deviceType)i == menuItem.deviceType.Timeline) continue;      
      if ((menuItem.deviceType)i == menuItem.deviceType.Funktion) continue;
      if ((menuItem.deviceType)i == menuItem.deviceType.SARSCov2) continue;
      //if ((menuItem.deviceType)i == menuItem.deviceType.Looper) continue;
      if ((menuItem.deviceType)i == menuItem.deviceType.Reverb) continue;

      // MultiMix and MultiSplit hack, want to have Multiple available for loading, but not in the menu palette
      if ((menuItem.deviceType)i == menuItem.deviceType.Multiple) 
      {
        GameObject tmpObj2 = Instantiate(item, Vector3.zero, Quaternion.identity) as GameObject;
        tmpObj2.transform.parent = rootNode.transform;
        //menuItems.Add(tmpObj2);
        menuItem m2 = tmpObj2.GetComponent<menuItem>();
        refObjects[(menuItem.deviceType)i] = m2.Setup((menuItem.deviceType)i);
        //menuItemScripts.Add(m); 
        tmpObj2.SetActive(false);
        continue; 
      }


      if ((menuItem.deviceType)i == menuItem.deviceType.Camera) continue; // skip for windows, too, throws error otherwise

      GameObject tmpObj = Instantiate(item, Vector3.zero, Quaternion.identity) as GameObject;
      tmpObj.transform.parent = rootNode.transform;
      menuItems.Add(tmpObj);
      menuItem m = tmpObj.GetComponent<menuItem>();
      refObjects[(menuItem.deviceType)i] = m.Setup((menuItem.deviceType)i);
      menuItemScripts.Add(m);
    }

    int tempCount = 0;
    float h = 0;
    float arc = -37.5f * rowLength / 5; // depending on rowLength?

    //Debug.Log(menuItems.Length);
    while (tempCount < menuItems.Count) {
      for (int i = 0; i < rowLength; i++) {
        if (tempCount < menuItems.Count) {
          menuItems[tempCount].transform.localPosition = Quaternion.Euler(0, (arc / rowLength) * (i - rowLength / 2f) + (arc / rowLength) / 2f, 0) * (Vector3.forward * -.5f) - (Vector3.forward * -.5f) + Vector3.up * h;
          menuItems[tempCount].transform.rotation = Quaternion.Euler(0, (arc / rowLength) * (i - rowLength / 2f) + (arc / rowLength) / 2f, 0);
        }
        tempCount++;
      }
      h += 0.07f;
    }

    //metronomeNode.transform.localPosition = Quaternion.Euler(0, -arc / 2 - 10, 0) * (Vector3.forward * -.5f) - (Vector3.forward * -.5f) + Vector3.up * .014f;
    //metronomeNode.transform.rotation = Quaternion.Euler(0, -arc / 2 - 10, 0);
    //settingsNode.transform.localPosition = Quaternion.Euler(0, arc / 2 + 10, 0) * (Vector3.forward * -.5f) - (Vector3.forward * -.5f);
    //settingsNode.transform.rotation = Quaternion.Euler(0, arc / 2 + 10, 0);

    metronomeNode.transform.localPosition = new Vector3(0.329f, 0.012f, 0.107f);
    metronomeNode.transform.rotation = Quaternion.Euler(-0.529f, -40.157f, -0.7460001f);

    settingsNode.transform.localPosition = new Vector3(-0.341f, 0.023f, 0.125f);
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
      Vector3 camPos = Camera.main.transform.position;
      camPos.y -= .2f;
      camPos.x += 0.04f; // half eye distance, so that it centers of middle of both eyes
      transform.LookAt(camPos, Vector3.up);
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
    Vector3 camPos = Camera.main.transform.position;
    camPos.y = transform.position.y;
    transform.LookAt(camPos);
  }

  public bool simple = false;
  public pauseMenu simpleMenu;

  public bool buttonEvent(int controller, Transform pad) {
    bool on = true;

    if (controller != lastController) {
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
