using Mirror;
using UnityEngine;

public class Player : NetworkBehaviour
{
    /// <summary>
    /// The Sessions ID for the current server.
    /// </summary>
    [SyncVar]
    public string sessionId = "";

    /// <summary>
    /// Player name.
    /// </summary>
    public string username;

    public string ip;

    /// <summary>
    /// Platform the user is on.
    /// </summary>
    public string platform;

    /// <summary>
    /// Shifts the players position in space based on the inputs received.
    /// </summary>
    void HandleMovement()
    {
        if (isLocalPlayer)
        {
            float moveHorizontal = Input.GetAxis("Horizontal") * Time.deltaTime;
            float moveVertical = Input.GetAxis("Vertical") * Time.deltaTime;
            Vector3 movement = new Vector3(moveHorizontal * 3f, moveVertical * 3f, 0);
            transform.position = transform.position + movement;
        }
    }

    private void Awake()
    {
        username = SystemInfo.deviceName;
        platform = Application.platform.ToString();
        ip = NetworkManager.singleton.networkAddress;
    }

    private void Start()
    {
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
    }

    void Update()
    {
        HandleMovement();
    }

    public void MoveHorizontal(float value)
    {
        if (isLocalPlayer)
        {
            Vector3 movement = new Vector3(value * 0.1f, 0, 0);
            transform.position = transform.position + movement;
        }
    }
    public void MoveVertical(float value)
    {
        if (isLocalPlayer)
        {
            Vector3 movement = new Vector3(0f, value * 0.1f, 0);
            transform.position = transform.position + movement;
        }
    }

    /// <summary>
    /// Called after player has spawned in the scene.
    /// </summary>
    public override void OnStartServer()
    {
        Debug.Log("Player has been spawned on the server!");
    }
}
