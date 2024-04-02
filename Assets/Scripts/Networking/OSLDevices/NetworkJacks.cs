using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NetworkJacks : NetworkBehaviour
{
    public omniJack[] omniJacks;

    public static int idCounter = 99;

    public readonly SyncList<int> jackIds = new SyncList<int>();
    public readonly SyncList<int> connectedJackIds = new SyncList<int>();

    public override void OnStartServer()
    {
        base.OnStartServer();

        //set ID connected jacks into a synclist
        
        //add all to a global list


    }
}
