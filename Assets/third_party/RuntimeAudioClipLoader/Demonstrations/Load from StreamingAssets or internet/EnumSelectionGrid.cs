using System;
using System.Linq;
using UnityEngine;

namespace RuntimeAudioClipLoaderDemo
{
	[Serializable]
	public class EnumSelectionGrid<T>
	{
		public T value;

		public int selectedIndex = 0;

		public string[] texts;
		public T[] values;
		public EnumSelectionGrid()
		{
			texts = Enum.GetNames(typeof(T));
			values = Enum.GetValues(typeof(T)).OfType<T>().ToArray();
		}
		/// <summary>
		/// </summary>
		/// <returns>Returns true if value has changed.</returns>
		public bool OnGUI()
		{
			var newSelectedIndex = GUILayout.SelectionGrid(selectedIndex, texts, texts.Length, GUILayout.ExpandWidth(true));

			if (selectedIndex != newSelectedIndex)
			{
				selectedIndex = newSelectedIndex;
				value = values[selectedIndex];
				return true;
			}

			return false;
		}
	}
}