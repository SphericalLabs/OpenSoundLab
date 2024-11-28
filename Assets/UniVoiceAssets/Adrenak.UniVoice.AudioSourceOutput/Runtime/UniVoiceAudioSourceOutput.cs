using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Adrenak.UniVoice.AudioSourceOutput {
    /// <summary>
    /// This class feeds incoming segments of audio to an AudioBuffer 
    /// and plays the buffer's clip on an AudioSource. It also clears segments
    /// of the buffer based on the AudioSource's position.
    /// </summary>
    public class UniVoiceAudioSourceOutput : MonoBehaviour, IAudioOutput {
        const string TAG = "UniVoiceAudioSourceOutput";

        enum Status {
            Ahead,
            Current,
            Behind
        }

        private Dictionary<int, Status> segments = new Dictionary<int, Status>();

        /// <summary>
        /// Retrieves the count of segments that match the specified status.
        /// </summary>
        /// <param name="status">The status to count.</param>
        /// <returns>The number of segments with the specified status.</returns>
        private int GetSegmentCountByStatus(Status status)
        {
            int count = 0;
            // Iterate through the Values collection directly for better performance
            foreach (Status segmentStatus in segments.Values)
            {
                if (segmentStatus == status)
                {
                    count++;
                }
            }
            return count;
        }


        /// <summary>
        /// Returns the highest integer key in the segments dictionary.
        /// </summary>
        /// <returns>The highest integer key.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the segments dictionary is empty.</exception>
        public int GetHighestSegmentIndex()
        {
            if (segments == null || segments.Count == 0)
            {
                throw new InvalidOperationException("The segments dictionary is empty.");
            }

            // Initialize with the first key in the dictionary
            using (var enumerator = segments.Keys.GetEnumerator())
            {
                enumerator.MoveNext();
                int maxKey = enumerator.Current;

                // Iterate through the keys to find the maximum key
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current > maxKey)
                    {
                        maxKey = enumerator.Current;
                    }
                }

                return maxKey;
            }
        }

        /// <summary>
        /// Removes all segments with an index lower than the specified givenIndex to prevent memory leaks.
        /// </summary>
        /// <param name="givenIndex">The threshold index. All segments with an index lower than this will be removed.</param>
        private void RemoveSegmentsLowerThan(int givenIndex)
        {
            // Initialize with an estimated capacity to minimize reallocations
            List<int> keysToRemove = new List<int>(segments.Count / 2);

            // Iterate through the Keys collection directly
            foreach (int key in segments.Keys)
            {
                if (key < givenIndex)
                {
                    keysToRemove.Add(key);
                }
            }

            // Batch removal to minimize dictionary resize operations
            foreach (int key in keysToRemove)
            {
                segments.Remove(key);
                // Debug.unityLogger.Log(TAG, $"Removed segment with index: {key}");
            }
        }

        public AudioSource AudioSource { get; private set; }
        public int MinSegCount { get; private set; }
        public int MaxSegCount { get; private set; }

        CircularAudioClip circularAudioClip;

        public string ID {
            get => circularAudioClip.AudioClip.name;
            set {
                gameObject.name = "UniVoice Peer #" + value;
                circularAudioClip.AudioClip.name = "UniVoice Peer #" + value;
            }
        }

        [System.Obsolete("Cannot use new keyword to create an instance. Use .New() method instead")]
        public UniVoiceAudioSourceOutput() { }

        /// <summary>
        /// Creates a new instance using the dependencies.
        /// </summary>
        /// 
        /// <param name="buffer">
        /// The AudioBuffer that the streamer operates on.
        /// </param>
        /// 
        /// <param name="source">
        /// The AudioSource from where the incoming audio is played.
        /// </param>
        /// 
        /// <param name="minSegCount">
        /// The minimum number of audio segments <see cref="circularAudioClip"/> 
        /// must have for the streamer to play the audio. This value is capped
        /// between 1 and <see cref="CircularAudioClip.SegCount"/> of the 
        /// <see cref="circularAudioClip"/> passed.
        /// Default: 0. Results in the value being set to the max possible.
        /// </param>
        public static UniVoiceAudioSourceOutput New
        (CircularAudioClip buffer, AudioSource source, int minSegCount = 0, int maxSegCount = 20) {
            var ctd = source.gameObject.AddComponent<UniVoiceAudioSourceOutput>();
            //DontDestroyOnLoad(ctd.gameObject);

            source.loop = true;
            source.clip = buffer.AudioClip;
            source.spatialize = true;

            
            ctd.MinSegCount = Mathf.Clamp(minSegCount, 0, buffer.SegCount);
            ctd.MaxSegCount = Mathf.Clamp(maxSegCount, 2, buffer.SegCount);

            ctd.circularAudioClip = buffer;
            ctd.AudioSource = source;

            Debug.unityLogger.Log(TAG, $"Created with the following params:" +
            $"buffer SegCount: {buffer.SegCount}" +
            $"buffer SegDataLen: {buffer.SegLenghtInSamples}" +
            $"buffer MinSegCount: {ctd.MinSegCount}" +
            $"buffer AudioClip channels: {buffer.AudioClip.channels}" +
            $"buffer AudioClip frequency: {buffer.AudioClip.frequency}" +
            $"buffer AudioClip samples: {buffer.AudioClip.samples}");

            return ctd;
        }

        int lastIndex = -1;
        int loops = 0;
        int readyCount = 0;
        /// <summary>
        /// This is to make sure that if a segment is missed, its previous 
        /// contents won't be played again when the clip loops back.
        /// </summary>
        private void Update()
        {
            if (AudioSource.clip == null) return;

            // index loops from 0 to SegCount - 1
            var index = (int)(AudioSource.GetCurrentNormPosition() * circularAudioClip.SegCount);

            if (lastIndex > index)
            {
                loops++;
                // remove all segments with index lower than the given index
                RemoveSegmentsLowerThan(loops * circularAudioClip.SegCount + index); // this will render the behind mechanism useless                
            }


            // Check every frame to see if the AudioSource has 
            // just moved to a new segment in the AudioBuffer 
            // Note: This is probably missing segments every now and then, when the clip played more segments than one or two
            if (lastIndex != index)
            {
                // If so, clear the audio buffer so that in case the
                // AudioSource loops around, the old contents are not played.
                // Note: This only deletes the last segment, but if segment is very short there will be more segments between frames
                // Note: This does not remove the segment from the Dictionary
                circularAudioClip.Clear(lastIndex);

                // Note: This ensures that indices are marked as used and not fed again
                segments.EnsureKey(lastIndex, Status.Behind);
                segments.EnsureKey(index, Status.Current);

                lastIndex = index;
            }

            // Since the index that is calculated here is only between 0 and circularAudioClip.SegCount - 1, it actually has another addressing than the feeding mechanism. The waiting mechanism below is therefore only active at the very beginning of the patch, to give it a heads

            // Check if the number of ready segments is sufficient for us to 
            // play the audio. Whereas if the number is 0, we must stop audio
            // and wait for the minimum ready segment count to be met again.
            readyCount = GetSegmentCountByStatus(Status.Ahead);
            if (readyCount < MinSegCount)
            { // wait to fill segment buffer
                Debug.Log("UniVoice buffer not full enough");
                AudioSource.mute = true;
                if (AudioSource.isPlaying)
                    AudioSource.Pause();
            }
            else if (readyCount >= MinSegCount)
            {
                AudioSource.mute = false;
                if (!AudioSource.isPlaying)
                    AudioSource.Play();

                //if (readyCount >= MaxSegCount)
                //{ // catchup

                //    Debug.Log("UniVoice buffer too full");
                //    // skip audio to catch up with playback
                //    //int currentAbsIndex = loops * circularAudioClip.SegCount + index;

                //    // jump between min and maxsegcount as a solid restarting point
                //    int indicesToSkip = readyCount - ((MaxSegCount - MinSegCount) / 2);

                //    // wrapped position
                //    int newIndex = (index + indicesToSkip) % circularAudioClip.SegCount;

                //    // set the time in samples
                //    AudioSource.timeSamples = newIndex * circularAudioClip.SegLenghtInSamples;

                //    // full loops
                //    int loopsToAdd = (index + indicesToSkip) / circularAudioClip.SegCount;
                //    loops += loopsToAdd;

                //    int absoluteNewIndex = loops * circularAudioClip.SegCount + newIndex;
                //    RemoveSegmentsLowerThan(absoluteNewIndex);
                //}
            }
        }

        /// <summary>
        /// Feeds incoming audio into the audio buffer.
        /// </summary>
        /// 
        /// <param name="index">
        /// The absolute index of the segment, as reported by the peer to know 
        /// the normalized position of the segment on the buffer
        /// </param>
        /// 
        /// <param name="audioSamples">The audio samples being fed</param>
        public void Feed(int index, int frequency, int channelCount, float[] audioSamples)
        {
            // If we already have this index, don't bother
            // It's been passed already without playing.
            if (segments.ContainsKey(index)) return;

            int locIdx = (int)(AudioSource.GetCurrentNormPosition() * circularAudioClip.SegCount);
            locIdx = Mathf.Clamp(locIdx, 0, circularAudioClip.SegCount - 1);

            var bufferIndex = circularAudioClip.GetNormalizedIndex(index);

            // Don't write to the same segment index that we are reading
            if (locIdx == bufferIndex) return;

            // Finally write into the buffer 
            segments.Add(index, Status.Ahead);
            circularAudioClip.Write(index, audioSamples);
        }

        /// <summary>
        /// Feeds an incoming <see cref="ChatroomAudioSegment"/> into the audio buffer.
        /// </summary>
        /// <param name="segment"></param>
        public void Feed(ChatroomAudioSegment segment) =>
        Feed(segment.segmentIndex, segment.frequency, segment.channelCount, segment.samples);

        /// <summary>
        /// Disposes the instance by deleting the GameObject of the component.
        /// </summary>
        public void Dispose() {
            Destroy(gameObject);
        }

        /// <summary>
        /// Creates <see cref="UniVoiceAudioSourceOutput"/> instances
        /// </summary>
        public class Factory : IAudioOutputFactory {
            public int BufferSegCount { get; private set; }
            public int MinSegCount { get; private set; }
            public int MaxSegCount { get; private set; }

            public Factory() : this(10, 5, 10) { }

            public Factory(int bufferSegCount, int minSegCount, int maxSegCount) {
                BufferSegCount = bufferSegCount;
                MinSegCount = minSegCount;
                MaxSegCount = maxSegCount;
            }

            public IAudioOutput Create(int samplingRate, int channelCount, int segmentLength) {
                return New(
                    new CircularAudioClip(
                        samplingRate, channelCount, segmentLength, BufferSegCount
                    ),
                    new GameObject($"UniVoiceAudioSourceOutput").AddComponent<AudioSource>(),
                    MinSegCount, MaxSegCount
                );
            }
        }
    }
}
