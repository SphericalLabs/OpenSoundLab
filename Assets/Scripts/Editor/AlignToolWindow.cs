using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class AlignToolWindow : EditorWindow
{
    private const float ButtonSize = 40f;
    private const float Padding = 5f;
    private const float IconPadding = 8f;

    private enum ReferenceMode
    {
        SelectionBounds,
        ActiveObject, // Last selected
        FirstObject   // First in array (Hierarchy/Index 0)
    }

    private ReferenceMode currentReferenceMode = ReferenceMode.SelectionBounds;
    private readonly string[] referenceModeLabels = { "Selection Bounds", "Active (Last)", "First (Index 0)" };

    [MenuItem("Tools/Align Tool")]
    public static void ShowWindow()
    {
        GetWindow<AlignToolWindow>("Align Tool");
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical("box");

        GUILayout.Space(Padding);
        GUILayout.Label("Reference Mode", EditorStyles.boldLabel);
        currentReferenceMode = (ReferenceMode)GUILayout.Toolbar((int)currentReferenceMode, referenceModeLabels);

        GUILayout.Space(Padding * 2);
        GUILayout.Label("Alignment", EditorStyles.boldLabel);

        // X Axis Alignment
        GUILayout.BeginHorizontal();
        DrawAlignButton(Axis.X, AlignMode.Min);
        DrawAlignButton(Axis.X, AlignMode.Center);
        DrawAlignButton(Axis.X, AlignMode.Max);
        GUILayout.EndHorizontal();

        // Y Axis Alignment
        GUILayout.BeginHorizontal();
        DrawAlignButton(Axis.Y, AlignMode.Min);
        DrawAlignButton(Axis.Y, AlignMode.Center);
        DrawAlignButton(Axis.Y, AlignMode.Max);
        GUILayout.EndHorizontal();

        // Z Axis Alignment
        GUILayout.BeginHorizontal();
        DrawAlignButton(Axis.Z, AlignMode.Min);
        DrawAlignButton(Axis.Z, AlignMode.Center);
        DrawAlignButton(Axis.Z, AlignMode.Max);
        GUILayout.EndHorizontal();

        GUILayout.Space(Padding * 2);
        GUILayout.Label("Selection Bounds works well, other two modes are a bit unpredictable. Local mode removed for now.", EditorStyles.wordWrappedLabel);

        GUILayout.EndVertical();
    }

    private enum Axis { X, Y, Z }
    private enum AlignMode { Min, Center, Max }

    private void DrawAlignButton(Axis axis, AlignMode mode)
    {
        if (GUILayout.Button("", GUILayout.Width(ButtonSize), GUILayout.Height(ButtonSize)))
        {
            AlignSelectedObjects(axis, mode);
        }

        if (Event.current.type == EventType.Repaint)
        {
            Rect btnRect = GUILayoutUtility.GetLastRect();
            DrawButtonIcon(btnRect, axis, mode);
        }
    }

    private void DrawButtonIcon(Rect rect, Axis axis, AlignMode mode)
    {
        // Color based on axis
        Color axisColor = Color.white;
        switch (axis)
        {
            case Axis.X: axisColor = new Color(1f, 0.4f, 0.4f); break; // Red-ish
            case Axis.Y: axisColor = new Color(0.4f, 1f, 0.4f); break; // Green-ish
            case Axis.Z: axisColor = new Color(0.4f, 0.4f, 1f); break; // Blue-ish
        }

        // If using an anchor mode, maybe allow visual distinction?
        // For now, keep the clean icons.

        Handles.BeginGUI();
        Color oldColor = Handles.color;
        Handles.color = axisColor;

        // Draw Guide Line
        if (axis == Axis.X || axis == Axis.Z) // Vertical bars logic
        {
            float xPos = 0;
            switch(mode)
            {
                case AlignMode.Min: xPos = rect.x + IconPadding; break;
                case AlignMode.Center: xPos = rect.center.x; break;
                case AlignMode.Max: xPos = rect.xMax - IconPadding; break;
            }

            // Draw Line
            EditorGUI.DrawRect(new Rect(xPos - 1, rect.y + IconPadding, 2, rect.height - 2 * IconPadding), Color.gray);

            // Draw "Objects" (Bars)
            DrawBarX(rect, xPos, mode, 0.3f, 12f);
            DrawBarX(rect, xPos, mode, 0.5f, 16f);
            DrawBarX(rect, xPos, mode, 0.7f, 10f);
        }
        else if (axis == Axis.Y) // Horizontal bars logic
        {
            float yPos = 0;
            switch(mode)
            {
                case AlignMode.Min: yPos = rect.yMax - IconPadding; break; // Bottom
                case AlignMode.Center: yPos = rect.center.y; break;
                case AlignMode.Max: yPos = rect.y + IconPadding; break; // Top
            }

            // Draw Line
            EditorGUI.DrawRect(new Rect(rect.x + IconPadding, yPos - 1, rect.width - 2 * IconPadding, 2), Color.gray);

            // Draw "Objects"
            DrawBarY(rect, yPos, mode, 0.3f, 12f);
            DrawBarY(rect, yPos, mode, 0.5f, 16f);
            DrawBarY(rect, yPos, mode, 0.7f, 10f);
        }

        Handles.color = oldColor;
        Handles.EndGUI();
    }

    private void DrawBarX(Rect btnRect, float alignX, AlignMode mode, float vPct, float width)
    {
        // vPct from 0 to 1 (vertical position)
        float h = 6f; // height of bar
        float availableH = btnRect.height - 2 * IconPadding - h;
        float y = btnRect.y + IconPadding + availableH * vPct;

        float x = 0;
        switch(mode)
        {
            case AlignMode.Min: x = alignX; break;
            case AlignMode.Center: x = alignX - width / 2; break;
            case AlignMode.Max: x = alignX - width; break;
        }

        EditorGUI.DrawRect(new Rect(x, y, width, h), Handles.color);
    }

    private void DrawBarY(Rect btnRect, float alignY, AlignMode mode, float hPct, float height)
    {
        // hPct from 0 to 1 (horizontal position)
        float w = 6f;
        float availableW = btnRect.width - 2 * IconPadding - w;
        float x = btnRect.x + IconPadding + availableW * hPct;

        float drawY = 0;
        switch(mode)
        {
            case AlignMode.Min: drawY = alignY - height; break; // Above the line (bottom align means line is at bottom)
            case AlignMode.Center: drawY = alignY - height / 2; break;
            case AlignMode.Max: drawY = alignY; break; // Below the line (top align means line is at top)
        }

        EditorGUI.DrawRect(new Rect(x, drawY, w, height), Handles.color);
    }

    private void AlignSelectedObjects(Axis axis, AlignMode mode)
    {
        GameObject[] rawSelection = Selection.gameObjects;
        if (rawSelection.Length < 1) return;

        // Filter out objects that are children of other selected objects to avoid double-moving
        var selection = rawSelection.Where(g => !rawSelection.Any(p => p != g && g.transform.IsChildOf(p.transform))).ToArray();
        if (selection.Length == 0) return;

        Undo.RecordObjects(selection.Select(g => g.transform).ToArray(), "Align Objects");

        // Calculate target value based on Reference Mode
        float targetValue = 0f;

        if (currentReferenceMode == ReferenceMode.SelectionBounds)
        {
            // Calculate absolute Min/Max/Center of the entire selection block
            float min = float.MaxValue;
            float max = float.MinValue;

            foreach(GameObject go in selection)
            {
                Bounds b = GetBounds(go.transform);
                float valMin = 0, valMax = 0;

                switch(axis)
                {
                    case Axis.X: valMin = b.min.x; valMax = b.max.x; break;
                    case Axis.Y: valMin = b.min.y; valMax = b.max.y; break;
                    case Axis.Z: valMin = b.min.z; valMax = b.max.z; break;
                }

                if (valMin < min) min = valMin;
                if (valMax > max) max = valMax;
            }

            switch(mode)
            {
                case AlignMode.Min: targetValue = min; break;
                case AlignMode.Max: targetValue = max; break;
                case AlignMode.Center: targetValue = (min + max) / 2f; break;
            }
        }
        else
        {
            // Align to specific object key anchor
            GameObject referenceObj = null;

            if (currentReferenceMode == ReferenceMode.ActiveObject)
            {
                referenceObj = Selection.activeGameObject;
                // Fallback if active is not in filtered selection?
                // It should be fine to align TO it even if it's a child (though odd).
                // But usually Active is part of selection.
                if (referenceObj == null) referenceObj = selection.LastOrDefault();
            }
            else if (currentReferenceMode == ReferenceMode.FirstObject)
            {
                referenceObj = rawSelection.FirstOrDefault(); // Use raw selection order
                // Or selection[0]? Raw is safer for "click order" assumption if valid.
            }

            if (referenceObj == null) return;

            Bounds refBounds = GetBounds(referenceObj.transform);

            switch(axis)
            {
                case Axis.X:
                    if (mode == AlignMode.Min) targetValue = refBounds.min.x;
                    else if (mode == AlignMode.Max) targetValue = refBounds.max.x;
                    else targetValue = refBounds.center.x;
                    break;
                case Axis.Y:
                    if (mode == AlignMode.Min) targetValue = refBounds.min.y;
                    else if (mode == AlignMode.Max) targetValue = refBounds.max.y;
                    else targetValue = refBounds.center.y;
                    break;
                case Axis.Z:
                    if (mode == AlignMode.Min) targetValue = refBounds.min.z;
                    else if (mode == AlignMode.Max) targetValue = refBounds.max.z;
                    else targetValue = refBounds.center.z;
                    break;
            }
        }

        // Apply
        foreach(GameObject go in selection)
        {
            Transform t = go.transform;
            Bounds b = GetBounds(t);

            float currentVal = 0;

            switch(axis)
            {
                case Axis.X:
                    if (mode == AlignMode.Min) currentVal = b.min.x;
                    else if (mode == AlignMode.Max) currentVal = b.max.x;
                    else currentVal = b.center.x;

                    t.position += new Vector3(targetValue - currentVal, 0, 0);
                    break;

                case Axis.Y:
                     if (mode == AlignMode.Min) currentVal = b.min.y;
                    else if (mode == AlignMode.Max) currentVal = b.max.y;
                    else currentVal = b.center.y;

                    t.position += new Vector3(0, targetValue - currentVal, 0);
                    break;

                case Axis.Z:
                     if (mode == AlignMode.Min) currentVal = b.min.z;
                    else if (mode == AlignMode.Max) currentVal = b.max.z;
                    else currentVal = b.center.z;

                    t.position += new Vector3(0, 0, targetValue - currentVal);
                    break;
            }
        }
    }

    private Bounds GetBounds(Transform t)
    {
        Renderer r = t.GetComponent<Renderer>();
        if (r != null) return r.bounds;

        // If no renderer, check children
        Renderer[] childRenderers = t.GetComponentsInChildren<Renderer>();
        if (childRenderers.Length > 0)
        {
             Bounds b = childRenderers[0].bounds;
             for(int i=1; i<childRenderers.Length; i++)
                 b.Encapsulate(childRenderers[i].bounds);
             return b;
        }

        // Fallback to position - make a small bound
        return new Bounds(t.position, Vector3.zero);
    }
}
