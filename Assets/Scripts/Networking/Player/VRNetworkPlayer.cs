using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Unity.Collections.LowLevel.Unsafe;

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

    [SyncVar(hook = nameof(OnUserNameChanged))]
    public string userName;
    public TMPro.TMP_Text userNameText;

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
    private NetworkVoiceManager networkVoiceManager;
    private Transform voiceOverTransform;

    [Header("Network Tape")]
    public NetworkPlayerTapeHand leftNetworkTapeHand;
    public NetworkPlayerTapeHand rightNetworkTapeHand;

    [Header("Network Jack")]
    public NetworkPlayerPlugHand leftNetworkPlugHand;
    public NetworkPlayerPlugHand rightNetworkPlugHand;


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
            ChangeUserName(NetworkMenuManager.Instance.userName);
        }

        if (userNameText != null)
        {
            userNameText.gameObject.SetActive(false);
        }

        //deactivate meshrenderers
        if (networkHead != null && networkHead.childCount > 0)
        {
            Destroy(networkHead.GetChild(0).gameObject);
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
        StartCoroutine(ConnectToVoiceChatAgent());
    }

    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();
        RemoveManipulatorEvents();
    }

    private IEnumerator Start()
    {
        if (!isLocalPlayer)
        {
            if (NetworkManager.singleton is Network.LocalNetworkManager)
            {
                if (userNameText != null)
                {
                    userNameText.gameObject.SetActive(false);
                }
                if (networkHead != null && networkHead.childCount > 0)
                {
                    Destroy(networkHead.GetChild(0).gameObject);
                }
            }

            yield return new WaitForSeconds(1f);
            if (!moveVoiceChatObject)
            {
                OnVoiceChatIDChanged(voiceChatAgentID, voiceChatAgentID);
            }
        }
    }

    public void ChangeUserName(string newUserName)
    {
        if (isServer)
        {
            userName = newUserName;
            if (userNameText != null)
            {
                userNameText.text = userName;
            }
        }
        else
        {
            CmdChangeUserName(newUserName);
        }
    }

    [Command (requiresAuthority = false)]
    public void CmdChangeUserName(string newUserName)
    {
        userName = newUserName;
        if (userNameText != null)
        {
            userNameText.text = userName;
        }
    }

    public void OnUserNameChanged(string oldValue, string newValue)
    {
        //todo update text
        if (userNameText != null)
        {
            userNameText.text = newValue;
        }
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
        try
        { // hotfix for session killing null reference exception
            handle h = item.GetComponentInChildren<handle>();
            if(h != null) TargetManipulator(isLeftHand).ForceGrab(h);
        }
        catch (System.Exception e) { 
            Debug.LogWarning("An exception was caught during spawning that would have killed the session:"); 
            Debug.LogWarning(e); 
            // todo: cleanup grab on new object
        }
    }

    public manipulator TargetManipulator(bool isLeftHand)
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

    public void RemoveManipulatorEvents()
    {
        if (leftHandManipulator != null)
        {
            leftHandManipulator.onInputTriggerdEvent.RemoveListener(LeftHandTriggerStarted);
            leftHandManipulator.onInputReleasedEvent.RemoveListener(LeftHandTriggerReleased);
        }
        if (rightHandManipulator != null)
        {
            rightHandManipulator.onInputTriggerdEvent.RemoveListener(RightHandTriggerStarted);
            rightHandManipulator.onInputReleasedEvent.RemoveListener(RightHandTriggerReleased);
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

    public NetworkPlayerTapeHand GetTargetTapeHand(manipulator manipulator)
    {
        if (manipulator == leftHandManipulator)
        {
            return leftNetworkTapeHand;
        }
        else if (manipulator == rightHandManipulator)
        {
            return rightNetworkTapeHand;
        }
        return null;
    }

    public NetworkPlayerPlugHand GetTargetPlugHand(manipulator manipulator)
    {
        if (manipulator == leftHandManipulator)
        {
            return leftNetworkPlugHand;
        }
        else if (manipulator == rightHandManipulator)
        {
            return rightNetworkPlugHand;
        }
        return null;
    }

    public bool IsObjectGrabbedByLeftHand(manipObject manipObject)
    {
        if (leftHandManipulator.SelectedObject == manipObject)
        {
            return true;
        }
        return false;
    }


    public Transform TargetNetworkHand(bool isLeftHand)
    {
        return isLeftHand ? networkLeftHand : networkRightHand;
    }

    #endregion


    #region Voice Chat
    private IEnumerator ConnectToVoiceChatAgent()
    {
        //Debug.Log("Try connect to VoiceChatAgent by local Player");
        if (networkVoiceManager == null)
        {
            networkVoiceManager = FindObjectOfType<NetworkVoiceManager>();
        }
        if (networkVoiceManager != null)
        {
            while (networkVoiceManager.GetAgentID() < 0)
            {
                yield return new WaitForSeconds(1f);
                //Debug.Log("Wait for Agent ID over -1");
            }

            //Debug.Log($"set agent id {networkAudioManager.GetAgentID()}");
            if (isServer)
            {
                voiceChatAgentID = networkVoiceManager.GetAgentID();
            }
            else
            {
                CmdSetVoiceChatID(networkVoiceManager.GetAgentID());
            }
        }
    }

    [Command]
    public void CmdSetVoiceChatID(int id)
    {
        voiceChatAgentID = id;
        //Debug.Log($"Cmd voiceChatId of {gameObject.name} = {id}");
    }

    public void OnVoiceChatIDChanged(int old, int newValue)
    {
        voiceChatAgentID = newValue;
        //Debug.Log($"OnVoiceChatIDChanged {gameObject.name} = {voiceChatAgentID}");

        if (isLocalPlayer)
        {
            return;
        }
        //Search audio source object
        if (networkVoiceManager == null)
        {
            networkVoiceManager = GameObject.FindObjectOfType<NetworkVoiceManager>();
        }
        if (networkVoiceManager != null)
        {
            var audioOutput = networkVoiceManager.GetSourceOutput((short)voiceChatAgentID);
            if (audioOutput != null)
            {
                moveVoiceChatObject = true;
                voiceOverTransform = audioOutput.transform;
                audioOutput.AudioSource.spatialBlend = 1f;
                //Debug.Log($"found voicechat output object {gameObject.name}");
            }
        }
    }

    #endregion
}
