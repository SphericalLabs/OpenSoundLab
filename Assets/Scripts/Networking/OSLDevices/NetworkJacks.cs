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
        for (int i = 0; i < omniJacks.Length; i++)
        {
            NetworkSpawnManager.Instance.AddJack(omniJacks[i]);
            int index = i;
            omniJacks[i].onBeginnConnectionEvent.AddListener(delegate { SetJackConnection(index); });
            omniJacks[i].onEndConnectionEvent.AddListener(delegate { EndJackConnection(index); });
        }

        for (int i = 0; i < jackIds.Count; i++)
        {
            omniJacks[i].ID = jackIds[i];
            OnConnectionUpdated(SyncList<int>.Operation.OP_ADD, i, 0, connectedJackIds[i]);
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
                if (omniJacks[index].far != null && omniJacks[index].far.connected != null && omniJacks[index].far.connected.ID == newValue)
                {
                    Debug.Log($"Jack of id {omniJacks[index].ID} is already connected with jack of id {newValue} on this client");
                }
                else
                {
                    ManagePlugConnection(index, newValue);
                }
                break;
            case SyncList<int>.Operation.OP_INSERT:
                break;
            case SyncList<int>.Operation.OP_REMOVEAT:
                break;
            case SyncList<int>.Operation.OP_SET:
                if (omniJacks[index].far != null && omniJacks[index].far.connected != null && omniJacks[index].far.connected.ID == newValue)
                {
                    Debug.Log($"Jack of id {omniJacks[index].ID} is already connected with jack of id {newValue} on this client");
                }
                else if (oldValue != newValue)
                {
                    ManagePlugConnection(index, newValue);
                }
                break;
            case SyncList<int>.Operation.OP_CLEAR:
                break;
        }
    }

    public void SetJackConnection(int index)
    {
        if (index >= 0 && index < omniJacks.Length)
        {
            if (omniJacks[index].far.connected != null)
            {
                var otherJack = omniJacks[index].far.connected;
                Debug.Log($"Jack of id {omniJacks[index].ID} is connected with other jack {otherJack.ID}");
                if (isServer)
                {
                    connectedJackIds[index] = otherJack.ID;
                }
                else
                {
                    CmdUpdateJackConnection(index, otherJack.ID);
                }
            }
        }
    }

    public void EndJackConnection(int index)
    {
        if (index >= 0 && index < omniJacks.Length)
        {
            Debug.Log($"Jack of id {omniJacks[index].ID} get disconnected with jack of id {connectedJackIds[index]}");
            if (isServer)
            {
                connectedJackIds[index] = 0;
            }
            else
            {
                CmdUpdateJackConnection(index, 0);
            }
        }
    }


    [Command(requiresAuthority = false)]
    public void CmdUpdateJackConnection(int index, int otherId)
    {
        if (connectedJackIds[index] != otherId)
        {
            connectedJackIds[index] = otherId;
            Debug.Log($"On server update jack connection of id {omniJacks[index].ID} to {otherId}");
            //ManagePlugConnection(index, otherId);

            if (omniJacks[index].far != null && omniJacks[index].far.connected != null && omniJacks[index].far.connected.ID == otherId)
            {
                Debug.Log($"Jack of id {omniJacks[index].ID} is already connected with jack of id {otherId} on this client");
            }
            else
            {
                //create plug connection
                ManagePlugConnection(index, otherId);
            }
        }
    }

    //create jack plugs
    public void ManagePlugConnection(int index, int otherId)
    {
        if (otherId == 0)
        {
            if (omniJacks[index].near != null && omniJacks[index].far != null)
            {
                if (omniJacks[index].near.curState == manipObject.manipState.grabbed || omniJacks[index].far.curState == manipObject.manipState.grabbed)
                {
                    return;
                }
                Destroy(omniJacks[index].near.gameObject);
                Destroy(omniJacks[index].far.gameObject);
            }
            omniJacks[index].endConnection(false);
        }
        else
        {
            var omniJack = omniJacks[index];
            var otherJack = NetworkSpawnManager.Instance.GetJackById(otherId);
            if (otherJack != null)
            {
                //create plugs
                omniPlug o1 = (Instantiate(omniJack.plugPrefab, omniJack.transform.position, omniJack.transform.rotation) as GameObject).GetComponent<omniPlug>();
                o1.outputPlug = !omniJack.outgoing;
                omniPlug o2 = (Instantiate(otherJack.plugPrefab, otherJack.transform.position, otherJack.transform.rotation) as GameObject).GetComponent<omniPlug>();
                o2.outputPlug = !otherJack.outgoing;
                Vector3[] tempPath = new Vector3[] {
                    omniJack.transform.position,
                    otherJack.transform.position
                };

                Color tempColor = Color.HSVToRGB(0, .8f, .5f);
                o1.Activate(o2, omniJack, tempPath, tempColor);
                o2.Activate(o1, otherJack, tempPath, tempColor);
                Debug.Log($"Create new plug connection of {omniJack.ID} and {otherJack.ID}");
            }
        }
    }

}
