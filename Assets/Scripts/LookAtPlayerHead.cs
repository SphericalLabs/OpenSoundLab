using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtPlayerHead : MonoBehaviour
{
    public Transform playerParent;
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
        if (playerParent != null)
        {
            transform.position = playerParent.position + Vector3.up * 0.012f;
        }
        transform.LookAt(lookAtTarget);
        transform.Rotate(rotationOffset, Space.Self);
    }

}
