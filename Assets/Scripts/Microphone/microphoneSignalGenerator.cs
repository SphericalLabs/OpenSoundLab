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
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;
using UnityEngine.Android;

public class microphoneSignalGenerator : signalGenerator
{

    [DllImport("OSLNative")]
    public static extern void SetArrayToSingleValue(float[] a, int length, float val);
    [DllImport("OSLNative")]
    public static extern void CopyArray(float[] a, float[] b, int length);
    [DllImport("OSLNative")]
    public static extern void MicFunction(float[] a, float[] b, int length, float val);


    AudioClip micClip;
    AudioSource audioSource;
    float[] sharedBuffer;
    bool activated = false;

    public Dictionary<float, float[]> freqBuffers = new Dictionary<float, float[]>();

    public float amp = 1;

    public bool active = true;

    int micID = 0;
    string activeDeviceString;

    private long pitchAdjustThreshold = 0; // Sample threshold to consider a drift correction
    private int lastMicHeadSamples = 0;
    private int lastPlaybackHeadSamples = 0;
    private long micTravelSamples = 0; // Total distance traveled by the mic recording
    private long playbackTravelSamples = 0; // Total distance traveled by the playback
    private int minPlaybackLagSamples = 0;

    int targetBuffering;
    const int fadeInSampleCount = 256;
    float fadeInProgress = 1f;
    const float pitchAdjustSpeed = 5f; // Units per second for pitch change smoothing

    public override void Awake()
    {


        base.Awake();

        sharedBuffer = new float[MAX_BUFFER_LENGTH];

        AudioSettings.GetDSPBufferSize(out bufferSize, out numBuffers);
        targetBuffering = bufferSize * 2;
        pitchAdjustThreshold = Mathf.Max(bufferSize / 2, 128);
        minPlaybackLagSamples = bufferSize * 2;

        //int minFreq = 0, maxFreq = 0;
        //Microphone.GetDeviceCaps(Microphone.devices[micID], out minFreq, out maxFreq);
        //Debug.Log("Microphone: " + micID + ", minFreq: " + minFreq + ", maxFreq: " + maxFreq);
    }

    int bufferSize, numBuffers;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        OVRManager.TrackingAcquired += trackingAcquired;
        OVRManager.TrackingLost += trackingLost;

        // todo: visualize that microphone is disabled
        if (SceneManager.GetActiveScene().buildIndex == (int)masterControl.Scenes.Relay
            || !Permission.HasUserAuthorizedPermission(Permission.Microphone)) return;

        SelectMic(0);
    }

    void Update()
    {
        // todo: visualize that microphone is disabled
        if (SceneManager.GetActiveScene().buildIndex == (int)masterControl.Scenes.Relay
          || !Permission.HasUserAuthorizedPermission(Permission.Microphone)) return;

        if (!activated) return;
        CheckAndCompensateDrift();
    }

    int micClipLength;
    int playbackHeadSamples, micHeadSamples;
    long playbackPositionSamples, micPositionSamples, driftSamples;
    [SerializeField] private int hardResyncThreshold = 8000;
    [SerializeField] private int pitchCompensationDeadband = 2560;
    [SerializeField] private int pitchCompensationResumeDelta = 512;
    public bool logDriftEvents = true;
    int lastProducedBlockId = 0;
    int lastConsumedBlockId = -1;
    bool staleBlockLogged = false;
    bool pitchFrozen = false;
    void CheckAndCompensateDrift()
    {
        if (activeDeviceString == null || micClip == null || audioSource.clip == null) return;

        micClipLength = audioSource.clip.samples; // Total samples in the mic clip
        playbackHeadSamples = audioSource.timeSamples;
        micHeadSamples = Microphone.GetPosition(activeDeviceString);

        if (!Microphone.IsRecording(activeDeviceString) || micClipLength <= 0) return;
        if (micHeadSamples < 0 || micHeadSamples > micClipLength || playbackHeadSamples < 0 || playbackHeadSamples > micClipLength) return;

        // Update total distances traveled, accounting for loops
        if (playbackHeadSamples < lastPlaybackHeadSamples) // wrap around
        {
            playbackTravelSamples += micClipLength; // Assuming playback clip is the same length as the recording
        }
        if (micHeadSamples < lastMicHeadSamples) // wrap around
        {
            micTravelSamples += micClipLength;
        }

        lastPlaybackHeadSamples = playbackHeadSamples;
        lastMicHeadSamples = micHeadSamples;

        playbackPositionSamples = playbackTravelSamples + playbackHeadSamples;
        micPositionSamples = micTravelSamples + micHeadSamples;

        int desiredLag = Mathf.Max(targetBuffering, minPlaybackLagSamples);
        driftSamples = micPositionSamples - desiredLag - playbackPositionSamples;

        if (micPositionSamples - playbackPositionSamples < desiredLag)
        {
            int targetReadPos = Mathf.Clamp(micHeadSamples - desiredLag, 0, micClipLength - 1);
            audioSource.timeSamples = targetReadPos;
            playbackTravelSamples = micTravelSamples;
            lastPlaybackHeadSamples = targetReadPos;
            lastMicHeadSamples = micHeadSamples;
            if (logDriftEvents)
            {
                Debug.LogFormat("[MicDrift] Enforced min lag: micHead={0}, targetReadPos={1}, desiredLag={2}", micHeadSamples, targetReadPos, desiredLag);
            }
            return;
        }

        if (System.Math.Abs(driftSamples) > hardResyncThreshold)
        {
            int targetReadPos = Mathf.Clamp(micHeadSamples - desiredLag, 0, micClipLength - 1);
            audioSource.timeSamples = targetReadPos;
            playbackTravelSamples = micTravelSamples;
            lastPlaybackHeadSamples = targetReadPos;
            lastMicHeadSamples = micHeadSamples;
            if (logDriftEvents)
            {
                Debug.LogFormat("[MicDrift] Hard resync: micHead={0}, targetReadPos={1}, drift={2}", micHeadSamples, targetReadPos, driftSamples);
            }
            return;
        }

        int freezeThreshold = pitchCompensationDeadband;
        int resumeThreshold = pitchCompensationDeadband + pitchCompensationResumeDelta;

        if (pitchFrozen)
        {
            if (System.Math.Abs(driftSamples) >= resumeThreshold)
            {
                pitchFrozen = false;
            }
            else
            {
                audioSource.pitch = 1.0f;
                return;
            }
        }

        if (System.Math.Abs(driftSamples) <= freezeThreshold)
        {
            audioSource.pitch = 1.0f;
            pitchFrozen = true;
            return;
        }

        if (System.Math.Abs(driftSamples) > pitchAdjustThreshold)
        {
            float targetPitch = Utils.map(Mathf.Clamp((float)driftSamples, -2000f, 2000f), -2000f, 2000f, 0.97f, 1.03f);
            audioSource.pitch = Mathf.MoveTowards(audioSource.pitch, targetPitch, pitchAdjustSpeed * Time.unscaledDeltaTime);
        }
        else
        {
            audioSource.pitch = Mathf.MoveTowards(audioSource.pitch, 1.0f, pitchAdjustSpeed * Time.unscaledDeltaTime);
        }

        //Debug.LogFormat("readHead: {0}, writeHead: {1}, distance: {2}, pitch: {3}", correctedPlaybackPosition.ToString("0000000000"), correctedMicPosition.ToString("0000000000"), drift.ToString("0000000000"), audioSource.pitch);
    }


    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) SelectMic(0); // restart mic in order to clear its buffer and avoid severe latency
    }

    void OnApplicationPause(bool isPaused)
    {
        if (!isPaused) SelectMic(0); // restart mic in order to clear its buffer and avoid severe latency
    }


    public void trackingAcquired()
    {
        SelectMic(0); // restart mic in order to clear its buffer and avoid severe latency
    }

    public void trackingLost()
    {
        SelectMic(0); // restart mic in order to clear its buffer and avoid severe latency
    }

    void OnDestroy()
    {
        OVRManager.TrackingAcquired -= trackingAcquired;
        OVRManager.TrackingLost -= trackingLost;
    }


    Coroutine _MicActivateRoutine;
    void SelectMic(int num)
    {
        if (num >= Microphone.devices.Length)
        {
            return;
        }

        if (_MicActivateRoutine != null) StopCoroutine(_MicActivateRoutine);
        _MicActivateRoutine = StartCoroutine(MicActivateRoutine(num));
    }

    IEnumerator MicActivateRoutine(int num)
    {
        audioSource.Stop();
        activated = false;
        micID = num;
        activeDeviceString = Microphone.devices[micID];
        Microphone.End(activeDeviceString);

        int requestedSampleRate = AudioSettings.outputSampleRate;
        int minFreq;
        int maxFreq;
        Microphone.GetDeviceCaps(activeDeviceString, out minFreq, out maxFreq);
        Debug.Log("Selected Microphone: " + activeDeviceString + ", minFreq: " + minFreq + ", maxFreq: " + maxFreq);

        if (minFreq != 0 && requestedSampleRate < minFreq)
        {
            requestedSampleRate = minFreq;
        }
        if (maxFreq != 0 && requestedSampleRate > maxFreq)
        {
            requestedSampleRate = maxFreq;
        }

        micClip = Microphone.Start(activeDeviceString, true, 100, requestedSampleRate);

        yield return null;
        if (micClip != null)
        {
            Debug.LogFormat("Microphone started: {0}, requested SR={1}, clip SR={2}, output SR={3}", activeDeviceString, requestedSampleRate, micClip.frequency, AudioSettings.outputSampleRate);
            audioSource.clip = micClip;
            audioSource.loop = true;
            while (Microphone.GetPosition(activeDeviceString) <= targetBuffering) { yield return null; }
            // waits until there are n samples in the buffer in order to avoid drop outs
            fadeInProgress = 0f;
            audioSource.Play();
        }

        // trigger a global sync, since spawning the Microphone can introduce severe lag
        // please note that this will be called from host and all clients, so the sync will be called each time a Microphone is instantiated and fully initialized
        NetworkSyncEventManager.Instance.UpdateSync();

        yield return null;

    }



    private void OnAudioFilterRead(float[] buffer, int channels)
    {

        activated = true;
        lastProducedBlockId++;
        if (sharedBuffer.Length != buffer.Length)
            System.Array.Resize(ref sharedBuffer, buffer.Length);

        CopyArray(buffer, sharedBuffer, buffer.Length); // copy over for processBuffer, this is probably thread-safe since both calls are supposed to be in the audio thread
        SetArrayToSingleValue(buffer, buffer.Length, 0.0f); // this zeroes the buffer that is directly played from the AudioSource, so that only the OSL patch is heard
    }

    public override void processBufferImpl(float[] buffer, double dspTime, int channels)
    {
        if (!active || !activated)
        {
            return;
        }

        if (lastProducedBlockId == lastConsumedBlockId)
        {
            if (!staleBlockLogged && logDriftEvents)
            {
                Debug.Log("[MicOrder] Stale buffer reused (processBufferImpl called before new OnAudioFilterRead)");
                staleBlockLogged = true;
            }
        }
        else
        {
            lastConsumedBlockId = lastProducedBlockId;
            staleBlockLogged = false;
        }

        MicFunction(buffer, sharedBuffer, buffer.Length, amp);

        if (fadeInProgress < 1f)
        {
            float fadeStep = 1f / Mathf.Max(1, fadeInSampleCount);
            int totalSamples = buffer.Length / channels;
            for (int sample = 0; sample < totalSamples; sample++)
            {
                fadeInProgress = Mathf.Min(1f, fadeInProgress + fadeStep);
                float gain = fadeInProgress;
                int baseIndex = sample * channels;
                for (int channel = 0; channel < channels; channel++)
                {
                    buffer[baseIndex + channel] *= gain;
                }
            }
        }
    }
}
