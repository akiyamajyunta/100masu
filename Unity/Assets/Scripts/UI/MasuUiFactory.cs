using Masu.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    public static class MasuUiFactory
    {
        public static GameObject CreatePanel(Transform parent, string name, Color bg)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = bg;
            Stretch(go.GetComponent<RectTransform>());
            return go;
        }

        /// <summary>親の中央に固定サイズのパネル（Stretch しない）。</summary>
        public static GameObject CreateCenteredPanel(
            Transform parent,
            string name,
            Color bg,
            Vector2 size,
            Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bg;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;
            return go;
        }

        public static Text CreateText(
            Transform parent,
            string name,
            string text,
            int fontSize,
            Color color,
            TextAnchor align = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.text = text;
            label.color = color;
            label.alignment = align;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.resizeTextForBestFit = false;
            MasuFonts.Apply(label, fontSize);
            return label;
        }

        public static void ApplyBlackOutline(Text label, float distance = 1.6f)
        {
            if (label == null) return;
            var outline = label.GetComponent<Outline>();
            if (outline == null)
                outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
        }

        /// <summary>記録画面と同じテキスト（白・黒縁・明朝／Century）。</summary>
        public static readonly Color RankingScreenTextColor = Color.white;

        public static void ApplyRankingScreenJapanese(Text label, int fontSize, bool bold = false)
        {
            if (label == null) return;
            label.color = RankingScreenTextColor;
            ApplyBlackOutline(label, 2f);
            MasuFonts.ApplyRankingMincho(label, fontSize, bold);
        }

        public static void ApplyRankingScreenLatin(Text label, int fontSize, bool bold = false)
        {
            if (label == null) return;
            label.color = RankingScreenTextColor;
            ApplyBlackOutline(label, 2f);
            MasuFonts.ApplyRankingLatin(label, fontSize, bold);
        }

        public static InputField CreateInputField(Transform parent, string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            root.GetComponent<Image>().color = MasuTheme.CellEmpty;

            var textArea = new GameObject("Text Area", typeof(RectTransform));
            textArea.transform.SetParent(root.transform, false);
            var taRt = textArea.GetComponent<RectTransform>();
            Stretch(taRt, 8, 4, 8, 4);

            var placeholder = CreateText(textArea.transform, "Placeholder", "名前を入力", 22, MasuTheme.TextDim,
                TextAnchor.MiddleLeft);
            placeholder.fontStyle = FontStyle.Italic;
            var text = CreateText(textArea.transform, "Text", "", 24, MasuTheme.Text, TextAnchor.MiddleLeft);

            var field = root.AddComponent<InputField>();
            field.textComponent = text;
            field.placeholder = placeholder;
            field.characterLimit = Leaderboard.NameDisplayMax;
            return field;
        }

        public static void Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>全面背景用。四隅の1px隙間や素材端の透過を画面外へ逃がす。</summary>
        public static void StretchFullBleed(RectTransform rt, float bleedPixels = 32f)
        {
            Stretch(rt, -bleedPixels, -bleedPixels, -bleedPixels, -bleedPixels);
        }

        /// <summary>アンカーで配置した金枠＋内側パネル。子 UI は返却 RectTransform に配置。</summary>
        /// <param name="regionScale">配置領域に対する枠の横幅（1=いっぱい、0.9=90%で中央寄せ）。</param>
        /// <param name="regionScaleY">縦幅。省略時は regionScale と同じ。</param>
        public static RectTransform CreateFramedRegion(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color innerBg,
            float borderInset = 3f,
            float regionScale = 1f,
            float? regionScaleY = null,
            Color? borderColor = null)
        {
            var scaleX = Mathf.Min(1f, regionScale);
            var scaleY = Mathf.Min(1f, regionScaleY ?? regionScale);
            Transform frameParent = parent;
            if (scaleX < 0.999f || scaleY < 0.999f)
            {
                var slot = new GameObject(name + "Slot", typeof(RectTransform));
                slot.transform.SetParent(parent, false);
                var slotRt = slot.GetComponent<RectTransform>();
                slotRt.anchorMin = anchorMin;
                slotRt.anchorMax = anchorMax;
                slotRt.offsetMin = offsetMin;
                slotRt.offsetMax = offsetMax;
                frameParent = slot.transform;
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
                offsetMin = offsetMax = Vector2.zero;
                var padX = (1f - scaleX) * 0.5f;
                var padY = (1f - scaleY) * 0.5f;
                anchorMin = new Vector2(padX, padY);
                anchorMax = new Vector2(1f - padX, 1f - padY);
            }

            var frame = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            frame.transform.SetParent(frameParent, false);
            var frt = frame.GetComponent<RectTransform>();
            frt.anchorMin = anchorMin;
            frt.anchorMax = anchorMax;
            frt.offsetMin = offsetMin;
            frt.offsetMax = offsetMax;
            var border = borderColor ?? MasuTheme.BorderGold;

            // 半透明の内側: 壁紙の上に黒を重ね、朱色／金は縁の帯だけ（Outline は全面が塗られるので使わない）。
            if (innerBg.a < 0.99f)
            {
                frame.GetComponent<Image>().color = Color.clear;
                var backdrop = CreatePanel(frame.transform, "Backdrop", innerBg);
                Stretch(backdrop.GetComponent<RectTransform>());
                AddRectBorderEdges(frame.transform, border, borderInset);
                var content = CreatePanel(frame.transform, "Inner", Color.clear);
                Stretch(content.GetComponent<RectTransform>(), borderInset, borderInset, borderInset, borderInset);
                return content.GetComponent<RectTransform>();
            }

            frame.GetComponent<Image>().color = border;
            var inner = CreatePanel(frame.transform, "Inner", innerBg);
            Stretch(inner.GetComponent<RectTransform>(), borderInset, borderInset, borderInset, borderInset);
            return inner.GetComponent<RectTransform>();
        }

        public static void AddRectBorderEdges(Transform parent, Color borderColor, float thickness)
        {
            CreateBorderEdge(parent, "BorderTop", borderColor, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -thickness), Vector2.zero);
            CreateBorderEdge(parent, "BorderBottom", borderColor, new Vector2(0f, 0f), new Vector2(1f, 0f),
                Vector2.zero, new Vector2(0f, thickness));
            CreateBorderEdge(parent, "BorderLeft", borderColor, new Vector2(0f, 0f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(thickness, 0f));
            CreateBorderEdge(parent, "BorderRight", borderColor, new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-thickness, 0f), Vector2.zero);
        }

        static void CreateBorderEdge(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            go.GetComponent<Image>().raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        public static Text CreateSidebarStatRow(
            Transform parent,
            string name,
            string label,
            float topY,
            Color valueColor,
            int valueFontSize = 16,
            bool valueBold = true)
        {
            var pad = MasuScoreSidebarLayout.ContentPadX;
            var labelGo = CreateText(parent, name + "Label", label, MasuScoreSidebarLayout.StatLabelFontSize,
                RankingScreenTextColor, TextAnchor.UpperLeft);
            ApplyRankingScreenJapanese(labelGo, MasuScoreSidebarLayout.StatLabelFontSize);
            var labelRt = labelGo.rectTransform;
            labelRt.anchorMin = labelRt.anchorMax = new Vector2(0, 1);
            labelRt.pivot = new Vector2(0, 1);
            labelRt.anchoredPosition = new Vector2(pad, topY);
            labelRt.sizeDelta = new Vector2(220, 28);

            var value = CreateText(parent, name + "Value", "", valueFontSize, RankingScreenTextColor,
                TextAnchor.UpperRight);
            ApplyRankingScreenLatin(value, valueFontSize, valueBold);
            var valueRt = value.rectTransform;
            valueRt.anchorMin = valueRt.anchorMax = new Vector2(1, 1);
            valueRt.pivot = new Vector2(1, 1);
            valueRt.anchoredPosition = new Vector2(-pad, topY - 30);
            valueRt.sizeDelta = new Vector2(220, 32);
            return value;
        }

        public static Text CreateSidebarStarsRow(
            Transform parent,
            string name,
            string label,
            float topY,
            out Text stars)
        {
            var pad = MasuScoreSidebarLayout.ContentPadX;
            var labelGo = CreateText(parent, name + "Label", label, MasuScoreSidebarLayout.StatLabelFontSize,
                RankingScreenTextColor, TextAnchor.UpperLeft);
            ApplyRankingScreenJapanese(labelGo, MasuScoreSidebarLayout.StatLabelFontSize);
            PlaceTopLeft(labelGo.rectTransform, pad, topY, 220, 28);
            stars = CreateText(parent, name + "Stars", "", MasuScoreSidebarLayout.StarFontSize,
                RankingScreenTextColor, TextAnchor.UpperRight);
            ApplyRankingScreenLatin(stars, MasuScoreSidebarLayout.StarFontSize);
            PlaceTopRight(stars.rectTransform, -pad, topY - 30, 220, 28);
            return stars;
        }

        public static void PlaceTopCenter(RectTransform rt, float y, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(width, height);
        }

        public static void PlaceTopLeft(RectTransform rt, float x, float y, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);
        }

        public static void PlaceTopRight(RectTransform rt, float xFromRight, float y, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(xFromRight, y);
            rt.sizeDelta = new Vector2(width, height);
        }
    }
}
