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
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class omniPlug : manipObject
{
    public GameObject mouseoverFeedback;
    public int ID = -1;
    public bool outputPlug = false;
    public omniJack connected;

    Color cordColor;
    LineRenderer lr;
    public Material omniCableMat, omniCableSelectedMat;
    //Material mat;

    public Transform plugTrans;
    public Transform wireTrans;

    List<Vector3> plugPath = new List<Vector3>();
    public omniPlug otherPlug;

    Vector3 lastPos = Vector3.zero;
    Vector3 lastOtherPlugPos = Vector3.zero;
    float calmTime = 0;

    public signalGenerator signal;

    List<omniJack> targetJackList = new List<omniJack>();
    List<Transform> collCandidates = new List<Transform>();

    public MeshFilter resizeMeshFilter;

    private NetworkPlayerPlugHand targetNetworkPlugHand;
    public NetworkPlayerPlugHand TargetNetworkPlugHand { get => targetNetworkPlugHand; set => targetNetworkPlugHand = value; }

    MeshFilter plugMeshFilter;

    public override void Awake()
    {
        base.Awake();
        gameObject.layer = 12; //jacks
                               //mat = transform.GetChild(0).GetChild(0).GetComponent<Renderer>().material;
        lr = GetComponent<LineRenderer>();

        plugMeshFilter = this.GetComponentInChildren<MeshFilter>();

        cordColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
        //lr.material.SetColor("_TintColor", cordColor);
        //mat.SetColor("_TintColor", cordColor);
        //mouseoverFeedback.GetComponent<Renderer>().material.SetColor("_TintColor", cordColor);
        mouseoverFeedback.SetActive(false);

        //plugTrans = transform.GetChild(0);
        //wireTrans = transform.GetChild(1);

        onEndGrabEvents.AddListener(RemovePlugFromHand);

        if (masterControl.instance != null)
        {
            if (!masterControl.instance.jacksEnabled) GetComponent<Collider>().enabled = false;
        }
    }

    public void Setup(float c, bool outputting, omniPlug other)
    {
        //Color jackColor = Color.HSVToRGB(c, .8f, .5f);
        //cordColor = Color.HSVToRGB(c, .8f, .2f);

        //mat.SetColor("_TintColor", jackColor);
        //mouseoverFeedback.GetComponent<Renderer>().material.SetColor("_TintColor", jackColor);

        outputPlug = outputting;
        otherPlug = other;

        if (outputPlug)
        {
            //lr.material.SetColor("_TintColor", cordColor);
            plugPath.Add(otherPlug.wireTrans.position);

            updateLineVerts();
            lastOtherPlugPos = otherPlug.transform.position;
        }
    }

    public void setLineColor(Color c)
    {
        cordColor = c;
        //lr.material.SetColor("_TintColor", c);
    }

    

    public PlugData GetData()
    {
        PlugData data = new PlugData();
        data.ID = transform.GetInstanceID();
        data.position = transform.position;
        data.rotation = transform.rotation;
        data.scale = transform.localScale;
        data.outputPlug = outputPlug;
        data.connected = connected.transform.GetInstanceID();
        data.otherPlug = otherPlug.transform.GetInstanceID();
        data.plugPath = plugPath.ToArray();
        data.cordColor = cordColor;

        return data;
    }

    bool updateLineNeeded = false;

    void Update()
    {
        if (Time.frameCount % 12 == 0) UpdateLineRendererWidth();

        if (otherPlug == null)
        {
            if (lr) lr.positionCount = 0;
            if (connected != null)
            {
                endConnection();
            }
            Destroy(gameObject);
            return;
        }

        bool noChange = true;
        
        if (curState == manipState.grabbed)
        {
            if (collCandidates.Contains(closestJack))
            {
                if (connected == null) previewConnection(closestJack.GetComponent<omniJack>());
                else if (closestJack != connected.transform)
                {
                    previewConnection(closestJack.GetComponent<omniJack>());                    
                }
                updateLineNeeded = true;
            }
            if (connected != null)
            {
                if (!collCandidates.Contains(connected.transform))
                {
                    endConnection();
                }   
            }
        }


        if (lastPos != transform.position)
        {
            if (connected == null) // scanning for jacks
            {
                findClosestJack();
                if (closestJack != null) transform.LookAt(closestJack.position);
            }
            updateLineNeeded = true; // always trigger path update when moved
            lastPos = transform.position;
        }

        if (outputPlug)
        {
            if ((curState != manipState.grabbed && otherPlug.curState != manipState.grabbed)
                 && (Vector3.Distance(plugPath.Last(), transform.position) > .002f)
                 && (Vector3.Distance(plugPath[0], transform.position) > .002f))
            {
                Vector3 a = wireTrans.position - plugPath.Last();
                Vector3 b = otherPlug.wireTrans.transform.position - plugPath[0];
                for (int i = 0; i < plugPath.Count; i++) plugPath[i] += Vector3.Lerp(b, a, (float)i / (plugPath.Count - 1));
                noChange = false;
            }

            if (updateLineNeeded)
            {
                if (Vector3.Distance(plugPath.Last(), transform.position) > .005f)
                {
                    plugPath.Add(wireTrans.position);
                    calmTime = 0;
                    noChange = false;
                }
            }

            if (plugPath[0] != otherPlug.wireTrans.transform.position)
            {
                if (Vector3.Distance(plugPath[0], transform.position) > .005f)
                {
                    plugPath.Insert(0, otherPlug.wireTrans.position);
                    calmTime = 0;
                    noChange = false;
                }
            }

            //lrFlowEffect();

            //if (!noChange)
            //{
            //    calming();
            //    updateLineVerts(); // todo: cleanup
            //}

            updateLineVerts();
            //if (noChange) calmLine();

        }
    }

    public void UpdateLineRendererWidth()
    {

        if (otherPlug != null) lr.startWidth = otherPlug.plugTrans.transform.lossyScale.x * 0.010f;
        lr.endWidth = plugTrans.transform.lossyScale.x * 0.010f;
    }


    float flowVal = 0;
    void lrFlowEffect()
    {
        flowVal = Mathf.Repeat(flowVal - Time.deltaTime, 1);
        //lr.material.mainTextureOffset = new Vector2(flowVal, 0);
        //lr.material.SetFloat("_EmissionGain", .1f);
    }

    Transform closestJack;
    float jackDist = 0;
    void findClosestJack()
    {
        Transform t = null;
        float closest = Mathf.Infinity;
        bool shouldUpdateList = false;
        foreach (omniJack j in targetJackList)
        {
            if (j == null)
                shouldUpdateList = true;
            else if (j.near == null || j.near == this)
            {
                float z = Vector3.Distance(transform.position, j.transform.position);
                if (z < closest)
                {
                    closest = z;
                    t = j.transform;
                }
            }
        }

        if (shouldUpdateList) updateJackList();

        jackDist = closest;
        closestJack = t;
    }

    float calmingConstant = .5f;

    void calming()
    {
        for (int i = 0; i < plugPath.Count; i++)
        {
            if (i != 0 && i != plugPath.Count - 1)
            {
                Vector3 dest = (plugPath[i - 1] + plugPath[i] + plugPath[i + 1]) / 3;
                plugPath[i] = Vector3.Lerp(plugPath[i], dest, calmingConstant);
            }
        }

        for (int i = 0; i < plugPath.Count; i++)
        {
            if (i != 0 && i != plugPath.Count - 1)
            {
                if (Vector3.Distance(plugPath[i - 1], plugPath[i]) < .01f) plugPath.RemoveAt(i);
            }
        }

        updateLineVerts();
    }

    public void OnDestroy() { }

    void calmLine()
    {
        if (calmTime == 1)
        {
            return;
        }

        Vector3 beginPoint = plugPath[0];
        Vector3 endPoint = plugPath.Last();

        calmTime = Mathf.Clamp01(calmTime + Time.deltaTime / 1.5f);

        for (int i = 0; i < plugPath.Count; i++)
        {
            if (i != 0 && i != plugPath.Count - 1)
            {
                Vector3 dest = (plugPath[i - 1] + plugPath[i] + plugPath[i + 1]) / 3;
                plugPath[i] = Vector3.Lerp(plugPath[i], dest, Mathf.Lerp(calmingConstant, 0, calmTime));
            }
        }

        for (int i = 0; i < plugPath.Count; i++)
        {
            if (i != 0 && i != plugPath.Count - 1)
            {
                if (Vector3.Distance(plugPath[i - 1], plugPath[i]) < .01f) plugPath.RemoveAt(i);
            }
        }
        plugPath[0] = beginPoint;
        plugPath[plugPath.Count - 1] = endPoint;
        updateLineVerts();
    }


    public void updateLineType()
    {
        updateLineVerts();
    }


    bool forcedWireShow = false;
    void updateLineVerts(bool justLast = false)
    {
        if (!outputPlug) return; // only outputPlugs use their LineRenderers

        if (masterControl.instance.WireSetting == WireMode.Curved)
        {
            lr.positionCount = plugPath.Count;
            if (justLast) lr.SetPosition(plugPath.Count - 1, plugPath.Last());
            else lr.SetPositions(plugPath.ToArray());
        }
        else if (masterControl.instance.WireSetting == WireMode.Straight && plugPath.Count >= 2)
        {
            lr.positionCount = 2;
            lr.SetPosition(0, plugPath[0]);
            lr.SetPosition(1, plugPath.Last());
        }
        else if (forcedWireShow)
        {
            lr.positionCount = 2;
            lr.SetPosition(0, plugPath[0]);
            lr.SetPosition(1, plugPath.Last());
        }
        else // WireMode.Invisible
        {
            lr.positionCount = 0;
        }
    }

    void updateJackList()
    {
        targetJackList.Clear();

        omniJack[] possibleJacks = FindObjectsOfType<omniJack>();
        for (int i = 0; i < possibleJacks.Length; i++)
        {
            if (possibleJacks[i].outgoing != outputPlug)
            {
                if (otherPlug.connected == null)
                {
                    targetJackList.Add(possibleJacks[i]);
                }
                else if (otherPlug.connected.transform.parent != possibleJacks[i].transform.parent)
                {
                    targetJackList.Add(possibleJacks[i]);
                }
            }
        }

    }

    public void Destruct()
    {
        Destroy(gameObject);
    }



    // this matches the scale of the plug to the scale of the jack
    public void matchPlugtoJackScale(omniJack jackReference, bool preview = false)
    {
        if (jackReference != null)
        {
            if (preview)
            {
                // skip if already pretty much the same size
                if (Vector3.Distance(plugTrans.transform.lossyScale, jackReference.transform.lossyScale) < 0.0001f) return;

                plugTrans.localScale = Vector3.one;

                // mitigates the scaling difference
                plugTrans.localScale = new Vector3(
                       jackReference.transform.lossyScale.x / plugTrans.transform.lossyScale.x,
                       jackReference.transform.lossyScale.y / plugTrans.transform.lossyScale.y,
                       jackReference.transform.lossyScale.z / plugTrans.transform.lossyScale.z
                   );
            }
            else
            {
                plugTrans.localScale = Vector3.one; // reset plugTrans adjustments from preview
                transform.localScale = Vector3.one; // reset localScale after parenting adjusted scale 
            }

        }

        UpdateLineRendererWidth();

    }

    void OnCollisionEnter(Collision coll)
    {
        if (curState != manipState.grabbed) return;
        if (coll.transform.tag != "omnijack") return;

        omniJack j = coll.transform.GetComponent<omniJack>();

        if (!targetJackList.Contains(j)) return;
        if (j.signal != null || j.near != null) return;

        collCandidates.Add(j.transform);
    }


    // this is called when a preview connection is made, when the plug is half inserted
    void previewConnection(omniJack j)
    {
        if (connected == j) return;
        if (connected != null) endConnection();
        if (manipulatorObjScript != null) manipulatorObjScript.hapticPulse(1000);

        connected = j;
        //connected.beginConnection(this, true);
        signal = connected.homesignal;

        plugTrans.position = connected.transform.position;
        plugTrans.rotation = connected.transform.rotation;
        plugTrans.parent = connected.transform; // plugMesh set as child of connected jack
        plugTrans.Rotate(-90, 0, 0);
        
        plugTrans.parent = transform; // needs the right parents for this
        matchPlugtoJackScale(connected, true);        
        

        plugTrans.parent = connected.transform; // but put back immediately

        plugTrans.Translate(0, 0, -0.01f * plugTrans.lossyScale.x); // do not insert fully

        UpdateLineRendererWidth();
        // if connecting an outgoing plug that will not have its line renderer active,
        // then this will trigger the update of the other side that actually leads to visible results
        if (otherPlug != null) otherPlug.UpdateLineRendererWidth();

    }


    // This is called when fully inserting a plug during local live patching (not loading or via multi-user)
    public void Release()
    {

        if (otherPlug.connected != null)
        {
            otherPlug.connected.onIsGrabableEvent.Invoke();
        }
        if (connected == null)
        {
            if (lr) lr.positionCount = 0;
            otherPlug.Destruct();
            Destroy(gameObject);
        }
        else
        {
            // i.e. if it is attached to the manipulator?
            if (plugTrans.parent != connected.transform)
            {
                plugTrans.position = connected.transform.position;
                plugTrans.rotation = connected.transform.rotation;
                plugTrans.parent = connected.transform;
                plugTrans.Rotate(-90, 0, 0);
                //plugTrans.Translate(0, 0, -0.03f);
            }
            plugTrans.Translate(0, 0, 0.01f * plugTrans.lossyScale.x); // undo preview position from previewConnection
            transform.parent = plugTrans.parent; // omniPlug set as child of connected jack 
            transform.position = plugTrans.position;
            transform.rotation = plugTrans.rotation;
            plugTrans.parent = transform; // plugMesh set as child of omniPlug

            //plugTrans.Translate(0, 0, 0.014f);

            calmTime = 0;
            connected.beginConnection(this, true);

            collCandidates.Clear();
        }

        matchPlugtoJackScale(connected, false); 

    }


    // this is called when cables are created by loading or via multi-user
    public void Activate(omniPlug siblingPlug, omniJack jackIn, Vector3[] tempPath, Color tempColor, bool invokeEvent = false)
    {
        float h, s, v;

        if (outputPlug)
        {

            plugPath = tempPath.ToList<Vector3>();
            updateLineVerts();
            calmTime = 1;
        }

        otherPlug = siblingPlug;
        connected = jackIn;
        connected.beginConnection(this, invokeEvent);
        signal = connected.homesignal;

        plugTrans.position = connected.transform.position;
        plugTrans.rotation = connected.transform.rotation;
        plugTrans.parent = connected.transform;
        plugTrans.Rotate(-90, 0, 0);
        //plugTrans.Translate(0, 0, -.02f);

        transform.parent = plugTrans.parent;
        transform.position = plugTrans.position;
        transform.rotation = plugTrans.rotation;
        plugTrans.parent = transform;

        lastOtherPlugPos = otherPlug.plugTrans.transform.position;
        lastPos = transform.position;

        matchPlugtoJackScale(connected, false);
        //updateLineNeeded = true;
    }

    void OnCollisionExit(Collision coll)
    {
        omniJack j = coll.transform.GetComponent<omniJack>();
        if (j != null)
        {
            if (collCandidates.Contains(coll.transform)) collCandidates.Remove(coll.transform);
        }
    }



    // When removing a plug but still holding on to it
    void endConnection()
    {
        connected.endConnection(true, false);
        connected = null;
        signal = null;

        plugTrans.parent = transform;
        plugTrans.localPosition = Vector3.zero;
        plugTrans.localRotation = Quaternion.identity;
    }

    public void updateForceWireShow(bool on)
    {
        if (outputPlug)
        {
            forcedWireShow = on;
            updateLineVerts();
        }
        else
        {
            otherPlug.updateForceWireShow(on);
        }
    }

    public void mouseoverEvent(bool on)
    {
        mouseoverFeedback.SetActive(on);

        if (!on && curState == manipState.none)
        {
            updateForceWireShow(false);
        }
        else updateForceWireShow(true);
    }

    public override void setState(manipState state)
    {
        if (curState == state) return;

        if (curState == manipState.selected)
        {
            mouseoverFeedback.SetActive(false);
        }

        if (curState == manipState.grabbed)
        {
            Release();
        }

        curState = state;

        if (curState == manipState.none)
        {
            updateForceWireShow(false);
            setCableHighlighted(false);
            if (otherPlug != null) otherPlug.setCableHighlighted(false);
        }

        if (curState == manipState.selected)
        {
            updateForceWireShow(true);
            mouseoverFeedback.SetActive(true);
            setCableHighlighted(true);
            if (otherPlug != null) otherPlug.setCableHighlighted(true);
        }

        if (curState == manipState.grabbed)
        {
            updateForceWireShow(true);
            collCandidates.Clear();
            if (connected != null) collCandidates.Add(connected.transform);

            Vector3 posDiff = Vector3.zero;

            if (manipulatorObjScript.wasGazeBased) // remote patching
            {
                // grabbing a freshly spawned far plug or a plug that was already connected
                Transform grabReference = connected == null ? gazedObjectTracker.Instance.gazedAtManipObject.transform : connected.transform;


                // translate based on reference, in this case for gaze-based remote patching          
                transform.position = grabReference.transform.position + grabReference.transform.up * -0.075f;
                gazeBasedPosRotStart();

            }
            else // manual patching
            {
                transform.parent = manipulatorObj.parent;
                // fix position at hand
                posDiff = new Vector3(0f, 0f, 0.06f);
                transform.localPosition = posDiff;
            }

            AddPlugToHand(otherPlug.connected.ID);
        }

        updateJackList();

        //foreach (omniJack j in targetJackList) j.flash(cordColor); 

    }


    public override void grabUpdate(Transform t)
    {

        if (manipulatorObjScript.wasGazeBased)
        {
            gazeBasedPosRotUpdate();
            return;
        }

    }

    // copied from handle.cs, consider unifying refactoring

    Vector3 initialOffset;
    bool wasPrecisionGazeGrabbed = false; // at last frame


    void gazeBasedPosRotStart()
    {
        Transform go1 = manipulatorObj.transform;
        Transform go2 = this.transform;

        initialOffset = go2.position - go1.position;
    }

    void gazeBasedPosRotUpdate()
    {

        if (OSLInput.getInstance().isSidePressed(manipulatorObjScript.controllerIndex) == false) // fine by default
        {
            transform.parent = plugTrans.parent;

            if (!wasPrecisionGazeGrabbed)
            {
                gazeBasedPosRotStart();
            }

            Transform go1 = manipulatorObj.transform;
            Transform go2 = this.transform;

            // Calculate the desired position in world space for go2 based on the changes you want
            Vector3 desiredPosition = go1.position + initialOffset;

            // Apply changes to the local position of go2 based on the desired position
            go2.position = desiredPosition;

            wasPrecisionGazeGrabbed = true;
        }
        else // coarse
        {
            transform.parent = manipulatorObj.parent;
            wasPrecisionGazeGrabbed = false;
        }


    }


    public void setCableHighlighted(bool on)
    {
        lr.sharedMaterial = on ? omniCableSelectedMat : omniCableMat;
    }



    public void AddPlugToHand(int index)
    {
        Debug.Log($"Grab Plug connected with {index}");
        var targetHand = NetworkMenuManager.Instance.localPlayer.GetTargetPlugHand(manipulatorObjScript);
        if (targetHand != null)
        {
            targetHand.SetHandJackIndex(index, transform.localPosition, transform.localRotation);
            targetNetworkPlugHand = targetHand;
            targetNetworkPlugHand.PlugInHand = this;
        }
    }

    void RemovePlugFromHand()
    {
        if (targetNetworkPlugHand != null)
        {
            targetNetworkPlugHand.PlugInHand = null;
            targetNetworkPlugHand.SetHandJackIndex(0, Vector3.zero, Quaternion.identity);
            targetNetworkPlugHand = null;
        }
    }
}
