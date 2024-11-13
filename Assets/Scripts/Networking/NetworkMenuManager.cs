using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Mirror.Discovery;
using Network;
using TMPro;
using UnityEngine.UI;
using System.Net;
using UnityEngine.SceneManagement;

public class NetworkMenuManager : MonoBehaviour
{
    public static NetworkMenuManager Instance;

    [SerializeField] private bool createHostOnStart = true;
    [SerializeField] private bool isRelayScene = true;
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private OSLNetworkDiscovery networkDiscovery;
    private bool clientGotStopped = false;

    public static string relayCode = "";

    public string userName;

    readonly Dictionary<long, OSLServerResonse> discoveredServers = new Dictionary<long, OSLServerResonse>();

    [Header("UI")]
    [SerializeField] private TMP_InputField userNameInputField;
    [Header("Host Menu")]
    [SerializeField] private Transform hostMenuParent;
    [SerializeField] private Transform discoveryButtonParent;
    [SerializeField] private GameObject discoveryButtonPrefab;
    [SerializeField] private TMP_InputField relayCodeInputField;
    [SerializeField] private GameObject connectToRelayButton;
    [SerializeField] private TMP_Text ipAdressText;
    [SerializeField] private Toggle discoverableToggle;

    [Header("Relay Host Menu")]
    [SerializeField] private Transform relayHostMenuParent;
    [SerializeField] private TMP_Text relayJoinCodeText;
    [SerializeField] private GameObject backButtonObject;

    [Header("Client Menu")]
    [SerializeField] private Transform clientMenuParent;

    public string RelayJoinCode { get => relayCodeInputField.text; }

    public VRNetworkPlayer localPlayer;

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    IEnumerator Start()
    {
        LoadUserName();
        if (relayCodeInputField != null && relayCode.Length > 0)
        {
            relayCodeInputField.SetTextWithoutNotify(relayCode);
        }
        if (createHostOnStart)
        {
            yield return new WaitForSeconds(0.5f);
            if (isRelayScene)
            {
                if (networkManager is OslRelayNetworkManager)
                {
                    ((OslRelayNetworkManager)networkManager).onFailedStartHostEvent.AddListener(GoBackToLocalScene);
                    ((OslRelayNetworkManager)networkManager).onFailedConnectToServerEvent.AddListener(GoBackToLocalScene);
                    if (relayCode.Length > 0)
                    {
                        StartCoroutine(JoinRelayHostWaitTime(relayCode));
                    }
                    else
                    {
                        StartCoroutine(StartRelayHostWaitTime());
                    }
                }
                else
                {
                    SceneManager.LoadSceneAsync((int)masterControl.Scenes.Local);
                }
            }
            else
            {
                discoverableToggle.SetIsOnWithoutNotify(networkDiscovery.isDiscoverable);
                networkManager.StartHost();
                ActivateHostUI();
                yield return new WaitForSeconds(0.5f);
                networkDiscovery.AdvertiseServer();
            }
        }

        if (backButtonObject != null)
        {
            yield return new WaitForSeconds(6f);
            if (!networkManager.isNetworkActive)
            {
                backButtonObject.SetActive(true);
            }
        }
    }

    public void RestartHost()
    {
        networkManager.StopHost();
        StartCoroutine(RestartHostWaitTime());
    }

    private IEnumerator RestartHostWaitTime()
    {
        while (networkManager.isNetworkActive)
        {
            yield return new WaitForEndOfFrame();
        }
        yield return new WaitForEndOfFrame();
        clientGotStopped = false;
        if (networkManager is OslRelayNetworkManager)
        {
            ((OslRelayNetworkManager)networkManager).StartStandardHost();
        }
        else
        {
            networkManager.StartHost();
        }
        yield return new WaitForSeconds(0.5f);
        networkDiscovery.AdvertiseServer();
    }


    public void StopClient()
    {
        networkManager.StopClient();
        StartCoroutine(RestartHostWaitTime());
    }

    public void CheckIfClientGetKickedOut()
    {
        if (isRelayScene)
        {
            GoBackToLocalScene();
        }
        else
        {
            clientGotStopped = true;
            Debug.Log("Player got kicked out true");
            StartCoroutine(RestartHostWhenKickedOutWaitTime());
        }
    }

    private IEnumerator RestartHostWhenKickedOutWaitTime()
    {
        yield return new WaitForSeconds(2f);
        if (clientGotStopped && !networkManager.isNetworkActive)
        {
            StartCoroutine(RestartHostWaitTime());

            Debug.Log("Player got kicked out, restart");
        }
    }


    #region localNetwork

    public void DiscorverServer()
    {
        discoveredServers.Clear();
        networkDiscovery.StartDiscovery();
    }


    public void OnDiscoveredServer(OSLServerResonse info)
    {
        Debug.Log("On discover servers");
        discoveredServers[info.serverId] = info;

        DeleteAllServerDiscoveryButtons();

        //create a UI button if a server get discoverd
        foreach (OSLServerResonse newinfo in discoveredServers.Values)
        {
            if (newinfo.version == Application.version)
            {
                CreateServerDiscoveryButton(newinfo);
            }
            else
            {
                Debug.Log($"Host {newinfo.userName} and ip {info.EndPoint.Address} has a different verion number {newinfo.version}, so you can't connect to it");
            }
        }
    }

    public void FindServer()
    {
        StopDiscovery();
        StartCoroutine(FindServerWaitTime());
    }

    IEnumerator FindServerWaitTime()
    {
        yield return new WaitForSeconds(1f);
        discoveredServers.Clear();
        networkDiscovery.StartDiscovery();
    }

    public void StopDiscovery()
    {
        discoveredServers.Clear();
        networkDiscovery.StopDiscovery();
        DeleteAllServerDiscoveryButtons();
    }

    public void JoinLocalHost(OSLServerResonse info)
    {
        networkManager.StopHost();
        StartCoroutine(JoinLocalHostWaitTime(info));
    }

    private IEnumerator JoinLocalHostWaitTime(OSLServerResonse info)
    {
        while (networkManager.isNetworkActive)
        {
            yield return new WaitForEndOfFrame();
        }
        yield return new WaitForSeconds(1f);
        clientGotStopped = false;
        networkDiscovery.StopDiscovery();
        networkManager.StartClient(info.uri);
    }

    public void OnChangeRelayCodeValue(string value)
    {
        connectToRelayButton.SetActive(value.Length >= 6);
    }

    #endregion

    #region Relay

    public void StartRelayHost()
    {
        networkManager.StopHost();
        relayCode = "";
        StartCoroutine(LoadRelaySceneWaitingTime());
    }


    private IEnumerator LoadRelaySceneWaitingTime()
    {
        while (networkManager.isNetworkActive)
        {
            yield return new WaitForEndOfFrame();
        }
        yield return new WaitForSeconds(0.1f);
        SceneManager.LoadSceneAsync((int)masterControl.Scenes.Relay);
    }

    private IEnumerator StartRelayHostWaitTime()
    {
        if (networkManager is OslRelayNetworkManager)
        {
            var myNetworkManager = ((OslRelayNetworkManager)networkManager);
            if (!OslRelayNetworkManager.isLoggedIn)
            {
                //check if loggin works
                myNetworkManager.UnityLogin();
                while (!OslRelayNetworkManager.isLoggedIn)
                {
                    yield return new WaitForEndOfFrame();
                }
                //todo if not start normal host and show error
            }

            while (myNetworkManager.isNetworkActive)
            {
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForEndOfFrame();
            clientGotStopped = false;
            int maxPlayers = myNetworkManager.maxConnections;
            myNetworkManager.StartRelayHost(maxPlayers);
            //todo if not working, start normal host and show error
        }
    }

    public void JoinRelayHost()
    {
        if (RelayJoinCode.Length > 0)
        {
            networkManager.StopHost();

            relayCode = RelayJoinCode;
            StartCoroutine(LoadRelaySceneWaitingTime());
        }
    }

    private IEnumerator JoinRelayHostWaitTime(string joinCode)
    {
        if (networkManager is OslRelayNetworkManager)
        {
            var myNetworkManager = ((OslRelayNetworkManager)networkManager);


            while (myNetworkManager.isNetworkActive)
            {
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForEndOfFrame();
            clientGotStopped = false;

            if (!OslRelayNetworkManager.isLoggedIn)
            {
                myNetworkManager.UnityLogin();
                while (!OslRelayNetworkManager.isLoggedIn)
                {
                    yield return new WaitForEndOfFrame();
                }
            }
            myNetworkManager.relayJoinCode = joinCode;
            myNetworkManager.JoinRelayServer();

            //todo if no correct code, back to menu
        }

    }

    public void StopRelayClient()
    {
        networkManager.StopClient();
        SceneManager.LoadSceneAsync((int)masterControl.Scenes.Local);
    }

    public void StopRelayHost()
    {
        networkManager.StopHost();
        SceneManager.LoadSceneAsync((int)masterControl.Scenes.Local);
    }

    public void GoBackToLocalScene()
    {
        SceneManager.LoadSceneAsync((int)masterControl.Scenes.Local);
    }
    #endregion


    #region UI
    public void ActivateHostUI()
    {
        if (networkManager is OslRelayNetworkManager && ((OslRelayNetworkManager)networkManager).IsRelayEnabled())
        {
            hostMenuParent.gameObject.SetActive(false);
            relayHostMenuParent.gameObject.SetActive(true);
            clientMenuParent.gameObject.SetActive(false);

            relayJoinCodeText.text = $"Room Code: {((OslRelayNetworkManager)networkManager).relayJoinCode}";
        }
        else
        {
            hostMenuParent.gameObject.SetActive(true);
            relayHostMenuParent.gameObject.SetActive(false);
            clientMenuParent.gameObject.SetActive(false);

            ipAdressText.text = $"Your Address: {IPManager.GetLocalIPAddress()}";
        }

        if (backButtonObject != null)
        {
            backButtonObject.SetActive(false);
        }
    }

    public void ActivateClientUI()
    {
        hostMenuParent.gameObject.SetActive(false);
        relayHostMenuParent.gameObject.SetActive(false);
        clientMenuParent.gameObject.SetActive(true);

        if (backButtonObject != null)
        {
            backButtonObject.SetActive(false);
        }
        //Check if relay host
        /*if (networkManager.IsRelayEnabled())
        {
            //show relay code
        }*/
        //else show ip adress
    }

    public void DeactivateUI()
    {
        hostMenuParent.gameObject.SetActive(false);
        relayHostMenuParent.gameObject.SetActive(false);
        clientMenuParent.gameObject.SetActive(false);
    }

    public void DeleteAllServerDiscoveryButtons()
    {
        for (int i = discoveryButtonParent.childCount; i > 0; i--)
        {
            Destroy(discoveryButtonParent.GetChild(i - 1).gameObject);
        }
    }

    //create running server button
    public void CreateServerDiscoveryButton(OSLServerResonse info)
    {
        GameObject obj = Instantiate(discoveryButtonPrefab, discoveryButtonParent);
        TMP_Text[] objText = obj.GetComponentsInChildren<TMP_Text>();
        if(info.userName.Length > 0)
        {
            objText[0].text = info.userName;
            objText[1].text = info.EndPoint.Address.ToString();
        }
        else
        {
            objText[0].text = info.EndPoint.Address.ToString();
            objText[1].text = "";
        }

        obj.GetComponent<Button>().onClick.AddListener(delegate { JoinLocalHost(info); });
    }

    public void LoadUserName()
    {
        if (PlayerPrefs.HasKey("UserName"))
        {
            userName = PlayerPrefs.GetString("UserName");
            userNameInputField.SetTextWithoutNotify(userName);
            if (localPlayer != null)
            {
                localPlayer.ChangeUserName(userName);
            }
        }
    }


    public void OnChangeUserName(string userName)
    {
        this.userName = userName;
        PlayerPrefs.SetString("UserName", userName);
        PlayerPrefs.Save();
        if (localPlayer != null)
        {
            localPlayer.ChangeUserName(userName);
        }
    }

    #endregion

    #region Player

    public void PlayPlayerAudio()
    {
        if (localPlayer != null)
        {
            localPlayer.GetComponent<SimpleClientAudioTrigger>().TryPlayAudio();
        }
    }
    #endregion

    public void SetTickRate(int value)
    {
        Application.targetFrameRate = value;
    }
}

public static class IPManager
{
    public static string GetLocalIPAddress()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        throw new System.Exception("No network adapters with an IPv4 address in the system!");
    }
}