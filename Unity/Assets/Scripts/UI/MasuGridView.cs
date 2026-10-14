using System.Collections.Generic;
using Masu.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    public sealed class MasuGridView
    {
        private const int FocusPulseSteps = 30;
        private const float FocusPulseIntervalSec = 0.03f;

        private readonly Transform _root;
        private readonly GridLayoutGroup _layout;
        private readonly Dictionary<(int r, int c), CellWidgets> _cells = new();
        private float _cellSize = 56f;
        private int _headerFontSize = 22;
        private int _cellFontSize = 20;
        private int _markFontSize = 28;

        private int _focusPulseStep;
        private bool _focusPulseAscending = true;
        private float _focusPulseAccum;
        private Color _focusPulseColor = MasuTheme.FocusRingDim;

        private sealed class CellWidgets
        {
            public Image Bg;
            public Image FocusRingOuter;
            public Image FocusRingInner;
            public Text Label;
            public Text Mark;
            public bool IsHeader;
        }

        public MasuGridView(Transform parent)
        {
            var go = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            _root = go.transform;
            _layout = go.GetComponent<GridLayoutGroup>();
            _layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _layout.spacing = new Vector2(2, 2);
            _layout.childAlignment = TextAnchor.MiddleCenter;
        }

        public void Rebuild(GridConfig grid)
        {
            foreach (Transform ch in _root)
                Object.Destroy(ch.gameObject);
            _cells.Clear();

            var n = grid.GridSize;
            FitCellSize(n);
            _layout.constraintCount = n;
            _layout.cellSize = new Vector2(_cellSize, _cellSize);

            var rt = _root.GetComponent<RectTransform>();
            var total = n * _cellSize + (n - 1) * _layout.spacing.x;
            rt.sizeDelta = new Vector2(total, total);

            for (var row = 0; row < n; row++)
            for (var col = 0; col < n; col++)
            {
                var isCorner = row == 0 && col == 0;
                var isHeader = row == 0 || col == 0;
                if (isCorner)
                {
                    CreateCell(row, col, MasuTheme.PlusBg, true, "＋");
                    continue;
                }
                var bg = isHeader ? MasuTheme.HeaderSlot : MasuTheme.CellEmpty;
                CreateCell(row, col, bg, isHeader, "");
            }
        }

        private void FitCellSize(int n)
        {
            Canvas.ForceUpdateCanvases();
            var host = _root.parent as RectTransform;
            var w = host != null ? host.rect.width : 0f;
            var h = host != null ? host.rect.height : 0f;
            if (w < 8f || h < 8f)
            {
                var side = MasuDisplaySettings.DesignHeight * 0.9f * 0.9f;
                w = h = side;
            }

            var spacing = _layout.spacing.x;
            var total = Mathf.Min(w, h);
            _cellSize = (total - (n - 1) * spacing) / n;
            _cellSize = Mathf.Clamp(_cellSize, 24f, 256f);
            _headerFontSize = Mathf.Max(10, Mathf.RoundToInt(_cellSize * 0.4f));
            _cellFontSize = Mathf.Max(10, Mathf.RoundToInt(_cellSize * 0.36f));
            _markFontSize = Mathf.Max(12, Mathf.RoundToInt(_cellSize * 0.5f));
        }

        private void CreateCell(int row, int col, Color bg, bool isHeader, string initial)
        {
            var go = new GameObject($"c_{row}_{col}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root, false);
            var image = go.GetComponent<Image>();
            image.color = bg;

            Image focusOuter = null;
            Image focusInner = null;
            if (!isHeader)
            {
                var outerInset = Mathf.Max(1f, _cellSize * 0.02f);
                var innerInset = Mathf.Max(outerInset + 3f, _cellSize * 0.08f);
                var ringGo = new GameObject("FocusRingOuter", typeof(RectTransform), typeof(Image));
                ringGo.transform.SetParent(go.transform, false);
                focusOuter = ringGo.GetComponent<Image>();
                focusOuter.color = MasuTheme.FocusRingDim;
                focusOuter.raycastTarget = false;
                MasuUiFactory.Stretch(focusOuter.rectTransform, outerInset, outerInset, outerInset, outerInset);

                var innerGo = new GameObject("FocusRingInner", typeof(RectTransform), typeof(Image));
                innerGo.transform.SetParent(go.transform, false);
                focusInner = innerGo.GetComponent<Image>();
                focusInner.color = MasuTheme.CellEmpty;
                focusInner.raycastTarget = false;
                MasuUiFactory.Stretch(focusInner.rectTransform, innerInset, innerInset, innerInset, innerInset);

                ringGo.SetActive(false);
                innerGo.SetActive(false);
            }

            var label = MasuUiFactory.CreateText(go.transform, "Label", initial,
                isHeader ? _headerFontSize : _cellFontSize,
                MasuTheme.Text, TextAnchor.MiddleCenter);
            MasuUiFactory.Stretch(label.rectTransform, 2, 2, 2, 2);

            var mark = MasuUiFactory.CreateText(go.transform, "Mark", "", _markFontSize, MasuTheme.CorrectMark,
                TextAnchor.MiddleCenter);
            MasuUiFactory.Stretch(mark.rectTransform);
            mark.gameObject.SetActive(false);

            _cells[(row, col)] = new CellWidgets
            {
                Bg = image,
                FocusRingOuter = focusOuter,
                FocusRingInner = focusInner,
                Label = label,
                Mark = mark,
                IsHeader = isHeader
            };
        }

        public void TickFocusPulse(float deltaTime)
        {
            _focusPulseAccum += deltaTime;
            while (_focusPulseAccum >= FocusPulseIntervalSec)
            {
                _focusPulseAccum -= FocusPulseIntervalSec;
                AdvanceFocusPulseStep();
            }
        }

        private void AdvanceFocusPulseStep()
        {
            var span = FocusPulseSteps - 1;
            var t = _focusPulseAscending
                ? _focusPulseStep / (float)span
                : (span - _focusPulseStep) / (float)span;
            _focusPulseColor = Color.Lerp(MasuTheme.FocusRingDim, MasuTheme.FocusRingBright, t);

            if (_focusPulseStep + 1 >= FocusPulseSteps)
            {
                _focusPulseStep = 0;
                _focusPulseAscending = !_focusPulseAscending;
            }
            else
                _focusPulseStep++;
        }

        public void Refresh(
            GameState state,
            HashSet<(int r, int c)> revealedHeaders,
            Dictionary<(int r, int c), bool> scoreMarks,
            Dictionary<(int r, int c), bool> quickMarks,
            bool introAnimating,
            bool scoreAnimating)
        {
            var grid = state.Grid;
            var opSymbol = state.Operation.Symbol();
            _cells[(0, 0)].Label.text = opSymbol;

            for (var row = 0; row < grid.GridSize; row++)
            for (var col = 0; col < grid.GridSize; col++)
            {
                if (!_cells.TryGetValue((row, col), out var w)) continue;
                if (w.IsHeader)
                {
                    var revealed = !introAnimating || revealedHeaders.Contains((row, col));
                    if (row == 0 && col == 0) continue;
                    if (!revealed)
                    {
                        w.Label.text = "";
                        w.Bg.color = MasuTheme.HeaderSlot;
                    }
                    else
                    {
                        w.Label.text = state.Header[row][col].ToString();
                        w.Bg.color = row == 0 || col == 0 ? MasuTheme.HeaderNum : MasuTheme.HeaderSlot;
                    }
                    w.Mark.gameObject.SetActive(false);
                    continue;
                }

                if (!grid.IsPlayCell(row, col)) continue;

                var focused = row == state.FocusRow && col == state.FocusCol && !state.Finished;
                w.Bg.color = MasuTheme.CellEmpty;

                if (w.FocusRingOuter != null)
                {
                    w.FocusRingOuter.gameObject.SetActive(focused);
                    w.FocusRingInner.gameObject.SetActive(focused);
                    if (focused)
                        w.FocusRingOuter.color = _focusPulseColor;
                }

                var showMark = false;
                var markOk = false;
                if (scoreMarks != null && scoreMarks.TryGetValue((row, col), out var scoreOk))
                {
                    showMark = true;
                    markOk = scoreOk;
                }
                else if (quickMarks != null && quickMarks.TryGetValue((row, col), out var quickOk))
                {
                    showMark = true;
                    markOk = quickOk;
                    w.Bg.color = quickOk ? MasuTheme.CellQuickCorrect : MasuTheme.CellQuickWrong;
                }

                if (showMark)
                {
                    w.Mark.text = markOk ? "○" : "×";
                    w.Mark.color = markOk ? MasuTheme.CorrectMark : MasuTheme.WrongMark;
                    w.Mark.gameObject.SetActive(true);
                    if (!focused && w.FocusRingOuter != null)
                    {
                        w.FocusRingOuter.gameObject.SetActive(false);
                        w.FocusRingInner.gameObject.SetActive(false);
                    }
                }
                else
                {
                    w.Mark.gameObject.SetActive(false);
                }

                w.Label.text = state.PlayCellDisplay(row, col);
            }
        }

        public void PopCell(int row, int col, float scale)
        {
            if (!_cells.TryGetValue((row, col), out var w)) return;
            w.Label.transform.localScale = Vector3.one * scale;
        }

        public void PopMark(int row, int col, float scale)
        {
            if (!_cells.TryGetValue((row, col), out var w)) return;
            w.Mark.transform.localScale = Vector3.one * scale;
        }

        public void ResetMarkScales()
        {
            foreach (var w in _cells.Values)
                w.Mark.transform.localScale = Vector3.one;
        }

        public void ResetPopScales()
        {
            foreach (var w in _cells.Values)
                w.Label.transform.localScale = Vector3.one;
        }
    }
}
