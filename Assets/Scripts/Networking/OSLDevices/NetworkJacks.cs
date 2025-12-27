// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
//
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
//
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
//
//
// Copyright © 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright © 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright © 2017 Apache 2.0 Google LLC SoundStage VR
//
// Licensed under the Apache License, Version 2.0 (the "License");
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;


public class NetworkJacks : NetworkBehaviour
{
    public omniJack[] omniJacks;

    public static int idCounter = 99;
    public static int GetNextId { get { return idCounter++; } }

    public readonly SyncList<int> jackIds = new SyncList<int>();
    public readonly SyncList<int> connectedJackIds = new SyncList<int>();
    public readonly SyncList<bool> jackGrabedSync = new SyncList<bool>();

    bool started;

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Start network jack on sever");
        if (omniJacks == null) omniJacks = new omniJack[0];
        //set ID connected jacks into a synclist
        foreach (var jack in omniJacks)
        {
            if (jack.ID == -1)
            {
                jack.SetID(GetNextId, false);
            }
            jackIds.Add(jack.ID);
            connectedJackIds.Add(0);
            jackGrabedSync.Add(false);
        }
        //add update events to jacks
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isServer)
        {
            jackIds.Callback += OnJackIdUpdated;
            connectedJackIds.Callback += OnConnectionUpdated;
            jackGrabedSync.Callback += OnJackGrabedUpdated;
        }
    }

    private void Start()
    {
        Debug.Log("Start network jack");
        if (omniJacks == null) omniJacks = new omniJack[0];
        for (int i = 0; i < omniJacks.Length; i++)
        {
            attachJack(i, omniJacks[i]);
        }

        for (int i = 0; i < jackIds.Count; i++)
        {
            if (i >= omniJacks.Length) break;
            omniJacks[i].SetID(jackIds[i], false);
            OnConnectionUpdated(SyncList<int>.Operation.OP_ADD, i, 0, connectedJackIds[i]);
        }

        started = true;
    }

    private void OnDestroy()
    {
        if (omniJacks == null) return;
        foreach (var jack in omniJacks)
        {
            NetworkSpawnManager.Instance.RemoveJack(jack);
        }
    }

    public void registerJacks(omniJack[] newJacks)
    {
        // Dynamic registration for runtime-created jacks (e.g., Sequencer row output jacks).
        // Usage: call with newly spawned jacks so they get SyncList entries and callbacks.
        // Note: this list is flat and ordered by append; no x/y scheme here.
        if (newJacks == null || newJacks.Length == 0) return;
        if (omniJacks == null) omniJacks = new omniJack[0];

        for (int i = 0; i < newJacks.Length; i++)
        {
            omniJack newJack = newJacks[i];
            if (newJack == null) continue;
            if (containsJack(newJack)) continue;

            int index = omniJacks.Length;
            omniJacks = Utils.AddElementsToArray(omniJacks, new omniJack[] { newJack });

            if (started)
            {
                attachJack(index, newJack);
            }

            if (isServer)
            {
                if (newJack.ID == -1) newJack.SetID(GetNextId, false);
                jackIds.Add(newJack.ID);
                connectedJackIds.Add(0);
                jackGrabedSync.Add(false);
            }
            else if (jackIds.Count > index)
            {
                newJack.SetID(jackIds[index], false);
                if (connectedJackIds.Count > index)
                {
                    OnConnectionUpdated(SyncList<int>.Operation.OP_ADD, index, 0, connectedJackIds[index]);
                }
            }
        }
    }
    void OnConnectionUpdated(SyncList<int>.Operation op, int index, int oldValue, int newValue)
    {
        if (omniJacks == null || index < 0 || index >= omniJacks.Length) return;
        switch (op)
        {
            case SyncList<int>.Operation.OP_ADD:
                if (omniJacks[index].far != null && omniJacks[index].far.connected != null && omniJacks[index].far.connected.ID == newValue)
                {
                    Debug.Log($"Jack of id {omniJacks[index].ID} is already connected with jack of id {newValue} on this client");
                }
                else
                {
                    ManagePlugConnection(index, newValue, false);
                }
                break;
            case SyncList<int>.Operation.OP_INSERT:
                break;
            case SyncList<int>.Operation.OP_REMOVEAT:
                break;
            case SyncList<int>.Operation.OP_SET:
                if (omniJacks[index].far != null && omniJacks[index].far.connected != null && omniJacks[index].far.connected.ID == newValue)
                {
                    //Debug.Log($"Jack of id {omniJacks[index].ID} is already connected with jack of id {newValue} on this client");
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

    void OnJackIdUpdated(SyncList<int>.Operation op, int index, int oldValue, int newValue)
    {
        if (op != SyncList<int>.Operation.OP_ADD && op != SyncList<int>.Operation.OP_SET) return;
        if (omniJacks == null || index < 0 || index >= omniJacks.Length) return;
        omniJacks[index].SetID(newValue, false);
        if (connectedJackIds.Count > index)
        {
            OnConnectionUpdated(SyncList<int>.Operation.OP_ADD, index, 0, connectedJackIds[index]);
        }
    }

    public void SetJackConnection(int index)
    {
        Debug.Log($"{gameObject.name} set jack index {index}");
        if (index >= 0 && index < omniJacks.Length)
        {
            if (omniJacks[index].far.connected != null)
            {
                var otherJack = omniJacks[index].far.connected;
                //Debug.Log($"Jack of id {omniJacks[index].ID} is connected with other jack {otherJack.ID}");
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
            //Debug.Log($"Jack of id {omniJacks[index].ID} get disconnected with jack of id {connectedJackIds[index]}");
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
            //Debug.Log($"On server update jack connection of id {omniJacks[index].ID} to {otherId}");
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
    public void ManagePlugConnection(int index, int otherId, bool endConnection = true)
    {
        if (otherId == 0)
        {
            if (!endConnection)
            {
                return;
            }
            if (omniJacks[index].near != null && omniJacks[index].far != null)
            {
                if (omniJacks[index].near.curState == manipObject.manipState.grabbed || omniJacks[index].far.curState == manipObject.manipState.grabbed)
                {
                    return;
                }
                Destroy(omniJacks[index].near.gameObject);
                Destroy(omniJacks[index].far.gameObject);
            }
            omniJacks[index].endConnection(false, true);
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
                    o1.wireTrans.position,
                    o2.wireTrans.position
                };

                Color tempColor = Color.HSVToRGB(0, .8f, .5f);
                o1.Activate(o2, omniJack, tempPath, tempColor);
                o2.Activate(o1, otherJack, tempPath, tempColor);
                //Debug.Log($"Create new plug connection of {omniJack.ID} and {otherJack.ID}");
            }
        }
    }




    void OnJackGrabedUpdated(SyncList<bool>.Operation op, int index, bool oldValue, bool newValue)
    {
        if (omniJacks == null || index < 0 || index >= omniJacks.Length) return;
        switch (op)
        {
            case SyncList<bool>.Operation.OP_ADD:
                omniJacks[index].CanBeGrabed = newValue;
                break;
            case SyncList<bool>.Operation.OP_INSERT:
                break;
            case SyncList<bool>.Operation.OP_REMOVEAT:
                break;
            case SyncList<bool>.Operation.OP_SET:
                if (omniJacks[index].curState != manipObject.manipState.grabbed)
                {
                    omniJacks[index].CanBeGrabed = !newValue;
                }
                break;
            case SyncList<bool>.Operation.OP_CLEAR:
                break;
        }
    }

    void attachJack(int index, omniJack jack)
    {
        if (jack == null) return;
        NetworkSpawnManager.Instance.AddJack(jack);
        int localIndex = index;
        jack.onBeginnConnectionEvent.AddListener(delegate { SetJackConnection(localIndex); });
        jack.onEndConnectionEvent.AddListener(delegate { EndJackConnection(localIndex); });
        jack.onNotGrabableEvent.AddListener(delegate { SetJackGrab(localIndex, true); });
        jack.onIsGrabableEvent.AddListener(delegate { SetJackGrab(localIndex, false); });
    }

    bool containsJack(omniJack candidate)
    {
        for (int i = 0; i < omniJacks.Length; i++)
        {
            if (omniJacks[i] == candidate) return true;
        }
        return false;
    }

    public void SetJackGrab(int index, bool b)
    {
        if (index >= 0 && index < jackGrabedSync.Count)
        {
            if (isServer)
            {
                jackGrabedSync[index] = b;
                Debug.Log($"{gameObject.name} jack of index {index} is grabed {b}");
            }
            else
            {
                CmdUpdateJackGrabed(index, b);
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateJackGrabed(int index, bool b)
    {
        if (index >= 0 && index < jackGrabedSync.Count)
        {
            jackGrabedSync[index] = b;
            omniJacks[index].CanBeGrabed = !b;
            Debug.Log($"CMD {gameObject.name} jack of index {index} is grabed {b}");
        }
    }
}
