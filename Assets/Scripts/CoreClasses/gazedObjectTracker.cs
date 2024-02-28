using Unity.XR.Oculus;
using UnityEngine;
using static manipObject;

public class gazedObjectTracker : MonoBehaviour
{

  private static gazedObjectTracker instance;
  public manipObject gazedAtManipObject;
  public Vector3 correction;
  public GameObject centerIndicator, calibIndicator, leftIndicator;
  public GameObject calibrationPlane;
  public GameObject calibrationPlaneCenter;
  public GameObject leftGazeAnchor, rightGazeAnchor;
  public GameObject centerEyeAnchor;

  public Vector3Averager averagedCenterPos, averagedCenterDir, averagedLeftPos, averagedLeftDir;

  public GazeMode currentMode = GazeMode.Off; // will be overriden in Unity Editor, should be set there for now, later it will be set in the settings menu

  public enum GazeMode
  {
    Off,
    FixedGaze,
    TrackedGaze
    // changing the sequence of these will break the PlayerPrefs
  }

  void Awake()
  {
    // Check if instance already exists and set it if it doesn't
    if (instance == null)
    {
      instance = this;
    }
    else if (instance != this)
    {
      // Destroy this instance because it is a duplicate
      Destroy(gameObject);
    }

    // read the gaze prefs
    if (PlayerPrefs.HasKey("GazeMode")) 
    {
      currentMode = (GazeMode)PlayerPrefs.GetInt("GazeMode");      
    }
    else if(isEyeTrackingCapable()) {
      // default to trackedGaze if eye tracking is available
      currentMode = GazeMode.TrackedGaze;
    }

    if (currentMode == GazeMode.TrackedGaze)
    {
      if(!isEyeTrackingCapable()) currentMode = GazeMode.FixedGaze; // fallback to fixedGaze
    }

    calibrationPlane = GameObject.Find("GazeCalibPlane");
    calibrationPlaneCenter = GameObject.Find("GazeCalibCenter");
    centerEyeAnchor = GameObject.Find("CenterEyeAnchor");

    if(currentMode == GazeMode.Off) calibrationPlane.SetActive(false);  // hide the calibration plane

    averagedCenterPos = new Vector3Averager(4);
    averagedCenterDir = new Vector3Averager(4);
    averagedLeftPos = new Vector3Averager(4);
    averagedLeftDir = new Vector3Averager(4);
    
  }

  public bool isEyeTrackingCapable(){
    return (Unity.XR.Oculus.Utils.GetSystemHeadsetType() == SystemHeadset.Meta_Quest_Pro
      || Unity.XR.Oculus.Utils.GetSystemHeadsetType() == SystemHeadset.Meta_Link_Quest_Pro)
      && Unity.XR.Oculus.Utils.IsEyeTrackingPermissionGranted();
  }

  public static gazedObjectTracker Instance
  {
    get
    {
      return instance;
    }
  }

  public void toggleGaze()
  {
    // Determine the new mode based on the current mode and whether eye tracking is available.
    if (currentMode == GazeMode.Off)
    {
      currentMode = isEyeTrackingCapable() ? GazeMode.TrackedGaze : GazeMode.FixedGaze;
    }
    else
    {
      currentMode = GazeMode.Off;

      // clear off all currently ongoing gaze-based interations
      gazedAtManipObject = null;
      foreach( manipulator mani in FindObjectsByType<manipulator>(FindObjectsSortMode.None) ){
        mani.SetTrigger(false);
        mani.ForceRelease();
        //mani.wasGazeBased = false;
        //mani.selectedObject.setGrab(false, null);
        //mani.selectedObject.setSelect(false, null);
        //mani.selectedObject = null;
        //mani.selectedTransform = null;
      }
    }

    // Determine the active state of components based on whether the gaze mode is Off.
    bool isActive = currentMode != GazeMode.Off;

    // Apply the active state to calibrationPlane and calibIndicator.
    calibrationPlane.SetActive(isActive);
    calibIndicator.SetActive(isActive);
    centerIndicator.SetActive(isActive);
    

    // Save the current mode to PlayerPrefs.
    PlayerPrefs.SetInt("GazeMode", (int)currentMode);
  }


  // Start is called before the first frame update
  void Start()
  {
    if (currentMode != GazeMode.Off)
    {
      if (PlayerPrefs.HasKey("CorrectionX"))
      {
        correction.x = PlayerPrefs.GetFloat("CorrectionX");
      }

      if (PlayerPrefs.HasKey("CorrectionY"))
      {
        correction.y = PlayerPrefs.GetFloat("CorrectionY");
      }
    }
  }

  Ray gazeRay;
  RaycastHit hit;
  int layerMask;
  Vector3 worldOrientedOffset;
  Vector3 centerPosition, centerDirection;

  void Update()
  {
    if (currentMode == GazeMode.Off) return;

    // Reset every frame
    gazedAtManipObject = null;
    centerIndicator.SetActive(false);
    //layerMask = 1 << 9; // LayerMask 9 for manipOnly
    layerMask = ~(1 << 16); // all besides layer 16

    if (currentMode == GazeMode.FixedGaze)
    {
      
      gazeRay = new Ray(centerEyeAnchor.transform.position, centerEyeAnchor.transform.forward);

      runCalibration();
      
      // turn localPosition offset to current worldRotation
      worldOrientedOffset = centerEyeAnchor.transform.rotation * correction;

      // prepare ray for actual gaze tracking
      gazeRay = new Ray(centerEyeAnchor.transform.position + worldOrientedOffset, centerEyeAnchor.transform.forward);
      
    } else if(currentMode == GazeMode.TrackedGaze) { 

      // Calculate average gaze data from both eyes
      centerPosition = (leftGazeAnchor.transform.position + rightGazeAnchor.transform.position) / 2;
      centerDirection = (leftGazeAnchor.transform.forward + rightGazeAnchor.transform.forward).normalized;
      averagedCenterPos.Add(centerPosition);
      averagedCenterDir.Add(centerDirection);
      gazeRay = new Ray(averagedCenterPos.GetAverage(), averagedCenterDir.GetAverage());

      runCalibration();

      // turn localPosition offset to current worldRotation
      worldOrientedOffset = centerEyeAnchor.transform.rotation * correction;
      // note: maybe using the rotation of centerEyeAnchor here is not optimal, maybe construct a Quaternion from left and right gaze?
      

      // left eye only viz
      if(false){          
        averagedLeftPos.Add(leftGazeAnchor.transform.position);
        averagedLeftDir.Add(leftGazeAnchor.transform.forward);

        leftIndicator.SetActive(false);

        gazeRay = new Ray(averagedLeftPos.GetAverage() /*+ worldOrientedOffset*/, averagedLeftDir.GetAverage());

        if (Physics.SphereCast(gazeRay, 0.01f, out hit, 10f, layerMask))
        {
          leftIndicator.transform.position = hit.point;
          leftIndicator.SetActive(true);        
        }
      }

      // prepare ray for actual gaze tracking
      gazeRay = new Ray(averagedCenterPos.GetAverage() + worldOrientedOffset, averagedCenterDir.GetAverage());
    }

    centerIndicator.SetActive(false);

    if (Physics.SphereCast(gazeRay, 0.01f, out hit, 10f, layerMask))
    {
      centerIndicator.transform.position = hit.point;
      centerIndicator.SetActive(true);

      gazedAtManipObject = hit.collider.GetComponent<manipObject>();
      gazedAtTrashcan = hit.collider.GetComponent<trashcan>();
    }


  }

  public trashcan gazedAtTrashcan;

  private void runCalibration(){

    calibIndicator.SetActive(false);

    if (Physics.Raycast(gazeRay, out hit, 5f, layerMask))
    {
      if (hit.collider.gameObject == calibrationPlane)
      {
        calibIndicator.transform.position = hit.point;
        calibIndicator.SetActive(true);

        if (Time.frameCount % 15 == 0 && OSLInput.getInstance().isAnyTriggerFullPressed())
        {
          Vector3 localizedHitPoint = centerEyeAnchor.transform.InverseTransformPoint(hit.point);
          Vector3 localizedPlaneCenter = centerEyeAnchor.transform.InverseTransformPoint(calibrationPlaneCenter.transform.position);
          correction.x = localizedPlaneCenter.x - localizedHitPoint.x;
          correction.y = localizedPlaneCenter.y - localizedHitPoint.y;
          PlayerPrefs.SetFloat("CorrectionX", correction.x);
          PlayerPrefs.SetFloat("CorrectionY", correction.y);
          PlayerPrefs.Save();
        }
        return;
      }
    }
  }

}


public class Vector3Averager
{
  private Vector3[] values;
  private int currentIndex = 0;
  private int count = 0;

  public Vector3Averager(int size)
  {
    values = new Vector3[size];
  }

  public void Add(Vector3 newValue)
  {
    values[currentIndex] = newValue;
    currentIndex = (currentIndex + 1) % values.Length;

    if (count < values.Length)
    {
      count++;
    }
  }

  public Vector3 GetAverage()
  {
    Vector3 sum = Vector3.zero;

    for (int i = 0; i < count; i++)
    {
      sum += values[i];
    }

    return (count > 0) ? sum / count : Vector3.zero;
  }
}

