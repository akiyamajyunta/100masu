using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>画面上部左の竹テクスチャ帯＋明朝説明文（モック2枚目）。</summary>
    public static class MasuInstructionBanner
    {
        private static readonly Color BannerTextBlack = new(0.06f, 0.06f, 0.06f);
        private const string BannerResource = "title_instruction_banner";

        /// <summary>設計幅（1280×960）。高さは素材の縦横比から算出。</summary>
        public const float BannerWidth = 808f;
        public const int FontSize = 38;
        public const float TextPaddingLeft = 36f;
        public const float TextPaddingRight = 28f;
        public const float TextPaddingVertical = 6f;
        /// <summary>上端からの下げ量（設計座標 1280×960）。</summary>
        public const float TopOffset = 24f;
        public const float LeftInset = 0f;

        private const float FallbackBannerHeight = 146f;

        public static Text Create(Transform parent, string message)
        {
            var tex = Resources.Load<Texture2D>("TitleArt/" + BannerResource);
            var bannerHeight = tex != null
                ? BannerWidth * tex.height / (float)tex.width
                : FallbackBannerHeight;

            var root = new GameObject("InstructionBanner", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            root.transform.SetAsFirstSibling();
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(LeftInset, -TopOffset);
            rt.sizeDelta = new Vector2(BannerWidth, bannerHeight);

            if (tex != null)
            {
                var artGo = new GameObject("Art", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                artGo.transform.SetParent(root.transform, false);
                artGo.transform.SetAsFirstSibling();
                var img = artGo.GetComponent<Image>();
                img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.raycastTarget = false;
                MasuUiFactory.Stretch(artGo.GetComponent<RectTransform>());
            }

            var label = MasuUiFactory.CreateText(root.transform, "Caption", message, FontSize, BannerTextBlack,
                TextAnchor.MiddleLeft);
            MasuFonts.ApplyInstructionBannerMincho(label, FontSize);
            var labelRt = label.rectTransform;
            MasuUiFactory.Stretch(labelRt, TextPaddingLeft, TextPaddingVertical, TextPaddingRight,
                TextPaddingVertical);
            label.transform.SetAsLastSibling();
            return label;
        }
    }
}
