using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ServerAudioPlayer : NetworkBehaviour
{

    public void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GetComponent<AudioSource>().Play();
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        GetComponent<AudioSource>().Play();
    }

}
