using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Utils
{

    static public float lerp(float start, float stop, float amt)
    {
        return start + (stop - start) * amt;
    }


    static public float map(float value,
                              float start1, float stop1,
                              float start2, float stop2)
    {
        try
        {
            return start2 + (stop2 - start2) * ((value - start1) / (stop1 - start1));
        }
        catch (DivideByZeroException e)
        {
            return 0;
        }
    }

}
