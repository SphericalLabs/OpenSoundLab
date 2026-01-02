using UnityEngine;
using UnityEditor;

namespace SuperPivot
{
    internal class Window : EditorWindow
    {
        static Window m_Window;

        [MenuItem("Tools/Super Pivot", false)]
        static void OpenWindow()
        {
            m_Window = EditorWindow.GetWindow<Window>();
            EditorAPI.Instance.StopMove();
            if (m_Window)
            {
                m_Window.titleContent = new GUIContent("Super Pivot");
                m_Window.Show();
            }
        }

        void Update()
        {
            EditorAPI.Instance.OnWindowUpdate();
        }

        void OnGUI()
        {
            if (m_Window == null)
                OpenWindow();

            var selectedTransforms = EditorAPI.GetSelectedTransforms();
            EditorAPI.Instance.DrawGUI(selectedTransforms, false);
        }

        void RepaintWindow()
        {
            if (m_Window)
                m_Window.Repaint();
        }

        void OnFocus()
        {
            EditorAPI.Instance.askToRepaintWindowDelegate -= RepaintWindow;
            EditorAPI.Instance.askToRepaintWindowDelegate += RepaintWindow;
        }

        void OnDestroy()
        {
            EditorAPI.Instance.askToRepaintWindowDelegate -= RepaintWindow;
        }
    }
}
