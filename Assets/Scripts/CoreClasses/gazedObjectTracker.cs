using Unity.XR.Oculus;
using UnityEngine;
using static manipObject;

public class gazedObjectTracker : MonoBehaviour
{

  private static gazedObjectTracker instance;
  public manipObject gazedAtManipObject;
  public Vector3 correction;
  public GameObject gazeIndicator;
  public GameObject calibIndicator;
  public GameObject calibrationPlane;
  public GameObject calibrationPlaneCenter;
  public GameObject leftGazeAnchor, rightGazeAnchor;
  public GameObject centerEyeAnchor;
  //public Vector3 centerEyeCompensation;

  public GazeMode currentMode;
  public enum GazeMode
  {
    Off,
    FixedGaze,
    TrackedGaze
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

    currentMode = GazeMode.FixedGaze;

    if (
    (Unity.XR.Oculus.Utils.GetSystemHeadsetType() == SystemHeadset.Meta_Quest_Pro
    || Unity.XR.Oculus.Utils.GetSystemHeadsetType() == SystemHeadset.Meta_Link_Quest_Pro)
    && Unity.XR.Oculus.Utils.IsEyeTrackingPermissionGranted()
    )
    {
      currentMode = GazeMode.TrackedGaze;
    }

    calibrationPlane = GameObject.Find("GazeCalibPlane");
    calibrationPlaneCenter = GameObject.Find("GazeCalibCenter");
  }

  public static gazedObjectTracker Instance
  {
    get
    {
      return instance;
    }
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
  Vector3 avgPosition, avgDirection;

  void Update()
  {
    if (currentMode == GazeMode.Off) return;

    // Reset every frame
    gazedAtManipObject = null;
    gazeIndicator.SetActive(false);
    layerMask = 1 << 9; // LayerMask 9 for manipOnly
    
    if (currentMode == GazeMode.FixedGaze)
    {
      
      gazeRay = new Ray(centerEyeAnchor.transform.position, centerEyeAnchor.transform.forward);

      runCalibration();
      
      // turn localPosition offset to current worldRotation
      worldOrientedOffset = centerEyeAnchor.transform.rotation * correction;

      gazeRay = new Ray(centerEyeAnchor.transform.position + worldOrientedOffset, centerEyeAnchor.transform.forward);
      
    } else if(currentMode == GazeMode.TrackedGaze) { 

      // Calculate average gaze data from both eyes
      avgPosition = (leftGazeAnchor.transform.position + rightGazeAnchor.transform.position) / 2;
      avgDirection = (leftGazeAnchor.transform.forward + rightGazeAnchor.transform.forward).normalized;
      gazeRay = new Ray(avgPosition, avgDirection);

      runCalibration();

      // turn localPosition offset to current worldRotation
      worldOrientedOffset = centerEyeAnchor.transform.rotation * correction;
      // note: maybe using the rotation of centerEyeAnchor here is not optimal, maybe construct a Quaternion from left and right gaze?
      gazeRay = new Ray(avgPosition + worldOrientedOffset, avgDirection);

    }

    gazeIndicator.SetActive(false);

    layerMask = ~0; // not 0, i.e. all layers
    if (Physics.SphereCast(gazeRay, 0.015f, out hit, Mathf.Infinity, layerMask))
    {
      gazeIndicator.transform.position = hit.point;
      gazeIndicator.SetActive(true);

      gazedAtManipObject = hit.collider.GetComponent<manipObject>();
      gazedAtTrashcan = hit.collider.GetComponent<trashcan>();
    }
  }

  public trashcan gazedAtTrashcan;

  private void runCalibration(){

    calibIndicator.SetActive(false);

    if (Physics.Raycast(gazeRay, out hit, Mathf.Infinity, layerMask))
    {
      if (hit.collider.gameObject == calibrationPlane)
      {
        calibIndicator.transform.position = hit.point;
        calibIndicator.SetActive(true);

        if (Time.frameCount % 15 == 0 && isFullPressed())
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

  bool isHalfPressed()
  {
    return (Input.GetAxis("triggerL") > 0.05 || Input.GetAxis("triggerR") > 0.05);
  }

  bool isFullPressed()
  {
    return (Input.GetAxis("triggerL") > 0.7 || Input.GetAxis("triggerR") > 0.7);
  }
}

