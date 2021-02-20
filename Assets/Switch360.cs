using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Switch360 : MonoBehaviour
{
    public List<Texture> environments;
    public float lastPointer = 0;
    float newPointer = 0f;
    int pointer = 0;
    Material mat;
    Vector2 leftStick;
    Vector3 rotation;

    void Start()
    {
        mat = GetComponent<Renderer>().materials[0];
        mat.mainTexture = environments[0];
        rotation = new Vector3(0f, 0f, 0f);
    }

    void Update()
    {
        leftStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);

        // rotate 360 sphere around y-axis
        if (leftStick.x != 0f)
        {
            rotation.y = -1 * Mathf.Sign(leftStick.x) * Mathf.Abs(Mathf.Pow(leftStick.x, 4) * 1f);
            transform.Rotate(rotation);
        }

        // shuttle through 360 panos, allows for hi-speed fun...
        //if (leftStick.y != 0f) { 
        //newPointer += Mathf.Sign(leftStick.y) * Mathf.Abs(Mathf.Pow(leftStick.y, 3) * 0.3f);
        //if (newPointer > environments.Count - 1)
        //{
        //  newPointer = 0;
        //}
        //else if (newPointer < 0)
        //{
        //  newPointer = environments.Count - 1;
        //}
        //if (Mathf.RoundToInt(lastPointer) != Mathf.RoundToInt(newPointer)) {
        //  mat.mainTexture = environments[Mathf.RoundToInt(newPointer)];
        //}
        //lastPointer = newPointer;

        //}

        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickUp, OVRInput.Controller.LTouch))
        {
            pointer = mod(pointer + 1, environments.Count);
            mat.mainTexture = environments[pointer];
        }
        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickDown, OVRInput.Controller.LTouch))
        {
            pointer = mod(pointer - 1, environments.Count);
            mat.mainTexture = environments[pointer];
        }

    }
    int mod(int a, int b)
    {
        int c = a % b;
        return (c < 0) ? c + b : c;
    }
}
