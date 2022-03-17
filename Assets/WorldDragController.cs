using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldDragController : MonoBehaviour
{
  public manipulator leftManip, rightManip;
  public Transform leftHandAnchor, rightHandAnchor;

  Vector3 currentControllerMiddle, lastControllerMiddle;
  float currentControllerAngle, lastControllerAngle, currentControllerDistance, lastControllerDistance;
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

      lastControllerMiddle = getMiddle(leftHandAnchor, rightHandAnchor);
      lastControllerAngle = getAngleBetweenControllers();
      lastControllerDistance = getDistanceBetweenControllers();
      
    }
    
    // end of drag
    if (isDragging && (OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger) < 0.3f || OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger) < 0.3f) )
    {
      isDragging = false;
    }

    // do the drag
    if(isDragging){

      //Debug.DrawLine(Vector3.zero, patchPositionAtBeginDrag, new Color(1f, 0f, 0f));
      //Debug.DrawLine(Vector3.zero, new Vector3(Mathf.Sin(Mathf.Deg2Rad * patchAngleAtBeginDrag), 0f, Mathf.Cos(Mathf.Deg2Rad * patchAngleAtBeginDrag)), new Color(0f, 1f, 0f));
      //Debug.DrawLine(Vector3.zero, controllerMiddleAtBeginDrag, new Color(0f, 0f, 1f));
      //Debug.DrawLine(Vector3.zero, new Vector3(Mathf.Sin(Mathf.Deg2Rad * controllerAngleAtBeginDrag), 0f, Mathf.Cos(Mathf.Deg2Rad * controllerAngleAtBeginDrag)), new Color(1f, 1f, 0f));
      //Debug.DrawLine(Vector3.zero, new Vector3(Mathf.Sin(Mathf.Deg2Rad * currentControllerAngle), 0f, Mathf.Cos(Mathf.Deg2Rad * currentControllerAngle)), new Color(1f, 0f, 1f));
      //Debug.DrawLine(Vector3.zero, getMiddle(leftHandAnchor, rightHandAnchor), new Color(0f, 1f, 1f));

      currentControllerMiddle = getMiddle(leftHandAnchor, rightHandAnchor);

      // scale
      currentControllerDistance = getDistanceBetweenControllers();
      scaleAround(transform, currentControllerMiddle, transform.localScale * (1f + currentControllerDistance - lastControllerDistance));

      // rotation
      currentControllerAngle = getAngleBetweenControllers();
      transform.RotateAround(currentControllerMiddle, Vector3.up, lastControllerAngle - currentControllerAngle);

      // translation
      transform.Translate(currentControllerMiddle - lastControllerMiddle, Space.World);

      // for next frame
      lastControllerMiddle = currentControllerMiddle;
      lastControllerAngle = currentControllerAngle;
      lastControllerDistance = currentControllerDistance;
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

  float getDistanceBetweenControllers(){
    return Vector3.Distance(leftHandAnchor.transform.position, rightHandAnchor.transform.position);
  }

  // https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
  public void scaleAround(Transform target, Vector3 pivot, Vector3 newScale)
  {
    Vector3 A = target.localPosition;
    Vector3 B = pivot;

    Vector3 C = A - B; // diff from object pivot to desired pivot/origin

    float RS = newScale.x / target.localScale.x; // relative scale factor

    // calc final position post-scale
    Vector3 FP = B + C * RS;

    // finally, actually perform the scale/translation
    target.localScale = newScale;
    target.localPosition = FP;
  }
}
