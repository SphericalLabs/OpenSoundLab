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

    /// <summary>
    /// At the moment, equal power crossfade just takes the square root, which is only ideal for totally uncorrelated signals.
    /// Maybe in the future a more complex routine will be implemented.
    /// </summary>
    static public float equalPowerCrossfadeGain(float lin)
    {
        float log = Mathf.Sqrt(lin);
        return log;
    }


    public static T[] AddElementToArray<T>(T[] original, T elementToAdd)
    {
        if(elementToAdd == null) return original;

        T[] newArray = new T[original.Length + 1];
        for (int i = 0; i < original.Length; i++)
        {
            newArray[i] = original[i];
        }
        newArray[original.Length] = elementToAdd;
        return newArray;
    }


    // https://chatgpt.com/share/e2a3a329-910c-42cc-83bb-b736f4f2b858
    // For array sizes lower than 10^3 these methods should not be a problem for the render budget
    public static T[] RemoveElementFromArray<T>(T[] original, T elementToRemove)
    {
        if(elementToRemove == null) return original;

        int index = System.Array.IndexOf(original, elementToRemove);
        if (index < 0)
        {
            return original;
        }

        T[] newArray = new T[original.Length - 1];
        for (int i = 0, j = 0; i < original.Length; i++)
        {
            if (i == index) continue;
            newArray[j++] = original[i];
        }
        return newArray;
    }


    public static T[] AddElementsToArray<T>(T[] original, T[] elementsToAdd)
    {
        if (elementsToAdd == null || elementsToAdd.Length == 0) return original;

        T[] newArray = new T[original.Length + elementsToAdd.Length];
        for (int i = 0; i < original.Length; i++)
        {
            newArray[i] = original[i];
        }
        for (int i = 0; i < elementsToAdd.Length; i++)
        {
            newArray[original.Length + i] = elementsToAdd[i];
        }
        return newArray;
    }

    public static T[] RemoveElementsFromArray<T>(T[] original, T[] elementsToRemove)
    {
        if (elementsToRemove == null || elementsToRemove.Length == 0) return original;

        bool[] toRemove = new bool[original.Length];
        int newSize = original.Length;

        foreach (T elementToRemove in elementsToRemove)
        {
            for (int i = 0; i < original.Length; i++)
            {
                if (original[i].Equals(elementToRemove))
                {
                    if (!toRemove[i])
                    {
                        toRemove[i] = true;
                        newSize--;
                    }
                }
            }
        }

        T[] newArray = new T[newSize];
        for (int i = 0, j = 0; i < original.Length; i++)
        {
            if (!toRemove[i])
            {
                newArray[j++] = original[i];
            }
        }

        return newArray;
    }

    private static int seedCounter = 0;
    private static readonly int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
    private static readonly int initialSeed = Guid.NewGuid().GetHashCode();

    public static int GetNoiseSeed()
    {
        seedCounter++;
        long ticks = DateTime.UtcNow.Ticks;
        int frameCount = UnityEngine.Time.frameCount;

        int seed = initialSeed ^ processId ^ seedCounter ^ (int)(ticks & 0xFFFFFFFF) ^ frameCount;

        // Ensure the seed is always positive by clearing the sign bit
        return seed & int.MaxValue;
    }

}
