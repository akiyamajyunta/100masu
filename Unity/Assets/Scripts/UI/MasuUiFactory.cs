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
            float? regionScaleY = null)
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
            frame.GetComponent<Image>().color = MasuTheme.BorderGold;

            var inner = CreatePanel(frame.transform, "Inner", innerBg);
            Stretch(inner.GetComponent<RectTransform>(), borderInset, borderInset, borderInset, borderInset);
            return inner.GetComponent<RectTransform>();
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
                MasuTheme.Text, TextAnchor.UpperLeft);
            var labelRt = labelGo.rectTransform;
            labelRt.anchorMin = labelRt.anchorMax = new Vector2(0, 1);
            labelRt.pivot = new Vector2(0, 1);
            labelRt.anchoredPosition = new Vector2(pad, topY);
            labelRt.sizeDelta = new Vector2(220, 24);

            var value = CreateText(parent, name + "Value", "", valueFontSize, valueColor, TextAnchor.UpperRight);
            if (valueBold) value.fontStyle = FontStyle.Bold;
            MasuFonts.Apply(value, valueFontSize);
            var valueRt = value.rectTransform;
            valueRt.anchorMin = valueRt.anchorMax = new Vector2(1, 1);
            valueRt.pivot = new Vector2(1, 1);
            valueRt.anchoredPosition = new Vector2(-pad, topY - 26);
            valueRt.sizeDelta = new Vector2(220, 26);
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
                MasuTheme.Text, TextAnchor.UpperLeft);
            PlaceTopLeft(labelGo.rectTransform, pad, topY, 220, 24);
            stars = CreateText(parent, name + "Stars", "", MasuScoreSidebarLayout.StarFontSize, MasuTheme.Text,
                TextAnchor.UpperRight);
            PlaceTopRight(stars.rectTransform, -pad, topY - 26, 220, 22);
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
