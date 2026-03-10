// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2026 OSLLv1 Sphericals OpenSoundLab
//
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
//
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
//
//
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
//
// Licensed under the Apache License, Version 2.0 (the "License");
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.IO;
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
    [SyncVar(hook = nameof(OnLeftHandManipulatorTriggerd))]
    public bool leftHandManipulatorTriggerd;
    [SyncVar(hook = nameof(OnRightHandManipulatorTriggerd))]
    public bool rightHandManipulatorTriggerd;

    public ManipulatorVisual leftManipulatorVisual;
    public ManipulatorVisual rightManipulatorVisual;

    [Header("Voice Chat")]
    [SyncVar(hook = nameof(OnVoiceChatIDChanged))]
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

    Coroutine patchLoadCoroutine;
    int nextPatchUploadId = 1;
    int pendingPatchUploadId = -1;
    string pendingPatchFileName;
    byte[] pendingCompressedPatchUpload;
    int pendingPatchUploadOffset;


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

    [Command(requiresAuthority = false)]
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
            if (h != null) TargetManipulator(isLeftHand).ForceGrab(h);
        }
        catch (System.Exception e)
        {
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
            var audioSource = networkVoiceManager.GetSourceOutput(voiceChatAgentID);
            if (audioSource != null)
            {
                moveVoiceChatObject = true;
                voiceOverTransform = audioSource.transform;
                audioSource.spatialBlend = 1f;
                //Debug.Log($"found voicechat output object {gameObject.name}");
            }
        }
    }

    #endregion

    #region PlayMode Patch Menu Commands

    const float networkedLoadDelaySeconds = 0.3f;

    public bool RequestLoadPatchFromLocalFile(string path)
    {
        if (!isLocalPlayer)
        {
            Debug.LogError("Cannot upload patch: RequestLoadPatchFromLocalFile must be called on the local player.");
            return false;
        }

        if (!NetworkClient.isConnected)
        {
            Debug.LogError("Cannot upload patch: Mirror client is not connected.");
            return false;
        }

        if (!NetworkPatchTransferUtility.TryPreparePatchUpload(path, out string patchFileName, out byte[] compressedPatch, out string error))
        {
            Debug.LogError(error);
            return false;
        }

        int chunkSize = NetworkPatchTransferUtility.GetPatchUploadChunkBytes();
        if (chunkSize <= 0)
        {
            Debug.LogError("Cannot upload patch: reliable chunk size is not available.");
            return false;
        }

        if (nextPatchUploadId == int.MaxValue)
        {
            nextPatchUploadId = 1;
        }

        int uploadId = nextPatchUploadId++;
        CmdBeginCompressedPatchUpload(uploadId, patchFileName, compressedPatch.Length);

        for (int offset = 0; offset < compressedPatch.Length; offset += chunkSize)
        {
            int chunkLength = Mathf.Min(chunkSize, compressedPatch.Length - offset);
            byte[] chunk = new byte[chunkLength];
            Buffer.BlockCopy(compressedPatch, offset, chunk, 0, chunkLength);
            CmdAppendCompressedPatchChunk(uploadId, chunk);
        }

        CmdFinishCompressedPatchUpload(uploadId);
        Debug.Log($"Requested Server to Load Patch from {path}");
        return true;
    }

    [Command(requiresAuthority = false)]
    public void CmdLoadLastPlayModePatch()
    {
        if (SaveLoadInterface.instance == null)
        {
            Debug.LogError("Cannot load LastPlayModePatch: SaveLoadInterface is not available.");
            return;
        }

        string path = GetPlayModePatchPath();
        if (!File.Exists(path))
        {
            Debug.LogWarning($"LastPlayModePatch not found at {path}");
            return;
        }

        BeginServerPatchLoad(path);
        Debug.Log($"Queued LastPlayModePatch load from {path} via Client Request");
    }

    [Command(requiresAuthority = false)]
    public void CmdSaveLastPlayModePatch()
    {
        if (SaveLoadInterface.instance != null)
        {
            string path = GetPlayModePatchPath();
            EnsurePatchDirectory(path);
            SaveLoadInterface.instance.Save(path);
            Debug.Log($"Saved LastPlayModePatch to {path} via Client Request");
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdNewPatch()
    {
        if (SaveLoadInterface.instance != null)
        {
            SaveLoadInterface.instance.StartNewPatch();
            Debug.Log("Started new patch via Client Request");
        }
    }

    [Command(requiresAuthority = false)]
    void CmdBeginCompressedPatchUpload(int uploadId, string patchFileName, int totalBytes)
    {
        if (SaveLoadInterface.instance == null)
        {
            Debug.LogError("Cannot load uploaded patch: SaveLoadInterface is not available.");
            clearPendingPatchUpload();
            return;
        }

        if (uploadId <= 0)
        {
            Debug.LogError($"Cannot begin patch upload: invalid upload id {uploadId}.");
            clearPendingPatchUpload();
            return;
        }

        if (totalBytes <= 0 || totalBytes > NetworkPatchTransferUtility.GetMaxBufferedPatchBytes())
        {
            Debug.LogError($"Cannot begin patch upload {patchFileName}: invalid compressed size {totalBytes} bytes.");
            clearPendingPatchUpload();
            return;
        }

        pendingPatchUploadId = uploadId;
        pendingPatchFileName = patchFileName;
        pendingCompressedPatchUpload = new byte[totalBytes];
        pendingPatchUploadOffset = 0;
    }

    [Command(requiresAuthority = false)]
    void CmdAppendCompressedPatchChunk(int uploadId, byte[] chunk)
    {
        if (pendingCompressedPatchUpload == null || uploadId != pendingPatchUploadId)
        {
            Debug.LogError($"Cannot append patch chunk: no active upload for id {uploadId}.");
            clearPendingPatchUpload();
            return;
        }

        if (chunk == null || chunk.Length == 0)
        {
            Debug.LogError($"Cannot append patch chunk: upload {uploadId} sent an empty chunk.");
            clearPendingPatchUpload();
            return;
        }

        if (pendingPatchUploadOffset + chunk.Length > pendingCompressedPatchUpload.Length)
        {
            Debug.LogError($"Cannot append patch chunk: upload {uploadId} exceeds announced size.");
            clearPendingPatchUpload();
            return;
        }

        Buffer.BlockCopy(chunk, 0, pendingCompressedPatchUpload, pendingPatchUploadOffset, chunk.Length);
        pendingPatchUploadOffset += chunk.Length;
    }

    [Command(requiresAuthority = false)]
    void CmdFinishCompressedPatchUpload(int uploadId)
    {
        if (pendingCompressedPatchUpload == null || uploadId != pendingPatchUploadId)
        {
            Debug.LogError($"Cannot finish patch upload: no active upload for id {uploadId}.");
            clearPendingPatchUpload();
            return;
        }

        if (pendingPatchUploadOffset != pendingCompressedPatchUpload.Length)
        {
            Debug.LogError($"Cannot finish patch upload {pendingPatchFileName}: received {pendingPatchUploadOffset} of {pendingCompressedPatchUpload.Length} bytes.");
            clearPendingPatchUpload();
            return;
        }

        byte[] compressedPatch = pendingCompressedPatchUpload;
        string patchFileName = pendingPatchFileName;
        clearPendingPatchUpload();

        if (!NetworkPatchTransferUtility.TryWriteUploadedPatch(compressedPatch, patchFileName, netId, out string patchPath, out string error))
        {
            Debug.LogError(error);
            return;
        }

        BeginServerPatchLoad(patchPath);
        Debug.Log($"Queued uploaded patch {patchFileName} via Client Request");
    }

    private string GetPlayModePatchPath()
    {
        string baseDir = (masterControl.instance != null) ? masterControl.instance.SaveDir : null;
        if (string.IsNullOrEmpty(baseDir))
        {
            baseDir = Application.persistentDataPath;
        }
        return Path.Combine(baseDir, "Saves", "LastPlayModePatch.xml");
    }

    private void EnsurePatchDirectory(string path)
    {
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    void BeginServerPatchLoad(string path)
    {
        if (!isServer)
        {
            Debug.LogError("Cannot begin server patch load: this player is not running on the server.");
            return;
        }

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Debug.LogError($"Cannot begin server patch load: patch file not found at {path}");
            return;
        }

        if (patchLoadCoroutine != null)
        {
            StopCoroutine(patchLoadCoroutine);
        }

        patchLoadCoroutine = StartCoroutine(loadPatchAfterClear(path));
    }

    IEnumerator loadPatchAfterClear(string path)
    {
        if (SaveLoadInterface.instance == null)
        {
            Debug.LogError("Cannot load patch after clear: SaveLoadInterface is not available.");
            patchLoadCoroutine = null;
            yield break;
        }

        SaveLoadInterface.instance.ClearInstruments();
        yield return new WaitForSecondsRealtime(networkedLoadDelaySeconds);

        if (SaveLoadInterface.instance == null)
        {
            Debug.LogError("Cannot finish patch load: SaveLoadInterface is not available.");
            patchLoadCoroutine = null;
            yield break;
        }

        SaveLoadInterface.instance.Load(path);
        patchLoadCoroutine = null;
    }

    void clearPendingPatchUpload()
    {
        pendingPatchUploadId = -1;
        pendingPatchFileName = null;
        pendingCompressedPatchUpload = null;
        pendingPatchUploadOffset = 0;
    }

    #endregion
}
