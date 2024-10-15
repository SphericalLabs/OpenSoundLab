using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.Experimental.UI;

public class TextFieldKeyboard : MonoBehaviour
{
    private TMP_InputField inputField;

    public bool repositionKeyboard = false;
    public Transform positonTarget;
    public float distance = 1f;
    public float verticalOffset = -1f;

    // Start is called before the first frame update
    void Start()
    {
        inputField = GetComponent<TMP_InputField>();
        inputField.onSelect.AddListener(x => OpenKeyboard());
    }

    public void OpenKeyboard()
    {
        NonNativeKeyboard.Instance.InputField = inputField;
        NonNativeKeyboard.Instance.PresentKeyboard(inputField.text);

        if (repositionKeyboard && positonTarget != null)
        {
            var offset = positonTarget.forward;
            offset.y = 0f;
            offset.Normalize();

            var position = positonTarget.position + offset * distance + Vector3.up * verticalOffset;

            NonNativeKeyboard.Instance.RepositionKeyboard(position);
        }
    }
}
