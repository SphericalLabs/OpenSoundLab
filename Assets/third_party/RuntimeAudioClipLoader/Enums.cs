namespace RuntimeAudioClipLoader
{
	/// <summary>
	/// What decoder to use.
	/// </summary>
	public enum SelectDecoder
	{
		/// <summary>
		/// Attempt auto detection.
		/// </summary>
		AutoDetect = 0,
		WAV = 1,
		MP3 = 2,
		AIFF = 3,
		Ogg = 4,

	}

	/// <summary>
	/// What decoted was used.
	/// </summary>
	public enum UsedDecoder
	{
		/// <summary>
		/// Auto detection has failed.
		/// </summary>
		None = 0,
		WAV = 1,
		MP3 = 2,
		AIFF = 3,
		Ogg = 4,
	}

	/// <summary>
	/// Used to decide which decoder to use if both native and mannaged decoder are available for some <see cref="SelectDecoder"/>.
	/// </summary>
	public enum PreferredDecoder
	{
		/// <summary>
		/// Prefer native decoder provided by underlying operating system.
		/// </summary>
		PreferNative = 0,
		/// <summary>
		/// Prefer fully mannaged written in C# decoder.
		/// </summary>
		PreferManaged = 1,
	}

	/// <summary>
	/// Where and how to load audio data.
	/// </summary>
	public enum LoadMethod
	{
		/// <summary>
		/// Load all parts in background thread.
		/// </summary>
		AllPartsInBackgroundThread,
		/// <summary>
		/// Stream on demand in Unity thread.
		/// </summary>
		StreamInUnityThread,
		/// <summary>
		/// Load all parts in Unity thread, uses coroutines which load part every render frame.
		/// </summary>
		AllPartsInUnityThread,
	}

}

namespace UnityEngine
{
	// polyfills
#if UNITY_4 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
	//
	// Summary:
	//	 ///
	//	 Determines how the audio clip is loaded in.
	//	 ///
	public enum AudioClipLoadType
	{
		//
		// Summary:
		//	 ///
		//	 The audio data is decompressed when the audio clip is loaded.
		//	 ///
		DecompressOnLoad = 0,
		//
		// Summary:
		//	 ///
		//	 The audio data of the clip will be kept in memory in compressed form.
		//	 ///
		CompressedInMemory = 1,
		//
		// Summary:
		//	 ///
		//	 Streams audio data from disk.
		//	 ///
		Streaming = 2
	}

	//
	// Summary:
	//	 ///
	//	 Value describing the current load state of the audio data associated with an
	//	 AudioClip.
	//	 ///
	public enum AudioDataLoadState
	{
		//
		// Summary:
		//	 ///
		//	 Value returned by AudioClip.loadState for an AudioClip that has no audio data
		//	 loaded and where loading has not been initiated yet.
		//	 ///
		Unloaded = 0,
		//
		// Summary:
		//	 ///
		//	 Value returned by AudioClip.loadState for an AudioClip that is currently loading
		//	 audio data.
		//	 ///
		Loading = 1,
		//
		// Summary:
		//	 ///
		//	 Value returned by AudioClip.loadState for an AudioClip that has succeeded loading
		//	 its audio data.
		//	 ///
		Loaded = 2,
		//
		// Summary:
		//	 ///
		//	 Value returned by AudioClip.loadState for an AudioClip that has failed loading
		//	 its audio data.
		//	 ///
		Failed = 3
	}
#endif
}