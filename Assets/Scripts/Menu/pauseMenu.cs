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
using UnityEngine.SceneManagement;
using System.IO;
using System;
using Mirror;

using System.Linq;

public class pauseMenu : MonoBehaviour
{
    public GameObject savePanel, wireSettingsPanel, settingsLabel;

    public pauseMenuItem newItem, loadItem, saveItem;
    public Material disabledMaterial;

    public enum itemType
    {
        main,
        newItem,
        exitItem,
        saveItem,
        loadItem,
        confirmItem,
        cancelItem,
        wireSettingsItem,
        tooltipItem,
        exampleItem,
        gazeItem,
        events
    };
    // note for adding more menu items: add to the end of itemType enum, duplicate an existing gameobject in itemRoot, set type in pauseMenuItem, add your code to itemSelect switch

    public GameObject[] items;
    bool active = true;

    public GameObject menuObject;

    List<pauseMenuItem> menuItems;

    
    void Awake()
    {
        menuItems = new List<pauseMenuItem>();
        for (int i = 0; i < items.Length; i++)
        {
            pauseMenuItem temp = items[i].GetComponent<pauseMenuItem>();
            if (temp != null) menuItems.Add(temp);
        }

        menuObject.SetActive(false);
        savePanel.SetActive(false);
    }

    Coroutine flashCoroutine;
    void Start()
    {
        saveLoadPanel = GetComponentInChildren<uiPanelComponentInterface>(true);
        toggleMenu(false);
    }

    uiPanelComponentInterface saveLoadPanel;
    void connectionChanged(){
        //Debug.Log("adaptSaveLoadMenu");
        saveLoadPanel.cancel();
        cancelFileMenu();
    }

    bool lastServerActive = false;

    void Update()
    {
        // NetworkClient.OnConnectedEvent and NetworkClient.OnDisconnectedEvent didnt seem to work, so fallback solution is:
        if (NetworkServer.active != lastServerActive) connectionChanged();
        lastServerActive = NetworkServer.active;

        // if in main menu, then run this update continously, since the server client state could change at any time
        // it is a bit brute force and could be improved by a more elegant rewrite
        if (curItem == itemType.main) 
        {
            if (!NetworkServer.active) // Now check if Client
            {
                newItem.gameObject.SetActive(false);
                loadItem.gameObject.SetActive(false);
            }

            if (NetworkServer.active) // or Server
            {
                newItem.gameObject.SetActive(true);
                loadItem.gameObject.SetActive(true);
            }
        }        
    }

    public void endFlash()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
    }

    void mainMenuActive()
    {
        for (int i = 0; i < items.Length - 1; i++) items[i].SetActive(true);
        items[items.Length - 1].SetActive(false);
    }

    void justItemActive(int n)
    {
        for (int i = 0; i < items.Length - 1; i++)
        {
            items[i].SetActive(i == n);
        }
        items[items.Length - 1].SetActive(true);
    }

    public bool GetActive()
    {
        return active;
    }

    void noneActive()
    {
        for (int i = 0; i < items.Length - 1; i++) items[i].SetActive(false);
    }

    public void saveFile(string s)
    {
        if (s == "[new file]")
        {
            s = masterControl.instance.SaveDir + Path.DirectorySeparatorChar + "Saves" + Path.DirectorySeparatorChar +
                string.Format("{0:yyyy-MM-dd_HH-mm-ss}.xml",
                DateTime.Now);
        }

        SaveLoadInterface.instance.Save(s);
        mainMenuActive();
        toggleMenu();
        curItem = itemType.main;
    }

    public void loadFile(string s)
    {
        clearInstruments();
        SaveLoadInterface.instance.Load(s);
        mainMenuActive();
        toggleMenu();
        curItem = itemType.main;
    }

    public void cancelFileMenu()
    {
        itemSelect(itemType.cancelItem);
    }

    itemType curItem = itemType.main;
    public void itemSelect(itemType t, int ID = -1)
    {
        if (t == itemType.exitItem)
        {
            justItemActive(3);
            curItem = itemType.exitItem;
        }
        if (t == itemType.tooltipItem)
        {
            masterControl.instance.toggleTooltips();
        }
        if (t == itemType.exampleItem)
        {
            masterControl.instance.toggleExamples();
        }
        if (t == itemType.newItem)
        {
            justItemActive(0);
            curItem = itemType.newItem;
        }
        if (t == itemType.loadItem)
        {
            noneActive();
            savePanel.SetActive(true);
            savePanel.GetComponent<uiPanelComponentInterface>().refreshFiles(false);

            curItem = itemType.loadItem;
        }
        if (t == itemType.saveItem)
        {
            noneActive();
            savePanel.SetActive(true);
            savePanel.GetComponent<uiPanelComponentInterface>().refreshFiles(true);

            curItem = itemType.saveItem;
        }
        if (t == itemType.wireSettingsItem)
        {
            noneActive();
            wireSettingsPanel.SetActive(true);
            curItem = itemType.wireSettingsItem;
        }
        if (t == itemType.cancelItem)
        {
            mainMenuActive();
            curItem = itemType.main;
        }
        if (t == itemType.confirmItem)
        {
            if (curItem == itemType.exitItem)
            {

                // avoid audio stuttering as Unity tries to shutdown
                AudioSource[] allAudioSources = FindObjectsOfType(typeof(AudioSource)) as AudioSource[];
                foreach (AudioSource audioS in allAudioSources)
                {
                    audioS.Stop();
                }

                Application.Quit();
                toggleMenu();
            }
            else if (curItem == itemType.newItem)
            {
                metronome m = FindObjectOfType<metronome>();
                if (m != null) m.Reset();

                toggleMenu();

                masterControl.instance.currentScene = "";
                clearInstruments();
            }
        }
        if (t == itemType.gazeItem)
        {
            gazedObjectTracker tracker = gazedObjectTracker.Instance;
            if (tracker != null)
            {
                tracker.toggleGaze();
            }
        }
        return;
    }

    public void clearInstruments()
    {
        GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("instrument");
        for (int i = 0; i < gameObjects.Length; i++)
        {
            if (gameObjects[i].name == "Tutorials(Clone)") continue; // Tutorials stays persistent
            Destroy(gameObjects[i]);
        }

        if (masterControl.instance.examplesOn) masterControl.instance.toggleExamples();
    }

    public void toggleMenu(bool animated = true)
    {
        active = !active;
        settingsLabel.SetActive(!active);
        if (!active)
        {

            savePanel.GetComponent<uiPanelComponentInterface>().cancel();
            wireSettingsPanel.SetActive(false);
            mainMenuActive();
            curItem = itemType.main;
        }

        if (_menuAnimation != null) StopCoroutine(_menuAnimation);

        if (animated) _menuAnimation = StartCoroutine(menuAnimation(active));
        else menuObject.SetActive(active);
    }

    void OnDisable()
    {
        if (_menuAnimation != null) StopCoroutine(_menuAnimation);
        menuObject.SetActive(active);
    }

    Coroutine _menuAnimation;
    IEnumerator menuAnimation(bool on)
    {
        if (on) menuObject.SetActive(on);

        if (on)
        {
            List<int> remaining = new List<int>();
            for (int i = 0; i < menuItems.Count; i++)
            {
                remaining.Add(i);
                menuItems[i].transform.localScale = Vector3.zero;
            }

            float timer = 0;
            while (timer < 1)
            {
                timer = Mathf.Clamp01(timer + Time.deltaTime * 8);
                for (int i = 0; i < menuItems.Count; i++)
                {
                    if (remaining.Contains(i))
                    {
                        if (timer * .3 > Vector3.Distance(menuObject.transform.position, menuItems[i].transform.position))
                        {
                            menuItems[i].Appear(on);
                            remaining.Remove(i);
                        }
                    }
                }
                yield return null;
            }

        }
        else
        {
            for (int i = 0; i < menuItems.Count; i++)
            {
                menuItems[i].Appear(on);
            }
            yield return new WaitForSeconds(1);
        }

        yield return null;
        if (!on) menuObject.SetActive(on);
    }
}