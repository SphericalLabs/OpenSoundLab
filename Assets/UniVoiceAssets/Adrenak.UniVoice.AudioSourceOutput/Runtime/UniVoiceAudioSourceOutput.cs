﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq; // Added for LINQ operations
using UnityEngine;
using UnityEngine.Audio;

namespace Adrenak.UniVoice.AudioSourceOutput
{
    /// <summary>
    /// This class feeds incoming mono audio segments directly to a stereo AudioSource via OnAudioFilterRead.
    /// It manages playback synchronization, handles missing segments, and implements smooth crossfades during catchup.
    /// </summary>
    
    public class UniVoiceAudioSourceOutput : MonoBehaviour, IAudioOutput
    {
        private const string TAG = "UniVoiceAudioSourceOutput";

        /// <summary>
        /// Thread-safe collection to store incoming audio segments.
        /// Key: Absolute segment index.
        /// Value: Mono audio samples.
        /// </summary>
        private ConcurrentDictionary<int, float[]> segments = new ConcurrentDictionary<int, float[]>();

        /// <summary>
        /// Queue to manage the order of segments for playback.
        /// </summary>
        private Queue<float[]> playbackQueue = new Queue<float[]>();

        /// <summary>
        /// Current segment playback tracking.
        /// </summary>
        private int playbackSampleIndex = 0;

        /// <summary>
        /// Next expected segment index.
        /// </summary>
        private int nextSegmentIndex = 0;

        /// <summary>
        /// Audio settings.
        /// </summary>
        private int frequency;
        private int channelCount;
        private int segmentLengthInSamples;

        /// <summary>
        /// Buffer thresholds.
        /// </summary>
        public int MinSegCount { get; private set; }
        public int MaxSegCount { get; private set; }

        /// <summary>
        /// Synchronization lock for thread safety.
        /// </summary>
        private object playLock = new object();

        /// <summary>
        /// Crossfade parameters.
        /// </summary>
        private const int crossfadeSampleCount = 512; // Number of samples over which to perform crossfade
        private bool isCrossfading = false;
        private int crossfadeSamplesLeft = 0;

        /// <summary>
        /// AudioSource component.
        /// </summary>
        public AudioSource audioSource;

        /// <summary>
        /// Identifier for the audio source.
        /// </summary>
        public string ID
        {
            get => audioSource.name;
            set
            {
                gameObject.name = "UniVoice Peer #" + value;
                audioSource.name = "UniVoice Peer #" + value;
            }
        }

        /// <summary>
        /// Thread-safe queue for actions to be executed on the main thread.
        /// </summary>
        private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

        private string outputMixer = "MIXER";
        private string outputMixerGroup = "univoice";

        /// <summary>
        /// Initializes the UniVoiceAudioSourceOutput.
        /// </summary>
        /// <param name="frequency">Sampling rate of the audio.</param>
        /// <param name="channelCount">Number of audio channels (fixed to 2 for stereo).</param>
        /// <param name="segmentLengthInSamples">Number of samples per mono segment.</param>
        /// <param name="minSegCount">Minimum buffer segments required to start playback.</param>
        /// <param name="maxSegCount">Maximum buffer segments before initiating catchup.</param>
        public void Initialize(int frequency, int channelCount, int segmentLengthInSamples, int minSegCount = 0, int maxSegCount = 20)
        {
            this.frequency = frequency;
            this.channelCount = channelCount;
            this.segmentLengthInSamples = segmentLengthInSamples;

            // Ensure channelCount is 2 (stereo)
            if (this.channelCount != 2)
            {
                Debug.unityLogger.LogWarning(TAG, $"Channel count set to {channelCount}. This implementation expects stereo AudioSource. Proceeding with stereo.");
                this.channelCount = 2;
            }

            MinSegCount = Mathf.Clamp(minSegCount, 0, maxSegCount);
            MaxSegCount = Mathf.Clamp(maxSegCount, MinSegCount + 1, maxSegCount);

            // Ensure AudioSource is present
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            AudioMixer audioMixer = Resources.Load<AudioMixer>(outputMixer);
            AudioMixerGroup[] mixerGroups = audioMixer.FindMatchingGroups(string.Empty);

            foreach (AudioMixerGroup group in mixerGroups)
            {
                if (group.name == outputMixerGroup)
                {
                    audioSource.outputAudioMixerGroup = group;
                    break;
                }
            }

            // Configure AudioSource settings
            audioSource.playOnAwake = false;
            audioSource.loop = false;            
            audioSource.spatialize = true; // Please note that the AudioSource must be after this script in order for the spatialization to work
            audioSource.spatialBlend = 1f;
            audioSource.mute = false;

            Debug.unityLogger.Log(TAG, $"Initialized with frequency: {frequency}, channels: {this.channelCount}, segmentLength: {segmentLengthInSamples}, MinSegCount: {MinSegCount}, MaxSegCount: {MaxSegCount}");
        }

        /// <summary>
        /// Feeds incoming mono audio into the playback system.
        /// </summary>
        /// <param name="index">Absolute index of the segment.</param>
        /// <param name="frequency">Sampling rate of the audio.</param>
        /// <param name="channelCount">Number of audio channels (expected to be 1 for mono).</param>
        /// <param name="audioSamples">Mono audio samples being fed.</param>
        public void Feed(int index, int frequency, int channelCount, float[] audioSamples)
        {
            if (audioSamples.Length != segmentLengthInSamples)
            {
                Debug.unityLogger.LogWarning(TAG, $"Incorrect segment length: {audioSamples.Length}. Expected: {segmentLengthInSamples}");
                return;
            }

            // Ensure audio settings match
            if (this.frequency != frequency)
            {
                Debug.unityLogger.LogError(TAG, $"Frequency mismatch. Expected: {this.frequency}, Received: {frequency}");
                return;
            }

            // Expecting mono segments
            if (channelCount != 1)
            {
                Debug.unityLogger.LogWarning(TAG, $"Channel count for segments is {channelCount}. Expected: 1 (mono). Proceeding by treating as mono.");
            }

            // Avoid overwriting existing segments
            if (segments.TryAdd(index, audioSamples))
            {
                // Segment added successfully
            }
            else
            {
                Debug.unityLogger.LogWarning(TAG, $"Segment {index} already exists. Ignoring duplicate.");
            }
        }

        /// <summary>
        /// Feeds an incoming ChatroomAudioSegment into the audio buffer.
        /// </summary>
        /// <param name="segment">The audio segment to feed.</param>
        public void Feed(ChatroomAudioSegment segment) =>
            Feed(segment.segmentIndex, segment.frequency, segment.channelCount, segment.samples);

        // 16khz to 48khz
        public int UpsampleFactor = 3; 

        /// <summary>
        /// OnAudioFilterRead is called on a separate audio thread to supply audio data.
        /// This implementation expects stereo output and mono input segments.
        /// Mono segments are duplicated to both left and right channels.
        /// </summary>
        /// <param name="data">The buffer to fill with audio data.</param>
        /// <param name="channels">Number of audio channels (expected to be 2 for stereo).</param>
        private void OnAudioFilterRead(float[] data, int channels)
        {
            lock (playLock)
            {
                int samplesPerChannel = data.Length / channels;
                Array.Clear(data, 0, data.Length);

                // Fill playbackQueue with available segments up to MaxSegCount
                while (playbackQueue.Count < MaxSegCount && !segments.IsEmpty)
                {
                    // Find the smallest available segment index
                    int minKey = segments.Keys.Min();

                    if (segments.TryRemove(minKey, out float[] segment))
                    {
                        playbackQueue.Enqueue(segment);

                        // Update nextSegmentIndex to the next expected index after the smallest one
                        if (minKey >= nextSegmentIndex)
                        {
                            nextSegmentIndex = minKey + 1;
                        }
                        else
                        {
                            // Handle cases where minKey is less than nextSegmentIndex
                            nextSegmentIndex = Math.Max(nextSegmentIndex, minKey + 1);
                        }
                    }
                    else
                    {
                        // If removal failed, possibly due to race conditions, break the loop
                        break;
                    }
                }

                // Determine buffer state
                int readyCount = segments.Count;

                if (readyCount < MinSegCount)
                {
                    //// Insufficient buffer, enqueue mute and pause actions if not already muted
                    //EnqueueMainThreadAction(() =>
                    //{
                    //    if (!audioSource.mute)
                    //    {
                    //        audioSource.mute = true;
                    //        Debug.unityLogger.Log(TAG, "Muted due to insufficient buffer.");
                    //    }
                    //});
                }
                else if (readyCount >= MaxSegCount)
                {
                    // Buffer overflow, initiate catchup
                    int segmentsToSkip = readyCount - ((MaxSegCount - MinSegCount) / 2);
                    SkipSegments(segmentsToSkip);
                }
                else
                {
                    //// Sufficient buffer, enqueue unmute actions if muted
                    //EnqueueMainThreadAction(() =>
                    //{
                    //    if (audioSource.mute)
                    //    {
                    //        audioSource.mute = false;
                    //        Debug.unityLogger.Log(TAG, "Unmuted playback.");
                    //    }
                    //});
                }

                // Initialize a separate output sample index
                int outputSampleIndex = 0;

                // Fill the data buffer with audio samples with on-the-fly upsampling
                while (outputSampleIndex < samplesPerChannel && playbackQueue.Count > 0)
                {
                    float[] currentSegment = playbackQueue.Peek();

                    if (playbackSampleIndex >= currentSegment.Length)
                    {
                        // Current segment exhausted, dequeue it
                        playbackQueue.Dequeue();
                        playbackSampleIndex = 0;
                        continue;
                    }

                    float sample = currentSegment[playbackSampleIndex];

                    // Insert each sample three times for upsampling
                    for (int up = 0; up < UpsampleFactor && outputSampleIndex < samplesPerChannel; up++)
                    {
                        for (int ch = 0; ch < channels; ch++)
                        {
                            data[outputSampleIndex * channels + ch] += sample;
                        }
                        outputSampleIndex++;
                    }

                    playbackSampleIndex++;
                }

                // Insert silence for any remaining samples
                while (outputSampleIndex < samplesPerChannel)
                {
                    for (int ch = 0; ch < channels; ch++)
                    {
                        data[outputSampleIndex * channels + ch] += 0f;
                    }
                    outputSampleIndex++;
                }

                // Apply crossfade if active
                if (isCrossfading)
                {
                    ApplyCrossfade(data, channels);
                }
            }
        }


        /// <summary>
        /// Applies a smooth crossfade to the current audio buffer to prevent artifacts.
        /// </summary>
        /// <param name="data">Audio data buffer.</param>
        /// <param name="channels">Number of audio channels.</param>
        private void ApplyCrossfade(float[] data, int channels)
        {
            if (crossfadeSamplesLeft > 0)
            {
                float fadeFactor = 1f - ((float)crossfadeSamplesLeft / crossfadeSampleCount);
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] *= fadeFactor;
                }
                crossfadeSamplesLeft--;
                if (crossfadeSamplesLeft <= 0)
                {
                    isCrossfading = false;
                }
            }
        }

        /// <summary>
        /// Skips a specified number of segments to catch up with the playback.
        /// Implements smooth crossfading to minimize audio artifacts.
        /// </summary>
        /// <param name="segmentsToSkip">Number of segments to skip.</param>
        private void SkipSegments(int segmentsToSkip)
        {
            lock (playLock)
            {
                for (int i = 0; i < segmentsToSkip; i++)
                {
                    if (playbackQueue.Count > 0)
                    {
                        playbackQueue.Dequeue();
                        nextSegmentIndex++;
                    }
                }

                // Initiate crossfade
                isCrossfading = true;
                crossfadeSamplesLeft = crossfadeSampleCount;
            }

            //// Enqueue a crossfade initiation log on the main thread
            //EnqueueMainThreadAction(() =>
            //{
            //    Debug.unityLogger.Log(TAG, $"Initiated crossfade for skipping {segmentsToSkip} segments.");
            //});
        }

        /// <summary>
        /// Disposes the instance by deleting the GameObject of the component.
        /// </summary>
        public void Dispose()
        {
            // Ensure disposal is done on the main thread
            EnqueueMainThreadAction(() => Destroy(gameObject));
        }

        /// <summary>
        /// Factory class for creating UniVoiceAudioSourceOutput instances.
        /// </summary>
        public class Factory : IAudioOutputFactory
        {
            public int BufferSegCount { get; private set; }
            public int MinSegCount { get; private set; }
            public int MaxSegCount { get; private set; }

            public Factory() : this(20, 5, 15) { }

            public Factory(int bufferSegCount, int minSegCount, int maxSegCount)
            {
                BufferSegCount = bufferSegCount;
                MinSegCount = minSegCount;
                MaxSegCount = maxSegCount;
            }

            public IAudioOutput Create(int samplingRate, int channelCount, int segmentLength)
            {
                var go = new GameObject($"UniVoiceAudioSourceOutput");
                var output = go.AddComponent<UniVoiceAudioSourceOutput>();
                output.Initialize(samplingRate, channelCount, segmentLength, MinSegCount, MaxSegCount);
                return output;
            }
        }

        /// <summary>
        /// Enqueues an action to be executed on the main thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        private void EnqueueMainThreadAction(Action action)
        {
            mainThreadActions.Enqueue(action);
        }

        /// <summary>
        /// Executes all enqueued actions on the main thread.
        /// </summary>
        private void Update()
        {
            while (mainThreadActions.TryDequeue(out Action action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.unityLogger.LogError(TAG, $"Error executing main thread action: {ex}");
                }
            }
        }
    }
}
