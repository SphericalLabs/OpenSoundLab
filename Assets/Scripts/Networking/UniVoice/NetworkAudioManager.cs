using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Adrenak.UniVoice;
using Adrenak.UniVoice.MirrorNetwork;
using Adrenak.UniVoice.UniMicInput;
using Adrenak.UniVoice.AudioSourceOutput;

using UnityEngine.Android;
using System.Collections;
using Adrenak.UniVoice.Samples;

public class NetworkAudioManager : MonoBehaviour
{
    ChatroomAgent agent;

    [Header("UI")]
    public Text menuMessage;
    public Transform peerViewContainer;
    public PeerView peerViewTemplate;
    public Toggle muteSelfToggle;
    public Toggle muteOthersToggle;

    Dictionary<short, PeerView> peerViews = new Dictionary<short, PeerView>();


    IEnumerator Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

#if UNITY_ANDROID
        while (!Permission.HasUserAuthorizedPermission("android.permission.RECORD_AUDIO"))
        {
            Permission.RequestUserPermission("android.permission.RECORD_AUDIO");
            yield return new WaitForSeconds(1);
        }
#endif
        yield return null;
        yield return new WaitForSeconds(1);

        InitializeAgent();
        InitializeMenu();
    }


    void InitializeAgent()
    {
        agent = new ChatroomAgent(
            new UniVoiceMirrorNetwork(),
            //new UniVoiceBusAudioInput(500),
            //new SinusAudioInput(0, 4000, 25),
            new UniVoiceUniMicInput(0, 8000, 25),
            new UniVoiceAudioSourceOutput.Factory(100, 30)
        );
        agent.Network.OnCreatedChatroom += () => {
            ShowMessage($"Chatroom created!\nYou are Peer ID {agent.Network.OwnID}");
        };

        agent.Network.OnChatroomCreationFailed += ex => {
            ShowMessage("Chatroom creation failed");
        };

        agent.Network.OnClosedChatroom += () => {
            ShowMessage("You closed the chatroom! All peers have been kicked");
        };

        agent.Network.OnJoinedChatroom += id => {
            ShowMessage("Joined chatroom ");
            ShowMessage("You are Peer ID " + id);
        };

        agent.Network.OnChatroomJoinFailed += ex => {
            ShowMessage(ex);
        };

        agent.Network.OnLeftChatroom += () => {
            ShowMessage("You left the chatroom");
        };

        agent.Network.OnPeerJoinedChatroom += id => {
            var view = Instantiate(peerViewTemplate, peerViewContainer);
            view.IncomingAudio = !agent.PeerSettings[id].muteThem;
            view.OutgoingAudio = !agent.PeerSettings[id].muteSelf;

            view.OnIncomingModified += value =>
                agent.PeerSettings[id].muteThem = !value;

            view.OnOutgoingModified += value =>
                agent.PeerSettings[id].muteSelf = !value;

            peerViews.Add(id, view);
            view.SetPeerID(id);
        };

        agent.Network.OnPeerLeftChatroom += id => {
            var peerViewInstance = peerViews[id];
            Destroy(peerViewInstance.gameObject);
            peerViews.Remove(id);
        };

        agent.MuteOthers = false;
        agent.MuteSelf = false;
    }


    void InitializeMenu()
    {
        muteSelfToggle.SetIsOnWithoutNotify(agent.MuteSelf);
        muteSelfToggle.onValueChanged.AddListener(value =>
            agent.MuteSelf = value);

        muteOthersToggle.SetIsOnWithoutNotify(agent.MuteOthers);
        muteOthersToggle.onValueChanged.AddListener(value =>
            agent.MuteOthers = value);
    }

    public short GetAgentID()
    {
        if (agent != null)
            return agent.Network.OwnID;
        return -1;
    }

    void Update()
    {
        if (agent == null || agent.PeerOutputs == null) return;
        
        foreach (var output in agent.PeerOutputs)
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

                var audioSource = (output.Value as UniVoiceAudioSourceOutput).AudioSource;
                var spectrumData = new float[size];
                audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

                var indices = Enumerable.Range(0, size - 1).ToList();
                var minVocalFrequencyIndex = indices.Min(x => (Mathf.Abs(x * frequencyResolution - minVocalFrequency), x)).x;
                var maxVocalFrequencyIndex = indices.Min(x => (Mathf.Abs(x * frequencyResolution - maxVocalFrequency), x)).x;
                var indexRange = maxVocalFrequencyIndex - minVocalFrequency;

                spectrumData = spectrumData.Select(x => 1000 * x)
                    .ToList()
                    .GetRange(minVocalFrequency, indexRange)
                    .ToArray();
                peerViews[output.Key].DisplaySpectrum(spectrumData);
            }
        }
    }

    void ShowMessage(object obj)
    {
        Debug.Log("<color=blue>" + obj + "</color>");
        menuMessage.text = obj.ToString();
    }

    public UniVoiceAudioSourceOutput GetSourceOutput(short id)
    {
        if (agent.PeerOutputs.ContainsKey(id) && agent.PeerOutputs.TryGetValue(id, out IAudioOutput audioOutput) && audioOutput is UniVoiceAudioSourceOutput)
        {
            return audioOutput as UniVoiceAudioSourceOutput;
        }
        return null;
    }
}
