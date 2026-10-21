using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
/// <summary>
/// Pseudo-Mandelbrot background: always colored (orbit-trap interior), fixed GPU/CPU cost.
/// Zoom breathes in/out so it never goes fully black from deep interior.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public sealed class MandelbrotBackground : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] Color coreColor = new Color32(0x05, 0x02, 0x01, 0xff);
    [SerializeField] int maxIterations = 96;

    [Header("View")]
    [SerializeField] Vector2 center = new Vector2(-0.743643887037151f, 0.13182590420533f);
    [SerializeField] float initialScale = 2.8f;
    [SerializeField] float zoomSpeed = 0.42f;
    [SerializeField] float minZoomFraction = 0.07f;
    [SerializeField] float maxZoomFraction = 0.48f;
    [SerializeField] float panStrength = 0.32f;

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
    double _baseCenterX;
    double _baseCenterY;

    public bool Paused
    {
        get => paused;
        set => paused = value;
    }

    void Awake()
    {
        _rawImage = GetComponent<RawImage>();
        _rawImage.raycastTarget = false;
        _baseCenterX = center.x;
        _baseCenterY = center.y;
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

    double CurrentHalfWidth()
    {
        double wave = 0.5 + 0.5 * Math.Cos(_elapsed * zoomSpeed * 0.55);
        double frac = minZoomFraction + (maxZoomFraction - minZoomFraction) * wave;
        return initialScale * frac;
    }

    void Render()
    {
        double aspect = (double)_texHeight / _texWidth;
        double halfW = CurrentHalfWidth();
        double halfH = halfW * aspect;

        double pan = halfW * panStrength;
        double cx = _baseCenterX + Math.Cos(_elapsed * 0.31) * pan;
        double cy = _baseCenterY + Math.Sin(_elapsed * 0.27) * pan;

        for (int py = 0; py < _texHeight; py++)
        {
            double y0 = cy + (1.0 - (py + 0.5) / _texHeight * 2.0) * halfH;
            int row = py * _texWidth;
            for (int px = 0; px < _texWidth; px++)
            {
                double x0 = cx + ((px + 0.5) / _texWidth * 2.0 - 1.0) * halfW;
                _pixels[row + px] = SamplePseudo(x0, y0, maxIterations, (Color32)coreColor);
            }
        }

        _texture.SetPixels32(_pixels);
    }

    static Color32 SamplePseudo(double x0, double y0, int maxIter, Color32 core)
    {
        double x = 0.0;
        double y = 0.0;
        int iter = 0;

        while (iter < maxIter)
        {
            double xx = x * x;
            double yy = y * y;
            double r2 = xx + yy;

            if (r2 > 4.0)
            {
                double smooth = iter + 1.0 - Math.Log(Math.Log(r2)) / Math.Log(2.0);
                return WarmExterior((float)(smooth / maxIter));
            }

            y = 2.0 * x * y + y0;
            x = xx - yy + x0;
            iter++;
        }

        return core;
    }

    static Color32 WarmExterior(float t)
    {
        t = Mathf.Clamp01(t);
        t = Mathf.Pow(t, 0.5f);

        Color32 c0 = new Color32(0x55, 0x14, 0x00, 0xff);
        Color32 c1 = new Color32(0xd4, 0x48, 0x00, 0xff);
        Color32 c2 = new Color32(0xff, 0xa0, 0x18, 0xff);
        Color32 c3 = new Color32(0xff, 0xee, 0xaa, 0xff);

        if (t < 0.34f) return Color32.Lerp(c0, c1, t / 0.34f);
        if (t < 0.68f) return Color32.Lerp(c1, c2, (t - 0.34f) / 0.34f);
        return Color32.Lerp(c2, c3, (t - 0.68f) / 0.32f);
    }

    void OnDestroy()
    {
        if (_texture != null) Destroy(_texture);
    }
}
}
