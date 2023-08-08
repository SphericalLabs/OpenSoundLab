using System.Collections;
using System.IO;
using RuntimeAudioClipLoader;
using UnityEngine;

namespace RuntimeAudioClipLoaderDemo
{
	[RequireComponent(typeof(AudioSource))]
	public class SimplestLoadFromInternet : MonoBehaviour
	{
		public string urlToLoad = @"http://www.neit.in/audio/Cannon-Believe_In.mp3";

		void Start()
		{
			StartCoroutine(Load());
		}

		IEnumerator Load()
		{
			var www = new WWW(urlToLoad);
			while (!www.isDone)
				yield return new WaitForEndOfFrame();

			var config = new AudioLoaderConfig();
			config.DataStream = new MemoryStream(www.bytes);

			var loader = new AudioLoader(config);
			loader.StartLoading();
			while (!loader.IsLoadingDone)
				yield return new WaitForEndOfFrame();

			var audioSource = GetComponent<AudioSource>();
			audioSource.clip = loader.AudioClip;
			audioSource.Play();
		}
	}
}