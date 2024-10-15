using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class SimpleClientAudioTrigger : NetworkBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryPlayAudio();
        }
    }

    public void TryPlayAudio()
    {
        if (isLocalPlayer)
        {
            if (isClientOnly)
            {
                CmdPlay();
            }
            audioSource.Play();
        }
    }

    [Command]
    void CmdPlay()
    {
        audioSource.Play();
    }
}
