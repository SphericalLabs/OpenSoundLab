using System;
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
        public int readyCount = 0;
        public bool fillingUp = false;

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

                // Determine buffer state
                readyCount = segments.Count;

                if (fillingUp && readyCount >= MinSegCount + (MaxSegCount - MinSegCount) / 2)
                {
                    fillingUp = false; // enough segments gathered
                    return;
                }

                if (readyCount < MinSegCount)
                {
                    // Insufficient buffer, log and wait until enough segments are ready
                    EnqueueMainThreadAction(() =>
                    {
                        Debug.unityLogger.Log(TAG, "Insufficient segments available: " + readyCount);
                    });

                    fillingUp = true;
                    return;
                }

                // Compute total available samples in playbackQueue
                int totalAvailableSamples = 0;

                if (playbackQueue.Count > 0)
                {
                    float[] currentSegment = playbackQueue.Peek();
                    totalAvailableSamples += (currentSegment.Length - playbackSampleIndex) * UpsampleFactor;
                }

                foreach (var segment in playbackQueue.Skip(1))
                {
                    totalAvailableSamples += segment.Length * UpsampleFactor;
                }

                // Fill playbackQueue with enough segments to fill data
                while (totalAvailableSamples < samplesPerChannel && !segments.IsEmpty)
                {
                    // Find the smallest available segment index
                    int minKey = segments.Keys.Min();

                    if (segments.TryRemove(minKey, out float[] segment))
                    {
                        playbackQueue.Enqueue(segment);
                        totalAvailableSamples += segment.Length * UpsampleFactor;

                        // Update nextSegmentIndex
                        nextSegmentIndex = Math.Max(nextSegmentIndex, minKey + 1);
                    }
                    else
                    {
                        // If removal failed, possibly due to race conditions, break the loop
                        break;
                    }
                }

                // Initialize output sample index
                int outputSampleIndex = 0;

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

                    // Get current sample
                    float currentSample = currentSegment[playbackSampleIndex];

                    // Get next sample
                    float nextSample = currentSample; // Default to current sample

                    if (playbackSampleIndex + 1 < currentSegment.Length)
                    {
                        nextSample = currentSegment[playbackSampleIndex + 1];
                    }
                    else
                    {
                        // At the end of current segment, try to get the first sample of the next segment
                        if (playbackQueue.Count > 1)
                        {
                            float[] nextSegment = playbackQueue.ElementAt(1);
                            if (nextSegment.Length > 0)
                            {
                                nextSample = nextSegment[0];
                            }
                        }
                    }

                    // Use the UpsampleSegmentLinear function
                    UpsampleSegmentLinear(currentSample, nextSample, UpsampleFactor, data, ref outputSampleIndex, samplesPerChannel, channels);

                    playbackSampleIndex++;
                }
            }
        }

        private void UpsampleSegmentLinear(float currentSample, float nextSample, int upsampleFactor, float[] outputData, ref int outputSampleIndex, int samplesPerChannel, int channels)
        {
            for (int up = 0; up < upsampleFactor && outputSampleIndex < samplesPerChannel; up++)
            {
                float t = (float)up / upsampleFactor;
                float interpolatedSample = Mathf.Lerp(currentSample, nextSample, t);

                for (int ch = 0; ch < channels; ch++)
                {
                    outputData[outputSampleIndex * channels + ch] += interpolatedSample;
                }
                outputSampleIndex++;
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

            }

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
            public int MinSegCount { get; private set; }
            public int MaxSegCount { get; private set; }

            public Factory() : this(5, 15) { }

            public Factory(int minSegCount, int maxSegCount)
            {

                MinSegCount = minSegCount;
                MaxSegCount = maxSegCount;
            }

            public IAudioOutput Create(int samplingRate, int channelCount, int segmentLengthInSamples)
            {
                var go = new GameObject($"UniVoiceAudioSourceOutput");
                var output = go.AddComponent<UniVoiceAudioSourceOutput>();
                output.Initialize(samplingRate, channelCount, segmentLengthInSamples, MinSegCount, MaxSegCount);
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
