using UnityEngine;

namespace Masu.UI
{
    /// <summary>
    /// 仏テーマ時、涅槃像＋マンダラ GIF を一体としてパン・ズーム・画角（引き寄り＋微回転）する。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class MasuThemeBuddhaBackdropMotion : MonoBehaviour
    {
        const string BuddhaThemeId = "buddha";

        RectTransform _rt;
        Vector3 _baseScale;
        Vector2 _basePosition;
        Vector3 _baseEuler;

        void Awake()
        {
            _rt = (RectTransform)transform;
            CaptureBase();
        }

        void OnEnable() => ResetMotion();

        void Update()
        {
            if (MasuThemeManager.ActiveDefinition?.ThemeId != BuddhaThemeId)
            {
                if (!IsAtBaseTransform())
                    ResetMotion();
                return;
            }

            var t = Time.unscaledTime;
            var zoomWave = 0.5f + 0.5f * Mathf.Sin(t * MasuThemePlayfieldVisualLayout.BuddhaBackdropZoomSpeed);
            var fovWave = 0.5f + 0.5f * Mathf.Sin(
                t * MasuThemePlayfieldVisualLayout.BuddhaBackdropFovSpeed + 1.7f);
            var zoom = 1f
                + MasuThemePlayfieldVisualLayout.BuddhaBackdropZoomAmplitude * zoomWave
                + MasuThemePlayfieldVisualLayout.BuddhaBackdropFovAmplitude * fovWave;
            zoom = Mathf.Max(1f, zoom);

            var rawRot = Mathf.Sin(t * MasuThemePlayfieldVisualLayout.BuddhaBackdropRotateSpeed) *
                MasuThemePlayfieldVisualLayout.BuddhaBackdropRotateAmplitudeDegrees;
            var maxRot = MaxRotationInsideFrame(_rt, zoom);
            var rotZ = Mathf.Clamp(rawRot, -maxRot, maxRot);

            var rawPan = new Vector2(
                Mathf.Sin(t * MasuThemePlayfieldVisualLayout.BuddhaBackdropPanSpeedX) *
                MasuThemePlayfieldVisualLayout.BuddhaBackdropPanAmplitudePixels,
                Mathf.Cos(t * MasuThemePlayfieldVisualLayout.BuddhaBackdropPanSpeedY) *
                MasuThemePlayfieldVisualLayout.BuddhaBackdropPanAmplitudePixels *
                MasuThemePlayfieldVisualLayout.BuddhaBackdropPanVerticalScale);
            var pan = ClampPanInsideFrame(_rt, rawPan, zoom, rotZ);

            _rt.localScale = _baseScale * zoom;
            _rt.localEulerAngles = new Vector3(_baseEuler.x, _baseEuler.y, _baseEuler.z + rotZ);
            _rt.anchoredPosition = _basePosition + pan;
        }

        bool IsAtBaseTransform()
        {
            return _rt.localScale == _baseScale
                && _rt.anchoredPosition == _basePosition
                && _rt.localEulerAngles == _baseEuler;
        }

        void CaptureBase()
        {
            _baseScale = _rt.localScale;
            _basePosition = _rt.anchoredPosition;
            _baseEuler = _rt.localEulerAngles;
        }

        void ResetMotion()
        {
            _rt.localScale = _baseScale;
            _rt.anchoredPosition = _basePosition;
            _rt.localEulerAngles = _baseEuler;
        }

        static Vector2 ClampPanInsideFrame(RectTransform rt, Vector2 rawPan, float zoom, float rotZDegrees)
        {
            var bleed = MasuThemePlayfieldVisualLayout.BackdropBleedPixels;
            var safety = MasuThemePlayfieldVisualLayout.BuddhaBackdropPanBleedSafety;
            var halfW = Mathf.Max(64f, rt.rect.width) * 0.5f;
            var halfH = Mathf.Max(64f, rt.rect.height) * 0.5f;
            var maxPanX = ((zoom - 1f) * halfW + bleed) * safety;
            var maxPanY = ((zoom - 1f) * halfH + bleed) * safety;

            var rotRad = Mathf.Abs(rotZDegrees) * Mathf.Deg2Rad;
            var rotSlack = Mathf.Cos(rotRad);
            maxPanX *= rotSlack;
            maxPanY *= rotSlack;

            return new Vector2(
                Mathf.Clamp(rawPan.x, -maxPanX, maxPanX),
                Mathf.Clamp(rawPan.y, -maxPanY, maxPanY));
        }

        static float MaxRotationInsideFrame(RectTransform rt, float zoom)
        {
            var bleed = MasuThemePlayfieldVisualLayout.BackdropBleedPixels *
                MasuThemePlayfieldVisualLayout.BuddhaBackdropPanBleedSafety;
            var halfDiag = Mathf.Sqrt(rt.rect.width * rt.rect.width + rt.rect.height * rt.rect.height) * 0.5f;
            halfDiag = Mathf.Max(64f, halfDiag);
            var fromBleed = Mathf.Atan(bleed / halfDiag) * Mathf.Rad2Deg;
            var fromZoom = (zoom - 1f) * 14f;
            return Mathf.Min(
                MasuThemePlayfieldVisualLayout.BuddhaBackdropRotateAmplitudeDegrees,
                fromBleed + fromZoom);
        }
    }
}
