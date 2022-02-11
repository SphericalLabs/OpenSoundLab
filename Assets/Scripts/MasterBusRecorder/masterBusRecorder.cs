using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class masterBusRecorder : MonoBehaviour
{
    [DllImport("SoundStageNative")]
    static extern IntPtr MasterBusRecorder_GetRecorderInstance();
    [DllImport("SoundStageNative")]
    static extern void MasterBusRecorder_StartRecording();
    [DllImport("SoundStageNative")]
    static extern void MasterBusRecorder_StopRecording();
    [DllImport("SoundStageNative")]
    static extern bool MasterBusRecorder_ReadRecordedSample(ref float sample);
    [DllImport("SoundStageNative")]
    static extern float MasterBusRecorder_GetLevel_Lin();
    [DllImport("SoundStageNative")]
    static extern float MasterBusRecorder_GetLevel_dB();

    public enum State
    {
        Idle,
        Recording,
        Finishing
    }

    State _state;

    public State state => _state;

    BinaryWriter bw;
    FileStream fs;
    string filename;
    int length;
    int bitDepth = 24;

    private void Awake()
    {
        //You can set the bitDepth here if you want:
        //bitDepth = 16;
        //bitDepth = 24;

        WritePseudoFile();
    }

    private void Update()
    {
        if(_state == State.Recording)
        {
            //You can use these for level visualization:
            float lin = MasterBusRecorder_GetLevel_Lin(); // [0...1]
            float db = MasterBusRecorder_GetLevel_dB(); //[-192...0]
        }
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

        //Reset state so a new recording session can be started:
        _state = State.Idle;
    }

    public IEnumerator QuerySamples(masterBusRecorder recInterface, Action onEnded)
    {
        ///Prepare data structures
        float sample = 0;
        int convertedSample;
        byte[] bytes = new byte[3];

        ///Repeatingly query native code for new samples
        while (recInterface.state != masterBusRecorder.State.Idle)
        {
            ///If we can get a new sample, we convert it to the desired number format and write it to the file
            if (MasterBusRecorder_ReadRecordedSample(ref sample) == true)
            {
                recInterface.length++;
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
                    onEnded(); // we can call this here already bc we will no longer access the native code
                }
                /// 2) We consumed all samples that are currently available, but  new samples may be available in the future bc we are still recording:
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