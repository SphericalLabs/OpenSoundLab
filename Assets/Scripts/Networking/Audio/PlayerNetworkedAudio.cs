using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Mirror;

public class PlayerNetworkedAudio : NetworkBehaviour
{
    private AudioSource audioSource;
    public static Queue<Action> ExecuteOnMainThread = new Queue<Action>();
    public int chunkSize = 1024;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }


    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            audioSource.Play();
        }
        while (ExecuteOnMainThread.Count > 0)
        {
            ExecuteOnMainThread.Dequeue().Invoke();
        }
    }
    private void OnAudioFilterRead(float[] data, int channels)
    {
        // Capture audio data and send it to all clients
        if (isServer)
        {
            // Convert float array to byte array
            byte[] byteData = FloatArrayToByteArray(data);

            // Break the byte array into chunks
            for (int i = 0; i < byteData.Length; i += chunkSize)
            {
                int remainingBytes = Mathf.Min(chunkSize, byteData.Length - i);
                ArraySegment<byte> chunk = new ArraySegment<byte>(byteData, i, remainingBytes);

                // Send audio data chunk to all clients
                ExecuteOnMainThread.Enqueue(() => RpcReceiveAudioDataOnClients(chunk));
            }
        }
    }

    //private IEnumerator SendAudioDataInChunks(byte[] audioData)
    //{
    //    int totalChunks = Mathf.CeilToInt((float)audioData.Length / chunkSize);

    //    for (int i = 0; i < totalChunks; i++)
    //    {
    //        int startIdx = i * chunkSize;
    //        int endIdx = Mathf.Min((i + 1) * chunkSize, audioData.Length);

    //        byte[] chunk = new byte[endIdx - startIdx];
    //        System.Array.Copy(audioData, startIdx, chunk, 0, chunk.Length);

    //        // Send audio chunk to all clients
    //        RpcReceiveAudioDataOnClients(chunk);
    //        yield return null; // Wait for a short time between chunks
    //    }
    //}

    [ClientRpc(includeOwner = false)]
    void RpcReceiveAudioDataOnClients(ArraySegment<byte> audioData)
    {
        Debug.Log("RPC Call");
        // On clients, convert byte array to float array and play the audio
        // Debug.Log("Received Audio Data");
        // Convert byte array to float array
        float[] receivedAudioData = ByteArrayToFloatArray(audioData.Array, audioData.Offset, audioData.Count);
        // Play the received audio on the AudioSource
        PlayReceivedAudio(receivedAudioData);


    }



    void PlayReceivedAudio(float[] audioData)
    {
        // Set the received audio data to the AudioSource
        audioSource.clip = AudioClip.Create("ReceivedAudio", audioData.Length, 1, AudioSettings.outputSampleRate, false);
        audioSource.clip.SetData(audioData, 0);

        // Play the received audio
        audioSource.Play();
    }

    byte[] FloatArrayToByteArray(float[] floatArray)
    {
        byte[] byteArray = new byte[floatArray.Length * 4];
        Buffer.BlockCopy(floatArray, 0, byteArray, 0, byteArray.Length);
        return byteArray;
    }

    private float[] ByteArrayToFloatArray(byte[] byteArray, int offset, int count)
    {
        float[] floatArray = new float[count / 4]; // Assuming 4 bytes per float
        Buffer.BlockCopy(byteArray, offset, floatArray, 0, count);
        return floatArray;
    }


    //public AudioSource audioSource;
    //public AudioClip audioClip;
    //private AudioClip receivedAudioClip;
    //public int position = 0;
    //public int samplerate = 44100;
    //public float frequency = 440;
    //float[] samples;
    //// Convert audio data to byte array
    //byte[] ConvertAudioToByteArray()
    //{
    //    //AudioClip audioClip = audioSource.clip;

    //    // Ensure the audio clip is not null
    //    if (audioClip == null)
    //    {
    //        Debug.LogError("AudioClip is null.");
    //        return null;
    //    }

    //    // Convert the audio clip to a float array
    //    float[] samples = new float[audioClip.samples * audioClip.channels];
    //    audioClip.GetData(samples, 0);

    //    // Convert the float array to a byte array
    //    byte[] audioData = new byte[samples.Length * 4]; // 4 bytes per float
    //    Buffer.BlockCopy(samples, 0, audioData, 0, audioData.Length);

    //    return audioData;
    //}

    //// Convert byte array to audio data
    //void ConvertByteArrayToAudio(byte[] audioData)
    //{
    //    // Convert the byte array to a float array
    //    samples = new float[audioData.Length / 4]; // 4 bytes per float
    //    Buffer.BlockCopy(audioData, 0, samples, 0, audioData.Length);

    //    // Create a new audio clip and set the data
    //    receivedAudioClip.SetData(samples, 0);
    //    receivedAudioClip = AudioClip.Create("ReceivedAudio", samples.Length, 1, samplerate, true, OnAudioRead, OnAudioSetPosition);
    //    //receivedAudioClip.SetData(samples, 0);

    //    // Play the received audio

    //    audioSource.clip = receivedAudioClip;


    //    audioSource.Play();
    //}
    //void OnAudioRead(float[] data)
    //{

    //    int count = 0;
    //    while (count < data.Length)
    //    {
    //        //samples[count] = data[count];

    //        data[count] = MathF.Sin(2 * Mathf.PI * frequency * position / samplerate);
    //        position++;
    //        count++;
    //    }
    //}
    //void OnAudioSetPosition(int newPosition)
    //{
    //    position = newPosition;
    //}
    //// Send byte array over the network
    //[Command]
    //void CmdSendAudioDataToServer(byte[] audioData)
    //{
    //    RpcReceiveAudioDataOnClient(audioData);
    //}

    //[ClientRpc]
    //void RpcReceiveAudioDataOnClient(byte[] audioData)
    //{
    //    ConvertByteArrayToAudio(audioData);
    //}

    //// Example of how to trigger the audio data transfer
    //void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space) && isLocalPlayer)
    //    {
    //        byte[] audioData = ConvertAudioToByteArray();
    //        CmdSendAudioDataToServer(audioData);
    //    }
    //}
    //_---------------------------------------------------------------------------------------

    //private float[] receivedAudioData;
    //private int positionInSamples;

    //void OnAudioFilterRead(float[] data, int channels)
    //{
    //    // Check if we have received audio data
    //    if (receivedAudioData != null)
    //    {
    //        // Copy audio data from the received array to the output data
    //        int samplesToCopy = Mathf.Min(data.Length, receivedAudioData.Length - positionInSamples);
    //        System.Array.Copy(receivedAudioData, positionInSamples, data, 0, samplesToCopy);
    //        positionInSamples += samplesToCopy;

    //        // If we've reached the end of the received audio, reset for the next iteration
    //        if (positionInSamples >= receivedAudioData.Length)
    //        {
    //            receivedAudioData = null;
    //            positionInSamples = 0;
    //        }
    //    }
    //}

    //// Send byte array over the network
    //[Command]
    //void CmdSendAudioDataToServer(float[] audioData)
    //{
    //    RpcReceiveAudioDataOnClient(audioData);
    //}

    //[ClientRpc]
    //void RpcReceiveAudioDataOnClient(float[] audioData)
    //{
    //    // Set the received audio data
    //    receivedAudioData = audioData;
    //    positionInSamples = 0;

    //    AudioSource audioSource = GetComponent<AudioSource>();
    //    int channels = 2; // Adjust based on your audio data
    //    audioSource.clip = AudioClip.Create("ReceivedAudio", receivedAudioData.Length, channels, 44100, true);
    //    audioSource.clip.SetData(receivedAudioData, 0);
    //    audioSource.Play();
    //}

    //// Example of how to trigger the audio data transfer
    //void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space) && isLocalPlayer)
    //    {
    //        // Capture the audio from the AudioListener
    //        int bufferSize = 8192; // Adjust as needed
    //        float[] audioData = new float[bufferSize];
    //        AudioListener.GetOutputData(audioData, 0);

    //        // Send audio data to the server
    //        CmdSendAudioDataToServer(audioData);
    //    }
    //}
}
