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
