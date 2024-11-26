using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Adrenak.UniMic {
    //[ExecuteAlways]
    public class Mic : MonoBehaviour {
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
        public float[] Sample { get; private set; }

        /// <summary>
        /// Sample duration/length in milliseconds
        /// </summary>
        public int SampleDurationMS { get; private set; }

        /// <summary>
        /// The length of the sample float array
        /// </summary>
        public int SampleLength {
            get { return Frequency * SampleDurationMS / 1000; }
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

        void Awake() {
            //if(Application.isPlaying)
            //    DontDestroyOnLoad(gameObject);
            if (Devices.Count > 0)
                CurrentDeviceIndex = 0;
        }

        /// <summary>
        /// Sets a Mic device for Recording
        /// </summary>
        /// <param name="index">The index of the Mic device. Refer to <see cref="Devices"/> for available devices</param>
        public void SetDeviceIndex(int index) {
            Microphone.End(CurrentDeviceName);
            CurrentDeviceIndex = index;
            if (IsRecording)
                StartRecording(Frequency, SampleDurationMS);
        }

        /// <summary>
        /// Resumes recording at the frequency and sample duration that was 
        /// previously being used.
        /// </summary>
        public void ResumeRecording() {
            StartRecording(Frequency, SampleDurationMS);
        }

        /// <summary>
        /// Starts to stream the input of the current Mic device
        /// </summary>
        public void StartRecording(int frequency = 16000, int sampleDurationMS = 10) {
            StopRecording();
            IsRecording = true;

            Frequency = frequency;
            SampleDurationMS = sampleDurationMS;

            AudioClip = Microphone.Start(CurrentDeviceName, true, 10, Frequency);
            Sample = new float[Frequency / 1000 * SampleDurationMS * AudioClip.channels];
            temp = new float[Sample.Length]; // Allocate temp here
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

        private float[] temp;
        IEnumerator ReadRawAudio() {
            long loops = 0;
            long readAbsPos = 0;
            int prevPos = 0;            

            // set the gain for the mic input in dB here
            // todo: use the OSL_Native Compressor / Limiter here
            // but probably Meta Quest already does some dynamic gain regulation and other filtering on their mic
            // still that some handling of different output levels varying between platforms would be helpful, or allow for 
            float gaindB = 18;
            float gainMult = Mathf.Pow(10.0f, gaindB / 20.0f);

            while (AudioClip != null && Microphone.IsRecording(CurrentDeviceName)) {
                bool isNewDataAvailable = true;

                while (isNewDataAvailable) {
                    int currPos = Microphone.GetPosition(CurrentDeviceName);
                    if (currPos < prevPos)
                        loops++;
                    prevPos = currPos;

                    var currAbsPos = loops * AudioClip.samples + currPos;
                    var nextReadAbsPos = readAbsPos + temp.Length;

                    if (nextReadAbsPos < currAbsPos) {
                        try
                        {
                            AudioClip.GetData(temp, (int)(readAbsPos % AudioClip.samples));
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Failed to get audio data: {ex.Message}");
                            yield break;
                        }

                        for (int i = 0; i < temp.Length; i++)
                        {
                            temp[i] *= gainMult;
                        }
                                                
                        Array.Copy(temp, Sample, temp.Length);

                        m_SampleCount++;
                        OnSampleReady?.Invoke(m_SampleCount, Sample);

                        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        OnTimestampedSampleReady?.Invoke(timestamp, Sample);

                        readAbsPos = nextReadAbsPos;
                        isNewDataAvailable = true;
                    }
                    else
                        isNewDataAvailable = false;
                }
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