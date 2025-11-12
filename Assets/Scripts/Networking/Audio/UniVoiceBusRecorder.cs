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

﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using Adrenak.UniVoice;
using Newtonsoft.Json.Linq;
using UnityEngine.InputSystem;
using Unity.Mathematics;

[RequireComponent(typeof(AudioSource))]
public class UniVoiceBusRecorder : MonoBehaviour
{
    public AudioSource audioSource;

    public bool IsRecording { get; private set; }

    public int Frequency { get; private set; }

    public float[] SegmentData { get; private set; }

    public int SampleDurationMS { get; private set; }

    public int SampleLength
    {
        get { return Frequency * SampleDurationMS / 1000; }
    }

    public AudioClip AudioClip { get; private set; }
    int m_SampleCount = 0;

    public event Action<int, float[]> OnSampleReady;

    private int BridgingSegmentPointer = 0;
    private long lockedSamplePos;
    private int bufferPointer = 0;
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
        SegmentData = new float[Frequency / 1000 * SampleDurationMS * 2];
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

    int bufferSamplesdue = 0;

    public float frequency = 440f; // Frequency of the sine wave (in Hz)
    public float amplitude = 0.1f; // Amplitude of the sine wave

    private double sampleRate;     // Sample rate of the audio
    private double increment;      // Amount to increment the phase
    private double phase;          // Current phase of the sine wave

    void Start()
    {
        // Get the sample rate of the audio
        sampleRate = AudioSettings.outputSampleRate;
        increment = 2.0 * Mathf.PI * frequency / sampleRate;
    }

    private readonly object lockObject = new object(); // Lock object for thread safety

    private void Update()
    {
        if (!IsRecording)
            return;
        bufferPointer = 0;


        int localBufferSamplesDue;
        lock (lockObject)
        {
            localBufferSamplesDue = bufferSamplesdue;
            bufferSamplesdue = 0; // Reset samplesDue within the lock
        }

        while (bufferPointer < localBufferSamplesDue)
        {
            if (BridgingSegmentPointer > SegmentData.Length - 1)
            {
                m_SampleCount++;
                BridgingSegmentPointer = 0;
                //Debug.Log("<color=red>OnSegmentReady</color>"); // try not to use debug.log in the audio thread!
                OnSampleReady?.Invoke(m_SampleCount, SegmentData);
            }

            // sine wave
            SegmentData[BridgingSegmentPointer] = amplitude * Mathf.Sin((float)phase);
            SegmentData[BridgingSegmentPointer + 1] = SegmentData[BridgingSegmentPointer];

            // noise
            //SegmentData[BridgingSegmentPointer] = UnityEngine.Random.Range(-1f, 1f);
            //SegmentData[BridgingSegmentPointer + 1] = UnityEngine.Random.Range(-1f, 1f);

            // Increment the phase
            phase += increment;

            // Keep the phase in the range [0, 2π]
            if (phase > 2.0 * Mathf.PI)
            {
                phase -= 2.0 * Mathf.PI;
            }

            BridgingSegmentPointer += 2;
            bufferPointer += 2;
        }

    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        lock (lockObject)
        {
            bufferSamplesdue += data.Length;
        }
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
    //  while (IsRecording)
    //  {
    //    bool isNewDataAvailable = true;

    //    while (isNewDataAvailable)
    //    {
    //      //Get current position of microphone
    //      int currPos /*= Microphone.GetPosition(CurrentDeviceName);*/ = 0; //= 0; so no errors are thrown. it does nothing

    //      //If current Position is smaller than Previouse Position it looped
    //      if (currPos < prevPos)
    //        loops++;
    //      //Set previouse To current position
    //      prevPos = currPos;

    //      //abstract position? position if data was glued together?
    //      var currAbsPos = loops * data.Length + currPos;
    //      var nextReadAbsPos = readAbsPos + temp.Length;

    //      if (nextReadAbsPos < currAbsPos)
    //      {
    //        temp = data[readAbsPos..(data.Length - 1)];
    //        AudioClip.GetData(temp, readAbsPos % AudioClip.samples);
    //        Sample = temp;
    //        m_SampleCount++;
    //        OnSampleReady?.Invoke(m_SampleCount, Sample);

    //        readAbsPos = nextReadAbsPos;
    //        isNewDataAvailable = true;
    //      }
    //      else
    //        isNewDataAvailable = false;
    //    }
    //    yield return null;
    //  }
    //}

    //IEnumerator ReadRawAudio()
    //{
    //  while (IsRecording)
    //  {
    //    bool isNewDataAvailable = true;

    //    while (isNewDataAvailable)
    //    {
    //      //Get current position of microphone
    //      int currPos /*= Microphone.GetPosition(CurrentDeviceName);*/ = 0; //= 0; so no errors are thrown. it does nothing

    //      //If current Position is smaller than Previouse Position it looped
    //      if (currPos < prevPos)
    //        loops++;
    //      //Set previouse To current position
    //      prevPos = currPos;

    //      //abstract position? position if data was glued together?
    //      var currAbsPos = loops * AudioClip.samples + currPos;
    //      var nextReadAbsPos = readAbsPos + temp.Length;

    //      if (nextReadAbsPos < currAbsPos)
    //      {
    //        AudioClip.GetData(temp, readAbsPos % AudioClip.samples);

    //        Sample = temp;
    //        m_SampleCount++;
    //        OnSampleReady?.Invoke(m_SampleCount, Sample);

    //        readAbsPos = nextReadAbsPos;
    //        isNewDataAvailable = true;
    //      }
    //      else
    //        isNewDataAvailable = false;
    //    }
    //    yield return null;
    //  }

    //  //this was from chris' UniVoiceAudioSampler.cs file
    //  --------------------------------------------------------
    //  int loops = 0;
    //  int readAbsPos = 0;
    //  int prevPos = 0;
    //  float[] temp = new float[Sample.Length];

    //  while (AudioClip != null)
    //  {
    //    bool isNewDataAvailable = true;

    //    while (isNewDataAvailable)
    //    {
    //      int currPos = 0; //Microphone.GetPosition(CurrentDeviceName);
    //      if (currPos < prevPos)
    //        loops++;
    //      prevPos = currPos;

    //      var currAbsPos = loops * AudioClip.samples + currPos;
    //      var nextReadAbsPos = readAbsPos + temp.Length;

    //      if (nextReadAbsPos < currAbsPos)
    //      {
    //        AudioClip.GetData(temp, readAbsPos % AudioClip.samples);

    //        Sample = temp;
    //        m_SampleCount++;
    //        OnSampleReady?.Invoke(m_SampleCount, Sample);

    //        readAbsPos = nextReadAbsPos;
    //        isNewDataAvailable = true;
    //      }
    //      else
    //        isNewDataAvailable = false;
    //    }
    //    yield return null;
    //  }
    //}


    #endregion
}
