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
        private Operation _operation = Operation.Add;
        private Operation _pendingOperation = Operation.Add;
        private int _gridSize = 4;

        private int _titleMenu;
        private int _modeCursor;
        private int _gridCursor;
        private int _rankingOpCursor;
        private int _rankingGridCursor;

        private bool _introAnimating;
        private bool _scoreAnimating;
        private int _introGen;
        private int _scoreGen;
        private readonly HashSet<(int r, int c)> _revealedHeaders = new();
        private readonly Dictionary<(int r, int c), bool> _scoreMarks = new();

        private int _displayScore;
        private float _lastAccuracy;
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
        private Text[] _rankingOpTabs;
        private Text[] _rankingGridTabs;
        private Text[] _rankingRows;
        private Text _sidebarMode;
        private Text _sidebarHigh;
        private Text _sidebarSession;
        private Text _sidebarCorrect;
        private Text _sidebarTime;
        private Text _guideText;
        private Text _resultTitle;
        private Text _resultSummary;
        private Text[] _resultMenuLabels;
        private Text[] _resultPreviewRows;
        private InputField _nameInput;
        private GameObject _resultConfirmPanel;
        private GameObject _resultEntryPanel;
        private GameObject _resultMenuPanel;

        private Coroutine _timerRoutine;
        private Coroutine _introRoutine;
        private Coroutine _scoreRoutine;

        private static readonly Operation[] Operations =
            { Operation.Add, Operation.Sub, Operation.Mul, Operation.Div };

        private void Awake()
        {
            _store = ScoreStore.Load();
            BuildUi();
            ShowScreen(AppScreen.Title);
        }

        private void OnDestroy()
        {
            if (_timerRoutine != null) StopCoroutine(_timerRoutine);
            if (_introRoutine != null) StopCoroutine(_introRoutine);
            if (_scoreRoutine != null) StopCoroutine(_scoreRoutine);
        }

        private void Update()
        {
            if (_endConfirmVisible)
            {
                HandleEndConfirmInput();
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
                case AppScreen.ScoreRanking: HandleRankingInput(); break;
                case AppScreen.Game: HandleGameInput(); break;
            }
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            _menuRoot = MasuUiFactory.CreatePanel(canvasGo.transform, "MenuLayer", MasuTheme.Bg).transform;
            _gameRoot = MasuUiFactory.CreatePanel(canvasGo.transform, "GameLayer", MasuTheme.Bg).transform;
            _resultRoot = MasuUiFactory.CreatePanel(canvasGo.transform, "ResultOverlay", new Color(0, 0, 0, 0.75f)).transform;
            _endConfirmRoot = MasuUiFactory.CreatePanel(canvasGo.transform, "EndConfirm", new Color(0, 0, 0, 0.6f)).transform;

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
                "矢印 / WASD … マス移動\n数字 … 入力\nBackspace … 削除\nEnter … 終了・採点\nEsc … タイトルへ",
                24, MasuTheme.Text, TextAnchor.UpperLeft);
            _menuPanels[AppScreen.Help] = helpPanel;

            _modeLabels = BuildMenuList(_menuRoot, "Mode",
                new[] { "たし算", "ひき算", "かけ算", "わり算" }, out var modePanel);
            _menuPanels[AppScreen.Mode] = modePanel;

            _gridLabels = BuildMenuList(_menuRoot, "Grid",
                Array.ConvertAll(GameConstants.GridSizes, GridConfig.GridLabel), out var gridPanel);
            _gridOpHint = MasuUiFactory.CreateText(gridPanel.transform, "OpHint", "", 22, MasuTheme.TextDim);
            _menuPanels[AppScreen.Grid] = gridPanel;

            var rankPanel = MasuUiFactory.CreatePanel(_menuRoot, "Ranking", MasuTheme.MainPanel);
            _rankingOpTabs = new Text[4];
            _rankingGridTabs = new Text[GameConstants.GridSizes.Length];
            _rankingRows = new Text[10];
            var rankHeader = MasuUiFactory.CreateText(rankPanel.transform, "RankTitle", "RESULT", 36, MasuTheme.CursorGold);
            rankHeader.rectTransform.anchoredPosition = new Vector2(0, 280);
            for (var i = 0; i < 4; i++)
            {
                _rankingOpTabs[i] = MasuUiFactory.CreateText(rankPanel.transform, $"OpTab{i}",
                    Operations[i].LabelJa(), 20, MasuTheme.TextDim);
                _rankingOpTabs[i].rectTransform.anchoredPosition = new Vector2(-420 + i * 140, 220);
            }
            for (var i = 0; i < GameConstants.GridSizes.Length; i++)
            {
                _rankingGridTabs[i] = MasuUiFactory.CreateText(rankPanel.transform, $"GridTab{i}",
                    GridConfig.GridLabel(GameConstants.GridSizes[i]), 18, MasuTheme.TextDim);
                _rankingGridTabs[i].rectTransform.anchoredPosition = new Vector2(320, 180 - i * 28);
            }
            for (var i = 0; i < 10; i++)
            {
                _rankingRows[i] = MasuUiFactory.CreateText(rankPanel.transform, $"RankRow{i}", "-", 18, MasuTheme.Text,
                    TextAnchor.MiddleLeft);
                _rankingRows[i].rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                _rankingRows[i].rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _rankingRows[i].rectTransform.sizeDelta = new Vector2(900, 28);
                _rankingRows[i].rectTransform.anchoredPosition = new Vector2(0, 120 - i * 30);
            }
            _menuPanels[AppScreen.ScoreRanking] = rankPanel;
        }

        private static Text[] BuildMenuList(
            Transform parent,
            string panelName,
            string[] items,
            out GameObject panel)
        {
            panel = MasuUiFactory.CreatePanel(parent, panelName, MasuTheme.MainPanel);
            var labels = new Text[items.Length];
            for (var i = 0; i < items.Length; i++)
            {
                labels[i] = MasuUiFactory.CreateText(panel.transform, $"Item{i}", items[i], 28, MasuTheme.Text);
                labels[i].rectTransform.anchoredPosition = new Vector2(0, 80 - i * 48);
            }
            return labels;
        }

        private void BuildGameLayer()
        {
            var gridHost = new GameObject("GridHost", typeof(RectTransform));
            gridHost.transform.SetParent(_gameRoot, false);
            var rt = gridHost.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.35f, 0.5f);
            rt.anchorMax = new Vector2(0.35f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            _gridView = new MasuGridView(gridHost.transform);

            var side = MasuUiFactory.CreatePanel(_gameRoot, "Sidebar", MasuTheme.MainPanel);
            var srt = side.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.72f, 0.15f);
            srt.anchorMax = new Vector2(0.95f, 0.85f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;

            _sidebarMode = MasuUiFactory.CreateText(side.transform, "Mode", "", 22, MasuTheme.Text, TextAnchor.UpperLeft);
            _sidebarHigh = MasuUiFactory.CreateText(side.transform, "High", "", 20, MasuTheme.TextDim, TextAnchor.UpperLeft);
            _sidebarSession = MasuUiFactory.CreateText(side.transform, "Session", "", 20, MasuTheme.TextDim, TextAnchor.UpperLeft);
            _sidebarCorrect = MasuUiFactory.CreateText(side.transform, "Correct", "", 20, MasuTheme.Text, TextAnchor.UpperLeft);
            _sidebarTime = MasuUiFactory.CreateText(side.transform, "Time", "", 32, MasuTheme.CursorGold, TextAnchor.UpperLeft);
            _guideText = MasuUiFactory.CreateText(_gameRoot, "Guide", "", 18, MasuTheme.TextDim, TextAnchor.LowerCenter);
            _guideText.rectTransform.anchorMin = new Vector2(0.5f, 0);
            _guideText.rectTransform.anchorMax = new Vector2(0.5f, 0);
            _guideText.rectTransform.anchoredPosition = new Vector2(0, 24);

            var y = -20f;
            foreach (var t in new[] { _sidebarMode, _sidebarHigh, _sidebarSession, _sidebarCorrect, _sidebarTime })
            {
                t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0, 1);
                t.rectTransform.pivot = new Vector2(0, 1);
                t.rectTransform.anchoredPosition = new Vector2(16, y);
                t.rectTransform.sizeDelta = new Vector2(260, 40);
                y -= 36;
            }
            _sidebarTime.fontSize = 36;
        }

        private void BuildResultOverlay()
        {
            var panel = MasuUiFactory.CreatePanel(_resultRoot, "Panel", MasuTheme.MainPanel);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.15f, 0.12f);
            prt.anchorMax = new Vector2(0.85f, 0.88f);
            prt.offsetMin = prt.offsetMax = Vector2.zero;

            _resultTitle = MasuUiFactory.CreateText(panel.transform, "Title", "", 32, MasuTheme.CursorGold);
            _resultTitle.rectTransform.anchoredPosition = new Vector2(0, 240);
            _resultSummary = MasuUiFactory.CreateText(panel.transform, "Summary", "", 20, MasuTheme.Text);
            _resultSummary.rectTransform.sizeDelta = new Vector2(900, 120);

            _resultMenuPanel = MasuUiFactory.CreatePanel(panel.transform, "MenuPanel", Color.clear);
            MasuUiFactory.Stretch(_resultMenuPanel.GetComponent<RectTransform>());
            _resultMenuLabels = BuildMenuList(_resultMenuPanel.transform, "ResultItems",
                new[] { "タイトルへ戻る", "スコアを記録する" }, out _);

            _resultConfirmPanel = MasuUiFactory.CreatePanel(panel.transform, "ConfirmPanel", Color.clear);
            MasuUiFactory.Stretch(_resultConfirmPanel.GetComponent<RectTransform>());
            _resultPreviewRows = new Text[10];
            for (var i = 0; i < 10; i++)
            {
                _resultPreviewRows[i] = MasuUiFactory.CreateText(_resultConfirmPanel.transform, $"Prev{i}", "-", 18,
                    MasuTheme.Text, TextAnchor.MiddleLeft);
                _resultPreviewRows[i].rectTransform.anchoredPosition = new Vector2(-400, 100 - i * 28);
                _resultPreviewRows[i].rectTransform.sizeDelta = new Vector2(880, 26);
            }
            MasuUiFactory.CreateText(_resultConfirmPanel.transform, "ConfirmHint",
                "Enter … 名前入力へ / Esc … 戻る", 18, MasuTheme.TextDim);

            _resultEntryPanel = MasuUiFactory.CreatePanel(panel.transform, "EntryPanel", Color.clear);
            MasuUiFactory.Stretch(_resultEntryPanel.GetComponent<RectTransform>());
            _nameInput = MasuUiFactory.CreateInputField(_resultEntryPanel.transform, "NameInput");
            _nameInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 40);
            _nameInput.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 40);
            MasuUiFactory.CreateText(_resultEntryPanel.transform, "EntryHint",
                "名前を入力して Enter（Esc で戻る）", 18, MasuTheme.TextDim);
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
            HighlightList(_modeLabels, _modeCursor, new[] { "たし算", "ひき算", "かけ算", "わり算" });
            var gridTexts = Array.ConvertAll(GameConstants.GridSizes, GridConfig.GridLabel);
            HighlightList(_gridLabels, _gridCursor, gridTexts);
            if (_gridOpHint != null)
                _gridOpHint.text = _pendingOperation.LabelJa();

            for (var i = 0; i < _rankingOpTabs.Length; i++)
                _rankingOpTabs[i].color = i == _rankingOpCursor ? MasuTheme.CursorGold : MasuTheme.TextDim;
            for (var i = 0; i < _rankingGridTabs.Length; i++)
                _rankingGridTabs[i].color = i == _rankingGridCursor ? MasuTheme.CursorGold : MasuTheme.TextDim;
            RefreshRankingTable();
        }

        private void RefreshRankingTable()
        {
            var op = Operations[_rankingOpCursor];
            var size = GameConstants.GridSizes[_rankingGridCursor];
            var entries = Leaderboard.Padded(_store.GetLeaderboard(op, size));
            for (var i = 0; i < 10; i++)
            {
                var e = entries[i];
                var date = string.IsNullOrEmpty(e.PlayedAt) ? Leaderboard.EmptyDate : e.PlayedAt;
                var name = Leaderboard.IsPlaceholder(e) ? Leaderboard.PlaceholderName : e.Name;
                _rankingRows[i].text =
                    $"{i + 1,2}. {name,-8} {Leaderboard.FormatScore(e.Score)}  {date}  {Leaderboard.FormatAccuracy(e.Accuracy)}";
            }
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

        private void StartGame(Operation op, int size)
        {
            _operation = op;
            _gridSize = size;
            _state.Grid.Configure(size);
            _state.Operation = op;
            ResetResultFlow();
            _resultScoreSaved = false;
            ShowScreen(AppScreen.Game);
            RestartGame();
        }

        private void RestartGame()
        {
            _introGen++;
            _scoreGen++;
            if (_timerRoutine != null) StopCoroutine(_timerRoutine);
            if (_introRoutine != null) StopCoroutine(_introRoutine);
            if (_scoreRoutine != null) StopCoroutine(_scoreRoutine);

            _introAnimating = true;
            _scoreAnimating = false;
            _revealedHeaders.Clear();
            _scoreMarks.Clear();
            _displayScore = 0;
            _lastAccuracy = 0;
            _gridView.Rebuild(_state.Grid);
            _state.Reset(startClock: false);
            _gridView.ResetPopScales();
            RefreshGrid();
            UpdateSidebar();
            UpdateEndGuide();
            _introRoutine = StartCoroutine(RunIntroAnimation());
        }

        private IEnumerator RunIntroAnimation()
        {
            var gen = _introGen;
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
            _state.StartRealtime = Time.realtimeSinceStartupAsDouble;
            _timerRoutine = StartCoroutine(TickTimer());
            RefreshGrid();
            UpdateEndGuide();
        }

        private IEnumerator TickTimer()
        {
            while (_screen == AppScreen.Game)
            {
                if (!_state.Finished && !_introAnimating)
                    UpdateSidebarTime();
                yield return new WaitForSeconds(0.05f);
            }
        }

        private void RefreshGrid() =>
            _gridView.Refresh(_state, _revealedHeaders, _scoreMarks, _introAnimating, _scoreAnimating);

        private void UpdateSidebar()
        {
            var s = _state;
            var playN = s.Grid.PlayCellCount;
            int correctN;
            if (_scoreMarks.Count > 0)
                correctN = 0; // stars during anim use marks
            else if (s.Finished)
                correctN = s.CorrectCount();
            else
                correctN = s.FilledCount();

            foreach (var kv in _scoreMarks)
                if (kv.Value) correctN++;

            var correctText = s.Finished
                ? $"{s.CorrectCount()} / {playN}"
                : $"{s.FilledCount()} / {playN}";

            _sidebarMode.text = $"{_operation.LabelJa()} {GridConfig.GridLabel(_gridSize)}";
            _sidebarHigh.text = $"最高 {_store.HighScore(_operation, _gridSize):N0}";
            _sidebarSession.text = s.Finished ? $"得点 {_displayScore:N0}" : "得点 ---";
            _sidebarCorrect.text = $"入力 {correctText}";
            UpdateSidebarTime();
        }

        private void UpdateSidebarTime()
        {
            var elapsed = _state.StartRealtime.HasValue || _state.ScoringPlayElapsed.HasValue
                ? _state.ElapsedSeconds()
                : 0;
            _sidebarTime.text = GameLogic.FormatTime(elapsed);
        }

        private void UpdateEndGuide()
        {
            if (_state.Finished || _introAnimating || _scoreAnimating)
                _guideText.text = "";
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
            _scoreAnimating = true;
            _scoreGen++;
            _scoreMarks.Clear();
            UpdateSidebar();
            RefreshGrid();
            if (_scoreRoutine != null) StopCoroutine(_scoreRoutine);
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
                var ok = _state.GradeCellAtScore(r, c);
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
                _store.RecordSession(_operation, _gridSize, _displayScore);
                _resultScoreSaved = true;
            }
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
        }

        private void ShowResultPanel()
        {
            _resultMenuPanel.SetActive(_resultView == ResultView.Menu);
            _resultConfirmPanel.SetActive(_resultView == ResultView.Confirm);
            _resultEntryPanel.SetActive(_resultView == ResultView.Entry);

            if (_resultView == ResultView.Menu)
            {
                HighlightList(_resultMenuLabels, _resultMenu, new[] { "タイトルへ戻る", "スコアを記録する" });
            }
            else if (_resultView == ResultView.Confirm)
            {
                var previewName = string.IsNullOrWhiteSpace(_entryName) ? "-" : _entryName.Trim();
                var (entries, rank) = Leaderboard.PreviewAfterInsert(
                    _store.GetLeaderboard(_operation, _gridSize),
                    previewName,
                    _displayScore,
                    _lastPlayedAt,
                    _lastAccuracy);
                _entryPreviewRank = rank;
                var padded = Leaderboard.Padded(entries);
                for (var i = 0; i < 10; i++)
                {
                    var e = padded[i];
                    var hl = i + 1 == rank ? "▶ " : "  ";
                    var date = string.IsNullOrEmpty(e.PlayedAt) ? Leaderboard.EmptyDate : e.PlayedAt;
                    _resultPreviewRows[i].text =
                        $"{hl}{i + 1,2}. {e.Name,-8} {Leaderboard.FormatScore(e.Score)}  {date}  {Leaderboard.FormatAccuracy(e.Accuracy)}";
                    _resultPreviewRows[i].color = i + 1 == rank ? MasuTheme.CursorGold : MasuTheme.Text;
                }
            }
            else if (_resultView == ResultView.Entry)
            {
                _nameInput.text = _entryName;
                _nameInput.Select();
                _nameInput.ActivateInputField();
            }
        }

        private void BeginScoreRecord()
        {
            var (_, rank) = Leaderboard.PreviewAfterInsert(
                _store.GetLeaderboard(_operation, _gridSize),
                "-",
                _displayScore,
                _lastPlayedAt,
                _lastAccuracy);
            _entryPreviewRank = rank;
            _entryName = "";
            _resultView = ResultView.Confirm;
            ShowResultPanel();
        }

        private void SubmitScoreEntry()
        {
            _entryName = _nameInput.text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(_entryName))
            {
                _store.AddLeaderboardEntry(
                    _operation, _gridSize, _entryName, _displayScore, _lastPlayedAt, _lastAccuracy);
            }
            _entryName = "";
            _resultView = ResultView.Menu;
            UpdateSidebar();
            ShowResultPanel();
        }

        private void ReturnToTitle()
        {
            ResetResultFlow();
            _resultRoot.gameObject.SetActive(false);
            _titleMenu = 0;
            if (_timerRoutine != null) StopCoroutine(_timerRoutine);
            ShowScreen(AppScreen.Title);
        }

        private static int NavVertical(int cursor, int delta, int count) =>
            count <= 0 ? cursor : (cursor + delta + count) % count;

        private void HandleTitleInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) { _titleMenu = NavVertical(_titleMenu, -1, 3); RefreshMenuHighlights(); }
            else if (Input.GetKeyDown(KeyCode.DownArrow)) { _titleMenu = NavVertical(_titleMenu, 1, 3); RefreshMenuHighlights(); }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (_titleMenu == 0) { _modeCursor = 0; ShowScreen(AppScreen.Mode); }
                else if (_titleMenu == 1) ShowScreen(AppScreen.Help);
                else { _rankingOpCursor = 0; _rankingGridCursor = 0; ShowScreen(AppScreen.ScoreRanking); }
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
                StartGame(_pendingOperation, GameConstants.GridSizes[_gridCursor]);
            else if (Input.GetKeyDown(KeyCode.Escape)) ShowScreen(AppScreen.Mode);
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
                _rankingGridCursor = (_rankingGridCursor - 1 + GameConstants.GridSizes.Length) % GameConstants.GridSizes.Length;
                RefreshMenuHighlights();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _rankingGridCursor = (_rankingGridCursor + 1) % GameConstants.GridSizes.Length;
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
                _state.HandleBackspace();
                RefreshGrid();
                UpdateEndGuide();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                _state.CommitFocus();
                RefreshGrid();
                PromptEndGame();
                return;
            }
            if (_state.Operation == Operation.Sub && Input.GetKeyDown(KeyCode.Minus))
            {
                _state.HandleChar('-');
                RefreshGrid();
                return;
            }
            if (_state.Operation == Operation.Div && Input.GetKeyDown(KeyCode.Period))
            {
                _state.HandleChar('.');
                RefreshGrid();
                return;
            }
            for (var d = 0; d <= 9; d++)
            {
                var kc = KeyCode.Alpha0 + d;
                if (Input.GetKeyDown(kc) || Input.GetKeyDown(KeyCode.Keypad0 + d))
                {
                    _state.HandleChar((char)('0' + d));
                    RefreshGrid();
                    return;
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape)) ReturnToTitle();
        }

        private bool TryMoveFocus()
        {
            int dr = 0, dc = 0;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) dr = -1;
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) dr = 1;
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) dc = -1;
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) dc = 1;
            if (dr == 0 && dc == 0) return false;
            _state.MoveFocus(dr, dc);
            RefreshGrid();
            return true;
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
                _entryName = _nameInput.text ?? "";
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    _resultView = ResultView.Confirm;
                    ShowResultPanel();
                }
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    SubmitScoreEntry();
                return;
            }
            if (_resultView == ResultView.Confirm)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    _resultView = ResultView.Menu;
                    ShowResultPanel();
                }
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    _resultView = ResultView.Entry;
                    ShowResultPanel();
                }
                return;
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _resultMenu = NavVertical(_resultMenu, -1, 2);
                ShowResultPanel();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _resultMenu = NavVertical(_resultMenu, 1, 2);
                ShowResultPanel();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (_resultMenu == 0) ReturnToTitle();
                else BeginScoreRecord();
            }
        }
    }
}
