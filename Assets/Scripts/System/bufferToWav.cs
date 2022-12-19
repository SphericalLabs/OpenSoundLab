// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 
// 
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

public class bufferToWav : MonoBehaviour {
    public static bufferToWav instance; // singleton
    public bool applyNormalization = false;

    [DllImport("SoundStageNative")]
    public static extern void NormalizeClip(float[] clip, int length);

    void Awake()
    {
        instance = this;
    }

    public bool savingInProgress = false;
    public Coroutine Save(string filename, float[] clip, int channels, int length, TextMesh txt, signalGenerator sig, bool normalize)
    {
        savingInProgress = true;
        applyNormalization = normalize;

        if (!filename.ToLower().EndsWith(".wav")) filename += ".wav";
        return StartCoroutine(SaveRoutine(filename, clip, length, txt, sig));
    }

    public void WavHeader(BinaryWriter b, int length, int _samplelength = 2)
    {
        int _samplerate = AudioSettings.outputSampleRate;
        int _channels = 2;
       
        b.Write(Encoding.ASCII.GetBytes("RIFF")); // chunkid
        b.Write(36 + length * _samplelength); // chunksize = (length * _samplelength) + 36???
        b.Write(Encoding.ASCII.GetBytes("WAVE")); // format
        b.Write(Encoding.ASCII.GetBytes("fmt "));  // subchunk1 ID (fmt)
        b.Write(16); // subchunk1 size -- constant size
        b.Write((short)1);  // udioformat
        b.Write((short)_channels); //   // channels
        b.Write(_samplerate);  // samplerate
        b.Write(_samplerate * _samplelength * _channels);    //byterate
        b.Write((short)(_samplelength * _channels)); // block align
        b.Write((short)(8 * _samplelength)); // bits per sample
        b.Write(Encoding.ASCII.GetBytes("data"));  // subchunk2 ID (data)    
        b.Write(length * _samplelength); // subchunk 2 size
    }

    int sample;

    IEnumerator SaveRoutine(string filename, float[] clip, int length, TextMesh txt, signalGenerator sig)
    {
        txt.gameObject.SetActive(true);
        txt.text = "Saving...";
        
        FileStream _filestream = new FileStream(filename, FileMode.Create);
        BinaryWriter _binarystream = new BinaryWriter(_filestream);
        WavHeader(_binarystream, length);

        if(applyNormalization) 
          NormalizeClip(clip, clip.Length);

        int counter = 0;
        for (int i = 0; i < length; i++)
        {
            // mind non-linearity around 0: https://www.cs.cmu.edu/~rbd/papers/cmj-float-to-int.html
            // this also fixed a weird bug in the previous int16 conversion, where converting 0f would always crash
            sample = (((int)(Mathf.Clamp(clip[i], -1f, 1f) * 32760f + 32768.5)) - 32768); // TODO: move clipping to native preprocessing function

            // sample = (((int)(Mathf.Clamp(clip[i], -1f, 1f) * 8388600 + 8388608.5)) - 8388608);
            // bc 2^23 == 8388608
            _binarystream.Write((short)sample); // would it be faster if full more data is written at once? but mind frame budget!

            counter++;

            if (counter > 10000) 
            {
                counter = 0;
                txt.text = "Saving... " + (int)(100 * (float)i / length) + "% Complete";
                yield return null;
            }
        }

        _binarystream.Close();
        _filestream.Close();
        txt.text = "Saved";
        
        sampleManager.instance.AddRecording(filename);
        txt.text = "Saved";
        savingInProgress = false;
        txt.gameObject.SetActive(false);
        
        // flush after saving, good for quick flow and otherwise we would have to update display. buffer was altered if normalization was on. more clean to simply flush away the old data.
        if (sig is waveTranscribeRecorder)
        {
          waveTranscribeRecorder rec = (waveTranscribeRecorder)sig;
          rec.Flush();
        }

        sig.updateTape(filename);

        yield return new WaitForSeconds(1.5f);
    }

    public void UpdateWavHeader(string fileName, uint length, int _samplelength = 2)
    {
        using (var stream = File.Open(fileName, FileMode.Open))
        {
            // Note: The explicit (uint) cast is necessary here, otherwise
            // an automatic cast to long (=signed 64 bit integer) will be executed!
            byte[] bytes = BitConverter.GetBytes((uint)(36 + length * _samplelength));
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            stream.Position = 4;
            stream.Write(bytes, 0, bytes.Length);
            bytes = BitConverter.GetBytes((uint)(length * _samplelength));
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            stream.Position = 40;
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
