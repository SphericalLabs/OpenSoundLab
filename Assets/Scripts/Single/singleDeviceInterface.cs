using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleDeviceInterface : MonoBehaviour
{

    clipPlayerSimple player;
    public omniJack jackSampleOut, jackPitch, jackAmp;
    public dial dialPitch, dialAmp;
    public button buttonPlay;

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

    }

}
