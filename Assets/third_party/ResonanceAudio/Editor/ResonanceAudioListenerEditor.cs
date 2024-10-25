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
using UnityEditor;
using System.Collections;
using System.IO;

/// A custom editor for properties on the ResonanceAudioListener script. This appears in the
/// Inspector window of a ResonanceAudioListener object.
[CustomEditor(typeof(ResonanceAudioListener))]
public class ResonanceAudioListenerEditor : Editor {
  private SerializedProperty globalGainDb = null;
  private SerializedProperty occlusionMask = null;
  private SerializedProperty stereoSpeakerModeEnabled = null;
  private SerializedProperty recorderFoldout = null;
  private SerializedProperty recorderSeamless = null;
  private SerializedProperty recorderSourceTag = null;
  private SerializedProperty recorderAmbisonicsOrder = null;
  private SerializedProperty recorderMaxTime = null;
  private SerializedProperty recorderAutoStart = null;

  private GUIContent globalGainDbLabel = new GUIContent("Global Gain (dB)");
  private GUIContent stereoSpeakerModeEnabledLabel = new GUIContent("Enable Stereo Speaker Mode");
  private GUIContent recorderLabel = new GUIContent("Soundfield Recorder",
     "Soundfield recorder allows pre-baking spatial audio sources into first-order ambisonic " +
     "soundfield assets to be played back at run time.");
  private GUIContent recorderSeamlessLabel = new GUIContent("Seamless Loop");
  private GUIContent recorderSourceTagLabel = new GUIContent("Source Tag");
  private GUIContent recorderAmbisonicsOrderLabel = new GUIContent("Ambisonics Order");
  private GUIContent recorderMaxTimeLabel = new GUIContent("Max Recording Time");
  private GUIContent recorderAutoStartLabel = new GUIContent("Auto-Record in Play");

  // Target listener instance.
  private ResonanceAudioListener listener = null;

  void OnEnable() {
    globalGainDb = serializedObject.FindProperty("globalGainDb");
    occlusionMask = serializedObject.FindProperty("occlusionMask");
    stereoSpeakerModeEnabled = serializedObject.FindProperty("stereoSpeakerModeEnabled");
    recorderFoldout = serializedObject.FindProperty("recorderFoldout");
    recorderSeamless = serializedObject.FindProperty("recorderSeamless");
    recorderSourceTag = serializedObject.FindProperty("recorderSourceTag");
    recorderAmbisonicsOrder = serializedObject.FindProperty("recorderAmbisonicsOrder");
    recorderMaxTime = serializedObject.FindProperty("recorderMaxTime");
    recorderAutoStart = serializedObject.FindProperty("recorderAutoStart");
    listener = (ResonanceAudioListener) target;
  }

  /// @cond
  public override void OnInspectorGUI() {
    serializedObject.Update();

    // Add clickable script field, as would have been provided by DrawDefaultInspector()
    MonoScript script = MonoScript.FromMonoBehaviour(target as MonoBehaviour);
    EditorGUI.BeginDisabledGroup(true);
    EditorGUILayout.ObjectField ("Script", script, typeof(MonoScript), false);
    EditorGUI.EndDisabledGroup();

    EditorGUILayout.Separator();
    EditorGUILayout.Slider(globalGainDb, ResonanceAudio.minGainDb, ResonanceAudio.maxGainDb,
                           globalGainDbLabel);

    EditorGUILayout.Separator();
    EditorGUILayout.PropertyField(occlusionMask);

    EditorGUILayout.Separator();
    EditorGUILayout.PropertyField(stereoSpeakerModeEnabled, stereoSpeakerModeEnabledLabel);

    EditorGUILayout.Separator();

    // Draw soundfield recorder properties.
    recorderFoldout.boolValue = EditorGUILayout.Foldout(recorderFoldout.boolValue, recorderLabel);
    if (recorderFoldout.boolValue) {
      ++EditorGUI.indentLevel;
      EditorGUI.BeginDisabledGroup(listener.IsRecording || Application.isPlaying);
      recorderSourceTag.stringValue = EditorGUILayout.TagField(recorderSourceTagLabel,
                                                               recorderSourceTag.stringValue);

      EditorGUILayout.Separator();
      recorderAmbisonicsOrder.intValue = EditorGUILayout.IntSlider(recorderAmbisonicsOrderLabel,
                                                                   recorderAmbisonicsOrder.intValue,
                                                                   0, 3);

      EditorGUILayout.Separator();
      recorderMaxTime.floatValue = EditorGUILayout.Slider(recorderMaxTimeLabel,
                                                          recorderMaxTime.floatValue,
                                                          0.0f, 600.0f);

      EditorGUILayout.Separator();
      EditorGUILayout.PropertyField(recorderSeamless, recorderSeamlessLabel);

      EditorGUILayout.Separator();
      EditorGUILayout.PropertyField(recorderAutoStart, recorderAutoStartLabel);
      EditorGUI.EndDisabledGroup();

      EditorGUILayout.Separator();

      EditorGUI.BeginDisabledGroup(recorderMaxTime.floatValue < 1.0f);
      EditorGUILayout.BeginHorizontal();
      GUILayout.Space(15 * EditorGUI.indentLevel);
      if (listener.IsRecording) {
        EditorGUILayout.BeginVertical();
        if (GUILayout.Button("Stop"))
          listener.StopSoundfieldRecorder();
        --EditorGUI.indentLevel;
        float duration = (float)listener.GetCurrentRecordDuration();
        bool exceeded = duration > recorderMaxTime.floatValue;
        EditorGUILayout.HelpBox(exceeded ? "Max Recording Time reached."
                                         : "Recording in progress: " +
                                           duration.ToString("F1") + " seconds.",
                                exceeded ? MessageType.Warning : MessageType.Info);
        ++EditorGUI.indentLevel;
        EditorGUILayout.EndVertical();
        Repaint();
      } else {
        EditorGUILayout.BeginVertical();
        if (GUILayout.Button("Record"))
          listener.StartSoundfieldRecorder();
        if (listener.HasRecordedData) {
          --EditorGUI.indentLevel;
          EditorGUILayout.HelpBox("The recording will be saved when leaving Play Mode.",
                                  MessageType.Info);
          ++EditorGUI.indentLevel;
        }
        EditorGUILayout.EndVertical();
      }
      EditorGUILayout.EndHorizontal();
      EditorGUI.EndDisabledGroup();
    }

    serializedObject.ApplyModifiedProperties();
  }
  /// @endcond
}
