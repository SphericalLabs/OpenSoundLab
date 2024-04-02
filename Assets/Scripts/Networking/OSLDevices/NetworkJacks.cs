using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NetworkJacks : NetworkBehaviour
{
    public omniJack[] omniJacks;

    public static int idCounter = 99;
    public static int GetNextId {  get { return idCounter++; } }

    public readonly SyncList<int> jackIds = new SyncList<int>();
    public readonly SyncList<int> connectedJackIds = new SyncList<int>();

    public override void OnStartServer()
    {
        base.OnStartServer();

        //set ID connected jacks into a synclist
        foreach (var jack in omniJacks)
        {
            jack.ID = GetNextId;
            jackIds.Add(jack.ID);
            connectedJackIds.Add(0);
        }
        //add update events to jacks
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isServer)
        {
            connectedJackIds.Callback += OnConnectionUpdated;
        }
    }

    private void Start()
    {
        foreach (var jack in omniJacks)
        {
            NetworkSpawnManager.Instance.AddJack(jack);
            // Process initial SyncList payload
            for (int i = 0; i < jackIds.Count; i++)
            {
                omniJacks[i].ID = jackIds[i];
                OnConnectionUpdated(SyncList<int>.Operation.OP_ADD, i, 0, connectedJackIds[i]);
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var jack in omniJacks)
        {
            NetworkSpawnManager.Instance.RemoveJack(jack);
        }
    }
    void OnConnectionUpdated(SyncList<int>.Operation op, int index, int oldValue, int newValue)
    {
        switch (op)
        {
            case SyncList<int>.Operation.OP_ADD:
                break;
            case SyncList<int>.Operation.OP_INSERT:
                break;
            case SyncList<int>.Operation.OP_REMOVEAT:
                break;
            case SyncList<int>.Operation.OP_SET:
                break;
            case SyncList<int>.Operation.OP_CLEAR:
                break;
        }
    }

    public void SetJackConnection(int near, int far)
    {

    }

    public void EndJackConnection(int near, int far)
    {

    }

    //create jack plugs
}
