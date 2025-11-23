// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
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

using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class NetworkTutorials : NetworkBehaviour
{
    private tutorialsDeviceInterface tutorialsDevice;
    private float lastTimeSync;
    private const float syncInterval = 1f;
    private const float minorDriftThreshold = 0.04f;
    private const float majorDriftThreshold = 0.150f;
    private const float maxRttCompensation = 0.075f; // cap RTT contribution so drift stays under ~150ms
    private const float speedAdjustScale = 0.5f;
    private const float maxSpeedAdjust = 0.05f;
    private int activeTutorialIndex;
    private bool hasInitializedStartup;
    private bool requestedInitialSync;

    private void Awake()
    {
        tutorialsDevice = GetComponent<tutorialsDeviceInterface>();
        if (tutorialsDevice != null)
        {
            tutorialsDevice.openOnStartup = false;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Position in front of the host's camera
        if (Camera.main != null)
        {
            Transform camTransform = Camera.main.transform;
            // Position 1.5m in front, keeping vertical position relative or fixed?
            // User said "place itself in front of the users headset".
            // Let's keep the device's Y but move X/Z to be in front.
            // Or just place it directly in front.
            // Usually tutorials are floating panels.

            Vector3 targetPos = camTransform.position + camTransform.forward * 1.5f;
            // Ensure it's not tilted weirdly, look at camera but keep upright
            Vector3 lookPos = camTransform.position;
            lookPos.y = targetPos.y; // Keep level

            transform.position = targetPos;
            transform.LookAt(lookPos);
            //transform.Rotate(0, 180, 0); // Face the user
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (tutorialsDevice != null)
        {
            tutorialsDevice.openOnStartup = false;
        }

        if (!isServer)
        {
            requestInitialSync();
        }
    }

    private void Start()
    {
        if (tutorialsDevice != null)
        {
            tutorialsDevice.OnTriggerOpenTutorial += HandleTriggerOpenTutorial;
            tutorialsDevice.OnPlay += HandlePlay;
            tutorialsDevice.OnPause += HandlePause;
            tutorialsDevice.OnSeek += HandleSeek;

            hasInitializedStartup = InitializeStartupTutorial();
        }
    }

    private void OnDestroy()
    {
        if (tutorialsDevice != null)
        {
            tutorialsDevice.OnTriggerOpenTutorial -= HandleTriggerOpenTutorial;
            tutorialsDevice.OnPlay -= HandlePlay;
            tutorialsDevice.OnPause -= HandlePause;
            tutorialsDevice.OnSeek -= HandleSeek;
        }
    }

    private void Update()
    {
        if (!hasInitializedStartup)
        {
            hasInitializedStartup = InitializeStartupTutorial();
        }

        if (!isServer || tutorialsDevice == null || tutorialsDevice.videoPlayer == null)
        {
            return;
        }

        if (!tutorialsDevice.IsReady)
        {
            return;
        }

        if (Time.time - lastTimeSync > syncInterval)
        {
            lastTimeSync = Time.time;
            float serverTime = Mathf.Max(0f, (float)tutorialsDevice.videoPlayer.time);
            RpcSyncTime(serverTime, tutorialsDevice.videoPlayer.isPlaying);
        }
    }

    #region Event Handlers

    private bool HandleTriggerOpenTutorial(tutorialPanel tut, bool startPaused)
    {
        int tutorialIndex = Array.IndexOf(tutorialsDevice.tutorials, tut);
        if (tutorialIndex < 0)
        {
            return false;
        }

        activeTutorialIndex = tutorialIndex;

        // Force startPaused to false to prevent "odd tutorial" hangs where UI might pass true.
        // We always want the video to play when opened.
        bool forcePlay = false;

        if (isServer)
        {
            RpcTriggerOpenTutorial(tutorialIndex, forcePlay);
            // Do NOT sync time here. We are opening a new video, so time should reset to 0.
            // Syncing time here would send the OLD video's time, causing the new video to seek to that time.
            return true;
        }
        else if (NetworkClient.active)
        {
            CmdTriggerOpenTutorial(tutorialIndex, forcePlay);
            return true; // Suppress local execution, wait for Rpc from server
        }

        return false;
    }

    private void HandlePlay()
    {
        if (isServer) RpcPlay();
        else CmdPlay();
    }

    private void HandlePause()
    {
        if (isServer) RpcPause();
        else CmdPause();
    }

    private void HandleSeek(float time)
    {
        float clampedTime = Mathf.Max(0f, time);
        if (isServer) RpcSeek(clampedTime);
        else CmdSeek(clampedTime);
    }

    private void requestInitialSync()
    {
        if (requestedInitialSync || !NetworkClient.active)
        {
            return;
        }

        requestedInitialSync = true;
        CmdRequestInitialState();
    }

    #endregion

    #region Commands

    [Command(requiresAuthority = false)]
    private void CmdTriggerOpenTutorial(int tutorialIndex, bool startPaused)
    {
        activeTutorialIndex = tutorialIndex;
        RpcTriggerOpenTutorial(tutorialIndex, startPaused);
    }

    [Command(requiresAuthority = false)]
    private void CmdPlay()
    {
        RpcPlay();
    }

    [Command(requiresAuthority = false)]
    private void CmdPause()
    {
        RpcPause();
    }

    [Command(requiresAuthority = false)]
    private void CmdSeek(float time)
    {
        RpcSeek(time);
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestInitialState(NetworkConnectionToClient sender = null)
    {
        if (!isServer || tutorialsDevice == null || tutorialsDevice.tutorials == null || tutorialsDevice.tutorials.Length == 0)
        {
            return;
        }

        int tutorialIndex = Mathf.Clamp(activeTutorialIndex, 0, tutorialsDevice.tutorials.Length - 1);
        float serverTime = tutorialsDevice.videoPlayer != null ? Mathf.Max(0f, (float)tutorialsDevice.videoPlayer.time) : 0f;
        bool isReady = tutorialsDevice.IsReady;
        bool isPlaying = isReady && tutorialsDevice.videoPlayer != null && tutorialsDevice.videoPlayer.isPlaying;

        TargetApplyInitialState(sender, tutorialIndex, serverTime, isPlaying);
    }

    #endregion

    #region ClientRPCs

    [ClientRpc]
    private void RpcTriggerOpenTutorial(int tutorialIndex, bool startPaused)
    {
        if (tutorialIndex >= 0 && tutorialIndex < tutorialsDevice.tutorials.Length)
        {
            activeTutorialIndex = tutorialIndex;
            tutorialsDevice.InternalOpenTutorial(tutorialsDevice.tutorials[tutorialIndex], startPaused);
        }
    }

    [ClientRpc]
    private void RpcPlay()
    {
        tutorialsDevice.InternalPlay();
    }

    [ClientRpc]
    private void RpcPause()
    {
        tutorialsDevice.InternalPause();
    }

    [ClientRpc]
    private void RpcSeek(float time)
    {
        tutorialsDevice.InternalSeek(time);
    }

    [ClientRpc]
    private void RpcSyncTime(float serverTime, bool isServerPlaying)
    {
        serverTime = Mathf.Max(0f, serverTime);

        if (tutorialsDevice == null || tutorialsDevice.videoPlayer == null)
        {
            return;
        }

        if (!tutorialsDevice.videoPlayer.isPrepared)
        {
            tutorialsDevice.InternalSeek(serverTime);
            if (isServerPlaying)
            {
                tutorialsDevice.InternalPlay();
            }
            else
            {
                tutorialsDevice.InternalPause();
            }
            return;
        }

        bool isLocalPlaying = tutorialsDevice.videoPlayer.isPlaying;

        // Sync Play/Pause state
        if (isServerPlaying && !isLocalPlaying)
        {
            tutorialsDevice.InternalPlay();
        }
        else if (!isServerPlaying && isLocalPlaying)
        {
            tutorialsDevice.InternalPause();
            tutorialsDevice.InternalSetSpeed(1f); // Reset speed when paused
        }

        // Sync Time
        //if (isServerPlaying)
        if (false)
        {
            float halfRtt = (float)Math.Max(0.0, NetworkTime.rtt * 0.5f);
            float targetServerTime = serverTime + Mathf.Min(halfRtt, maxRttCompensation);
            float localTime = (float)tutorialsDevice.videoPlayer.time;
            float drift = targetServerTime - localTime;
            float absDrift = Math.Abs(drift);

            if (absDrift > majorDriftThreshold)
            {
                tutorialsDevice.InternalSeek(targetServerTime);
                tutorialsDevice.InternalSetSpeed(1f);
                return;
            }

            // Hysteresis: If we are already compensating, use a lower threshold to stop.
            // This prevents rapid toggling when drift is near the threshold.
            bool isCompensating = Math.Abs(tutorialsDevice.videoPlayer.playbackSpeed - 1f) > 0.001f;
            float activeThreshold = isCompensating ? 0.01f : minorDriftThreshold;

            if (absDrift > activeThreshold)
            {
                float speedOffset = Mathf.Clamp(drift * speedAdjustScale, -maxSpeedAdjust, maxSpeedAdjust);
                //tutorialsDevice.InternalSetSpeed(1f + speedOffset);
                tutorialsDevice.InternalSetSpeed(1f); // do not adjust speed for now because that
            }
            else if (isCompensating)
            {
                tutorialsDevice.InternalSetSpeed(1f);
            }
        }
        //else
        if (true)
        {
            float localTime = (float)tutorialsDevice.videoPlayer.time;

            if (Math.Abs(localTime - serverTime) > majorDriftThreshold)
            {
                tutorialsDevice.InternalSeek(serverTime);
            }

            // if (Math.Abs(tutorialsDevice.videoPlayer.playbackSpeed - 1f) > 0.001f)
            // {
            //     tutorialsDevice.InternalSetSpeed(1f);
            // }
        }
    }

    #endregion

    [TargetRpc]
    private void TargetApplyInitialState(NetworkConnection target, int tutorialIndex, float serverTime, bool isPlaying)
    {
        if (tutorialsDevice == null || tutorialsDevice.tutorials == null || tutorialsDevice.tutorials.Length == 0)
        {
            return;
        }

        if (tutorialIndex < 0 || tutorialIndex >= tutorialsDevice.tutorials.Length)
        {
            return;
        }

        StartCoroutine(applyInitialStateRoutine(tutorialIndex, serverTime, isPlaying));
    }

    private IEnumerator applyInitialStateRoutine(int tutorialIndex, float serverTime, bool isPlaying)
    {
        activeTutorialIndex = tutorialIndex;
        tutorialsDevice.InternalOpenTutorial(tutorialsDevice.tutorials[tutorialIndex], true);

        while (tutorialsDevice.videoPlayer != null && !tutorialsDevice.videoPlayer.isPrepared)
        {
            yield return null;
        }

        if (tutorialsDevice.videoPlayer == null)
        {
            yield break;
        }

        tutorialsDevice.InternalSeek(serverTime);
        tutorialsDevice.InternalSetSpeed(1f);

        if (isPlaying)
        {
            tutorialsDevice.InternalPlay();
        }
    }

    private bool InitializeStartupTutorial()
    {
        if (tutorialsDevice == null || tutorialsDevice.tutorials == null || tutorialsDevice.tutorials.Length == 0)
        {
            return false;
        }

        activeTutorialIndex = Mathf.Clamp(activeTutorialIndex, 0, tutorialsDevice.tutorials.Length - 1);

        if (!NetworkServer.active && !NetworkClient.active)
        {
            tutorialsDevice.InternalOpenTutorial(tutorialsDevice.tutorials[activeTutorialIndex], false);
            return true;
        }

        if (isServer)
        {
            RpcTriggerOpenTutorial(activeTutorialIndex, false);
            if (tutorialsDevice.IsReady)
            {
                float serverTime = Mathf.Max(0f, (float)tutorialsDevice.videoPlayer.time);
                RpcSyncTime(serverTime, tutorialsDevice.videoPlayer.isPlaying);
                lastTimeSync = Time.time;
            }
        }

        return true;
    }
}
