using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldDragController : MonoBehaviour
{
  public manipulator leftManip, rightManip;
  public Transform leftHandAnchor, rightHandAnchor;

  Vector3 startPosSpace, startPosLeft, startPosRight, midOfControllers;
  float startAngle;
  float newAngle;
  bool isDragging = false;

  void Awake()
  {
   
  }

  void Update()
  {
    
    if (leftManip == null) leftManip = GameObject.Find("ControllerLeft").GetComponentInChildren<manipulator>();
    if (rightManip == null) rightManip = GameObject.Find("ControllerRight").GetComponentInChildren<manipulator>();

    // don't start the moving if grabbing something else already
    if (!isDragging && (leftManip.isGrabbing() || rightManip.isGrabbing())) return; 

    // begin drag, save start values
    if (!isDragging && OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger) > 0.5f && OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger) > 0.5f)
    {
      isDragging = true;
      startPosSpace = transform.localPosition;
      startPosLeft = leftHandAnchor.localPosition;
      startPosRight = rightHandAnchor.localPosition;
      startAngle = calcAngleBetweenControllers();
    }
    
    // end of drag
    if (isDragging && (OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger) < 0.3f || OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger) < 0.3f) )
    {
      isDragging = false;
    }

    // do the drag
    if(isDragging){

      // translation
      transform.localPosition = startPosSpace + Vector3.Lerp(leftHandAnchor.localPosition - startPosLeft, rightHandAnchor.localPosition - startPosRight, 0.5f);

      // rotation
      midOfControllers = Vector3.Lerp(leftHandAnchor.localPosition, rightHandAnchor.localPosition, 0.5f);
      newAngle = calcAngleBetweenControllers();
      transform.rotation = Quaternion.identity; // first reset and then rotate, because RotateAround is adding up
      transform.RotateAround(midOfControllers, Vector3.up, startAngle - newAngle);
      
    }
  }

  float calcAngleBetweenControllers(){
    // on x,z plane
    return Mathf.Rad2Deg * Mathf.Atan2(leftHandAnchor.transform.localPosition.z - rightHandAnchor.transform.localPosition.z, leftHandAnchor.transform.localPosition.x - rightHandAnchor.transform.localPosition.x);
  }
}
