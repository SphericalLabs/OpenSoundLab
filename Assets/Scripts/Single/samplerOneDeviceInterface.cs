using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

public class samplerOneDeviceInterface : deviceInterface
{

    clipPlayerSimple player;

    public omniJack jackTrigger, jackOut, jackPitch, jackAmp, jackStart;
    public dial dialPitch, dialAmp, dialStart;
    public button buttonPlay;

    public string currentSample;

    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<clipPlayerSimple>();
    }

    // Update is called once per frame
    void Update()
    {
        // todo: only update when necessary
        player.playbackSpeed = 1f * Mathf.Pow(2, Utils.map(dialPitch.percent, 0f, 1f, -4f, 4f));
        player.amplitude = Mathf.Pow(dialAmp.percent, 2);

        if (player.sampleStart != dialStart.percent)
        {
            player.sampleStart = dialStart.percent;
            // do not update the sample bound on change already, only when a sample is triggered!
        }

        if (player.freqExpGen != jackPitch.signal) player.freqExpGen = jackPitch.signal;
        if (player.ampGen != jackAmp.signal) player.ampGen = jackAmp.signal;
        if (player.seqGen != jackTrigger.signal) player.seqGen = jackTrigger.signal;
        if (player.startGen != jackStart.signal) player.startGen = jackStart.signal;

    }

    public override void hit(bool on, int ID = -1)
    {
        if (on && ID == 0) player.Play();
    }

    public void flashTriggerButton()
    {
        buttonPlay.queueFlash();
    }


    public override InstrumentData GetData()
    {
        // TODO implement serialization for knobs, etc
        SamplerOneData data = new SamplerOneData();
        data.deviceType = DeviceType.SamplerOne;
        GetTransformData(data);


        data.file = GetComponent<samplerLoad>().CurFile;
        data.label = GetComponent<samplerLoad>().CurTapeLabel;

        data.jackOutID = jackOut.transform.GetInstanceID();
        data.jackTrigID = jackTrigger.transform.GetInstanceID();
        data.jackAmp = jackAmp.transform.GetInstanceID();
        data.jackPitch = jackPitch.transform.GetInstanceID();
        data.jackStart = jackStart.transform.GetInstanceID();

        data.dialPitch = dialPitch.percent;
        data.dialAmp = dialAmp.percent;
        data.dialStart = dialStart.percent;

        return data;
    }

    public override void Load(InstrumentData d, bool copyMode)
    {
        SamplerOneData data = d as SamplerOneData;
        base.Load(data, copyMode);

        GetComponent<samplerLoad>().SetSample(data.label, data.file);

        jackTrigger.SetID(data.jackTrigID, copyMode);
        jackOut.SetID(data.jackOutID, copyMode);

        dialPitch.setPercent(data.dialPitch);
        jackPitch.SetID(data.jackPitch, copyMode);

        dialAmp.setPercent(data.dialAmp);
        jackAmp.SetID(data.jackAmp, copyMode);

        dialStart.setPercent(data.dialStart);
        jackStart.SetID(data.jackStart, copyMode);
    }

}

public class SamplerOneData : InstrumentData
{

    public string file;
    public string label;

    public int jackTrigID;
    public int jackOutID;

    public float dialPitch;
    public int jackPitch;

    public float dialAmp;
    public int jackAmp;

    public float dialStart;
    public int jackStart;

    //public float dialSampleStart;
    //public int jackSampleStart;
    //public float dialLowCut;
    //public float dialAttack;
    //public float dialDecay;
    //public float dialLinearity;

}
