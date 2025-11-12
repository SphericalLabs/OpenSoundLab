using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkPlugCopy : manipObject
{
    public Transform otherEnd;
    public LineRenderer lineRenderer;

    private NetworkPlayerPlugHand networkPlayerPlugHand;
    private omniJack targetJack;

    public void Initialize(NetworkPlayerPlugHand networkPlayerPlugHand, omniJack targetJack)
    {
        this.networkPlayerPlugHand = networkPlayerPlugHand;
        this.targetJack = targetJack;

        // this code here is for the preview of plugs that are currently being patched by another client
        // this code does not have the full wire width and plug size matching that omniPlug has, but it currently does the job
        // as soon as the client releases and fully patches the new cable omniplug.Activate() is called and properly sets everything up

        otherEnd.transform.parent = targetJack.gameObject.transform;
        otherEnd.transform.localScale = Vector3.one;
        otherEnd.localPosition = new Vector3(0, -0.03f, 0);        
        otherEnd.localRotation = Quaternion.Euler(-90, 0, 0);

        onStartGrabEvents.AddListener(OnGrabbedByOther);
    }

    public void OnGrabbedByOther()
    {
        //delete this and send passing to network
        if (networkPlayerPlugHand != null)
        {
            networkPlayerPlugHand.PassToOtherHand();
            targetJack.CreatePlugInHand(manipulatorObjScript);
            //start new omnoPlug grab from target jack
            //copy this offset to hand to new jack
            Destroy(this.gameObject);
        }
    }

    private void Update()
    {
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, otherEnd.position);
    }

    private void OnDestroy()
    {
        Destroy(otherEnd.gameObject);
    }
}
