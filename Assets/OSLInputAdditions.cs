using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public partial class OSLInput 
{

    private static OSLInput instance;

    public static OSLInput getInstance(){
    if (instance == null)
    {
      instance = new OSLInput();
      instance.Enable();
    }
      return instance;
    }
    
    public bool isMenuStarted(int controllerIndex){
      return (Patcher.PrimaryLeft.WasPressedThisFrame() && controllerIndex == 0) || (Patcher.PrimaryRight.WasPressedThisFrame() && controllerIndex == 1);
    }

    public bool isTriggerStarted(int controllerIndex)
    {
      return (Patcher.TriggerLeft.WasPressedThisFrame() && controllerIndex == 0) || (Patcher.TriggerRight.WasPressedThisFrame() && controllerIndex == 1);
    }    
    
    public bool isTriggerReleased(int controllerIndex)
    {
      return (Patcher.TriggerLeft.WasReleasedThisFrame() && controllerIndex == 0) || (Patcher.TriggerRight.WasReleasedThisFrame() && controllerIndex == 1);
    }

    public bool isCopyStarted(int controllerIndex)
    {
      return (Patcher.SecondaryLeft.WasPressedThisFrame() && controllerIndex == 0) || (Patcher.SecondaryRight.WasPressedThisFrame() && controllerIndex == 1);
    }    
    
    public bool isCopyReleased(int controllerIndex)
    {
      return (Patcher.SecondaryLeft.WasReleasedThisFrame() && controllerIndex == 0) || (Patcher.SecondaryRight.WasReleasedThisFrame() && controllerIndex == 1);
    }

    public bool isAnyTriggerFullPressed()
    {
      return Patcher.TriggerLeftAnalog.ReadValue<float>() > 0.7f || Patcher.TriggerRightAnalog.ReadValue<float>() > 0.7f;
    }

    public bool areBothTriggersFullPressed()
    {
      return Patcher.TriggerLeftAnalog.ReadValue<float>() > 0.7f && Patcher.TriggerRightAnalog.ReadValue<float>() > 0.7f;
    }


    public bool isTriggerHalfPressed(int controllerIndex)
    {
      if (controllerIndex == 0)
      {
        return Patcher.TriggerLeftAnalog.ReadValue<float>() > 0.05f && Patcher.TriggerLeftAnalog.ReadValue<float>() <= 0.7f;
      }
      else if (controllerIndex == 1)
      {
        return Patcher.TriggerRightAnalog.ReadValue<float>() > 0.05f && Patcher.TriggerRightAnalog.ReadValue<float>() <= 0.7f;
      }
      return false;
    }

    public bool isTriggerFullPressed(int controllerIndex)
    {
      if (controllerIndex == 0)
      {
        return Patcher.TriggerLeftAnalog.ReadValue<float>() >= 0.7f;  
    }
      else if (controllerIndex == 1)
      {
        return Patcher.TriggerRightAnalog.ReadValue<float>() >= 0.7f;
    }
      return false;
    }

    public bool isTriggerPressed(int controllerIndex)
    {
      if (controllerIndex == 0)
      {
        return Patcher.TriggerLeftAnalog.ReadValue<float>() >= 0.05f;
      }
      else if (controllerIndex == 1)
      {
        return Patcher.TriggerRightAnalog.ReadValue<float>() >= 0.05f;
      }
      return false;
    }

    public bool isSidePressed(int controllerIndex)
    {      
      if (controllerIndex == 0)
      {
        return Patcher.GripLeft.ReadValue<float>() >= 0.1f;
      }
      else if (controllerIndex == 1)
      {
        return Patcher.GripRight.ReadValue<float>() >= 0.1f;
      }
      return false;
    }

    public bool areBothSidesPressed()
    {
      return Patcher.GripLeft.ReadValue<float>() >= 0.1f && Patcher.GripRight.ReadValue<float>() >= 0.1f;
    }


}
