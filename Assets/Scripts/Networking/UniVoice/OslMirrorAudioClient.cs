#if MIRROR
using System;
using System.Collections.Generic;
using System.Linq;

using Adrenak.BRW;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Networks;

using Mirror;

using UnityEngine;

using Utp;

public class OslMirrorAudioClient : IAudioClient<int>
{
    const string tag = "[OslMirrorAudioClient]";

    public int ID { get; private set; } = -1;
    public List<int> PeerIDs { get; private set; }
    public VoiceSettings YourVoiceSettings { get; private set; }

    public event Action<int, List<int>> OnJoined;
    public event Action OnLeft;
    public event Action<int> OnPeerJoined;
    public event Action<int> OnPeerLeft;
    public event Action<int, AudioFrame> OnReceivedPeerAudioFrame;

    readonly MirrorModeObserver mirrorEvents;

    public OslMirrorAudioClient()
    {
        PeerIDs = new List<int>();
        YourVoiceSettings = new VoiceSettings();

        mirrorEvents = MirrorModeObserver.New("for OslMirrorAudioClient");
        mirrorEvents.ModeChanged += onModeChanged;

        NetworkClient.RegisterHandler<MirrorMessage>(onReceivedMessage, false);
    }

    public void Dispose()
    {
        mirrorEvents.ModeChanged -= onModeChanged;
        NetworkClient.UnregisterHandler<MirrorMessage>();
        PeerIDs.Clear();
    }

    void onModeChanged(NetworkManagerMode oldMode, NetworkManagerMode newMode)
    {
        NetworkClient.ReplaceHandler<MirrorMessage>(onReceivedMessage);

        bool clientOnlyToOffline = newMode == NetworkManagerMode.Offline && oldMode == NetworkManagerMode.ClientOnly;
        bool hostToServerOnlyOrOffline = oldMode == NetworkManagerMode.Host;

        if (clientOnlyToOffline || hostToServerOnlyOrOffline)
        {
            if (clientOnlyToOffline)
            {
                NetworkClient.UnregisterHandler<MirrorMessage>();
            }

            onClientDisconnected();
        }
    }

    void onClientDisconnected()
    {
        YourVoiceSettings = new VoiceSettings();
        var oldPeerIds = PeerIDs;
        PeerIDs.Clear();
        ID = -1;

        foreach (var peerId in oldPeerIds)
        {
            OnPeerLeft?.Invoke(peerId);
        }

        OnLeft?.Invoke();
    }

    void onReceivedMessage(MirrorMessage message)
    {
        var reader = new BytesReader(message.data);
        var messageTag = reader.ReadString();

        switch (messageTag)
        {
            case MirrorMessageTags.PEER_INIT:
                ID = reader.ReadInt();
                PeerIDs = reader.ReadIntArray().ToList();

                string log = $"Initialized with ID {ID}. ";
                log += PeerIDs.Count > 0
                    ? $"Peer list: {string.Join(", ", PeerIDs)}"
                    : "There are currently no peers.";

                Debug.unityLogger.Log(LogType.Log, tag, log);
                OnJoined?.Invoke(ID, PeerIDs);

                foreach (var peerId in PeerIDs)
                {
                    OnPeerJoined?.Invoke(peerId);
                }
                break;

            case MirrorMessageTags.PEER_JOINED:
                var newPeerId = reader.ReadInt();
                if (!PeerIDs.Contains(newPeerId))
                {
                    PeerIDs.Add(newPeerId);
                    Debug.unityLogger.Log(LogType.Log, tag, $"Peer {newPeerId} joined. Peer list is now {string.Join(", ", PeerIDs)}");
                    OnPeerJoined?.Invoke(newPeerId);
                }
                break;

            case MirrorMessageTags.PEER_LEFT:
                var leftPeerId = reader.ReadInt();
                if (PeerIDs.Contains(leftPeerId))
                {
                    PeerIDs.Remove(leftPeerId);

                    string leftLog = $"Peer {leftPeerId} left. ";
                    leftLog += PeerIDs.Count == 0
                        ? "There are no peers anymore."
                        : $"Peer list is now {string.Join(", ", PeerIDs)}";

                    Debug.unityLogger.Log(LogType.Log, tag, leftLog);
                    OnPeerLeft?.Invoke(leftPeerId);
                }
                break;

            case MirrorMessageTags.AUDIO_FRAME:
                var sender = reader.ReadInt();
                if (sender == ID || !PeerIDs.Contains(sender))
                {
                    return;
                }

                var frame = new AudioFrame
                {
                    timestamp = reader.ReadLong(),
                    frequency = reader.ReadInt(),
                    channelCount = reader.ReadInt(),
                    samples = reader.ReadByteArray()
                };

                OnReceivedPeerAudioFrame?.Invoke(sender, frame);
                break;
        }
    }

    public void SendAudioFrame(AudioFrame frame)
    {
        if (ID == -1)
        {
            return;
        }

        var writer = new BytesWriter();
        writer.WriteString(MirrorMessageTags.AUDIO_FRAME);
        writer.WriteInt(ID);
        writer.WriteLong(frame.timestamp);
        writer.WriteInt(frame.frequency);
        writer.WriteInt(frame.channelCount);
        writer.WriteByteArray(frame.samples);

        var message = new MirrorMessage
        {
            data = writer.Bytes
        };

        NetworkClient.Send(message, UtpTransport.VoiceChannel);
    }

    public void SubmitVoiceSettings()
    {
        if (ID == -1)
        {
            return;
        }

        Debug.unityLogger.Log(tag, "Submitting : " + YourVoiceSettings);

        var writer = new BytesWriter();
        writer.WriteString(MirrorMessageTags.VOICE_SETTINGS);
        writer.WriteInt(YourVoiceSettings.muteAll ? 1 : 0);
        writer.WriteIntArray(YourVoiceSettings.mutedPeers.ToArray());
        writer.WriteInt(YourVoiceSettings.deafenAll ? 1 : 0);
        writer.WriteIntArray(YourVoiceSettings.deafenedPeers.ToArray());
        writer.WriteStringArray(YourVoiceSettings.myTags.ToArray());
        writer.WriteStringArray(YourVoiceSettings.mutedTags.ToArray());
        writer.WriteStringArray(YourVoiceSettings.deafenedTags.ToArray());

        var message = new MirrorMessage
        {
            data = writer.Bytes
        };

        NetworkClient.Send(message, Channels.Reliable);
    }

    public void UpdateVoiceSettings(Action<VoiceSettings> modification)
    {
        modification?.Invoke(YourVoiceSettings);
        SubmitVoiceSettings();
    }
}
#endif
