using UnityEngine;
using Mirror;
using System;
using System.Collections;
using kcp2k;
using UnityEngine.SceneManagement;
using Network;


// this is not working yet, Relay connection does not start when given a join code via command-line
// please note that this file must also be added to a gameobject in the relay scene! that is currently not the case.
public class RelayClientConnector : MonoBehaviour
{
    public string ipAddress = "localhost";
    public ushort port = 7777; // Default Mirror port
    public bool autoConnect = false;
    public string relayJoinCode = "";

    private void Start()
    {

        ParseCommandLineArgs();

        if (relayJoinCode != "")
        {
            if (SceneManager.GetActiveScene().buildIndex == 0)
            {
                SceneManager.LoadScene(1);
            }
            else if (SceneManager.GetActiveScene().buildIndex == 1)
            {
                SetupRelay();
                if (autoConnect) StartCoroutine(DelayedConnectRelay(5));
            }
        }
        else
        {
            SetupLocal();
            if (autoConnect) ConnectLocal();
        }

        //Debug.LogWarning("ClientConnector is only supported on Windows Standalone or Editor.");
        //this.enabled = false;

    }

    private void Update()
    {

        if (Input.GetKeyDown("c"))
        {
            if (relayJoinCode != "")
            {
                DelayedConnectRelay(2);
            }
            else
            {
                ConnectLocal();
            }
        }

    }

    private void ParseCommandLineArgs()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-ip" && i + 1 < args.Length)
            {
                ipAddress = args[i + 1];
            }
            else if (args[i] == "-port" && i + 1 < args.Length)
            {
                if (ushort.TryParse(args[i + 1], out ushort parsedPort))
                {
                    port = parsedPort;
                }
                else
                {
                    Debug.LogError("Invalid port number provided.");
                }
            }
            else if (args[i] == "-autoconnect")
            {
                autoConnect = true;
            }
            else if (args[i] == "-relaycode" && i + 1 < args.Length)
            {
                relayJoinCode = args[i + 1];
            }
        }
    }


    MyNetworkManager relayManager;
    private void SetupRelay()
    {
        relayManager = FindObjectOfType<MyNetworkManager>();
        if (relayManager == null)
        {
            Debug.LogError("MyNetworkManager not found in the scene.");
            return;
        }

        if (!string.IsNullOrEmpty(relayJoinCode))
        {
            relayManager.relayJoinCode = relayJoinCode;
        }
    }

    private IEnumerator DelayedConnectRelay(int sec = 5)
    {

        Debug.Log("Waiting 5 seconds before connecting to Relay...");
        yield return new WaitForSeconds(sec);
        ConnectRelay();
    }

    private void ConnectRelay()
    {
        relayManager.JoinRelayServer();
    }

    NetworkManager localManager;
    public void SetupLocal()
    {

        localManager = NetworkManager.singleton;
        if (Transport.active is TelepathyTransport telepathyTransport)
        {
            telepathyTransport.port = port;
        }
        else if (Transport.active is KcpTransport kcpTransport)
        {
            kcpTransport.Port = port;
        }
        else
        {
            Debug.LogError("Unsupported transport type");
            return;
        }

        relayManager.networkAddress = ipAddress;
    }

    public void ConnectLocal()
    {

        relayManager.StartClient();
        Debug.Log($"Connecting to {ipAddress}:{port}");

    }
}