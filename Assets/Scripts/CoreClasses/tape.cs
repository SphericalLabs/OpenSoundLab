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
using System.IO;

public class tape : manipObject {
  
  public Transform masterObj;
  public GameObject loadingPrefab;

  Transform deck;
  Transform tapeTrans;
  Renderer tapeRend;
  public TextMesh labelMesh;
  public string label;
  public string filename;
  public bool fullpath = false;
  Vector3 origPos = Vector3.zero;
  Quaternion origRot = Quaternion.identity;

  public bool previewWhenGrabbed = false;

  public static Vector3 correctOffset = Vector3.forward * 0.12f + Vector3.left * 0.02f; // corrects position when grabbing

  Material mat, highlightMat, highlightGrabbedMat;
  Color tapeColor;
  float tapeHue;

  public override void Awake() { 
    base.Awake();
    gameObject.layer = 13;
    tapeTrans = transform.GetChild(0);
    tapeRend = tapeTrans.GetComponent<Renderer>();
    if (mat == null) mat = tapeRend.sharedMaterial;

    labelMesh = GetComponentInChildren<TextMesh>();
    labelMesh.GetComponent<Renderer>().material.SetColor("_Color", Color.black);
    //tapeHue = Random.value;
    tapeHue = 0.5f;
    tapeColor = Color.HSVToRGB(tapeHue, .5f, 1);
    SetColor(tapeColor);
    
  }

  public void setOrigTrans(Vector3 pos, Quaternion rot) {
    origPos = pos;
    origRot = rot;
  }

  public void getOrigTrans(out Vector3 pos, out Quaternion rot) {
    pos = origPos;
    rot = origRot;
  }

  public void Setup(string s, string f) {
    if (masterObj == null) masterObj = transform.parent;
    label = s;
    filename = f;
    labelMesh.text = s;
    origPos = transform.localPosition;
    origRot = transform.localRotation;
  }

  public bool inDeck() {
    return (deck != null);
  }

  public Color setHue(float h) {
    tapeHue = h;
    tapeColor = Color.HSVToRGB(tapeHue, .5f, 1);
    SetColor(tapeColor);
    highlightMat.SetColor("_TintColor", Color.HSVToRGB(tapeHue, .9f, .3f));
    return tapeColor;
  }


  void SetColor(Color c) {
    mat.SetColor("_Color", c);
    mat.SetColor("_SpecColor", c);
  }


  public override void setGrab(bool on, Transform t)
  {
    base.setGrab(on, t);
  }

  public override void grabUpdate(Transform t)
  {
    if (manipulatorObjScript.wasGazeBased)
    {
      gazeBasedPosRotUpdate();
      return;
    }
  }


  Vector3 initialOffset;
  Quaternion initialRotationOffset;

  bool wasPrecisionGazeGrabbed = false; // at last frame

  void gazeBasedPosRotStart()
  {

    Transform go1 = manipulatorObj.transform;
    Transform go2 = this.transform;

    initialOffset = go2.position - go1.position;
    initialRotationOffset = Quaternion.Inverse(go1.rotation) * go2.rotation;

  }


  void gazeBasedPosRotUpdate()
  {
    
    if (OSLInput.getInstance().isSidePressed(manipulatorObjScript.controllerIndex) == false) // fine by default
    {

      transform.parent = deck;  

      if (!wasPrecisionGazeGrabbed)
      {
        gazeBasedPosRotStart();
      }

      Transform go1 = manipulatorObj.transform;
      Transform go2 = this.transform;

      // Calculate the desired position in world space for go2 based on the changes you want
      Vector3 desiredPosition = go1.position + initialOffset;

      // Apply changes to the local position of go2 based on the desired position
      go2.position = desiredPosition;

      //// Calculate the desired rotation for go2 relative to go1
      Quaternion desiredRotation = go1.rotation * initialRotationOffset;

      //// Apply changes to the local rotation of go2 relative to go1
      go2.localRotation = Quaternion.Inverse(go1.localRotation) * desiredRotation;

      wasPrecisionGazeGrabbed = true;
    }
    else // coarse
    {
      transform.parent = manipulatorObj.parent;
      wasPrecisionGazeGrabbed = false;
    }

  }

  manipulator tempM;
  void OnCollisionEnter(Collision coll) {
    if (curState != manipState.grabbed) return;
    if (coll.transform.name != "tapeInsert") return;
    if (manipulatorObjScript != null) {
      manipulatorObjScript.hapticPulse(1000);
      tempM = manipulatorObjScript;
    }

    if (deck != null && deck != coll.transform) //if still in another deck
    {
      deck.parent.GetComponent<samplerLoad>().UnloadTape(this);
    }

    deck = coll.transform;

    tapeTrans.position = deck.transform.position;
    tapeTrans.rotation = deck.transform.rotation;
    tapeTrans.parent = deck;
    tapeTrans.Rotate(-90, 0, 0);

  }

  void OnCollisionExit(Collision coll) {
    if (curState != manipState.grabbed) return;
    if (deck == coll.transform) {
      deck.parent.GetComponent<samplerLoad>().UnloadTape(this);
      if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(500);
      deck = null;
      tapeTrans.parent = transform;
      tapeTrans.localPosition = Vector3.zero;
      tapeTrans.localRotation = Quaternion.identity;
    }
  }

  Coroutine insertCoroutine;
  IEnumerator insertRoutine() {
    float timer = 0;
    Vector3 dest = new Vector3(0, 0, -.0225f);
    while (timer < 1) {
      if (tempM != null && timer < 0.5f) tempM.hapticPulse(500);
      timer = Mathf.Clamp01(timer + Time.deltaTime * 2);
      transform.localPosition = Vector3.Lerp(Vector3.zero, dest, timer);
      yield return null;
    }
  }

  void flashEmptySamplers(bool on) {
    if (!on) {
      tape[] tempTapes = FindObjectsOfType<tape>();
      for (int i = 0; i < tempTapes.Length; i++) {
        if (tempTapes[i] != this && tempTapes[i].curState == manipState.grabbed) return;
      }
    }

    samplerLoad[] samps = FindObjectsOfType<samplerLoad>();
    for (int i = 0; i < samps.Length; i++) {
      samps[i].flashDecklight(on);
    }
  }

  Coroutine returnRoutineID;
  IEnumerator returnRoutine() {
    Vector3 curPos = transform.localPosition;
    Quaternion curRot = transform.localRotation;

    float t = 0;
    float modT = 0;
    while (t < 1) {
      t = Mathf.Clamp01(t + Time.deltaTime * 2);
      modT = Mathf.Sin(t * Mathf.PI * 0.5f);
      transform.localPosition = Vector3.Lerp(curPos, origPos, modT);
      transform.localRotation = Quaternion.Lerp(curRot, origRot, modT);
      yield return null;
    }
  }

  public void Eject() {
    deck = null;
    if (masterObj) {
      transform.parent = masterObj;
      if (gameObject.activeInHierarchy) {
        if (returnRoutineID != null) StopCoroutine(returnRoutineID);
        returnRoutineID = StartCoroutine(returnRoutine());
      } else {
        transform.localPosition = origPos;
        transform.localRotation = origRot;
      }
    } else Destroy(gameObject);
  }

  void OnEnable() {
    if (deck == null) {
      transform.localPosition = origPos;
      transform.localRotation = origRot;
    }
  }

  public void Release() {
    if (deck == null) {
      if (masterObj == null) Destroy(gameObject);

      transform.parent = masterObj;

      if (gameObject.activeInHierarchy) {

        if (returnRoutineID != null) StopCoroutine(returnRoutineID);
        returnRoutineID = StartCoroutine(returnRoutine());
      } else {
        transform.localPosition = origPos;
        transform.localRotation = origRot;
      }
    } else {

      tapeTrans.position = deck.transform.position;
      tapeTrans.rotation = deck.transform.rotation;
      tapeTrans.parent = deck;
      tapeTrans.Rotate(-90, 0, 0);

      transform.parent = tapeTrans.parent;
      transform.position = tapeTrans.position;
      transform.rotation = tapeTrans.rotation;
      tapeTrans.parent = transform;
      deck.parent.GetComponent<samplerLoad>().LoadTape(this);
      if (insertCoroutine != null) StopCoroutine(insertCoroutine);
      insertCoroutine = StartCoroutine(insertRoutine());
    }
  }

  public void ForceLoad(Transform targetDeck) {
    deck = targetDeck;
    tapeTrans.position = deck.transform.position;
    tapeTrans.rotation = deck.transform.rotation;
    tapeTrans.parent = deck;
    tapeTrans.Rotate(-90, 0, 0);

    transform.parent = tapeTrans.parent;
    transform.position = tapeTrans.position;
    transform.rotation = tapeTrans.rotation;
    tapeTrans.parent = transform;
    transform.localPosition = new Vector3(0, 0, -.0225f);
    deck.parent.GetComponent<samplerLoad>().LoadTape(this);
  }

  IEnumerator streamRoutine(string f) {
    AudioClip c = RuntimeAudioClipLoader.Manager.Load(f, false, true, true);

    loaderObject = Instantiate(loadingPrefab, transform, false) as GameObject;
    loaderObject.transform.localPosition = new Vector3(-.03f, -.037f, .01f);
    loaderObject.transform.localRotation = Quaternion.Euler(-90, 180, 0);
    loaderObject.transform.localScale = Vector3.one * .1f;

    while (RuntimeAudioClipLoader.Manager.GetAudioClipLoadState(c) != AudioDataLoadState.Loaded) {
      yield return null;
    }
    if (loaderObject != null) Destroy(loaderObject);
    if(masterObj.parent.GetComponent<AudioSource>() != null) masterObj.parent.GetComponent<AudioSource>().PlayOneShot(c, .25f);
  }

  Coroutine _StreamRoutine;
  bool previewing = false;
  void preview(bool on) {
    previewing = on;
    if (on) {
      if (masterObj == null) return;
      if (masterObj.parent == null) return;

      string f = sampleManager.instance.parseFilename(filename);
      if (!File.Exists(f)) return;
      if (_StreamRoutine != null) {
        if (loaderObject != null) Destroy(loaderObject);
        StopCoroutine(_StreamRoutine);
      }
      _StreamRoutine = StartCoroutine(streamRoutine(f));
    } else {
      if (loaderObject != null) Destroy(loaderObject);
      if (_StreamRoutine != null) StopCoroutine(_StreamRoutine);
      if (masterObj != null) {
        if (masterObj.parent.GetComponent<AudioSource>() != null) masterObj.parent.GetComponent<AudioSource>().Stop();
      }
    }
  }

  GameObject loaderObject;
  public override void setState(manipState state) {
    if (curState == state) return;

    if (previewing) {
      if (previewWhenGrabbed) {
        if (state == manipState.none) preview(false);
      } else if (curState == manipState.selected) preview(false);
    }

    if (curState == manipState.grabbed && state != manipState.grabbed) {
      flashEmptySamplers(false);
      Release();
    }

    curState = state;

    // had to place the material loading here, because this script is often deactivated and thus Awake() would not work
    if(mat == null) mat = tapeRend.sharedMaterial;
    if (highlightMat == null) highlightMat = Resources.Load("Materials/highlight") as Material;
    if (highlightGrabbedMat == null) highlightGrabbedMat = Resources.Load("Materials/highlightGrabbed") as Material;

    if (curState == manipState.none) {
      tapeRend.sharedMaterial = mat;
    }
    if (curState == manipState.selected) {
      tapeRend.sharedMaterial = highlightMat;
      if (deck == null) preview(true);
    }
    if (curState == manipState.grabbed) {
      tapeRend.sharedMaterial = highlightGrabbedMat;
      if (returnRoutineID != null) StopCoroutine(returnRoutineID);
      flashEmptySamplers(true);
      if (insertCoroutine != null) StopCoroutine(insertCoroutine);
      transform.parent = manipulatorObj.parent;
    }
  }
}