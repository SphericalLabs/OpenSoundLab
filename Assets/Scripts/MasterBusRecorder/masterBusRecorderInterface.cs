// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2026 OSLLv1 Sphericals OpenSoundLab
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

using System;
using System.IO;
using System.Text;
using UnityEngine;

public class masterBusRecorderInterface : componentInterface
{
    public button recordButton;
    public Transform meterLeft;
    public Transform meterRight;
    public TextMesh infoText;
    public TextMesh meterLeftText;
    public TextMesh meterRightText;
    public masterBusRecorder recorder;
    public float meterMinDb = -60f;
    public float meterMaxDb = 0f;

    Renderer infoRenderer;
    Renderer meterLeftRenderer;
    Renderer meterRightRenderer;
    Material meterLeftMaterial;
    Material meterRightMaterial;
    Vector3 leftScale;
    Vector3 rightScale;
    int textFrameToggle = 0;

    void Awake()
    {
        if (recordButton != null)
        {
            recordButton._componentInterface = this;
        }

        if (infoText != null)
        {
            infoRenderer = infoText.GetComponent<Renderer>();
        }

        if (meterLeft != null)
        {
            leftScale = meterLeft.localScale;
            meterLeftRenderer = meterLeft.GetComponentInChildren<Renderer>();
        }

        if (meterRight != null)
        {
            rightScale = meterRight.localScale;
            meterRightRenderer = meterRight.GetComponentInChildren<Renderer>();
        }

        setupMeterMaterials();

        if (recorder == null && masterControl.instance != null)
        {
            recorder = masterControl.instance.recorder;
        }
    }

    void Update()
    {
        if (recorder == null && masterControl.instance != null)
        {
            recorder = masterControl.instance.recorder;
        }

        updateButtonState();
        updateMeters();

        if (!shouldUpdateText())
        {
            return;
        }

        if (infoText != null)
        {
            infoText.text = buildStatusTextMinimal();
        }

        updateMeterText();
    }

    public override void hit(bool on, int ID = -1)
    {
        if (recorder == null)
        {
            return;
        }
        recorder.ToggleRec(on);
    }

    void updateButtonState()
    {
        if (recordButton == null || recorder == null)
        {
            return;
        }

        bool isRecording = recorder.state != masterBusRecorder.State.Idle;
        if (recordButton.isHit != isRecording)
        {
            recordButton.phantomHit(isRecording);
        }
    }

    void updateMeters()
    {
        if (recorder == null)
        {
            return;
        }

        if (meterLeft != null)
        {
            float scale = getMeterScale(recorder.levelDbLeft);
            meterLeft.localScale = new Vector3(leftScale.x, scale, leftScale.z);
            setMeterColor(meterLeftMaterial, getMeterColor(scale));
        }

        if (meterRight != null)
        {
            float scale = getMeterScale(recorder.levelDbRight);
            meterRight.localScale = new Vector3(rightScale.x, scale, rightScale.z);
            setMeterColor(meterRightMaterial, getMeterColor(scale));
        }
    }

    bool shouldUpdateText()
    {
        if (infoText == null || infoRenderer == null)
        {
            return false;
        }

        if (!infoRenderer.isVisible)
        {
            textFrameToggle = 0;
            return false;
        }

        bool shouldUpdate = textFrameToggle == 0;
        textFrameToggle = 1 - textFrameToggle;
        return shouldUpdate;
    }

    string buildStatusTextDetailled()
    {
        StringBuilder builder = new StringBuilder(256);
        builder.AppendLine("MASTER BUS RECORDER");
        builder.Append("STATE: ").AppendLine(getStateLabel());
        builder.AppendLine();
        builder.Append("WAV LIMIT: ").AppendLine(formatDuration(getRemainingWavSeconds()));
        builder.Append("STORAGE LIMIT: ").AppendLine(formatDuration(getRemainingStorageSeconds()));
        builder.Append("STORAGE FREE: ").AppendLine(formatBytes(getAvailableStorageBytes()));
        builder.AppendLine();
        builder.Append("FILE: ").AppendLine(getFilenameLabel());
        builder.Append("PROGRESS: ").AppendLine(formatDuration(getRecordedSeconds()));
        builder.Append("DROPPED: ").Append(formatSamples(getDroppedSamples())).AppendLine(" samples");
        return builder.ToString();
    }

    string buildStatusTextMinimal()
    {
        if (recorder == null || recorder.state == masterBusRecorder.State.Idle)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(96);
        builder.AppendLine(formatDurationMinimal(getRecordedSeconds()));

        int dropped = getDroppedSamples();
        builder.Append("Dropped ").Append(formatSamples(dropped)).AppendLine(" samples");

        int percentFull = getStoragePercentFull();
        builder.Append("Storage ").Append(percentFull).AppendLine("% full");

        return builder.ToString();
    }

    string getStateLabel()
    {
        if (recorder == null)
        {
            return "Idle";
        }

        if (recorder.state == masterBusRecorder.State.Recording)
        {
            return "Recording";
        }

        if (recorder.state == masterBusRecorder.State.Finishing)
        {
            return "Finishing";
        }

        return "Idle";
    }

    float getRecordedSeconds()
    {
        return recorder != null ? recorder.recordedSeconds : 0f;
    }

    float getRemainingWavSeconds()
    {
        return recorder != null ? recorder.remainingSeconds : 0f;
    }

    float getRemainingStorageSeconds()
    {
        return recorder != null ? recorder.remainingSecondsFromStorage : 0f;
    }

    long getAvailableStorageBytes()
    {
        return recorder != null ? recorder.availableStorageBytes : 0;
    }

    long getTotalStorageBytes()
    {
        return recorder != null ? recorder.totalStorageBytes : 0;
    }

    int getStoragePercentFull()
    {
        long totalBytes = getTotalStorageBytes();
        if (totalBytes <= 0)
        {
            return 0;
        }

        long availableBytes = getAvailableStorageBytes();
        float percentFull = 1f - Mathf.Clamp01(availableBytes / (float)totalBytes);
        return Mathf.RoundToInt(percentFull * 100f);
    }

    int getDroppedSamples()
    {
        return recorder != null ? recorder.droppedSamples : 0;
    }

    float getLevelLeftDb()
    {
        return recorder != null ? recorder.levelDbLeft : float.NegativeInfinity;
    }

    float getLevelRightDb()
    {
        return recorder != null ? recorder.levelDbRight : float.NegativeInfinity;
    }

    string getFilenameLabel()
    {
        if (recorder == null || string.IsNullOrEmpty(recorder.filename))
        {
            return "-";
        }

        return Path.GetFileName(recorder.filename);
    }

    string formatDb(float db)
    {
        if (float.IsNegativeInfinity(db) || float.IsNaN(db))
        {
            return "-inf";
        }
        return db.ToString("0.0");
    }

    float getMeterScale(float db)
    {
        if (float.IsNegativeInfinity(db) || float.IsNaN(db))
        {
            return 0f;
        }

        float mapped = Utils.map(db, meterMinDb, meterMaxDb, 0f, 1f);
        return Mathf.Clamp01(mapped);
    }

    void updateMeterText()
    {
        string leftValue = formatDb(getLevelLeftDb());
        string rightValue = formatDb(getLevelRightDb());

        if (meterLeftText != null)
        {
            meterLeftText.text = leftValue;
        }

        if (meterRightText != null)
        {
            meterRightText.text = rightValue;
        }
    }

    void setupMeterMaterials()
    {
        Shader shader = Shader.Find("Meta/Depth/URP/Occlusion Unlit");
        if (shader == null)
        {
            return;
        }

        if (meterLeftRenderer != null)
        {
            meterLeftMaterial = new Material(shader);
            meterLeftRenderer.material = meterLeftMaterial;
        }

        if (meterRightRenderer != null)
        {
            meterRightMaterial = new Material(shader);
            meterRightRenderer.material = meterRightMaterial;
        }
    }

    void setMeterColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        else if (material.HasProperty("_TintColor"))
        {
            material.SetColor("_TintColor", color);
        }
    }

    Color getMeterColor(float level)
    {
        if (level <= 0.25f)
        {
            return Color.Lerp(Color.blue, Color.green, level / 0.25f);
        }

        if (level <= 0.75f)
        {
            return Color.Lerp(Color.green, Color.yellow, (level - 0.25f) / 0.5f);
        }

        if (level <= 0.95f)
        {
            return Color.Lerp(Color.yellow, Color.red, (level - 0.75f) / 0.2f);
        }

        return Color.red;
    }

    string formatDuration(float seconds)
    {
        if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds))
        {
            return "00m 00s";
        }

        long totalSeconds = (long)seconds;
        long days = totalSeconds / 86400;
        totalSeconds -= days * 86400;
        long hours = totalSeconds / 3600;
        totalSeconds -= hours * 3600;
        long minutes = totalSeconds / 60;
        long secs = totalSeconds - minutes * 60;

        if (days > 0)
        {
            return string.Format("{0}d {1:00}h {2:00}m {3:00}s", days, hours, minutes, secs);
        }

        if (hours > 0)
        {
            return string.Format("{0}h {1:00}m {2:00}s", hours, minutes, secs);
        }

        return string.Format("{0:00}m {1:00}s", minutes, secs);
    }

    string formatDurationMinimal(float seconds)
    {
        if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds))
        {
            return "0s";
        }

        long totalSeconds = (long)seconds;
        if (totalSeconds < 60)
        {
            return string.Format("{0}s", totalSeconds);
        }

        long hours = totalSeconds / 3600;
        totalSeconds -= hours * 3600;
        long minutes = totalSeconds / 60;
        long secs = totalSeconds - minutes * 60;

        if (hours > 0)
        {
            return string.Format("{0}h {1}m {2}s", hours, minutes, secs);
        }

        return string.Format("{0}m {1}s", minutes, secs);
    }

    string formatSamples(int samples)
    {
        return samples.ToString("N0");
    }

    string formatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "0.0 GB";
        }

        float gb = bytes / (1024f * 1024f * 1024f);
        return gb.ToString("0.0") + " GB";
    }
}
