using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RuntimeAudioClipLoaderDemo
{
	public class AudioGraph
	{
		int samplesCountToKeep;
		float seenMaxSampleValue = 0.01f;

		Queue<float> historySamples = new Queue<float>();

		AudioClip lastAudioClip;
		AudioSource lastAudioSource;
		float lastTime = -1;

		public void AddSamples(AudioSource audioSource)
		{
			var time = audioSource.time;
			if (audioSource != lastAudioSource)
			{
				lastAudioSource = audioSource;
				lastTime = -1;
			}

			if (lastTime == -1)
				lastTime = time;

			if (audioSource.clip)
			{
				var audioClip = audioSource.clip;
				var samplesPerSecond = audioClip.samples / audioClip.length;
				var dataLength = Math.Max(audioClip.channels, (int)((time - lastTime) * samplesPerSecond / audioClip.channels) * audioClip.channels);
				var data = new float[dataLength];
				lastAudioSource.GetOutputData(data, 0);
				lastTime = time;

				AddSamples(data);
			}
			else
			{
				AddSamples(new float[] { 0 });
			}
		}

		public void AddSamples(AudioClip audioClip, float time)
		{
			if (audioClip != lastAudioClip)
			{
				lastAudioClip = audioClip;
				lastTime = -1;
			}

			if (lastTime == -1)
				lastTime = time;

			var samplesPerSecond = audioClip.samples / audioClip.length;
			var dataLength = Math.Max(audioClip.channels, (int)((time - lastTime) * samplesPerSecond / audioClip.channels) * audioClip.channels);
			var data = new float[dataLength];
			audioClip.GetData(data, (int)(lastTime * samplesPerSecond));
			lastTime = time;

			AddSamples(data);
		}

		public void AddSamples(float[] samples)
		{
			var average = samples.Select(d => Mathf.Abs(d)).Average();
			historySamples.Enqueue(average);

			seenMaxSampleValue = Mathf.Max(seenMaxSampleValue, average);

			while (historySamples.Count > samplesCountToKeep)
				historySamples.Dequeue();
		}

		public void OnGUILayout()
		{
			var rect = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true));
			OnGUI(rect);
		}

		float lastSampleLerpedY = 0;

		public void OnGUI(Rect rect)
		{
			if (Event.current.type != EventType.Repaint)
				return;

			samplesCountToKeep = (int)rect.width;

			GUI.DrawTexture(rect, ColorFilledTexture2DFactory.Get(Color.black));

			if (historySamples.Count == 0)
				return;

			// last sample
			{
				var lastSample = historySamples.Last();
				var lerpedSampleLine = ColorFilledTexture2DFactory.Get(Color.red);
				var realSampleBar = ColorFilledTexture2DFactory.Get(new Color(0.7f, 0, 0));
				var lastSampleRealY = lastSample / seenMaxSampleValue;
				lastSampleLerpedY = Mathf.Lerp(lastSampleLerpedY, lastSampleRealY, Time.deltaTime * 4);
				lastSampleLerpedY = Mathf.Max(lastSampleLerpedY, lastSampleRealY);
				GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height * (1 - lastSampleLerpedY), 10, rect.height * lastSampleLerpedY), realSampleBar);
				GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height * (1 - lastSampleRealY) - 1, 10, 1), lerpedSampleLine);

				rect.x += 9; // move history to right
				rect.width -= 9;
			}

			// history
			{
				int xInt = Mathf.Min((int)rect.width, historySamples.Count);
				var sampleBar = ColorFilledTexture2DFactory.Get(Color.green);
				foreach (var sample in historySamples)
				{
					var x = xInt / (float)rect.width;
					var y = sample / seenMaxSampleValue;
					GUI.DrawTexture(new Rect(rect.x + rect.width * x, rect.y + rect.height * (1 - y), 1, rect.height * y), sampleBar);
					xInt--;
					if (xInt == 0)
						break;
				}
			}


		}

		public static class ColorFilledTexture2DFactory
		{
			static Dictionary<Color, Texture2D> cache = new Dictionary<Color, Texture2D>();
			public static Texture2D Get(Color color)
			{
				Texture2D result;
				if (!cache.TryGetValue(color, out result))
				{
					result = new Texture2D(1, 1);
					result.SetPixel(0, 0, color);
					result.Apply();
					cache[color] = result;
				}
				return result;
			}
		}
	}
}
