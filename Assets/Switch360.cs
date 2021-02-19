using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Switch360 : MonoBehaviour
{
    public List<Texture> environments;
    public float lastPointer = 0;
    float newPointer = 0f;
    Material mat;

    void Start()
    {
        mat = GetComponent<Renderer>().materials[0];
        mat.mainTexture = environments[0];
    }

    void Update()
    {

        newPointer += Mathf.Pow(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).x, 2) * 0.2f;
        if (newPointer > environments.Count - 1)
        {
            newPointer = 0;
        }
        else if (newPointer < 0)
        {
            newPointer = environments.Count - 1;
        }
        if (Mathf.RoundToInt(lastPointer) != Mathf.RoundToInt(newPointer)) {
            mat.mainTexture = environments[Mathf.RoundToInt(newPointer)];
        }
        lastPointer = newPointer;

    }
}
