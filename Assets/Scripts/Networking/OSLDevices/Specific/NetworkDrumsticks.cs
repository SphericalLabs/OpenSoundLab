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
            drumsticks[i].onStartFollowEvent.AddListener(delegate { OnChangeStickFollow(index, true); });
            drumsticks[i].onEndFollowEvent.AddListener(delegate { OnChangeStickFollow(index, false); });
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
    void SetDrumstickHandTarget(NetworkIdentity targetPlayer, bool leftHand)
    {
        //find player
        //let stick follow hand
        //disable grabbable

        //if target null reenable grabbable
    }

    public void OnChangeStickFollow(int index, bool grabbed)
    {
        GetLocalPlayerHand(index, grabbed, out NetworkIdentity playerIdentity, out bool leftHand);

        if (isServer)
        {
            UpdateHandTarget(index, playerIdentity, leftHand);
        }
        else
        {
            CmdOnChangeStickFollow(index, playerIdentity, leftHand);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdOnChangeStickFollow(int index, NetworkIdentity target, bool leftHand)
    {
        UpdateHandTarget(index, target, leftHand);
    }

    void UpdateHandTarget(int index, NetworkIdentity target, bool leftHand)
    {
        Debug.Log($"Update Hand Target of stick {index} to {target} by lefthand {leftHand}");
        if (grabberHand[index].player != target || grabberHand[index].isLeftHand != leftHand)
        {
            var newTarget = new HandTraget();
            newTarget.player = target;
            newTarget.isLeftHand = leftHand;

            grabberHand[index] = newTarget;
        }
    }

    private void GetLocalPlayerHand(int index,bool isGrabbed, out NetworkIdentity networkIdentity, out bool leftHand)
    {
        networkIdentity = null;
        leftHand = false;

        if (isGrabbed && index >= 0 && index < drumsticks.Length)
        {
            leftHand = NetworkMenuManager.Instance.localPlayer.IsObjectGrabbedByLeftHand(drumsticks[index]);
            networkIdentity = NetworkMenuManager.Instance.localPlayer.netIdentity;
        }
    }
}
