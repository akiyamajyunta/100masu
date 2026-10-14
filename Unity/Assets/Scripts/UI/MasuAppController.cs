using System;
using System.Collections;
using System.Collections.Generic;
using Masu.Core;
using Masu.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Masu.UI
{
    [DisallowMultipleComponent]
    public sealed class MasuAppController : MonoBehaviour
    {
        private ScoreStore _store;
        private readonly GameState _state = new();
        private MasuGridView _gridView;

        private AppScreen _screen = AppScreen.Title;
        private MasuLauncherModal _launcher;
        private MasuSessionDisplayGuard _displayGuard;
        private CanvasScaler _canvasScaler;
        private Operation _operation = Operation.Add;
        private Operation _pendingOperation = Operation.Add;
        private int _gridSize = 4;
        private GamePlayMode _playMode = GamePlayMode.Classic;

        private int _titleMenu;
        private int _modeCursor;
        private int _gridCursor;
        private int _rankingOpCursor;
        private int _rankingBoardCursor;

        private bool _introAnimating;
        private bool _scoreAnimating;
        private int _introGen;
        private int _scoreGen;
        private readonly HashSet<(int r, int c)> _revealedHeaders = new();
        private readonly Dictionary<(int r, int c), bool> _scoreMarks = new();
        private readonly Dictionary<(int r, int c), bool> _quickMarks = new();
        private Coroutine _quickPopRoutine;
        private int _quickPopGen;

        private int _displayScore;
        private float _lastAccuracy;
        private double _lastElapsedSeconds;
        private string _lastPlayedAt = "";
        private bool _resultScoreSaved;
        private ResultView _resultView = ResultView.Menu;
        private int _resultMenu;
        private string _entryName = "";
        private int _entryPreviewRank = 1;

        private bool _endConfirmVisible;
        private int _endConfirmChoice;

        private Transform _menuRoot;
        private Transform _gameRoot;
        private Transform _resultRoot;
        private Transform _endConfirmRoot;
        private readonly Dictionary<AppScreen, GameObject> _menuPanels = new();
        private Text[] _titleLabels;
        private Text[] _modeLabels;
        private Text[] _gridLabels;
        private Text _gridOpHint;
        private int _pendingGridSize = 4;
        private int _playStyleCursor;
        private Text[] _playStyleLabels;
        private Text _playStyleHint;
        private Text[] _rankingOpTabs;
        private Text _rankingGridLabel;
        private Text _rankingGridArrowUp;
        private Text _rankingGridArrowDown;
        private Text[] _rankingRows;
        private Text _sidebarMode;
        private Text _sidebarGrid;
        private Text _sidebarHigh;
        private Text _sidebarSession;
        private Text _sidebarCorrect;
        private Text _sidebarAnswerStars;
        private Text _sidebarSkillStars;
        private Text _sidebarTime;
        private Text _guideText;
        private Text _resultTitle;
        private Text _resultSummary;
        private Text[] _resultMenuLabels;
        private Text[] _resultPreviewRows;
        private Text _resultConfirmOp;
        private Text _resultConfirmGrid;
        private Text _resultSessionSummary;
        private Text _resultEntryLine;
        private Text[][] _keyboardKeyLabels;
        private MasuNameKeyboard _nameKeyboard = new();
        private bool _recordConfirmEnterOpensKeyboard = true;
        private GameObject _resultConfirmPanel;
        private GameObject _resultEntryPanel;
        private GameObject _resultMenuPanel;
        private GameObject _resultContentPanel;

        private Coroutine _timerRoutine;
        private Coroutine _introRoutine;
        private Coroutine _scoreRoutine;

        private float _focusMoveNextRepeat;
        private const float FocusMoveInitialDelay = 0.35f;
        private const float FocusMoveRepeatInterval = 0.06f;

        private static readonly Operation[] Operations =
            { Operation.Add, Operation.Sub, Operation.Mul, Operation.Div };

        private const float GameFrameRegionScaleBase = 0.9f;
        private const float GameFrameRegionScaleX = GameFrameRegionScaleBase * 0.9f * 1.2f;
        private const float GameFrameRegionScaleY = GameFrameRegionScaleBase * 1.1f;
        /// <summary>メイン枠内で 100 マス盤が占める比率（各辺 5% 余白）。</summary>
        private const float MainGridFrameFill = 0.9f;
        private static float GameColumnSplitX => MasuScoreSidebarLayout.ColumnSplitAnchorX;

        private static string[] OperationMenuLabelsJa()
        {
            var labels = new string[Operations.Length];
            for (var i = 0; i < Operations.Length; i++)
                labels[i] = Operations[i].LabelJa();
            return labels;
        }

        private void Awake()
        {
            _store = ScoreStore.Load();
            var idx = MasuDisplaySettings.LoadSavedIndex();
            var ask = MasuDisplaySettings.LoadAskEveryTime();
            if (ask)
                MasuDisplaySettings.ApplyPreview(idx);
            else
                MasuDisplaySettings.ApplySavedForSession();

            BuildUi();

            if (ask)
            {
                _menuRoot.gameObject.SetActive(false);
                _launcher.Open(idx, ask);
            }
            else
                ShowScreen(AppScreen.Title);
        }

        private void OnDestroy()
        {
            StopGameCoroutines();
        }

        private void StopGameCoroutines()
        {
            _quickPopGen++;
            if (_quickPopRoutine != null)
            {
                StopCoroutine(_quickPopRoutine);
                _quickPopRoutine = null;
            }
            if (_timerRoutine != null)
            {
                StopCoroutine(_timerRoutine);
                _timerRoutine = null;
            }
            if (_introRoutine != null)
            {
                StopCoroutine(_introRoutine);
                _introRoutine = null;
            }
            if (_scoreRoutine != null)
            {
                StopCoroutine(_scoreRoutine);
                _scoreRoutine = null;
            }
        }

        private void Update()
        {
            if (_endConfirmVisible)
            {
                HandleEndConfirmInput();
                return;
            }

            if (_launcher != null && _launcher.IsOpen)
            {
                HandleLauncherInput();
                return;
            }

            if (_resultRoot.gameObject.activeSelf)
            {
                HandleResultInput();
                return;
            }

            switch (_screen)
            {
                case AppScreen.Title: HandleTitleInput(); break;
                case AppScreen.Help: HandleHelpInput(); break;
                case AppScreen.Mode: HandleModeInput(); break;
                case AppScreen.Grid: HandleGridInput(); break;
                case AppScreen.PlayStyle: HandlePlayStyleInput(); break;
                case AppScreen.ScoreRanking: HandleRankingInput(); break;
                case AppScreen.Game:
                    if (!_state.Finished && !_introAnimating && !_scoreAnimating)
                    {
                        _gridView.TickFocusPulse(Time.deltaTime);
                        _gridView.Refresh(_state, _revealedHeaders, _scoreMarks, _quickMarks, _introAnimating, _scoreAnimating);
                    }
                    HandleGameInput();
                    break;
            }
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvasScaler = canvasGo.GetComponent<CanvasScaler>();
            MasuCanvasSetup.Configure(_canvasScaler);
            _displayGuard = canvasGo.AddComponent<MasuSessionDisplayGuard>();

            _menuRoot = MasuUiFactory.CreatePanel(canvasGo.transform, "MenuLayer", MasuTheme.Bg).transform;
            _gameRoot = MasuUiFactory.CreatePanel(canvasGo.transform, "GameLayer", MasuTheme.Bg).transform;
            _resultRoot = MasuUiFactory.CreatePanel(canvasGo.transform, "ResultOverlay", Color.clear).transform;
            _endConfirmRoot = MasuUiFactory.CreatePanel(canvasGo.transform, "EndConfirm", new Color(0, 0, 0, 0.6f)).transform;

            _launcher = new MasuLauncherModal(canvasGo.transform);
            BuildMenuPanels();
            BuildGameLayer();
            BuildResultOverlay();
            BuildEndConfirm();

            _gameRoot.gameObject.SetActive(false);
            _resultRoot.gameObject.SetActive(false);
            _endConfirmRoot.gameObject.SetActive(false);
        }

        private void BuildMenuPanels()
        {
            _titleLabels = BuildMenuList(_menuRoot, "Title", new[] { "はじめる", "操作説明", "RESULT" }, out var titlePanel);
            _menuPanels[AppScreen.Title] = titlePanel;
            MasuUiFactory.CreateText(titlePanel.transform, "Heading", "100マス計算", 48, MasuTheme.CursorGold);

            var helpPanel = MasuUiFactory.CreatePanel(_menuRoot, "Help", MasuTheme.MainPanel);
            MasuUiFactory.CreateText(helpPanel.transform, "HelpText",
                "矢印 / WASD … マス移動（長押しで連続）\n数字 … 入力　Backspace … 削除\n" +
                "タイマー … ゲーム開始（ヘッダー表示）と同時に計測\n" +
                "クラシック … Enter で終了・一括採点\n" +
                "クイック … 別マスへ移動で採点（○×表示）\n" +
                "　　　　　最終マスは Enter で確定・全マス○でクリア\n" +
                "誤答 … +10秒　未入力（クラシック採点時）… +10秒\nEsc … 前の画面 / タイトル",
                22, MasuTheme.Text, TextAnchor.UpperLeft);
            _menuPanels[AppScreen.Help] = helpPanel;

            _modeLabels = BuildMenuList(_menuRoot, "Mode", OperationMenuLabelsJa(), out var modePanel);
            _menuPanels[AppScreen.Mode] = modePanel;

            _gridLabels = BuildMenuList(_menuRoot, "Grid",
                Array.ConvertAll(GameConstants.GridSizes, GridConfig.GridLabel), out var gridPanel);
            _gridOpHint = MasuUiFactory.CreateText(gridPanel.transform, "OpHint", "", 22, MasuTheme.TextDim);
            _menuPanels[AppScreen.Grid] = gridPanel;

            _playStyleLabels = BuildMenuList(_menuRoot, "PlayStyle",
                new[] { GamePlayMode.Classic.LabelJa(), GamePlayMode.Quick.LabelJa() }, out var playStylePanel);
            _playStyleHint = MasuUiFactory.CreateText(playStylePanel.transform, "GridHint", "", 24, MasuTheme.CursorGold);
            _playStyleHint.rectTransform.anchoredPosition = new Vector2(0, 120);
            _menuPanels[AppScreen.PlayStyle] = playStylePanel;

            BuildRankingPanel(out var rankPanel);
            _menuPanels[AppScreen.ScoreRanking] = rankPanel;
        }

        private static Text[] BuildMenuList(
            Transform parent,
            string panelName,
            string[] items,
            out GameObject panel,
            float rowStep = 48f)
        {
            panel = MasuUiFactory.CreatePanel(parent, panelName, MasuTheme.MainPanel);
            var labels = new Text[items.Length];
            for (var i = 0; i < items.Length; i++)
            {
                labels[i] = MasuUiFactory.CreateText(panel.transform, $"Item{i}", items[i], 28, MasuTheme.Text);
                labels[i].rectTransform.anchoredPosition = new Vector2(0, 80 - i * rowStep);
            }
            return labels;
        }

        private void BuildRankingPanel(out GameObject rankPanel)
        {
            rankPanel = MasuUiFactory.CreatePanel(_menuRoot, "Ranking", MasuTheme.ResultBg);
            MasuUiFactory.Stretch(rankPanel.GetComponent<RectTransform>());

            var title = MasuUiFactory.CreateText(rankPanel.transform, "RankTitle", "RESULT",
                MasuRankingLayout.TitleFontSize, MasuTheme.ResultText);
            MasuUiFactory.PlaceTopCenter(title.rectTransform, -28f, 480f, 64f);

            _rankingOpTabs = new Text[4];
            for (var i = 0; i < 4; i++)
            {
                _rankingOpTabs[i] = MasuUiFactory.CreateText(rankPanel.transform, $"OpTab{i}",
                    Operations[i].LabelEn(), MasuRankingLayout.OperationTabFontSize, MasuTheme.ResultText,
                    TextAnchor.UpperCenter);
                var opRt = _rankingOpTabs[i].rectTransform;
                opRt.anchorMin = opRt.anchorMax = new Vector2(0.5f, 1f);
                opRt.pivot = new Vector2(0.5f, 1f);
                opRt.anchoredPosition = new Vector2(-270f + i * 180f, -108f);
                opRt.sizeDelta = new Vector2(170f, 40f);
            }

            _rankingGridArrowUp = MasuUiFactory.CreateText(rankPanel.transform, "GridUp", "▲",
                MasuRankingLayout.GridArrowFontSize, MasuTheme.ResultArrow);
            MasuUiFactory.PlaceTopCenter(_rankingGridArrowUp.rectTransform, -158f, 56f, 44f);

            _rankingGridLabel = MasuUiFactory.CreateText(rankPanel.transform, "GridLabel", "4×4",
                MasuRankingLayout.GridLabelFontSize, MasuTheme.ResultText);
            MasuUiFactory.PlaceTopCenter(_rankingGridLabel.rectTransform, -196f, 320f, 44f);

            _rankingGridArrowDown = MasuUiFactory.CreateText(rankPanel.transform, "GridDown", "▼",
                MasuRankingLayout.GridArrowFontSize, MasuTheme.ResultArrow);
            MasuUiFactory.PlaceTopCenter(_rankingGridArrowDown.rectTransform, -234f, 56f, 44f);

            _rankingRows = new Text[10];
            for (var i = 0; i < 10; i++)
            {
                _rankingRows[i] = MasuUiFactory.CreateText(rankPanel.transform, $"RankRow{i}", "-",
                    MasuRankingLayout.RowFontSize, MasuTheme.ResultText, TextAnchor.MiddleCenter);
                MasuUiFactory.PlaceTopCenter(_rankingRows[i].rectTransform,
                    MasuRankingLayout.RowStartY - i * MasuRankingLayout.RowLineHeight,
                    MasuRankingLayout.RowWidth, 36f);
            }

            var hint = MasuUiFactory.CreateText(rankPanel.transform, "RankHint", "↑↓ マス目　←→ 演算　Esc タイトルへ",
                MasuRankingLayout.HintFontSize, MasuTheme.ResultText, TextAnchor.LowerCenter);
            var hintRt = hint.rectTransform;
            hintRt.anchorMin = hintRt.anchorMax = new Vector2(0.5f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.anchoredPosition = new Vector2(0, 24);
            hintRt.sizeDelta = new Vector2(900, 28);
        }

        private void BuildGameLayer()
        {
            var playInner = MasuUiFactory.CreateFramedRegion(
                _gameRoot,
                "MainPlayFrame",
                new Vector2(0f, 0f),
                new Vector2(GameColumnSplitX, 1f),
                new Vector2(16f, 16f),
                new Vector2(-8f, -16f),
                MasuTheme.MainInner,
                regionScale: GameFrameRegionScaleX,
                regionScaleY: GameFrameRegionScaleY);

            var gridHost = new GameObject("GridHost", typeof(RectTransform));
            gridHost.transform.SetParent(playInner, false);
            var rt = gridHost.GetComponent<RectTransform>();
            var gridPad = (1f - MainGridFrameFill) * 0.5f;
            rt.anchorMin = new Vector2(gridPad, 0.1f);
            rt.anchorMax = new Vector2(1f - gridPad, 1f - gridPad);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _gridView = new MasuGridView(gridHost.transform);

            _guideText = MasuUiFactory.CreateText(playInner, "Guide", "", 16, MasuTheme.TextDim, TextAnchor.LowerCenter);
            _guideText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            _guideText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            _guideText.rectTransform.pivot = new Vector2(0.5f, 0f);
            _guideText.rectTransform.anchoredPosition = new Vector2(0, 12);
            _guideText.rectTransform.sizeDelta = new Vector2(640, 48);

            BuildScoreSidebar();
        }

        private void BuildScoreSidebar()
        {
            var scoreInner = MasuUiFactory.CreateFramedRegion(
                _gameRoot,
                "ScoreFrame",
                new Vector2(GameColumnSplitX, 0f),
                new Vector2(1f, 1f),
                new Vector2(MasuScoreSidebarLayout.GapFromMain, 16f),
                new Vector2(-MasuScoreSidebarLayout.MarginRight, -16f),
                MasuTheme.StatusInner,
                regionScale: MasuScoreSidebarLayout.FrameRegionScaleX,
                regionScaleY: MasuScoreSidebarLayout.FrameRegionScaleY);

            _sidebarMode = MasuUiFactory.CreateText(scoreInner, "Mode", "", MasuScoreSidebarLayout.ModeFontSize,
                MasuTheme.CursorGold, TextAnchor.UpperCenter);
            _sidebarMode.fontStyle = FontStyle.Bold;
            MasuFonts.Apply(_sidebarMode, MasuScoreSidebarLayout.ModeFontSize);
            MasuUiFactory.PlaceTopCenter(_sidebarMode.rectTransform, -12f, 230f, 48f);

            _sidebarGrid = MasuUiFactory.CreateText(scoreInner, "GridSize", "", MasuScoreSidebarLayout.GridFontSize,
                MasuTheme.Text, TextAnchor.UpperCenter);
            MasuUiFactory.PlaceTopCenter(_sidebarGrid.rectTransform, -58f, 230f, 32f);

            var rowY = -96f;
            _sidebarHigh = MasuUiFactory.CreateSidebarStatRow(scoreInner, "High", "最高得点", rowY, MasuTheme.Text,
                MasuScoreSidebarLayout.StatValueFontSize);
            rowY -= 50f;
            _sidebarSession = MasuUiFactory.CreateSidebarStatRow(scoreInner, "Session", "得点", rowY,
                MasuTheme.CursorGold, MasuScoreSidebarLayout.StatValueFontSize);
            rowY -= 50f;
            _sidebarCorrect = MasuUiFactory.CreateSidebarStatRow(scoreInner, "Correct", "正解", rowY, MasuTheme.Text,
                MasuScoreSidebarLayout.CorrectValueFontSize);
            rowY -= 44f;

            MasuUiFactory.CreateSidebarStarsRow(scoreInner, "Answer", "答力 :", rowY, out _sidebarAnswerStars);
            rowY -= 40f;
            MasuUiFactory.CreateSidebarStarsRow(scoreInner, "Skill", "技能 :", rowY, out _sidebarSkillStars);

            var pad = MasuScoreSidebarLayout.ContentPadX;
            var timerLabel = MasuUiFactory.CreateText(scoreInner, "TimerLabel", "時間",
                MasuScoreSidebarLayout.StatLabelFontSize, MasuTheme.Text, TextAnchor.LowerLeft);
            var tlRt = timerLabel.rectTransform;
            tlRt.anchorMin = tlRt.anchorMax = new Vector2(0, 0);
            tlRt.pivot = new Vector2(0, 0);
            tlRt.anchoredPosition = new Vector2(pad, 16f);
            tlRt.sizeDelta = new Vector2(120, 18);

            _sidebarTime = MasuUiFactory.CreateText(scoreInner, "Time", "0:00.00", MasuScoreSidebarLayout.TimerFontSize,
                MasuTheme.GaugeTimer, TextAnchor.LowerRight);
            _sidebarTime.fontStyle = FontStyle.Bold;
            MasuFonts.ApplyTimer(_sidebarTime, MasuScoreSidebarLayout.TimerFontSize);
            var timeRt = _sidebarTime.rectTransform;
            timeRt.anchorMin = timeRt.anchorMax = new Vector2(1, 0);
            timeRt.pivot = new Vector2(1, 0);
            timeRt.anchoredPosition = new Vector2(-pad, 16f);
            timeRt.sizeDelta = new Vector2(200, 36);
        }

        private static readonly string[] ResultMenuItems =
            { "タイトルに戻る", "記録する", "ゲームを終了する" };

        private void BuildResultOverlay()
        {
            _resultContentPanel = MasuUiFactory.CreatePanel(_resultRoot, "Panel", MasuTheme.ResultBg);
            MasuUiFactory.Stretch(_resultContentPanel.GetComponent<RectTransform>());

            _resultTitle = MasuUiFactory.CreateText(_resultContentPanel.transform, "Title", "", 36,
                MasuTheme.ResultText);
            _resultTitle.rectTransform.anchoredPosition = new Vector2(0, 200);
            _resultSummary = MasuUiFactory.CreateText(_resultContentPanel.transform, "Summary", "", 22,
                MasuTheme.ResultText);
            _resultSummary.rectTransform.sizeDelta = new Vector2(900, 100);
            _resultSummary.rectTransform.anchoredPosition = new Vector2(0, 120);

            _resultMenuPanel = MasuUiFactory.CreatePanel(_resultContentPanel.transform, "MenuPanel", Color.clear);
            MasuUiFactory.Stretch(_resultMenuPanel.GetComponent<RectTransform>());
            _resultMenuLabels = BuildMenuList(_resultMenuPanel.transform, "ResultItems", ResultMenuItems, out _);

            _resultConfirmPanel = MasuUiFactory.CreatePanel(_resultContentPanel.transform, "ConfirmPanel",
                MasuTheme.ResultBg);
            MasuUiFactory.Stretch(_resultConfirmPanel.GetComponent<RectTransform>());

            _resultConfirmOp = MasuUiFactory.CreateText(_resultConfirmPanel.transform, "Op", "Addition", 34,
                MasuTheme.CursorGold, TextAnchor.UpperCenter);
            MasuUiFactory.PlaceTopCenter(_resultConfirmOp.rectTransform, -48f, 400f, 40f);

            _resultConfirmGrid = MasuUiFactory.CreateText(_resultConfirmPanel.transform, "Grid", "4×4", 28,
                MasuTheme.ResultText, TextAnchor.UpperCenter);
            MasuUiFactory.PlaceTopCenter(_resultConfirmGrid.rectTransform, -88f, 200f, 36f);

            _resultPreviewRows = new Text[10];
            for (var i = 0; i < 10; i++)
            {
                _resultPreviewRows[i] = MasuUiFactory.CreateText(_resultConfirmPanel.transform, $"Prev{i}", "-",
                    MasuRankingLayout.RowFontSize, MasuTheme.ResultText, TextAnchor.MiddleCenter);
                MasuUiFactory.PlaceTopCenter(_resultPreviewRows[i].rectTransform,
                    MasuRankingLayout.RowStartY - i * MasuRankingLayout.RowLineHeight,
                    MasuRankingLayout.RowWidth, 36f);
            }

            _resultSessionSummary = MasuUiFactory.CreateText(_resultConfirmPanel.transform, "SessionSummary", "",
                MasuRankingLayout.RowFontSize, MasuTheme.ResultText, TextAnchor.LowerCenter);
            var sumRt = _resultSessionSummary.rectTransform;
            sumRt.anchorMin = sumRt.anchorMax = new Vector2(0.5f, 0f);
            sumRt.pivot = new Vector2(0.5f, 0f);
            sumRt.anchoredPosition = new Vector2(0, 48);
            sumRt.sizeDelta = new Vector2(MasuRankingLayout.RowWidth, 40);

            _resultEntryPanel = MasuUiFactory.CreatePanel(_resultContentPanel.transform, "EntryPanel",
                MasuTheme.ResultBg);
            MasuUiFactory.Stretch(_resultEntryPanel.GetComponent<RectTransform>());

            _resultEntryLine = MasuUiFactory.CreateText(_resultEntryPanel.transform, "EntryLine", "",
                MasuRankingLayout.RowFontSize, MasuTheme.ResultText, TextAnchor.UpperCenter);
            MasuUiFactory.PlaceTopCenter(_resultEntryLine.rectTransform, -40f, MasuRankingLayout.RowWidth, 40f);

            BuildNameKeyboardUi(_resultEntryPanel.transform);
        }

        private void BuildNameKeyboardUi(Transform parent)
        {
            var rows = MasuNameKeyboard.Rows;
            _keyboardKeyLabels = new Text[rows.Length][];
            var colStep = MasuNameKeyboardLayout.ColumnStep;
            var halfW = MasuNameKeyboardLayout.KeyboardWidth * 0.5f;
            var pad = 2f;
            for (var r = 0; r < rows.Length; r++)
            {
                var line = rows[r];
                _keyboardKeyLabels[r] = new Text[line.Length];
                for (var c = 0; c < line.Length; c++)
                {
                    var label = MasuUiFactory.CreateText(parent, $"Key_{r}_{c}", line[c],
                        (int)MasuNameKeyboardLayout.KeyFontSize, MasuTheme.ResultText);
                    label.alignment = TextAnchor.MiddleCenter;
                    label.resizeTextForBestFit = true;
                    label.resizeTextMinSize = 10;
                    label.resizeTextMaxSize = (int)MasuNameKeyboardLayout.KeyFontSize;
                    var rt = label.rectTransform;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    var x = -halfW + (c + 0.5f) * colStep;
                    var y = MasuNameKeyboardLayout.TopOffset - r * MasuNameKeyboardLayout.RowStep;
                    rt.anchoredPosition = new Vector2(x, y);
                    rt.sizeDelta = new Vector2(colStep - pad, MasuNameKeyboardLayout.RowStep - pad);
                    _keyboardKeyLabels[r][c] = label;
                }
            }
        }

        private void BuildEndConfirm()
        {
            var panel = MasuUiFactory.CreatePanel(_endConfirmRoot, "Box", MasuTheme.MainPanel);
            var rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(520, 220);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            MasuUiFactory.CreateText(panel.transform, "Msg", "終了して採点しますか？", 26, MasuTheme.Text);
            MasuUiFactory.CreateText(panel.transform, "Yes", "▶ はい", 24, MasuTheme.CursorGold)
                .rectTransform.anchoredPosition = new Vector2(0, -40);
            MasuUiFactory.CreateText(panel.transform, "No", "  いいえ", 24, MasuTheme.Text)
                .rectTransform.anchoredPosition = new Vector2(0, -80);
        }

        private void ShowScreen(AppScreen screen)
        {
            _screen = screen;
            _menuRoot.gameObject.SetActive(screen != AppScreen.Game);
            _gameRoot.gameObject.SetActive(screen == AppScreen.Game);
            foreach (var kv in _menuPanels)
                kv.Value.SetActive(kv.Key == screen);
            RefreshMenuHighlights();
        }

        private void RefreshMenuHighlights()
        {
            HighlightList(_titleLabels, _titleMenu, new[] { "はじめる", "操作説明", "RESULT" });
            HighlightList(_modeLabels, _modeCursor, OperationMenuLabelsJa());
            HighlightList(_gridLabels, _gridCursor,
                Array.ConvertAll(GameConstants.GridSizes, GridConfig.GridLabel));
            if (_gridOpHint != null)
                _gridOpHint.text = _pendingOperation.LabelJa();

            if (_playStyleLabels != null)
            {
                HighlightList(_playStyleLabels, _playStyleCursor,
                    new[] { GamePlayMode.Classic.LabelJa(), GamePlayMode.Quick.LabelJa() });
                if (_playStyleHint != null)
                    _playStyleHint.text = GridConfig.GridLabel(_pendingGridSize);
            }

            for (var i = 0; i < _rankingOpTabs.Length; i++)
                _rankingOpTabs[i].color = i == _rankingOpCursor ? MasuTheme.CursorGold : MasuTheme.ResultText;
            if (_rankingGridLabel != null)
            {
                var board = PlayBoardCatalog.RankingBoards[_rankingBoardCursor];
                _rankingGridLabel.text = PlayBoardCatalog.Label(board);
            }
            RefreshRankingTable();
        }

        private void RefreshRankingTable()
        {
            var op = Operations[_rankingOpCursor];
            var board = PlayBoardCatalog.RankingBoards[_rankingBoardCursor];
            var entries = Leaderboard.Padded(_store.GetLeaderboard(op, board.PlaySize, board.Mode));
            for (var i = 0; i < 10; i++)
                _rankingRows[i].text = Leaderboard.FormatRankingRow(i + 1, entries[i]);
        }

        private static void HighlightList(Text[] labels, int cursor, string[] items)
        {
            if (labels == null) return;
            for (var i = 0; i < labels.Length; i++)
            {
                var sel = i == cursor;
                labels[i].text = (sel ? "▶ " : "  ") + items[i];
                labels[i].color = sel ? MasuTheme.CursorGold : MasuTheme.Text;
            }
        }

        private void StartGame(Operation op, in PlayBoard board)
        {
            _operation = op;
            _gridSize = board.PlaySize;
            _playMode = board.Mode;
            _state.Grid.Configure(board.PlaySize);
            _state.Operation = op;
            _state.PlayMode = board.Mode;
            ResetResultFlow();
            _resultScoreSaved = false;
            ShowScreen(AppScreen.Game);
            RestartGame();
        }

        private void RestartGame()
        {
            _introGen++;
            _scoreGen++;
            StopGameCoroutines();

            _introAnimating = true;
            _scoreAnimating = false;
            _revealedHeaders.Clear();
            _scoreMarks.Clear();
            _quickMarks.Clear();
            _quickPopGen++;
            _displayScore = 0;
            _lastAccuracy = 0;
            _gridView.Rebuild(_state.Grid);
            StartCoroutine(RebuildGridAfterLayout());
            _state.Reset(startClock: false);
            _gridView.ResetPopScales();
            RefreshGrid();
            UpdateSidebar();
            UpdateEndGuide();
            _introRoutine = StartCoroutine(RunIntroAnimation());
        }

        private IEnumerator RebuildGridAfterLayout()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            _gridView.Rebuild(_state.Grid);
            RefreshGrid();
        }

        private IEnumerator RunIntroAnimation()
        {
            var gen = _introGen;
            _state.StartRealtime = Time.realtimeSinceStartupAsDouble;
            _timerRoutine = StartCoroutine(TickTimer());
            var cells = _state.Grid.HeaderRevealCells();
            var (scaleMs, staggerMs) = AnimTimings.IntroAnimTimings(cells.Count);
            var scales = AnimTimings.IntroPopScales();

            for (var i = 0; i < cells.Count; i++)
            {
                if (gen != _introGen) yield break;
                var (r, c) = cells[i];
                foreach (var s in scales)
                {
                    _gridView.PopCell(r, c, s);
                    RefreshGrid();
                    yield return new WaitForSeconds(scaleMs / 1000f);
                }
                _revealedHeaders.Add((r, c));
                _gridView.PopCell(r, c, 1f);
                RefreshGrid();
                yield return new WaitForSeconds(staggerMs / 1000f);
            }

            if (gen != _introGen) yield break;
            _introAnimating = false;
            _gridView.ResetPopScales();
            RefreshGrid();
            UpdateEndGuide();
        }

        private IEnumerator TickTimer()
        {
            while (_screen == AppScreen.Game)
            {
                if (!_state.Finished && _state.StartRealtime.HasValue)
                    UpdateSidebarTime();
                yield return new WaitForSeconds(0.05f);
            }
        }

        private void RefreshGrid() =>
            _gridView.Refresh(_state, _revealedHeaders, _scoreMarks, _quickMarks, _introAnimating, _scoreAnimating);

        private void UpdateSidebar()
        {
            var s = _state;
            var playN = s.Grid.PlayCellCount;
            int correctN;
            if (_scoreMarks.Count > 0)
            {
                correctN = 0;
                foreach (var kv in _scoreMarks)
                    if (kv.Value) correctN++;
            }
            else if (s.Finished || s.PlayMode == GamePlayMode.Quick)
                correctN = s.CorrectCount();
            else
                correctN = s.FilledCount();

            var correctText = s.Finished || s.PlayMode == GamePlayMode.Quick
                ? $"{s.CorrectCount()} / {playN}"
                : $"{s.FilledCount()} / {playN}";

            var starMax = GameConstants.StarGaugeMax;
            var answerStars = playN > 0 ? starMax * correctN / playN : 0;
            var skillStars = Math.Max(0, starMax - Math.Min(starMax, s.PenaltyCount));

            _sidebarMode.text = _operation.LabelEn();
            _sidebarGrid.text = GridConfig.BoardLabel(_gridSize, _playMode);
            _sidebarHigh.text = $"{_store.HighScore(_operation, _gridSize, _playMode):N0}";
            _sidebarSession.text = s.Finished ? $"{_displayScore:N0}" : "---";
            _sidebarCorrect.text = correctText;
            _sidebarAnswerStars.text = FormatStarGauge(answerStars, starMax);
            _sidebarSkillStars.text = FormatStarGauge(skillStars, starMax);
            UpdateSidebarTime();
        }

        private static string FormatStarGauge(int filled, int total)
        {
            filled = Math.Max(0, Math.Min(total, filled));
            return new string('★', filled) + new string('☆', total - filled);
        }

        private void UpdateSidebarTime()
        {
            var elapsed = _state.StartRealtime.HasValue || _state.ScoringPlayElapsed.HasValue
                ? _state.ElapsedSeconds()
                : 0;
            _sidebarTime.text = GameLogic.FormatTimeTabular(elapsed);
        }

        private void UpdateEndGuide()
        {
            if (_state.Finished || _introAnimating || _scoreAnimating)
                _guideText.text = "";
            else if (_playMode == GamePlayMode.Quick)
                _guideText.text = "移動で採点・最終マスは Enter 確定・全マス○でクリア";
            else
                _guideText.text = "Enter キーで終了・採点の確認";
        }

        private void PromptEndGame()
        {
            if (_state.Finished || _introAnimating || _scoreAnimating) return;
            _endConfirmChoice = 0;
            _endConfirmVisible = true;
            _endConfirmRoot.gameObject.SetActive(true);
        }

        private void FinishWithScore()
        {
            _state.CommitFocus();
            _state.FreezeScoringClock();
            _state.Finish();
            _scoreGen++;
            _scoreMarks.Clear();
            UpdateSidebar();
            RefreshGrid();
            if (_scoreRoutine != null) StopCoroutine(_scoreRoutine);
            if (_playMode == GamePlayMode.Quick)
            {
                foreach (var (r, c) in _state.Grid.PlayCellsScoreOrder())
                    _scoreMarks[(r, c)] = _state.Cells[(r, c)].State == CellState.Correct;
                _scoreAnimating = false;
                RefreshGrid();
                FinalizeScore();
                ShowScoreResults();
                return;
            }
            _scoreAnimating = true;
            _scoreRoutine = StartCoroutine(RunScoreAnimation());
        }

        private IEnumerator RunScoreAnimation()
        {
            var gen = _scoreGen;
            var cells = _state.Grid.PlayCellsScoreOrder();
            var (scaleMs, staggerMs) = AnimTimings.ScoreAnimTimings(cells.Count);
            var scales = AnimTimings.ScorePopScales();

            for (var i = 0; i < cells.Count; i++)
            {
                if (gen != _scoreGen) yield break;
                var (r, c) = cells[i];
                foreach (var s in scales)
                {
                    _gridView.PopCell(r, c, s);
                    RefreshGrid();
                    yield return new WaitForSeconds(scaleMs / 1000f);
                }
                var ok = _playMode == GamePlayMode.Quick
                    ? _state.Cells[(r, c)].State == CellState.Correct
                    : _state.GradeCellAtScore(r, c);
                _scoreMarks[(r, c)] = ok;
                _gridView.PopCell(r, c, 1f);
                UpdateSidebar();
                RefreshGrid();
                yield return new WaitForSeconds(staggerMs / 1000f);
            }

            if (gen != _scoreGen) yield break;
            _scoreAnimating = false;
            FinalizeScore();
            ShowScoreResults();
        }

        private void FinalizeScore()
        {
            var correct = _state.CorrectCount();
            var playN = _state.Grid.PlayCellCount;
            _lastAccuracy = Scoring.AccuracyPercent(correct, playN);
            var elapsed = _state.ElapsedSeconds();
            _displayScore = Scoring.ComputeSessionScore(elapsed, correct, playN, _operation);
            if (!_resultScoreSaved)
            {
                _store.RecordSession(_operation, _gridSize, _playMode, _displayScore);
                _resultScoreSaved = true;
            }
            _lastElapsedSeconds = elapsed;
            _lastPlayedAt = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
            UpdateSidebar();
        }

        private void ShowScoreResults()
        {
            var wrong = _state.WrongCount();
            var empty = _state.EmptyCount();
            if (wrong == 0 && empty == 0)
            {
                _resultTitle.text = "クリア！";
                _resultSummary.text = "すべて正解です。";
            }
            else
            {
                _resultTitle.text = "採点結果";
                var parts = new List<string>();
                if (wrong > 0) parts.Add($"不正解: {wrong} マス");
                if (empty > 0) parts.Add($"未入力: {empty} マス（+{GameConstants.PenaltyEmptySeconds * empty} 秒）");
                parts.Add(
                    $"正解 {_state.CorrectCount()}/{_state.Grid.PlayCellCount}  得点 {_displayScore:N0}  タイム {GameLogic.FormatTime(_state.ElapsedSeconds())}");
                _resultSummary.text = string.Join(" / ", parts);
            }
            ResetResultFlow();
            _resultRoot.gameObject.SetActive(true);
            ShowResultPanel();
        }

        private void ResetResultFlow()
        {
            _resultView = ResultView.Menu;
            _resultMenu = 0;
            _entryName = "";
            _recordConfirmEnterOpensKeyboard = true;
            _nameKeyboard = new MasuNameKeyboard();
        }

        private void ShowResultPanel()
        {
            var isMenu = _resultView == ResultView.Menu;
            _resultTitle.gameObject.SetActive(isMenu);
            _resultSummary.gameObject.SetActive(isMenu);
            _resultMenuPanel.SetActive(isMenu);
            _resultConfirmPanel.SetActive(_resultView == ResultView.Confirm);
            _resultEntryPanel.SetActive(_resultView == ResultView.Entry);

            if (_resultView == ResultView.Menu)
                HighlightResultMenu();
            else if (_resultView == ResultView.Confirm)
                RefreshRecordConfirmPanel();
            else if (_resultView == ResultView.Entry)
                RefreshNameEntryPanel();
        }

        private void HighlightResultMenu()
        {
            for (var i = 0; i < _resultMenuLabels.Length; i++)
            {
                var sel = i == _resultMenu;
                _resultMenuLabels[i].text = (sel ? "▶ " : "  ") + ResultMenuItems[i];
                _resultMenuLabels[i].color = sel ? MasuTheme.CursorGold : MasuTheme.ResultText;
            }
        }

        private void RefreshRecordConfirmPanel()
        {
            _resultConfirmOp.text = _operation.LabelEn();
            _resultConfirmGrid.text = GridConfig.BoardLabel(_gridSize, _playMode);

            var previewName = string.IsNullOrWhiteSpace(_entryName) ? "-" : _entryName.Trim();
            var (entries, rank) = Leaderboard.PreviewAfterInsert(
                _store.GetLeaderboard(_operation, _gridSize, _playMode),
                previewName,
                _displayScore,
                _lastPlayedAt,
                _lastAccuracy,
                _lastElapsedSeconds);
            _entryPreviewRank = rank;
            var padded = Leaderboard.Padded(entries);
            for (var i = 0; i < 10; i++)
            {
                _resultPreviewRows[i].text = Leaderboard.FormatRankingRow(i + 1, padded[i]);
                _resultPreviewRows[i].color = i + 1 == rank ? MasuTheme.CursorGold : MasuTheme.ResultText;
            }

            var played = string.IsNullOrEmpty(_lastPlayedAt) ? "----/--/-- --:--" : _lastPlayedAt;
            _resultSessionSummary.text =
                $"{GameLogic.FormatTime(_lastElapsedSeconds)}  {played}  {_lastAccuracy:F1}%";
        }

        private void RefreshNameEntryPanel()
        {
            var played = string.IsNullOrEmpty(_lastPlayedAt) ? "----/--/-- --:--" : _lastPlayedAt;
            _resultEntryLine.text =
                $"{_entryPreviewRank}{MasuNameKeyboard.FormatNameSlots(_entryName, Leaderboard.NameDisplayMax)}  " +
                $"{GameLogic.FormatTime(_lastElapsedSeconds)}  {Leaderboard.FormatPlayedDate(_lastPlayedAt)}  " +
                $"{Leaderboard.FormatAccuracy(_lastAccuracy)}";
            RefreshKeyboardHighlight();
        }

        private void RefreshKeyboardHighlight()
        {
            if (_keyboardKeyLabels == null) return;
            for (var r = 0; r < _keyboardKeyLabels.Length; r++)
            for (var c = 0; c < _keyboardKeyLabels[r].Length; c++)
            {
                var sel = r == _nameKeyboard.Row && c == _nameKeyboard.Col;
                _keyboardKeyLabels[r][c].color = sel ? MasuTheme.CursorGold : MasuTheme.ResultText;
            }
        }

        private void BeginScoreRecord()
        {
            var previewName = string.IsNullOrWhiteSpace(_entryName) ? "-" : _entryName.Trim();
            var (_, rank) = Leaderboard.PreviewAfterInsert(
                _store.GetLeaderboard(_operation, _gridSize, _playMode),
                previewName,
                _displayScore,
                _lastPlayedAt,
                _lastAccuracy,
                _lastElapsedSeconds);
            _entryPreviewRank = rank;
            _recordConfirmEnterOpensKeyboard = string.IsNullOrWhiteSpace(_entryName);
            if (_recordConfirmEnterOpensKeyboard)
                _entryName = "";
            _nameKeyboard = new MasuNameKeyboard();
            _resultView = ResultView.Confirm;
            ShowResultPanel();
        }

        private void SubmitScoreEntry()
        {
            var name = (_entryName ?? "").Trim();
            if (!string.IsNullOrEmpty(name))
            {
                _store.AddLeaderboardEntry(
                    _operation, _gridSize, _playMode, name, _displayScore, _lastPlayedAt, _lastAccuracy,
                    _lastElapsedSeconds);
            }
            _entryName = "";
            _recordConfirmEnterOpensKeyboard = true;
            _resultView = ResultView.Menu;
            UpdateSidebar();
            ShowResultPanel();
        }

        private static void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ReturnToTitle()
        {
            ResetResultFlow();
            _resultRoot.gameObject.SetActive(false);
            _endConfirmVisible = false;
            if (_endConfirmRoot != null)
                _endConfirmRoot.gameObject.SetActive(false);
            _titleMenu = 0;
            StopGameCoroutines();
            _introAnimating = false;
            _scoreAnimating = false;
            ShowScreen(AppScreen.Title);
        }

        private static int NavVertical(int cursor, int delta, int count) =>
            count <= 0 ? cursor : (cursor + delta + count) % count;

        private void HandleLauncherInput()
        {
            var n = MasuDisplaySettings.Options.Length;
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _launcher.SelectedIndex = NavVertical(_launcher.SelectedIndex, -1, n);
                MasuDisplaySettings.ApplyPreview(_launcher.SelectedIndex);
                _launcher.Refresh();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _launcher.SelectedIndex = NavVertical(_launcher.SelectedIndex, 1, n);
                MasuDisplaySettings.ApplyPreview(_launcher.SelectedIndex);
                _launcher.Refresh();
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                _launcher.AskEveryTime = !_launcher.AskEveryTime;
                _launcher.Refresh();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ConfirmLauncherAndStartGame();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }

        private void ConfirmLauncherAndStartGame()
        {
            MasuDisplaySettings.CommitSession(_launcher.SelectedIndex, _launcher.AskEveryTime);
            _displayGuard.Recapture();
            _launcher.Close();
            _menuRoot.gameObject.SetActive(true);
            _titleMenu = 0;
            ShowScreen(AppScreen.Title);
        }

        private void HandleTitleInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) { _titleMenu = NavVertical(_titleMenu, -1, 3); RefreshMenuHighlights(); }
            else if (Input.GetKeyDown(KeyCode.DownArrow)) { _titleMenu = NavVertical(_titleMenu, 1, 3); RefreshMenuHighlights(); }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (_titleMenu == 0) { _modeCursor = 0; ShowScreen(AppScreen.Mode); }
                else if (_titleMenu == 1) ShowScreen(AppScreen.Help);
                else { _rankingOpCursor = 0; _rankingBoardCursor = 0; ShowScreen(AppScreen.ScoreRanking); }
            }
            else if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
        }

        private void HandleHelpInput()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Escape))
                ShowScreen(AppScreen.Title);
        }

        private void HandleModeInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) { _modeCursor = NavVertical(_modeCursor, -1, 4); RefreshMenuHighlights(); }
            else if (Input.GetKeyDown(KeyCode.DownArrow)) { _modeCursor = NavVertical(_modeCursor, 1, 4); RefreshMenuHighlights(); }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                _pendingOperation = Operations[_modeCursor];
                _gridCursor = 0;
                ShowScreen(AppScreen.Grid);
            }
            else if (Input.GetKeyDown(KeyCode.Escape)) ShowScreen(AppScreen.Title);
        }

        private void HandleGridInput()
        {
            var n = GameConstants.GridSizes.Length;
            if (Input.GetKeyDown(KeyCode.UpArrow)) { _gridCursor = NavVertical(_gridCursor, -1, n); RefreshMenuHighlights(); }
            else if (Input.GetKeyDown(KeyCode.DownArrow)) { _gridCursor = NavVertical(_gridCursor, 1, n); RefreshMenuHighlights(); }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                _pendingGridSize = GameConstants.GridSizes[_gridCursor];
                _playStyleCursor = 0;
                ShowScreen(AppScreen.PlayStyle);
            }
            else if (Input.GetKeyDown(KeyCode.Escape)) ShowScreen(AppScreen.Mode);
        }

        private void HandlePlayStyleInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _playStyleCursor = NavVertical(_playStyleCursor, -1, 2);
                RefreshMenuHighlights();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _playStyleCursor = NavVertical(_playStyleCursor, 1, 2);
                RefreshMenuHighlights();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                var mode = _playStyleCursor == 0 ? GamePlayMode.Classic : GamePlayMode.Quick;
                StartGame(_pendingOperation, new PlayBoard(_pendingGridSize, mode));
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                ShowScreen(AppScreen.Grid);
            }
        }

        private void HandleRankingInput()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                _rankingOpCursor = (_rankingOpCursor - 1 + 4) % 4;
                RefreshMenuHighlights();
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                _rankingOpCursor = (_rankingOpCursor + 1) % 4;
                RefreshMenuHighlights();
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                var n = PlayBoardCatalog.RankingBoards.Length;
                _rankingBoardCursor = (_rankingBoardCursor - 1 + n) % n;
                RefreshMenuHighlights();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                var n = PlayBoardCatalog.RankingBoards.Length;
                _rankingBoardCursor = (_rankingBoardCursor + 1) % n;
                RefreshMenuHighlights();
            }
            else if (Input.GetKeyDown(KeyCode.Escape)) ShowScreen(AppScreen.Title);
        }

        private void HandleGameInput()
        {
            if (_state.Finished || _introAnimating || _scoreAnimating) return;

            if (TryMoveFocus()) return;
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                ClearQuickMarkOnEdit();
                _state.HandleBackspace();
                RefreshGrid();
                UpdateEndGuide();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (_playMode == GamePlayMode.Classic)
                {
                    _state.CommitFocus();
                    RefreshGrid();
                    PromptEndGame();
                }
                else
                {
                    _state.QuickGradeFocusedCell();
                    ApplyQuickGradeFeedback();
                    RefreshGrid();
                    UpdateSidebar();
                    TryCompleteQuickMode();
                }
                return;
            }
            if (_state.Operation == Operation.Sub && Input.GetKeyDown(KeyCode.Minus))
            {
                ClearQuickMarkOnEdit();
                _state.HandleChar('-');
                RefreshGrid();
                return;
            }
            if (_state.Operation == Operation.Div && Input.GetKeyDown(KeyCode.Period))
            {
                ClearQuickMarkOnEdit();
                _state.HandleChar('.');
                RefreshGrid();
                return;
            }
            for (var d = 0; d <= 9; d++)
            {
                var kc = KeyCode.Alpha0 + d;
                if (Input.GetKeyDown(kc) || Input.GetKeyDown(KeyCode.Keypad0 + d))
                {
                    ClearQuickMarkOnEdit();
                    _state.HandleChar((char)('0' + d));
                    RefreshGrid();
                    return;
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape)) ReturnToTitle();
        }

        private bool TryMoveFocus()
        {
            if (!TryReadFocusMoveDelta(out var dr, out var dc))
            {
                _focusMoveNextRepeat = 0f;
                return false;
            }

            var pressedThisFrame =
                (dr == -1 && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)))
                || (dr == 1 && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)))
                || (dc == -1 && (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)))
                || (dc == 1 && (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)));

            var now = Time.unscaledTime;
            if (pressedThisFrame)
                _focusMoveNextRepeat = now + FocusMoveInitialDelay;
            else
            {
                if (_focusMoveNextRepeat <= 0f)
                    _focusMoveNextRepeat = now + FocusMoveInitialDelay;
                if (now < _focusMoveNextRepeat)
                    return false;
                _focusMoveNextRepeat = now + FocusMoveRepeatInterval;
            }

            _state.MoveFocus(dr, dc);
            ApplyQuickGradeFeedback();
            RefreshGrid();
            UpdateSidebar();
            TryCompleteQuickMode();
            return true;
        }

        private void ApplyQuickGradeFeedback()
        {
            if (_playMode != GamePlayMode.Quick) return;
            var ev = _state.TakeQuickGradeFeedback();
            if (!ev.HasValue) return;
            _quickMarks[(ev.Row, ev.Col)] = ev.Correct;
            if (_quickPopRoutine != null) StopCoroutine(_quickPopRoutine);
            _quickPopGen++;
            _quickPopRoutine = StartCoroutine(PopQuickGradeMark(ev.Row, ev.Col, _quickPopGen));
        }

        private IEnumerator PopQuickGradeMark(int row, int col, int gen)
        {
            var scales = new[] { 1.35f, 0.92f, 1.12f, 1f };
            foreach (var s in scales)
            {
                if (gen != _quickPopGen) yield break;
                _gridView.PopMark(row, col, s);
                RefreshGrid();
                yield return new WaitForSeconds(0.045f);
            }
            _gridView.ResetMarkScales();
            RefreshGrid();
        }

        private void ClearQuickMarkOnEdit()
        {
            if (_playMode != GamePlayMode.Quick) return;
            _quickMarks.Remove((_state.FocusRow, _state.FocusCol));
        }

        private void TryCompleteQuickMode()
        {
            if (_playMode != GamePlayMode.Quick || _state.Finished || _introAnimating || _scoreAnimating)
                return;
            if (!_state.IsQuickClear()) return;
            FinishWithScore();
        }

        private static bool TryReadFocusMoveDelta(out int dr, out int dc)
        {
            dr = 0;
            dc = 0;
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) dr = -1;
            else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) dr = 1;
            else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) dc = -1;
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) dc = 1;
            return dr != 0 || dc != 0;
        }

        private void HandleEndConfirmInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
                _endConfirmChoice = 1 - _endConfirmChoice;
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                _endConfirmVisible = false;
                _endConfirmRoot.gameObject.SetActive(false);
                if (_endConfirmChoice == 0) FinishWithScore();
                else UpdateEndGuide();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                _endConfirmVisible = false;
                _endConfirmRoot.gameObject.SetActive(false);
                UpdateEndGuide();
            }
        }

        private void HandleResultInput()
        {
            if (_resultView == ResultView.Entry)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow))
                    _nameKeyboard.Move(-1, 0);
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                    _nameKeyboard.Move(1, 0);
                else if (Input.GetKeyDown(KeyCode.LeftArrow))
                    _nameKeyboard.Move(0, -1);
                else if (Input.GetKeyDown(KeyCode.RightArrow))
                    _nameKeyboard.Move(0, 1);
                else if (Input.GetKeyDown(KeyCode.Backspace))
                    _entryName = _entryName.Length > 0 ? _entryName.Substring(0, _entryName.Length - 1) : _entryName;
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    var (newName, exitToMenu) = _nameKeyboard.ApplyKey(_entryName, Leaderboard.NameDisplayMax);
                    _entryName = newName;
                    if (exitToMenu)
                    {
                        _recordConfirmEnterOpensKeyboard = false;
                        _resultView = ResultView.Menu;
                        ShowResultPanel();
                        return;
                    }
                }
                else
                    return;

                RefreshNameEntryPanel();
                return;
            }

            if (_resultView == ResultView.Confirm)
            {
                if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                {
                    _resultView = ResultView.Menu;
                    ShowResultPanel();
                }
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    if (_recordConfirmEnterOpensKeyboard)
                    {
                        _recordConfirmEnterOpensKeyboard = false;
                        _nameKeyboard = new MasuNameKeyboard();
                        _resultView = ResultView.Entry;
                        ShowResultPanel();
                    }
                    else if (!string.IsNullOrWhiteSpace(_entryName))
                        SubmitScoreEntry();
                    else
                    {
                        _recordConfirmEnterOpensKeyboard = false;
                        _nameKeyboard = new MasuNameKeyboard();
                        _resultView = ResultView.Entry;
                        ShowResultPanel();
                    }
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _resultMenu = NavVertical(_resultMenu, -1, ResultMenuItems.Length);
                ShowResultPanel();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _resultMenu = NavVertical(_resultMenu, 1, ResultMenuItems.Length);
                ShowResultPanel();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (_resultMenu == 0) ReturnToTitle();
                else if (_resultMenu == 1) BeginScoreRecord();
                else QuitApplication();
            }
        }
    }
}
