using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RemoveDeviceComponents : MonoBehaviour
{
    public GameObject[] targets;
    public Material newMaterial;

    public void RemoveComponents()
    {
        foreach (GameObject g in targets)
        {
            tooltips t = g.GetComponentInChildren<tooltips>(true);
            if (t != null) DestroyImmediate(t.gameObject, true);

            MonoBehaviour[] m = g.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < m.Length; i++) DestroyImmediate(m[i], true);

            AudioSource[] audios = g.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < audios.Length; i++) DestroyImmediate(audios[i], true);

            Rigidbody[] rig = g.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rig.Length; i++) DestroyImmediate(rig[i], true);

            Collider[] col = g.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < col.Length; i++) DestroyImmediate(col[i], true);

            TextMesh[] tm = g.GetComponentsInChildren<TextMesh>(true);
            for (int i = 0; i < tm.Length; i++) DestroyImmediate(tm[i].gameObject, true);

            Renderer[] r = g.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < r.Length; i++)
            {
                r[i].material = newMaterial;
            }
        }
        
    }
}
