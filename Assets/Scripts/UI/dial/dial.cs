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

public class dial : manipObject {

  public float percent = 0f;

  float defaultPercent; 

  glowDisk dialFeedback;
  Material[] mats;

  public Color customColor;

  public bool externalHue = false;
  public float hue = .5f;

  GameObject littleDisk;

  public float deltaRot = 0;
  public float curRot = 0;
  public float realRot = 0f;
  public float prevShakeRot = 0f;
  public float fineMult = 5f;

  public override void Awake() {
    base.Awake();

    // store the first value on Awake and keep it as default
    defaultPercent = percent;

    littleDisk = transform.Find("littleDisk").gameObject;

    mats = new Material[3];
    mats[0] = littleDisk.GetComponent<Renderer>().material;
    mats[1] = transform.parent.Find("glowDisk").GetComponent<Renderer>().material;
    mats[2] = transform.parent.Find("Label").GetComponent<Renderer>().material;

    customColor = Color.HSVToRGB(hue, 1f, 0.4f);

    setGlowState(manipState.none);

    dialFeedback = transform.parent.Find("glowDisk").GetComponent<glowDisk>();
  }

  void setGlowState(manipState s) {
    Color c = customColor;

    switch (s) {
      case manipState.none:
        littleDisk.SetActive(false);

        for (int i = 0; i < mats.Length; i++) {
          mats[i].SetFloat("_EmissionGain", .5f);
          mats[i].SetColor("_TintColor", c);
        }
        break;
      case manipState.selected:
        littleDisk.SetActive(true);
        for (int i = 0; i < mats.Length; i++) {
          mats[i].SetFloat("_EmissionGain", 0.5f);
          mats[i].SetColor("_TintColor", c);
        }
        break;
      case manipState.grabbed:
        littleDisk.SetActive(true);

        for (int i = 0; i < mats.Length; i++) {
          mats[i].SetFloat("_EmissionGain", 0.5f);
          mats[i].SetColor("_TintColor", c);
        }
        break;
      default:
        break;
    }
  }


  void updateMatsColor(Color c) {
    foreach (Material m in mats) m.SetColor("_TintColor", c);
  }



  void Start() {
    setPercent(percent);
  }

  void Update() {

    if (curState == manipState.selected || curState == manipState.grabbed)
    {
      // reset dial to default
      if ((selectObj.parent.parent.name == "ControllerLeft" && OVRInput.Get(OVRInput.RawButton.Y)
      || (selectObj.parent.parent.name == "ControllerRight" && OVRInput.Get(OVRInput.RawButton.B))))
      {
        setPercent(defaultPercent);
        deltaRot = controllerRot - curRot;
      }
    }

    updatePercent();
  }

  public void setPercent(float p) {
    percent = Mathf.Clamp01(p);
    realRot = Utils.map(percent, 0f, 1f, -150f, 150f);
    
    curRot = realRot; // can be removed?
    transform.localRotation = Quaternion.Euler(0, realRot, 0);
  }

  void updatePercent() {

    percent = Utils.map(realRot, -150f, 150f, 0f, 1f);

    // viz
    dialFeedback.percent = percent * 0.85f; // why that multiplier?
    dialFeedback.PercentUpdate();
  }

  public float controllerRot = 0f;

  // begin grab, this sets the entry rotation via deltaRot
  public override void setState(manipState state) {
    curState = state;
    setGlowState(state);

    if (curState == manipState.grabbed) {
      turnCount = 0;

      // trigger beginners guidance
      if (!masterControl.instance.dialUsed) { 
        if (_dialCheckRoutine != null) StopCoroutine(_dialCheckRoutine);
        _dialCheckRoutine = StartCoroutine(dialCheckRoutine());
      }

      Vector2 temp = dialCoordinates(manipulatorObj.up);
      controllerRot = Vector2.Angle(temp, Vector2.up) * Mathf.Sign(temp.x);

      // UPDATE THIS
      if (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger) > 0.2f || OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger) > 0.2f)
      {
        deltaRot = controllerRot - curRot * fineMult;        
      } else {
        deltaRot = controllerRot - curRot;
      }
      
    }
  }


  public override void grabUpdate(Transform t)
  {

    Vector2 temp = dialCoordinates(manipulatorObj.up);
    controllerRot = Vector2.Angle(temp, Vector2.up) * Mathf.Sign(temp.x);
    
    curRot = Vector2.Angle(temp, Vector2.up) * Mathf.Sign(temp.x) - deltaRot;

    // fine tune modifier
    // https://developer.oculus.com/documentation/unity/unity-ovrinput
    // only raw input worked properly
    if ((t.parent.parent.name == "ControllerLeft" && OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger) > 0.1f)
    || (t.parent.parent.name == "ControllerRight" && OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger) > 0.1f))
      curRot /= fineMult;


    curRot = Mathf.Clamp(curRot, -150f, 150f);


    // catch naughty fliparounds, use realRot as lastRot
    if (realRot == -150f && curRot > 0f) return;
    if (realRot ==  150f && curRot < 0f) return;
    realRot = curRot;

    // apply
    transform.localRotation = Quaternion.Euler(0, realRot, 0);

    // haptics
    if (Mathf.Abs(realRot - prevShakeRot) > 10f)
    {
      if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(500);
      prevShakeRot = realRot;
      turnCount++;
    }
  }


  Vector2 dialCoordinates(Vector3 vec)
  {
    Vector3 flat = transform.parent.InverseTransformDirection(Vector3.ProjectOnPlane(vec, transform.parent.up));
    return new Vector2(flat.x, flat.z);
  }


  // code for first steps guidance below, currently not enabled

  int turnCount = 0;

  Coroutine _dialCheckRoutine;
  IEnumerator dialCheckRoutine()
  {
    Vector3 lastPos = Vector3.zero;
    float cumulative = 0;
    if (manipulatorObj != null)
    {
      lastPos = manipulatorObj.position;
    }

    while (curState == manipState.grabbed && manipulatorObj != null && !masterControl.instance.dialUsed)
    {
      cumulative += Vector3.Magnitude(manipulatorObj.position - lastPos);
      lastPos = manipulatorObj.position;

      if (turnCount > 3) masterControl.instance.dialUsed = true;
      else if (cumulative > .2f)
      {
        masterControl.instance.dialUsed = true;
        Instantiate(Resources.Load("Hints/TurnVignette", typeof(GameObject)), transform.parent, false);
      }

      yield return null;
    }
    yield return null;
  }
}
