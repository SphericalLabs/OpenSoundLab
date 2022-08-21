// Copyright © 2017, 2020-2022 Logan Olson, Google LLC, James Surine, Ludwig Zeller, Hannes Barfuss
//
// This file is part of OpenSoundLab.
//
// OpenSoundLab is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenSoundLab is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenSoundLab.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoPlayerUI : manipObject {

  VideoPlayerDeviceInterface _interface;
  public GameObject frameQuad, controlQuad, playQuad, pauseQuad;
  public Renderer[] controlRends;
  Color defaultColor = new Color32(0x3A, 0x61, 0xD0, 0xFF);

  public override void Awake() {
    base.Awake();
    _interface = GetComponentInParent<VideoPlayerDeviceInterface>();
    generateMeshes();
  }

  void generateMeshes() {
    generateFrameMesh();
    generatePlayMesh();
    generatePauseMesh();

    for (int i = 0; i < controlRends.Length; i++) {
      controlRends[i].material.SetFloat("_EmissionGain", .3f);
      controlRends[i].material.SetColor("_TintColor", defaultColor);
    }
  }

  void generateFrameMesh() {
    Mesh mesh = new Mesh();
    frameQuad.GetComponent<MeshFilter>().mesh = mesh;

    float width = .48f;
    float height = .27f;
    Vector3[] points = new Vector3[]
    {
            new Vector3(-width/2f,-height/2,0),
            new Vector3(-width/2f,height/2,0),
            new Vector3(width/2f,height/2,0),
            new Vector3(width/2f,-height/2,0)
    };

    int[] lines = new int[] { 0, 1, 1, 2, 2, 3, 3, 0 };

    mesh.vertices = points;
    mesh.SetIndices(lines, MeshTopology.Lines, 0);
  }

  void generatePlayMesh() {
    Mesh mesh = new Mesh();
    playQuad.GetComponent<MeshFilter>().mesh = mesh;


    float width = .16f;
    float height = .12f;
    Vector3[] points = new Vector3[]
    {
            new Vector3(-width/2f,-height/2,0),
            new Vector3(width/2f,0),
            new Vector3(-width/2f,height/2,0)
    };

    int[] lines = new int[] { 0, 1, 1, 2, 2, 0 };
    mesh.vertices = points;
    mesh.SetIndices(lines, MeshTopology.Lines, 0);
  }


  void generatePauseMesh() {
    Mesh mesh = new Mesh();
    pauseQuad.GetComponent<MeshFilter>().mesh = mesh;

    float width = .035f;
    float height = .2f;
    Vector3[] points = new Vector3[]
    {
            new Vector3(-width*1.5f,height*.5f,0),
            new Vector3(-width*.5f,height*.5f,0),
            new Vector3(-width*.5f,-height*.5f,0),
            new Vector3(-width*1.5f,-height*.5f,0),

            new Vector3(width*.5f,height*.5f,0),
            new Vector3(width*1.5f,height*.5f,0),
            new Vector3(width*1.5f,-height*.5f,0),
            new Vector3(width*.5f,-height*.5f,0)
    };

    int[] lines = new int[] { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4 };

    mesh.vertices = points;
    mesh.SetIndices(lines, MeshTopology.Lines, 0);
  }

  void triggerEvent() {
    _interface.togglePlay();
  }

  public void Reset() {
    controlQuad.SetActive(true);
    updateControlQuad();
    controlRends[0].material.SetFloat("_EmissionGain", .3f);
  }

  public void updateControlQuad() {
    playQuad.SetActive(!_interface.playing);
    pauseQuad.SetActive(_interface.playing);
  }

  public override void setState(manipState state) {
    if (curState == state) return;
    curState = state;

    if (curState == manipState.none) {
      if (_interface.playing) controlQuad.SetActive(false);

      controlRends[0].material.SetFloat("_EmissionGain", _interface.playing ? 0f : .3f);
      for (int i = 1; i < controlRends.Length; i++) controlRends[i].material.SetFloat("_EmissionGain", .3f);
    }
    if (curState == manipState.selected) {
      controlQuad.SetActive(true);
      for (int i = 0; i < controlRends.Length; i++) controlRends[i].material.SetFloat("_EmissionGain", .45f);

    }
    if (curState == manipState.grabbed) {
      controlQuad.SetActive(true);
      triggerEvent();
    }

    updateControlQuad();
  }
}
