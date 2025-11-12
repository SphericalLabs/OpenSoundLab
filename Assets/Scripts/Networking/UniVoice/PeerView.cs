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

﻿using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Adrenak.UniVoice.Samples
{
    public static class ToggleExtension
    {
        public static void SetIsOnWithoutNotify(this Toggle instance, bool value)
        {
            var originalEvent = instance.onValueChanged;
            instance.onValueChanged = new Toggle.ToggleEvent();
            instance.isOn = value;
            instance.onValueChanged = originalEvent;
        }
    }

    public class PeerView : MonoBehaviour
    {
        public event Action<bool> OnIncomingModified;
        public event Action<bool> OnOutgoingModified;

        [SerializeField] TMP_Text idText;
        [SerializeField] Transform barContainer;
        [SerializeField] Transform barTemplate;
        [SerializeField] Toggle speakerToggle;
        [SerializeField] Toggle micToggle;

        public bool IncomingAudio
        {
            get => speakerToggle.isOn;
            set => speakerToggle.SetIsOnWithoutNotify(value);
        }

        public bool OutgoingAudio
        {
            get => micToggle.isOn;
            set => micToggle.SetIsOnWithoutNotify(value);
        }

        List<Transform> bars = new List<Transform>();

        void Start()
        {
            speakerToggle.onValueChanged.AddListener(value =>
                OnIncomingModified?.Invoke(value));

            micToggle.onValueChanged.AddListener(value =>
                OnOutgoingModified?.Invoke(value));
        }

        public void SetPeerID(short id)
        {
            idText.text = id.ToString();
        }

        public void DisplaySpectrum(float[] spectrum)
        {
            InitBars(spectrum.Length);

            if (spectrum.Length != bars.Count) return;

            for (int i = 0; i < bars.Count; i++)
                bars[i].localScale = new Vector3(1, Mathf.Clamp01(spectrum[i]), 1);
        }

        void InitBars(int count)
        {
            if (bars.Count == count) return;

            foreach (var bar in bars)
                Destroy(bar.gameObject);
            bars.Clear();

            for (int i = 0; i < count; i++)
            {
                var instance = Instantiate(barTemplate, barContainer);
                instance.gameObject.SetActive(true);
                bars.Add(instance);
            }
        }
    }
}
