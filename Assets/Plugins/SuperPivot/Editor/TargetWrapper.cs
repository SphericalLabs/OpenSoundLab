//#define TAKE_ACCOUNT_POINT_ENTITIES

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace SuperPivot
{
    internal class TargetWrapper
    {
        public enum Component { X=0, Y=1, Z=2 }
        public Transform transform { get; private set; }
        public string name { get { return transform.name; } }

        Vector3 m_CachedPosition;
        Quaternion m_CachedRotation;
        Vector3 m_CachedScale;

        public TargetWrapper(Transform t)
        {
            Debug.Assert(t != null);
            transform = t;
            UpdateTargetCachedData();
        }

        public void UpdateTargetCachedData()
        {
            Debug.Assert(transform);
            m_CachedPosition = transform.position;
            m_CachedRotation = transform.rotation;
            m_CachedScale = transform.localScale;
        }

        public bool TargetTransformHasChanged()
        {
            Debug.Assert(transform);
            return transform.position != m_CachedPosition
                || transform.rotation != m_CachedRotation
                || transform.localScale != m_CachedScale;
        }

        public void SetPivotPosition(Vector3 pos, API.Space space)
        {
            API.SetPivotPosition(transform, pos, space);
        }

        public void SetPivotPosition(Component comp, float value, API.Space space)
        {
            Debug.Assert(transform, "Invalid target entity");
            var pivotPos = transform.GetPivotPosition(space);
            pivotPos[(int)comp] = value;
            API.SetPivotPosition(transform, pivotPos, space);
        }

        public void SetPivotRotation(Vector3 euler, API.Space space)
        {
            API.SetPivotRotation(transform, euler, space);
        }

        public void SetPivotRotation(Quaternion quat, API.Space space)
        {
            API.SetPivotRotation(transform, quat, space);
        }

        static float InverseLerpUnclamped(float from, float to, float value)
        {
            if (from == to) return 0.5f;
            return (value - from) / (to - from);
        }

        static bool GUIButtonZero()
        {
            var buttonStyle = new GUIStyle(EditorStyles.miniButton);
            buttonStyle.fixedWidth = 40f;
            return GUILayout.Button("Zero", buttonStyle);
        }

        public void GUIPositions()
        {
            if (FoldableHeader.Begin("Position"))
            {
                GUIPosition(API.Space.Global);
                GUIPosition(API.Space.Local);
            }
            FoldableHeader.End();
        }

        void GUIPosition(API.Space space)
        {
            if (space == API.Space.Global || transform.parent != null)
            {
                var str = (space == API.Space.Global) ? "World Position" : string.Format("Local Position (relative to '{0}')", transform.parent.name);
                EditorGUILayout.LabelField(str, EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    var newPos = EditorGUILayout.Vector3Field("", space == API.Space.Global ? transform.position : transform.localPosition);
                    if (EditorGUI.EndChangeCheck())
                        SetPivotPosition(newPos, space);

                    if (GUIButtonZero())
                        SetPivotPosition(Vector3.zero, space);
                }
            }
        }

        public bool CanBeRotated()
        {
            var scale = transform.localScale;
            return Mathf.Approximately(scale.x, scale.y) && Mathf.Approximately(scale.x, scale.z);
        }

        public void GUIRotations()
        {
            if (FoldableHeader.Begin("Rotation"))
            {
                if (CanBeRotated())
                {
                    GUIRotation(API.Space.Global);
                    GUIRotation(API.Space.Local);
                }
                else
                {
                    EditorGUILayout.HelpBox("You cannot change the pivot rotation of this GameObject because its local scale in uneven.", MessageType.Warning);
                }
            }
            FoldableHeader.End();
        }

        void GUIRotation(API.Space space)
        {
            if (space == API.Space.Global || transform.parent != null)
            {
                var str = (space == API.Space.Global) ? "World Rotation" : string.Format("Local Rotation (relative to '{0}')", transform.parent.name);
                EditorGUILayout.LabelField(str, EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    var newRot = EditorGUILayout.Vector3Field("", space == API.Space.Global ? transform.rotation.eulerAngles : transform.localRotation.eulerAngles);
                    if (EditorGUI.EndChangeCheck())
                        SetPivotRotation(newRot, space);

                    if (GUIButtonZero())
                        SetPivotRotation(Vector3.zero, space);
                }
            }
        }
    }
}
