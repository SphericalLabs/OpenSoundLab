// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright � 2020-2024 OSLLv1 Spherical Labs OpenSoundLab
//
// OpenSoundLab is licensed under the OpenSoundLab License Agreement (OSLLv1).
// You may obtain a copy of the License at
// https://github.com/SphericalLabs/OpenSoundLab/LICENSE-OSLLv1.md
//
// By using, modifying, or distributing this software, you agree to be bound by the terms of the license.
//
//
// Copyright � 2020 Apache 2.0 Maximilian Maroe SoundStage VR
// Copyright � 2019-2020 Apache 2.0 James Surine SoundStage VR
// Copyright � 2017 Apache 2.0 Google LLC SoundStage VR
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.Events;

public class multipleDeviceInterface : deviceInterface
{

    public GameObject splitterNodePrefab;
    private List<multipleNodeSignalGenerator> nodeSignalGenerators;
    private NetworkJacks networkJacks;
    public omniJack input, output;
    public xHandle stretchSlider;
    public Transform handleA, handleB;
    multipleSignalGenerator signal;
    basicSwitch flowSwitch;

    public Renderer symbolA;
    public Renderer symbolB;

    int count = 0;
    int lastCount = 0;

    public bool isSplitter = true; // this is overwritten by the prefab inspector config

    public Material mixerMaterial;
    public Material splitterMaterial;

    public UnityEvent OnIsSplitterChanged;

    public override void Awake()
    {
        signal = GetComponent<multipleSignalGenerator>();
        signal.nodes = new List<multipleNodeSignalGenerator>();

        symbolA.sharedMaterial = mixerMaterial;
        symbolB.sharedMaterial = mixerMaterial;

        CreateInactiveNodes();

        stretchSlider.onPosSetEvent.AddListener(CalculateAndUpdateNodeCount);

        CalculateAndUpdateNodeCount();

        setFlow(); // reads in default value from the prefab to enable MultiMix and MultiSplit
    }

    void CreateInactiveNodes()
    {
        if (stretchSlider != null)
        {
            nodeSignalGenerators = new List<multipleNodeSignalGenerator>();
            List<omniJack> newJacks = new List<omniJack>();

            int maxCount = Mathf.FloorToInt((stretchSlider.xBounds.x - .02f) / -.04f) - 1;
            for (int i = 0; i < maxCount; i++)
            {
                multipleNodeSignalGenerator s = (Instantiate(splitterNodePrefab, transform, false) as GameObject).GetComponent<multipleNodeSignalGenerator>();
                s.setup(signal, isSplitter);
                s.transform.localPosition = new Vector3(-.04f * (i + 1), 0, 0);
                nodeSignalGenerators.Add(s);
                var nodeJack = s.GetComponentInChildren<omniJack>();
                if (nodeJack != null)
                {
                    newJacks.Add(nodeJack);
                }
                s.gameObject.SetActive(false);
            }

            // update internal list, too
            updateNodeCount(false);

            networkJacks = GetComponent<NetworkJacks>();
            foreach (var jack in networkJacks.omniJacks)
            {
                newJacks.Add(jack); // this adds the manually added omniJacks to the end of the array
            }

            networkJacks.omniJacks = newJacks.ToArray();
        }
    }

    public void CalculateAndUpdateNodeCount()
    {
        float xVal = stretchSlider.transform.localPosition.x;

        count = Mathf.FloorToInt((xVal - .02f) / -.04f) - 1;
        updateNodeCount();
    }

    void updateNodeCount(bool updateByGrab = false)
    {

        if (count == lastCount)
            return;

        if (count > lastCount)
        {
            for (int i = lastCount; i < count; i++)
            {
                if (!signal.nodes.Contains(nodeSignalGenerators[i]))
                {
                    signal.nodes.Add(nodeSignalGenerators[i]);
                }
                nodeSignalGenerators[i].gameObject.SetActive(true);
            }
        }
        else if (count < lastCount)
        {
            for (int i = lastCount - 1; i >= count; i--)
            {
                signal.nodes.RemoveAt(i);
                nodeSignalGenerators[i].gameObject.SetActive(false);
                if (updateByGrab && networkJacks.omniJacks[i].near != null && networkJacks.omniJacks[i].far != null)
                {
                    var nearPlug = networkJacks.omniJacks[i].near;
                    var farPlug = networkJacks.omniJacks[i].far;
                    networkJacks.omniJacks[i].endConnection(true, true);

                    Destroy(nearPlug.gameObject);
                    Destroy(farPlug.gameObject);
                }
            }
        }
        lastCount = count;
        handleA.localPosition = new Vector3(-.02f * signal.nodes.Count, 0, 0);
        handleB.localPosition = new Vector3(-.02f * signal.nodes.Count, 0, 0);

        handleA.localScale = new Vector3(.04f * (signal.nodes.Count + 1), 0.04f, 0.04f);
        handleB.localScale = new Vector3(.04f * (signal.nodes.Count + 1), 0.04f, 0.04f);
    }

    public void setFlow()
    {
        if (isSplitter)
        {
            symbolA.transform.localPosition = new Vector3(.0025f, .0012f, .0217f);
            symbolA.transform.localRotation = Quaternion.Euler(0, 180, 0);
            symbolA.sharedMaterial = mixerMaterial;

            symbolB.transform.localPosition = new Vector3(.0025f, .0012f, -.0217f);
            symbolB.transform.localRotation = Quaternion.Euler(0, 180, 0);
            symbolB.sharedMaterial = mixerMaterial;
        }
        else
        {
            symbolA.transform.localPosition = new Vector3(.00075f, -.0016f, .0217f);
            symbolA.transform.localRotation = Quaternion.Euler(0, 0, 90);
            symbolA.sharedMaterial = splitterMaterial;

            symbolB.transform.localPosition = new Vector3(.00075f, -.0016f, -.0217f);
            symbolB.transform.localRotation = Quaternion.Euler(0, 0, 90);
            symbolB.sharedMaterial = splitterMaterial;
        }

        if (input.near != null)
        {
            input.near.Destruct();
            input.signal = null;
        }
        if (output.near != null)
        {
            output.near.Destruct();
            output.signal = null;
        }

        input.outgoing = !isSplitter;
        output.outgoing = isSplitter;

        for (int i = 0; i < signal.nodes.Count; i++) signal.nodes[i].setFlow(isSplitter);

        signal.setFlow(isSplitter);

        OnIsSplitterChanged.Invoke();
    }


    void Update()
    {
        linkJackandGenerator();

        if (stretchSlider.curState == manipObject.manipState.grabbed)
        {
            float xVal = stretchSlider.transform.localPosition.x;
            count = Mathf.FloorToInt((xVal - .02f) / -.04f) - 1;
            if (count != signal.nodes.Count) updateNodeCount(true);
        }
    }

    void linkJackandGenerator()
    {
        if (isSplitter)
        {
            if (signal.incoming != input.signal) signal.incoming = input.signal;
        }
        else if (signal.incoming != output.signal) signal.incoming = output.signal;
    }

    public override InstrumentData GetData()
    {
        MultipleData data = new MultipleData();

        data.deviceType = isSplitter ? DeviceType.MultiSplit : DeviceType.MultiMix; // this defines which prefab is loaded, even though both share the same deviceInterface. isSplitter is set in the prefabs.
        GetTransformData(data);

        data.jackInID = input.transform.GetInstanceID();

        data.jackCount = count + 1;
        data.jackOutID = new int[data.jackCount];

        data.jackOutID[0] = output.transform.GetInstanceID();

        for (int i = 1; i < data.jackCount; i++)
        {
            data.jackOutID[i] = signal.nodes[i - 1].jack.transform.GetInstanceID();
        }

        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        MultipleData data = d as MultipleData;
        base.Load(data, copyMode);

        input.SetID(data.jackInID, copyMode);


        if (data.jackCount < 2)
        {
            count = 1;
            Vector3 pos = stretchSlider.transform.localPosition;
            pos.x = (count + 1) * -.04f;
            stretchSlider.transform.localPosition = pos;
            updateNodeCount();

            output.SetID(data.jackOutAID, copyMode);
            signal.nodes[0].jack.SetID(data.jackOutBID, copyMode);
        }
        else
        {
            count = data.jackCount - 1;
            Vector3 pos = stretchSlider.transform.localPosition;
            pos.x = (count + 1) * -.04f;
            stretchSlider.transform.localPosition = pos;
            updateNodeCount();

            output.SetID(data.jackOutID[0], copyMode);

            for (int i = 1; i < data.jackCount; i++)
            {
                signal.nodes[i - 1].jack.SetID(data.jackOutID[i], copyMode);
            }

        }

        setFlow();

    }
}


public class MultipleData : InstrumentData
{
    public int jackOutAID;
    public int jackOutBID;
    public int jackCount;
    public int[] jackOutID;
    public int jackInID;
}
