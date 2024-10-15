using UnityEngine;
using System;

namespace Utp
{
	/// <summary>
	/// Different levels of log sensitivity.
	/// </summary>
	public enum LogLevel : int
	{
		Off,
		Error,
		Warning,
		Info,
		Verbose
	}

	/// <summary>
	/// The logging class for UTP activity.
	/// </summary>
	public static class UtpLog
	{
		public static Action<string> Verbose = Debug.Log;
		public static Action<string> Info    = Debug.Log;
		public static Action<string> Warning = Debug.LogWarning;
		public static Action<string> Error   = Debug.LogError;
	}
}