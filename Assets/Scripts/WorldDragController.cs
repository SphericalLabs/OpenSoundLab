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

using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IAS.CoLocationMUVR;

/*
Controls whole-patch world dragging from the local VR rig.
During a networked drag, the server locks the patch to one player,
temporarily gives that player authority over PatchAnchor, and bakes
the final transform back into the child devices when the drag ends.
*/
public class WorldDragController : NetworkBehaviour
{
    public static WorldDragController Instance { get; private set; }

    public manipulator leftManip, rightManip;
    public Transform leftHandAnchor, rightHandAnchor;
    public Transform centerEyeAnchor;

    [SyncVar(hook = nameof(OnActiveDragOwnerChanged))]
    public uint activeDragOwnerNetId;

    [SyncVar(hook = nameof(OnActiveDragSessionChanged))]
    public uint activeDragSessionId;

    Vector3 currentControllerMiddle, lastControllerMiddle;
    float currentControllerAngle, lastControllerAngle, currentControllerDistance, lastControllerDistance;
    bool isDragging = false;
    bool isVertical = false;
    bool isHorizontal = false;
    bool localSidesPressedLastFrame = false;
    uint localEndingDragSessionId = 0;
    uint nextDragSessionId = 1;
    float nextPatchAnchorScaleLogTime = 0f;
    Transform[] transArray;
    NetworkTransformBase patchNetTransform;
    uint lastAppliedFinalSessionId = 0;
    CalibrationManager calibrationManager;

    Vector3 tiltAxis;
    Vector3 rollAxis;
    TransformSnapshot centerEyeAnchorSnapshot;
    Vector3 rotationPoint;

    void Awake()
    {
        if (Instance == null) Instance = this;

        // These references to the manipulators will be reused by VRNetworkPlayer so don't skip finding them if on client
        cacheLocalRig();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        cacheLocalRig();
        setPatchSyncActive(activeDragSessionId != 0);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        cacheLocalRig();
        setPatchSyncActive(activeDragSessionId != 0);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        //logPatchAnchorScalePeriodically();
        updateLocalDragRequest();
        updateLocalDragReleaseRequest();
        updateOwnedDrag();

        if (isServer)
        {
            updateServerDragLock();
        }
    }

    void logPatchAnchorScalePeriodically()
    {
        if (Time.unscaledTime < nextPatchAnchorScaleLogTime) return;

        nextPatchAnchorScaleLogTime = Time.unscaledTime + 1f;
        Debug.Log($"WorldDragController PatchAnchor scale={transform.localScale} session={activeDragSessionId} owner={activeDragOwnerNetId} isServer={isServer} isClient={isClient} isOwned={isOwned}");
    }

    void cacheLocalRig()
    {
        if (patchNetTransform == null)
        {
            patchNetTransform = GetComponent<NetworkTransformBase>();
        }

        if (leftManip == null)
        {
            GameObject leftAnchor = GameObject.Find("LeftHandAnchor");
            if (leftAnchor != null)
            {
                leftManip = leftAnchor.GetComponentInChildren<manipulator>();
                if (leftHandAnchor == null) leftHandAnchor = leftAnchor.transform;
            }
        }

        if (rightManip == null)
        {
            GameObject rightAnchor = GameObject.Find("RightHandAnchor");
            if (rightAnchor != null)
            {
                rightManip = rightAnchor.GetComponentInChildren<manipulator>();
                if (rightHandAnchor == null) rightHandAnchor = rightAnchor.transform;
            }
        }

        if (centerEyeAnchor == null)
        {
            GameObject centerEye = GameObject.Find("CenterEyeAnchor");
            if (centerEye != null) centerEyeAnchor = centerEye.transform;
        }
    }

    void setPatchSyncActive(bool active)
    {
        if (patchNetTransform == null) return;

        updatePatchSyncDirection();

        if (active)
        {
            if (!patchNetTransform.enabled)
            {
                patchNetTransform.enabled = true;
            }
            patchNetTransform.ResetState();
            return;
        }

        patchNetTransform.ResetState();
        if (patchNetTransform.enabled)
        {
            patchNetTransform.enabled = false;
        }
    }

    void updatePatchSyncDirection()
    {
        if (patchNetTransform == null) return;

        bool serverReceivesClientDrag = isServer && activeDragSessionId != 0;
        bool localOwnerSendsClientDrag = isClient && !isServer && activeDragSessionId != 0 && isLocalDragOwner();
        patchNetTransform.syncDirection = serverReceivesClientDrag || localOwnerSendsClientDrag
            ? SyncDirection.ClientToServer
            : SyncDirection.ServerToClient;
    }

    void updateLocalDragRequest()
    {
        if (!isClient) return;

        cacheLocalRig();

        bool bothSidesPressed = OSLInput.getInstance() != null && OSLInput.getInstance().areBothSidesPressed();
        if (isCalibrationUiActive())
        {
            localSidesPressedLastFrame = bothSidesPressed;
            return;
        }

        if (bothSidesPressed && !localSidesPressedLastFrame)
        {
            beginLocalDragRequest();
        }

        localSidesPressedLastFrame = bothSidesPressed;
    }

    void beginLocalDragRequest()
    {
        if (isCalibrationUiActive()) return;
        if (activeDragOwnerNetId != 0) return;
        if (leftManip == null || rightManip == null) return;
        if (leftManip.isGrabbing() || rightManip.isGrabbing()) return;

        if (isServer)
        {
            tryBeginWorldDrag(getLocalDragPlayer());
        }
        else
        {
            CmdRequestBeginWorldDrag();
        }
    }

    VRNetworkPlayer getLocalDragPlayer()
    {
        if (NetworkClient.localPlayer == null) return null;
        return NetworkClient.localPlayer.GetComponent<VRNetworkPlayer>();
    }

    bool isLocalDragOwner()
    {
        return activeDragOwnerNetId != 0 &&
               NetworkClient.localPlayer != null &&
               NetworkClient.localPlayer.netId == activeDragOwnerNetId;
    }

    bool canDriveLocalDrag()
    {
        // Once a release was detected for this session, stop applying more
        // local drag deltas until the server finalizes the bake.
        return isClient &&
               isLocalDragOwner() &&
               activeDragSessionId != localEndingDragSessionId &&
               (isOwned || isServer);
    }

    [Command(requiresAuthority = false)]
    void CmdRequestBeginWorldDrag(NetworkConnectionToClient sender = null)
    {
        if (sender == null || sender.identity == null) return;

        VRNetworkPlayer player = sender.identity.GetComponent<VRNetworkPlayer>();
        if (player == null) return;

        tryBeginWorldDrag(player);
    }

    bool tryBeginWorldDrag(VRNetworkPlayer player)
    {
        if (player == null) return false;
        if (activeDragOwnerNetId != 0) return false;
        if (NetworkAuthorityHandle.AnyModuleMoveInProgress()) return false;

        NetworkConnectionToClient dragConnection = player.connectionToClient;
        if (dragConnection == null) return false;

        cacheLocalRig();
        if (patchNetTransform == null) return false;

        activeDragOwnerNetId = player.netId;
        activeDragSessionId = allocateDragSessionId();

        setPatchSyncActive(true);

        if (netIdentity.connectionToClient != null && netIdentity.connectionToClient != dragConnection)
        {
            netIdentity.RemoveClientAuthority();
        }
        if (netIdentity.connectionToClient != dragConnection)
        {
            netIdentity.AssignClientAuthority(dragConnection);
        }

        return true;
    }

    void updateOwnedDrag()
    {
        if (!canDriveLocalDrag()) return;

        if (isCalibrationUiActive())
        {
            requestLocalDragEnd(false);
            return;
        }

        cacheLocalRig();
        if (leftHandAnchor == null || rightHandAnchor == null || centerEyeAnchor == null) return;

        bool bothSidesPressed = OSLInput.getInstance() != null && OSLInput.getInstance().areBothSidesPressed();
        if (!bothSidesPressed)
        {
            if (activeDragSessionId != 0)
            {
                endLocalDrag();
            }
            return;
        }

        if (!isDragging)
        {
            startLocalDrag();
        }

        getCurrentValuesHorizontal();

        if (!OSLInput.getInstance().areBothTriggersFullPressed())
        {
            if (!isHorizontal)
            {
                isHorizontal = true;
                isVertical = false;
                getCurrentValuesHorizontal();
                storeCurrentValuesHorizontal();
            }

            if (isHorizontal)
            {
                currentControllerAngle = getAngleBetweenControllersXZ();
                transform.RotateAround(currentControllerMiddle, Vector3.up, lastControllerAngle - currentControllerAngle);

                currentControllerMiddle = getMiddle(leftHandAnchor, rightHandAnchor);
                currentControllerDistance = getDistanceBetweenControllers();
                scaleAround(transform, currentControllerMiddle, transform.localScale * (1f + currentControllerDistance - lastControllerDistance));
                transform.Translate(currentControllerMiddle - lastControllerMiddle, Space.World);
                storeCurrentValuesHorizontal();
            }
        }
        else
        {
            if (!isVertical)
            {
                isHorizontal = false;
                isVertical = true;
                tiltAxis = centerEyeAnchor.right;
                rollAxis = centerEyeAnchor.forward;
                centerEyeAnchorSnapshot = new TransformSnapshot(centerEyeAnchor);
                rotationPoint = centerEyeAnchor.position;
                getCurrentValuesVertical();
                storeCurrentValuesVertical();
            }

            if (isVertical)
            {
                currentControllerMiddle = centerEyeAnchorSnapshot.WorldToLocal(getMiddle(leftHandAnchor, rightHandAnchor));
                float rollAngle = Utils.map((currentControllerMiddle - lastControllerMiddle).x, -1f, 1f, 90f, -90f);
                float tiltAngle = Utils.map((currentControllerMiddle - lastControllerMiddle).y, -0.3f, 0.3f, 90f, -90f);
                transform.RotateAround(rotationPoint, rollAxis, rollAngle);
                transform.RotateAround(rotationPoint, tiltAxis, tiltAngle);
                storeCurrentValuesVertical();
            }
        }
    }

    void startLocalDrag()
    {
        isDragging = true;
        isVertical = false;
        isHorizontal = false;
        getCurrentValuesHorizontal();
        storeCurrentValuesHorizontal();
    }

    void endLocalDrag()
    {
        requestLocalDragEnd(false);
    }

    [Command(requiresAuthority = false)]
    void CmdEndWorldDrag(uint sessionId, Vector3 finalPosition, Quaternion finalRotation, Vector3 finalScale, NetworkConnectionToClient sender = null)
    {
        if (sender == null || sender.identity == null) return;
        if (sessionId == 0 || sessionId != activeDragSessionId) return;
        if (sender.identity.netId != activeDragOwnerNetId) return;

        finishWorldDrag(sessionId, finalPosition, finalRotation, finalScale);
    }

    void updateServerDragLock()
    {
        if (activeDragOwnerNetId == 0 || netIdentity.connectionToClient != null) return;

        finishWorldDrag(activeDragSessionId, transform.position, transform.rotation, transform.localScale);
    }

    void finishWorldDrag(uint sessionId, Vector3 finalPosition, Quaternion finalRotation, Vector3 finalScale)
    {
        if (sessionId == 0 || sessionId != activeDragSessionId) return;

        stopPatchSync();
        RpcFinishWorldDrag(sessionId, finalPosition, finalRotation, finalScale);
        applyFinalWorldDrag(sessionId, finalPosition, finalRotation, finalScale);

        if (netIdentity.connectionToClient != null)
        {
            netIdentity.RemoveClientAuthority();
        }
        activeDragOwnerNetId = 0;
        activeDragSessionId = 0;
        localEndingDragSessionId = 0;
        setPatchSyncActive(false);
    }

    void applyDragTransform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        transform.position = position;
        transform.rotation = rotation;
        transform.localScale = scale;
    }

    void applyFinalWorldDrag(uint sessionId, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (sessionId <= lastAppliedFinalSessionId)
        {
            // Each drag needs a unique session id. If a later drag reuses an
            // old id, the bake would be skipped as a stale finalize.
            Debug.LogWarning($"WorldDragController skipped stale final drag session={sessionId} lastApplied={lastAppliedFinalSessionId}");
            return;
        }

        lastAppliedFinalSessionId = sessionId;
        applyDragTransform(position, rotation, scale);
        bakeTransforms();
        clearLocalDragState();
    }

    void clearLocalDragState()
    {
        isDragging = false;
        isVertical = false;
        isHorizontal = false;
    }

    void OnActiveDragOwnerChanged(uint oldValue, uint newValue)
    {
        updatePatchSyncDirection();
    }

    void OnActiveDragSessionChanged(uint oldValue, uint newValue)
    {
        setPatchSyncActive(newValue != 0);

        if (newValue == 0 || newValue != oldValue)
        {
            localEndingDragSessionId = 0;
        }

        if (newValue == 0)
        {
            clearLocalDragState();
            if (!isServer)
            {
                activeDragOwnerNetId = 0;
            }
        }
    }

    void updateLocalDragReleaseRequest()
    {
        if (!isClient) return;
        if (activeDragSessionId == 0 || !isLocalDragOwner()) return;
        if (activeDragSessionId == localEndingDragSessionId) return;

        cacheLocalRig();

        OSLInput input = OSLInput.getInstance();
        bool missingRigReference = leftHandAnchor == null || rightHandAnchor == null || centerEyeAnchor == null;
        // The original drag-end path depended on the owner still being in the
        // normal drive loop. Watch for release separately so we still finalize
        // if ownership/input/rig state changes during release.
        bool releaseRequested = input == null || input.didAnySideReleaseThisFrame() || !input.areBothSidesPressed();

        if (!releaseRequested && !missingRigReference) return;

        requestLocalDragEnd(false);
    }

    void requestLocalDragEnd(bool bakeImmediately)
    {
        uint sessionId = activeDragSessionId;
        if (sessionId == 0 || sessionId == localEndingDragSessionId) return;

        Vector3 finalPosition = transform.position;
        Quaternion finalRotation = transform.rotation;
        Vector3 finalScale = transform.localScale;

        localEndingDragSessionId = sessionId;
        clearLocalDragState();

        if (isServer)
        {
            finishWorldDrag(sessionId, finalPosition, finalRotation, finalScale);
            return;
        }

        if (bakeImmediately)
        {
            // Save/load/new patch can happen before the server round-trip
            // finishes. Bake locally first so PatchAnchor is already normalized.
            stopPatchSync();
            applyFinalWorldDrag(sessionId, finalPosition, finalRotation, finalScale);
            setPatchSyncActive(false);
        }

        CmdEndWorldDrag(sessionId, finalPosition, finalRotation, finalScale);
    }

    [ClientRpc]
    void RpcFinishWorldDrag(uint sessionId, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (isServer) return;

        stopPatchSync();
        applyFinalWorldDrag(sessionId, position, rotation, scale);
        setPatchSyncActive(false);
        activeDragOwnerNetId = 0;
    }

    public bool IsModuleMoveLocked()
    {
        return activeDragOwnerNetId != 0;
    }

    public bool ShouldBlockModuleGrab(manipObject manipObj)
    {
        if (!IsModuleMoveLocked() || manipObj == null || !(manipObj is handle)) return false;
        return manipObj.GetComponentInParent<NetworkAuthorityHandle>() != null;
    }

    public bool ShouldBlockModuleGrab(NetworkIdentity identity)
    {
        if (!IsModuleMoveLocked() || identity == null) return false;
        return identity.GetComponent<NetworkAuthorityHandle>() != null;
    }

    void getCurrentValuesHorizontal()
    {
        currentControllerMiddle = getMiddle(leftHandAnchor, rightHandAnchor);
        currentControllerAngle = getAngleBetweenControllersXZ();
        currentControllerDistance = getDistanceBetweenControllers();
    }

    void storeCurrentValuesHorizontal()
    {
        lastControllerMiddle = currentControllerMiddle;
        lastControllerAngle = currentControllerAngle;
        lastControllerDistance = currentControllerDistance;
    }

    void getCurrentValuesVertical()
    {
        currentControllerMiddle = centerEyeAnchorSnapshot.WorldToLocal(getMiddle(leftHandAnchor, rightHandAnchor)); // in local space
    }

    void storeCurrentValuesVertical()
    {
        lastControllerMiddle = currentControllerMiddle;
    }

    float getAngleBetweenControllersXZ()
    {
        // on x,z plane
        return Mathf.Rad2Deg * Mathf.Atan2(leftHandAnchor.transform.position.z - rightHandAnchor.transform.position.z, leftHandAnchor.transform.position.x - rightHandAnchor.transform.position.x);
    }

    void bakeTransforms()
    {
        // move all children transform to parent, save list of transforms
        transArray = new Transform[transform.childCount];

        // populate array first, otherwise weird index/list bugs when moving transforms while iterating on them
        for (int i = 0; i < transform.childCount; i++)
        {
            transArray[i] = transform.GetChild(i);
        }

        // move them up
        for (int n = 0; n < transArray.Length; n++)
        {
            transArray[n].parent = transform.parent;
        }

        // fully reset parent transform
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        // move them back down again, baking the global drag transform into them
        for (int n = 0; n < transArray.Length; n++)
        {
            transArray[n].parent = transform;
        }

        // Child NetworkTransforms may still hold pre-bake local snapshots.
        // Clear them so old buffered states aren't replayed under the reset parent.
        resetChildTransformSyncBuffers();
    }

    void resetChildTransformSyncBuffers()
    {
        NetworkTransformBase[] childTransforms = GetComponentsInChildren<NetworkTransformBase>(true);
        for (int i = 0; i < childTransforms.Length; i++)
        {
            if (childTransforms[i] == null || childTransforms[i] == patchNetTransform) continue;
            childTransforms[i].ResetState();
        }
    }

    void stopPatchSync()
    {
        if (patchNetTransform == null) return;

        patchNetTransform.syncDirection = SyncDirection.ServerToClient;
        patchNetTransform.ResetState();
        patchNetTransform.enabled = false;
    }

    public void PrepareForPersistence()
    {
        if (activeDragSessionId != 0)
        {
            if (isServer || (isClient && isLocalDragOwner()))
            {
                requestLocalDragEnd(true);
            }
            return;
        }

        if (hasResidualPatchTransform())
        {
            bakeTransforms();
            clearLocalDragState();
        }
    }

    bool hasResidualPatchTransform()
    {
        if (transform.position.sqrMagnitude > 0.000001f) return true;
        if (Quaternion.Angle(transform.rotation, Quaternion.identity) > 0.001f) return true;
        return (transform.localScale - Vector3.one).sqrMagnitude > 0.000001f;
    }

    uint allocateDragSessionId()
    {
        if (nextDragSessionId == 0)
        {
            nextDragSessionId = 1;
        }

        // Keep drag session ids monotonic across drags. Reusing "1" after every
        // drag caused later finalization bakes to be rejected as stale.
        uint sessionId = nextDragSessionId;
        nextDragSessionId++;

        if (nextDragSessionId == 0)
        {
            nextDragSessionId = 1;
        }

        return sessionId;
    }


    Vector3 getMiddle(Transform a, Transform b)
    {
        return Vector3.Lerp(a.position, b.position, 0.5f);
    }


    float getDistanceBetweenControllers()
    {
        return Vector3.Distance(leftHandAnchor.transform.position, rightHandAnchor.transform.position);
    }

    bool isCalibrationUiActive()
    {
        if (calibrationManager == null)
        {
            calibrationManager = FindObjectOfType<CalibrationManager>();
        }

        if (calibrationManager == null) return false;
        if (calibrationManager.uiParentObject != null && calibrationManager.uiParentObject.activeInHierarchy) return true;
        if (calibrationManager.calibrateCourser != null && calibrationManager.calibrateCourser.activeInHierarchy) return true;
        return false;
    }

    // https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
    public void scaleAround(Transform target, Vector3 pivot, Vector3 newScale)
    {
        Vector3 A = target.localPosition;
        Vector3 B = pivot;

        Vector3 C = A - B; // diff from object pivot to desired pivot/origin

        float RS = newScale.x / target.localScale.x; // relative scale factor

        // calc final position post-scale
        Vector3 FP = B + C * RS;

        // finally, actually perform the scale/translation
        target.localScale = newScale;
        target.localPosition = FP;
    }
}


public class TransformSnapshot
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public TransformSnapshot(Transform transform)
    {
        position = transform.position;
        rotation = transform.rotation;
        scale = transform.localScale;
    }

    public Vector3 WorldToLocal(Vector3 worldPoint)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale).inverse;
        return matrix.MultiplyPoint3x4(worldPoint);
    }

    public Vector3 LocalToWorld(Vector3 localPoint)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);
        return matrix.MultiplyPoint3x4(localPoint);
    }

    public Vector3 TransformDirection(Vector3 localDirection)
    {
        return rotation * localDirection;
    }

    public Quaternion TransformRotation(Quaternion localRotation)
    {
        return rotation * localRotation;
    }
}
