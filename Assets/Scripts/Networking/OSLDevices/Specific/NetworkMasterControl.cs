using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkMasterControl : NetworkSyncListener
{
    [SerializeField] private metronome metronome;
    [SerializeField] private button startButton;
    [SerializeField] private button rewindButton;
    [SyncVar(hook = nameof(BroadcastBpm))]
    private float pitchBendMult = 1f;

    private void Start()
    {
        metronome.PitchBendChange += OnPitchBendChange;
        startButton.onStartGrabEvents.AddListener(OnButtonPress);
        rewindButton.onStartGrabEvents.AddListener(OnButtonPress);
    }

    private void OnDestroy()
    {
        metronome.PitchBendChange -= OnPitchBendChange;
        startButton.onToggleChangedEvent.RemoveListener(OnButtonPress);
        rewindButton.onToggleChangedEvent.RemoveListener(OnButtonPress);
    }

    private void OnPitchBendChange(float pitchChangeMultiplier)
    {
        pitchBendMult = pitchChangeMultiplier;
    }

    private void BroadcastBpm(float oldPitchBend, float newPitchBend)
    {
        Debug.Log("Changed BPM to " + newPitchBend);
        metronome.broadcastBpm();
    }

    private void OnButtonPress()
    {
        NetworkSyncEventManager.Instance.UpdateSync();
    }


    #region Mirror

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isServer)
        {
            CmdRequestSync();
        }
    }
    protected override void OnSync()
    {
        if (isServer)
        {
            RpcUpdate_measurePhase(masterControl.instance.MeasurePhase);
            pitchBendMult = metronome.PitchBendMult;
        }
        else
        {
            CmdRequestSync();
        }
    }

    protected override void OnIntervalSync()
    {
        base.OnIntervalSync();
        if (isServer)
        {
            RpcUpdate_measurePhase(masterControl.instance.MeasurePhase);
            pitchBendMult = metronome.PitchBendMult;
        }
    }

    [Command(requiresAuthority = false)]
    protected void CmdRequestSync()
    {
        Debug.Log($"{gameObject.name} CmdRequestSync");

        RpcUpdate_measurePhase(masterControl.instance.MeasurePhase);
    }

    [ClientRpc]
    protected virtual void RpcUpdate_measurePhase(double measurePhase)
    {
        if (isClient && !isServer)
        {
            Debug.Log($"{gameObject.name} old _measurePhase: { masterControl.instance.MeasurePhase}, new _measurePhase {measurePhase}");

            masterControl.instance.MeasurePhase = measurePhase;
        }
    }
    #endregion
}
