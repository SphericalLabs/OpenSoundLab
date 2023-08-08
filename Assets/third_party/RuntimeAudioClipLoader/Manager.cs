using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RuntimeAudioClipLoader
{
	/// <summary>
	/// Obsolete, please use <see cref="AudioLoader"/> instead.
	/// Facilitates loading of AudioClips at runtime from Stream or file path.
	/// </summary>
	public static class Manager
	{
		public static event Action<AudioClip, Exception> OnLoadingFailed;

		public static PreferredDecoder PreferredDecoder { get; set; }

		static Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
		static Dictionary<AudioClip, AudioClipLoadType> audioClipToLoadType = new Dictionary<AudioClip, AudioClipLoadType>();
		static Dictionary<AudioClip, AudioDataLoadState> audioClipToLoadState = new Dictionary<AudioClip, AudioDataLoadState>();

		/// <summary>
		/// Loads an AudioClip from file located at filePath.
		/// Supported formats: waw, mp3, aiff, ogg.
		/// </summary>
		/// <param name="filePath">filePath is used as Unity's name of the AudioClip and as a key used in the caching dictionary</param>
		/// <param name="doStream">audioClip will be loaded and decoded on the fly on demand, use for long one time use clips</param>
		/// <param name="loadInBackground">if !doStream and loadInBackground then loading is done in own thread so it doesnt hang up caller's thread</param>
		/// <param name="useCache">audioClip will be cached, so it won't have to be loaded again next time</param>
		/// <returns>null if error (i.e. unsupported format, invalid audio file), may contain undefined data or only silence if clip is not yet loaded</returns>
		public static AudioClip Load(
			string filePath, bool doStream = false, bool loadInBackground = true, bool useCache = true
#if UNITY_4 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
			, bool is3D = true
#endif
		)
		{
			if (!IsSupportedFormat(filePath))
			{
				Debug.LogError(
					"Could not load AudioClip at path '" + filePath +
					"' it's extensions marks unsupported format, supported formats are: " +
					string.Join(", ", Enum.GetNames(typeof(SelectDecoder))));
				return null;
			}
			AudioClip audioClip = null;
			string cacheKey = null;
			if (useCache)
			{
				cacheKey = filePath + (doStream ? "1" : "0");
				if (cache.TryGetValue(cacheKey, out audioClip) && audioClip)
				{
					return audioClip;
				}
			}

			var dataStream = File.OpenRead(filePath);
			audioClip = Load(
				dataStream, GetAudioFormat(filePath), filePath, doStream, loadInBackground, diposeDataStreamIfNotNeeded: true
#if UNITY_4 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
				, is3D:is3D
#endif
			);

			if (useCache)
			{
				cache[cacheKey] = audioClip;
			}
			return audioClip;
		}

		/// <summary>
		/// Loads an AudioClip from Stream, you need to manually supply AudioFormat.
		/// Use GetAudioFormat to get AudioFormat from filePath.
		/// Supported formats: waw, mp3, aiff, ogg.
		/// </summary>
		/// <param name="dataStream">Stream used to read the audio data</param>
		/// <param name="audioFormat">Use GetAudioFormat to get AudioFormat from filePath</param>
		/// <param name="unityAudioClipName">Used as Unity's name of the AudioClip</param>
		/// <param name="doStream">audioClip will be loaded and decoded on the fly on demand, use for long one time use clips</param>
		/// <param name="loadInBackground">if !doStream and loadInBackground then loading is done in own thread so it doesnt hang up caller's thread</param>
		/// <param name="diposeDataStreamIfNotNeeded">if !doStream will Close and Dispose the dataStream if/when it's no longer needed</param>
		/// <returns>null if error (i.e. unsupported format), may contain unknown data or only silence if clip is not yet loaded</returns>
		public static AudioClip Load(
			Stream dataStream, SelectDecoder audioFormat, string unityAudioClipName, bool doStream = false, bool loadInBackground = true, bool diposeDataStreamIfNotNeeded = true
#if UNITY_4 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
			, bool is3D = true
#endif
		)
		{
			LoadMethod loadType;
			if (doStream) loadType = LoadMethod.StreamInUnityThread;
			else if (loadInBackground) loadType = LoadMethod.AllPartsInBackgroundThread;
			else loadType = LoadMethod.AllPartsInUnityThread;

			return Load(dataStream, audioFormat, unityAudioClipName, loadType, diposeDataStreamIfNotNeeded);
		}

		public static AudioClip Load(
			Stream dataStream, SelectDecoder audioFormat, string unityAudioClipName, LoadMethod loadType = LoadMethod.AllPartsInBackgroundThread, bool diposeDataStreamIfNotNeeded = true
#if UNITY_4 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
			, bool is3D = true
#endif
		)
		{
			var config = new AudioLoaderConfig()
			{
				DataStream = dataStream,
				AudioFormat = audioFormat,
				UnityAudioClipName = unityAudioClipName,
				LoadMethod = loadType,
				DisposeDataStreamIfNotNeeded = diposeDataStreamIfNotNeeded,
				PreferredDecoder = PreferredDecoder,
#if UNITY_4 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
				Is3D = is3D,
#endif
			};

			var audioLoader = new AudioLoader(config);

			audioLoader.OnLoadingDone += () => OnLoadingDonePrivate(audioLoader);
			audioLoader.OnLoadingAborted += () => OnLoadingAbortedPrivate(audioLoader);
			audioLoader.OnLoadingFailed += (exception) => OnLoadingFailedPrivate(audioLoader, exception);

			if (loadType == LoadMethod.StreamInUnityThread) SetAudioClipLoadType(audioLoader.AudioClip, AudioClipLoadType.Streaming);
			else SetAudioClipLoadType(audioLoader.AudioClip, AudioClipLoadType.DecompressOnLoad);
			SetAudioClipLoadState(audioLoader.AudioClip, audioLoader.LoadState);

			audioLoader.StartLoading();

			SetAudioClipLoadState(audioLoader.AudioClip, audioLoader.LoadState);
			return audioLoader.AudioClip;
		}

		static void OnLoadingDonePrivate(AudioLoader loader)
		{
			SetAudioClipLoadState(loader.AudioClip, loader.LoadState);
		}
		static void OnLoadingAbortedPrivate(AudioLoader loader)
		{
			SetAudioClipLoadState(loader.AudioClip, loader.LoadState);
		}
		static void OnLoadingFailedPrivate(AudioLoader loader, Exception exception)
		{
			SetAudioClipLoadState(loader.AudioClip, loader.LoadState);
			if (OnLoadingFailed != null) OnLoadingFailed(loader.AudioClip, exception);
			else Debug.LogException(exception);
		}


		/// <summary>
		/// Facilitates user settable/gettable AudioClip.loadState
		/// </summary>
		public static void SetAudioClipLoadState(AudioClip audioClip, AudioDataLoadState newLoadState)
		{
			if (audioClip != null) audioClipToLoadState[audioClip] = newLoadState;
		}

		/// <summary>
		/// Facilitates user settable/gettable AudioClip.loadState
		/// </summary>
		/// <returns>AudioDataLoadState.Failed if audioClip is null or audioClip.loadState if this manager did not touch the audioClip</returns>
		public static AudioDataLoadState GetAudioClipLoadState(AudioClip audioClip)
		{
			AudioDataLoadState ret = AudioDataLoadState.Failed;
#if UNITY_4 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
			// audioLoadState.TryGetValue(audioClip, out ret); // TODO: fix
#else
			if (audioClip != null)
			{
				ret = audioClip.loadState;
				audioClipToLoadState.TryGetValue(audioClip, out ret);
			}
#endif
			return ret;
		}

		/// <summary>
		/// Facilitates a user settable/gettable AudioClip.loadType
		/// </summary>
		public static void SetAudioClipLoadType(AudioClip audioClip, AudioClipLoadType newLoadType)
		{
			if (audioClip != null) audioClipToLoadType[audioClip] = newLoadType;
		}

		/// <summary>
		/// Facilitates a user settable/gettable AudioClip.loadType
		/// </summary>
		/// <returns>-1 if audioClip is null or audioClip.loadType if this manager did not touch the audioClip</returns>
		public static AudioClipLoadType GetAudioClipLoadType(AudioClip audioClip)
		{
			AudioClipLoadType ret = (AudioClipLoadType)(-1);
#if UNITY_4 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
			// audioClipLoadType.TryGetValue(audioClip, out ret); //TODO: fix
#else
			if (audioClip != null)
			{
				ret = audioClip.loadType;
				audioClipToLoadType.TryGetValue(audioClip, out ret);
			}
#endif
			return ret;
		}

		/// <summary>
		/// Checks if filePath constains file with supported extension
		/// </summary>
		/// <returns>true if extension is supported, false if not</returns>
		public static bool IsSupportedFormat(string filePath)
		{
			return AudioLoaderConfig.IsSupportedFormat(filePath);
		}

		/// <summary>
		/// Returns AudioFormat enum from filePath extension
		/// </summary>
		/// <returns>AudioFormat if filePath extension is supported format, -1 otherwise</returns>
		public static SelectDecoder GetAudioFormat(string filePath)
		{
			return AudioLoaderConfig.GetAudioFormat(filePath);
		}

		/// <summary>
		/// Clears all cached AudioClips
		/// </summary>
		public static void ClearCache()
		{
			cache.Clear();
			audioClipToLoadType.Clear();
			audioClipToLoadState.Clear();
		}
	}
}