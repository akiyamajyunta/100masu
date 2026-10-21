using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
/// <summary>
/// Conway's Game of Life background for Unity UI (full-screen RawImage).
/// Colors and timing match the web version (Tron-style, scroll right → left).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public sealed class GameOfLifeBackground : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] int cellPixelSize = 30;
    [SerializeField] int gapPixels = 2;
    [SerializeField] Color backgroundColor = new Color32(0x0d, 0x11, 0x17, 0xff);
    [SerializeField] Color deadColor = new Color32(0x05, 0x08, 0x10, 0xff);
    [SerializeField] Color liveColor = new Color32(0x00, 0xe5, 0xff, 0xff);

    [Header("Simulation")]
    [SerializeField] float stepInterval = 0.16f;
    [SerializeField] float scrollPixelsPerSecond = 22f;
    [SerializeField] bool torus = true;
    [SerializeField] int stirEveryGenerations = 45;
    [SerializeField] int boringLiveMax = 36;

    [Header("Control")]
    [SerializeField] bool paused;

    static readonly int[][] Glider = { new[] { 1, 0 }, new[] { 2, 1 }, new[] { 0, 2 }, new[] { 1, 2 }, new[] { 2, 2 } };
    static readonly int[][] Lwss = { new[] { 0, 1 }, new[] { 1, 1 }, new[] { 2, 1 }, new[] { 3, 1 }, new[] { 4, 1 }, new[] { 0, 0 }, new[] { 4, 2 } };
    static readonly int[][] RPentomino = { new[] { 1, 0 }, new[] { 0, 0 }, new[] { 0, 1 }, new[] { 0, 2 }, new[] { 1, 2 } };
    static readonly int[][] Acorn = { new[] { 2, 1 }, new[] { 1, 2 }, new[] { 0, 3 }, new[] { 0, 4 }, new[] { 1, 4 }, new[] { 2, 4 }, new[] { 3, 4 }, new[] { 0, 5 }, new[] { 1, 5 } };
    static readonly int[][][] ChaosPatterns = { Glider, Lwss, RPentomino, Acorn };

    RawImage _rawImage;
    Texture2D _texture;
    Color32[] _pixels;
    byte[,] _grid;
    int _cols;
    int _rows;
    int _texWidth;
    int _texHeight;
    float _scrollPx;
    float _lifeTimer;
    int _generation;
    readonly List<string> _recentFingerprints = new List<string>(8);

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
        RebuildGridAndTexture();
    }

    void Update()
    {
        if (_grid == null) return;

        if (!paused)
        {
            _lifeTimer += Time.deltaTime;
            while (_lifeTimer >= stepInterval)
            {
                _lifeTimer -= stepInterval;
                EvolveLife();
            }

            int stepPx = CellStepPx();
            int wrap = _cols * stepPx;
            if (wrap > 0)
            {
                _scrollPx += scrollPixelsPerSecond * Time.deltaTime;
                _scrollPx %= wrap;
            }
        }

        RedrawTexture();
        _texture.Apply(false);
    }

    public void RebuildGridAndTexture()
    {
        var rect = _rawImage.rectTransform.rect;
        int w = Mathf.Max(64, Mathf.RoundToInt(rect.width));
        int h = Mathf.Max(64, Mathf.RoundToInt(rect.height));

        int stepPx = CellStepPx();
        _cols = Mathf.Max(3, w / stepPx);
        _rows = Mathf.Max(3, h / stepPx);
        _texWidth = _cols * stepPx;
        _texHeight = _rows * stepPx;

        _grid = new byte[_rows, _cols];
        _pixels = new Color32[_texWidth * _texHeight];

        if (_texture != null) Destroy(_texture);
        _texture = new Texture2D(_texWidth, _texHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        _rawImage.texture = _texture;

        SeedInitial();
        _scrollPx = 0;
        _lifeTimer = 0;
    }

    /// <summary>プレイ画面表示時にテーママネージャから呼ぶ。</summary>
    public void PlayBackground()
    {
        paused = false;
        Canvas.ForceUpdateCanvases();
        RebuildGridAndTexture();
    }

    public void StopBackground()
    {
        paused = true;
    }

    int CellStepPx() => cellPixelSize + gapPixels;

    void SeedInitial()
    {
        for (int y = 0; y < _rows; y++)
        for (int x = 0; x < _cols; x++)
            _grid[y, x] = 0;

        int count = Mathf.Max(16, (_cols * _rows) / 120);
        for (int i = 0; i < count; i++) RandomStamp();

        for (int y = 0; y < _rows; y++)
        for (int x = 0; x < _cols; x++)
            if (Random.value < 0.09f) _grid[y, x] = 1;

        _recentFingerprints.Clear();
        _generation = 0;
    }

    void RandomStamp()
    {
        var pat = ChaosPatterns[Random.Range(0, ChaosPatterns.Length)];
        int ox = Random.Range(1, Mathf.Max(2, _cols - 8));
        int oy = Random.Range(1, Mathf.Max(2, _rows - 8));
        Stamp(pat, ox, oy);
    }

    void Stamp(int[][] cells, int ox, int oy)
    {
        foreach (var c in cells)
        {
            int x = ox + c[0];
            int y = oy + c[1];
            if (x >= 0 && x < _cols && y >= 0 && y < _rows) _grid[y, x] = 1;
        }
    }

    void InjectChaos()
    {
        for (int i = 0; i < 5; i++) RandomStamp();
        int budget = Mathf.Max(12, Mathf.RoundToInt(_cols * _rows * 0.004f));
        int flipped = 0;
        while (flipped < budget)
        {
            int x = Random.Range(0, _cols);
            int y = Random.Range(0, _rows);
            if (_grid[y, x] == 0)
            {
                _grid[y, x] = 1;
                flipped++;
            }
        }
        _recentFingerprints.Clear();
    }

    int CountLive()
    {
        int n = 0;
        for (int y = 0; y < _rows; y++)
        for (int x = 0; x < _cols; x++)
            n += _grid[y, x];
        return n;
    }

    string Fingerprint()
    {
        int h = 0;
        int live = 0;
        for (int y = 0; y < _rows; y++)
        for (int x = 0; x < _cols; x++)
        {
            if (_grid[y, x] == 0) continue;
            live++;
            h = (h * 33 + x * 73856093 + y * 19349663);
        }
        return $"{live}:{h}";
    }

    bool IsBoringOscillator()
    {
        int live = CountLive();
        if (live == 0 || live > boringLiveMax) return false;
        if (_recentFingerprints.Count < 6) return false;

        int n = _recentFingerprints.Count;
        string s0 = _recentFingerprints[n - 1];
        string s1 = _recentFingerprints[n - 2];
        string s2 = _recentFingerprints[n - 3];
        string s3 = _recentFingerprints[n - 4];

        if (s0 == s2 && s1 == s3) return true;
        if (s0 == s1 && live <= 12) return true;
        foreach (var s in _recentFingerprints)
            if (s != s0) return false;
        return true;
    }

    void EvolveLife()
    {
        var next = new byte[_rows, _cols];
        int live = 0;
        for (int y = 0; y < _rows; y++)
        {
            for (int x = 0; x < _cols; x++)
            {
                int neighbors = CountNeighbors(x, y);
                bool alive = _grid[y, x] == 1;
                byte v = alive
                    ? (neighbors == 2 || neighbors == 3 ? (byte)1 : (byte)0)
                    : (neighbors == 3 ? (byte)1 : (byte)0);
                next[y, x] = v;
                live += v;
            }
        }
        _grid = next;
        _generation++;

        _recentFingerprints.Add(Fingerprint());
        if (_recentFingerprints.Count > 8) _recentFingerprints.RemoveAt(0);

        if (live == 0)
        {
            SeedInitial();
            return;
        }
        if (IsBoringOscillator())
        {
            InjectChaos();
            return;
        }
        if (_generation % stirEveryGenerations == 0)
        {
            RandomStamp();
            RandomStamp();
        }
    }

    int CountNeighbors(int x, int y)
    {
        int n = 0;
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx;
            int ny = y + dy;
            if (torus)
            {
                nx = (nx + _cols) % _cols;
                ny = (ny + _rows) % _rows;
            }
            else if (nx < 0 || ny < 0 || nx >= _cols || ny >= _rows) continue;
            n += _grid[ny, nx];
        }
        return n;
    }

    void RedrawTexture()
    {
        var bg = (Color32)backgroundColor;
        var dead = (Color32)deadColor;
        var live = (Color32)liveColor;

        for (int i = 0; i < _pixels.Length; i++) _pixels[i] = bg;

        int stepPx = CellStepPx();
        int wrap = _cols * stepPx;
        float offset = _scrollPx % wrap;

        for (int gy = 0; gy < _rows; gy++)
        {
            int py0 = gy * stepPx;
            for (int gx = 0; gx < _cols; gx++)
            {
                var col = _grid[gy, gx] == 1 ? live : dead;
                float px0 = gx * stepPx - offset;
                DrawCell(px0, py0, col, stepPx);
                DrawCell(px0 + wrap, py0, col, stepPx);
            }
        }

        _texture.SetPixels32(_pixels);
    }

    void DrawCell(float px0, int py0, Color32 col, int stepPx)
    {
        int xStart = Mathf.RoundToInt(px0);
        if (xStart + stepPx < 0 || xStart >= _texWidth) return;

        int x0 = Mathf.Max(0, xStart);
        int x1 = Mathf.Min(_texWidth, xStart + cellPixelSize);
        int y0 = Mathf.Max(0, py0);
        int y1 = Mathf.Min(_texHeight, py0 + cellPixelSize);

        for (int y = y0; y < y1; y++)
        {
            int row = y * _texWidth;
            for (int x = x0; x < x1; x++) _pixels[row + x] = col;
        }
    }

    void OnDestroy()
    {
        if (_texture != null) Destroy(_texture);
    }
}
}
