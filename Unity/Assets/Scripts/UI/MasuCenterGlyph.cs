using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>
    /// uGUI Text のメッシュを、字形の外接矩形の中心がピボットに来るようずらす。
    /// ベースライン基準のままでは ○× がマスの上側に寄る。
    /// </summary>
    [RequireComponent(typeof(Text))]
    public sealed class MasuCenterGlyph : BaseMeshEffect
    {
        public override void ModifyMesh(VertexHelper vh)
        {
            if (vh == null) return;
            var count = vh.currentVertCount;
            if (count == 0) return;

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            var vert = new UIVertex();
            for (var i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vert, i);
                var p = vert.position;
                if (p.x < min.x) min.x = p.x;
                if (p.y < min.y) min.y = p.y;
                if (p.x > max.x) max.x = p.x;
                if (p.y > max.y) max.y = p.y;
            }

            var shift = new Vector3(-(min.x + max.x) * 0.5f, -(min.y + max.y) * 0.5f, 0f);
            if (shift.sqrMagnitude < 0.0001f) return;

            for (var i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vert, i);
                vert.position += shift;
                vh.SetUIVertex(vert, i);
            }
        }
    }
}
