using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace Network
{
    public class LocalNetworkManager : NetworkManager
    {

        public override void OnStartHost()
        {
            base.OnStartHost();

            if (NetworkMenuManager.Instance != null)
            {
                NetworkMenuManager.Instance.ActivateHostUI();
            }
        }

        public override void OnStopHost()
        {
            base.OnStopHost();
            if (NetworkMenuManager.Instance != null)
            {
                NetworkMenuManager.Instance.DeactivateUI();
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (NetworkMenuManager.Instance != null && mode == NetworkManagerMode.ClientOnly)
            {
                NetworkMenuManager.Instance.ActivateClientUI();
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            if (NetworkMenuManager.Instance != null)
            {
                NetworkMenuManager.Instance.DeactivateUI();
            }
        }


        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            if (NetworkMenuManager.Instance != null)
            {
                NetworkMenuManager.Instance.CheckIfClientGetKickedOut();
            }
        }

    }
}

