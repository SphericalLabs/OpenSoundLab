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
using static OVRHand;
using OculusSampleFramework;
//using System.Linq;
//using Valve.VR;

public class manipulator : MonoBehaviour
{
  int controllerIndex = -1; // 0 => left, 1 => right
  public GameObject activeTip;
  public Transform tipL, tipR;
  
  List<Transform> hitTransforms = new List<Transform>();
  Transform selectedTransform;
  manipObject selectedObject;


  menuspawn _menuspawn;
  
  bool usingOculus = false;
  public Color onColor = Color.HSVToRGB(208 / 359f, 234 / 255f, 93 / 255f);

  void Awake()
  {
    _menuspawn = GetComponent<menuspawn>(); 
    activeTip.SetActive(false);
  }

  bool controllerVisible = true;
  public void toggleController(bool on)
  {
    controllerVisible = on;
  }

  public void SetDeviceIndex(int index)
  {
    controllerIndex = index;    
  }

  public bool isLeftController() {
    return controllerIndex == 0 ? true : false;
  }

  bool grabbing;
  public bool emptyGrab;

  public bool isGrabbing()
  {
    return (selectedObject != null);
  }

  public void showTip()
  {
    
  }

  public void invertScale()
  {
    transform.parent.localScale = new Vector3(-1, 1, 1);

  }

  void Grab(bool on)
  {
    grabbing = on;
    showTip();

    if (selectedObject != null)
    {
      selectedObject.setGrab(grabbing, transform);
      if (!on) toggleController(true);
      else if (usingOculus) selectedObject.setTouch(true);
    }
    else emptyGrab = on;
  }

  public manipObject getSelection()
  {
    if (!grabbing) return null;
    return selectedObject;
  }

  public void ForceRelease()
  {
    showTip();
    if (grabbing == true)
    {
      release();

      grabbing = false;
    }
  }

  void release()
  {
    selectedObject.setState(manipObject.manipState.none);
    toggleController(true);
  }

  public void ForceGrab(manipObject o)
  {
    if (selectedObject != null) release();

    grabbing = true;
    selectedObject = o;
    selectedObject.setGrab(grabbing, transform);
    emptyGrab = false;
    selectedTransform = o.transform;
  }

  void OnCollisionEnter(Collision coll)
  {
    manipObject o = coll.transform.GetComponent<manipObject>();
    if (o != null) o.onTouch(true, this);
  }

  void OnCollisionExit(Collision coll)
  {
    manipObject o = coll.transform.GetComponent<manipObject>();
    if (o != null) o.onTouch(false, this);
  }


  void OnCollisionStay(Collision coll)
  {
    hitTransforms.Add(coll.transform);
  }


  void FixedUpdate()
  {
    hitTransforms.Clear();
  }

  Coroutine pulseCoroutine;
  public void constantPulse(bool on)
  {
    if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
    if (on)
    {
      pulseCoroutine = StartCoroutine(pulseRoutine());
    }

  }

  IEnumerator pulseRoutine()
  {
    while (true)
    {
      hapticPulse(750);
      yield return new WaitForSeconds(0.3f);
    }
  }

  public void hapticPulse(float hapticPower = 1200f) {
     
    doHaptic(hapticPower);
  }

  public void bigHaptic(float hapticPower = 750f, float dur = 0.1f)
  {
    doHaptic(hapticPower, dur);
  }


  void doHaptic(float hapticPower = 750f, float dur = 0.1f)
  {
    if (Unity.XR.Oculus.Utils.GetSystemHeadsetType() == Unity.XR.Oculus.SystemHeadset.Meta_Quest_Pro 
    || Unity.XR.Oculus.Utils.GetSystemHeadsetType() == Unity.XR.Oculus.SystemHeadset.Meta_Link_Quest_Pro)
    {
      hapticPower = hapticPower * 2.3f;
      dur *= 0.07f;      
    }

    if (Unity.XR.Oculus.Utils.GetSystemHeadsetType() == Unity.XR.Oculus.SystemHeadset.Oculus_Quest_2)
    {
      hapticPower *= 3f;
      dur *= 0.07f;
    }

    if (Unity.XR.Oculus.Utils.GetSystemHeadsetType() == Unity.XR.Oculus.SystemHeadset.Oculus_Link_Quest_2)
    {
      hapticPower *= 4f;
      dur *= 0.07f;
    }

    

    List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();

    if (controllerIndex == 0)
    {
      UnityEngine.XR.InputDevices.GetDevicesWithRole(UnityEngine.XR.InputDeviceRole.LeftHanded, devices);
    }
    else if (controllerIndex == 1)
    {
      UnityEngine.XR.InputDevices.GetDevicesWithRole(UnityEngine.XR.InputDeviceRole.RightHanded, devices);
    }

    foreach (var device in devices)
    {
      UnityEngine.XR.HapticCapabilities capabilities;
      if (device.TryGetHapticCapabilities(out capabilities))
      {
        if (capabilities.supportsImpulse)
        {
          uint channel = 0;
          float amplitude = hapticPower / 3999.0f;
          amplitude = Mathf.Clamp01(amplitude);
          device.SendHapticImpulse(channel, amplitude, dur);
        }
      }
    }
  }


  void LateUpdate()
  {
    if (grabbing) return;
    Transform candidate = null;

    if (selectedObject != null)
    {
      if (hitTransforms.Contains(selectedTransform)) candidate = selectedTransform;
    }
    else
    {
      foreach (Transform t in hitTransforms)
      {
        if (t != null)
        {
          manipObject o = t.GetComponent<manipObject>();
          if (o != null)
          {
            if (o.curState != manipObject.manipState.grabbed && o.curState != manipObject.manipState.selected)
            {
              candidate = t;
              break;
            }
          }
        }
      }
    }

    if (candidate != selectedTransform)
    {
      if (selectedObject != null) selectedObject.GetComponent<manipObject>().setSelect(false, transform);

      if (candidate != null)
      {
        candidate.GetComponent<manipObject>().setSelect(true, transform);
        if (candidate.GetComponent<handle>() != null) toggleCopy(true);
        else if (candidate.GetComponent<manipObject>().canBeDeleted) toggleDelete(true);
        hapticPulse();
        selectedTransform = candidate;
        selectedObject = candidate.GetComponent<manipObject>();
      }
      else
      {
        toggleCopy(false);
        toggleDelete(false);
        selectedTransform = null;
        selectedObject = null;
      }
    }
  }

  bool copyEnabled = false;
  void toggleCopy(bool on)
  {
    copyEnabled = on;
    if (!usingOculus){ }
    else if (!copying)
    {
      //oculusSprites[0].gameObject.SetActive(!on);
      //oculusSprites[1].gameObject.SetActive(on);
      //oculusSprites[2].gameObject.SetActive(false);
      //oculusSprites[3].gameObject.SetActive(false);

    }

  }

  bool deleteEnabled = false;
  void toggleDelete(bool on)
  {
    if (multiselecting) return;

    deleteEnabled = on;
    if (!usingOculus) { }
    else
    {
      //oculusSprites[0].gameObject.SetActive(!on);
      //oculusSprites[1].gameObject.SetActive(false);
      //oculusSprites[2].gameObject.SetActive(on);
      //oculusSprites[3].gameObject.SetActive(false);
    }

  }

  timelineGridUI multiselectGrid;

  bool multiselectEnabled = false;
  public void toggleMultiselect(bool on, timelineGridUI _grid)
  {
    if (!on && multiselecting) return;

    multiselectGrid = on ? _grid : null;
    multiselectEnabled = on;    
  }

  public void SetTrigger(bool on)
  {
    triggerDown = on;

    if (selectedObject != null)
    {
      if (selectedObject.stickyGrip && selectedObject.curState == manipObject.manipState.grabbed)
      {
        if (on) Grab(!on);
        return;
      }
    }
    Grab(on);
  }

  bool multiselecting = false;
  public void MultiselectSelection(bool on)
  {
    multiselecting = on;

    if (multiselectGrid == null)
    {
      Debug.Log("HRM..");
      return;
    }
    else
    {
      multiselectGrid.onMultiselect(on, transform);
    }
  }

  bool deleting = false;
  public void DeleteSelection(bool on)
  {
    deleting = on;
    if (on && selectedObject != null)
    {
      selectedObject.selfdelete();
    }

    if (!on)
    {
      if (selectedObject == null) toggleDelete(false);
      else if (!selectedObject.canBeDeleted) toggleDelete(false);
    }
  }

  bool copying = false;
  public void SetCopy(bool on)
  {
    if (menuManager.instance.simple) return;

    copying = on;

    if (selectedObject != null)
    {
      if (on)
      {
        if (selectedObject.GetComponent<handle>() != null) SaveLoadInterface.instance.Copy(selectedObject.transform.parent.gameObject, this);
      }
      else if (selectedObject.GetComponent<handle>() != null && !triggerDown) Grab(false);
    }
  }

  void updateProngs()
  {
    float val = 0;
    //    if (masterControl.instance.currentPlatform == masterControl.platform.Vive) val = SteamVR_Controller.Input(controllerIndex).GetAxis(EVRButtonId.k_EButton_Axis1).x;
    if (controllerIndex == 0)
    {
      val = Input.GetAxis("triggerR");
    }
    else if (controllerIndex == 1)
    {
      val = Input.GetAxis("triggerL");
    }
    else
    {
      val = 0;
    }

    tipL.localPosition = new Vector3(Mathf.Lerp(-.005f, 0, val), -.005f, -.018f);
    tipR.localPosition = new Vector3(Mathf.Lerp(.004f, -.001f, val), -.005f, -.018f);
  }

  bool showingTips = false;
  public void toggleTips(bool on)
  {

    //showingTips = on;
    //for (int i = 0; i < tipTexts.Length; i++) {
    //  tipTexts[i].SetActive(showingTips);
    //}
    //if (!usingOculus) _touchpad.setQuestionMark(showingTips);
  }


  public void changeHW(string s)
  {
    if (s == "oculus")
    {
      usingOculus = true;
    }
  }

  public void setVerticalPosition(Transform t)
  {


  }

  bool touchpadActive = false;
  public void viveTouchpadUpdate()
  {

    bool tOn; // = SteamVR_Controller.Input(controllerIndex).GetTouchDown(SteamVR_Controller.ButtonMask.Touchpad);
    bool tOff; // = SteamVR_Controller.Input(controllerIndex).GetTouchUp(SteamVR_Controller.ButtonMask.Touchpad);

    bool pOn; // = SteamVR_Controller.Input(controllerIndex).GetPressDown(SteamVR_Controller.ButtonMask.Touchpad);
    bool pOff; // = SteamVR_Controller.Input(controllerIndex).GetPressUp(SteamVR_Controller.ButtonMask.Touchpad);

    if (controllerIndex == 0)
    {
      tOn = Input.GetButtonDown("touchR");
      tOff = Input.GetButtonUp("touchR");
      pOn = Input.GetButtonDown("pressR");
      pOff = Input.GetButtonUp("pressR");
    }
    else if (controllerIndex == 1)
    {
      tOn = Input.GetButtonDown("touchL");
      tOff = Input.GetButtonUp("touchL");
      pOn = Input.GetButtonDown("pressL");
      pOff = Input.GetButtonUp("pressL");
    }
    else
    {
      tOn = tOff = pOn = pOff = false;
    }
    bool activeManipObj = (grabbing && selectedObject != null);

    if (tOn)
    {
      touchpadActive = true;

      if (activeManipObj) selectedObject.setTouch(true);
    }
    if (tOff)
    {
      touchpadActive = false;
      if (activeManipObj) selectedObject.setTouch(false);
    }

    if (!touchpadActive) return;

    Vector2 pos; // = SteamVR_Controller.Input(controllerIndex).GetAxis();
    if (controllerIndex == 0)
    {
      pos = new Vector2(Input.GetAxis("touchAxisXR"), Input.GetAxis("touchAxisYR"));
    }
    else if (controllerIndex == 1)
    {
      pos = new Vector2(Input.GetAxis("touchAxisXL"), Input.GetAxis("touchAxisYL"));
    }
    else
    {
      pos = new Vector2(0.5f, 0.5f);
    }

    if (activeManipObj) selectedObject.updateTouchPos(pos);

    if (pOn)
    {

      if (activeManipObj) selectedObject.setPress(true);
    }

    if (pOff)
    {
      if (activeManipObj) selectedObject.setPress(false);
    }
  }

  void secondaryOculusButtonUpdate()
  {
    bool secondaryDown = false;
    bool secondaryUp = false;

    if (masterControl.instance.currentPlatform == masterControl.platform.Oculus)
    {
      //      secondaryDown = SteamVR_Controller.Input(controllerIndex).GetPressDown(SteamVR_Controller.ButtonMask.ApplicationMenu);
      //      secondaryUp = SteamVR_Controller.Input(controllerIndex).GetPressUp(SteamVR_Controller.ButtonMask.ApplicationMenu);
      if (controllerIndex == 0)
      {
        secondaryDown = Input.GetButtonDown("secondaryButtonR");
      }
      else if (controllerIndex == 1)
      {
        secondaryDown = Input.GetButtonDown("secondaryButtonL");
      }
      else
      {
        secondaryDown = false;
      }

      if (controllerIndex == 0)
      {
        secondaryUp = Input.GetButtonUp("secondaryButtonR");
      }
      else if (controllerIndex == 1)
      {
        secondaryUp = Input.GetButtonUp("secondaryButtonL");
      }
      else
      {
        secondaryUp = false;
      }
    }
    if (controllerVisible)
    {
      if (secondaryDown)
      {
        if (copyEnabled) SetCopy(true);
        else if (deleteEnabled) DeleteSelection(true);
        else if (multiselectEnabled) MultiselectSelection(true);
        else toggleTips(true);
        //oculusContextButtonGlow.SetActive(true);
      }
      else if (secondaryUp)
      {
        toggleTips(false);
        if (copying) SetCopy(false);
        else if (deleting) DeleteSelection(false);
        else if (multiselectEnabled) MultiselectSelection(false);
        //oculusContextButtonGlow.SetActive(false);
      }
    }
    else if (grabbing && selectedObject != null)
    {
      if (secondaryDown) selectedObject.setPress(true);
      if (secondaryUp) selectedObject.setPress(false);
    }
  }

  public bool triggerDown = false;
  public bool pinchPinkyDown = false;
  static OVRPlugin.Controller lastControl = OVRPlugin.Controller.None;
  public bool isTrackingWorking;

  void Update()
  {
    OVRPlugin.Controller currentControl = OVRPlugin.GetActiveController(); //get current controller scheme

    
    OVRInput.Controller currentInput = OVRInput.Controller.None;
    if (controllerIndex == 0) {
      currentInput = OVRInput.Controller.LTouch; // check if correct
    } else if (controllerIndex == 1) {
      currentInput = OVRInput.Controller.RTouch;
    }

    //Debug.Log("");
    //Debug.Log(OVRInput.GetControllerOrientationTracked(currentInput));
    //Debug.Log(OVRInput.GetControllerPositionTracked(currentInput));
    //Debug.Log(OVRInput.GetControllerOrientationValid(currentInput));
    //Debug.Log(OVRInput.GetControllerPositionValid(currentInput));
    //Debug.Log("");


    isTrackingWorking = OVRInput.GetControllerOrientationTracked(currentInput) &&
      OVRInput.GetControllerPositionTracked(currentInput) &&
      OVRInput.GetControllerOrientationValid(currentInput) &&
      OVRInput.GetControllerPositionValid(currentInput);
          
      // bruteforce!!!
      foreach (Renderer childRenderer in gameObject.transform.parent.parent.parent.GetComponentsInChildren<Renderer>())
      {
        childRenderer.enabled = isTrackingWorking;
      }

      foreach (SkinnedMeshRenderer childRenderer in gameObject.transform.parent.parent.parent.GetComponentsInChildren<SkinnedMeshRenderer>()){
        childRenderer.enabled = isTrackingWorking;
      }
      
      if(!isTrackingWorking) return;
            


    if (controllerIndex == 0)
    {
      transform.position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
      transform.rotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch);
    } 

    if(controllerIndex == 1){
      transform.position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
      transform.rotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
    }

    updateProngs();
    bool triggerButtonDown, triggerButtonUp, menuButtonDown;

    

    bool currentControlHands = false;
    bool currentControlChanged = false;
    float pinchIndexStrength = 0;
    float pinchPinkyStrength = 0;
    //if ((OVRPlugin.Controller.Hands == currentControl) || (OVRPlugin.Controller.LHand == currentControl) || (OVRPlugin.Controller.RHand == currentControl))
    //{
    //  currentControlHands = true;
    //  //pinchIndexStrength = Hands.Instance.RightHand.PinchStrength(OVRPlugin.HandFinger.Index);
    //  //pinchPinkyStrength = Hands.Instance.RightHand.PinchStrength(OVRPlugin.HandFinger.Pinky);
    //}
    if (currentControl != lastControl)
    {
      currentControlChanged = true;
      lastControl = currentControl;
    }
    if (masterControl.instance.currentPlatform == masterControl.platform.Oculus)
    {
      //      triggerButtonDown = SteamVR_Controller.Input(controllerIndex).GetPressDown(SteamVR_Controller.ButtonMask.Trigger);
      //      triggerButtonUp = SteamVR_Controller.Input(controllerIndex).GetPressUp(SteamVR_Controller.ButtonMask.Trigger);
      if (!triggerDown)
      {
        if (currentControlHands)
        {
          triggerButtonDown = pinchIndexStrength > 0.85;
        }
        else
        {
          if (controllerIndex == 0)
          {
            triggerButtonDown = Input.GetAxis("triggerR") > 0.75;
          }
          else if (controllerIndex == 1)
          {
            triggerButtonDown = Input.GetAxis("triggerL") > 0.75;
          }
          else
          {
            triggerButtonDown = false;
          }
        }
      }
      else
      {
        triggerButtonDown = false;
      }
      if (triggerDown)
      {
        if (currentControlHands)
        {
          triggerButtonUp = pinchIndexStrength < 0.50;
        }
        else if (!currentControlHands && currentControlChanged)
        {
          // force an trigger button up when we lose hands
          triggerButtonUp = true;
        }
        else
        {
          if (controllerIndex == 0)
          {
            triggerButtonUp = Input.GetAxis("triggerR") < 0.25;
          }
          else if (controllerIndex == 1)
          {
            triggerButtonUp = Input.GetAxis("triggerL") < 0.25;
          }
          else
          {
            triggerButtonUp = false;
          }
        }
      }
      else
      {
        triggerButtonUp = false;
      }

      viveTouchpadUpdate();
      if (!usingOculus)
      {
        //        menuButtonDown = SteamVR_Controller.Input(controllerIndex).GetPressDown(SteamVR_Controller.ButtonMask.ApplicationMenu);
        if (currentControlHands)
        {
          if (pinchPinkyStrength > 0.85)
          {
            if (pinchPinkyDown)
            {
              menuButtonDown = false;
            }
            else
            {
              menuButtonDown = true;
              pinchPinkyDown = true;
            }
          }
          else if (pinchPinkyStrength < 0.25)
          {
            pinchPinkyDown = false;
            menuButtonDown = false;
          }
          else
          {
            menuButtonDown = false;
          }
        }
        else
        {
          if (controllerIndex == 0)
          {
            menuButtonDown = Input.GetButtonDown("menuButtonR");
          }
          else if (controllerIndex == 1)
          {
            menuButtonDown = Input.GetButtonDown("menuButtonL");
          }
          else
          {
            menuButtonDown = false;
          }
        }
      }
      else
      {
        Vector2 pos; // = SteamVR_Controller.Input(controllerIndex).GetAxis();
        if (controllerIndex == 0)
        {
          pos = new Vector2(Input.GetAxis("touchAxisXR"), Input.GetAxis("touchAxisYR"));
        }
        else if (controllerIndex == 1)
        {
          pos = new Vector2(Input.GetAxis("touchAxisXL"), Input.GetAxis("touchAxisYL"));
        }
        else
        {
          pos = new Vector2(0.5f, 0.5f);
        }

        if (grabbing && selectedObject != null) selectedObject.updateTouchPos(pos);

        secondaryOculusButtonUpdate();
        //        menuButtonDown = SteamVR_Controller.Input(controllerIndex).GetPressDown(EVRButtonId.k_EButton_A);
        if (currentControlHands)
        {
          if (pinchPinkyStrength > 0.85)
          {
            if (pinchPinkyDown)
            {
              menuButtonDown = false;
            }
            else
            {
              menuButtonDown = true;
              pinchPinkyDown = true;
            }
          }
          else if (pinchPinkyStrength < 0.25)
          {
            pinchPinkyDown = false;
            menuButtonDown = false;
          }
          else
          {
            menuButtonDown = false;
          }
        }
        else
        {
          if (controllerIndex == 0)
          {
            menuButtonDown = Input.GetButtonDown("menuButtonR");
          }
          else if (controllerIndex == 1)
          {
            menuButtonDown = Input.GetButtonDown("menuButtonL");
          }
          else
          {
            menuButtonDown = false;
          }
        }
      }
    }
    else
    {
      triggerButtonDown = triggerButtonUp = menuButtonDown = false;
    }

    if (triggerButtonDown)
    {
      activeTip.SetActive(true);
      tipL.gameObject.SetActive(false);
      tipR.gameObject.SetActive(false);
      SetTrigger(true);
    }
    if (triggerButtonUp)
    {
      activeTip.SetActive(false);
      tipL.gameObject.SetActive(true);
      tipR.gameObject.SetActive(true);
      SetTrigger(false);
    }

    if (menuButtonDown) _menuspawn.togglePad();

    if (grabbing && selectedObject != null) selectedObject.grabUpdate(transform);
    else if (selectedObject != null) selectedObject.selectUpdate(transform);
  }
}


