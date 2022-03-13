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
  Quaternion startQuat;
  bool isDragging = false;

  void Awake()
  {
   
  }

  void Update()
  {
    
    if (leftManip == null) leftManip = GameObject.Find("ControllerLeft").GetComponentInChildren<manipulator>();
    if (rightManip == null) rightManip = GameObject.Find("ControllerRight").GetComponentInChildren<manipulator>();

    if (!isDragging && (leftManip.isGrabbing() || rightManip.isGrabbing())) return;

    if (!isDragging && OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger) > 0.5f && OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger) > 0.5f)
    {
      isDragging = true;
      startPosSpace = transform.localPosition;
      startPosLeft = leftHandAnchor.localPosition;
      startPosRight = rightHandAnchor.localPosition;
      startAngle = calcAngleBetweenControllers();
    }
    
    if (isDragging && (OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger) < 0.3f || OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger) < 0.3f) )
    {
      isDragging = false;
    }

    if(isDragging){
      //midOfControllers = Vector3.Lerp(leftHandAnchor.localPosition, rightHandAnchor.localPosition, 0.5f);
      //newAngle = calcAngleBetweenControllers();

      //transform.Rotate(Vector3.up, newAngle, Space.Self);

      
      //transform.localPosition = midOfControllers;
      //transform.RotateAround(midOfControllers, Vector3.up, transform.eulerAngles.y - newAngle);
      //transform.localEulerAngles = new Vector3(0f, newAngle - startAngle, 0f);
      
      //Quaternion temp = transform.rotation;
      //temp.eulerAngles = new Vector3(0f, newAngle - startAngle, 0f);
      //transform.rotation = temp;

      transform.localPosition = startPosSpace + Vector3.Lerp(leftHandAnchor.localPosition - startPosLeft, rightHandAnchor.localPosition - startPosRight, 0.5f) * 1.3f;
      
      //(midOfControllers, Vector3.up, Vector3.Angle(leftHandAnchor.localPosition, rightHandAnchor.localPosition) - startAngle);
    }
  }

  float calcAngleBetweenControllers(){
    return Mathf.Rad2Deg * Mathf.Atan2(leftHandAnchor.transform.localPosition.y - rightHandAnchor.transform.localPosition.y, leftHandAnchor.transform.localPosition.x - rightHandAnchor.transform.localPosition.x);
  }
}
