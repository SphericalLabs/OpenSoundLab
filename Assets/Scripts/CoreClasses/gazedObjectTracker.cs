
using UnityEngine;

public class gazedObjectTracker : MonoBehaviour
{

    private static gazedObjectTracker instance;
    public manipObject gazedAtManipObject;
    public Vector3 correction;

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

      // Optionally, persist this instance across scenes
      // DontDestroyOnLoad(gameObject);
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
        
    }

    // Update is called once per frame
    void Update()
    {
        // reset every frame
        gazedAtManipObject = null;

        transform.Translate(correction); // shift 2cm 

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        int layerMask = 1 << 9; // layerMask 9 = manipOnly

        if (Physics.Raycast(ray, out hit, 2f, layerMask)) 
        {
          manipObject targetObject = hit.collider.GetComponent<manipObject>();
          if (targetObject != null)
          {            
            gazedAtManipObject = targetObject;
            //targetObject.setState(manipObject.manipState.selected);
            //Debug.Log("gaze set selected");
          }
        }
    }
}

