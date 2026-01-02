#if UNITY_EDITOR
#if UNITY_2019_1_OR_NEWER
#define UI_USE_FOLDOUT_HEADER_2019
#endif

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace SuperPivot
{
    public static class FoldableHeader
    {
        public static void OnEnable()
        {
            ms_CurrentFoldableHeader = null;
        }

        public static bool Begin(string label)
        {
            return Begin(label, label);
        }

        public static bool Begin(string label, string uniqueString)
        {
            if (ms_StyleHeaderFoldable == null)
            {
                ms_StyleHeaderFoldable = new GUIStyle(EditorStyles.foldout);
                ms_StyleHeaderFoldable.fontStyle = FontStyle.Bold;
            }

            ms_CurrentFoldableHeader = uniqueString;

            bool folded = ms_FoldedHeaders.Contains(uniqueString);

#if UI_USE_FOLDOUT_HEADER_2019
            folded = !EditorGUILayout.BeginFoldoutHeaderGroup(!folded, label);
#elif UNITY_5_5_OR_NEWER
            folded = !EditorGUILayout.Foldout(!folded, label, toggleOnLabelClick: true, style: ms_StyleHeaderFoldable);
#else
            folded = !EditorGUILayout.Foldout(!folded, label, ms_StyleHeaderFoldable);
#endif

            if (folded) ms_FoldedHeaders.Add(uniqueString);
            else ms_FoldedHeaders.Remove(uniqueString);

            return !folded;
        }

        public static void End()
        {
            Debug.Assert(ms_CurrentFoldableHeader != null, "Trying to call FoldableHeader.End() but there is no header opened");

            bool folded = ms_FoldedHeaders.Contains(ms_CurrentFoldableHeader);
            ms_CurrentFoldableHeader = null;

#if UI_USE_FOLDOUT_HEADER_2019
            EditorGUILayout.EndFoldoutHeaderGroup();
#endif
            if (!folded)
                EditorGUILayout.Separator();
        }

        static string ms_CurrentFoldableHeader = null;
        static HashSet<String> ms_FoldedHeaders = new HashSet<String>();
        static GUIStyle ms_StyleHeaderFoldable = null;
    }
}
#endif // UNITY_EDITOR

