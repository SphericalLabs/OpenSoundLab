using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldDragController : MonoBehaviour
{
  public manipulator leftManip, rightManip;
  public Transform leftHandAnchor, rightHandAnchor;
  public Transform centerEyeAnchor;

  Vector3 currentControllerMiddle, lastControllerMiddle;
  float currentControllerAngle, lastControllerAngle, currentControllerDistance, lastControllerDistance;
  bool isDragging = false;
  bool isVertical = false;
  bool isHorizontal = false;
  Transform[] transArray;



  void Update()
  {
    
    if (leftManip == null) leftManip = GameObject.Find("LeftHandAnchor").GetComponentInChildren<manipulator>();
    if (rightManip == null) rightManip = GameObject.Find("RightHandAnchor").GetComponentInChildren<manipulator>();

    // don't start the moving if grabbing something else already
    if (!isDragging && (leftManip.isGrabbing() || rightManip.isGrabbing())) return; 

    // begin drag, save start values
    if (!isDragging && bothSidesDown())
    {
      isDragging = true;

      getCurrentValuesHorizontal();
      storeCurrentValues();
    }
    
    // end of drag
    if (isDragging && !bothSidesDown() )
    {
      isDragging = false;
      isVertical = false;
      isHorizontal = false;
      bakeTransforms();
    }

    // do the drag
    if(isDragging){

      getCurrentValuesHorizontal(); // run twice when starting...

      if (bothSidesDown() && !bothTriggersDown())
      {
        
        if (!isHorizontal) // init horizontal
        {
          isHorizontal = true;
          isVertical = false;

          getCurrentValuesHorizontal();
          storeCurrentValues();
        }

        
        if (isHorizontal) // run horizontal
        {
          // rotate
          currentControllerAngle = getAngleBetweenControllersXZ();
          transform.RotateAround(currentControllerMiddle, Vector3.up, lastControllerAngle - currentControllerAngle);

          currentControllerMiddle = getMiddle(leftHandAnchor, rightHandAnchor);

          // scale
          currentControllerDistance = getDistanceBetweenControllers();
          scaleAround(transform, currentControllerMiddle, transform.localScale * (1f + currentControllerDistance - lastControllerDistance));

          // translation
          transform.Translate(currentControllerMiddle - lastControllerMiddle, Space.World);

          // for next frame
          storeCurrentValues();
        }
        
      } else if (bothSidesDown() && bothTriggersDown()) {

          if (!isVertical) // init vertical
          {
            isHorizontal = false;
            isVertical = true;

            getCurrentValuesVertical();
            storeCurrentValues();

            rotationAxis = centerEyeAnchor.right; // take snapshot of head position at beginning
            chest = centerEyeAnchor.position - centerEyeAnchor.up * 0.3f;
        }

          if (isVertical) // running vertical
          {

            currentControllerMiddle = getMiddle(leftHandAnchor, rightHandAnchor);

            // rotation
            currentControllerAngle = getAngleBetweenControllersCenterEye();
            transform.RotateAround(centerEyeAnchor.position, rotationAxis, currentControllerAngle - lastControllerAngle);

            // translation
            //transform.Translate(currentControllerMiddle - lastControllerMiddle, Space.World);

            // for next frame
            storeCurrentValues();
          }

        }
    }
  }
  
  Vector3 rotationAxis;
  Vector3 chest;

  void getCurrentValuesHorizontal(){
    currentControllerMiddle = getMiddle(leftHandAnchor, rightHandAnchor);
    currentControllerAngle = getAngleBetweenControllersXZ();
    currentControllerDistance = getDistanceBetweenControllers();
  }
  
  void getCurrentValuesVertical(){
    currentControllerMiddle = getMiddle(leftHandAnchor, rightHandAnchor);
    //currentControllerAngle = getAngleBetweenControllersZY();
    currentControllerAngle = getAngleBetweenControllersCenterEye();
    currentControllerDistance = getDistanceBetweenControllers();
  }

  void storeCurrentValues(){
    lastControllerMiddle = currentControllerMiddle;
    lastControllerAngle = currentControllerAngle;
    lastControllerDistance = currentControllerDistance;
  }

  float getAngleBetweenControllersXZ()
  {
    // on x,z plane
    return Mathf.Rad2Deg * Mathf.Atan2(leftHandAnchor.transform.position.z - rightHandAnchor.transform.position.z, leftHandAnchor.transform.position.x - rightHandAnchor.transform.position.x);
  }

  float getAngleBetweenControllersZY()
  {
    Vector3 localizedLeft = centerEyeAnchor.InverseTransformPoint(leftHandAnchor.position);
    Vector3 localizedRight = centerEyeAnchor.InverseTransformPoint(rightHandAnchor.position);

    // on local z,y plane from camera view point of centerEyeAnchor
    return Mathf.Rad2Deg * Mathf.Atan2(localizedLeft.z - localizedRight.z, localizedLeft.y - localizedRight.y);
  }

  float getAngleBetweenControllersCenterEye()
  {
    Vector3 point1 = centerEyeAnchor.InverseTransformPoint(currentControllerMiddle);
    //Vector3 point2 = centerEyeAnchor.InverseTransformPoint(centerEyeAnchor);
    Vector3 point2 = Vector3.zero;

    // on local z,y plane from camera view point of centerEyeAnchor
    return Mathf.Rad2Deg * Mathf.Atan2(point1.z - point2.z, point1.y - point2.y);
  }


  void bakeTransforms(){
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

    // move them back down again, baking the global drag transform into them
    for (int n = 0; n < transArray.Length; n++)
    {
      transArray[n].parent = transform;
    }
  }

  // dragging around the y axis
  public static bool bothSidesDown() // similar code is in manipulator.cs
  {
    return OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger) > 0.1f && OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger) > 0.1f;
  }

  // tilting the world up and down
  public static bool bothTriggersDown()
  {
    return OVRInput.Get(OVRInput.RawAxis1D.LIndexTrigger) > 0.1f && OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger) > 0.1f;
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
