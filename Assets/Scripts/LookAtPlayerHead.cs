using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtPlayerHead : MonoBehaviour
{
    Transform lookAtTarget;
    public Vector3 rotationOffset;
    // Start is called before the first frame update
    void Start()
    {
        var ovrCameraRig = FindAnyObjectByType<OVRCameraRig>();
        if (ovrCameraRig != null)
        {
            lookAtTarget = ovrCameraRig.centerEyeAnchor;
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(lookAtTarget);
        transform.Rotate(rotationOffset, Space.Self);
    }
}
