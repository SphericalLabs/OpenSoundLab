// Runtime axes drawer for small gizmo-style orientation hints that also renders in standalone builds.
using UnityEngine;

public class AxesDebug : MonoBehaviour
{
    public float length = 0.03f;
    public float lineWidth = 0.0015f;

    LineRenderer xLine;
    LineRenderer yLine;
    LineRenderer zLine;

    void Awake()
    {
        ensureLines();
    }

    void LateUpdate()
    {
        ensureLines();

        Vector3 p = transform.position;
        Quaternion r = transform.rotation;

        updateLine(xLine, p, p + r * Vector3.right * length, Color.red);
        updateLine(yLine, p, p + r * Vector3.up * length, Color.green);
        updateLine(zLine, p, p + r * Vector3.forward * length, Color.blue);

#if UNITY_EDITOR
        Debug.DrawLine(p, p + r * Vector3.right * length, Color.red);
        Debug.DrawLine(p, p + r * Vector3.up * length, Color.green);
        Debug.DrawLine(p, p + r * Vector3.forward * length, Color.blue);
#endif
    }

    void ensureLines()
    {
        if (xLine != null && yLine != null && zLine != null) return;

        xLine = createLineRenderer("AxesDebug_X");
        yLine = createLineRenderer("AxesDebug_Y");
        zLine = createLineRenderer("AxesDebug_Z");
    }

    LineRenderer createLineRenderer(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.textureMode = LineTextureMode.Stretch;
        return lr;
    }

    void updateLine(LineRenderer lr, Vector3 start, Vector3 end, Color color)
    {
        if (lr == null) return;
        lr.startColor = color;
        lr.endColor = color;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }
}
