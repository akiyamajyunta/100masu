using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>
    /// 曼荼羅の時計回り回転（60 秒／周）＋カメラ周回（ヨー／ピッチ）の疑似 3D。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(RawImage))]
    public sealed class MasuThemeBuddhaMandalaOrbitView : MonoBehaviour
    {
        const string ShaderName = "Masu/UI/MandalaCameraOrbit";
        const string OrbitMaterialResource = "Theme/MasuBuddhaMandalaOrbit";

        static readonly int YawId = Shader.PropertyToID("_YawDeg");
        static readonly int PitchId = Shader.PropertyToID("_PitchDeg");
        static readonly int FocalId = Shader.PropertyToID("_Focal");
        static readonly int PivotId = Shader.PropertyToID("_Pivot");
        static readonly int SpinId = Shader.PropertyToID("_SpinDeg");
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        static Material s_orbitMaterialTemplate;

        RectTransform _rt;
        RawImage _rawImage;
        Material _material;
        Texture2D _texture;

        void Awake()
        {
            _rt = (RectTransform)transform;
            _rawImage = GetComponent<RawImage>();
            _rawImage.raycastTarget = false;
        }

        void Update()
        {
            ApplyOrbitMaterialProperties();
        }

        public void Show(MasuThemeGameOverlayLayout layout)
        {
            if (!EnsureResources())
                return;

            gameObject.SetActive(true);
            _rawImage.texture = _texture;
            BindOrbitMaterial();
            _rawImage.color = Color.white;
            if (_rawImage.canvasRenderer != null)
                _rawImage.canvasRenderer.cullTransparentMesh = false;
            _rt.localScale = Vector3.one;
            _rt.localEulerAngles = Vector3.zero;
            if (layout == MasuThemeGameOverlayLayout.BuddhaMandalaOnGameBackground)
                MasuThemeOverlayRectLayout.ApplyBuddhaMandalaOnGameBackground(_rt);
            else
                MasuThemeOverlayRectLayout.ApplyGifOverlay(_rt, layout);
            StripForegroundCanvas();
            ApplyOrbitMaterialProperties();
            Canvas.ForceUpdateCanvases();
        }

        public void Hide()
        {
            if (_rawImage != null)
            {
                _rawImage.texture = null;
                _rawImage.material = null;
            }

            gameObject.SetActive(false);
        }

        void BindOrbitMaterial()
        {
            if (_material == null || _rawImage == null)
                return;

            _material.SetTexture(MainTexId, _texture);
            _rawImage.material = _material;
        }

        void ApplyOrbitMaterialProperties()
        {
            if (!isActiveAndEnabled || _rawImage == null || _material == null)
                return;

            var t = Time.unscaledTime;
            var yaw = MasuThemePlayfieldVisualLayout.BuddhaMandalaOrbitYawAmplitudeDegrees *
                Mathf.Sin(t * MasuThemePlayfieldVisualLayout.BuddhaMandalaOrbitYawSpeed);
            var pitch = MasuThemePlayfieldVisualLayout.BuddhaMandalaOrbitPitchAmplitudeDegrees *
                Mathf.Sin(t * MasuThemePlayfieldVisualLayout.BuddhaMandalaOrbitPitchSpeed + 1.047f);

            _material.SetFloat(YawId, yaw);
            _material.SetFloat(PitchId, pitch);
            _material.SetFloat(FocalId, MasuThemePlayfieldVisualLayout.BuddhaMandalaOrbitFocal);
            var pivot = MasuThemePlayfieldVisualLayout.BuddhaMandalaOrbitPivotUv;
            _material.SetVector(PivotId, new Vector4(pivot.x, pivot.y, 0f, 0f));
            var spinDeg = t * (360f / MasuThemePlayfieldVisualLayout.BuddhaMandalaSpinPeriodSeconds);
            _material.SetFloat(SpinId, spinDeg);

            if (_rawImage.material != _material)
                _rawImage.material = _material;

            _rawImage.SetMaterialDirty();
        }

        void StripForegroundCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
                Destroy(canvas);
            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                Destroy(raycaster);
        }

        bool EnsureResources()
        {
            if (_texture == null)
            {
                if (!MasuThemeResourceCache.TryGetTexture(
                        MasuThemePlayfieldVisualLayout.BuddhaMandalaTextureResource,
                        out _texture))
                {
                    Debug.LogWarning(
                        $"[MasuTheme] Mandala texture not found: TitleArt/{MasuThemePlayfieldVisualLayout.BuddhaMandalaTextureResource}");
                    return false;
                }

                _texture.wrapMode = TextureWrapMode.Clamp;
            }

            if (_material != null)
                return true;

            if (s_orbitMaterialTemplate == null)
                s_orbitMaterialTemplate = Resources.Load<Material>(OrbitMaterialResource);

            if (s_orbitMaterialTemplate != null)
            {
                _material = new Material(s_orbitMaterialTemplate) { hideFlags = HideFlags.HideAndDontSave };
                return true;
            }

            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogWarning(
                    $"[MasuTheme] Mandala orbit shader not found ({ShaderName}, Resources/{OrbitMaterialResource})");
                return true;
            }

            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            return true;
        }

        void OnDestroy()
        {
            if (_material != null)
                Destroy(_material);
        }
    }
}
