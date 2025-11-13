using UnityEngine;
using UnityEngine.UI;

namespace Nobi.UiRoundedCorners {
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class ImageWithRoundedCorners : MonoBehaviour {
        const string DefaultShaderName = "UI/RoundedCorners/RoundedCorners";
        private static readonly int Props = Shader.PropertyToID("_WidthHeightRadius");
        private static readonly int prop_OuterUV = Shader.PropertyToID("_OuterUV");

        public float radius = 40f;
        private Material material;
        private Vector4 outerUV = new Vector4(0, 0, 1, 1);

        [HideInInspector, SerializeField] private MaskableGraphic image;
        [SerializeField] Shader shaderOverride;

        private void OnValidate() {
            Validate();
            Refresh();
        }

        private void OnDestroy() {
            if (image != null) {
                image.material = null;
            }

            DestroyHelper.Destroy(material);
            image = null;
            material = null;
        }

        private void OnEnable() {
            var other = GetComponent<ImageWithIndependentRoundedCorners>();
            if (other != null) {
                radius = other.r.x;
                DestroyHelper.Destroy(other);
            }

            Validate();
            Refresh();
        }

        private void OnRectTransformDimensionsChange() {
            if (enabled && material != null) {
                Refresh();
            }
        }

        public void Validate() {
            Shader shader = ResolveShader();
            if (shader == null) {
                Debug.LogWarning("ImageWithRoundedCorners: rounded-corner shader not available.");
                return;
            }

            if (material == null || material.shader != shader) {
                DestroyHelper.Destroy(material);
                material = new Material(shader);
            }

            if (image == null) {
                TryGetComponent(out image);
            }

            if (image != null) {
                image.material = material;

                if (image is Image uiImage && uiImage.sprite != null) {
                    outerUV = UnityEngine.Sprites.DataUtility.GetOuterUV(uiImage.sprite);
                }
            }
        }

        public void Refresh() {
            if (material == null) {
                return;
            }

            var rect = ((RectTransform)transform).rect;

            material.SetVector(Props, new Vector4(rect.width, rect.height, radius * 2, 0));
            material.SetVector(prop_OuterUV, outerUV);
        }

        public void SetShaderOverride(Shader shader) {
            if (shaderOverride == shader) {
                return;
            }

            shaderOverride = shader;
            DestroyHelper.Destroy(material);
            material = null;
            if (image != null) {
                image.material = null;
            }
        }

        Shader ResolveShader() {
            if (shaderOverride != null) {
                return shaderOverride;
            }

            return Shader.Find(DefaultShaderName);
        }
    }
}
