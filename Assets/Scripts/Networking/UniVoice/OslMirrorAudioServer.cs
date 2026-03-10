#if MIRROR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Adrenak.BRW;
using Adrenak.UniVoice;
using Adrenak.UniVoice.Networks;

using Mirror;

using UnityEngine;

using Utp;

public class OslMirrorAudioServer : IAudioServer<int>
{
    const string tag = "[OslMirrorAudioServer]";

    public event Action OnServerStart;
    public event Action OnServerStop;
    public event Action OnClientVoiceSettingsUpdated;

    public List<int> ClientIDs { get; private set; }
    public Dictionary<int, VoiceSettings> ClientVoiceSettings { get; private set; }

    readonly MirrorModeObserver mirrorEvents;

    public OslMirrorAudioServer()
    {
        ClientIDs = new List<int>();
        ClientVoiceSettings = new Dictionary<int, VoiceSettings>();

        mirrorEvents = MirrorModeObserver.New("for OslMirrorAudioServer");
        mirrorEvents.ModeChanged += onModeChanged;

        NetworkServer.RegisterHandler<MirrorMessage>(onReceivedMessage, false);
    }

    public void Dispose()
    {
        mirrorEvents.ModeChanged -= onModeChanged;
        NetworkServer.UnregisterHandler<MirrorMessage>();
        onServerShutdown();
    }

    void onServerStarted()
    {
#if MIRROR_89_OR_NEWER
        NetworkManager.singleton.transport.OnServerConnectedWithAddress += onServerConnected;
#else
        NetworkManager.singleton.transport.OnServerConnected += onServerConnected;
#endif
        NetworkManager.singleton.transport.OnServerDisconnected += onServerDisconnected;
        OnServerStart?.Invoke();
    }

    void onServerShutdown()
    {
#if MIRROR_89_OR_NEWER
        NetworkManager.singleton.transport.OnServerConnectedWithAddress -= onServerConnected;
#else
        NetworkManager.singleton.transport.OnServerConnected -= onServerConnected;
#endif
        NetworkManager.singleton.transport.OnServerDisconnected -= onServerDisconnected;
        ClientIDs.Clear();
        ClientVoiceSettings.Clear();
        OnServerStop?.Invoke();
    }

    void onModeChanged(NetworkManagerMode oldMode, NetworkManagerMode newMode)
    {
        NetworkServer.ReplaceHandler<MirrorMessage>(onReceivedMessage, false);

        if (newMode == NetworkManagerMode.Host)
        {
            onServerStarted();
#if MIRROR_89_OR_NEWER
            onServerConnected(0, "localhost");
#else
            onServerConnected(0);
#endif
        }
        else if (newMode == NetworkManagerMode.ServerOnly)
        {
            if (oldMode == NetworkManagerMode.Host)
            {
                onServerDisconnected(0);
            }
            else if (oldMode == NetworkManagerMode.Offline)
            {
                onServerStarted();
            }
        }
        else if (newMode == NetworkManagerMode.Offline && (oldMode == NetworkManagerMode.ServerOnly || oldMode == NetworkManagerMode.Host))
        {
            if (oldMode == NetworkManagerMode.Host)
            {
                onServerDisconnected(0);
            }

            onServerShutdown();
        }
    }

    void onReceivedMessage(NetworkConnectionToClient connection, MirrorMessage message)
    {
        var clientId = connection.connectionId;
        var reader = new BytesReader(message.data);
        var messageTag = reader.ReadString();

        if (messageTag.Equals(MirrorMessageTags.AUDIO_FRAME))
        {
            var peersToForwardAudioTo = ClientIDs.Where(id => id != clientId);

            if (ClientVoiceSettings.TryGetValue(clientId, out var senderSettings))
            {
                if (senderSettings.deafenAll)
                {
                    return;
                }

                peersToForwardAudioTo = peersToForwardAudioTo
                    .Where(id => !senderSettings.deafenedPeers.Contains(id))
                    .Where(peerId =>
                    {
                        if (!ClientVoiceSettings.TryGetValue(peerId, out var peerVoiceSettings))
                        {
                            return true;
                        }

                        bool hasDeafenedPeer = senderSettings.deafenedTags.Intersect(peerVoiceSettings.myTags).Any();
                        return !hasDeafenedPeer;
                    });
            }

            foreach (var recipient in peersToForwardAudioTo)
            {
                if (ClientVoiceSettings.TryGetValue(recipient, out var recipientSettings))
                {
                    if (recipientSettings.muteAll)
                    {
                        continue;
                    }

                    if (recipientSettings.mutedPeers.Contains(clientId))
                    {
                        continue;
                    }

                    if (senderSettings != null && recipientSettings.mutedTags.Intersect(senderSettings.myTags).Any())
                    {
                        continue;
                    }
                }

                sendToClient(recipient, message.data, UtpTransport.VoiceChannel);
            }
        }
        else if (messageTag.Equals(MirrorMessageTags.VOICE_SETTINGS))
        {
            var voiceSettings = new VoiceSettings
            {
                muteAll = reader.ReadInt() == 1,
                mutedPeers = reader.ReadIntArray().ToList(),
                deafenAll = reader.ReadInt() == 1,
                deafenedPeers = reader.ReadIntArray().ToList(),
                myTags = reader.ReadStringArray().ToList(),
                mutedTags = reader.ReadStringArray().ToList(),
                deafenedTags = reader.ReadStringArray().ToList()
            };

            if (ClientVoiceSettings.ContainsKey(clientId))
            {
                ClientVoiceSettings[clientId] = voiceSettings;
            }
            else
            {
                ClientVoiceSettings.Add(clientId, voiceSettings);
            }

            OnClientVoiceSettingsUpdated?.Invoke();
        }
    }

#if MIRROR_89_OR_NEWER
    void onServerConnected(int connId, string addr)
#else
    void onServerConnected(int connId)
#endif
    {
        NetworkServer.ReplaceHandler<MirrorMessage>(onReceivedMessage, false);

        Debug.unityLogger.Log(LogType.Log, tag, $"Client {connId} connected");
        ClientIDs.Add(connId);

        foreach (var peer in ClientIDs)
        {
            if (peer == connId)
            {
                var otherPeerIds = ClientIDs.Where(id => id != connId).ToArray();
                var newClientPacket = new BytesWriter()
                    .WriteString(MirrorMessageTags.PEER_INIT)
                    .WriteInt(connId)
                    .WriteIntArray(otherPeerIds);

                sendToClientDelayed(connId, newClientPacket.Bytes, Channels.Reliable, 100);

                string log = $"Initializing new client with ID {connId}";
                if (otherPeerIds.Length > 0)
                {
                    log += $" and peer list {string.Join(", ", otherPeerIds)}";
                }

                Debug.unityLogger.Log(LogType.Log, tag, log);
            }
            else
            {
                var newPeerNotifyPacket = new BytesWriter()
                    .WriteString(MirrorMessageTags.PEER_JOINED)
                    .WriteInt(connId);

                Debug.unityLogger.Log(LogType.Log, tag, $"Notified client {peer} about new client {connId}");
                sendToClient(peer, newPeerNotifyPacket.Bytes, Channels.Reliable);
            }
        }
    }

    void onServerDisconnected(int connId)
    {
        NetworkServer.ReplaceHandler<MirrorMessage>(onReceivedMessage, false);

        ClientIDs.Remove(connId);
        Debug.unityLogger.Log(LogType.Log, tag, $"Client {connId} disconnected");

        foreach (var peerId in ClientIDs)
        {
            var packet = new BytesWriter()
                .WriteString(MirrorMessageTags.PEER_LEFT)
                .WriteInt(connId);

            Debug.unityLogger.Log(LogType.Log, tag, $"Notified client {peerId} about {connId} leaving");
            sendToClient(peerId, packet.Bytes, Channels.Reliable);
        }
    }

    async void sendToClientDelayed(int peerId, byte[] bytes, int channel, int delayMs)
    {
        await Task.Delay(delayMs);
        sendToClient(peerId, bytes, channel);
    }

    void sendToClient(int clientConnId, byte[] bytes, int channel)
    {
        var message = new MirrorMessage
        {
            data = bytes
        };

        var connection = getConnectionToClient(clientConnId);
        if (connection != null)
        {
            connection.Send(message, channel);
        }
    }

    NetworkConnectionToClient getConnectionToClient(int connectionId)
    {
        foreach (var connection in NetworkServer.connections)
        {
            if (connection.Key == connectionId)
            {
                return connection.Value;
            }
        }

        return null;
    }
}
#endif
