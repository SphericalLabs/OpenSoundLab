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
  bool isVertical = false;
  bool isHorizontal = false;
  Transform[] transArray;

  Quaternion lastRightHandRotation, lastLeftHandRotation;


  void Awake()
  {
    
  }

  // dragging around the y axis
  public static bool worldDraggedHorizontally(){
    return OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger) > 0.1f && OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger) > 0.1f;
  }

  // tilting the world up and down
  public static bool worldDraggedVertically(){
    return OVRInput.Get(OVRInput.RawAxis1D.LIndexTrigger) > 0.1f && OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger) > 0.1f;    
    }

  void Update()
  {
    
    if (leftManip == null) leftManip = GameObject.Find("LeftHandAnchor").GetComponentInChildren<manipulator>();
    if (rightManip == null) rightManip = GameObject.Find("RightHandAnchor").GetComponentInChildren<manipulator>();

    // don't start the moving if grabbing something else already
    if (!isDragging && (leftManip.isGrabbing() || rightManip.isGrabbing())) return; 

    // begin drag, save start values
    if (!isDragging && worldDraggedHorizontally())
    {
      isDragging = true;

      lastControllerMiddle = getMiddle(leftHandAnchor, rightHandAnchor);
      lastControllerAngle = getAngleBetweenControllers();
      lastControllerDistance = getDistanceBetweenControllers();

      lastLeftHandRotation = leftHandAnchor.rotation;
      lastRightHandRotation = rightHandAnchor.rotation;

    }
    
    // end of drag
    if (isDragging && !worldDraggedHorizontally() )
    {
      isDragging = false;

      // move all children transform to parent, save list of transforms
      transArray = new Transform[transform.childCount];
      
      // foreach(Transform child in transform){ // not working!
      // populate array first, otherwise weird index/list bugs when moving transforms while iterating on them
      for (int i = 0; i < transform.childCount; i++)
      {
        transArray[i] = transform.GetChild(i);
      }

      // move them up
      for (int n = 0; n < transArray.Length; n++)
      {
        transArray[n].parent = transform.parent;
      }

      // fully reset parent transform
      transform.position = Vector3.zero;
      transform.rotation = Quaternion.identity;
      transform.localScale = Vector3.one;

      // move them back down again, burning the global drag transform into them
      for(int n = 0; n < transArray.Length; n++){ 
        transArray[n].parent = transform;
      }

    }

    // do the drag
    if(isDragging){

      //Debug.DrawLine(Vector3.zero, patchPositionAtBeginDrag, new Color(1f, 0f, 0f));
      //Debug.DrawLine(Vector3.zero, new Vector3(Mathf.Sin(Mathf.Deg2Rad * patchAngleAtBeginDrag), 0f, Mathf.Cos(Mathf.Deg2Rad * patchAngleAtBeginDrag)), new Color(0f, 1f, 0f));
      //Debug.DrawLine(Vector3.zero, controllerMiddleAtBeginDrag, new Color(0f, 0f, 1f));
      //Debug.DrawLine(Vector3.zero, new Vector3(Mathf.Sin(Mathf.Deg2Rad * controllerAngleAtBeginDrag), 0f, Mathf.Cos(Mathf.Deg2Rad * controllerAngleAtBeginDrag)), new Color(1f, 1f, 0f));
      //Debug.DrawLine(Vector3.zero, new Vector3(Mathf.Sin(Mathf.Deg2Rad * currentControllerAngle), 0f, Mathf.Cos(Mathf.Deg2Rad * currentControllerAngle)), new Color(1f, 0f, 1f));
      //Debug.DrawLine(Vector3.zero, getMiddle(leftHandAnchor, rightHandAnchor), new Color(0f, 1f, 1f));

      if (worldDraggedVertically())
      {
        if (!isVertical)
        {
          isVertical = true;
          isHorizontal = false;
          // Update last hand rotations
          lastLeftHandRotation = leftHandAnchor.rotation;
          lastRightHandRotation = rightHandAnchor.rotation;
        }
        if (isVertical)
        {
          // Calculate the rotation deltas for both hands
          Quaternion deltaLeftRotation = Quaternion.Inverse(lastLeftHandRotation) * leftHandAnchor.rotation;
          Quaternion deltaRightRotation = Quaternion.Inverse(lastRightHandRotation) * rightHandAnchor.rotation;

          // Average the rotations and project onto the axis between the hands
          Quaternion averagedRotation = Quaternion.Slerp(deltaLeftRotation, deltaRightRotation, 0.5f);
          Vector3 handAxis = leftHandAnchor.position - rightHandAnchor.position;
          Quaternion actualRot = ProjectQuaternion(averagedRotation, handAxis);

          // Apply the rotation
          actualRot.ToAngleAxis(out float angle, out Vector3 axis);
          transform.RotateAround(currentControllerMiddle, axis, angle);

          // Update last hand rotations
          lastLeftHandRotation = leftHandAnchor.rotation;
          lastRightHandRotation = rightHandAnchor.rotation;
        }
      } else {
        currentControllerMiddle = getMiddle(leftHandAnchor, rightHandAnchor);

        // scale
        currentControllerDistance = getDistanceBetweenControllers();
        scaleAround(transform, currentControllerMiddle, transform.localScale * (1f + currentControllerDistance - lastControllerDistance));

        // rotation
        if (!isHorizontal)
        {
          isHorizontal = true;
          isVertical = false;
        }
        if (isHorizontal)
        {
          currentControllerAngle = getAngleBetweenControllers();
          transform.RotateAround(currentControllerMiddle, Vector3.up, lastControllerAngle - currentControllerAngle);
        }

        // translation
        transform.Translate(currentControllerMiddle - lastControllerMiddle, Space.World);

        // for next frame
        lastControllerMiddle = currentControllerMiddle;
        lastControllerAngle = currentControllerAngle;
        lastControllerDistance = currentControllerDistance;
      }

    }
  }

  public Quaternion ProjectQuaternion(Quaternion q1, Vector3 axis)
  {
    // Normalize the axis to ensure it's a unit vector
    axis.Normalize();

    // Decompose the input quaternion into an axis and an angle
    q1.ToAngleAxis(out float angle, out Vector3 rotationAxis);

    // Project the rotation axis onto the given axis
    Vector3 projectedAxis = Vector3.Project(rotationAxis, axis);

    // Recompose the quaternion with the projected axis and original angle
    Quaternion projectedQuaternion = Quaternion.AngleAxis(angle, projectedAxis);

    return projectedQuaternion;
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
