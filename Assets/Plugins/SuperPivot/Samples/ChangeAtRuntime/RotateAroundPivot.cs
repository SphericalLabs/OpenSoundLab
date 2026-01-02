using UnityEngine;

namespace SuperPivot
{
    namespace Samples
    {
        /// <summary>
        /// Simply rotate the GameObject around its pivot position
        /// </summary>
        public class RotateAroundPivot : MonoBehaviour
        {
            public float speed = 100f;

            void Update()
            {
                transform.rotation *= Quaternion.Euler(0f, speed * Time.deltaTime, 0f);
            }
        }
    }
}