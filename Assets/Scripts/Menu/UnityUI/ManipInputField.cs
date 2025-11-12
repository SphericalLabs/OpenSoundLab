using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ManipInputField : ManipUiObject
{
    private TMP_InputField inputField;

    protected override void Start()
    {
        inputField = GetComponent<TMP_InputField>();
        base.Start();
    }

    public override void OnGrab()
    {
        base.OnGrab();
        inputField.Select();
    }
}
