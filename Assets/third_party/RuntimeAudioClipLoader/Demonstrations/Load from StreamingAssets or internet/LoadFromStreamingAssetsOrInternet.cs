using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using RuntimeAudioClipLoader;

namespace RuntimeAudioClipLoaderDemo
{
	[RequireComponent(typeof(AudioSource))]
	public class LoadFromStreamingAssetsOrInternet : MonoBehaviour
	{
		public string urlToLoad = @"http://www.neit.in/audio/Cannon-Believe_In.mp3";

		static string rootFolder { get { return Application.streamingAssetsPath; } }
		static string sourceFolder { get { return Path.Combine(rootFolder, "RuntimeAudioClipLoader"); } }

		public string statusMessage;

		public EnumSelectionGrid<LoadMethod> loadMethod = new EnumSelectionGrid<LoadMethod>();
		public EnumSelectionGrid<PreferredDecoder> preferredDecoder = new EnumSelectionGrid<PreferredDecoder>();

		AudioSource audioSource;
		AudioGraph audioGraph = new AudioGraph();
		Coroutine lastCoroutine;

		void Start()
		{
			audioSource = GetComponent<AudioSource>();
		}

		bool directoryGetFilesThrewException = false;
		public string[] presetFiles;
		public string[] GetStreamingAssetsFiles()
		{
			// On some platforms we are unable to list all files with Directory.GetFiles due to security or architecture restrictions.
			// For those platforms we need to build the files list beforehand in editor.
			// Iam not sure where it's possible so we are trying it everywhere, if exception occurs, we stop trying.
			if (!directoryGetFilesThrewException)
			{
				try
				{
					return
						Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories)
						.Where(f => Manager.IsSupportedFormat(f))
						.Select(f => f.Substring(rootFolder.Length + 1))
						.ToArray();
				}
				catch
				{
					directoryGetFilesThrewException = true;
				}
			}
			return presetFiles;
		}


		AudioLoader lastAudioLoader;

		Stopwatch loadTimer;
		Vector3 scrollPos;
		void OnGUI()
		{
			GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
			scrollPos = GUILayout.BeginScrollView(scrollPos);
			{

				GUILayout.Label("Load from Streaming Assets:");

				var files = GetStreamingAssetsFiles();
				for (int i = 0; i < files.Length; i++)
				{
					var file = files[i];
					if (GUILayout.Button((i + 1) + ") Load: " + file))
					{
						var path = Path.Combine(rootFolder, file);
						if (!path.StartsWith("jar:") && !path.StartsWith("http:")) path = "file://" + path;
						StartLoading(path);
					}
				}

				GUILayout.Space(20);

				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("Load from URL:", GUILayout.ExpandWidth(false));
					urlToLoad = GUILayout.TextArea(urlToLoad);
					if (GUILayout.Button("Load from URL", GUILayout.ExpandWidth(false)))
						StartLoading(urlToLoad);
				}
				GUILayout.EndHorizontal();

				GUILayout.Space(20);

				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("LoadMethod:", GUILayout.ExpandWidth(false));
					if (loadMethod.OnGUI())
						RestartLoading();
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("PreferredDecoder:", GUILayout.ExpandWidth(false));
					if (preferredDecoder.OnGUI())
						RestartLoading();
				}
				GUILayout.EndHorizontal();

				GUILayout.Label("IsNativeMp3DecoderAvailable: " + RuntimeAudioClipLoader.Internal.AudioDataReader.IsNativeMp3DecoderAvailable);
				GUILayout.Label("IsManagedMp3DecoderAvailable: " + RuntimeAudioClipLoader.Internal.AudioDataReader.IsManagedMp3DecoderAvailable);
				GUILayout.Space(20);



				if (!string.IsNullOrEmpty(statusMessage))
					GUILayout.Label(statusMessage);

				if (lastAudioLoader != null)
				{
					if (lastAudioLoader.Config.LoadMethod != LoadMethod.StreamInUnityThread)
					{
						if (lastAudioLoader.LoadState == AudioDataLoadState.Loading)
						{
							statusMessage = "Loading elapsed " + loadTimer.Elapsed.TotalSeconds + " seconds";
						}
						else if (lastAudioLoader.LoadState == AudioDataLoadState.Loaded)
						{
							loadTimer.Stop();
							statusMessage = "Loaded in " + loadTimer.Elapsed.TotalSeconds + " seconds";
						}
					}

					GUILayout.Label("Load progress " + (int)(lastAudioLoader.LoadProgress * 100) + "% (" + (lastAudioLoader.LoadProgress * lastAudioLoader.AudioClip.length) + "/" + lastAudioLoader.AudioClip.length + " seconds)");
					GUILayout.Label("AudioClip.name: " + lastAudioLoader.AudioClip.name);
					GUILayout.Label("AudioClip.channels: " + lastAudioLoader.AudioClip.channels);
					GUILayout.Label("AudioClip.frequency: " + lastAudioLoader.AudioClip.frequency + " Hz");
					GUILayout.Label("AudioLoader.LoadState: " + lastAudioLoader.LoadState);
					GUILayout.Label("AudioLoader.UsedDecoder: " + lastAudioLoader.UsedDecoder);
					GUILayout.Label("AudioLoader.Config.LoadMethod: " + lastAudioLoader.Config.LoadMethod);

					GUILayout.Label("AudioClip.time: " + audioSource.time.ToString("0.000") + "/" + audioSource.clip.length.ToString("0.000"));
					if (audioSource.isPlaying)
					{
						if (GUILayout.Button("Stop")) audioSource.Stop();
					}
					else
					{
						if (GUILayout.Button("Play")) audioSource.Play();
					}
				}


				if (audioSource.clip)
					audioGraph.AddSamples(audioSource);
				audioGraph.OnGUILayout();

			}
			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		void StartLoading(string url)
		{
			if (lastCoroutine != null)
			{
				StopCoroutine("DownloadClipAndPlayFromUrl");
				lastCoroutine = null;
			}

			lastCoroutine = StartCoroutine(
				DownloadClipAndPlayFromUrl(url)
			);
		}

		void RestartLoading()
		{
			if (lastAudioLoader != null)
				StartLoading(lastAudioLoader.AudioClip.name);
		}

		// Unity unifies file system access in the WWW class, we can use the WWW class to get data both from internet and from local file system on any platform.
		IEnumerator DownloadClipAndPlayFromUrl(string url)
		{
			if (lastAudioLoader != null) lastAudioLoader.Destroy();
			lastAudioLoader = null;
			audioSource.clip = null;

			var isFromInternet = url.IndexOf("http", StringComparison.InvariantCultureIgnoreCase) != -1;

			statusMessage = "Getting data " + url;
			var www = new WWW(url);
			while (!www.isDone)
			{
				if (isFromInternet)
					statusMessage = "Downloading " + url + "\nDownload progress: " + (int)(www.progress * 100) + "%";
				yield return new WaitForEndOfFrame();
			}

			if (www.bytes.Length == 0)
			{
				statusMessage = "Error: downloaded zero bytes from " + url;
			}
			else
			{
				statusMessage = "Loading " + url;

				if (loadMethod.value == LoadMethod.StreamInUnityThread) statusMessage = "Streaming";
				else loadTimer = Stopwatch.StartNew();

				var config = new AudioLoaderConfig();
				config.DataStream = new MemoryStream(www.bytes);
				config.PreferredDecoder = preferredDecoder.value;
				config.UnityAudioClipName = url;
				config.LoadMethod = loadMethod.value;

				lastAudioLoader = new AudioLoader(config);
				lastAudioLoader.OnLoadingAborted += () => statusMessage = "Loading aborted.";
				lastAudioLoader.OnLoadingDone += () => statusMessage = "Loading done.";
				lastAudioLoader.OnLoadingFailed += (exception) => statusMessage = "Loading has failed: " + exception.Message;
				audioSource.clip = lastAudioLoader.AudioClip;
				lastAudioLoader.StartLoading();		
			}
		}
	}
}