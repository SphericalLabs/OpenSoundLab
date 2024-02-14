using System.Runtime.InteropServices;
using UnityEngine;
using System;

// Please note: this is a stub, it is not yet working
// The idea is that native dsp code can log to Unity console

public class NativeLogger {

  static IntPtr delegatePtr;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void LogDelegate(int level, string str);

  public static IntPtr getLogCallback()
  {
    if (delegatePtr == null)
    {
      LogDelegate callback_delegate = new LogDelegate(LogCallback);
      delegatePtr = Marshal.GetFunctionPointerForDelegate(callback_delegate);
    }
    return delegatePtr;
  }

  public static void LogCallback(int level, string msg)
  {
    if (level == 0)
        Debug.Log(msg);
      else if (level == 1)
        Debug.LogWarning(msg);
      else if (level == 2)
        Debug.LogError(msg);
  }

}