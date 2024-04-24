using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[System.Serializable]
public class ManipulatorVisual
{
    public GameObject activeObject;
    public GameObject inactiveObject;

    public void Toggle(bool b)
    {
        activeObject.gameObject.SetActive(b);
        inactiveObject.gameObject.SetActive(!b);
    }
}

public class VRNetworkPlayer : NetworkBehaviour
{
    [SerializeField] private Transform networkHead;
    [SerializeField] private Transform networkLeftHand;
    [SerializeField] private Transform networkRightHand;

    private Transform localPlayerHead;
    private Transform localPlayerRightHand;
    private Transform localPlayerLeftHand;


    private manipulator leftHandManipulator;
    private manipulator rightHandManipulator;

    [Header("Manipulators")]
    [SyncVar (hook = nameof(OnLeftHandManipulatorTriggerd))]
    public bool leftHandManipulatorTriggerd;
    [SyncVar (hook = nameof(OnRightHandManipulatorTriggerd))]
    public bool rightHandManipulatorTriggerd;

    public ManipulatorVisual leftManipulatorVisual;
    public ManipulatorVisual rightManipulatorVisual;

    [Header("Voice Chat")]
    [SyncVar (hook = nameof(OnVoiceChatIDChanged))]
    public int voiceChatAgentID;
    private bool moveVoiceChatObject = false;
    private NetworkAudioManager networkAudioManager;
    private Transform voiceOverTransform;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        var ovrCameraRig = FindAnyObjectByType<OVRCameraRig>();
        if (ovrCameraRig != null)
        {
            localPlayerHead = ovrCameraRig.centerEyeAnchor;
            localPlayerRightHand = ovrCameraRig.rightHandAnchor;
            localPlayerLeftHand = ovrCameraRig.leftHandAnchor;
        }

        if (NetworkMenuManager.Instance != null)
        {
            NetworkMenuManager.Instance.localPlayer = this;
        }

        //deactivate meshrenderers
        if (networkHead != null)
        {
            for(int i = networkHead.childCount - 1; i >= 0; i--)
            {
                Destroy(networkHead.GetChild(i).gameObject);
            }
        }
        if (networkLeftHand != null)
        {
            for (int i = networkLeftHand.childCount - 1; i >= 0; i--)
            {
                Destroy(networkLeftHand.GetChild(i).gameObject);
            }
        }
        if (networkRightHand != null)
        {
            for (int i = networkRightHand.childCount - 1; i >= 0; i--)
            {
                Destroy(networkRightHand.GetChild(i).gameObject);
            }
        }

        var worldDragController = GameObject.FindObjectOfType<WorldDragController>();
        if (worldDragController != null)
        {
            leftHandManipulator = worldDragController.leftManip;
            rightHandManipulator = worldDragController.rightManip;
        }

        InitializeManipulatorEvents();
        ConnectToVoiceChatAgent();
    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer)
        {
            networkHead.position = localPlayerHead.position;
            networkHead.rotation = localPlayerHead.rotation;

            networkLeftHand.position = localPlayerLeftHand.position;
            networkLeftHand.rotation = localPlayerLeftHand.rotation;

            networkRightHand.position = localPlayerRightHand.position;
            networkRightHand.rotation = localPlayerRightHand.rotation;
        }
        else if (moveVoiceChatObject)
        {
            voiceOverTransform.position = networkHead.position;
        }
    }

    //general networking actions
    [Command(requiresAuthority = false)]
    public void CmdGetObjectAuthority(NetworkIdentity item)
    {
        if (item.connectionToClient != null)
            item.RemoveClientAuthority();
        item.AssignClientAuthority(connectionToClient);
    }

    [Server]
    public void GrabNewObjectByHand(GameObject obj, bool isLeftHand)
    {
        if (obj.TryGetComponent<NetworkIdentity>(out NetworkIdentity item))
        {
            RpcGrabNewObjectByHand(connectionToClient, item, isLeftHand);
        }
    }

    [TargetRpc]
    public void RpcGrabNewObjectByHand(NetworkConnectionToClient target, NetworkIdentity item, bool isLeftHand)
    {
        Debug.Log($"Spawned item with name {item.gameObject} to this player, by lefthand {isLeftHand}");
        TargetManipulator(isLeftHand).ForceGrab(item.GetComponentInChildren<handle>());
    }

    private manipulator TargetManipulator(bool isLeftHand)
    {
        return isLeftHand ? leftHandManipulator : rightHandManipulator;
    }

    #region Manipulator Object Triggerd

    public void InitializeManipulatorEvents()
    {
        if (leftHandManipulator != null)
        {
            leftHandManipulator.onInputTriggerdEvent.AddListener(LeftHandTriggerStarted);
            leftHandManipulator.onInputReleasedEvent.AddListener(LeftHandTriggerReleased);
        }
        if (rightHandManipulator != null)
        {
            rightHandManipulator.onInputTriggerdEvent.AddListener(RightHandTriggerStarted);
            rightHandManipulator.onInputReleasedEvent.AddListener(RightHandTriggerReleased);
        }
    }

    //left Hand
    public void LeftHandTriggerStarted()
    {
        leftHandManipulatorTriggerd = true;
        if (!isServer)
        {
            CmdSetLeftHandManipulatorTriggerd(leftHandManipulatorTriggerd);
        }
    }
    public void LeftHandTriggerReleased()
    {
        leftHandManipulatorTriggerd = false;
        if (!isServer)
        {
            CmdSetLeftHandManipulatorTriggerd(leftHandManipulatorTriggerd);
        }
    }

    [Command]
    public void CmdSetLeftHandManipulatorTriggerd(bool b)
    {
        leftHandManipulatorTriggerd = b;
    }

    public void OnLeftHandManipulatorTriggerd(bool old, bool newValue)
    {
        leftHandManipulatorTriggerd = newValue;
        //update visual
        if (!isLocalPlayer)
        {
            leftManipulatorVisual.Toggle(leftHandManipulatorTriggerd);
        }
    }

    //right Hand
    public void RightHandTriggerStarted()
    {
        rightHandManipulatorTriggerd = true;
        if (!isServer)
        {
            CmdSetRightHandManipulatorTriggerd(rightHandManipulatorTriggerd);
        }
    }
    public void RightHandTriggerReleased()
    {
        rightHandManipulatorTriggerd = false;
        if (!isServer)
        {
            CmdSetRightHandManipulatorTriggerd(rightHandManipulatorTriggerd);
        }
    }

    [Command]
    public void CmdSetRightHandManipulatorTriggerd(bool b)
    {
        rightHandManipulatorTriggerd = b;
    }

    public void OnRightHandManipulatorTriggerd(bool old, bool newValue)
    {
        rightHandManipulatorTriggerd = newValue;
        //update visual
        if (!isLocalPlayer)
        {
            rightManipulatorVisual.Toggle(rightHandManipulatorTriggerd);
        }
    }
    #endregion


    #region Voice Chat
    private void ConnectToVoiceChatAgent()
    {
        Debug.Log("Try connect to VoiceChatAgent by local Player");
        if (networkAudioManager == null)
        {
            networkAudioManager = GameObject.FindObjectOfType<NetworkAudioManager>();
        }
        if (networkAudioManager != null)
        {
            if (isServer)
            {
                voiceChatAgentID = networkAudioManager.GetAgentID();
            }
            else
            {
                CmdSetVoiceChatID(networkAudioManager.GetAgentID());
            }
        }
    }

    [Command]
    public void CmdSetVoiceChatID(int id)
    {
        voiceChatAgentID = id;
        Debug.Log($"Cmd voiceChatId of {gameObject.name} = {id}");
        //we have to do this because the ID of the first player only is set correctly when the first peer joins (see UniVoiceMirrorNetwork.cs line 161)
        if (!isLocalPlayer && NetworkMenuManager.Instance != null)
        {
            if (NetworkMenuManager.Instance.localPlayer.voiceChatAgentID != 0)
            {
                NetworkMenuManager.Instance.localPlayer.voiceChatAgentID = 0;
                Debug.Log($"Set first player id to 0");
            }
        }
    }

    public void OnVoiceChatIDChanged(int old, int newValue)
    {
        voiceChatAgentID = newValue;
        Debug.Log($"OnVoiceChatIDChanged {gameObject.name} = {voiceChatAgentID}");

        if (isLocalPlayer)
        {
            return;
        }
        //Search audio source object
        if (networkAudioManager == null)
        {
            networkAudioManager = GameObject.FindObjectOfType<NetworkAudioManager>();
        }
        if (networkAudioManager != null)
        {
            var audioOutput = networkAudioManager.GetSourceOutput((short)voiceChatAgentID);
            if (audioOutput != null)
            {
                moveVoiceChatObject = true;
                voiceOverTransform = audioOutput.transform;
                audioOutput.AudioSource.spatialBlend = 1f;
                Debug.Log($"found voicechat output object {gameObject.name}");
            }
        }
    }

    #endregion
}
