using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class NetworkPlayerPlugHand : NetworkBehaviour
{
    public GameObject handParent;
    
    [SyncVar(hook = nameof(OnSetConnectedJackIndex))]
    public int connectedJackIndex;
    [SyncVar(hook = nameof(OnChangeOffset))]
    private Vector3 offset;
    [SyncVar(hook = nameof(OnChangeRotationOffset))]
    private Quaternion rotationOffset;
    private NetworkPlugCopy networkedPlugInHand;
    private omniPlug plugInHand;

    public GameObject plugPrefab;

    public omniPlug PlugInHand { get => plugInHand; set => plugInHand = value; }

    //check if it delets plug if like that
    private void Update()
    {
        if (isLocalPlayer && connectedJackIndex != 0 && plugInHand == null)
        {
            SetHandJackIndex(0, Vector3.zero, Quaternion.identity);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isServer && connectedJackIndex != 0)
        {
            SetSamplePlug(offset, rotationOffset);
        }
    }

    public void OnSetConnectedJackIndex(int old, int newValue)
    {
        if (old != newValue && !isServer)
        {
            Debug.Log($"{gameObject.name} On set connected Jack Index: {newValue}");

            SetSamplePlug(offset, rotationOffset);
        }
    }


    public void OnChangeOffset(Vector3 old, Vector3 newValue)
    {
        if (networkedPlugInHand != null && !isLocalPlayer)
        {
            networkedPlugInHand.transform.localPosition = newValue;
        }
    }
    public void OnChangeRotationOffset(Quaternion old, Quaternion newValue)
    {
        if (networkedPlugInHand != null && !isLocalPlayer)
        {
            networkedPlugInHand.transform.localRotation = newValue;
        }
    }


    public void SetHandJackIndex(int index, Vector3 position, Quaternion rotation)
    {
        Debug.Log($"{gameObject.name} Set hand jack: {index}, {position}, {rotation}");
        if (isServer)
        {
            offset = position;
            rotationOffset = rotation;
            connectedJackIndex = index;
            SetSamplePlug(position, rotation);
        }
        else
        {
            CmdSetHandJackIndex(index, position, rotation);
        }
    }

    [Command]
    public void CmdSetHandJackIndex(int index, Vector3 position, Quaternion rotation)
    {
        connectedJackIndex = index;
        rotationOffset = rotation;
        SetSamplePlug(position, rotation);
    }

    private void SetSamplePlug(Vector3 position, Quaternion rotation)
    {
        if (networkedPlugInHand != null)
        {
            Destroy(networkedPlugInHand.gameObject);
            Debug.Log($"Destroy grabbed plug of {handParent}");
        }
        if (!isLocalPlayer && connectedJackIndex != 0)
        {
            //check if target jack of ID existst
            var targetJack = NetworkSpawnManager.Instance.GetJackById(connectedJackIndex);
            if (targetJack != null)
            {
                Debug.Log($"Change plug of {handParent} to {connectedJackIndex}, {position}, {rotation}");

                GameObject g = Instantiate(plugPrefab, handParent.transform);
                g.transform.localPosition = position;
                g.transform.localRotation = rotation;
                networkedPlugInHand = g.GetComponent<NetworkPlugCopy>();
                networkedPlugInHand.Initialize(this, targetJack);
            }
        }
    }



    public void PassToOtherHand()
    {
        Debug.Log($"{gameObject.name} plug passed to other hand");
        if (isServer)
        {
            connectedJackIndex = 0;
            networkedPlugInHand = null;
            RpcDeleteGrabbedPlugInHand();

        }
        else
        {
            networkedPlugInHand = null;
            CmdPassToOtherPlayer();
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdPassToOtherPlayer()
    {
        Debug.Log($"Cmd {gameObject.name} plug passed to other hand");

        connectedJackIndex = 0;
        if (networkedPlugInHand != null)
        {
            Destroy(networkedPlugInHand.gameObject);
        }
        RpcDeleteGrabbedPlugInHand();
    }

    [ClientRpc]
    public void RpcDeleteGrabbedPlugInHand()
    {
        Debug.Log($"RPC {gameObject.name} delet grabbed plug");
        if (plugInHand != null)
        {
            Destroy(plugInHand.gameObject);
        }
    }
}
