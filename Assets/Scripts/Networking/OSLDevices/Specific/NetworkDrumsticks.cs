using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public struct HoldingHand
{
    public NetworkIdentity player;
    public bool isLeftHand;
}

public class NetworkDrumsticks : NetworkBehaviour
{
    public drumstick[] drumsticks;

    public drumpad drumpad;

    public SyncList<HoldingHand> holdingHands = new SyncList<HoldingHand>();


    private float[] lastHoldTimes;

    private void Start()
    {
        lastHoldTimes = new float[drumsticks.Length];
        for (int i = 0; i < drumsticks.Length; i++)
        {
            int index = i;
            drumsticks[i].onStartFollowEvent.AddListener(delegate { OnChangeStickFollow(index, true); });
            drumsticks[i].onEndFollowEvent.AddListener(delegate { OnChangeStickFollow(index, false); });

            lastHoldTimes[i] = -1f;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        for (int i = 0; i < drumsticks.Length; i++)
        {
            holdingHands.Add(new HoldingHand());
        }
        drumpad.onHitEvent.AddListener(OnHitDrumpad);
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            holdingHands.Callback += OnHandTargetUpdated;
            for (int i = 0; i < holdingHands.Count; i++)
            {
                OnHandTargetUpdated(SyncList<HoldingHand>.Operation.OP_ADD, i, holdingHands[i], holdingHands[i]);
            }
        }
    }


    private void OnDestroy()
    {
        for (int i = 0; i < drumsticks.Length; i++)
        {
            if (drumsticks[i].transform.parent != drumsticks[i].masterObj)
            {
                Destroy(drumsticks[i].gameObject);
            }
        }
    }

    void OnHandTargetUpdated(SyncList<HoldingHand>.Operation op, int index, HoldingHand oldItem, HoldingHand newItem)
    {
        switch (op)
        {
            case SyncList<HoldingHand>.Operation.OP_ADD:
                //when hand is added
                SetDrumstickHandTarget(index, newItem.player, newItem.isLeftHand);
                break;
            case SyncList<HoldingHand>.Operation.OP_INSERT:
                break;
            case SyncList<HoldingHand>.Operation.OP_REMOVEAT:
                break;
            case SyncList<HoldingHand>.Operation.OP_SET:
                //when hand is added
                SetDrumstickHandTarget(index, newItem.player, newItem.isLeftHand);
                break;
            case SyncList<HoldingHand>.Operation.OP_CLEAR:
                break;
        }
    }

    //update drumsticks on other devices
    void SetDrumstickHandTarget(int index, NetworkIdentity targetPlayer, bool leftHand)
    {
        Debug.Log("Try to set stick hand");
        if (drumsticks[index].curState != manipObject.manipState.grabbed && IsEndHoldCooldownOver(index)) 
        {
            Debug.Log($"Set stick hand target {index}, {targetPlayer}, {leftHand}");
            if (targetPlayer == null)
            {
                drumsticks[index].CanBeGrabed = true;

                drumsticks[index].transform.parent = drumsticks[index].masterObj;
                drumsticks[index].StartCoroutine(drumsticks[index].returnRoutine());
            }
            else if (targetPlayer.TryGetComponent<VRNetworkPlayer>(out VRNetworkPlayer networkPlayer))
            {
                var hand = networkPlayer.TargetNetworkHand(leftHand);
                drumsticks[index].transform.parent = hand;

                drumsticks[index].transform.localPosition = Vector3.zero;
                drumsticks[index].transform.localRotation = Quaternion.identity;

                drumsticks[index].CanBeGrabed = false;
            }
        }
    }

    public void OnChangeStickFollow(int index, bool grabbed)
    {
        if (!grabbed)
        {
            UpdateLastGrabedTime(index);
        }

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
        SetDrumstickHandTarget(index, target, leftHand);
    }

    void UpdateHandTarget(int index, NetworkIdentity target, bool leftHand)
    {
        Debug.Log($"Update Hand Target of stick {index} to {target} by lefthand {leftHand}");
        if (holdingHands[index].player != target || holdingHands[index].isLeftHand != leftHand)
        {
            var newTarget = new HoldingHand();
            newTarget.player = target;
            newTarget.isLeftHand = leftHand;

            holdingHands[index] = newTarget;
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


    public void UpdateLastGrabedTime(int index)
    {
        if (index >= 0 && index < lastHoldTimes.Length)
        {
            lastHoldTimes[index] = Time.time;
        }
    }

    private bool IsEndHoldCooldownOver(int index)
    {
        if (lastHoldTimes[index] + 0.5f < Time.time)
        {
            return true;
        }
        return false;
    }


    public void OnHitDrumpad()
    {
        Debug.Log("On hit drumpad");
        if (isServer)
        {
            RpcOnHitDrumpad();
        }
        else
        {
            CmdOnHitDrumpad();
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdOnHitDrumpad()
    {
        Debug.Log("Cmd On hit drumpad");
        drumpad.keyHit(true, false);
        RpcOnHitDrumpad();
    }

    [ClientRpc]
    public void RpcOnHitDrumpad()
    {
        Debug.Log("Rpc On hit drumpad");
        if (!isServer && drumpad.isHit)
        {
            drumpad.keyHit(true, false);
        }
    }

}
