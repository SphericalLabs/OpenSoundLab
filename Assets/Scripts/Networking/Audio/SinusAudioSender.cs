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

public class SinusAudioSender : MonoBehaviour
{
    public double bpm = 140.0F;
    public float gain = 0.5F;
    public int signatureHi = 4;
    public int signatureLo = 4;

    public GeneratorType generatorType;

    //Click
    private double nextTick = 0.0F;
    private float amp = 0.0F;
    private float phase = 0.0F;
    private double sampleRate = 0.0F;
    private int accent;
    private bool running = false;

    //Sinus
    public float frequency = 440.0f; // Frequency of the sine wave
    public float amplitude = 0.5f;   // Amplitude of the sine wave
    private float sinePhase = 0.0F;
    private float samplingFrequency = 48000.0f; // Default Unity audio sampling frequency

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
    private int filterReadPos = 0;
    private static ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    static SinusAudioSender m_Instance;
    public static SinusAudioSender Instance
    {
        get
        {
            if (m_Instance == null)
                m_Instance = FindObjectOfType<SinusAudioSender>();
            if (m_Instance == null)
                m_Instance = new GameObject("UniVoiceAudioSampler").AddComponent<SinusAudioSender>();
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
        //audioSource.PlayOneShot(audioSource.clip);
        running = true;
    }

    public void StartRecording(int frequency = 16000, int sampleDurationMS = 10)
    {
        Debug.Log("Start Recording Bus");
        StopRecording();

        Frequency = frequency;
        SampleDurationMS = sampleDurationMS;

        AudioClip = GetComponent<AudioSource>().clip;
        Debug.Log("Audioclip present = " + (AudioClip != null));

        Sample = new float[Frequency / 1000 * SampleDurationMS * 2];
        IsRecording = true;

    }

    private void Start()
    {
        //New
        accent = signatureHi;
        double startTick = AudioSettings.dspTime;
        sampleRate = AudioSettings.outputSampleRate;
        nextTick = startTick * sampleRate;
    }

    public void StopRecording()
    {
        IsRecording = false;

        Destroy(AudioClip);
        AudioClip = null;
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
        if (!running)
            return;
        if (generatorType == GeneratorType.sine)
        {
            float increment = frequency * 2.0f * Mathf.PI / samplingFrequency;
            for (int i = 0; i < data.Length; i += channels)
            {
                sinePhase += increment;
                if (sinePhase > 2.0f * Mathf.PI) sinePhase -= 2.0f * Mathf.PI;

                float sample = amplitude * Mathf.Sin(sinePhase);
                for (int j = 0; j < channels; j++)
                {
                    data[i + j] = sample;
                }
            }
        }
        if (generatorType == GeneratorType.beat)
        {
            double samplesPerTick = sampleRate * 60.0F / bpm * 4.0F / signatureLo;
            double sample = AudioSettings.dspTime * sampleRate;
            int dataLen = data.Length / channels;

            int n = 0;
            while (n < dataLen)
            {
                float x = gain * amp * Mathf.Sin(phase);
                int i = 0;
                while (i < channels)
                {
                    data[n * channels + i] += x;
                    i++;
                }
                while (sample + n >= nextTick)
                {
                    nextTick += samplesPerTick;
                    amp = 1.0F;
                    if (++accent > signatureHi)
                    {
                        accent = 1;
                        amp *= 2.0F;
                    }
                    Debug.Log("Tick: " + accent + "/" + signatureHi);
                }
                phase += amp * 0.3F;
                amp *= 0.993F;
                n++;
            }
        }
        EnqueueMainThreadAction(() =>
        {
            if (!IsRecording)
                return;

            int remainingSamples = data.Length;
            int bufferPosition = 0;

            while (remainingSamples > 0)
            {
                int spaceInSampleBuffer = Sample.Length - globalSampleReadPos;
                int samplesToCopy = Mathf.Min(remainingSamples, spaceInSampleBuffer);

                Array.Copy(data, bufferPosition, Sample, globalSampleReadPos, samplesToCopy);

                globalSampleReadPos += samplesToCopy;
                bufferPosition += samplesToCopy;
                remainingSamples -= samplesToCopy;

                if (globalSampleReadPos >= Sample.Length)
                {
                    m_SampleCount++;
                    OnSampleReady?.Invoke(m_SampleCount, Sample.ToArray());
                    globalSampleReadPos = 0;
                }
            }
        });
        //EnqueueMainThreadAction(() =>
        //{
        //    if (!IsRecording)
        //        return;
        //    filterReadPos = 0;

        //    while (filterReadPos < data.Length)
        //    {
        //        if (globalSampleReadPos >= Sample.Length)
        //        {
        //            m_SampleCount++;
        //            globalSampleReadPos = 0;
        //            Debug.Log("<color=red>OnSegmentReady</color>");
        //            OnSampleReady?.Invoke(m_SampleCount, Sample);
        //            //Array.Clear(Sample, 0, Sample.Length);
        //        }

        //        Sample[globalSampleReadPos] = data[filterReadPos];
        //        globalSampleReadPos++;
        //        filterReadPos++;
        //    }
        //});
        //EnqueueMainThreadAction(() =>
        //{
        //    if (!IsRecording)
        //        return;

        //    // Process the data and copy it into the Sample array
        //    int sampleOffset = 0;
        //    while (filterReadPos < data.Length)
        //    {
        //        int remainingSamples = data.Length - filterReadPos;
        //        int spaceInSampleBuffer = Sample.Length - globalSampleReadPos;

        //        int samplesToCopy = Mathf.Min(remainingSamples, spaceInSampleBuffer);

        //        Array.Copy(data, filterReadPos, Sample, globalSampleReadPos, samplesToCopy);

        //        globalSampleReadPos += samplesToCopy;
        //        filterReadPos += samplesToCopy;

        //        // If the sample buffer is full, send the segment and reset the buffer position
        //        if (globalSampleReadPos >= Sample.Length)
        //        {
        //            m_SampleCount++;
        //            OnSampleReady?.Invoke(m_SampleCount, Sample.ToArray());
        //            globalSampleReadPos = 0;
        //        }
        //    }

        //    // Reset filter read position for the next call
        //    filterReadPos = 0;
        //});
    }
}

public enum GeneratorType { sine, beat }
