using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using RuntimeAudioClipLoader.Internal;

namespace RuntimeAudioClipLoader
{
	/// <summary>
	/// Confguration for <see cref="AudioLoader"/>. You should not change this once loading starts. If you want to use similar <see cref="AudioLoaderConfig"/> instance for another <see cref="AudioLoader"/> use <see cref="AudioLoaderConfig.Clone"/>.
	/// </summary>
	public class AudioLoaderConfig : ICloneable
	{
		int raiseProgressChangedEveryMiliseconds = 100;
		public int RaiseProgressChangedEveryMiliseconds { get { return raiseProgressChangedEveryMiliseconds; } set { raiseProgressChangedEveryMiliseconds = value; } }

		int loadInUnityThreadMaximumMilisecondsSpentPerFrame = 10;
		/// <summary>
		/// Used if <see cref="LoadMethod"/>==<see cref="RuntimeAudioClipLoader.LoadMethod.AllPartsInUnityThread"/>, then loading of one part of AudioClip data per one render frame will not take longer than specified here.
		/// </summary>
		public int LoadInUnityThreadMaximumMilisecondsSpentPerFrame { get { return loadInUnityThreadMaximumMilisecondsSpentPerFrame; } set { loadInUnityThreadMaximumMilisecondsSpentPerFrame = value; } }

		int loadBufferSize = 4 * 1024;
		/// <summary>
		/// Maximum allowed size of buffer in bytes, buffed is used to load and set AudioClip data.
		/// </summary>
		public int LoadBufferSize { get { return loadBufferSize; } set { loadBufferSize = value; } }

		LoadMethod loadMethod = LoadMethod.AllPartsInBackgroundThread;
		/// <summary>
		/// Where and how to load audio data. default: <see cref="RuntimeAudioClipLoader.LoadMethod.AllPartsInBackgroundThread"/>
		/// </summary>
		public LoadMethod LoadMethod
		{
			get
			{
				if (IsWebGLPlayer)
				{
					// on WebGL we cant create threads or stream AudioClip, so we revert back to Unity thread
					if (loadMethod == LoadMethod.AllPartsInBackgroundThread || loadMethod == LoadMethod.StreamInUnityThread)
					{
						loadMethod = LoadMethod.AllPartsInUnityThread;
					}
				}
				return loadMethod;
			}
			set
			{
				loadMethod = value;
			}
		}

		public static bool IsWebGLPlayer
		{
			get
			{
#if UNITY_4 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
				return false; // RuntimePlatform.WebGLPlayer was added in Unity 5.0.0
#else
				return Application.platform == RuntimePlatform.WebGLPlayer;
#endif
			}
		}

		bool is3D = true;
		/// <summary>
		/// Unity 4.7 and older had a spatiality setting (true/false) directly in AudioClip. Nowadays you can change spatiality on AudioSource component. This is only used if you are working in Unity 4.7 or older. default: true
		/// </summary>
		public bool Is3D { get { return is3D; } set { is3D = value; } }

		PreferredDecoder preferredDecoder = PreferredDecoder.PreferNative;
		/// <summary>
		/// Used to decide which decoder to use if both native and mannaged decoder are available. default: <see cref="RuntimeAudioClipLoader.PreferredDecoder.PreferNative"/>
		/// </summary>
		public PreferredDecoder PreferredDecoder { get { return preferredDecoder; } set { preferredDecoder = value; } }

		SelectDecoder audioFormat;
		public SelectDecoder AudioFormat { get { return audioFormat; } set { audioFormat = value; } }

		bool disposeDataStreamIfNotNeeded = true;
		/// <summary>
		/// Should <see cref="AudioLoader"/> dispose <see cref="DataStream"/> once it's not needed ? default: true
		/// </summary>
		public bool DisposeDataStreamIfNotNeeded { get { return disposeDataStreamIfNotNeeded; } set { disposeDataStreamIfNotNeeded = value; } }

		Stream dataStream;
		public Stream DataStream { get { return dataStream; } set { dataStream = value; } }

		static ulong generatedUnityAudioClipNameCounter = 1;
		string unityAudioClipName;
		/// <summary>
		/// You will see this in <see cref="UnityEngine.AudioClip.name"/>. Defaults to auto generated name.
		/// </summary>
		public string UnityAudioClipName
		{
			get
			{
				if (unityAudioClipName == null)
				{
					unityAudioClipName = typeof(AudioClip) + " from " + typeof(AudioLoader) + " #" + generatedUnityAudioClipNameCounter;
					generatedUnityAudioClipNameCounter++;
				}
				return unityAudioClipName;
			}
			set
			{
				unityAudioClipName = value;
			}
		}

		IFloatArrayPool floatArrayPool = RuntimeAudioClipLoader.Internal.FloatArrayPool.Shared;
		/// <summary>
		/// default: <see cref="RuntimeAudioClipLoader.Internal.FloatArrayPool.Shared"/>
		/// </summary>
		public IFloatArrayPool FloatArrayPool { get { return floatArrayPool; } set { floatArrayPool = value; } }

		IActionRunner backgroundThreadRunner = RuntimeAudioClipLoader.Internal.BackgroundThreadRunner.Shared;
		/// <summary>
		/// default: <see cref="RuntimeAudioClipLoader.Internal.UnityMainThreadRunner.Shared"/>
		/// </summary>
		public IActionRunner BackgroundThreadRunner { get { return backgroundThreadRunner; } set { backgroundThreadRunner = value; } }

		IActionAndCoroutineRunner unityMainThreadRunner = RuntimeAudioClipLoader.Internal.UnityMainThreadRunner.Shared;
		/// <summary>
		/// default: <see cref="RuntimeAudioClipLoader.Internal.UnityMainThreadRunner.Shared"/>
		/// </summary>
		public IActionAndCoroutineRunner UnityMainThreadRunner { get { return unityMainThreadRunner; } set { unityMainThreadRunner = value; } }

		static readonly SelectDecoder[] selectDecoderValues;
		static readonly string[] selectDecoderNames;
		static readonly HashSet<string> supportedFormats;
		static AudioLoaderConfig()
		{
			selectDecoderValues = (SelectDecoder[])Enum.GetValues(typeof(SelectDecoder));
			selectDecoderNames = Enum.GetNames(typeof(SelectDecoder)).Select(s => s.ToLowerInvariant()).ToArray();
			supportedFormats = new HashSet<string>(selectDecoderNames);
		}

		public AudioLoaderConfig()
		{
		}

		public AudioLoaderConfig(Stream dataStream)
		{
			if (dataStream == null) throw new NullReferenceException("dataStream can't be null");

			this.dataStream = dataStream;
		}

		/// <summary>
		/// Checks if filePath constains file with supported extension.
		/// </summary>
		/// <returns>T if extension is supported.</returns>
		public static bool IsSupportedFormat(string filePath)
		{
			if (string.IsNullOrEmpty(filePath)) return false;
			return supportedFormats.Contains(GetExtension(filePath));
		}

		public static SelectDecoder GetAudioFormat(string filePath)
		{
			var extension = GetExtension(filePath);
			for (int i = 0; i < selectDecoderValues.Length; i++)
			{
				if (extension == selectDecoderNames[i])
					return selectDecoderValues[i];
			}
			return SelectDecoder.AutoDetect;
		}

		static string GetExtension(string filePath)
		{
			if (string.IsNullOrEmpty(filePath)) return string.Empty;
			var dot = filePath.LastIndexOf(".");
			if (dot == -1) return string.Empty;
			return filePath.Substring(dot + 1).ToLowerInvariant();
		}

		public virtual AudioLoaderConfig Clone()
		{
			return (AudioLoaderConfig)MemberwiseClone();
		}
		object ICloneable.Clone()
		{
			return MemberwiseClone();
		}
	}
}
