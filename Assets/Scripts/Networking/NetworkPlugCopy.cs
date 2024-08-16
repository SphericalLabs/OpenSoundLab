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

        otherEnd.SetParent(targetJack.gameObject.transform);
        otherEnd.localPosition = new Vector3(0, -.0175f, 0);
        otherEnd.localRotation = Quaternion.Euler(-90, 0, 0);

        onStartGrabEvents.AddListener(OnGrabedByOther);
    }

    public void OnGrabedByOther()
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
