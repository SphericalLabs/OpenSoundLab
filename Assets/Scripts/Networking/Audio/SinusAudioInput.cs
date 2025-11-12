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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Adrenak.UniVoice;
using System;

public class SinusAudioInput : IAudioInput
{
    public event Action<int, float[]> OnSegmentReady;

    public int Frequency => SinusAudioSender.Instance.Frequency;

    public int ChannelCount =>
        SinusAudioSender.Instance.AudioClip == null ? 0 : SinusAudioSender.Instance.AudioClip.channels;

    public int SegmentRate => 1000 / SinusAudioSender.Instance.SampleDurationMS;

    public SinusAudioInput(int deviceIndex = 0, int frequency = 16000, int sampleLen = 100)
    {
        SinusAudioSender.Instance.StartRecording(frequency, sampleLen);
        Debug.unityLogger.Log("UniVoiceBusAudioInput start recording");
        SinusAudioSender.Instance.OnSampleReady += Bus_OnSampleReady;
    }

    void Bus_OnSampleReady(int segmentIndex, float[] samples)
    {
        OnSegmentReady?.Invoke(segmentIndex, samples);
    }

    public void Dispose()
    {
        SinusAudioSender.Instance.OnSampleReady -= Bus_OnSampleReady;
    }
}
