using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleDeviceInterface : deviceInterface
{

    clipPlayerSimple player;

    public omniJack jackTrigger, jackOut, jackPitch, jackAmp;
    public dial dialPitch, dialAmp;
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
        player.playbackSpeed = 1f * Mathf.Pow(2, Utils.map(dialPitch.percent, 0f, 1f, -4f, 4f));
        player.amplitude = Mathf.Pow(dialAmp.percent, 2);

        if (player.freqExpGen != jackPitch.signal) player.freqExpGen = jackPitch.signal;
        if (player.ampGen != jackAmp.signal) player.ampGen = jackAmp.signal;
        if (player.seqGen != jackTrigger.signal) player.seqGen = jackTrigger.signal;

        // add sample start

    }

    public override void hit(bool on, int ID = -1){
        if(on && ID == 0) player.Play();
    }

    public void flashTriggerButton(){
        buttonPlay.queueFlash();
    }


    public override InstrumentData GetData()
    {
        // TODO implement serialization for knobs, etc
        SingleData data = new SingleData();
        data.deviceType = DeviceType.Single;
        GetTransformData(data);


        data.file = GetComponent<samplerLoad>().CurFile;
        data.label = GetComponent<samplerLoad>().CurTapeLabel;

        data.jackOutID = jackOut.transform.GetInstanceID();
        data.jackTrigID = jackTrigger.transform.GetInstanceID();
        data.jackAmp = jackAmp.transform.GetInstanceID();
        data.jackPitch = jackPitch.transform.GetInstanceID();

        data.dialPitch = dialPitch.percent;
        data.dialAmp = dialAmp.percent;

        return data;
    }

    public override void Load(InstrumentData d)
    {
        SingleData data = d as SingleData;
        base.Load(data);

        GetComponent<samplerLoad>().SetSample(data.label, data.file);

        jackTrigger.ID = data.jackTrigID;
        jackOut.ID = data.jackOutID;

        dialPitch.setPercent(data.dialPitch);
        jackPitch.ID = data.jackPitch;

        dialAmp.setPercent(data.dialAmp);
        jackAmp.ID = data.jackAmp;
    }

}

public class SingleData : InstrumentData{
    
    public string file;
    public string label;

    public int jackTrigID;
    public int jackOutID;

    public float dialPitch;
    public int jackPitch;

    public float dialAmp;
    public int jackAmp;

    //public float dialSampleStart;
        //public int jackSampleStart;
    //public float dialLowCut;
    //public float dialAttack;
    //public float dialDecay;
    //public float dialLinearity;

}
