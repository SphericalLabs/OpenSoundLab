using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Adrenak.UniVoice;

[RequireComponent(typeof(AudioSource))]
public class UniVoiceBusRecorder : MonoBehaviour
{
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

    private int loops = 0;
    private int readAbsPos = 0;
    private int prevPos = 0;
    private float[] temp;

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

    public void StartRecording(int frequency = 16000, int sampleDurationMS = 10)
    {
        StopRecording();
        IsRecording = true;

        Frequency = frequency;
        SampleDurationMS = sampleDurationMS;

        //AudioClip = Microphone.Start(CurrentDeviceName, true, 1, Frequency);
        Sample = new float[Frequency / 1000 * SampleDurationMS * 2];

        temp = new float[Sample.Length];
        //StartCoroutine(ReadRawAudio());
    }

    public void StopRecording()
    {
        IsRecording = false;

        Destroy(AudioClip);
        AudioClip = null;

        StopCoroutine(ReadRawAudio());
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        while (IsRecording)
        {
            bool isNewDataAvailable = true;

            while (isNewDataAvailable)
            {
                int currPos /*= Microphone.GetPosition(CurrentDeviceName);*/ = 0; //= 0; so no errors are thrown. it does nothing
                if (currPos < prevPos)
                    loops++;
                prevPos = currPos;

                var currAbsPos = loops * data.Length + currPos;
                var nextReadAbsPos = readAbsPos + temp.Length;

                if (nextReadAbsPos < currAbsPos)
                {
                    temp[readAbsPos] = data[currPos];
                    AudioClip.GetData(temp, readAbsPos % AudioClip.samples);

                    Sample = temp;
                    m_SampleCount++;
                    OnSampleReady?.Invoke(m_SampleCount, Sample);

                    readAbsPos = nextReadAbsPos;
                    isNewDataAvailable = true;
                }
                else
                {
                    isNewDataAvailable = false;
                }
            }
        }
    }

    IEnumerator ReadRawAudio()
    {
        while (IsRecording)
        {
            bool isNewDataAvailable = true;

            while (isNewDataAvailable)
            {
                int currPos /*= Microphone.GetPosition(CurrentDeviceName);*/ = 0; //= 0; so no errors are thrown. it does nothing
                if (currPos < prevPos)
                    loops++;
                prevPos = currPos;

                var currAbsPos = loops * AudioClip.samples + currPos;
                var nextReadAbsPos = readAbsPos + temp.Length;

                if (nextReadAbsPos < currAbsPos)
                {
                    AudioClip.GetData(temp, readAbsPos % AudioClip.samples);

                    Sample = temp;
                    m_SampleCount++;
                    OnSampleReady?.Invoke(m_SampleCount, Sample);

                    readAbsPos = nextReadAbsPos;
                    isNewDataAvailable = true;
                }
                else
                    isNewDataAvailable = false;
            }
            yield return null;
        }
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
    }
    #endregion
}
