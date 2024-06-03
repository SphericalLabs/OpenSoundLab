using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using Adrenak.UniVoice;

[RequireComponent(typeof(AudioSource))]
public class UniVoiceBusRecorder : MonoBehaviour
{
    public AudioSource audioSource;

    public bool IsRecording { get; private set; }

    public int Frequency { get; private set; }

    public float[] Sample { get; private set; }

    public int SampleDurationMS { get; private set; }

    public int SampleLength
    {
        get { return Frequency * SampleDurationMS / 1000; }
    }

    public AudioClip AudioClip { get; private set; }
    int m_SampleCount = 0;

    public event Action<int, float[]> OnSampleReady;

    private int globalSampleReadPos = 0;
    private long lockedSamplePos;
    private int filterReadPos = 0;
    private static ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
    //private int loops = 0;
    //private int readAbsPos = 0;
    //private int prevPos = 0;
    //private float[] temp;
    //private int currPos = 0;

    #region METHODS

    static UniVoiceBusRecorder m_Instance;
    public static UniVoiceBusRecorder Instance
    {
        get
        {
            if (m_Instance == null)
                m_Instance = FindObjectOfType<UniVoiceBusRecorder>();
            if (m_Instance == null)
                m_Instance = new GameObject("UniVoiceAudioSampler").AddComponent<UniVoiceBusRecorder>();
            return m_Instance;
        }
    }
    /*  I'm yet to try out the OnAudioFilterRead approach, but pretty much the only requirement here is to implement the IAudioInput interface properly.
     *  Make sure that each interface property returns the right values: Frequency, ChannelCount and SegmentRate
     *  Ensure that you're firing the OnSegmentReady event with the correct values. 
     *  The length of the float array you provide in the OnSegmentReady event should align with the values of the property.
     *  The formula is Length = Frequency * ChannelCount / SegmentRate
     *  So if you're capturing at 48000Hz with one audio channel 10 times every second, it would be 48000 * 1 / 10 = 4800. 
     *  UniVoice then expects the float array in the OnSegmentReady event to have a length of 4800
     *  The int is basically a count of how many times you've fired the event. You need to keep track of it internally. 
     *  Univoice uses it for caching audio during playback as well as reordering them if they arrive in the wrong order.
     *  OnAudioFilterRead need not be called with data being the length you need. 
     *  So you need to maintain an array internally and fire it when it reaches 4800. Some reference on how UniMic does it is here
     *  Refer to this line: https://github.com/adrenak/univoice-sample/blob/0fea95dd2d747886afd89bc1682f8957c9b51784/Assets/Scripts/GroupVoiceCallSample.cs#L76
     *  Here:
     *  - 0 is the device index (the sample app assumes there is atleast one mic available on the device)
     *  - 16000 is the frequency of the mic input audio stream
     *  - 100 is the time (in milliseconds) that determines how often the audio is gathered and sent
     *  
     *  This means assuming there is one channel, every 100 milliseconds we expect a float of length 1600 to be prepared for UniVoice to send. 
     *  If there were 2 channels, that would be 3200
     *  
     *  So, UniVoiceUniMicInput is able to call the OnSegmentReady event every 100ms with a float array that is of consistent length.
     *  One more thing to consider when it comes to this is the length of the array itself. 
     *  If you're capturing at 48KHz every 100ms with a single channel, you'll end up with 4800 floats going out in every packet, which is 19200 bytes. 
     *  That's usually very large for a single packet especially on UDP, which is the recommended protocol. 
     *  I mention this because in Unity settings the frequency is usually set at 48000 or 44100 which is what you'll receive in OnAudioFilterReady
     *  Instead 16KHz single channel every 10ms would be 640 bytes which is much more suitable size wise. 
     *  For example using KcpTransport in Mirror networking has a limit of around 1500 bytes for a single packet.
     */

    public void PlayAudio()
    {
        Debug.Log("Play Audio");
        audioSource.PlayOneShot(audioSource.clip);
    }

    public void StartRecording(int frequency = 16000, int sampleDurationMS = 10)
    {
        Debug.Log("Start Recording Bus");
        StopRecording();
        IsRecording = true;
        //currPos = 0;
        //readAbsPos = 0;
        Frequency = frequency;
        SampleDurationMS = sampleDurationMS;

        AudioClip = GetComponent<AudioSource>().clip;
        Debug.Log("Audioclip present = " + (AudioClip != null));

        //AudioClip = Microphone.Start(CurrentDeviceName, true, 1, Frequency);
        Sample = new float[Frequency / 1000 * SampleDurationMS * 1];
        //temp = new float[Sample.Length];
        //Debug.Log("<color=green>Sample Lenght = " + Sample.Length + "</color>");
        //currPos = 0;
        //StartCoroutine(ReadRawAudio());
    }

    public void StopRecording()
    {
        IsRecording = false;

        Destroy(AudioClip);
        AudioClip = null;

        //StopCoroutine(ReadRawAudio());
    }

    

    // This method can be called from any thread to enqueue actions for the main thread
    public static void EnqueueMainThreadAction(Action action)
    {
        mainThreadActions.Enqueue(action);
    }

    // Call this method from Update() to execute all queued actions
    private void ExecuteMainThreadActions()
    {
        while (mainThreadActions.TryDequeue(out var action))
        {
            action?.Invoke();
        }
    }

    private void Update()
    {
        ExecuteMainThreadActions();
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        EnqueueMainThreadAction(() =>
        {
            if (!IsRecording)
                return;
            filterReadPos = 0;

            while (filterReadPos < data.Length)
            {
                if (globalSampleReadPos > Sample.Length - 1)
                {
                    m_SampleCount++;
                    globalSampleReadPos = 0;
                    Debug.Log("<color=red>OnSegmentReady</color>");
                    OnSampleReady?.Invoke(m_SampleCount, Sample);
                }

                Sample[globalSampleReadPos] = data[filterReadPos];
                globalSampleReadPos++;
                filterReadPos++;
            }
        });

        /*
        Debug.Log("<color=red>Data Lenght: " + data.Length + "</color>");
        temp[currPos] = data[readAbsPos];
        Debug.Log("Current Position = " + currPos + " / Current Data Pos: " + readAbsPos);
        readAbsPos++;
        if (currPos == Sample.Length -1)
        {
            Debug.Log("<color=red>OnSegmentReady</color>");
            Sample = temp;
            m_SampleCount++;
            currPos = 0;
            OnSampleReady?.Invoke(m_SampleCount, Sample);
            return;
        }
        currPos++;
        */

    }

    //private IEnumerator ReadAudioData(float[] data)
    //{
    //    while (IsRecording)
    //    {
    //        bool isNewDataAvailable = true;

    //        while (isNewDataAvailable)
    //        {
    //            //Get current position of microphone
    //            int currPos /*= Microphone.GetPosition(CurrentDeviceName);*/ = 0; //= 0; so no errors are thrown. it does nothing

    //            //If current Position is smaller than Previouse Position it looped
    //            if (currPos < prevPos)
    //                loops++;
    //            //Set previouse To current position
    //            prevPos = currPos;

    //            //abstract position? position if data was glued together?
    //            var currAbsPos = loops * data.Length + currPos;
    //            var nextReadAbsPos = readAbsPos + temp.Length;

    //            if (nextReadAbsPos < currAbsPos)
    //            {
    //                temp = data[readAbsPos..(data.Length - 1)];
    //                AudioClip.GetData(temp, readAbsPos % AudioClip.samples);
    //                Sample = temp;
    //                m_SampleCount++;
    //                OnSampleReady?.Invoke(m_SampleCount, Sample);

    //                readAbsPos = nextReadAbsPos;
    //                isNewDataAvailable = true;
    //            }
    //            else
    //                isNewDataAvailable = false;
    //        }
    //        yield return null;
    //    }
    //}

    //IEnumerator ReadRawAudio()
    //{
    //    while (IsRecording)
    //    {
    //        bool isNewDataAvailable = true;

    //        while (isNewDataAvailable)
    //        {
    //            //Get current position of microphone
    //            int currPos /*= Microphone.GetPosition(CurrentDeviceName);*/ = 0; //= 0; so no errors are thrown. it does nothing

    //            //If current Position is smaller than Previouse Position it looped
    //            if (currPos < prevPos)
    //                loops++;
    //            //Set previouse To current position
    //            prevPos = currPos;

    //            //abstract position? position if data was glued together?
    //            var currAbsPos = loops * AudioClip.samples + currPos;
    //            var nextReadAbsPos = readAbsPos + temp.Length;

    //            if (nextReadAbsPos < currAbsPos)
    //            {
    //                AudioClip.GetData(temp, readAbsPos % AudioClip.samples);

    //                Sample = temp;
    //                m_SampleCount++;
    //                OnSampleReady?.Invoke(m_SampleCount, Sample);

    //                readAbsPos = nextReadAbsPos;
    //                isNewDataAvailable = true;
    //            }
    //            else
    //                isNewDataAvailable = false;
    //        }
    //        yield return null;
    //    }
        //this was from chris' UniVoiceAudioSampler.cs file
        //--------------------------------------------------------
        //int loops = 0;
        //int readAbsPos = 0;
        //int prevPos = 0;
        //float[] temp = new float[Sample.Length];

        //while (AudioClip != null)
        //{
        //    bool isNewDataAvailable = true;

        //    while (isNewDataAvailable)
        //    {
        //        int currPos = 0; //Microphone.GetPosition(CurrentDeviceName);
        //        if (currPos < prevPos)
        //            loops++;
        //        prevPos = currPos;

        //        var currAbsPos = loops * AudioClip.samples + currPos;
        //        var nextReadAbsPos = readAbsPos + temp.Length;

        //        if (nextReadAbsPos < currAbsPos)
        //        {
        //            AudioClip.GetData(temp, readAbsPos % AudioClip.samples);

        //            Sample = temp;
        //            m_SampleCount++;
        //            OnSampleReady?.Invoke(m_SampleCount, Sample);

        //            readAbsPos = nextReadAbsPos;
        //            isNewDataAvailable = true;
        //        }
        //        else
        //            isNewDataAvailable = false;
        //    }
        //    yield return null;
        //}
    //}
    #endregion
}
