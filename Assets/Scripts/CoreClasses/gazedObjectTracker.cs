
using UnityEngine;

public class gazedObjectTracker : MonoBehaviour
{

  private static gazedObjectTracker instance;
  public manipObject gazedAtManipObject;
  public Vector3 correction;
  public GameObject gazeIndicator;
  public GameObject calibrationPlane;
  public GameObject calibrationPlaneCenter;
  public GameObject leftGazeAnchor, rightGazeAnchor;

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
    if (PlayerPrefs.HasKey("CorrectionX"))
    {
      correction.x = PlayerPrefs.GetFloat("CorrectionX");
    }

    if (PlayerPrefs.HasKey("CorrectionY"))
    {
      correction.y = PlayerPrefs.GetFloat("CorrectionY");
    }
  }

  void Update()
  {
    // Reset every frame
    gazedAtManipObject = null;
    gazeIndicator.SetActive(false);

    // Calculate average gaze data from both eyes
    Vector3 avgPosition = (leftGazeAnchor.transform.position + rightGazeAnchor.transform.position) / 2;
    Vector3 avgDirection = (leftGazeAnchor.transform.forward + rightGazeAnchor.transform.forward).normalized;
    Ray avgRay = new Ray(avgPosition, avgDirection);

    RaycastHit hit;
    int layerMask = 1 << 9; // LayerMask 9 for manipOnly

    // Calibration cast
    if (Physics.Raycast(avgRay, out hit, 2f, layerMask))
    {
      if (hit.collider.gameObject == calibrationPlane)
      {
        gazeIndicator.transform.position = hit.point;
        gazeIndicator.SetActive(true);

        if (Time.frameCount % 30 == 0 && isFullPressed())
        {
          Vector3 localizedHitPoint = transform.InverseTransformPoint(hit.point);
          Vector3 localizedPlaneCenter = transform.InverseTransformPoint(calibrationPlaneCenter.transform.position);
          correction.x = localizedPlaneCenter.x - localizedHitPoint.x;
          correction.y = localizedPlaneCenter.y - localizedHitPoint.y;
          PlayerPrefs.SetFloat("CorrectionX", correction.x);
          PlayerPrefs.SetFloat("CorrectionY", correction.y);
          PlayerPrefs.Save();
        }
        return;
      }
    }

    // Apply correction and do a SphereCast
    transform.Translate(correction);
    avgRay = new Ray(avgPosition, avgDirection);

    layerMask = ~0; // Adjust the layer mask as needed
    gazeIndicator.SetActive(false);

    if (Physics.SphereCast(avgRay, 0.015f, out hit, 2f, layerMask))
    {
      manipObject targetObject = hit.collider.GetComponent<manipObject>();
      if (targetObject != null)
      {
        gazedAtManipObject = targetObject;
        gazeIndicator.transform.position = hit.point;
        gazeIndicator.SetActive(true);
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

