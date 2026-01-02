using UnityEngine;

namespace SuperPivot
{
    public class ShowPivot : MonoBehaviour
    {
        public bool visible = true;
        public Color color = Color.yellow;

        // Editor
        void OnDrawGizmos()
        {
            if (visible)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.color = color;
                Gizmos.DrawWireSphere(Vector3.zero, 0.15f);
            }
        }
    }
}
