using UnityEngine;

namespace SuperPivot
{
    namespace Samples
    {
        /// <summary>
        /// Demo script to highlight runtime pivot position edition
        /// </summary>
        [RequireComponent(typeof(Collider))]
        public class SetPivotUnderMouseClick : MonoBehaviour
        {
            public Transform target;

            void Update()
            {
                if (Input.GetMouseButtonDown(0))
                {
                    RaycastHit hit;
                    var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out hit)) // get the 3d position under the mouse click
                    {
                        if (target && hit.transform == transform)
                        {
                            Debug.Log("Change pivot position to " + hit.point);
                            SuperPivot.API.SetPivotPosition(target.transform, hit.point);
                        }
                    }
                }
            }
        }
    }
}
