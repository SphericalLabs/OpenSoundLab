using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Linq;

namespace RuntimeAudioClipLoaderDemo
{
	/// <summary>
	/// On some platforms we are unable to list all files with Directory.GetFiles thus we need to get the list of all playable files beforehand.
	/// </summary>
	[CustomEditor(typeof(LoadFromStreamingAssetsOrInternet))]
	public class DemoStreamingAssetsEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var myScript = (LoadFromStreamingAssetsOrInternet)target;
			if (GUILayout.Button("Fill Files Paths"))
			{
				myScript.presetFiles = myScript.GetStreamingAssetsFiles();
			}
		}
	}

}