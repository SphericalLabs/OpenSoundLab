using UnityEngine;
using UnityEditor;

namespace SuperPivot
{
    namespace Samples
    {
        [InitializeOnLoad]
        public static class EditorCallbackListener
        {
            /////////////////////////////////////////////////////////////////////
            // Uncomment the following code to print SuperPivot editor events
            /////////////////////////////////////////////////////////////////////
            /*
            static EditorCallbackListener()
            {
                SuperPivot.EditorAPI.onChangeBeginSimple += OnChangeBeginSimple;
                SuperPivot.EditorAPI.onChangeBeginArray += OnChangeBeginArray;
                SuperPivot.EditorAPI.onChangeEnd += OnChangeEnd;
            }

            static void OnChangeBeginSimple()
            {
                // This function will be called in Editor, each time the user starts to move a pivot position.
                // You can use this callback for example to disable other Plugin features
                // which may cause conflict with SuperPivot.

                Debug.Log("EditorCallbackListener :: OnChangeBeginSimple");
            }

            static void OnChangeBeginArray(Transform[] targets)
            {
                // This function will be called in Editor, each time the user starts to move a pivot position.
                // 'targets' is an array with all the Transform objects being modified.
                // You can use this callback to have the precise list of all the objects being modified by SuperPivot

                var strNames = "(" + targets[0].name;
                for (int i = 1; i < targets.Length; ++i)
                    strNames += ", " + targets[i].name;
                strNames += ")";
                Debug.Log("EditorCallbackListener :: OnChangeBeginArray " + strNames);
            }

            static void OnChangeEnd()
            {
                // This function will be called in Editor, each time the user stop editing a pivot.
                // You can use this callback to restore the values you changed in onChangeBegin.

                Debug.Log("EditorCallbackListener :: OnChangeEnd");
            }
            */
        }
    }
}