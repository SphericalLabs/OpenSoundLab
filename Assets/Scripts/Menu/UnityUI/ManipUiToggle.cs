using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManipUiToggle : ManipUiObject
{
    private Toggle toggle;

    protected override void Start()
    {
        toggle = GetComponent<Toggle>();
        image = toggle.image;
        base.Start();
    }

    public override void OnGrab()
    {
        base.OnGrab();
        toggle.isOn = !toggle.isOn;
    }
}
