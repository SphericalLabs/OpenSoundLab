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

public class timelineScrollInterface : componentInterface {

  timelineComponentInterface _deviceInterface;

  public xHandle window, edgeIn, edgeOut, quadEdge;
  public Transform timelineQuad;

  Mesh windowMesh;
  BoxCollider windowCollider, edgeInCollider, edgeOutCollider;

  float startduration = 32;

  float windowHeight = .02f;
  float unitScale = .01f;

  public Vector2 curIO;
  float timelineBound;
  Vector3 windowPos;

  void Awake() {
    _deviceInterface = GetComponentInParent<timelineComponentInterface>();

    windowMesh = new Mesh();
    window.GetComponent<MeshFilter>().mesh = windowMesh;
    window.GetComponent<Renderer>().material.SetColor("_TintColor", Color.white);
    windowCollider = window.GetComponent<BoxCollider>();
    edgeInCollider = edgeIn.GetComponent<BoxCollider>();
    edgeOutCollider = edgeOut.GetComponent<BoxCollider>();
  }

  bool initialized = false;
  public void Activate() {
    curIO = _deviceInterface._gridParams.range * unitScale;

    timelineBound = startduration * unitScale;
    quadEdge.xBounds = new Vector2(Mathf.NegativeInfinity, -unitScale * 4);
    quadEdge.transform.localPosition = new Vector3(-timelineBound, 0, 0);

    UpdateQuad();
    UpdateWindow();
    windowPos = window.transform.localPosition;
    initialized = true;
  }

  void UpdateQuad() {
    timelineBound = -quadEdge.transform.localPosition.x;
    timelineQuad.localScale = new Vector3(timelineBound, unitScale / 2f, 1);
    timelineQuad.localPosition = new Vector3(-timelineBound / 2f, 0, 0);

    updateClamps();
  }

  void updateClamps() {
    if (curIO.y > timelineBound) curIO.y = timelineBound;
    if (curIO.x > timelineBound) curIO.x = timelineBound - 4 * unitScale;

    float width = curIO.y - curIO.x;

    window.xBounds.y = 0 - width / 2f;
    window.xBounds.x = -timelineBound + width / 2f;

    float offset = _deviceInterface._gridParams.width / .2f * unitScale;

    edgeIn.xBounds.y = 0;
    edgeIn.xBounds.x = edgeOut.transform.localPosition.x + offset;

    edgeOut.xBounds.y = edgeIn.transform.localPosition.x - offset;
    edgeOut.xBounds.x = -timelineBound;
  }

  public void handleUpdate(float x) {
    _deviceInterface._gridParams.width = -x;

    curIO.y = curIO.x + _deviceInterface._gridParams.width / _deviceInterface._gridParams.unitSize * unitScale;
    UpdateWindow();
  }

  float curWindowX;

  void Update() {
    if (!initialized) return;
    bool updateRequested = false;

    edgeInCollider.enabled = (window.curState != manipObject.manipState.grabbed);
    edgeOutCollider.enabled = (window.curState != manipObject.manipState.grabbed);
    windowCollider.enabled = (edgeIn.curState != manipObject.manipState.grabbed && edgeOut.curState != manipObject.manipState.grabbed);

    if (window.transform.localPosition.x != -curWindowX) {
      float dif = window.transform.localPosition.x + curWindowX;
      curIO.x -= dif;
      curIO.y -= dif;

      Vector3 pos;
      pos = edgeIn.transform.localPosition;
      pos.x = -curIO.x;
      edgeIn.transform.localPosition = pos;

      pos.x = -curIO.y;
      edgeOut.transform.localPosition = pos;

      updateRequested = true;
    }

    if (edgeIn.transform.localPosition.x != -curIO.x) {
      curIO.x = -edgeIn.transform.localPosition.x;
      updateRequested = true;
    }

    if (edgeOut.transform.localPosition.x != -curIO.y) {
      curIO.y = -edgeOut.transform.localPosition.x;
      updateRequested = true;
    }

    if (timelineBound != -quadEdge.transform.localPosition.x) {
      UpdateQuad();
      updateRequested = true;
    }

    if (updateRequested) UpdateWindow();
  }

  void UpdateWindow() {
    windowMesh.Clear();

    float width = curIO.y - curIO.x;
    float pos = curIO.x + width / 2;

    windowCollider.size = new Vector3(width - .01f, windowHeight, .02f);

    Vector3[] points = new Vector3[]
    {
            new Vector3(-width/2f,-windowHeight/2,0),
            new Vector3(-width/2f,windowHeight/2,0),
            new Vector3(width/2f,windowHeight/2,0),
            new Vector3(width/2f,-windowHeight/2,0)
    };

    int[] lines = new int[] { 0, 1, 1, 2, 2, 3, 3, 0 };

    windowMesh.vertices = points;
    windowMesh.SetIndices(lines, MeshTopology.Lines, 0);

    edgeIn.transform.localPosition = new Vector3(-curIO.x, 0, 0);
    edgeOut.transform.localPosition = new Vector3(-curIO.y, 0, 0);
    window.transform.localPosition = new Vector3(-pos, 0, 0);

    updateClamps();

    curWindowX = pos;

    _deviceInterface.updateGrid(curIO / unitScale, _deviceInterface._gridParams.width);
  }
}
