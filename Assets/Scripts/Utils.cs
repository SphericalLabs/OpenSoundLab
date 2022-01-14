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

    /// <summary>
    /// Maps x from one range ro another, also applying a slope (see <c>expCurve</c>).
    /// </summary>
    static public float map(float x, float start1, float stop1, float start2, float stop2, float slope)
    {
        float a = (x - start1) / (stop1 - start1); //percentage of x in old range, linear
        a = expCurve(a, slope); //percentage of x in old range with slope applied
        return start2 + a * (stop2 - start2); //value mapped to new range
    }

    /// <summary>
    /// Evaluates y = ab^x-a. The curve intersects the point (0.5, s). In other words, s < 0.5 yields an exponential curve, s > 0.5 yields a logarithmic curve, s == 0.5 is perfectly linear.
    /// <para>Expects input values in range[0..1], values exceeding this range will be clamped.</para>
    /// <para>Return val will be in range[0..1]. </para>
    /// <para>To plot the curve on WolframAlpha follow this link and adjust s to your liking:</para>
    /// <para>https://www.wolframalpha.com/input/?i=plot+%281+%2F+%28%28%28%281+%2F+s%29+-+1%29%5E2%29+-+1%29%29+*+%28%28%281+%2F+s%29+-+1%29%5E2%29%5Ex+-+%281+%2F+%28%28%28%281+%2F+s%29+-+1%29%5E2%29+-+1%29%29+from+0+to+1%2C+s+%3D+0.4</para>
    /// </summary>
    static public float expCurve(float x, float s)
    {
        if (x <= 0) { return 0; }
        if (x >= 1) { return 1; }
        if (s == 0.5f) { return x; } //perfecctly linear
        //y = ab^x - a
        //y = (1/b-1) * ((1/ym) - 1)^2 - (1/b-1)
        float b = Mathf.Pow(1 / s - 1, 2);
        float a = (1 / (b - 1));
        float y = a * Mathf.Pow(b, x) - a;
        return y;
    }

    static public float dbToLin(float x)
    {
        return Mathf.Pow(10, x / 20);
    }

    static public float linToDb(float x)
    {
        return x == 0 ? -96 : 20 * Mathf.Log10(x);
    }

}
