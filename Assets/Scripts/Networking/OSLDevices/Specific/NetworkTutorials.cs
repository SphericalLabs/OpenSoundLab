using Mirror;
using System;

public class NetworkTutorials : NetworkBehaviour
{
    private tutorialsDeviceInterface tutorialsDevice;

    private void Awake()
    {
        tutorialsDevice = GetComponent<tutorialsDeviceInterface>();
        if (tutorialsDevice != null)
        {
            tutorialsDevice.OnTriggerOpenTutorial += HandleTriggerOpenTutorial;
        }
    }

    private void OnDestroy()
    {
        if (tutorialsDevice != null)
        {
            tutorialsDevice.OnTriggerOpenTutorial -= HandleTriggerOpenTutorial;
        }
    }

    private void HandleTriggerOpenTutorial(tutorialPanel tut, bool startPaused)
    {
        if (isServer)
        {
            RpcTriggerOpenTutorial(Array.IndexOf(tutorialsDevice.tutorials, tut), startPaused);
        }
        else
        {
            CmdTriggerOpenTutorial(Array.IndexOf(tutorialsDevice.tutorials, tut), startPaused);
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdTriggerOpenTutorial(int tutorialIndex, bool startPaused)
    {
        RpcTriggerOpenTutorial(tutorialIndex, startPaused);
    }

    [ClientRpc]
    private void RpcTriggerOpenTutorial(int tutorialIndex, bool startPaused)
    {
        if (tutorialIndex >= 0 && tutorialIndex < tutorialsDevice.tutorials.Length)
        {
            tutorialsDevice.InternalOpenTutorial(tutorialsDevice.tutorials[tutorialIndex], startPaused);
        }
    }
}