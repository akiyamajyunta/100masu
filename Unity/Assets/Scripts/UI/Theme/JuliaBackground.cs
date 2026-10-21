using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
/// <summary>
/// Julia set background (orbit-trap interior, cyan lightning exterior). Matches julia-background.js.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public sealed class JuliaBackground : MonoBehaviour
{
    static readonly Color32 Void = new Color32(6, 10, 18, 255);
    static readonly Color32 Cyan = new Color32(34, 224, 255, 255);
    static readonly Color32 White = new Color32(244, 252, 255, 255);
    static readonly Color32 Core = new Color32(6, 10, 18, 255);

    [Header("Julia parameter")]
    [SerializeField] Vector2 juliaC = new Vector2(-0.745f, 0.113f);
    [SerializeField] float cOrbitRadius = 0.065f;
    [SerializeField] float cOrbitSpeed = 0.32f;

    [Header("View")]
    [SerializeField] Vector2 center = Vector2.zero;
    [SerializeField] float initialScale = 2.6f;
    [SerializeField] float zoomSpeed = 0.42f;
    [SerializeField] float minZoomFraction = 0.07f;
    [SerializeField] float maxZoomFraction = 0.48f;
    [SerializeField] float panStrength = 0.28f;
    [SerializeField] int maxIterations = 96;

    [Header("Performance")]
    [SerializeField] int maxRenderWidth = 400;
    [SerializeField] int framesPerRender = 2;

    [Header("Control")]
    [SerializeField] bool paused;

    RawImage _rawImage;
    Texture2D _texture;
    Color32[] _pixels;
    int _texWidth;
    int _texHeight;
    double _elapsed;
    int _frame;

    public bool Paused
    {
        get => paused;
        set => paused = value;
    }

    void Awake()
    {
        _rawImage = GetComponent<RawImage>();
        _rawImage.raycastTarget = false;
    }

    IEnumerator Start()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        RebuildTexture();
        Render();
    }

    void Update()
    {
        if (_texture == null) return;

        if (!paused) _elapsed += Time.deltaTime;

        _frame++;
        if (_frame % Mathf.Max(1, framesPerRender) != 0) return;

        Render();
        _texture.Apply(false);
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
    }

    public void PlayBackground()
    {
        paused = false;
        Canvas.ForceUpdateCanvases();
        RebuildTexture();
        Render();
        _texture.Apply(false);
    }

    public void StopBackground()
    {
        paused = true;
    }

    (double cr, double ci) CurrentC()
    {
        double t = _elapsed * cOrbitSpeed;
        return (
            juliaC.x + Math.Cos(t * 0.71) * cOrbitRadius,
            juliaC.y + Math.Sin(t * 0.59) * cOrbitRadius
        );
    }

    double CurrentHalfWidth()
    {
        double wave = 0.5 + 0.5 * Math.Cos(_elapsed * zoomSpeed * 0.55);
        double frac = minZoomFraction + (maxZoomFraction - minZoomFraction) * wave;
        return initialScale * frac;
    }

    void Render()
    {
        var (cr, ci) = CurrentC();
        double halfW = CurrentHalfWidth();
        double aspect = (double)_texHeight / _texWidth;
        double halfH = halfW * aspect;
        double pan = halfW * panStrength;
        double cx = center.x + Math.Cos(_elapsed * 0.31) * pan;
        double cy = center.y + Math.Sin(_elapsed * 0.27) * pan;
        float time = (float)_elapsed;

        for (int py = 0; py < _texHeight; py++)
        {
            double y0 = cy + (1.0 - (py + 0.5) / _texHeight * 2.0) * halfH;
            int row = py * _texWidth;
            for (int px = 0; px < _texWidth; px++)
            {
                double x0 = cx + ((px + 0.5) / _texWidth * 2.0 - 1.0) * halfW;
                _pixels[row + px] = SampleJulia(x0, y0, cr, ci, time, maxIterations);
            }
        }

        _texture.SetPixels32(_pixels);
    }

    static Color32 SampleJulia(double zx, double zy, double cr, double ci, float time, int maxIter)
    {
        double x = zx;
        double y = zy;
        int i = 0;
        double minR2 = double.PositiveInfinity;

        while (i < maxIter)
        {
            double xx = x * x;
            double yy = y * y;
            double r2 = xx + yy;
            if (r2 < minR2) minR2 = r2;
            if (r2 > 4.0)
            {
                double smooth = i + 1.0 - Math.Log(Math.Log(r2)) / Math.Log(2.0);
                return LightningExterior((float)(smooth / maxIter), time);
            }

            double xNew = xx - yy + cr;
            y = 2.0 * x * y + ci;
            x = xNew;
            i++;
        }

        return LightningInterior(minR2);
    }

    static Color32 LightningExterior(float t, float time)
    {
        float edge = Mathf.Clamp01(t);
        edge = Mathf.Pow(edge, 2.35f);
        Color32 col;
        if (edge < 0.42f) col = LerpColor(Void, Cyan, edge / 0.42f);
        else col = LerpColor(Cyan, White, (edge - 0.42f) / 0.58f);

        float bright = Mathf.Clamp01((edge - 0.35f) / 0.65f);
        float flick = 0.5f + 0.5f * Mathf.Sin(time * 7.1f - edge * 5f);
        float gain = 1f + bright * flick * 0.12f;
        return ScaleRgbContrast(col, gain);
    }

    static Color32 LightningInterior(double minR2)
    {
        float trap = Mathf.Clamp01(1f - (float)Math.Sqrt(minR2) * 0.5f);
        trap = Mathf.Pow(trap, 2.1f);
        if (trap < 0.5f) return LerpColor(Core, Cyan, trap / 0.5f);
        return LerpColor(Cyan, White, (trap - 0.5f) / 0.5f);
    }

    static Color32 LerpColor(Color32 a, Color32 b, float t)
    {
        t = Mathf.Clamp01(t);
        return new Color32(
            (byte)Mathf.RoundToInt(a.r + (b.r - a.r) * t),
            (byte)Mathf.RoundToInt(a.g + (b.g - a.g) * t),
            (byte)Mathf.RoundToInt(a.b + (b.b - a.b) * t),
            255);
    }

    static Color32 ScaleRgbContrast(Color32 c, float gain)
    {
        gain = Mathf.Clamp(gain, 0.88f, 1.22f);
        return new Color32(
            (byte)Mathf.Min(255, Mathf.RoundToInt(c.r * gain)),
            (byte)Mathf.Min(255, Mathf.RoundToInt(c.g * gain)),
            (byte)Mathf.Min(255, Mathf.RoundToInt(c.b * gain)),
            255);
    }

    void OnDestroy()
    {
        if (_texture != null) Destroy(_texture);
    }
}
}
