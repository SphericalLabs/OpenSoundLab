using UnityEngine;

namespace SuperPivot
{
    public static class API
    {
        public const float Version = 2.6f;

        public enum Space
        {
            /// <summary>
            /// The pivot position is expressed in World space
            /// </summary>
            Global,

            /// <summary>
            /// The pivot position is expressed in Local space
            /// </summary>
            Local
        }

    
        /// <summary>
        /// Return the pivot position of the Transform object
        /// </summary>
        /// <param name="space">Specify if you want the pivot position in World or Local space</param>
        /// <returns>The pivot position</returns>
        public static Vector3 GetPivotPosition(this Transform self, Space space = Space.Global)
        {
            return (space == Space.Global) ? self.position : self.localPosition;
        }

        /// <summary>
        /// Return the pivot rotation of the Transform object
        /// </summary>
        /// <param name="space">Specify if you want the pivot rotation in World or Local space</param>
        /// <returns>The pivot rotation</returns>
        public static Quaternion GetPivotRotation(this Transform self, Space space = Space.Global)
        {
            return (space == Space.Global) ? self.rotation : self.localRotation;
        }

        /// <summary>
        /// Returns if you are allowed to change the pivot position of 'target' Transform
        /// Please note that this function is quite slow, so do not call it per frame.
        /// </summary>
        /// <param name="target">The Transform object you want to edit</param>
        /// <returns>true if you can change its pivot position, false otherwise</returns>
        public static bool CanChangePivot(Transform target)
        {
            string reason;
            return CanChangePivotPrivate(target, out reason) == CanChangePivotResult.Yes;
        }

        /// <summary>
        /// Returns if you are allowed to change the pivot position of 'target' Transform
        /// Please note that this function is quite slow, so do not call it per frame.
        /// </summary>
        /// <param name="target">The Transform object you want to edit</param>
        /// <param name="reason">A message explaining why you cannot edit the pivot, null otherwise</param>
        /// <returns>true if you can change its pivot position, false otherwise</returns>
        public static bool CanChangePivot(Transform target, out string reason)
        {
            return CanChangePivotPrivate(target, out reason) == CanChangePivotResult.Yes;
        }

        public static bool HasAtLeastOneTransformWithResult(Transform[] targets, out string reason, params CanChangePivotResult[] validResults)
        {
            reason = "UNKOWN";
            foreach (var t in targets)
            {
                var res = CanChangePivotPrivate(t, out reason);
                foreach (var validResult in validResults)
                {
                    if (res == validResult)
                        return true;
                }
            }
            return false;
        }


        public enum CanChangePivotResult
        {
            Yes,
            No,
            NoButShowMoveButton
        }

        static CanChangePivotResult CanChangePivotPrivate(Transform target, out string reason)
        {
            if (target == null)
            {
                reason = string.Format("The target Transform is null");
                return CanChangePivotResult.No;
            }

            if (target.childCount == 0)
            {
                reason = string.Format("'{0}' is not a group, since it has no children", target.name);
                return CanChangePivotResult.No;
            }

            var renderer = target.GetComponent<Renderer>();
            if (renderer)
            {
                reason = string.Format("Can't edit '{0}' because it has a '{1}' attached", target.name, renderer.GetType());
                return CanChangePivotResult.No;
            }

            if (target.GetComponent<Terrain>())
            {
                reason = string.Format("Can't edit '{0}' because it has a Terrain attached", target.name);
                return CanChangePivotResult.No;
            }

            if (target.GetComponent<Camera>())
            {
                reason = string.Format("Can't edit '{0}' because it has a Camera attached", target.name);
                return CanChangePivotResult.No;
            }

#if UNITY_EDITOR && UNITY_2018_3_OR_NEWER
            bool isPrefabInstance = UnityEditor.PrefabUtility.IsPartOfAnyPrefab(target);
            if (isPrefabInstance)
            {
                reason = string.Format("'{0}' is part of a prefab instance and Unity doesn't allow to modify a pivot through a prefab instance.\nBut you can open the prefab asset and modify its pivot in isolation mode.", target.name);
                return CanChangePivotResult.NoButShowMoveButton;
            }
#endif
            reason = null;
            return CanChangePivotResult.Yes;
        }

        /// <summary>
        /// Change the pivot position of "target"
        /// </summary>
        /// <param name="target">The GameObject you want to modify</param>
        /// <param name="pos">The new pivot position</param>
        /// <param name="space">Is the new pivot position expressed in World or Local space?</param>
        public static void SetPivotPosition(Transform target, Vector3 pos, Space space = Space.Global)
        {
            Debug.Assert(target, "Invalid target entity");
            if (target.GetPivotPosition(space) == pos)
                return;

            Matrix4x4 oldToNewMatrix;
            var children = OnChangePivotBegin(target, out oldToNewMatrix);

            SetPositionPrivate(target, pos, space);

            OnChangePivotEnd(target, children, oldToNewMatrix);
        }

        [System.Obsolete("Use 'SuperPivot.API.SetPivotPosition' instead")]
        public static void SetPivot(Transform target, Vector3 pos, Space space = Space.Global)
        {
            SetPivotPosition(target, pos, space);
        }

        /// <summary>
        /// Change the pivot rotation of "target"
        /// </summary>
        /// <param name="target">The GameObject you want to modify</param>
        /// <param name="euler">The new pivot rotation euler angles</param>
        /// <param name="space">Is the new pivot rotation expressed in World or Local space?</param>
        public static void SetPivotRotation(Transform target, Vector3 euler, Space space = Space.Global)
        {
            Debug.Assert(target, "Invalid target entity");
            if (target.GetPivotRotation(space).eulerAngles == euler)
               return;

            Matrix4x4 oldToNewMatrix;
            var children = OnChangePivotBegin(target, out oldToNewMatrix);

            SetRotationPrivate(target, euler, space);

            OnChangePivotEnd(target, children, oldToNewMatrix);
        }

        /// <summary>
        /// Change the pivot rotation of "target"
        /// </summary>
        /// <param name="target">The GameObject you want to modify</param>
        /// <param name="quat">The new pivot rotation quaternion</param>
        /// <param name="space">Is the new pivot rotation expressed in World or Local space?</param>
        public static void SetPivotRotation(Transform target, Quaternion quat, Space space = Space.Global)
        {
            Debug.Assert(target, "Invalid target entity");
            if (target.GetPivotRotation(space) == quat)
                return;

            Matrix4x4 oldToNewMatrix;
            var children = OnChangePivotBegin(target, out oldToNewMatrix);

            SetRotationPrivate(target, quat, space);

            OnChangePivotEnd(target, children, oldToNewMatrix);
        }


        ///////////////////////////////////
        // PRIVATE IMPLEMENTATION
        ///////////////////////////////////

        static Transform[] OnChangePivotBegin(Transform target, out Matrix4x4 oldToNewMatrix)
        {
            var children = new Transform[target.childCount];
            for (int i = 0; i < children.Length; ++i)
            {
                children[i] = target.GetChild(i);
            }

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(target, "Changed Pivot");
            if (children.Length > 0)
                UnityEditor.Undo.RecordObjects(children, "Changed Pivot");
#endif
            oldToNewMatrix = target.localToWorldMatrix;

            for (int i = 0; i < children.Length; ++i)
            {
                children[i].SetParent(null);
            }

            return children;
        }

        static void OnChangePivotEnd(Transform target, Transform[] children, Matrix4x4 oldToNewMatrix)
        {
            for (int i = 0; i < children.Length; ++i)
            {
                children[i].SetParent(target);
            }

            // Deal with colliders
            var colliders = target.GetComponents<Collider>();
            if (colliders.Length > 0)
            {
                oldToNewMatrix = target.worldToLocalMatrix * oldToNewMatrix;
#if UNITY_EDITOR
                UnityEditor.Undo.RecordObjects(colliders, "Changed Pivot");
#endif
                foreach (var collider in colliders)
                {
                    if (collider is BoxCollider)
                        ((BoxCollider)collider).center = oldToNewMatrix.MultiplyPoint(((BoxCollider)collider).center);
                    else if (collider is CapsuleCollider)
                        ((CapsuleCollider)collider).center = oldToNewMatrix.MultiplyPoint(((CapsuleCollider)collider).center);
                    else if (collider is SphereCollider)
                        ((SphereCollider)collider).center = oldToNewMatrix.MultiplyPoint(((SphereCollider)collider).center);
                }
            }
        }

        static void SetPositionPrivate(Transform target, Vector3 pos, Space space = Space.Global)
        {
            if (space == Space.Global)
                target.position = pos;
            else
                target.localPosition = pos;
        }

        static void SetRotationPrivate(Transform target, Quaternion quat, Space space = Space.Global)
        {
            if (space == Space.Global)
                target.rotation = quat;
            else
                target.localRotation = quat;
        }

        static void SetRotationPrivate(Transform target, Vector3 euler, Space space = Space.Global)
        {
            Quaternion rot;
            if (space == Space.Global) rot = target.rotation;
            else rot = target.localRotation;

            rot.eulerAngles = euler;

            if (space == Space.Global) target.rotation = rot;
            else target.localRotation = rot;
        }
    }
}