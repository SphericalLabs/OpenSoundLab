using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace Adrenak.UniMic {
    //[ExecuteAlways]

    

    public class Mic : MonoBehaviour {


        [DllImport("OSLNative")]
        public static extern void MultiplyArrayBySingleValue(float[] a, int length, float val);
        [DllImport("OSLNative")]
        public static extern void SetArrayToSingleValue(float[] a, int length, float val);


        // ================================================
        #region MEMBERS
        // ================================================
        /// <summary>
        /// Whether the microphone is running
        /// </summary>
        public bool IsRecording { get; private set; }

        /// <summary>
        /// The frequency at which the mic is operating
        /// </summary>
        public int Frequency { get; private set; }

        /// <summary>
        /// Last populated audio sample
        /// </summary>
        public float[] Segment { get; private set; }

        /// <summary>
        /// Sample duration/length in milliseconds
        /// </summary>
        public int SegmentDurationMS { get; private set; }

        /// <summary>
        /// The length of the sample float array
        /// </summary>
        public int SampleLength {
            get { return Frequency * SegmentDurationMS / 1000; }
        }

        /// <summary>
        /// The AudioClip currently being streamed in the Mic
        /// </summary>
        public AudioClip AudioClip { get; private set; }

        /// <summary>
        /// List of all the available Mic devices
        /// </summary>
        public List<string> Devices => Microphone.devices.ToList();

        /// <summary>
        /// Index of the current Mic device in m_Devices
        /// </summary>
        public int CurrentDeviceIndex { get; private set; } = -1;

        /// <summary>
        /// Gets the name of the Mic device currently in use
        /// </summary>
        public string CurrentDeviceName {
            get {
                if (CurrentDeviceIndex < 0 || CurrentDeviceIndex >= Microphone.devices.Length)
                    return string.Empty;
                return Devices[CurrentDeviceIndex];
            }
        }

        int m_SampleCount = 0;
        #endregion

        // ================================================
        #region EVENTS
        // ================================================
        /// <summary>
        /// Invoked when the instance starts Recording.
        /// </summary>
        public event Action OnStartRecording;

        /// <summary>
        /// Invoked everytime an audio frame is collected. Includes the frame count.
        /// NOTE: There isn't much use for the index of a sample. Refer to 
        /// <see cref="OnTimestampedSampleReady"/> for an event that gives you the
        /// unix timestamp with a millisecond precision.
        /// </summary>
        public event Action<int, float[]> OnSampleReady;

        /// <summary>
        /// Invoked everytime an audio sample is collected. Includes the unix timestamp
        /// from when the sample was captured with a millisecond precision.
        /// </summary>
        public event Action<long, float[]> OnTimestampedSampleReady;

        /// <summary>
        /// Invoked when the instance stop Recording.
        /// </summary>
        public event Action OnStopRecording;
        #endregion

        // ================================================
        #region METHODS
        // ================================================

        static Mic m_Instance;
        public static Mic Instance {
            get {
                if(m_Instance == null)
                    m_Instance = FindObjectOfType<Mic>();
                if (m_Instance == null) 
                    m_Instance = new GameObject("UniMic.Mic").AddComponent<Mic>();
                return m_Instance;
            }
        }

        // Prevent 'new' keyword construction
        [Obsolete("Mic is a MonoBehaviour class. Use Mic.Instance to get the instance", true)]
        public Mic() { }

        /// <summary>
        /// Ensures an instance of the Mic class
        /// </summary>
        public static Mic Instantiate() {
            return Instance;
        }

        float gaindB = 0f;
        float gainMult = 1f;

        void Awake() {
            //if(Application.isPlaying)
            //    DontDestroyOnLoad(gameObject);
            if (Devices.Count > 0)
                CurrentDeviceIndex = 0;

            gaindB = 12;

            if (Unity.XR.Oculus.Utils.GetSystemHeadsetType() == Unity.XR.Oculus.SystemHeadset.Oculus_Quest_2 ||
            Unity.XR.Oculus.Utils.GetSystemHeadsetType() == Unity.XR.Oculus.SystemHeadset.Oculus_Link_Quest_2)
            {
                gaindB = 14;
            }

            if (Unity.XR.Oculus.Utils.GetSystemHeadsetType() == Unity.XR.Oculus.SystemHeadset.Meta_Quest_3 ||
            Unity.XR.Oculus.Utils.GetSystemHeadsetType() == Unity.XR.Oculus.SystemHeadset.Meta_Link_Quest_3)
            {
                gaindB = 12;
            }

            gainMult = Mathf.Pow(10.0f, gaindB / 20.0f);
        }

        /// <summary>
        /// Sets a Mic device for Recording
        /// </summary>
        /// <param name="index">The index of the Mic device. Refer to <see cref="Devices"/> for available devices</param>
        public void SetDeviceIndex(int index) {
            Microphone.End(CurrentDeviceName);
            CurrentDeviceIndex = index;
            if (IsRecording)
                StartRecording(Frequency, SegmentDurationMS);
        }

        /// <summary>
        /// Resumes recording at the frequency and sample duration that was 
        /// previously being used.
        /// </summary>
        public void ResumeRecording() {
            StartRecording(Frequency, SegmentDurationMS);
        }

        /// <summary>
        /// Starts to stream the input of the current Mic device
        /// </summary>
        public void StartRecording(int frequency = 16000, int segmentLengthInMilliSec = 10) {
            StopRecording();
            IsRecording = true;

            Frequency = frequency;
            SegmentDurationMS = segmentLengthInMilliSec;

            AudioClip = Microphone.Start(CurrentDeviceName, true, 1, Frequency);

            Segment = new float[Frequency / 1000 * SegmentDurationMS * AudioClip.channels];

            Debug.Log("UniVoice audio channels: " + AudioClip.channels);

            readRawAudioCoroutine = StartCoroutine(ReadRawAudio());

            OnStartRecording?.Invoke();
        }


        private Coroutine readRawAudioCoroutine;

        /// <summary>
        /// Ends the Mic stream.
        /// </summary>
        public void StopRecording() {
            //if (!Microphone.IsRecording(CurrentDeviceName)) return;

            IsRecording = false;

            Microphone.End(CurrentDeviceName);
            //Destroy(AudioClip);
            AudioClip = null;

            if (readRawAudioCoroutine != null)
            {
                StopCoroutine(readRawAudioCoroutine);
                readRawAudioCoroutine = null;
            }

            OnStopRecording?.Invoke();
        }


        IEnumerator ReadRawAudio()
        {
            int loops = 0;
            int readAbsPos = 0;
            int prevPos = 0;

            // Cache the device name to avoid repeated expensive calls
            string currDevName = CurrentDeviceName;

            // Define buffer sizes
            int bufferSize = Segment.Length; // Original buffer size (e.g., 160 samples)
            int requiredBufferSize = 2 * bufferSize; // 2x buffer size (e.g., 320 samples)

            // State flag to control processing
            bool canProcess = false;

            while (IsRecording && AudioClip != null && Microphone.IsRecording(currDevName))
            {
                // Get the current microphone position
                int currPos = Microphone.GetPosition(currDevName);

                // Handle looping of the AudioClip buffer
                if (currPos < prevPos)
                    loops++;
                prevPos = currPos;

                // Calculate absolute positions to handle looping
                long currAbsPos = (long)loops * AudioClip.samples + currPos;
                long availableSamples = currAbsPos - readAbsPos;

                // Check if we have enough samples to start processing
                if (!canProcess && availableSamples >= requiredBufferSize)
                {
                    canProcess = true; // Ready to process
                }

                // If in processing state
                if (canProcess)
                {
                    // Recalculate available samples after potential state change
                    availableSamples = currAbsPos - readAbsPos;

                    // Process as long as we have enough samples
                    while (availableSamples >= bufferSize)
                    {
                        // If buffer is getting too empty, pause processing
                        if (availableSamples < requiredBufferSize)
                        {
                            canProcess = false;
                            break; // Exit the processing loop to wait for buffer refill
                        }

                        // Ensure we don't read beyond the available samples
                        int samplesToRead = (int)Mathf.Min(bufferSize, availableSamples);

                        // Allocate array to hold the samples
                        float[] availableSamplesArray = new float[samplesToRead];

                        // Get the samples from the AudioClip
                        bool dataObtained = AudioClip.GetData(availableSamplesArray, (int)(readAbsPos % AudioClip.samples));

                        if (!dataObtained)
                        {
                            Debug.LogWarning("Failed to get audio data from AudioClip.");
                            break;
                        }

                        // Apply gain adjustment using the native method
                        MultiplyArrayBySingleValue(availableSamplesArray, availableSamplesArray.Length, gainMult);

                        // Copy the processed samples into the Segment
                        Array.Copy(availableSamplesArray, 0, Segment, 0, samplesToRead);

                        // Invoke the events with the processed segment
                        m_SampleCount++;
                        OnSampleReady?.Invoke(m_SampleCount, Segment);

                        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        OnTimestampedSampleReady?.Invoke(timestamp, Segment);

                        // Update the read position
                        readAbsPos += samplesToRead;

                        // Update available samples
                        availableSamples -= samplesToRead;
                    }
                }

                // Yield execution until the next frame
                yield return null;
            }
        }


        #endregion

        void OnDestroy()
        {
            StopRecording();
        }

        [Obsolete("UpdateDevices method is no longer needed. Devices property is now always up to date")]
        public void UpdateDevices() { }

        /// <summary>
        /// Changes to a Mic device for Recording
        /// </summary>
        /// <param name="index">The index of the Mic device. Refer to <see cref="Devices"/></param>
        [Obsolete("ChangeDevice may go away in the future. Use SetDeviceIndex instead", false)]
        public void ChangeDevice(int index) {
            SetDeviceIndex(index);
        }
    }
}