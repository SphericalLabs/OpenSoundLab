using Adrenak.UniMic;
using Adrenak.UniVoice;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class UniVoiceMasterBusRecorder : MonoBehaviour
{
    [DllImport("OSLNative")]
    static extern IntPtr MasterBusRecorder_GetRecorderInstance();
    [DllImport("OSLNative")]
    static extern void MasterBusRecorder_StartRecording();
    [DllImport("OSLNative")]
    static extern void MasterBusRecorder_StopRecording();
    [DllImport("OSLNative")]
    static extern bool MasterBusRecorder_ReadRecordedSample(ref float sample);
    [DllImport("OSLNative")]
    static extern float MasterBusRecorder_GetLevel_Lin();
    [DllImport("OSLNative")]
    static extern float MasterBusRecorder_GetLevel_dB();
    [DllImport("OSLNative")]
    static extern int MasterBusRecorder_GetBufferPointer(IntPtr buffer, ref int offset);
    [DllImport("OSLNative")]
    static extern void MasterBusRecorder_Clear();

    //types
    public enum State
    {
        Idle,
        Streaming,
        Finishing
    }

    //public
    public State state => _state;

    //properties with private backing fields
    public int bitDepth
    {
        get => _bitDepth;
        set
        {
            _bitDepth = value;
            // The number format for the size fields (chunksize and subchunk2size) in the WAV header are 32bit unsigned integer.
            // The overhead for the header is 36 bytes, so the actual maximum number of audio samples we can write into a WAV file is:
            maxFileSize = (uint.MaxValue - 36) / (uint)(_bitDepth / 8);
        }
    }
    int _bitDepth = 24;

    //private
    State _state;

    uint length;
    int instanceId;
    uint maxFileSize;

    //static
    static int instances = 0;


    static UniVoiceMasterBusRecorder m_Instance;
    public static UniVoiceMasterBusRecorder Instance
    {
        get
        {
            if (m_Instance == null)
                m_Instance = FindObjectOfType<UniVoiceMasterBusRecorder>();
            if (m_Instance == null)
                m_Instance = new GameObject("UniVoiceAudioSampler").AddComponent<UniVoiceMasterBusRecorder>();
            return m_Instance;
        }
    }

    private void Awake()
    {
        bitDepth = _bitDepth; //Set property so maxFileSize is updated
        this.instanceId = instances;
        instances++;
        Debug.Log("Created new MasterBusRecorder with instanceId " + instanceId);
    }

    private void Start()
    {

        //// We check if a metronome with a recButton exists (should have been assigned in metronome's Awake() method).
        //// We need this only in case a recording reaches the file size limit,
        //// but we check it here bc if we don't and future code changes break this,
        //// it will go unnoticed until someone actually reaches the file size limit.
        //var metronome = FindObjectOfType<metronome>();
        //if (metronome == null)
        //    Debug.LogError("masterBusRecorder: No reference to metronome is set. Cannot update rec button toggle state.");
        //var recButton = metronome.recButton;
        //if (recButton == null)
        //    Debug.LogError("masterBusRecorder: No reference to recButton is set. Cannot update rec button toggle state.");
    }

    private void Update()
    {

    }

    public int Frequency => AudioSettings.outputSampleRate;

    public int ChannelCount => GetNumberOfAudioChannels();


    public float[] segmentBuffer { get; private set; }

    // this is doubled!
    public int GetNumberOfAudioChannels()
    {
        AudioSpeakerMode speakerMode = AudioSettings.speakerMode;
        switch (speakerMode)
        {
            case AudioSpeakerMode.Mono:
                return 1;
            case AudioSpeakerMode.Stereo:
                return 2;
            case AudioSpeakerMode.Quad:
                return 4;
            case AudioSpeakerMode.Surround:
                return 5;
            case AudioSpeakerMode.Mode5point1:
                return 6;
            case AudioSpeakerMode.Mode7point1:
                return 8;
            case AudioSpeakerMode.Prologic:
                return 2; // Prologic usually indicates stereo but encoded with surround information
            default:
                return 2; // Default to stereo if unknown
        }
    }

    private void OnDestroy()
    {
        ///Tell native plugin to stop recording bc its lifecycle is not tied to this instance's:
        if (_state == State.Streaming)
        {
            StopRec();
        }
        ///In case there is an unfinished recording session, we stop the coroutines and force the session to be finished:
        if (_state != State.Idle)
        {
            StopAllCoroutines();
            OnRecordingFinished();
        }
    }

    public void ToggleRec(bool newState)
    {
        if (newState == false && _state == State.Streaming)
        {
            StopRec();
        }
        else if (newState == true && _state == State.Idle)
        {
            StartRec();
        }

        //These are only here for debugging and should be removed for production:
        else if (newState == false && _state == State.Finishing)
        {
            Debug.LogError("Cannot stop recording because the finishing process has already been started before.");
        }
        else if (newState == true && _state == State.Finishing)
        {
            Debug.LogError("Cannot start a new recording while an old one is being finished.");
        }
        else if (newState == false && state != State.Streaming)
        {
            Debug.LogError("Cannot stop recording because recorder is idle.");
        }
        else if (newState == true && state != State.Idle)
        {
            Debug.LogError("Cannot start a new recording while recorder is busy.");
        }
        else
        {
            Debug.LogError("Uncaught case in masterBusRecorderDeviceInterface.ToggleRec()");
        }
    }

    public void StartRec(int segmentRate = 10)
    {
        segmentBuffer = new float[AudioSettings.outputSampleRate / segmentRate * ChannelCount];

        Debug.Log("MasterBusRecorder: Recording started...");

        //Update the state before starting coroutine, otherwise it will terminate immediately:
        _state = State.Streaming;

        //Tell native code to start recording:
        MasterBusRecorder_StartRecording();

        //Start coroutine with custom onEnded action:
        Action onEnded = delegate () { OnRecordingFinished(); };
        StartCoroutine(QuerySamples(this, onEnded));
    }

    public void StopRec()
    {
        Debug.Log("MasterBusRecorder: Recording stopped, waiting for Coroutine to finish...");

        //Tell native code to stop recording:
        MasterBusRecorder_StopRecording();

        //Tell coroutine to terminate as soon as all queued samples have been read:
        _state = State.Finishing;
    }

    public void OnRecordingFinished()
    {
        Debug.Log("MasterBusRecorder: Recording finished.");
        Debug.Log("Length in samples: " + length);



        //Reset state so a new recording session can be started:
        _state = State.Idle;
    }

    public event Action<int, float[]> OnSegmentReady;
    int segCounter = 0;
    float sample = 0;
    int segIndex = 0;

    public IEnumerator QuerySamples(UniVoiceMasterBusRecorder recInterface, Action onEnded)
    {

        ///Repeatedly query native code for new samples
        while (recInterface.state != State.Idle)
        {
            if (MasterBusRecorder_ReadRecordedSample(ref sample))
            { // consider having this run in a while loop until all samples are read
                segmentBuffer[segIndex] = sample;
                segIndex++;
                if (segIndex == segmentBuffer.Length)
                {
                    //Debug.Log("OnSegmentReady");
                    OnSegmentReady?.Invoke(segCounter, segmentBuffer);
                    segIndex = 0;
                    segCounter++;
                }
            }
            else
            {
                yield return null;
            }
        }


        if (recInterface.state == State.Finishing) // 1) We consumed all samples because the recording has been stopped:
        {
            onEnded();
        }
        else // 2) We consumed all samples that are currently available, but new samples may be available in the future bc we are still streaming:
        {
            yield return null;
        }

    }

}
