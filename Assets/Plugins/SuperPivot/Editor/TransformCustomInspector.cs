using UnityEngine;
using UnityEditor;

namespace SuperPivot
{
    [CanEditMultipleObjects, CustomEditor(typeof(Transform))]
    public class TransformCustomInspector : Editor
    {
        private const float LABEL_WIDTH_LEFT = 75.0f;
        private const bool WIDE_MODE = true;

        private const float POSITION_MAX = 100000.0f;

        private static GUIContent positionGUIContent = new GUIContent(LocalString("Position")
            , LocalString("The local position of this Game Object relative to the parent."));
        private static GUIContent rotationGUIContent = new GUIContent(LocalString("Rotation")
            , LocalString("The local rotation of this Game Object relative to the parent."));
        private static GUIContent scaleGUIContent = new GUIContent(LocalString("Scale")
            , LocalString("The local scaling of this Game Object relative to the parent."));

        private static string positionWarningText = LocalString("Due to floating-point precision limitations, it is recommended to bring the world coordinates of the GameObject within a smaller range.");

        private SerializedProperty m_PropPosition;
        private SerializedProperty m_PropRotation;
        private SerializedProperty m_PropScale;
        private SerializedProperty m_PropConstrainScale;

        private Vector3 m_ConstrainedScale = Vector3.one;
        private Vector3 constrainedScale
        {
            get { return m_ConstrainedScale; }
            set
            {
                bool foundZero = false;
                for (int i = 0; i < 3; i++)
                {
                    if (Mathf.Approximately(Mathf.Abs(value[i]), 0.0f))
                    {
                        foundZero = true;
                        break;
                    }
                }

                m_ConstrainedScale = foundZero ? Vector3.one : value;
            }
        }

        private static string LocalString(string text)
        {
#if UNITY_2017_3_OR_NEWER
            return text;
#else
            return LocalizationDatabase.GetLocalizedString(text);
#endif
        }

        void OnEnable()
        {
            m_PropPosition = serializedObject.FindProperty("m_LocalPosition");
            m_PropRotation = serializedObject.FindProperty("m_LocalRotation");
            m_PropScale = serializedObject.FindProperty("m_LocalScale");
            m_PropConstrainScale = serializedObject.FindProperty("m_ConstrainProportionsScale");
            constrainedScale = m_PropScale.vector3Value;
        }

        public override void OnInspectorGUI()
        {
            EditorAPI.Instance.OnWindowUpdate();

            EditorGUIUtility.wideMode = WIDE_MODE;
            EditorGUIUtility.labelWidth = LABEL_WIDTH_LEFT;

            serializedObject.Update();

            PositionPropertyField();
            RotationPropertyField();
            ScalePropertyField();

            if (!ValidatePosition(((Transform)this.target).position))
            {
                EditorGUILayout.HelpBox(positionWarningText, MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();

            // ================================================================
            // SUPER PIVOT
            var selectedTransforms = EditorAPI.GetSelectedTransforms();
            EditorGUILayout.Separator();
            EditorAPI.Instance.DrawGUI(selectedTransforms, true);
            // ================================================================
        }

        static GUIContent m_ConstrainContentLinked = null;
        static GUIContent constrainContentLinked
        {
            get
            {
                if (m_ConstrainContentLinked == null)
                    m_ConstrainContentLinked = EditorGUIUtility.IconContent("Linked", "|Disable constrained proportions");
                return m_ConstrainContentLinked;
            }
        }

        static GUIContent m_ConstrainContentUnlinked = null;
        static GUIContent constrainContentUnlinked
        {
            get
            {
                if (m_ConstrainContentUnlinked == null)
                    m_ConstrainContentUnlinked = EditorGUIUtility.IconContent("Unlinked", "|Enable constrained proportions");
                return m_ConstrainContentUnlinked;
            }
        }

        static GUIStyle m_ConstrainStyle = null;
        static GUIStyle constrainStyle
        {
            get
            {
                if (m_ConstrainStyle == null)
                {
                    m_ConstrainStyle = new GUIStyle(EditorStyles.label);
                    m_ConstrainStyle.alignment = TextAnchor.MiddleCenter;
                }
                return m_ConstrainStyle;
            }
        }

        private struct DrawVector3Result
        {
            public bool changedConstrain { get; private set; }
            private bool[] m_ChangedField;
            private Vector3 m_NewVec;

            public bool hasVectorChanged
            {
                get
                {
                    foreach (bool value in m_ChangedField)
                    {
                        if (value)
                            return true;
                    }
                    return false;
                }
            }

            public DrawVector3Result(Vector3 vec, bool[] fields, bool constrain)
            {
                Debug.Assert(fields != null);
                Debug.Assert(fields.Length == 3);
                m_NewVec = vec;
                m_ChangedField = fields;
                changedConstrain = constrain;
            }

            public Vector3 GetModifiedVector(Vector3 vec)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (m_ChangedField[i])
                        vec[i] = m_NewVec[i];
                }

                return vec;
            }
        }

        private static DrawVector3Result DrawVector3(GUIContent label, Vector3[] vecValues, SerializedProperty constrainProp, Vector3? constrainedScale)
        {
            bool constrainChanged = false;
            bool[] changedFields = new bool[3];

            EditorGUILayout.BeginHorizontal();

            EditorGUIUtility.labelWidth = constrainProp == null ? LABEL_WIDTH_LEFT : LABEL_WIDTH_LEFT - 28;
            EditorGUILayout.PrefixLabel(label);

            bool isContrained = false;
            if (constrainProp != null)
            {
                isContrained = constrainProp.boolValue;

                if (GUILayout.Button(isContrained ? constrainContentLinked : constrainContentUnlinked, constrainStyle, GUILayout.MaxWidth(25)))
                {
                    isContrained = !isContrained;
                    constrainProp.boolValue = isContrained;
                    constrainChanged = true;
                }
            }

            EditorGUIUtility.labelWidth = 13.0f;

            bool DoAllVectorsHaveSameFieldValue(Vector3 refVec, int fieldIdx)
            {
                foreach (var testedVec in vecValues)
                {
                    if (!Mathf.Approximately(testedVec[fieldIdx], refVec[fieldIdx]))
                    {
                        return false;
                    }
                }

                return true;
            }

            void DrawField(string fieldLabel, ref Vector3 refVec, int fieldIdx)
            {
                EditorGUI.showMixedValue = !DoAllVectorsHaveSameFieldValue(refVec, fieldIdx);

                EditorGUI.BeginChangeCheck();
                refVec[fieldIdx] = EditorGUILayout.FloatField(fieldLabel, refVec[fieldIdx]);
                if (EditorGUI.EndChangeCheck())
                {
                    changedFields[fieldIdx] = true;

                    if (isContrained && constrainedScale.HasValue)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            if (fieldIdx != i && !Mathf.Approximately(Mathf.Abs(constrainedScale.Value[fieldIdx]), 0.0f)) // constrainedScale should have no component with 0, but testing it just in case
                            {
                                float constrainedRatio = constrainedScale.Value[i] / constrainedScale.Value[fieldIdx];
                                refVec[i] = constrainedRatio * refVec[fieldIdx];
                                changedFields[i] = true; // do not forget to flag fields changed by scale constain
                            }
                        }
                    }
                }
            }

            var modifiedVec = vecValues[0];
            DrawField("X", ref modifiedVec, 0);
            DrawField("Y", ref modifiedVec, 1);
            DrawField("Z", ref modifiedVec, 2);

            EditorGUIUtility.labelWidth = 0.0f;
            EditorGUILayout.EndHorizontal();

            EditorGUI.showMixedValue = false;

            return new DrawVector3Result(modifiedVec, changedFields, constrainChanged);
        }

        private bool ValidatePosition(Vector3 position)
        {
            if (Mathf.Abs(position.x) > POSITION_MAX) return false;
            if (Mathf.Abs(position.y) > POSITION_MAX) return false;
            if (Mathf.Abs(position.z) > POSITION_MAX) return false;
            return true;
        }

        private void PositionPropertyField()
        {
            var allPos = new Vector3[this.targets.Length];
            for (int i = 0; i < this.targets.Length; i++)
            {
                var t = (Transform)(this.targets[i]);
                allPos[i] = t.localPosition;
            }

            var changeResult = DrawVector3(positionGUIContent, allPos, null, null);
            if (changeResult.hasVectorChanged)
            {
                Undo.RecordObjects(this.targets, "Position Changed");
                foreach (UnityEngine.Object obj in this.targets)
                {
                    Transform t = (Transform)obj;
                    t.localPosition = changeResult.GetModifiedVector(t.localPosition);
                }
                m_PropPosition.serializedObject.SetIsDifferentCacheDirty();
            }
        }

        private void ScalePropertyField()
        {
            var allScale = new Vector3[this.targets.Length];
            for (int i = 0; i < this.targets.Length; i++)
            {
                var t = (Transform)(this.targets[i]);
                allScale[i] = t.localScale;
            }

            var changeResult = DrawVector3(scaleGUIContent, allScale, m_PropConstrainScale, constrainedScale);
            if (changeResult.hasVectorChanged)
            {
                Undo.RecordObjects(this.targets, "Scale Changed");
                foreach (UnityEngine.Object obj in this.targets)
                {
                    Transform t = (Transform)obj;
                    t.localScale = changeResult.GetModifiedVector(t.localScale);
                }
                m_PropScale.serializedObject.SetIsDifferentCacheDirty();
            }

            if (changeResult.changedConstrain && m_PropConstrainScale.boolValue)
                constrainedScale = m_PropScale.vector3Value;
        }

        private void RotationPropertyField()
        {
            var allRot = new Vector3[this.targets.Length];
            for (int i = 0; i < this.targets.Length; i++)
            {
                var t = (Transform)(this.targets[i]);
                allRot[i] = TransformUtils.GetInspectorRotation(t);
            }

            var changeResult = DrawVector3(rotationGUIContent, allRot, null, null);
            if (changeResult.hasVectorChanged)
            {
                Undo.RecordObjects(this.targets, "Rotation Changed");
                foreach (UnityEngine.Object obj in this.targets)
                {
                    Transform t = (Transform)obj;
                    var currentRot = TransformUtils.GetInspectorRotation(t);
                    var changedRot = changeResult.GetModifiedVector(currentRot);
                    TransformUtils.SetInspectorRotation(t, changedRot);
                }
                m_PropRotation.serializedObject.SetIsDifferentCacheDirty();
            }
        }

        private bool SameVector3(Vector3 vec1, Vector3 vec2)
        {
            if (vec1.x != vec2.x) return false;
            if (vec1.y != vec2.y) return false;
            if (vec1.z != vec2.z) return false;
            return true;
        }

        private bool SameRotation(Quaternion rot1, Quaternion rot2)
        {
            if (rot1.x != rot2.x) return false;
            if (rot1.y != rot2.y) return false;
            if (rot1.z != rot2.z) return false;
            if (rot1.w != rot2.w) return false;
            return true;
        }
    }
}

