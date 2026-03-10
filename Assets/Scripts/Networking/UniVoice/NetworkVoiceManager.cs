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

using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Adrenak.UniVoice;
using Adrenak.UniVoice.Filters;
using Adrenak.UniVoice.Inputs;
using Adrenak.UniVoice.Networks;
using Adrenak.UniVoice.Outputs;
using Adrenak.UniMic;

using UnityEngine.Android;
using System.Collections;
using Adrenak.UniVoice.Samples;

public class NetworkVoiceManager : MonoBehaviour
{
    const int microphoneFrameDurationMs = 60;
    const int opusBitrate = 16000;
    const int opusResamplerQuality = 1;
    const int opusEncoderComplexity = 1;
    const int jitterStartupFrames = 2;
    const int jitterMaxBufferedFrames = 4;
    const int jitterReorderWindowMs = 80;

    IAudioServer<int> audioServer;
    IAudioClient<int> audioClient;
    ClientSession<int> clientSession;
    JitterBufferedAudioClient jitterBufferedAudioClient;

    bool requestedMicrophonePermission;
    bool microphoneInputReady;
    float nextMicrophoneCheckTime;

    [Header("UI")]

    public bool displaySpectrum = false;
    public TMP_Text menuMessage;
    public Transform peerViewContainer;
    public PeerView peerViewTemplate;
    public Toggle muteSelfToggle;
    public Toggle muteOthersToggle;

    Dictionary<int, PeerView> peerViews = new Dictionary<int, PeerView>();

    IEnumerator Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Mic.Init();

        // Relay host/client startup is triggered very early in this scene. UniVoice needs to
        // subscribe to Mirror transport callbacks before those connections happen, otherwise the
        // chatroom never sees peers join even though gameplay networking is already connected.
        InitializeVoice();
        InitializeMenu();

#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission("android.permission.RECORD_AUDIO"))
        {
            requestedMicrophonePermission = true;
            Permission.RequestUserPermission("android.permission.RECORD_AUDIO");
        }
#endif

        TryEnableMicrophoneInput();
        yield return null;
    }

    void OnDestroy()
    {
        clientSession?.Dispose();
        audioServer?.Dispose();
    }

    void InitializeVoice()
    {
        audioServer = new MirrorServer();
        jitterBufferedAudioClient = new JitterBufferedAudioClient(
            new MirrorClient(),
            jitterStartupFrames,
            jitterMaxBufferedFrames,
            jitterReorderWindowMs
        );
        audioClient = jitterBufferedAudioClient;
        clientSession = new ClientSession<int>(
            audioClient,
            new EmptyAudioInput(),
            new StreamedAudioSourceOutput.Factory()
        );

        clientSession.InputFilters.Add(new ConcentusEncodeFilter(
            ConcentusFrequencies.Frequency_16000,
            opusResamplerQuality,
            opusEncoderComplexity,
            opusBitrate
        ));
        clientSession.AddOutputFilter<ConcentusDecodeFilter>(() => new ConcentusDecodeFilter());

        audioServer.OnServerStart += () =>
        {
            ShowMessage("Chatroom created!");
        };

        audioServer.OnServerStop += () =>
        {
            ShowMessage("You closed the chatroom! All peers have been kicked");
        };

        audioClient.OnJoined += (id, peerIds) =>
        {
            ShowMessage("Joined chatroom ");
            ShowMessage("You are Peer ID " + id);
        };

        audioClient.OnLeft += () =>
        {
            ShowMessage("You left the chatroom");
        };

        audioClient.OnPeerJoined += id =>
        {
            var view = Instantiate(peerViewTemplate, peerViewContainer);
            view.IncomingAudio = !audioClient.YourVoiceSettings.mutedPeers.Contains(id);
            view.OutgoingAudio = !audioClient.YourVoiceSettings.deafenedPeers.Contains(id);

            view.OnIncomingModified += value =>
            {
                audioClient.YourVoiceSettings.SetMute(id, !value);
                audioClient.SubmitVoiceSettings();
                ApplyPeerIncomingAudio(id, value);
            };

            view.OnOutgoingModified += value =>
            {
                audioClient.YourVoiceSettings.SetDeaf(id, !value);
                audioClient.SubmitVoiceSettings();
            };

            peerViews.Add(id, view);
            view.SetPeerID(id);
            ApplyPeerIncomingAudio(id, view.IncomingAudio);
        };

        audioClient.OnPeerLeft += id =>
        {
            if (!peerViews.TryGetValue(id, out var peerViewInstance)) return;
            Destroy(peerViewInstance.gameObject);
            peerViews.Remove(id);
        };

        audioClient.YourVoiceSettings.muteAll = false;
        audioClient.YourVoiceSettings.deafenAll = false;
        ApplyGlobalAudioSettings();
    }


    void InitializeMenu()
    {
        muteSelfToggle.SetIsOnWithoutNotify(audioClient != null && audioClient.YourVoiceSettings.deafenAll);
        muteSelfToggle.onValueChanged.AddListener(value => {
            if (audioClient == null) return;
            audioClient.YourVoiceSettings.deafenAll = value;
            audioClient.SubmitVoiceSettings();
            ApplyGlobalAudioSettings();
        });

        muteOthersToggle.SetIsOnWithoutNotify(audioClient != null && audioClient.YourVoiceSettings.muteAll);
        muteOthersToggle.onValueChanged.AddListener(value => {
            if (audioClient == null) return;
            audioClient.YourVoiceSettings.muteAll = value;
            audioClient.SubmitVoiceSettings();
            ApplyGlobalAudioSettings();
        });
    }

    public int GetAgentID()
    {
        if (audioClient != null)
            return audioClient.ID;
        return -1;
    }

    void Update()
    {
        jitterBufferedAudioClient?.Tick();

        if (!microphoneInputReady && Time.unscaledTime >= nextMicrophoneCheckTime)
        {
            TryEnableMicrophoneInput();
        }

        if (clientSession == null || clientSession.PeerOutputs == null || !displaySpectrum) return;

        foreach (var output in clientSession.PeerOutputs)
        {
            if (peerViews.ContainsKey(output.Key))
            {
                /*
                 * This is an inefficient way of showing a part of the
                 * audio source spectrum. AudioSource.GetSpectrumData returns
                 * frequency values up to 24000 Hz in some cases. Most human
                 * speech is no more than 5000 Hz. Showing the entire spectrum
                 * will therefore lead to a spectrum where much of it doesn't
                 * change. So we take only the spectrum frequencies between
                 * the average human vocal range.
                 *
                 * Great source of information here:
                 * http://answers.unity.com/answers/158800/view.html
                 */
                var size = 512;
                var minVocalFrequency = 50;
                var maxVocalFrequency = 8000;
                var sampleRate = AudioSettings.outputSampleRate;
                var frequencyResolution = sampleRate / 2 / size;

                var audioSource = GetSourceOutput(output.Key);
                if (audioSource == null) continue;
                var spectrumData = new float[size];
                audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

                var indices = Enumerable.Range(0, size - 1).ToList();
                var minVocalFrequencyIndex = indices.Min(x => (Mathf.Abs(x * frequencyResolution - minVocalFrequency), x)).x;
                var maxVocalFrequencyIndex = indices.Min(x => (Mathf.Abs(x * frequencyResolution - maxVocalFrequency), x)).x;
                var indexRange = maxVocalFrequencyIndex - minVocalFrequencyIndex;
                if (indexRange <= 0) continue;

                spectrumData = spectrumData.Select(x => 1000 * x)
                    .ToList()
                    .GetRange(minVocalFrequencyIndex, indexRange)
                    .ToArray();
                peerViews[output.Key].DisplaySpectrum(spectrumData);
            }
        }
    }

    void ShowMessage(object obj)
    {
        Debug.Log("<color=blue>" + obj + "</color>");
        if (menuMessage != null)
            menuMessage.text = obj.ToString();
    }

    void TryEnableMicrophoneInput()
    {
        nextMicrophoneCheckTime = Time.unscaledTime + 1f;
        if (clientSession == null || microphoneInputReady) return;

#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission("android.permission.RECORD_AUDIO"))
        {
            if (!requestedMicrophonePermission)
            {
                requestedMicrophonePermission = true;
                Permission.RequestUserPermission("android.permission.RECORD_AUDIO");
            }
            return;
        }
#endif

        if (Mic.AvailableDevices.Count == 0) return;

        var mic = Mic.AvailableDevices[0];
        mic.StartRecording(microphoneFrameDurationMs);
        clientSession.Input = new MonotonicUniMicInput(mic);
        microphoneInputReady = true;
    }

    void ApplyGlobalAudioSettings()
    {
        if (clientSession == null || audioClient == null) return;
        clientSession.InputEnabled = !audioClient.YourVoiceSettings.deafenAll;
        clientSession.OutputsEnabled = !audioClient.YourVoiceSettings.muteAll;
    }

    void ApplyPeerIncomingAudio(int id, bool allowIncomingAudio)
    {
        var audioSource = GetSourceOutput(id);
        if (audioSource != null)
            audioSource.mute = !allowIncomingAudio;
    }

    public AudioSource GetSourceOutput(int id)
    {
        if (clientSession == null || clientSession.PeerOutputs == null) return null;
        if (!clientSession.PeerOutputs.TryGetValue(id, out var audioOutput)) return null;
        if (audioOutput is StreamedAudioSourceOutput streamedOutput)
            return streamedOutput.Stream.UnityAudioSource;
        return null;
    }

    public AudioSource GetSourceOutput(short id)
    {
        return GetSourceOutput((int)id);
    }
}
