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
        /// <summary>セル間 spacing と同じ幅。プレート外周＝最外枠の赤線。</summary>
        private const float GridFrameLine = 2f;

        private readonly Transform _root;
        private readonly RectTransform _gridPlateRt;
        private readonly RectTransform _gridOuterBorderRt;
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

        /// <summary>
        /// セル単位の一過性エフェクト。残り時間 (T) を Tick で減らし、Refresh 時に見た目へ合成する。
        /// 複数種類を同時に重ねられる（例: 採点の ○ パンチ + 背景フラッシュ + 揺れ）。
        /// </summary>
        private sealed class CellFx
        {
            public float FlashT, FlashDur, FlashStrength;
            public Color FlashColor;
            public float PunchT, PunchDur, PunchScale;
            public float MarkPunchT, MarkPunchDur, MarkPunchScale;
            public float ShakeT, ShakeDur, ShakeAmp;
            public float EnterT, EnterDur;
            public Vector2 EnterFrom;

            public bool Any => FlashT > 0f || PunchT > 0f || MarkPunchT > 0f || ShakeT > 0f || EnterT > 0f;

            public void Tick(float dt)
            {
                FlashT = Mathf.Max(0f, FlashT - dt);
                PunchT = Mathf.Max(0f, PunchT - dt);
                MarkPunchT = Mathf.Max(0f, MarkPunchT - dt);
                ShakeT = Mathf.Max(0f, ShakeT - dt);
                EnterT = Mathf.Max(0f, EnterT - dt);
            }
        }

        private readonly Dictionary<(int r, int c), CellFx> _fx = new();
        private readonly List<(int r, int c)> _fxDone = new();

        public MasuGridView(Transform parent)
        {
            // セル間の spacing に見える枠色（以前は外枠の朱色が透けていた）
            var plateGo = new GameObject("GridPlate", typeof(RectTransform), typeof(Image));
            plateGo.transform.SetParent(parent, false);
            _gridPlateRt = plateGo.GetComponent<RectTransform>();
            _gridPlateRt.anchorMin = _gridPlateRt.anchorMax = new Vector2(0.5f, 0.5f);
            _gridPlateRt.pivot = new Vector2(0.5f, 0.5f);
            var plateImg = plateGo.GetComponent<Image>();
            plateImg.color = MasuTheme.GameFrameBorder;
            plateImg.raycastTarget = false;

            var borderGo = new GameObject("GridOuterBorder", typeof(RectTransform));
            borderGo.transform.SetParent(parent, false);
            _gridOuterBorderRt = borderGo.GetComponent<RectTransform>();
            _gridOuterBorderRt.anchorMin = _gridOuterBorderRt.anchorMax = new Vector2(0.5f, 0.5f);
            _gridOuterBorderRt.pivot = new Vector2(0.5f, 0.5f);

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
            _fx.Clear();

            var n = grid.GridSize;
            FitCellSize(n);
            _layout.constraintCount = n;
            _layout.cellSize = new Vector2(_cellSize, _cellSize);

            var rt = _root.GetComponent<RectTransform>();
            var total = n * _cellSize + (n - 1) * _layout.spacing.x;
            var gridSize = new Vector2(total, total);
            var outerSize = gridSize + new Vector2(GridFrameLine * 2f, GridFrameLine * 2f);
            rt.sizeDelta = gridSize;
            _gridPlateRt.sizeDelta = outerSize;
            _gridOuterBorderRt.sizeDelta = outerSize;
            SyncGridOuterBorder();

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

            _gridOuterBorderRt.SetAsLastSibling();
        }

        private bool _gridOuterBorderBuilt;

        private void SyncGridOuterBorder()
        {
            if (_gridOuterBorderBuilt) return;
            _gridOuterBorderBuilt = true;
            MasuUiFactory.AddRectBorderEdges(_gridOuterBorderRt, MasuTheme.GameFrameBorder, GridFrameLine);
            _gridOuterBorderRt.SetAsLastSibling();
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
            var framePad = GridFrameLine * 2f;
            var total = Mathf.Min(w, h) - framePad;
            if (total < n * 8f)
                total = Mathf.Min(w, h);
            var fit = (total - (n - 1) * spacing) / n;
            _cellSize = Mathf.Clamp(fit, 8f, 256f);
            _headerFontSize = Mathf.Max(8, Mathf.RoundToInt(_cellSize * 0.4f));
            _cellFontSize = Mathf.Max(8, Mathf.RoundToInt(_cellSize * 0.36f));
            _markFontSize = Mathf.Max(10, Mathf.RoundToInt(_cellSize * 0.5f));
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

            var labelSize = isHeader ? _headerFontSize : _cellFontSize;
            var label = MasuUiFactory.CreateText(go.transform, "Label", initial, labelSize,
                MasuTheme.GridCellText, TextAnchor.MiddleCenter);
            MasuFonts.ApplyGridCell(label, labelSize);
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
                    if (row == 0 && col == 0)
                    {
                        ApplyFx(w, (row, col), MasuTheme.PlusBg);
                        continue;
                    }
                    var revealed = !introAnimating || revealedHeaders.Contains((row, col));
                    Color headerBg;
                    if (!revealed)
                    {
                        w.Label.text = "";
                        headerBg = MasuTheme.HeaderSlot;
                    }
                    else
                    {
                        w.Label.text = state.Header[row][col].ToString();
                        headerBg = MasuTheme.HeaderNum;
                    }
                    w.Mark.gameObject.SetActive(false);
                    ApplyFx(w, (row, col), headerBg);
                    continue;
                }

                if (!grid.IsPlayCell(row, col)) continue;

                var focused = row == state.FocusRow && col == state.FocusCol && !state.Finished;
                var baseBg = MasuTheme.CellEmpty;

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
                    baseBg = quickOk ? MasuTheme.CellQuickCorrect : MasuTheme.CellQuickWrong;
                }
                else if (!scoreAnimating && state.PlayMode == GamePlayMode.Quick
                         && state.Cells.TryGetValue((row, col), out var playCell)
                         && playCell.State == CellState.Correct)
                {
                    showMark = true;
                    markOk = true;
                    baseBg = MasuTheme.CellQuickCorrect;
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
                ApplyFx(w, (row, col), baseBg);
            }
        }

        // ------------------------------------------------------------------
        // セル演出
        // ------------------------------------------------------------------

        /// <summary>毎フレーム呼ぶ。全セルの演出の残り時間を進め、終わったものを捨てる。</summary>
        public void Tick(float dt)
        {
            if (_fx.Count == 0) return;
            _fxDone.Clear();
            foreach (var kv in _fx)
            {
                kv.Value.Tick(dt);
                if (!kv.Value.Any) _fxDone.Add(kv.Key);
            }
            foreach (var key in _fxDone)
            {
                _fx.Remove(key);
                if (_cells.TryGetValue(key, out var w)) ResetWidgetPose(w);
            }
        }

        public void ClearFx()
        {
            _fx.Clear();
            foreach (var w in _cells.Values) ResetWidgetPose(w);
        }

        private static void ResetWidgetPose(CellWidgets w)
        {
            w.Label.transform.localScale = Vector3.one;
            w.Label.rectTransform.anchoredPosition = Vector2.zero;
            w.Label.color = MasuTheme.GridCellText;
            w.Mark.transform.localScale = Vector3.one;
        }

        private CellFx FxFor(int row, int col)
        {
            if (!_fx.TryGetValue((row, col), out var fx))
            {
                fx = new CellFx();
                _fx[(row, col)] = fx;
            }
            return fx;
        }

        private static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private void ApplyFx(CellWidgets w, (int r, int c) key, Color baseBg)
        {
            if (!_fx.TryGetValue(key, out var fx))
            {
                w.Bg.color = baseBg;
                return;
            }

            var bg = baseBg;
            var labelScale = 1f;
            var markScale = 1f;
            var labelAlpha = 1f;
            var offset = Vector2.zero;

            if (fx.FlashT > 0f && fx.FlashDur > 0f)
            {
                var p = fx.FlashT / fx.FlashDur; // 1 → 0 で減衰
                bg = Color.Lerp(baseBg, fx.FlashColor, p * p * fx.FlashStrength);
            }
            if (fx.PunchT > 0f && fx.PunchDur > 0f)
            {
                var p = 1f - fx.PunchT / fx.PunchDur;
                labelScale = Mathf.LerpUnclamped(fx.PunchScale, 1f, EaseOutBack(p));
            }
            if (fx.MarkPunchT > 0f && fx.MarkPunchDur > 0f)
            {
                var p = 1f - fx.MarkPunchT / fx.MarkPunchDur;
                markScale = Mathf.LerpUnclamped(fx.MarkPunchScale, 1f, EaseOutBack(p));
            }
            if (fx.ShakeT > 0f && fx.ShakeDur > 0f)
            {
                var p = fx.ShakeT / fx.ShakeDur;
                offset.x += Mathf.Sin(fx.ShakeT * 80f) * fx.ShakeAmp * p;
            }
            if (fx.EnterT > 0f && fx.EnterDur > 0f)
            {
                var p = 1f - fx.EnterT / fx.EnterDur;
                offset += fx.EnterFrom * (1f - EaseOutBack(p));
                labelAlpha = Mathf.Clamp01(p * 2.5f);
            }

            w.Bg.color = bg;
            w.Label.transform.localScale = Vector3.one * Mathf.Max(0.01f, labelScale);
            w.Label.rectTransform.anchoredPosition = offset;
            var lc = MasuTheme.GridCellText;
            lc.a = labelAlpha;
            w.Label.color = lc;
            w.Mark.transform.localScale = Vector3.one * Mathf.Max(0.01f, markScale);
        }

        public void PlayFlash(int row, int col, Color color, float duration, float strength)
        {
            var fx = FxFor(row, col);
            fx.FlashColor = color;
            fx.FlashDur = fx.FlashT = Mathf.Max(0.01f, duration);
            fx.FlashStrength = Mathf.Clamp01(strength);
        }

        public void PlayPunch(int row, int col, float scale, float duration)
        {
            var fx = FxFor(row, col);
            fx.PunchScale = scale;
            fx.PunchDur = fx.PunchT = Mathf.Max(0.01f, duration);
        }

        public void PlayMarkPunch(int row, int col, float scale, float duration)
        {
            var fx = FxFor(row, col);
            fx.MarkPunchScale = scale;
            fx.MarkPunchDur = fx.MarkPunchT = Mathf.Max(0.01f, duration);
        }

        public void PlayShake(int row, int col, float amplitude, float duration)
        {
            var fx = FxFor(row, col);
            fx.ShakeAmp = amplitude;
            fx.ShakeDur = fx.ShakeT = Mathf.Max(0.01f, duration);
        }

        /// <summary>イントロ: ヘッダー数字が外側（左列は左から、上段は上から）から滑り込んで着地。</summary>
        public void PlayHeaderEnter(int row, int col, float duration)
        {
            var fx = FxFor(row, col);
            var dist = _cellSize * 0.9f;
            fx.EnterFrom = col == 0 ? new Vector2(-dist, 0f) : new Vector2(0f, dist);
            fx.EnterDur = fx.EnterT = Mathf.Max(0.01f, duration);
            PlayFlash(row, col, MasuTheme.CursorGold, duration, 0.5f);
        }

        /// <summary>演算子マス（左上）の強調パンチ。イントロの締め・採点開始の合図に使う。</summary>
        public void PlayOperatorPunch()
        {
            PlayPunch(0, 0, 1.6f, 0.4f);
            PlayFlash(0, 0, Color.white, 0.4f, 0.6f);
        }

        /// <summary>数字入力: 文字がポンと出て背景が一瞬金色に。</summary>
        public void PlayInputPunch(int row, int col)
        {
            PlayPunch(row, col, 1.3f, 0.2f);
            PlayFlash(row, col, MasuTheme.CursorGold, 0.22f, 0.4f);
        }

        /// <summary>入力拒否（範囲外・ロック中）: 小さく横に震えて赤く。</summary>
        public void PlayRejectShake(int row, int col)
        {
            PlayShake(row, col, Mathf.Max(2f, _cellSize * 0.06f), 0.2f);
            PlayFlash(row, col, MasuTheme.WrongMark, 0.2f, 0.35f);
        }

        /// <summary>Backspace: 控えめな揺れと薄いフラッシュ。</summary>
        public void PlayEraseFeedback(int row, int col)
        {
            PlayShake(row, col, Mathf.Max(1.5f, _cellSize * 0.035f), 0.12f);
            PlayFlash(row, col, MasuTheme.TextDim, 0.15f, 0.25f);
        }

        /// <summary>フォーカス移動先の軽いフラッシュ（連打中でもうるさくならない強さ）。</summary>
        public void PlayFocusMoved(int row, int col)
        {
            PlayFlash(row, col, MasuTheme.FocusRingBright, 0.18f, 0.22f);
        }

        /// <summary>採点: ○× をスタンプ。正解は緑フラッシュ、不正解は赤フラッシュ + 揺れ。</summary>
        public void PlayGradeStamp(int row, int col, bool correct, float duration)
        {
            PlayMarkPunch(row, col, 1.7f, duration);
            if (correct)
                PlayFlash(row, col, MasuTheme.CorrectMark, duration, 0.45f);
            else
            {
                PlayFlash(row, col, MasuTheme.WrongMark, duration, 0.6f);
                PlayShake(row, col, Mathf.Max(2.5f, _cellSize * 0.07f), duration * 0.9f);
            }
        }
    }
}
