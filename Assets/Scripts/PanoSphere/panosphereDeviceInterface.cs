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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class panosphereDeviceInterface : deviceInterface
{
    public Transform texSphere;
    public TextMesh label;
    Texture2D tex;
    button showButton;
    public Renderer flat;
    string filename;

    public bool sphereActive = false;

    public override void Awake()
    {
        base.Awake();
        showButton = GetComponentInChildren<button>();
        tex = new Texture2D(4, 4, TextureFormat.DXT1, false);

        texSphere.parent = null;
        texSphere.position = Vector3.zero;
        texSphere.rotation = Quaternion.identity;
    }

    public void toggleActive(bool on)
    {
        if (on == sphereActive) return;
        showButton.phantomHit(on);

        sphereActive = on;
        texSphere.gameObject.SetActive(on);
    }

    public override void hit(bool on, int ID = -1)
    {
        toggleActive(on);
        imageLoad.instance.togglePano(this, on);
    }

    void OnDestroy()
    {
        if (sphereActive) imageLoad.instance.togglePano(this, false);
        imageLoad.instance.removePano(this);
        Destroy(texSphere.gameObject);
    }

    public void loadImage(string path)
    {
        if (!File.Exists(path))
        {
            Debug.Log("PATH FAILED: " + path);
            Destroy(gameObject);
            return;
        }

        filename = path;
        label.text = Path.GetFileNameWithoutExtension(path);
        StartCoroutine(loadImageRoutine(path));
    }

    IEnumerator loadImageRoutine(string path)
    {
        path = "file:///" + path;
        WWW www = new WWW(path);
        yield return www;
        www.LoadImageIntoTexture(tex);
        flat.material.mainTexture = tex;
    }

    public override InstrumentData GetData()
    {
        PanoData data = new PanoData();
        data.deviceType = DeviceType.Pano;
        GetTransformData(data);
        data.filename = filename;
        data.active = sphereActive;
        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        PanoData data = d as PanoData;
        base.Load(data, copyMode);
        imageLoad.instance.addPano(this);
        loadImage(data.filename);
        showButton.startToggled = data.active;
    }
}

public class PanoData : InstrumentData
{
    public string filename;
    public bool active;
}
