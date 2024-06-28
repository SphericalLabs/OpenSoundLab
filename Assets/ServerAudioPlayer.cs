using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ServerAudioPlayer : NetworkBehaviour
{

    public override void OnStartServer()
    {
        base.OnStartServer();
        GetComponent<AudioSource>().Play();
    }

}
