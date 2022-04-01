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
using UnityEngine.UI;

public class AdvanceSubmenu : MonoBehaviour {
  public bool expanded = false;
  public Image expandSprite;
  public GameObject submenu;

  public RectTransform[] lowerButtons;
  public int lowerAmount;
  public void toggleAdvanced() {
    expanded = !expanded;
    expandSprite.gameObject.SetActive(!expanded);
    submenu.SetActive(expanded);

    foreach (RectTransform t in lowerButtons) {
      Vector2 v = t.anchoredPosition;
      v.y += expanded ? -lowerAmount : lowerAmount;
      t.anchoredPosition = v;
    }
  }
}
