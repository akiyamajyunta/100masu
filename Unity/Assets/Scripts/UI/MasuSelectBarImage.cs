using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>1枚目の赤グラデ素材（title_select_bar）を UI Image に載せる。</summary>
    public static class MasuSelectBarImage
    {
        private static Sprite _sprite;

        public static Image Create(Transform parent, string name, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            var img = go.GetComponent<Image>();
            img.sprite = GetSprite();
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);
            return img;
        }

        private static Sprite GetSprite()
        {
            if (_sprite != null) return _sprite;
            var tex = Resources.Load<Texture2D>("TitleArt/title_select_bar");
            if (tex == null) return null;
            _sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            return _sprite;
        }
    }
}
