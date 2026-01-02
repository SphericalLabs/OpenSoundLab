using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


namespace SuperPivot
{
    internal class TargetManager
    {
        public bool isEmpty { get { return m_Targets.Count == 0; } }
        public bool isUnique { get { return m_Targets.Count == 1; } }
        public TargetWrapper uniqueTarget { get { Debug.Assert(isUnique); return m_Targets[0]; } }
        public int count { get { return m_Targets.Count; } }
        public string errorMsg { get; private set; }

        List<TargetWrapper> m_Targets = new List<TargetWrapper>();

        public IEnumerable<TargetWrapper> EveryTarget()
        {
            foreach (var t in m_Targets)
                yield return t;
        }

        public Transform[] GetTransformTargets()
        {
            var transforms = new Transform[m_Targets.Count];
            for (int i = 0; i < transforms.Length; i++)
                transforms[i] = m_Targets[i].transform;
            return transforms;
        }

        public void SetTargets(Transform[] selectedTransforms)
        {
            m_Targets.Clear();
            errorMsg = null;

            if (selectedTransforms != null)
            {
                foreach (var t in selectedTransforms)
                {
                    string localError = null;
                    if (API.CanChangePivot(t, out localError))
                    {
                        m_Targets.Add(new TargetWrapper(t));
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(errorMsg) && localError != null)
                            errorMsg = localError;
                    }
                }
            }
        }
    }
}