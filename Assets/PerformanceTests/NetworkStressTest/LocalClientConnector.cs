using UnityEngine;
using Mirror;
using System;
using kcp2k;

public class LocalClientConnector : MonoBehaviour
{
    public string ipAddress = "localhost";
    public ushort port = 7777; // Default Mirror port
    public bool autoConnect = false;

    void Start()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        ParseCommandLineArgs();
        Debug.Log("Press c to connect to localhost or host that was set in commandline variables");
        if (autoConnect) ConnectToServer();
#else
        Debug.LogWarning("ClientConnector is only supported on Windows Standalone or Editor.");
        this.enabled = false;
#endif
    }

    public void Update()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (Input.GetKeyDown("c"))
        {
            ConnectToServer();
            Debug.LogError("c pressed"); // error so that you can see it in dev build desktop
        }
#endif
    }

    void ParseCommandLineArgs()
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
        }
    }

    public void ConnectToServer()
    {
        NetworkManager networkManager = NetworkManager.singleton;
        if (Transport.active is TelepathyTransport telepathyTransport)
        {
            telepathyTransport.port = port;
        }
        else if (Transport.active is KcpTransport kcpTransport)
        {
            kcpTransport.port = port;
        }
        else
        {
            Debug.LogError("Unsupported transport type");
            return;
        }
        networkManager.networkAddress = ipAddress;
        networkManager.StartClient();
        Debug.Log($"Connecting to {ipAddress}:{port}");
    }
}