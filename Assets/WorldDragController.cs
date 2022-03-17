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
      patchPositionAtBeginDrag = transform.position;
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

      Debug.DrawLine(Vector3.zero, patchPositionAtBeginDrag, new Color(1f, 0f, 0f));
      Debug.DrawLine(Vector3.zero, new Vector3(Mathf.Sin(Mathf.Deg2Rad * patchAngleAtBeginDrag), 0f, Mathf.Cos(Mathf.Deg2Rad * patchAngleAtBeginDrag)), new Color(0f, 1f, 0f));
      Debug.DrawLine(Vector3.zero, controllerMiddleAtBeginDrag, new Color(0f, 0f, 1f));
      Debug.DrawLine(Vector3.zero, new Vector3(Mathf.Sin(Mathf.Deg2Rad * controllerAngleAtBeginDrag), 0f, Mathf.Cos(Mathf.Deg2Rad * controllerAngleAtBeginDrag)), new Color(1f, 1f, 0f));
      Debug.DrawLine(Vector3.zero, new Vector3(Mathf.Sin(Mathf.Deg2Rad * currentControllerAngle), 0f, Mathf.Cos(Mathf.Deg2Rad * currentControllerAngle)), new Color(1f, 0f, 1f));
      Debug.DrawLine(Vector3.zero, getMiddle(leftHandAnchor, rightHandAnchor), new Color(0f, 1f, 1f));

      // translation
      //transform.position = patchPositionAtBeginDrag + getMiddle(leftHandAnchor, rightHandAnchor) - controllerMiddleAtBeginDrag;

      // rotation
      currentControllerAngle = getAngleBetweenControllers();
      transform.rotation = Quaternion.identity; // first reset and then rotate, because RotateAround is adding up

      transform.RotateAround(getMiddle(leftHandAnchor, rightHandAnchor), Vector3.up, patchAngleAtBeginDrag + controllerAngleAtBeginDrag - currentControllerAngle);
      //transform.RotateAround(transform.position, Vector3.up, patchAngleAtBeginDrag + controllerAngleAtBeginDrag - currentControllerAngle);
      //transform.RotateAround(getPatchCenter(), Vector3.up, patchAngleAtBeginDrag + controllerAngleAtBeginDrag - currentControllerAngle);

      transform.position = patchPositionAtBeginDrag + getMiddle(leftHandAnchor, rightHandAnchor) - controllerMiddleAtBeginDrag;

    }
  }

  Vector3 getPatchCenter(){
    Vector3 sumVector = new Vector3(0f, 0f, 0f);

    foreach (Transform child in transform)
    {
      sumVector += child.position;
    }

    return sumVector / transform.childCount;
  }

  Vector3 getMiddle(Transform a, Transform b){
    return Vector3.Lerp(a.position, b.position, 0.5f);
  }

  float getAngleBetweenControllers(){
    // on x,z plane
    return Mathf.Rad2Deg * Mathf.Atan2(leftHandAnchor.transform.position.z - rightHandAnchor.transform.position.z, leftHandAnchor.transform.position.x - rightHandAnchor.transform.position.x);
  }
}
