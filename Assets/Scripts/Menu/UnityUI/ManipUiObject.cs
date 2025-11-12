using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManipUiObject : manipObject
{
    [Header("UI")]
    public float buttonThickness = 20f;

    protected Image image;
    protected Color normalColor;
    public Color selectedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    public Color grabbedColor = new Color(0.8f, 0.8f, 0.8f, 1f);


    protected virtual void Start()
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }

        normalColor = image.color;

        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        var collider = gameObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y, buttonThickness);

        Vector3 offset = new Vector3(rectTransform.sizeDelta.x * (rectTransform.pivot.x - 0.5f), rectTransform.sizeDelta.y * (rectTransform.pivot.y - 0.5f), 0f);
        offset *= -1f;
        collider.center = offset;
    }

    public override void setState(manipState state)
    {
        curState = state;
        if (curState == manipState.none)
        {
            image.color = normalColor;
        }
        else if (curState == manipState.selected)
        {
            image.color = selectedColor;
        }
        else if (curState == manipState.grabbed)
        {
            image.color = grabbedColor;
            OnGrab();
        }
    }

    public virtual void OnGrab()
    {

    }
}
