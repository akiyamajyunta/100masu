using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    /// <summary>
    /// 透過 PNG を2枚縦に並べて流す（ループ境界が目立たない雪オーバーレイ用）。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class MasuThemeOverlayScrollMotion : MonoBehaviour
    {
        RectTransform _rt;
        Image _host;
        RectTransform[] _tiles;
        float _loopHeight;
        float _totalScroll;
        float _speed;

        void Awake() => enabled = false;

        public void Begin(float pixelsPerSecond)
        {
            _rt = (RectTransform)transform;
            _host = GetComponent<Image>();
            _speed = pixelsPerSecond;

            if (_host == null || _host.sprite == null || pixelsPerSecond <= 0.01f)
            {
                Stop();
                return;
            }

            EnsureMask();
            RebuildTiles(_host.sprite);
            _host.enabled = false;

            Canvas.ForceUpdateCanvases();
            _loopHeight = Mathf.Max(_rt.rect.height, 64f);
            LayoutTiles();
            _totalScroll = 0f;
            enabled = true;
        }

        public void Stop()
        {
            enabled = false;
            _speed = 0f;
            _totalScroll = 0f;
            DestroyTiles();
            if (_host != null)
                _host.enabled = true;
        }

        void Update()
        {
            if (_tiles == null || _tiles.Length < 2 || _loopHeight <= 0f) return;

            _totalScroll += _speed * Time.deltaTime;
            var y0 = -(_totalScroll % _loopHeight);
            _tiles[0].anchoredPosition = new Vector2(0f, y0);
            _tiles[1].anchoredPosition = new Vector2(0f, y0 + _loopHeight);
        }

        void EnsureMask()
        {
            if (GetComponent<RectMask2D>() == null)
                gameObject.AddComponent<RectMask2D>();
        }

        void RebuildTiles(Sprite sprite)
        {
            DestroyTiles();
            _tiles = new RectTransform[2];
            for (var i = 0; i < 2; i++)
            {
                var go = new GameObject($"ScrollTile{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(_rt, false);
                var img = go.GetComponent<Image>();
                img.sprite = sprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.raycastTarget = false;
                img.color = Color.white;
                if (img.canvasRenderer != null)
                    img.canvasRenderer.cullTransparentMesh = false;
                _tiles[i] = go.GetComponent<RectTransform>();
            }
        }

        void LayoutTiles()
        {
            if (_tiles == null) return;
            var width = _rt.rect.width;
            for (var i = 0; i < _tiles.Length; i++)
            {
                var t = _tiles[i];
                t.anchorMin = new Vector2(0.5f, 1f);
                t.anchorMax = new Vector2(0.5f, 1f);
                t.pivot = new Vector2(0.5f, 1f);
                t.sizeDelta = new Vector2(width, _loopHeight);
            }
        }

        void DestroyTiles()
        {
            if (_tiles == null) return;
            foreach (var t in _tiles)
            {
                if (t != null)
                    Destroy(t.gameObject);
            }

            _tiles = null;
        }
    }
}
