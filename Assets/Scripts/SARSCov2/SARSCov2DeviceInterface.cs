// Copyright 2017-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
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

public class SARSCov2DeviceInterface : deviceInterface
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
    SARSCov2Data data = new SARSCov2Data();
    data.deviceType = menuItem.deviceType.SARSCov2;
    GetTransformData(data);
    
    return data;
  }

  public override void Load(InstrumentData d)
  {
    SARSCov2Data data = d as SARSCov2Data;

    transform.localPosition = data.position;
    transform.localRotation = data.rotation;
    transform.localScale = data.scale;

  }
}

public class SARSCov2Data : InstrumentData
{

}