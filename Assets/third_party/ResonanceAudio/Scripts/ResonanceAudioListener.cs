// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

#if UNITY_EDITOR
using System.IO;
#endif

/// Resonance Audio listener component that enhances AudioListener to provide advanced spatial audio
/// features.
///
/// There should be only one instance of this which is attached to the AudioListener's game object.
[AddComponentMenu("ResonanceAudio/ResonanceAudioListener")]
[RequireComponent(typeof(AudioListener))]
[ExecuteInEditMode]
public class ResonanceAudioListener : MonoBehaviour {
  /// Global gain in decibels to be applied to the processed output.
  [Tooltip("Sets the global gain for all spatialized audio sources. Can be used to adjust the " +
           "overall output volume.")]
  public float globalGainDb = 0.0f;

  /// Global layer mask to be used in occlusion detection.
  [Tooltip("Sets the global layer mask for occlusion detection.")]
  public LayerMask occlusionMask = -1;

  /// Stereo speaker mode toggle.
  [Tooltip("Disables HRTF-based rendering and force stereo-panning only rendering for all " +
           "spatialized audio sources. This mode is recommended only when the audio output is " +
           "routed to a stereo loudspeaker configuration.")]
  public bool stereoSpeakerModeEnabled = false;

  /// Denotes whether the soundfield should be recorded in a seamless loop.
  [Tooltip("Sets whether the recorded soundfield clip should be saved as a seamless loop.")]
  public bool recorderSeamless = false;

  /// Target tag for spatial audio sources to be recorded into soundfield.
  [Tooltip("Specify by tag which spatialized audio sources will be recorded. Choose " +
           "\"Untagged\" to include all enabled spatialized audio sources in the scene.")]
  public string recorderSourceTag = "Untagged";

  /// Ambisonics order for soundfield recording, determines number of recording channels.
  [Tooltip("Specify the Ambisonics order for soundfield recording (0-3). " +
           "This will set the number of recorded channels.")]
  public int recorderAmbisonicsOrder = 1;

  /// Maximum recording time.
  [Tooltip("Set the maximum recording time. Recording will take place to pre-allocated " +
           "memory and will be written to disk after. " +
           "Set to 0 if you do not intend to record in order prevent allocation.")]
  public float recorderMaxTime = 0.0f;

  /// Flag to automatically start recording when entering Play Mode.
  [Tooltip("Sets whether recording should start automatically when " +
           "entering Play Mode.")]
  public bool recorderAutoStart = false;

  /// Is currently recording soundfield?
  public bool IsRecording { get; private set; }

  /// Has recorded yet unsaved soundfield data?
  public bool HasRecordedData { get; private set; }

#pragma warning disable 0414  // private variable assigned but is never used.
  // Denotes whether the soundfield recorder foldout should be expanded.
  [SerializeField]
  private bool recorderFoldout = false;
#pragma warning restore 0414

  // List of target spatial audio sources to be recorded.
  private List<AudioSource> recorderTaggedSources = null;

  // Record start time in seconds.
  private double recorderStartTime = 0.0;

  void OnEnable() {
    if (Application.isEditor) {
      IsRecording = false;
      HasRecordedData = false;
      recorderStartTime = 0.0;

      if (!Application.isPlaying)
        recorderTaggedSources = new List<AudioSource>();
    }
  }

  void OnDisable() {
#if UNITY_EDITOR
    if (Application.isEditor && IsRecording) {
      // Stop soundfield recorder.
      StopSoundfieldRecorder();
      Debug.LogWarning("Soundfield recording is stopped.");
    }
    // Trigger saving to file when returning from Play Mode.
    if (Application.isPlaying && HasRecordedData) {
      WriteSoundfieldRecordingToFile();
    }
#endif
  }

  void Start() {
#if UNITY_EDITOR
      // Init soundfield recorder when Play Mode starts.
      if (Application.isEditor && Application.isPlaying && recorderMaxTime > 1.0f) {
      InitSoundFieldRecorder();

      if (recorderAutoStart)
        StartSoundfieldRecorder();
    }
#endif
  }

  void Update() {
    if (Application.isEditor && !Application.isPlaying && !IsRecording) {
#if UNITY_EDITOR
        // Update soundfield recorder properties.
        UpdateTaggedSources();
#endif
    } else {
      // Update global properties.
      ResonanceAudio.UpdateAudioListener(this);
    }
  }

  /// Returns the current record duration in seconds.
  public double GetCurrentRecordDuration() {
    if (IsRecording) {
      double currentTime = AudioSettings.dspTime;
      return currentTime - recorderStartTime;
    }
    return 0.0;
  }
#if UNITY_EDITOR
    /// Starts soundfield recording.
    public void StartSoundfieldRecorder() {
    if (!Application.isEditor) {
      Debug.LogError("Soundfield recording is only supported in Unity Editor.");
      return;
    }

    if (IsRecording) {
      Debug.LogWarning("Soundfield recording is already in progress.");
      return;
    }

    // Assume recorder has been initalised by Start() in play mode.
    if (!Application.isPlaying && !InitSoundFieldRecorder())
        return;

    // correct by potentially previous recording time without saving
    recorderStartTime = AudioSettings.dspTime -
                          (HasRecordedData ? recorderStartTime : 0.0);

    // Sources need to be played explicitly only in edit mode.
    if (!Application.isPlaying) {
      for (int i = 0; i < recorderTaggedSources.Count; ++i) {
        if (recorderTaggedSources[i].playOnAwake) {
          recorderTaggedSources[i].PlayScheduled(recorderStartTime);
        }
      }
    }

    IsRecording = ResonanceAudio.StartRecording();
    if (!IsRecording) {
      Debug.LogError("Failed to start soundfield recording.");
      IsRecording = false;

      if (!Application.isPlaying)
        StopTaggedSources();
    }
  }


  /// Stops soundfield recording
  public void StopSoundfieldRecorder()
  {
    if (!Application.isEditor) {
      Debug.LogError("Soundfield recording is only supported in Unity Editor.");
      return;
    }

    if (!IsRecording) {
      Debug.LogWarning("No recording taking place.");
      return;
    }

    recorderStartTime = GetCurrentRecordDuration(); // store recorded time
    IsRecording = false;

    if (!ResonanceAudio.StopRecording())
      Debug.LogError("Failed to stop soundfield recording.");

    HasRecordedData = true;

    if (!Application.isPlaying) {
      StopTaggedSources();
      WriteSoundfieldRecordingToFile();
    }
  }

  

    /// Stops soundfield recording and saves the recorded data into target file path.
    private void WriteSoundfieldRecordingToFile() {
    if (!Application.isEditor) {
      Debug.LogError("Soundfield recording is only supported in Unity Editor.");
      return;
    }

    if (!HasRecordedData) {
      Debug.LogWarning("No recorded soundfield was found.");
      return;
    }

    // Save recorded soundfield clips into a temporary folder.
    string tempFolderPath = FileUtil.GetUniqueTempPathInProject();
    if (!Directory.Exists(tempFolderPath))
    {
      Directory.CreateDirectory(tempFolderPath);
    }
    string tempFileName = name + string.Format("_{0:yyyy-MM-dd_hh-mm-ss-tt}.ogg", System.DateTime.Now);
    string tempFilePath = Path.Combine(tempFolderPath, tempFileName);
    if (!ResonanceAudio.WriteRecording(tempFilePath, recorderSeamless)) {
      Debug.LogError("Writing recording to temporary location failed: " + tempFilePath);
      return;
    }

    // Copy the recorded file as an ambisonic audio clip into project assets.
    string relativeClipPath = EditorUtility.SaveFilePanelInProject("Save Soundfield Recording", tempFileName,
                                                                   "ogg", null);
    if (relativeClipPath.Length > 0 && File.Exists(tempFilePath)) {
      string projectFolderPath =
          Application.dataPath.Substring(0, Application.dataPath.IndexOf("Assets"));
      string targetFilePath = Path.Combine(projectFolderPath, relativeClipPath);
      FileUtil.ReplaceFile(tempFilePath, targetFilePath);
      AssetDatabase.Refresh();

      AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(relativeClipPath);
      importer.ambisonic = true;
      AssetDatabase.Refresh();
    }

    // Cleanup temporary files.
    if (Directory.Exists(tempFolderPath)) {
      Directory.Delete(tempFolderPath, true);
    }

    HasRecordedData = false;
    recorderStartTime = 0.0;
  }


  // Initialize the sound field recorder with current parameters.
  private bool InitSoundFieldRecorder() {
    bool result = ResonanceAudio.InitRecording(recorderAmbisonicsOrder, recorderMaxTime);

    if (!result)
      Debug.LogError("Failed to initialize soundfield recorder.");

    return result;
  }


    // Updates the list of the target spatial audio sources to be recorded.
    private void UpdateTaggedSources() {
    recorderTaggedSources.Clear();
    var sources = GameObject.FindObjectsOfType<AudioSource>();
    for (int i = 0; i < sources.Length; ++i) {
      // Untagged is treated as *all* spatial audio sources in the scene.
      if ((recorderSourceTag == "Untagged" || sources[i].tag == recorderSourceTag) &&
          sources[i].enabled && sources[i].spatialize) {
        recorderTaggedSources.Add(sources[i]);
      }
    }
  }

  // Stops all sources in the list of tagged record sources.
  private void StopTaggedSources() {
    for (int i = 0; i < recorderTaggedSources.Count; ++i) {
      recorderTaggedSources[i].Stop();
    }
  }
#endif
}
