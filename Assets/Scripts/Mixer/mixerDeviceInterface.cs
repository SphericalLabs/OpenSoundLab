// This file is part of OpenSoundLab, which is based on SoundStage VR.
//
// Copyright © 2020-2023 GPLv3 Ludwig Zeller OpenSoundLab
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

public class mixerDeviceInterface : deviceInterface
{

    mixer signal;
    public GameObject mixerPrefab;
    public Transform stretchSlider, speaker, output, lengthSlider;

    public List<fader> faderList = new List<fader>();
    fader[] allFaders;

    int count = 0;
    int maxCount;

    float faderLength = 0;
    float prevFaderLength = 0;

    NetworkSliders networkSliders;
    NetworkJacks networkJacks;

    public override void Awake()
    {
        base.Awake();
        signal = GetComponent<mixer>();

        count = calculateCount(stretchSlider.localPosition.x);

        networkJacks = GetComponent<NetworkJacks>();
        networkSliders = GetComponent<NetworkSliders>();

        createInactiveFaders();
        updateMixerCount();
    }

    int calculateCount(float xVal)
    {
        return Mathf.FloorToInt((xVal + .075f) / -.04f) + 1;
    }

    void createInactiveFaders()
    {

        maxCount = calculateCount(stretchSlider.GetComponent<xHandle>().xBounds.x);
        allFaders = new fader[maxCount];
        List<omniJack> newJacks = new List<omniJack>();
        List<slider> newSliders = new List<slider>();

        for (int i = 0; i < maxCount; i++)
        {
            signalGenerator s = (Instantiate(mixerPrefab, transform, false) as GameObject).GetComponent<signalGenerator>();
            
            s.transform.localPosition = new Vector3(-.03f - .04f * i, 0, 0);
            
            allFaders[i] = s.GetComponent<fader>();

            updateFader(allFaders[i]);

            s.gameObject.SetActive(false);
            

            foreach (omniJack j in s.GetComponentsInChildren<omniJack>())
            { // add the two jacks per fader to a temp list
                newJacks.Add(j);
            }
            newSliders.Add(s.GetComponentInChildren<slider>());

        }

        // // this adds the manually added omniJacks and sliders to the end of the arrays
        networkJacks.omniJacks = Utils.AddElementsToArray(newJacks.ToArray(), networkJacks.omniJacks);
        networkSliders.sliders = Utils.AddElementsToArray(newSliders.ToArray(), networkSliders.sliders);
    }

    void updateFader(fader f){
        float fL = 1 + faderLength * 4f;
        f.updateFaderLength(fL);
        Vector3 pos = f.transform.localPosition;
        pos.z = -.12f * fL + .12f;
        f.transform.localPosition = pos;
    }

    void updateMixerCount()
    {

        int cur = signal.incomingSignals.Count;

        if (cur == count) return;

        if (count > cur) // increase
        { 

            for (int i = cur; i < count; i++)
            {

                if(!faderList.Contains(allFaders[i])) // still needed?
                { 
                    faderList.Add(allFaders[i]);
                }

                if (!signal.incomingSignals.Contains(allFaders[i]))
                {
                    signal.incomingSignals.Add(allFaders[i]);
                }

                updateFader(allFaders[i]);

                allFaders[i].gameObject.SetActive(true);

            }
        }
        else // count < cur, decrease
        {
            for (int i = cur - 1; i > count - 1; i--)
            {

                signalGenerator s = signal.incomingSignals.Last();
                faderList.RemoveAt(faderList.Count - 1);
                signal.incomingSignals.RemoveAt(signal.incomingSignals.Count - 1);
                s.gameObject.SetActive(false);

                // each fader has two omniJacks and thus this cleanup runs twice
                networkJacks.omniJacks[2 * i].endConnection(true, true);
                networkJacks.omniJacks[2 * i + 1].endConnection(true, true);

            }
        }
    }


    void Update()
    {
        float xVal = stretchSlider.localPosition.x;
        speaker.localPosition = new Vector3(xVal - .0125f, 0, .11f);
        output.localPosition = new Vector3(xVal - .0125f, 0, .14f);

        count = Mathf.FloorToInt((xVal + .075f) / -.04f) + 1;
        if (count != signal.incomingSignals.Count) updateMixerCount();


        faderLength = lengthSlider.localPosition.x;
        if (faderLength != prevFaderLength)
        {
            prevFaderLength = faderLength;
            float fL = 1 + faderLength * 4f;
            for (int i = 0; i < faderList.Count; i++)
            {
                faderList[i].updateFaderLength(fL);
                Vector3 pos = faderList[i].transform.localPosition;
                pos.z = -.12f * fL + .12f;
                faderList[i].transform.localPosition = pos;
            }
        }
    }

    public override InstrumentData GetData()
    {
        MixerData data = new MixerData();
        data.deviceType = DeviceType.Mixer;
        data.jackOutID = output.GetInstanceID();

        GetTransformData(data);

        data.jackInID = new int[count * 2];
        data.sliders = new float[count];

        for (int i = 0; i < count; i++)
        {
            data.sliders[i] = signal.incomingSignals[i].GetComponent<fader>().fadeSlider.percent;
            data.jackInID[2 * i] = signal.incomingSignals[i].GetComponent<fader>().inputA.transform.GetInstanceID();
            data.jackInID[2 * i + 1] = signal.incomingSignals[i].GetComponent<fader>().inputB.transform.GetInstanceID();
        }

        data.height = lengthSlider.localPosition.x;

        return data;
    }

    public override void Load(InstrumentData d)
    {
        MixerData data = d as MixerData;
        base.Load(data);
        output.GetComponent<omniJack>().ID = data.jackOutID;

        count = data.sliders.Length;
        Vector3 pos = stretchSlider.localPosition;
        pos.x = (count - 1) * -.04f - .076f;
        stretchSlider.localPosition = pos;

        updateMixerCount();

        pos = lengthSlider.localPosition;
        pos.x = data.height;
        lengthSlider.localPosition = pos;

        for (int i = 0; i < count; i++)
        {
            signal.incomingSignals[i].GetComponent<fader>().fadeSlider.setPercent(data.sliders[i]);
            signal.incomingSignals[i].GetComponent<fader>().inputA.ID = data.jackInID[2 * i];
            signal.incomingSignals[i].GetComponent<fader>().inputB.ID = data.jackInID[2 * i + 1];
        }
    }
}

public class MixerData : InstrumentData
{
    public int[] jackInID;
    public float[] sliders;
    public int jackOutID;
    public float height;
}
