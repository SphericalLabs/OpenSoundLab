// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright � 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
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

    public bool isSplitter = true;

    public Material mixerMaterial;
    public Material splitterMaterial;


    public override void Awake()
    {
        signal = GetComponent<multipleSignalGenerator>();
        //flowSwitch = GetComponentInChildren<basicSwitch>();
        signal.nodes = new List<multipleNodeSignalGenerator>();

        symbolA.sharedMaterial = mixerMaterial;
        symbolB.sharedMaterial = mixerMaterial;

        CreateInactiveNodes();
        stretchSlider.onPosSetEvent.AddListener(CalculateSplitterCount);

        float xVal = stretchSlider.transform.localPosition.x;

        count = Mathf.FloorToInt((xVal - .02f) / -.04f) - 1;
        updateSplitterCount();
        setFlow(isSplitter, true); // reads in default value from the prefab to enable MultiMix and MultiSplit
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

            networkJacks = GetComponent<NetworkJacks>();
            foreach (var jack in networkJacks.omniJacks)
            {
                newJacks.Add(jack);
            }

            networkJacks.omniJacks = newJacks.ToArray();
        }
    }

    public void CalculateSplitterCount()
    {
        float xVal = stretchSlider.transform.localPosition.x;

        count = Mathf.FloorToInt((xVal - .02f) / -.04f) - 1;
        updateSplitterCount();
    }

    void updateSplitterCount(bool updateByGrab = false)
    {
        /*
        int cur = signal.nodes.Count;
        if (count > cur)
        {
            for (int i = 0; i < count - cur; i++)
            {
                multipleNodeSignalGenerator s = (Instantiate(splitterNodePrefab, transform, false) as GameObject).GetComponent<multipleNodeSignalGenerator>();
                s.setup(signal, isSplitter);
                signal.nodes.Add(s);
                s.transform.localPosition = new Vector3(-.04f * signal.nodes.Count, 0, 0);
            }
        }
        else
        {
            for (int i = 0; i < cur - count; i++)
            {
                signalGenerator s = signal.nodes.Last();
                signal.nodes.RemoveAt(signal.nodes.Count - 1);
                Destroy(s.gameObject);
            }
        }*/
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
                    networkJacks.omniJacks[i].endConnection(true);

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

    void setFlow(bool on, bool init = false)
    {
        if (isSplitter == on && !init) return;
        isSplitter = on;

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
    }

    
    void Update()
    {
        linkJackandGenerator();    

        if (stretchSlider.curState == manipObject.manipState.grabbed)
        {
            float xVal = stretchSlider.transform.localPosition.x;
            count = Mathf.FloorToInt((xVal - .02f) / -.04f) - 1;
            if (count != signal.nodes.Count) updateSplitterCount(true);
        }
    }

    void linkJackandGenerator(){
        if (isSplitter)
        {
            if (signal.incoming != input.signal) signal.incoming = input.signal;
        }
        else if (signal.incoming != output.signal) signal.incoming = output.signal;
    }

    public override InstrumentData GetData()
    {
        MultipleData data = new MultipleData();
        data.deviceType = DeviceType.Multiple;
        GetTransformData(data);

        data.isSplitter = isSplitter;
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

    public override void Load(InstrumentData d)
    {
        MultipleData data = d as MultipleData;
        base.Load(data);

        input.ID = data.jackInID;

        setFlow(data.isSplitter);
        //flowSwitch.setSwitch(isSplitter);

        if (data.jackCount < 2)
        {
            count = 1;
            Vector3 pos = stretchSlider.transform.localPosition;
            pos.x = (count + 1) * -.04f;
            stretchSlider.transform.localPosition = pos;
            updateSplitterCount();

            output.ID = data.jackOutAID;
            signal.nodes[0].jack.ID = data.jackOutBID;
        }
        else
        {
            count = data.jackCount - 1;
            Vector3 pos = stretchSlider.transform.localPosition;
            pos.x = (count + 1) * -.04f;
            stretchSlider.transform.localPosition = pos;
            updateSplitterCount();

            output.ID = data.jackOutID[0];

            for (int i = 1; i < data.jackCount; i++)
            {
                signal.nodes[i - 1].jack.ID = data.jackOutID[i];
            }

        }
    }
}


// MultiMix and MultiSplit are both prefabs that do not have their own MultiMixDeviceInterface resp. MultiSplitDeviceInterface. They both use MultipleDevice interface and are serialized (save, copy) as MultipleDeviceInterface and thus loaded resp. copied from the Multiple prefab. It's a bit hacky, but the idea was to have both modes separately in the menu and not to have to set the mode each time.
public class MultipleData : InstrumentData
{
    public bool isSplitter;
    public int jackOutAID;
    public int jackOutBID;
    public int jackCount;
    public int[] jackOutID;
    public int jackInID;
}