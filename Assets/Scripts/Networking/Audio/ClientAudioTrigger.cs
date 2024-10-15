using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ClientAudioTrigger : NetworkBehaviour
{
    private AudioSource audioSource;
    public int position = 0;
    public int samplerate = 44100;
    public float baseFrequency = 440f; // Default frequency for A4 (440 Hz)
    private float currentFrequency;
    public float amplitude = 0.5f;
    public float gain = 1f;
    private bool isPlaying = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (isLocalPlayer /*&& !isPlaying*/ && isClientOnly)
        {
            //if (Input.GetKeyDown(KeyCode.A)) PlayNote(440f);
            //if (Input.GetKeyDown(KeyCode.S)) PlayNote(466.16f);
            //if (Input.GetKeyDown(KeyCode.D)) PlayNote(493.88f);
            if (Input.GetKeyDown(KeyCode.F)) PlayNote(523.25f);
            if (Input.GetKeyDown(KeyCode.G)) PlayNote(554.37f);
            if (Input.GetKeyDown(KeyCode.H)) PlayNote(587.33f);
            if (Input.GetKeyDown(KeyCode.J)) PlayNote(622.25f);
            if (Input.GetKeyDown(KeyCode.K)) PlayNote(659.26f);
            if (Input.GetKeyDown(KeyCode.L)) PlayNote(698.46f);
        }
    }

    [Command]
    void PlayNote(float frequency)
    {
        currentFrequency = frequency;
        // Set the audio source properties for the sine wave
        audioSource.pitch = 1f; // Set pitch to 1 (no change)
        audioSource.volume = amplitude;
        audioSource.clip = AudioClip.Create("Note", 44100 * 2, 1, 44100, true, OnAudioRead, OnAudioSetPosition);
        audioSource.Play();
    }

    void OnAudioRead(float[] data)
    {
        int count = 0;
        while (count < data.Length)
        {
            data[count] = Mathf.Sin(2 * Mathf.PI * currentFrequency * position / samplerate);
            position++;
            count++;
        }
    }

    void OnAudioSetPosition(int newPosition)
    {
        position = newPosition;
    }
}


