using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
/// <summary>
/// Infinite Penrose line pattern (de Bruijn pentagrid). Port of penrose-background.js.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public sealed class PenroseBackground : MonoBehaviour
{
    const float ViewWorld = 0.42f;
    const float GridSpacing = 0.062f;
    const float PanSpeed = 0.061f;
    const float InitialPanSec = 10f;
    const float LineWidthPx = 2.5f;

    [Header("Appearance")]
    [SerializeField] Color backgroundColor = new Color32(0x0d, 0x11, 0x17, 0xff);
    [SerializeField] Color lineColor = new Color32(0x00, 0xe5, 0xff, 0xff);

    [Header("Performance")]
    [SerializeField] int maxRenderWidth = 720;
    [SerializeField] int framesPerRender = 1;

    [Header("Control")]
    [SerializeField] bool paused;

    RawImage _rawImage;
    Texture2D _texture;
    Color32[] _pixels;
    int _texWidth;
    int _texHeight;
    Vector2 _scroll;
    int _frame;
    Coroutine _playRoutine;
    readonly Vector2[] _gridNormals = new Vector2[5];

    public bool Paused
    {
        get => paused;
        set => paused = value;
    }

    void Awake()
    {
        _rawImage = GetComponent<RawImage>();
        _rawImage.raycastTarget = false;
        _rawImage.color = Color.white;
        CacheGridNormals();
        InitScroll();
    }

    void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled || _rawImage == null) return;
        RebuildTexture();
        Redraw();
        if (_texture != null) _texture.Apply(false);
    }

    void Update()
    {
        if (_texture == null || _pixels == null) return;

        if (!paused)
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            float step = PanSpeed * dt / Mathf.Sqrt(2f);
            _scroll.x += step;
            _scroll.y += step;
        }

        _frame++;
        if (_frame % Mathf.Max(1, framesPerRender) != 0) return;

        Redraw();
        _texture.Apply(false);
    }

    void CacheGridNormals()
    {
        for (int j = 0; j < 5; j++)
        {
            float a = Mathf.PI * 0.5f + j * (2f * Mathf.PI / 5f);
            _gridNormals[j] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }
    }

    void InitScroll()
    {
        float step = PanSpeed / Mathf.Sqrt(2f);
        _scroll = new Vector2(step * InitialPanSec, step * InitialPanSec);
    }

    public void RebuildTexture()
    {
        var rect = _rawImage.rectTransform.rect;
        int w = Mathf.Max(64, Mathf.RoundToInt(rect.width));
        int h = Mathf.Max(64, Mathf.RoundToInt(rect.height));

        if (w > maxRenderWidth)
        {
            float aspect = (float)h / w;
            w = maxRenderWidth;
            h = Mathf.Max(64, Mathf.RoundToInt(w * aspect));
        }

        _texWidth = w;
        _texHeight = h;
        _pixels = new Color32[w * h];

        if (_texture != null) Destroy(_texture);
        _texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        _rawImage.texture = _texture;
        _rawImage.color = Color.white;
    }

    public void PlayBackground()
    {
        paused = false;
        _frame = 0;
        if (_playRoutine != null)
            StopCoroutine(_playRoutine);
        _playRoutine = StartCoroutine(PlayWhenReady());
    }

    public void StopBackground()
    {
        paused = true;
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }
    }

    IEnumerator PlayWhenReady()
    {
        Canvas.ForceUpdateCanvases();
        yield return null;
        Canvas.ForceUpdateCanvases();
        RebuildTexture();
        Redraw();
        if (_texture != null) _texture.Apply(false);
        _playRoutine = null;
    }

    static void GetViewWorld(int w, int h, out float viewW, out float viewH)
    {
        float aspect = h / (float)Mathf.Max(1, w);
        if (aspect >= 1f)
        {
            viewH = ViewWorld;
            viewW = ViewWorld * aspect;
        }
        else
        {
            viewW = ViewWorld;
            viewH = ViewWorld / aspect;
        }
    }

    void Redraw()
    {
        if (_pixels == null || _pixels.Length != _texWidth * _texHeight)
            return;

        var bg = (Color32)backgroundColor;
        for (int i = 0; i < _pixels.Length; i++) _pixels[i] = bg;

        GetViewWorld(_texWidth, _texHeight, out float viewW, out float viewH);
        float wx = _scroll.x;
        float wy = _scroll.y;

        for (int j = 0; j < 5; j++)
            DrawGridFamily(_gridNormals[j], wx, wy, viewW, viewH);

        _texture.SetPixels32(_pixels);
    }

    void DrawGridFamily(Vector2 n, float wx, float wy, float viewW, float viewH)
    {
        var corners = new[]
        {
            new Vector2(wx, wy),
            new Vector2(wx + viewW, wy),
            new Vector2(wx + viewW, wy + viewH),
            new Vector2(wx, wy + viewH),
        };

        float minDot = float.MaxValue;
        float maxDot = float.MinValue;
        foreach (var p in corners)
        {
            float d = Vector2.Dot(p, n);
            if (d < minDot) minDot = d;
            if (d > maxDot) maxDot = d;
        }

        int iMin = Mathf.FloorToInt(minDot / GridSpacing - 1f);
        int iMax = Mathf.CeilToInt(maxDot / GridSpacing + 1f);
        var tangent = new Vector2(-n.y, n.x);

        for (int i = iMin; i <= iMax; i++)
        {
            float c = i * GridSpacing;
            var anchor = n * c;
            if (!ClipLineToWorldRect(anchor, tangent, wx, wy, viewW, viewH, out var a, out var b))
                continue;

            float u0 = (a.x - _scroll.x) / viewW;
            float v0 = (a.y - _scroll.y) / viewH;
            float u1 = (b.x - _scroll.x) / viewW;
            float v1 = (b.y - _scroll.y) / viewH;

            if (!ClipToUnitRect(ref u0, ref v0, ref u1, ref v1))
                continue;

            float px0 = u0 * _texWidth;
            float py0 = (1f - v0) * _texHeight;
            float px1 = u1 * _texWidth;
            float py1 = (1f - v1) * _texHeight;
            StrokeLine(px0, py0, px1, py1);
        }
    }

    static bool ClipLineToWorldRect(Vector2 anchor, Vector2 tangent, float wx, float wy, float vw, float vh,
        out Vector2 a, out Vector2 b)
    {
        float sMin = float.NegativeInfinity;
        float sMax = float.PositiveInfinity;

        if (!ClipSlab(tangent.x, anchor.x, wx, wx + vw, ref sMin, ref sMax))
        {
            a = b = default;
            return false;
        }

        if (!ClipSlab(tangent.y, anchor.y, wy, wy + vh, ref sMin, ref sMax))
        {
            a = b = default;
            return false;
        }

        if (sMin > sMax)
        {
            a = b = default;
            return false;
        }

        a = anchor + tangent * sMin;
        b = anchor + tangent * sMax;
        return true;
    }

    static bool ClipSlab(float dir, float origin, float min, float max, ref float sMin, ref float sMax)
    {
        if (Mathf.Abs(dir) < 1e-8f)
            return origin >= min && origin <= max;

        float s1 = (min - origin) / dir;
        float s2 = (max - origin) / dir;
        if (s1 > s2) (s1, s2) = (s2, s1);
        sMin = Mathf.Max(sMin, s1);
        sMax = Mathf.Min(sMax, s2);
        return true;
    }

    static bool ClipToUnitRect(ref float x0, ref float y0, ref float x1, ref float y1)
    {
        const float min = 0f;
        const float max = 1f;
        float dx = x1 - x0;
        float dy = y1 - y0;
        float t0 = 0f;
        float t1 = 1f;

        bool Clip(float p, float q)
        {
            if (Mathf.Abs(p) < 1e-8f) return q >= 0f;
            float r = q / p;
            if (p < 0f)
            {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            }
            else
            {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }

            return true;
        }

        if (!Clip(-dx, x0 - min)) return false;
        if (!Clip(dx, max - x0)) return false;
        if (!Clip(-dy, y0 - min)) return false;
        if (!Clip(dy, max - y0)) return false;

        float nx0 = x0 + t0 * dx;
        float ny0 = y0 + t0 * dy;
        float nx1 = x0 + t1 * dx;
        float ny1 = y0 + t1 * dy;
        x0 = nx0;
        y0 = ny0;
        x1 = nx1;
        y1 = ny1;
        return true;
    }

    /// <summary>Canvas2D stroke (lineWidth 2.5, round joins) approximation.</summary>
    void StrokeLine(float x0, float y0, float x1, float y1)
    {
        var col = (Color32)lineColor;
        float halfW = LineWidthPx * 0.5f;
        const float aa = 1f;

        float dx = x1 - x0;
        float dy = y1 - y0;
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < 1e-4f) return;

        float invLen = 1f / len;
        float nx = -dy * invLen;
        float ny = dx * invLen;
        int steps = Mathf.Max(1, Mathf.CeilToInt(len));
        int perp = Mathf.CeilToInt(halfW + aa + 1f);

        for (int s = 0; s <= steps; s++)
        {
            float t = s / (float)steps;
            float cx = x0 + dx * t;
            float cy = y0 + dy * t;

            for (int o = -perp; o <= perp; o++)
            {
                float sx = cx + nx * o;
                float sy = cy + ny * o;
                int ix = Mathf.FloorToInt(sx);
                int iy = Mathf.FloorToInt(sy);

                for (int oy = 0; oy <= 1; oy++)
                for (int ox = 0; ox <= 1; ox++)
                {
                    int px = ix + ox;
                    int py = iy + oy;
                    if (px < 0 || px >= _texWidth || py < 0 || py >= _texHeight) continue;

                    float d = DistancePointToSegment(px + 0.5f, py + 0.5f, x0, y0, x1, y1);
                    float coverage = 1f - SmoothStep(halfW, halfW + aa, d);
                    if (coverage <= 0.001f) continue;
                    BlendLinePixel(py * _texWidth + px, col, coverage);
                }
            }
        }
    }

    static float DistancePointToSegment(float px, float py, float x0, float y0, float x1, float y1)
    {
        float dx = x1 - x0;
        float dy = y1 - y0;
        float len2 = dx * dx + dy * dy;
        if (len2 < 1e-8f)
            return Mathf.Sqrt((px - x0) * (px - x0) + (py - y0) * (py - y0));

        float t = ((px - x0) * dx + (py - y0) * dy) / len2;
        t = Mathf.Clamp01(t);
        float qx = x0 + t * dx;
        float qy = y0 + t * dy;
        float ex = px - qx;
        float ey = py - qy;
        return Mathf.Sqrt(ex * ex + ey * ey);
    }

    static float SmoothStep(float edge0, float edge1, float x)
    {
        if (edge1 <= edge0) return x >= edge1 ? 1f : 0f;
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    void BlendLinePixel(int index, Color32 line, float alpha)
    {
        var dst = _pixels[index];
        float a = Mathf.Clamp01(alpha);
        _pixels[index] = new Color32(
            (byte)Mathf.RoundToInt(dst.r + (line.r - dst.r) * a),
            (byte)Mathf.RoundToInt(dst.g + (line.g - dst.g) * a),
            (byte)Mathf.RoundToInt(dst.b + (line.b - dst.b) * a),
            255);
    }

    void OnDestroy()
    {
        if (_texture != null) Destroy(_texture);
    }
}
}
