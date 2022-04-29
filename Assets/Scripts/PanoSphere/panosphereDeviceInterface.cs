// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of SoundStage Lab.
//
// SoundStage Lab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// SoundStage Lab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SoundStage Lab.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class panosphereDeviceInterface : deviceInterface {
  public Transform texSphere;
  public TextMesh label;
  Texture2D tex;
  button showButton;
  public Renderer flat;
  string filename;

  public bool sphereActive = false;

  public override void Awake() {
    base.Awake();
    showButton = GetComponentInChildren<button>();
    tex = new Texture2D(4, 4, TextureFormat.DXT1, false);

    texSphere.parent = null;
    texSphere.position = Vector3.zero;
    texSphere.rotation = Quaternion.identity;
  }

  public void toggleActive(bool on) {
    if (on == sphereActive) return;
    showButton.phantomHit(on);

    sphereActive = on;
    texSphere.gameObject.SetActive(on);
  }

  public override void hit(bool on, int ID = -1) {
    toggleActive(on);
    imageLoad.instance.togglePano(this, on);
  }

  void OnDestroy() {
    if (sphereActive) imageLoad.instance.togglePano(this, false);
    imageLoad.instance.removePano(this);
    Destroy(texSphere.gameObject);
  }

  public void loadImage(string path) {
    if (!File.Exists(path)) {
      Debug.Log("PATH FAILED: " + path);
      Destroy(gameObject);
      return;
    }

    filename = path;
    label.text = Path.GetFileNameWithoutExtension(path);
    StartCoroutine(loadImageRoutine(path));
  }

  IEnumerator loadImageRoutine(string path) {
    path = "file:///" + path;
    WWW www = new WWW(path);
    yield return www;
    www.LoadImageIntoTexture(tex);
    flat.material.mainTexture = tex;
  }

  public override InstrumentData GetData() {
    PanoData data = new PanoData();
    data.deviceType = menuItem.deviceType.Pano;
    GetTransformData(data);
    data.filename = filename;
    data.active = sphereActive;
    return data;
  }

  public override void Load(InstrumentData d) {
    PanoData data = d as PanoData;
    base.Load(data);
    imageLoad.instance.addPano(this);
    loadImage(data.filename);
    showButton.startToggled = data.active;
  }
}

public class PanoData : InstrumentData {
  public string filename;
  public bool active;
}