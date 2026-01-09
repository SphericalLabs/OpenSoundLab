using UnityEditor;
using UnityEngine;

public static class DeviceSpawnerMenu
{
    private const string MenuRoot = "OpenSoundLab/Play Mode/Device Spawner/";

    [MenuItem(MenuRoot + "AD")] public static void SpawnAD() => Spawn("AD");
    [MenuItem(MenuRoot + "ADSR")] public static void SpawnADSR() => Spawn("ADSR");
    [MenuItem(MenuRoot + "Airhorn")] public static void SpawnAirhorn() => Spawn("Airhorn");
    [MenuItem(MenuRoot + "Artifact")] public static void SpawnArtifact() => Spawn("Artifact");
    [MenuItem(MenuRoot + "Button")] public static void SpawnButton() => Spawn("Button");
    [MenuItem(MenuRoot + "Camera")] public static void SpawnCamera() => Spawn("Camera");
    [MenuItem(MenuRoot + "PhaseToClockDivider")] public static void SpawnPhaseToClockDivider() => Spawn("PhaseToClockDivider");
    [MenuItem(MenuRoot + "Compressor")] public static void SpawnCompressor() => Spawn("Compressor");
    [MenuItem(MenuRoot + "Controller")] public static void SpawnController() => Spawn("Controller");
    [MenuItem(MenuRoot + "Delay")] public static void SpawnDelay() => Spawn("Delay");
    [MenuItem(MenuRoot + "Drum")] public static void SpawnDrum() => Spawn("Drum");
    [MenuItem(MenuRoot + "Filter")] public static void SpawnFilter() => Spawn("Filter");
    [MenuItem(MenuRoot + "Gain")] public static void SpawnGain() => Spawn("Gain");
    [MenuItem(MenuRoot + "Glide")] public static void SpawnGlide() => Spawn("Glide");
    [MenuItem(MenuRoot + "Keyboard")] public static void SpawnKeyboard() => Spawn("Keyboard");
    [MenuItem(MenuRoot + "Knob")] public static void SpawnKnob() => Spawn("Knob");
    [MenuItem(MenuRoot + "Looper")] public static void SpawnLooper() => Spawn("Looper");
    [MenuItem(MenuRoot + "Maracas")] public static void SpawnMaracas() => Spawn("Maracas");
    [MenuItem(MenuRoot + "Microphone")] public static void SpawnMicrophone() => Spawn("Microphone");
    [MenuItem(MenuRoot + "MIDIIN")] public static void SpawnMIDIIN() => Spawn("MIDIIN");
    [MenuItem(MenuRoot + "MIDIOUT")] public static void SpawnMIDIOUT() => Spawn("MIDIOUT");
    [MenuItem(MenuRoot + "MixerOne")] public static void SpawnMixerOne() => Spawn("MixerOne");
    [MenuItem(MenuRoot + "MixerTwo")] public static void SpawnMixerTwo() => Spawn("MixerTwo");
    [MenuItem(MenuRoot + "Noise")] public static void SpawnNoise() => Spawn("Noise");
    [MenuItem(MenuRoot + "Oscillator")] public static void SpawnOscillator() => Spawn("Oscillator");
    [MenuItem(MenuRoot + "Pano")] public static void SpawnPano() => Spawn("Pano");
    [MenuItem(MenuRoot + "PhaseGenerator")] public static void SpawnPhaseGenerator() => Spawn("PhaseGenerator");
    [MenuItem(MenuRoot + "Polarizer")] public static void SpawnPolarizer() => Spawn("Polarizer");
    [MenuItem(MenuRoot + "Quantizer")] public static void SpawnQuantizer() => Spawn("Quantizer");
    [MenuItem(MenuRoot + "Recorder")] public static void SpawnRecorder() => Spawn("Recorder");
    [MenuItem(MenuRoot + "Reverb")] public static void SpawnReverb() => Spawn("Reverb");
    [MenuItem(MenuRoot + "SampleHold")] public static void SpawnSampleHold() => Spawn("SampleHold");
    [MenuItem(MenuRoot + "Sampler")] public static void SpawnSampler() => Spawn("Sampler");
    [MenuItem(MenuRoot + "SamplerTwo")] public static void SpawnSamplerTwo() => Spawn("SamplerTwo");
    [MenuItem(MenuRoot + "Scope")] public static void SpawnScope() => Spawn("Scope");
    [MenuItem(MenuRoot + "Sequencer")] public static void SpawnSequencer() => Spawn("Sequencer");
    [MenuItem(MenuRoot + "Speaker")] public static void SpawnSpeaker() => Spawn("Speaker");
    [MenuItem(MenuRoot + "Splitter")] public static void SpawnSplitter() => Spawn("Splitter");
    [MenuItem(MenuRoot + "TapeGroup")] public static void SpawnTapeGroup() => Spawn("TapeGroup");
    [MenuItem(MenuRoot + "Tapes")] public static void SpawnTapes() => Spawn("Tapes");
    [MenuItem(MenuRoot + "Timeline")] public static void SpawnTimeline() => Spawn("Timeline");
    [MenuItem(MenuRoot + "Tutorials")] public static void SpawnTutorials() => Spawn("Tutorials");
    [MenuItem(MenuRoot + "VCA")] public static void SpawnVCA() => Spawn("VCA");
    [MenuItem(MenuRoot + "Xylophone")] public static void SpawnXylophone() => Spawn("Xylophone");

    // Validators
    [MenuItem(MenuRoot + "AD", true)]
    [MenuItem(MenuRoot + "ADSR", true)]
    [MenuItem(MenuRoot + "Airhorn", true)]
    [MenuItem(MenuRoot + "Artifact", true)]
    [MenuItem(MenuRoot + "Button", true)]
    [MenuItem(MenuRoot + "Camera", true)]
    [MenuItem(MenuRoot + "PhaseToClockDivider", true)]
    [MenuItem(MenuRoot + "Compressor", true)]
    [MenuItem(MenuRoot + "Controller", true)]
    [MenuItem(MenuRoot + "Delay", true)]
    [MenuItem(MenuRoot + "Drum", true)]
    [MenuItem(MenuRoot + "Filter", true)]
    [MenuItem(MenuRoot + "Gain", true)]
    [MenuItem(MenuRoot + "Glide", true)]
    [MenuItem(MenuRoot + "Keyboard", true)]
    [MenuItem(MenuRoot + "Knob", true)]
    [MenuItem(MenuRoot + "Looper", true)]
    [MenuItem(MenuRoot + "Maracas", true)]
    [MenuItem(MenuRoot + "Microphone", true)]
    [MenuItem(MenuRoot + "MIDIIN", true)]
    [MenuItem(MenuRoot + "MIDIOUT", true)]
    [MenuItem(MenuRoot + "MixerOne", true)]
    [MenuItem(MenuRoot + "MixerTwo", true)]
    [MenuItem(MenuRoot + "Noise", true)]
    [MenuItem(MenuRoot + "Oscillator", true)]
    [MenuItem(MenuRoot + "Pano", true)]
    [MenuItem(MenuRoot + "PhaseGenerator", true)]
    [MenuItem(MenuRoot + "Polarizer", true)]
    [MenuItem(MenuRoot + "Quantizer", true)]
    [MenuItem(MenuRoot + "Recorder", true)]
    [MenuItem(MenuRoot + "Reverb", true)]
    [MenuItem(MenuRoot + "SampleHold", true)]
    [MenuItem(MenuRoot + "Sampler", true)]
    [MenuItem(MenuRoot + "SamplerTwo", true)]
    [MenuItem(MenuRoot + "Scope", true)]
    [MenuItem(MenuRoot + "Sequencer", true)]
    [MenuItem(MenuRoot + "Speaker", true)]
    [MenuItem(MenuRoot + "Splitter", true)]
    [MenuItem(MenuRoot + "TapeGroup", true)]
    [MenuItem(MenuRoot + "Tapes", true)]
    [MenuItem(MenuRoot + "Timeline", true)]
    [MenuItem(MenuRoot + "Tutorials", true)]
    [MenuItem(MenuRoot + "VCA", true)]
    [MenuItem(MenuRoot + "Xylophone", true)]
    private static bool ValidatePlayMode()
    {
        return Application.isPlaying;
    }

    private static void Spawn(string deviceName)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Device Spawner only works in Play Mode.");
            return;
        }

        if (NetworkSpawnManager.Instance == null)
        {
            Debug.LogError("NetworkSpawnManager Instance is null. Cannot spawn device.");
            return;
        }

        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;

        if (SceneView.lastActiveSceneView != null)
        {
            spawnPosition = SceneView.lastActiveSceneView.pivot;
            if (SceneView.lastActiveSceneView.camera != null)
            {
                // Look at the camera
                Vector3 direction = SceneView.lastActiveSceneView.camera.transform.position - spawnPosition;
                if (direction != Vector3.zero)
                {
                    spawnRotation = Quaternion.LookRotation(direction);
                }
            }
        }

        GetOffsets(deviceName, out Vector3 localPositionOffset, out Vector3 localRotationOffset);

        NetworkSpawnManager.Instance.CreateItem(deviceName, spawnPosition, spawnRotation, localPositionOffset, localRotationOffset);
        Debug.Log($"Spawned {deviceName} via Editor Menu");
    }

    private static void GetOffsets(string deviceName, out Vector3 localPositionOffset, out Vector3 localRotationOffset)
    {
        localPositionOffset = Vector3.zero;
        localRotationOffset = Vector3.zero;

        // Logic adapted from menuItem.cs to ensure correct orientation
        switch (deviceName)
        {
            case "Tapes":
                localPositionOffset = new Vector3(.1f, .02f, .15f);
                localRotationOffset = new Vector3(0, 180, 0);
                break;
            case "Controller":
                localPositionOffset = new Vector3(0f, 0f, -0.15f);
                break;
            case "Xylophone":
                localRotationOffset = new Vector3(90, 0, 0);
                localPositionOffset = new Vector3(0.15f, 0f, -0.05f);
                break;
            case "Drum":
            case "Keyboard":
            case "MixerTwo":
                localRotationOffset = new Vector3(90, 0, 0);
                break;
        }
    }
}
