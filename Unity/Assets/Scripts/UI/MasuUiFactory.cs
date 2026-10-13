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
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = align;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            MasuFonts.Apply(label);
            return label;
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
            field.characterLimit = 8;
            return field;
        }

        public static void Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
