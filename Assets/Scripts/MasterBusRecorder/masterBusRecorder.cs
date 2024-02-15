using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class masterBusRecorder : MonoBehaviour
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
        Recording,
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
    BinaryWriter bw;
    FileStream fs;
    string filename;
    uint length;
    int instanceId;
    uint maxFileSize;

    //static
    static int instances = 0;

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

    private void OnDestroy()
    {
        ///Tell native plugin to stop recording bc its lifecycle is not tied to this instance's:
        if (_state == State.Recording)
        {
            StopRec();
        }
        ///In case there is an unfinished recording session, we stop the coroutines and force the session to be finished:
        if(_state != State.Idle)
        {
            StopAllCoroutines();
            OnRecordingFinished();
        }
    }

    public void ToggleRec(bool newState)
    {
        if (newState == false && _state == State.Recording)
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
        else if (newState == false && state != State.Recording)
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

    public void StartRec()
    {
        Debug.Log("MasterBusRecorder: Recording started...");

        //Create filename and filestream, create directory if does not exist:
        string dir = masterControl.instance.SaveDir + System.IO.Path.DirectorySeparatorChar + "Samples" + System.IO.Path.DirectorySeparatorChar +
"Sessions";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }        
        filename = dir + System.IO.Path.DirectorySeparatorChar +
string.Format("{0:yyyy-MM-dd_HH-mm-ss}.wav",
DateTime.Now);
        Debug.Log(filename);
        fs = new FileStream(filename, FileMode.Create);
        bw = new BinaryWriter(fs);
        bufferToWav.instance.WavHeader(bw, 0, bitDepth / 8); ///We will update the size fields at the end of the process
        length = 0;

        //Update the state before starting coroutine, otherwise it will terminate immediately:
        _state = State.Recording;

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

        ///Close the I/O stream
        bw.Close();
        fs.Close();

        ///Finally, update the WAV header so it has the correct size
        bufferToWav.instance.UpdateWavHeader(filename, length, bitDepth / 8);

        // show it in Tapes->Sessions
        sampleManager.instance.AddSession(filename);
        
        //Reset state so a new recording session can be started:
        _state = State.Idle;
    }

    public IEnumerator QuerySamples(masterBusRecorder recInterface, Action onEnded)
    {
        ///Prepare data structures
        float sample = 0;
        int convertedSample;
        byte[] bytes = new byte[3];

        ///Repeatedly query native code for new samples
        while (recInterface.state != masterBusRecorder.State.Idle)
        {
            ///If we reached the file size limit, stop the recording:
            if (recInterface.length == maxFileSize)
            {
                //Since we reached the file size limit, we do not read the rest of the samples in the rec buffer.
                //Instead, we immediately clear the rec buffer, finalize the WAV file and set the IDLE state:
                MasterBusRecorder_StopRecording();
                MasterBusRecorder_Clear();
                onEnded();
                //After finishing the recording, if the menu is enabled, we send a phantom hit to the recButton so it changes its toggle state.
                //Note that the recButton will in turn trigger a ToggleRec(false) in the recorder instance,
                //which will be ignored bc the recorder is already in the "Idle" state after onEnded().
                //* Why don't we just call phantomHit, which will in turn trigger the call to onEnded()?
                //=> Because we need to guarantee here that the coroutine stops IMMEDIATELY.
                var metronome = FindObjectOfType<metronome>();
                if(metronome != null)
                {
                    metronome.recButton.phantomHit(false);
                }    
            }
            ///If we can get a new sample, we convert it to the desired number format and write it to the file
            else if (MasterBusRecorder_ReadRecordedSample(ref sample) == true)
            {
                recInterface.length++;
                //Some debugging, this block can be removed anytime:
                /*if(recInterface.length % AudioSettings.outputSampleRate == 0)
                {
                    IntPtr buf = IntPtr.Zero;
                    int offset = 0;
                    int n = MasterBusRecorder_GetBufferPointer(buf, ref offset);
                    Debug.Log("instance " + instanceId + ": " + n + " samples in queue.");
                }*/
                if(recInterface.bitDepth == 16)
                {
                    convertedSample = (((int)(Mathf.Clamp(sample, -1f, 1f) * 32760f + 32768.5)) - 32768);
                    recInterface.bw.Write((short)convertedSample);
                }
                else if (recInterface.bitDepth == 24)
                {
                    //Adapted for 24bit: boundaries are [-2^23...2^23-1]
                    convertedSample = (((int)(Mathf.Clamp(sample, -1f, 1f) * 8388600 + 8388608.5)) - 8388608);
                    //Have to spread across 3 bytes bc there is no 24bit data type
                    bytes[0] = (byte)(convertedSample);
                    bytes[1] = (byte)(convertedSample >> 8);
                    bytes[2] = (byte)(convertedSample >> 16);
                    recInterface.bw.Write(bytes);
                }
            }
            ///There are 2 cases where no new sample is available:
            else
            {
                ///1) We consumed all samples because the recording has been stopped:
                if (recInterface.state == masterBusRecorder.State.Finishing)
                {
                    onEnded();
                }
                /// 2) We consumed all samples that are currently available, but new samples may be available in the future bc we are still recording:
                else
                    yield return null;
            }
        }
        yield return null;
    }

    public void WritePseudoFile()
    {
        filename = masterControl.instance.SaveDir + System.IO.Path.DirectorySeparatorChar + "Samples" + System.IO.Path.DirectorySeparatorChar +
"Recordings" + System.IO.Path.DirectorySeparatorChar +
"DUMMY.txt";
        fs = new FileStream(filename, FileMode.Create);
        bw = new BinaryWriter(fs);
        bufferToWav.instance.WavHeader(bw, 0, 2);
        bw.Close();
        fs.Close();
        File.Delete(filename);
        Debug.Log(filename);
    }
}