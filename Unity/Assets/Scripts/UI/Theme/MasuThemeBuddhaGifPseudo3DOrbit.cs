using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>
    /// 仏テーマのマンダラ GIF を疑似3Dオービット（ヨー／ピッチ）で変形。UI の頂点を書き換える（Overlay で確実に動く）。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(MasuUiGifPlayer))]
    public sealed class MasuThemeBuddhaGifPseudo3DOrbit : BaseMeshEffect
    {
        const string BuddhaThemeId = "buddha";

        float _yawDegrees;
        float _pitchDegrees;

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshLayout();
        }

        void LateUpdate()
        {
            if (!ShouldApply())
                return;

            var t = Time.unscaledTime;
            _yawDegrees = MasuThemePlayfieldVisualLayout.BuddhaGifOrbitYawAmplitudeDegrees *
                Mathf.Sin(t * MasuThemePlayfieldVisualLayout.BuddhaGifOrbitYawSpeed);
            _pitchDegrees = MasuThemePlayfieldVisualLayout.BuddhaGifOrbitPitchAmplitudeDegrees *
                Mathf.Sin(t * MasuThemePlayfieldVisualLayout.BuddhaGifOrbitPitchSpeed + 1.15f);

            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        public void RefreshLayout()
        {
            if (graphic != null)
                graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (vh == null || !ShouldApply())
                return;

            var yaw = _yawDegrees * Mathf.Deg2Rad;
            var pitch = _pitchDegrees * Mathf.Deg2Rad;
            var vert = new UIVertex();
            var count = vh.currentVertCount;
            for (var i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vert, i);
                var p = (Vector3)vert.position;
                p.z = 0f;
                var r = RotateYawPitch(p, yaw, pitch);
                vert.position = new Vector3(r.x, r.y, vert.position.z);
                vh.SetUIVertex(vert, i);
            }
        }

        bool ShouldApply() =>
            isActiveAndEnabled
            && graphic != null
            && graphic.enabled
            && MasuThemeManager.ActiveDefinition != null
            && MasuThemeManager.ActiveDefinition.ThemeId == BuddhaThemeId;

        static Vector3 RotateYawPitch(Vector3 p, float yaw, float pitch)
        {
            var cy = Mathf.Cos(yaw);
            var sy = Mathf.Sin(yaw);
            var x1 = p.x * cy + p.z * sy;
            var z1 = -p.x * sy + p.z * cy;
            var y1 = p.y;

            var cp = Mathf.Cos(pitch);
            var sp = Mathf.Sin(pitch);
            var y2 = y1 * cp - z1 * sp;
            var x2 = x1;
            return new Vector3(x2, y2, 0f);
        }
    }
}
