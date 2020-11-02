// Copyright 2017 Google LLC
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

public class conferenceCallDeviceInterface : deviceInterface
{

  SARSCov2Data data;

  public override void Awake()
  {
    base.Awake();
  }

  void Start()
  {

  }


  void Update()
  {

  }

  public override InstrumentData GetData()
  {
    ConferenceCallData data = new ConferenceCallData();
    data.deviceType = menuItem.deviceType.ConferenceCall;
    GetTransformData(data);
    
    return data;
  }

  public override void Load(InstrumentData d)
  {
    ConferenceCallData data = d as ConferenceCallData;

    transform.localPosition = data.position;
    transform.localRotation = data.rotation;
    transform.localScale = data.scale;

  }
}

public class ConferenceCallData : InstrumentData
{

}