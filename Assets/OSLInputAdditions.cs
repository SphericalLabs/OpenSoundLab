using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class OSLInput 
{

    
    
    public bool wasMenuStartedByController(int controllerIndex){
      return (Patcher.PrimaryLeft.WasPressedThisFrame() && controllerIndex == 0) || (Patcher.PrimaryRight.WasPressedThisFrame() && controllerIndex == 1);
    }

    public bool wasTriggerStartedByController(int controllerIndex)
    {
      return (Patcher.TriggerLeft.WasPressedThisFrame() && controllerIndex == 0) || (Patcher.TriggerRight.WasPressedThisFrame() && controllerIndex == 1);
    }    
    
    public bool wasTriggerReleasedByController(int controllerIndex)
    {
      return (Patcher.TriggerLeft.WasReleasedThisFrame() && controllerIndex == 0) || (Patcher.TriggerRight.WasReleasedThisFrame() && controllerIndex == 1);
    }

    public bool wasCopyStartedByController(int controllerIndex)
    {
      return (Patcher.SecondaryLeft.WasPressedThisFrame() && controllerIndex == 0) || (Patcher.SecondaryRight.WasPressedThisFrame() && controllerIndex == 1);
    }    
    
    public bool wasCopyReleasedByController(int controllerIndex)
    {
      return (Patcher.SecondaryLeft.WasReleasedThisFrame() && controllerIndex == 0) || (Patcher.SecondaryRight.WasReleasedThisFrame() && controllerIndex == 1);
    }

}
