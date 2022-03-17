using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldDragController : MonoBehaviour
{
  public manipulator leftManip, rightManip;
  public Transform leftHandAnchor, rightHandAnchor;

  Vector3 patchPositionAtBeginDrag, controllerMiddleAtBeginDrag;
  float controllerAngleAtBeginDrag, patchAngleAtBeginDrag;
  float currentControllerAngle;
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
      patchPositionAtBeginDrag = transform.localPosition;
      patchAngleAtBeginDrag = transform.rotation.eulerAngles.y;

      controllerMiddleAtBeginDrag = getMiddle(leftHandAnchor, rightHandAnchor);
      controllerAngleAtBeginDrag = getAngleBetweenControllers();
    }
    
    // end of drag
    if (isDragging && (OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger) < 0.3f || OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger) < 0.3f) )
    {
      isDragging = false;
    }

    // do the drag
    if(isDragging){

      // translation
      transform.localPosition = patchPositionAtBeginDrag + getMiddle(leftHandAnchor, rightHandAnchor) - controllerMiddleAtBeginDrag;

      // rotation
      currentControllerAngle = getAngleBetweenControllers();
      transform.rotation = Quaternion.identity; // first reset and then rotate, because RotateAround is adding up
      transform.RotateAround(getMiddle(leftHandAnchor, rightHandAnchor), Vector3.up, patchAngleAtBeginDrag + controllerAngleAtBeginDrag - currentControllerAngle);
      
    }
  }

  Vector3 getMiddle(Transform a, Transform b){
    return Vector3.Lerp(a.localPosition, b.localPosition, 0.5f);
  }

  float getAngleBetweenControllers(){
    // on x,z plane
    return Mathf.Rad2Deg * Mathf.Atan2(leftHandAnchor.transform.localPosition.z - rightHandAnchor.transform.localPosition.z, leftHandAnchor.transform.localPosition.x - rightHandAnchor.transform.localPosition.x);
  }
}
