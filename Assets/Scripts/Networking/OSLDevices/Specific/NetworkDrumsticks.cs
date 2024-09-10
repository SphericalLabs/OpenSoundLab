using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public struct HandTraget
{
    public NetworkIdentity player;
    public bool isLeftHand;
}

public class NetworkDrumsticks : NetworkBehaviour
{
    public drumstick[] drumsticks;

    public SyncList<HandTraget> grabberHand = new SyncList<HandTraget>();

    public override void OnStartClient()
    {
        grabberHand.Callback += OnHandTargetUpdated;

        // Process initial SyncList payload
        for (int i = 0; i < drumsticks.Length; i++)
        {
            int index = i;
            drumsticks[i].onStartGrabEvents.AddListener(delegate { OnChangeHandTarget(index, true); });
            drumsticks[i].onEndGrabEvents.AddListener(delegate { OnChangeHandTarget(index, false); });
            grabberHand.Add(new HandTraget());
        }
    }

    void OnHandTargetUpdated(SyncList<HandTraget>.Operation op, int index, HandTraget oldItem, HandTraget newItem)
    {
        switch (op)
        {
            case SyncList<HandTraget>.Operation.OP_ADD:
                break;
            case SyncList<HandTraget>.Operation.OP_INSERT:
                break;
            case SyncList<HandTraget>.Operation.OP_REMOVEAT:
                break;
            case SyncList<HandTraget>.Operation.OP_SET:
                //when hand is added
                break;
            case SyncList<HandTraget>.Operation.OP_CLEAR:
                break;
        }
    }

    //update drumsticks on other devices
    void SetDrumstickHandTarget()
    {
        //find player
        //let stick follow hand
        //disable grabbable

        //if target null reenable grabbable
    }

    public void OnChangeHandTarget(int index, bool grabbed)
    {
        //find and set target player

        //set if lefthand
    }
}
