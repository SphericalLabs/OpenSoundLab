using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldDragController : NetworkBehaviour
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

  void Awake(){
    // These references to the manipulators will be reused by VRNetworkPlayer so don't skip finding them if on client
    if (leftManip == null) leftManip = GameObject.Find("LeftHandAnchor").GetComponentInChildren<manipulator>();
    if (rightManip == null) rightManip = GameObject.Find("RightHandAnchor").GetComponentInChildren<manipulator>();
  }

  void Update()
  {
    // Skip if on client
    if (!isServer) return;
    if (leftHandAnchor == null) leftHandAnchor = GameObject.Find("LeftHandAnchor").transform;
    if (centerEyeAnchor == null) centerEyeAnchor = GameObject.Find("CenterEyeAnchor").transform;
    if (rightHandAnchor == null) rightHandAnchor = GameObject.Find("RightHandAnchor").transform;

    // don't start the moving if grabbing something else already
    if (!isDragging && (leftManip.isGrabbing() || rightManip.isGrabbing())) return; 

    // begin drag, save start values
    if (!isDragging && OSLInput.getInstance().areBothSidesPressed())
    {
      isDragging = true;

      getCurrentValuesHorizontal();
      storeCurrentValuesHorizontal();
    }
    
    // end of drag
    if (isDragging && !OSLInput.getInstance().areBothSidesPressed())
    {
      isDragging = false;
      isVertical = false;
      isHorizontal = false;
      bakeTransforms();
    }

    // do the drag
    if(isDragging){

      getCurrentValuesHorizontal(); // run twice when starting...

      if (OSLInput.getInstance().areBothSidesPressed() && !OSLInput.getInstance().areBothTriggersFullPressed())
      {
        
        if (!isHorizontal) // init horizontal
        {
          isHorizontal = true;
          isVertical = false;

          getCurrentValuesHorizontal();
          storeCurrentValuesHorizontal();
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
          storeCurrentValuesHorizontal();
        }
        
      } else if (OSLInput.getInstance().areBothSidesPressed() && OSLInput.getInstance().areBothTriggersFullPressed()) {

          if (!isVertical) // init vertical
          {
            isHorizontal = false;
            isVertical = true;

            // take snapshots at beginning so that turning the head while dragging is not yielding weird results
            tiltAxis = centerEyeAnchor.right; 
            rollAxis = centerEyeAnchor.forward;
            centerEyeAnchorSnapshot = new TransformSnapshot(centerEyeAnchor); // this is offering world to local projection
            //rotationPoint = getMiddle(leftHandAnchor, rightHandAnchor); // in world space
            rotationPoint = centerEyeAnchor.position;
                        
            getCurrentValuesVertical();
            storeCurrentValuesVertical();            
          }

          if (isVertical) // running vertical
          {
            currentControllerMiddle = centerEyeAnchorSnapshot.WorldToLocal(getMiddle(leftHandAnchor, rightHandAnchor));

            // rotation
            float rollAngle = Utils.map((currentControllerMiddle - lastControllerMiddle).x, -1f, 1f, 90f, -90f);
            float tiltAngle = Utils.map((currentControllerMiddle - lastControllerMiddle).y, -0.3f, 0.3f, 90f, -90f);

            transform.RotateAround(rotationPoint, rollAxis, rollAngle);
            transform.RotateAround(rotationPoint, tiltAxis, tiltAngle);

            // for next frame
            storeCurrentValuesVertical();
          }

        }
    }
  }
  
  Vector3 tiltAxis;
  Vector3 rollAxis;
  TransformSnapshot centerEyeAnchorSnapshot;
  Vector3 rotationPoint;

  void getCurrentValuesHorizontal(){
    currentControllerMiddle = getMiddle(leftHandAnchor, rightHandAnchor);
    currentControllerAngle = getAngleBetweenControllersXZ();
    currentControllerDistance = getDistanceBetweenControllers();
  }
  
  void storeCurrentValuesHorizontal(){
    lastControllerMiddle = currentControllerMiddle;
    lastControllerAngle = currentControllerAngle;
    lastControllerDistance = currentControllerDistance;
  }

  void getCurrentValuesVertical(){
    currentControllerMiddle = centerEyeAnchorSnapshot.WorldToLocal(getMiddle(leftHandAnchor, rightHandAnchor)); // in local space 
  }

  void storeCurrentValuesVertical(){
    lastControllerMiddle = currentControllerMiddle;
  }

  float getAngleBetweenControllersXZ()
  {
    // on x,z plane
    return Mathf.Rad2Deg * Mathf.Atan2(leftHandAnchor.transform.position.z - rightHandAnchor.transform.position.z, leftHandAnchor.transform.position.x - rightHandAnchor.transform.position.x);
  }
  
  void bakeTransforms(){
    // move all children transform to parent, save list of transforms
    transArray = new Transform[transform.childCount];

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


public class TransformSnapshot
{
  public Vector3 position;
  public Quaternion rotation;
  public Vector3 scale;

  public TransformSnapshot(Transform transform)
  {
    position = transform.position;
    rotation = transform.rotation;
    scale = transform.localScale;
  }

  public Vector3 WorldToLocal(Vector3 worldPoint)
  {
    Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale).inverse;
    return matrix.MultiplyPoint3x4(worldPoint);
  }

  public Vector3 LocalToWorld(Vector3 localPoint)
  {
    Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);
    return matrix.MultiplyPoint3x4(localPoint);
  }

  public Vector3 TransformDirection(Vector3 localDirection)
  {
    return rotation * localDirection;
  }

  public Quaternion TransformRotation(Quaternion localRotation)
  {
    return rotation * localRotation;
  }
}
