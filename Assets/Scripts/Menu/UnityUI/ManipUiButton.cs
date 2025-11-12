using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManipUiButton : ManipUiObject
{
    private Button button;

    protected override void Start()
    {
        button = GetComponent<Button>();
        base.Start();
    }

    public override void OnGrab()
    {
        base.OnGrab();
        button.onClick.Invoke();
    }
}
