using UnityEngine;

// The code example shows how to implement a metronome that procedurally
// generates the click sounds via the OnAudioFilterRead callback.
// While the game is paused or suspended, this time will not be updated and sounds
// playing will be paused. Therefore developers of music scheduling routines do not have
// to do any rescheduling after the app is unpaused

[RequireComponent(typeof(AudioSource))]
public class AudioTest : MonoBehaviour
{
    AudioClip customAudioClip;
    int sampleRate;

    void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;
        // Create a new audio clip with a PCM read callback
        customAudioClip = AudioClip.Create("CustomAudio", sampleRate, 1, sampleRate, true, OnAudioRead);

        // Assign the custom audio clip to an AudioSource
        AudioSource audioSource = GetComponent<AudioSource>();
        audioSource.clip = customAudioClip;

        // Start playing the audio
        audioSource.Play();
    }

    // PCM read callback function
    void OnAudioRead(float[] data)
    {
        // Generate or provide custom audio data here
        // Populate the 'data' array with audio samples
        // ...

        // For simplicity, let's fill the array with a sine wave
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = Mathf.Sin(2 * Mathf.PI * 440 * i / (float)sampleRate);
        }
    }

}