using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using RuntimeAudioClipLoader.Internal;

namespace RuntimeAudioClipLoader
{
	/// <summary>
	/// Handles <see cref="UnityEngine.AudioClip"/> loading from arbitrary byte stream.
	/// </summary>
	public class AudioLoader
	{
		public event Action<Exception> OnLoadingFailed;
		public event Action OnLoadingDone;
		public event Action OnLoadingAborted;
		public event Action OnLoadProgressChanged;

		/// <summary>
		/// Shortcut for <see cref="LoadState"/> == <see cref="AudioDataLoadState.Loaded"/>
		/// </summary>
		public bool IsLoadingDone { get { return LoadState == AudioDataLoadState.Loaded; } }
		/// <summary>
		/// Audio data will be loaded into this <see cref="UnityEngine.AudioClip"/> instance. Make sure to call <see cref="UnityEngine.Object.Destroy(UnityEngine.Object)"/> on the instance after you are done using it. If you have thousands of unused AudioClips you risk running out of memory if you don't destroy them.
		/// </summary>
		public AudioClip AudioClip { get; private set; }
		/// <summary>
		/// Total count of data samples.
		/// </summary>
		public int SamplesCount { get; private set; }
		/// <summary>
		/// Unity's <see cref="UnityEngine.AudioClip.length"/> is number of time units, one time unit plays samples for all channels.
		/// <see cref="UnitySamplesCount"/> = <see cref="SamplesCount"/> / <see cref="Channels"/>
		/// </summary>
		public int UnitySamplesCount
		{
			get
			{
				if (Channels == 0) return 0;
				return SamplesCount / Channels;
			}
		}
		/// <summary>
		/// 1 means mono, 2 means stereo, theoretically audio files support any number of channels.
		/// </summary>
		public int Channels { get; private set; }
		/// <summary>
		/// Number of samples played per second.
		/// </summary>
		public int SampleRate { get; private set; }
		/// <summary>
		/// Value describing the current load state of the audio data.
		/// </summary>
		public AudioDataLoadState LoadState { get; private set; }
		/// <summary>
		/// Total lenght of audio data in seconds.
		/// </summary>
		public float Length
		{
			get
			{
				if (SampleRate == 0) return 0;
				if (Channels == 0) return 0;
				return ((float)SamplesCount) / Channels / SampleRate;
			}
		}
		/// <summary>
		/// <see cref="AudioClip"/> load progress in inclusive range of &lt;0,1&gt; e.g.: 0 is 0%, 0.5 is 50% (halfway loaded), 1 is 100% (fully loaded).
		/// </summary>
		public float LoadProgress
		{
			get
			{
				if (SamplesCount == 0) return 0;
				return (float)SamplesLoaded / SamplesCount;
			}
		}
		/// <summary>
		/// Number of samples that have been loaded so far.
		/// </summary>
		public int SamplesLoaded { get; private set; }
		/// <summary>
		/// What decoder was used to decode the audio data.
		/// </summary>
		public UsedDecoder UsedDecoder { get; private set; }
		/// <summary>
		/// Do not change while loading. Holds the <see cref="AudioLoaderConfig"/> instance provided in constructor. Changing contents while loading is underway may result in unexpected behavior.
		/// </summary>
		public AudioLoaderConfig Config { get; private set; }
		/// <summary>
		/// If true loading will stop in the nearest possible future.
		/// Set to true in <see cref="AbortLoading"/>.
		/// Set to false in <see cref="StartLoading"/>.
		/// </summary>
		public bool AbortLoadingRequested { get; set; }
		bool isDisposed;
		AudioDataReader reader;
		bool AudioClipSetDataOffsetSupported { get { return AudioLoaderConfig.IsWebGLPlayer == false; } }
		bool ShouldAbortLoading { get { return AudioClip == null || AbortLoadingRequested; } }

		/// <summary>
		/// After constructor all properties all set and <see cref="AudioClip"/> is created.
		/// Use <see cref="StartLoading"/> to start loading.
		/// At this point you can subscribe to events to make sure you don't miss any.
		/// </summary>
		/// <param name="config"></param>
		public AudioLoader(AudioLoaderConfig config)
		{
			this.Config = config;
			CreateReader();
			if (reader != null) CreateAudioClip();
		}

		void CreateReader()
		{
			try
			{
				reader = new AudioDataReader(Config.DataStream, Config.AudioFormat, Config.PreferredDecoder);
				SamplesCount = (int)(reader.Length / (long)(reader.WaveFormat.BitsPerSample / 8));
				Channels = reader.WaveFormat.Channels;
				SampleRate = reader.WaveFormat.SampleRate;
				UsedDecoder = reader.UsedDecoder;
			}
			catch (Exception exception)
			{
				MyOnLoadingFailed(exception);
			}
		}

		void CreateAudioClip()
		{
			try
			{
				if (Config.LoadMethod == LoadMethod.StreamInUnityThread)
				{
					LoadState = AudioDataLoadState.Loading;

					AudioClip = AudioClip.Create(
						Config.UnityAudioClipName, UnitySamplesCount, Channels, SampleRate,
#if UNITY_4 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
						Config.Is3D,
#endif
						true,
						delegate (float[] target)
						{
							if (reader != null)
								reader.Read(target, 0, target.Length);
						},
						delegate (int target)
						{
							if (reader != null)
								reader.Seek((long)target, SeekOrigin.Begin);
						}
					);
				}
				else
				{
					LoadState = AudioDataLoadState.Unloaded;

					AudioClip = AudioClip.Create(
						Config.UnityAudioClipName, UnitySamplesCount, Channels, SampleRate,
#if UNITY_4 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
						Config.Is3D,
#endif
						false
					);
				}
			}
			catch (Exception exception)
			{
				MyOnLoadingFailed(exception);
			}
		}

		/// <summary>
		/// Starts the process of loading AudioClip. Before calling this you can subscribe to events to make sure you dont miss any. The events are: <see cref="OnLoadingDone"/>, <see cref="OnLoadingAborted"/>, <see cref="OnLoadingFailed"/> and <see cref="OnLoadProgressChanged"/>.
		/// </summary>
		/// <returns>True if loading was successfully started.</returns>
		public bool StartLoading()
		{
			AbortLoadingRequested = false; // false, we are requesting start of loading now
			if (ShouldAbortLoading) // AudioClip was destroyed
			{
				MyOnLoadingAborted();
				return false;
			}
			if (Config.LoadMethod == LoadMethod.StreamInUnityThread) return false; // no need to load data, they are being streamed
			if (reader == null) return false; // already Disposed

			if (LoadState == AudioDataLoadState.Loading) return false; // StartLoading() already called
			LoadState = AudioDataLoadState.Loading;

			try
			{
				switch (Config.LoadMethod)
				{
					case LoadMethod.AllPartsInBackgroundThread:
						Config.BackgroundThreadRunner.Enqueue(LoadInBackgroundThread);
						break;
					case LoadMethod.AllPartsInUnityThread:
						Config.UnityMainThreadRunner.Enqueue(LoadInUnityThread());
						break;
				}
			}
			catch (Exception exception)
			{
				MyOnLoadingFailed(exception);
			}
			return true;
		}

		static void ClampToMultiplierOf(ref int number, int multiplier)
		{
			number = ((int)((float)number / multiplier)) * multiplier;
		}

		IEnumerator LoadInUnityThread()
		{
			if (ShouldAbortLoading)
			{
				MyOnLoadingAborted();
				yield break;
			}

			var audioClipSetDataOffsetSupported = AudioClipSetDataOffsetSupported;

			float[] allData = null;
			if (!audioClipSetDataOffsetSupported)
				allData = Config.FloatArrayPool.Rent(SamplesCount);

			var currentDataLength = Config.LoadBufferSize;
			if (currentDataLength > SamplesCount) currentDataLength = SamplesCount;
			ClampToMultiplierOf(ref currentDataLength, Channels);

			float[] currentData = Config.FloatArrayPool.Rent(currentDataLength);
			int currentOffset = 0;

			var yieldStopwatch = Stopwatch.StartNew();
			int yieldPartsLoaded = 0;
			var raiseProgressChangedStopwatch = Stopwatch.StartNew();

			while (currentOffset < SamplesCount)
			{
				if (currentOffset + currentDataLength >= SamplesCount)
					currentDataLength = SamplesCount - currentOffset; // this is last part, will read less samples

				if (currentData != null && currentDataLength != currentData.Length)
				{
					Config.FloatArrayPool.Return(currentData);
					currentData = Config.FloatArrayPool.Rent(currentDataLength);
				}

				reader.Read(currentData, 0, currentDataLength);

				if (ShouldAbortLoading)
				{
					Config.FloatArrayPool.Return(currentData);
					if (allData != null) Config.FloatArrayPool.Return(allData);
					MyOnLoadingAborted();
					yield break;
				}

				if (audioClipSetDataOffsetSupported)
					AudioClip.SetData(currentData, currentOffset / Channels);
				else
					Array.Copy(currentData, 0, allData, currentOffset, currentDataLength);

				currentOffset += currentDataLength;
				SamplesLoaded = currentOffset;

				if (raiseProgressChangedStopwatch.ElapsedMilliseconds > Config.RaiseProgressChangedEveryMiliseconds)
				{
					MyOnLoadProgressChanged();
					raiseProgressChangedStopwatch.Reset();
					raiseProgressChangedStopwatch.Start();
				}

				// Case 1:
				// |---------------------------Unity render frame---------------------------|
				// |--loaded part 1--||--loaded part 2--||--loaded part 3--||--loaded part 4--||--loaded part 5--||--loaded part 6--|
				//
				// Case 2:
				// |---------------------------Unity render frame---------------------------|
				// |----------------loaded part 1----------------||----------------loaded part 2----------------||----------------loaded part 3----------------|
				//
				// Case 3:
				// |---------------------------Unity render frame---------------------------|
				// |-------------------------------------------loaded part 1-------------------------------------------|

				// estimate how long would it take to load next part, if its over limit lets wait
				yieldPartsLoaded++;
				if ((float)yieldStopwatch.ElapsedMilliseconds / yieldPartsLoaded * (yieldPartsLoaded + 1) > Config.LoadInUnityThreadMaximumMilisecondsSpentPerFrame)
				{
					if (yieldPartsLoaded == 1 && yieldStopwatch.ElapsedMilliseconds > Config.LoadInUnityThreadMaximumMilisecondsSpentPerFrame)
					{
						// we loaded only 1 part, but the part it self is already over the limit, we should reduce load size
						var newCurrentDataLength = (int)(currentDataLength * Config.LoadInUnityThreadMaximumMilisecondsSpentPerFrame / (float)yieldStopwatch.ElapsedMilliseconds);

						if (newCurrentDataLength < Channels * 10) // lowest limit
							newCurrentDataLength = Channels * 10;

						ClampToMultiplierOf(ref newCurrentDataLength, Channels);
						currentDataLength = newCurrentDataLength;
					}

					yield return new WaitForEndOfFrame();
					yieldStopwatch.Reset();
					yieldStopwatch.Start();
					yieldPartsLoaded = 0;
				}

			} // end of while

			Config.FloatArrayPool.Return(currentData);

			if (ShouldAbortLoading)
			{
				if (allData != null) Config.FloatArrayPool.Return(allData);
				MyOnLoadingAborted();
				yield break;
			}

			if (!audioClipSetDataOffsetSupported)
			{
				AudioClip.SetData(allData, 0);
				Config.FloatArrayPool.Return(allData);
			}

			LoadState = AudioDataLoadState.Loaded;
			MyOnLoadingDone();
		}

		void LoadInBackgroundThread()
		{
			try
			{
				var loadingHasAborted = false;
				var currentDataLength = Config.LoadBufferSize;
				if (currentDataLength > SamplesCount) currentDataLength = SamplesCount;
				ClampToMultiplierOf(ref currentDataLength, Channels);

				float[] currentData;
				int currentOffset = 0;

				var raiseProgressChangedStopwatch = Stopwatch.StartNew();

				while (currentOffset < SamplesCount)
				{
					if (currentOffset + currentDataLength >= SamplesCount)
						currentDataLength = SamplesCount - currentOffset; // this is last part, will read less samples

					currentData = Config.FloatArrayPool.Rent(currentDataLength);

					reader.Read(currentData, 0, currentDataLength);

					if (loadingHasAborted)
					{
						Config.FloatArrayPool.Return(currentData);
						MyOnLoadingAborted();
						return;
					}

					var dataToSet = currentData;
					var setDataAtOffset = currentOffset / Channels;
					Config.UnityMainThreadRunner.Enqueue(() =>
					{
						if (ShouldAbortLoading) // here we are comparing UnityEngine.AudioClip to null, in older Unity version this is possible only on main Unity's thread
							loadingHasAborted = true;
						else
							AudioClip.SetData(dataToSet, setDataAtOffset);
						Config.FloatArrayPool.Return(dataToSet);
					});

					currentOffset += currentDataLength;
					SamplesLoaded = currentOffset;

					if (raiseProgressChangedStopwatch.ElapsedMilliseconds >= Config.RaiseProgressChangedEveryMiliseconds)
					{
						MyOnLoadProgressChanged();
						raiseProgressChangedStopwatch.Reset();
						raiseProgressChangedStopwatch.Start();
					}

				} // end of while

				if (loadingHasAborted)
				{
					MyOnLoadingAborted();
					return;
				}

				LoadState = AudioDataLoadState.Loaded;
				MyOnLoadingDone();

			}
			catch (Exception exception)
			{
				MyOnLoadingFailed(exception);
			}
		}

		void MyOnLoadProgressChanged()
		{
			if (OnLoadProgressChanged != null) OnLoadProgressChanged();
		}

		void MyOnLoadingDone()
		{
			SamplesLoaded = SamplesCount;
			MyOnLoadProgressChanged();
			DataWillNoLongerBeNeeded();
			if (OnLoadingDone != null) OnLoadingDone();
		}

		void MyOnLoadingAborted()
		{
			DataWillNoLongerBeNeeded();
			LoadState = AudioDataLoadState.Unloaded;
			if (OnLoadingAborted != null) OnLoadingAborted();
		}

		void MyOnLoadingFailed(Exception exception)
		{
			DataWillNoLongerBeNeeded();
			LoadState = AudioDataLoadState.Failed;
			if (OnLoadingFailed != null) OnLoadingFailed(exception);
			else UnityEngine.Debug.LogException(exception);
		}

		void DataWillNoLongerBeNeeded()
		{
			if (isDisposed) return;
			isDisposed = true;
			if (reader != null)
			{
				reader.Dispose();
				reader = null;
			}
			if (Config.DisposeDataStreamIfNotNeeded && Config.DataStream != null) Config.DataStream.Dispose();
		}

		/// <summary>
		/// Request loading to abort in nearest possible future.
		/// </summary>
		public void AbortLoading()
		{
			AbortLoadingRequested = true;
		}

		/// <summary>
		/// Aborts loading and destroys associated <see cref="AudioClip"/>.
		/// </summary>
		public void Destroy()
		{
			AbortLoading();
			if (AudioClip)
			{
				UnityEngine.AudioClip.Destroy(AudioClip);
				AudioClip = null;
			}
		}

		public static implicit operator AudioClip(AudioLoader self)
		{
			return self.AudioClip;
		}
	}
}