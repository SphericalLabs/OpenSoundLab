using System;
using UnityEngine;
using TMPro;

namespace Mirror
{
    /// </summary>
    public class UiNetworkPingText : MonoBehaviour
    {

        public TMP_Text rttText;
        public TMP_Text qText;

        void Update()
        {
            // only while client is active
            if (!NetworkClient.active) return;

            rttText.text = $"RTT: {Math.Round(NetworkTime.rtt * 1000)}ms";

            qText.text = $"Q: {new string('-', (int)NetworkClient.connectionQuality)}";
        }
    }
}
