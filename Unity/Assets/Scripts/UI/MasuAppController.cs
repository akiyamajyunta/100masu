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
        private GamePlayMode _pendingPlayMode = GamePlayMode.Classic;
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
        private Text _endConfirmYes;
        private Text _endConfirmNo;
        private Text[] _endConfirmLabels;

        // 演出系: 選択スライドイン / 決定フラッシュ / 暗転 / キーボードスライド
        private readonly Dictionary<Text[], int> _listCursorMemo = new();
        private readonly Dictionary<Text, Vector2> _labelBasePos = new();
        private readonly Dictionary<Text, Coroutine> _labelAnims = new();
        private bool _menuBusy;
        private Image _fadeImage;
        private Text _fadeCaption;
        private RectTransform _keyboardRoot;
        private Coroutine _keyboardSlideRoutine;

        private Transform _menuRoot;
        private Transform _gameRoot;
        private Transform _resultRoot;
        private Transform _endConfirmRoot;
        private Transform _pauseRoot;
        private Text[] _pauseLabels;
        private int _pauseMenu;
        private bool _pauseVisible;
        private readonly Dictionary<AppScreen, GameObject> _menuPanels = new();
        private Text[] _titleLabels;
        private Text[] _modeLabels;
        private Text _modeStepHint;
        private Text[] _gridLabels;
        private Text _gridOpHint;
        private int _pendingGridSize = 4;
        private int _playStyleCursor;
        private Text[] _playStyleLabels;
        private Text _playStyleStepHint;
        private Text _playStyleHint;
        private Text[] _optionsLabels;
        private Text _optionsHint;
        private int _optionsCursor;
        private static readonly string[] OptionsMenuItems = { "戻る" };
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
        private Text _resultConfirmHint;
        private Text _resultSessionSummary;
        private Text _resultEntryLine;
        private Text[][] _keyboardKeyLabels;
        private MasuNameKeyboard _nameKeyboard = new();
        private bool _recordConfirmEnterOpensKeyboard = true;
        private GameObject _resultConfirmPanel;
        private GameObject _resultEntryPanel;
        private GameObject _resultMenuPanel;
        private GameObject _resultMenuBox;
        private GameObject _resultContentPanel;

        private Coroutine _timerRoutine;
        private Coroutine _introRoutine;
        private Coroutine _scoreRoutine;
        private int _layoutGen;
        private int _trackedScreenWidth;
        private int _trackedScreenHeight;

        private double? _clockPausedAt;
        private float _focusMoveNextRepeat;
        private const float FocusMoveInitialDelay = 0.35f;
        private const float FocusMoveRepeatInterval = 0.06f;
        private static readonly Color OverlayDim = new(0f, 0f, 0f, 0.55f);

        private static readonly Operation[] Operations =
            { Operation.Add, Operation.Sub, Operation.Mul, Operation.Div };

        private static readonly string[] TitleMenuItems = { "はじめる", "操作説明", "記録", "オプション" };
        /// <summary>タイトルメニュー: 未選択行の先頭（全角スペースで字下げ）。</summary>
        private const string TitleMenuPrefixIdle = "　　　";
        private const string TitleMenuPrefixSelected = "▶　";
        private static readonly string[] PauseMenuItems = { "ゲームに戻る", "メニューに戻る" };
        private static readonly string[] EndConfirmItems = { "はい", "いいえ" };

        private const float IntroHeaderBudgetSec = 1.0f;
        private const float IntroHeaderEnterSec = 0.3f;
        private const float ScoreStartDelaySec = 0.35f;

        private const float SelectSlideOffsetX = -36f;
        private const float SelectSlideSeconds = 0.22f;
        private const int SelectFlashCount = 2;
        private const int DecideFlashCount = 3;
        private const float DecideFlashInterval = 0.055f;

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
            if (!ask)
                MasuDisplaySettings.ApplySavedForSession();

            BuildUi();
            _displayGuard.Bind(this);

            if (ask)
            {
                _menuRoot.gameObject.SetActive(false);
                _launcher.Open(idx, ask);
            }
            else
                ShowScreen(AppScreen.Title);

            _trackedScreenWidth = Screen.width;
            _trackedScreenHeight = Screen.height;
        }

        private void OnDestroy()
        {
            StopGameCoroutines();
        }

        private void StopGameCoroutines()
        {
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
            // 決定フラッシュ・暗転・キーボード退場中は入力を受け付けない
            if (_menuBusy) return;

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

            if (_pauseVisible)
            {
                HandlePauseInput();
                return;
            }

            if (_resultRoot.gameObject.activeSelf)
            {
                HandleResultInput();
                return;
            }

            CheckScreenSizeChanged();

            switch (_screen)
            {
                case AppScreen.Title: HandleTitleInput(); break;
                case AppScreen.Help: HandleHelpInput(); break;
                case AppScreen.Mode: HandleModeInput(); break;
                case AppScreen.Grid: HandleGridInput(); break;
                case AppScreen.PlayStyle: HandlePlayStyleInput(); break;
                case AppScreen.ScoreRanking: HandleRankingInput(); break;
                case AppScreen.Options: HandleOptionsInput(); break;
                case AppScreen.Game:
                    // セル演出は毎フレーム進める（イントロ・採点中も含む）
                    _gridView.Tick(Time.unscaledDeltaTime);
                    if (!_state.Finished && !_introAnimating && !_scoreAnimating)
                        _gridView.TickFocusPulse(Time.deltaTime);
                    RefreshGrid();
                    if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                        OpenPauseMenu();
                    else
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
            _resultRoot = MasuUiFactory.CreatePanel(canvasGo.transform, "ResultOverlay", OverlayDim).transform;
            _endConfirmRoot = MasuUiFactory.CreatePanel(canvasGo.transform, "EndConfirm", new Color(0, 0, 0, 0.6f)).transform;

            _launcher = new MasuLauncherModal(canvasGo.transform);
            BuildMenuPanels();
            BuildGameLayer();
            BuildResultOverlay();
            BuildEndConfirm();
            BuildPauseOverlay(canvasGo.transform);
            BuildFadeOverlay(canvasGo.transform);

            _gameRoot.gameObject.SetActive(false);
            _resultRoot.gameObject.SetActive(false);
            _endConfirmRoot.gameObject.SetActive(false);
            _pauseRoot.gameObject.SetActive(false);
            _fadeImage.gameObject.SetActive(false);
        }

        /// <summary>ゲーム開始時の暗転用。常に最前面に出してから使う。</summary>
        private void BuildFadeOverlay(Transform canvas)
        {
            var go = MasuUiFactory.CreatePanel(canvas, "FadeOverlay", new Color(0f, 0f, 0f, 0f));
            _fadeImage = go.GetComponent<Image>();
            _fadeCaption = MasuUiFactory.CreateText(go.transform, "Caption", "", 30, MasuTheme.TextDim);
            _fadeCaption.rectTransform.anchorMin = _fadeCaption.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _fadeCaption.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _fadeCaption.rectTransform.anchoredPosition = Vector2.zero;
            _fadeCaption.rectTransform.sizeDelta = new Vector2(900, 48);
            MasuUiAnimations.SetAlpha(_fadeCaption, 0f);
        }

        private void BuildMenuPanels()
        {
            _titleLabels = BuildMenuList(_menuRoot, "Title", TitleMenuItems,
                out var titlePanel, startY: 20f);
            _menuPanels[AppScreen.Title] = titlePanel;
            var heading = MasuUiFactory.CreateText(titlePanel.transform, "Heading", "100マス計算", 48, MasuTheme.CursorGold);
            heading.rectTransform.anchorMin = heading.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            heading.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            heading.rectTransform.anchoredPosition = new Vector2(0, 200);
            heading.rectTransform.sizeDelta = new Vector2(720, 72);

            var helpPanel = MasuUiFactory.CreatePanel(_menuRoot, "Help", MasuTheme.MainPanel);
            var helpText = MasuUiFactory.CreateText(helpPanel.transform, "HelpText",
                "【 プレイ中 】\n" +
                "矢印 / WASD … マス移動（長押しで連続）\n" +
                "数字 … 入力　Backspace … 削除\n" +
                "Shift または Esc … 一時停止（ゲームに戻る / メニューに戻る）\n" +
                "タイマー … ヘッダー表示と同時に計測開始\n" +
                "クラシック … Enter で終了・一括採点\n" +
                "クイック … 別マスへ移動で採点（○ / × 表示）\n" +
                "　　　　　最終マスは Enter で確定（空欄は無視）・全マス○でクリア\n" +
                "誤答 … +10秒　未入力（クラシック採点時）… +10秒\n\n" +
                "【 メニュー 】\n" +
                "はじめる … クラシック/クイック → 演算 → マス目\n" +
                "記録 … スコア一覧（←→ 演算　↑↓ 盤面）\n" +
                "オプション … 準備中（戻る）\n" +
                "Esc … タイトル（演算はプレイスタイルへ、マス目は演算へ）\n" +
                "操作説明 … Enter または Esc でタイトルへ\n\n" +
                "【 リザルト 】\n" +
                "記録確認画面 … Shift で3択メニューへ戻る",
                20, MasuTheme.Text, TextAnchor.UpperLeft);
            MasuUiFactory.Stretch(helpText.rectTransform, 48, 48, 48, 48);
            _menuPanels[AppScreen.Help] = helpPanel;

            _modeLabels = BuildMenuList(_menuRoot, "Mode", OperationMenuLabelsJa(), out var modePanel,
                startY: 20f);
            _modeStepHint = MasuUiFactory.CreateText(modePanel.transform, "StepHint", "【 演算を選ぶ 】", 24,
                MasuTheme.CursorGold);
            _modeStepHint.rectTransform.anchorMin = _modeStepHint.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _modeStepHint.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _modeStepHint.rectTransform.anchoredPosition = new Vector2(0, 200);
            _modeStepHint.rectTransform.sizeDelta = new Vector2(480, 36);
            _menuPanels[AppScreen.Mode] = modePanel;

            _gridLabels = BuildMenuList(_menuRoot, "Grid",
                Array.ConvertAll(GameConstants.GridSizes, GridConfig.GridLabel), out var gridPanel,
                startY: 30f, rowStep: 44f);
            _gridOpHint = MasuUiFactory.CreateText(gridPanel.transform, "OpHint", "", 24, MasuTheme.CursorGold);
            _gridOpHint.rectTransform.anchorMin = _gridOpHint.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _gridOpHint.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _gridOpHint.rectTransform.anchoredPosition = new Vector2(0, 200);
            _gridOpHint.rectTransform.sizeDelta = new Vector2(480, 36);
            _menuPanels[AppScreen.Grid] = gridPanel;

            _playStyleLabels = BuildMenuList(_menuRoot, "PlayStyle",
                new[] { GamePlayMode.Classic.LabelJa(), GamePlayMode.Quick.LabelJa() }, out var playStylePanel,
                startY: 20f);
            _playStyleStepHint = MasuUiFactory.CreateText(playStylePanel.transform, "StepHint", "【 プレイスタイルを選ぶ 】",
                24, MasuTheme.CursorGold);
            _playStyleStepHint.rectTransform.anchorMin = _playStyleStepHint.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _playStyleStepHint.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _playStyleStepHint.rectTransform.anchoredPosition = new Vector2(0, 200);
            _playStyleStepHint.rectTransform.sizeDelta = new Vector2(480, 36);
            _playStyleHint = MasuUiFactory.CreateText(playStylePanel.transform, "GridHint", "", 22, MasuTheme.ResultText);
            _playStyleHint.rectTransform.anchorMin = _playStyleHint.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _playStyleHint.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _playStyleHint.rectTransform.anchoredPosition = new Vector2(0, 158);
            _playStyleHint.rectTransform.sizeDelta = new Vector2(480, 32);
            _menuPanels[AppScreen.PlayStyle] = playStylePanel;

            BuildRankingPanel(out var rankPanel);
            _menuPanels[AppScreen.ScoreRanking] = rankPanel;

            _optionsLabels = BuildMenuList(_menuRoot, "Options", OptionsMenuItems, out var optionsPanel,
                startY: 20f);
            _optionsHint = MasuUiFactory.CreateText(optionsPanel.transform, "OpHint",
                "【 オプション 】（準備中）", 24, MasuTheme.CursorGold);
            _optionsHint.rectTransform.anchorMin = _optionsHint.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _optionsHint.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _optionsHint.rectTransform.anchoredPosition = new Vector2(0, 200);
            _optionsHint.rectTransform.sizeDelta = new Vector2(720, 36);
            _menuPanels[AppScreen.Options] = optionsPanel;
        }

        private static Text[] BuildMenuList(
            Transform parent,
            string panelName,
            string[] items,
            out GameObject panel,
            float startY = 80f,
            float rowStep = 48f,
            Color? panelBackground = null)
        {
            panel = MasuUiFactory.CreatePanel(parent, panelName, panelBackground ?? MasuTheme.MainPanel);
            var labels = new Text[items.Length];
            for (var i = 0; i < items.Length; i++)
            {
                labels[i] = MasuUiFactory.CreateText(panel.transform, $"Item{i}", items[i], 28, MasuTheme.Text);
                labels[i].rectTransform.anchoredPosition = new Vector2(0, startY - i * rowStep);
            }
            return labels;
        }

        private void BuildPauseOverlay(Transform canvas)
        {
            _pauseRoot = MasuUiFactory.CreatePanel(canvas, "PauseOverlay", OverlayDim).transform;
            var box = MasuUiFactory.CreatePanel(_pauseRoot, "Box", Color.clear);
            var rt = box.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(480, 220);
            rt.anchoredPosition = Vector2.zero;

            var caption = MasuUiFactory.CreateText(box.transform, "Caption", "一時停止", 32, MasuTheme.CursorGold);
            caption.rectTransform.anchorMin = caption.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            caption.rectTransform.anchoredPosition = new Vector2(0, 72);
            caption.rectTransform.sizeDelta = new Vector2(400, 48);

            _pauseLabels = new Text[PauseMenuItems.Length];
            for (var i = 0; i < PauseMenuItems.Length; i++)
            {
                _pauseLabels[i] = MasuUiFactory.CreateText(box.transform, $"Pause{i}", PauseMenuItems[i], 28, MasuTheme.Text);
                _pauseLabels[i].rectTransform.anchorMin = _pauseLabels[i].rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _pauseLabels[i].rectTransform.anchoredPosition = new Vector2(0, 10 - i * 48);
                _pauseLabels[i].rectTransform.sizeDelta = new Vector2(420, 40);
            }
        }

        private void BuildRankingPanel(out GameObject rankPanel)
        {
            rankPanel = MasuUiFactory.CreatePanel(_menuRoot, "Ranking", MasuTheme.ResultBg);
            MasuUiFactory.Stretch(rankPanel.GetComponent<RectTransform>());

            var title = MasuUiFactory.CreateText(rankPanel.transform, "RankTitle", "記録",
                MasuRankingLayout.TitleFontSize, MasuTheme.ResultText);
            MasuUiFactory.PlaceTopCenter(title.rectTransform, -28f, 480f, 64f);

            _rankingOpTabs = new Text[4];
            for (var i = 0; i < 4; i++)
            {
                _rankingOpTabs[i] = MasuUiFactory.CreateText(rankPanel.transform, $"OpTab{i}",
                    Operations[i].LabelJa(), MasuRankingLayout.OperationTabFontSize, MasuTheme.ResultText,
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

            var gridHost = new GameObject("GridHost", typeof(RectTransform), typeof(RectMask2D));
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
            _resultContentPanel = MasuUiFactory.CreatePanel(_resultRoot, "Panel", Color.clear);
            MasuUiFactory.Stretch(_resultContentPanel.GetComponent<RectTransform>());

            _resultMenuBox = MasuUiFactory.CreateCenteredPanel(
                _resultContentPanel.transform, "MenuBox", Color.clear,
                new Vector2(720, 400), Vector2.zero);

            _resultTitle = MasuUiFactory.CreateText(_resultMenuBox.transform, "Title", "", 36,
                MasuTheme.CursorGold);
            _resultTitle.rectTransform.anchoredPosition = new Vector2(0, 200);
            _resultSummary = MasuUiFactory.CreateText(_resultMenuBox.transform, "Summary", "", 22,
                MasuTheme.Text);
            _resultSummary.rectTransform.sizeDelta = new Vector2(660, 72);
            _resultSummary.rectTransform.anchoredPosition = new Vector2(0, 150);

            _resultMenuPanel = MasuUiFactory.CreatePanel(_resultMenuBox.transform, "MenuPanel", Color.clear);
            MasuUiFactory.Stretch(_resultMenuPanel.GetComponent<RectTransform>());
            _resultMenuLabels = BuildMenuList(_resultMenuPanel.transform, "ResultItems", ResultMenuItems, out _,
                startY: 20f, rowStep: 44f, panelBackground: Color.clear);

            _resultConfirmPanel = MasuUiFactory.CreatePanel(_resultContentPanel.transform, "ConfirmPanel", Color.clear);
            MasuUiFactory.Stretch(_resultConfirmPanel.GetComponent<RectTransform>());

            _resultConfirmOp = MasuUiFactory.CreateText(_resultConfirmPanel.transform, "Op", "", 34,
                MasuTheme.CursorGold, TextAnchor.UpperCenter);
            MasuUiFactory.PlaceTopCenter(_resultConfirmOp.rectTransform, -48f, 400f, 40f);

            _resultConfirmGrid = MasuUiFactory.CreateText(_resultConfirmPanel.transform, "Grid", "4×4", 28,
                MasuTheme.Text, TextAnchor.UpperCenter);
            MasuUiFactory.PlaceTopCenter(_resultConfirmGrid.rectTransform, -88f, 200f, 36f);

            _resultPreviewRows = new Text[10];
            for (var i = 0; i < 10; i++)
            {
                _resultPreviewRows[i] = MasuUiFactory.CreateText(_resultConfirmPanel.transform, $"Prev{i}", "-",
                    MasuRankingLayout.RowFontSize, MasuTheme.Text, TextAnchor.MiddleCenter);
                MasuUiFactory.PlaceTopCenter(_resultPreviewRows[i].rectTransform,
                    MasuRankingLayout.RowStartY - i * MasuRankingLayout.RowLineHeight,
                    MasuRankingLayout.RowWidth, 36f);
            }

            _resultSessionSummary = MasuUiFactory.CreateText(_resultConfirmPanel.transform, "SessionSummary", "",
                MasuRankingLayout.RowFontSize, MasuTheme.Text, TextAnchor.LowerCenter);
            var sumRt = _resultSessionSummary.rectTransform;
            sumRt.anchorMin = sumRt.anchorMax = new Vector2(0.5f, 0f);
            sumRt.pivot = new Vector2(0.5f, 0f);
            sumRt.anchoredPosition = new Vector2(0, 48);
            sumRt.sizeDelta = new Vector2(MasuRankingLayout.RowWidth, 40);

            _resultConfirmHint = MasuUiFactory.CreateText(_resultConfirmPanel.transform, "ConfirmHint",
                "Shift … 3択メニューに戻る", MasuRankingLayout.HintFontSize, MasuTheme.TextDim,
                TextAnchor.LowerCenter);
            var hintRt = _resultConfirmHint.rectTransform;
            hintRt.anchorMin = hintRt.anchorMax = new Vector2(0.5f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.anchoredPosition = new Vector2(0, 16);
            hintRt.sizeDelta = new Vector2(900, 28);

            _resultEntryPanel = MasuUiFactory.CreatePanel(_resultContentPanel.transform, "EntryPanel", Color.clear);
            MasuUiFactory.Stretch(_resultEntryPanel.GetComponent<RectTransform>());

            _resultEntryLine = MasuUiFactory.CreateText(_resultEntryPanel.transform, "EntryLine", "",
                MasuRankingLayout.RowFontSize, MasuTheme.Text, TextAnchor.UpperCenter);
            MasuUiFactory.PlaceTopCenter(_resultEntryLine.rectTransform, -40f, MasuRankingLayout.RowWidth, 40f);
            MasuFonts.ApplyKeyboard(_resultEntryLine, MasuRankingLayout.RowFontSize);
            MasuUiFactory.ApplyBlackOutline(_resultEntryLine, 1.4f);

            // キーボードだけを下からスライドさせるため、専用のルートにぶら下げる
            var keyboardRootGo = MasuUiFactory.CreatePanel(_resultEntryPanel.transform, "KeyboardRoot", Color.clear);
            _keyboardRoot = keyboardRootGo.GetComponent<RectTransform>();
            BuildNameKeyboardUi(_keyboardRoot);
        }

        private void BuildNameKeyboardUi(Transform parent)
        {
            var rows = MasuNameKeyboard.Rows;
            _keyboardKeyLabels = new Text[rows.Length][];
            var colStep = MasuNameKeyboardLayout.ColumnStep;
            var halfW = MasuNameKeyboardLayout.KeyboardWidth * 0.5f;
            var pad = 1f;
            var rowCount = rows.Length;
            for (var r = 0; r < rowCount; r++)
            {
                var line = rows[r];
                _keyboardKeyLabels[r] = new Text[line.Length];
                var lineHalf = line.Length * colStep * 0.5f;
                for (var c = 0; c < line.Length; c++)
                {
                    var label = MasuUiFactory.CreateText(parent, $"Key_{r}_{c}", line[c],
                        (int)MasuNameKeyboardLayout.KeyFontSize, MasuTheme.Text);
                    MasuFonts.ApplyKeyboard(label, (int)MasuNameKeyboardLayout.KeyFontSize);
                    MasuUiFactory.ApplyBlackOutline(label);
                    label.alignment = TextAnchor.MiddleCenter;
                    label.resizeTextForBestFit = true;
                    label.resizeTextMinSize = 14;
                    label.resizeTextMaxSize = (int)MasuNameKeyboardLayout.KeyFontSize;
                    var rt = label.rectTransform;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    var x = -lineHalf + (c + 0.5f) * colStep;
                    var y = MasuNameKeyboardLayout.BottomMargin + (rowCount - 1 - r) * MasuNameKeyboardLayout.RowStep;
                    rt.anchoredPosition = new Vector2(x, y);
                    rt.sizeDelta = new Vector2(colStep - pad, MasuNameKeyboardLayout.RowStep - pad);
                    _keyboardKeyLabels[r][c] = label;
                }
            }
        }

        private void BuildEndConfirm()
        {
            var panel = MasuUiFactory.CreatePanel(_endConfirmRoot, "Box", Color.clear);
            var rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(520, 240);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

            var msg = MasuUiFactory.CreateText(panel.transform, "Msg", "終了して採点しますか？", 26, MasuTheme.Text);
            msg.rectTransform.anchorMin = msg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            msg.rectTransform.anchoredPosition = new Vector2(0, 56);
            msg.rectTransform.sizeDelta = new Vector2(480, 40);

            _endConfirmYes = MasuUiFactory.CreateText(panel.transform, "Yes", "はい", 24, MasuTheme.Text);
            _endConfirmYes.rectTransform.anchorMin = _endConfirmYes.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _endConfirmYes.rectTransform.anchoredPosition = new Vector2(0, -8);
            _endConfirmYes.rectTransform.sizeDelta = new Vector2(320, 36);

            _endConfirmNo = MasuUiFactory.CreateText(panel.transform, "No", "いいえ", 24, MasuTheme.Text);
            _endConfirmNo.rectTransform.anchorMin = _endConfirmNo.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _endConfirmNo.rectTransform.anchoredPosition = new Vector2(0, -52);
            _endConfirmNo.rectTransform.sizeDelta = new Vector2(320, 36);
            _endConfirmLabels = new[] { _endConfirmYes, _endConfirmNo };
        }

        private void ShowScreen(AppScreen screen)
        {
            if (screen != AppScreen.Game)
                DismissGameOverlays();

            _screen = screen;
            _menuRoot.gameObject.SetActive(screen != AppScreen.Game);
            _gameRoot.gameObject.SetActive(screen == AppScreen.Game);
            foreach (var kv in _menuPanels)
                kv.Value.SetActive(kv.Key == screen);
            // 画面に入った直後は、カーソルが変わっていなくても選択項目をスライドインさせる
            ForgetListCursor(LabelsForScreen(screen));
            RefreshMenuHighlights();
        }

        private Text[] LabelsForScreen(AppScreen screen) => screen switch
        {
            AppScreen.Title => _titleLabels,
            AppScreen.Mode => _modeLabels,
            AppScreen.Grid => _gridLabels,
            AppScreen.PlayStyle => _playStyleLabels,
            _ => null
        };

        private void DismissGameOverlays()
        {
            if (_resultRoot != null)
                _resultRoot.gameObject.SetActive(false);
            SetEndConfirmVisible(false);
            ClosePauseMenu();
        }

        private void SetEndConfirmVisible(bool visible)
        {
            _endConfirmVisible = visible;
            if (_endConfirmRoot != null)
                _endConfirmRoot.gameObject.SetActive(visible);
            if (visible)
            {
                ForgetListCursor(_endConfirmLabels);
                RefreshEndConfirmHighlight();
            }
        }

        private void RefreshEndConfirmHighlight() =>
            HighlightList(_endConfirmLabels, _endConfirmChoice, EndConfirmItems);

        private void CheckScreenSizeChanged()
        {
            if (Screen.width == _trackedScreenWidth && Screen.height == _trackedScreenHeight)
                return;
            _trackedScreenWidth = Screen.width;
            _trackedScreenHeight = Screen.height;
            RequestUILayoutRefresh();
        }

        public void NotifyDisplayLayoutChanged() => RequestUILayoutRefresh();

        private void RequestUILayoutRefresh()
        {
            _trackedScreenWidth = Screen.width;
            _trackedScreenHeight = Screen.height;
            Canvas.ForceUpdateCanvases();
            if (_screen == AppScreen.Game && _gridView != null)
            {
                _layoutGen++;
                StartCoroutine(RebuildGridAfterLayout(_layoutGen));
            }
            RefreshMenuHighlights();
        }

        private void RefreshMenuHighlights()
        {
            HighlightList(_titleLabels, _titleMenu, TitleMenuItems,
                TitleMenuPrefixIdle, TitleMenuPrefixSelected);
            HighlightList(_modeLabels, _modeCursor, OperationMenuLabelsJa());
            if (_modeStepHint != null)
                _modeStepHint.text = $"【 {_pendingPlayMode.LabelJa()} 】 演算を選ぶ";
            HighlightList(_gridLabels, _gridCursor,
                Array.ConvertAll(GameConstants.GridSizes, GridConfig.GridLabel));
            if (_gridOpHint != null)
                _gridOpHint.text =
                    $"【 {_pendingOperation.LabelJa()} ・ {_pendingPlayMode.LabelJa()} 】";

            if (_playStyleLabels != null)
            {
                HighlightList(_playStyleLabels, _playStyleCursor,
                    new[] { GamePlayMode.Classic.LabelJa(), GamePlayMode.Quick.LabelJa() });
                if (_playStyleHint != null)
                    _playStyleHint.text = "";
            }

            if (_screen == AppScreen.Options)
                RefreshOptionsMenuHighlights();

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

        /// <summary>
        /// リストのハイライト更新。カーソルが動いた（または ForgetListCursor 済み）ときだけ
        /// 選択項目を横からスライドイン＋点滅させる。
        /// </summary>
        private void HighlightList(
            Text[] labels,
            int cursor,
            string[] items,
            string unselectedPrefix = "  ",
            string selectedPrefix = "▶ ")
        {
            if (labels == null || labels.Length == 0) return;
            var changed = !_listCursorMemo.TryGetValue(labels, out var prev) || prev != cursor;
            _listCursorMemo[labels] = cursor;

            for (var i = 0; i < labels.Length; i++)
            {
                var sel = i == cursor;
                labels[i].text = (sel ? selectedPrefix : unselectedPrefix) + items[i];
                if (!sel)
                {
                    RestoreLabelPose(labels[i]);
                    labels[i].color = MasuTheme.Text;
                }
            }

            if (cursor < 0 || cursor >= labels.Length) return;
            var selected = labels[cursor];
            if (changed && selected.gameObject.activeInHierarchy)
                PlaySelectSlideIn(selected);
            else if (!_labelAnims.ContainsKey(selected))
                selected.color = MasuTheme.CursorGold;
        }

        private void ForgetListCursor(Text[] labels)
        {
            if (labels != null) _listCursorMemo.Remove(labels);
        }

        private Vector2 BasePosOf(Text label)
        {
            if (!_labelBasePos.TryGetValue(label, out var pos))
            {
                pos = label.rectTransform.anchoredPosition;
                _labelBasePos[label] = pos;
            }
            return pos;
        }

        /// <summary>進行中の演出を止め、位置を定位置へ戻す。</summary>
        private void RestoreLabelPose(Text label)
        {
            if (label == null) return;
            if (_labelAnims.TryGetValue(label, out var co))
            {
                if (co != null) StopCoroutine(co);
                _labelAnims.Remove(label);
            }
            if (_labelBasePos.TryGetValue(label, out var pos))
                label.rectTransform.anchoredPosition = pos;
        }

        private void PlaySelectSlideIn(Text label)
        {
            RestoreLabelPose(label);
            var basePos = BasePosOf(label);
            _labelAnims[label] = StartCoroutine(RunLabelAnim(label,
                MasuUiAnimations.SlideInFlash(label, basePos, SelectSlideOffsetX, SelectSlideSeconds,
                    MasuTheme.CursorGold, Color.white, SelectFlashCount)));
        }

        private IEnumerator RunLabelAnim(Text label, IEnumerator inner)
        {
            yield return inner;
            _labelAnims.Remove(label);
        }

        /// <summary>決定: 選択項目を点滅させてから then を実行。演出中は入力を止める。</summary>
        private void Decide(Text label, Action then)
        {
            if (_menuBusy) return;
            _menuBusy = true;
            StartCoroutine(DecideRoutine(label, then));
        }

        private IEnumerator DecideRoutine(Text label, Action then)
        {
            if (label != null)
            {
                RestoreLabelPose(label);
                yield return MasuUiAnimations.Flash(label, MasuTheme.CursorGold, Color.white,
                    DecideFlashCount, DecideFlashInterval);
            }
            _menuBusy = false;
            then?.Invoke();
        }

        /// <summary>プレイスタイル決定 → 暗転 → 盤面準備 → 明転 → イントロ開始。</summary>
        private IEnumerator GameStartTransition(Operation op, PlayBoard board)
        {
            _menuBusy = true;
            try
            {
                _fadeImage.transform.SetAsLastSibling();
                _fadeImage.gameObject.SetActive(true);
                MasuUiAnimations.SetAlpha(_fadeImage, 0f);
                MasuUiAnimations.SetAlpha(_fadeCaption, 0f);
                _fadeCaption.text = $"{op.LabelJa()}　{GridConfig.BoardLabel(board.PlaySize, board.Mode)}";

                yield return MasuUiAnimations.FadeAlpha(_fadeImage, 0f, 1f, 0.35f, MasuUiAnimations.EaseInCubic);
                yield return MasuUiAnimations.FadeAlpha(_fadeCaption, 0f, 1f, 0.25f);
                yield return new WaitForSecondsRealtime(0.55f);
                yield return MasuUiAnimations.FadeAlpha(_fadeCaption, 1f, 0f, 0.2f);

                StartGame(op, board);
                yield return null; // レイアウト確定を 1 フレーム待つ
                yield return MasuUiAnimations.FadeAlpha(_fadeImage, 1f, 0f, 0.5f, MasuUiAnimations.EaseOutCubic);
            }
            finally
            {
                _fadeImage.gameObject.SetActive(false);
                _menuBusy = false;
            }
            if (_screen == AppScreen.Game)
                BeginIntro();
        }

        /// <summary>盤面の準備まで（イントロは BeginIntro で別途開始）。</summary>
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
            PrepareGameBoard();
        }

        private void PrepareGameBoard()
        {
            _introGen++;
            _scoreGen++;
            _layoutGen++;
            _clockPausedAt = null;
            StopGameCoroutines();

            _introAnimating = true;
            _scoreAnimating = false;
            _revealedHeaders.Clear();
            _scoreMarks.Clear();
            _quickMarks.Clear();
            _displayScore = 0;
            _lastAccuracy = 0;
            _gridView.Rebuild(_state.Grid);
            var layoutGen = _layoutGen;
            StartCoroutine(RebuildGridAfterLayout(layoutGen));
            _state.Reset(startClock: false);
            _gridView.ClearFx();
            RefreshGrid();
            UpdateSidebar();
            UpdateEndGuide();
        }

        private void BeginIntro()
        {
            if (_introRoutine != null) StopCoroutine(_introRoutine);
            _introRoutine = StartCoroutine(RunIntroAnimation());
        }

        private IEnumerator RebuildGridAfterLayout(int layoutGen)
        {
            yield return null;
            if (layoutGen != _layoutGen || _screen != AppScreen.Game) yield break;
            Canvas.ForceUpdateCanvases();
            _gridView.Rebuild(_state.Grid);
            RefreshGrid();
        }

        /// <summary>
        /// イントロ: 左列の数字が左から、上段の数字が上から順に滑り込んで着地（金色フラッシュ）。
        /// 全部そろったら演算子マスがパンチし、盤面を斜めの光の波がひと撫でしてプレイ開始。
        /// </summary>
        private IEnumerator RunIntroAnimation()
        {
            var gen = _introGen;
            try
            {
                _state.StartRealtime = Time.realtimeSinceStartupAsDouble;
                _timerRoutine = StartCoroutine(TickTimer());
                var cells = _state.Grid.HeaderRevealCells();
                var stagger = Mathf.Clamp(IntroHeaderBudgetSec / Mathf.Max(1, cells.Count), 0.03f, 0.12f);

                foreach (var (r, c) in cells)
                {
                    if (gen != _introGen) yield break;
                    _revealedHeaders.Add((r, c));
                    _gridView.PlayHeaderEnter(r, c, IntroHeaderEnterSec);
                    yield return new WaitForSecondsRealtime(stagger);
                }

                yield return new WaitForSecondsRealtime(0.18f);
                if (gen != _introGen) yield break;
                _gridView.PlayOperatorPunch();
                yield return PlayBoardWave(MasuTheme.CursorGold, 0.35f, 0.28f, 0.022f, punchMarks: false);
                if (gen != _introGen) yield break;
            }
            finally
            {
                if (gen == _introGen)
                    CompleteIntroAnimation(gen);
            }
        }

        private void CompleteIntroAnimation(int gen)
        {
            if (gen != _introGen) return;
            _introAnimating = false;
            RefreshGrid();
            UpdateEndGuide();
        }

        /// <summary>左上から右下へ、斜め 1 列ずつ順にフラッシュする光の波。</summary>
        private IEnumerator PlayBoardWave(Color color, float strength, float cellDuration, float diagonalStagger, bool punchMarks)
        {
            var g = _state.Grid;
            var minD = g.PlayRowMin + g.PlayColMin;
            var maxD = g.PlayRowMax + g.PlayColMax;
            for (var d = minD; d <= maxD; d++)
            {
                for (var r = g.PlayRowMin; r <= g.PlayRowMax; r++)
                {
                    var c = d - r;
                    if (!g.IsPlayCell(r, c)) continue;
                    _gridView.PlayFlash(r, c, color, cellDuration, strength);
                    if (punchMarks) _gridView.PlayMarkPunch(r, c, 1.3f, cellDuration);
                }
                yield return new WaitForSecondsRealtime(diagonalStagger);
            }
            yield return new WaitForSecondsRealtime(cellDuration);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (_screen != AppScreen.Game || _state.Finished) return;
            if (_state.ScoringPlayElapsed.HasValue || !_state.StartRealtime.HasValue) return;

            if (!hasFocus)
            {
                _clockPausedAt = Time.realtimeSinceStartupAsDouble;
                return;
            }

            if (!_clockPausedAt.HasValue) return;
            var pausedFor = Time.realtimeSinceStartupAsDouble - _clockPausedAt.Value;
            _state.StartRealtime = _state.StartRealtime.Value + pausedFor;
            _clockPausedAt = null;
            UpdateSidebarTime();
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

            _sidebarMode.text = _operation.LabelJa();
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
                _guideText.text =
                    "移動で採点・最終マスは Enter 確定（空欄は無視）・全マス○でクリア\nShift / Esc … 一時停止";
            else
                _guideText.text = "Enter キーで終了・採点の確認\nShift / Esc … 一時停止";
        }

        private void PromptEndGame()
        {
            if (_state.Finished || _introAnimating || _scoreAnimating) return;
            _endConfirmChoice = 0;
            SetEndConfirmVisible(true);
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
            _scoreAnimating = true;
            if (_playMode == GamePlayMode.Quick)
            {
                // クイックは離脱時に採点済み。全マス○なので祝福の金色ウェーブだけ流す
                foreach (var (r, c) in _state.Grid.PlayCellsScoreOrder())
                    _scoreMarks[(r, c)] = _state.Cells[(r, c)].State == CellState.Correct;
                RefreshGrid();
                _scoreRoutine = StartCoroutine(RunQuickClearCelebration());
                return;
            }
            _scoreRoutine = StartCoroutine(RunScoreAnimation());
        }

        /// <summary>
        /// 採点: 演算子マスの合図 → 列ごとに ○× をスタンプ（正解=緑、不正解=赤+揺れ）→
        /// 全問正解なら金色の波、そうでなければ一呼吸おいてリザルトへ。
        /// </summary>
        private IEnumerator RunScoreAnimation()
        {
            var gen = _scoreGen;
            var cells = _state.Grid.PlayCellsScoreOrder();
            var (scaleMs, staggerMs) = AnimTimings.ScoreAnimTimings(cells.Count);
            var perCell = (AnimTimings.ScorePopScales().Length * scaleMs + staggerMs) / 1000f;
            var stampSec = Mathf.Clamp(perCell * 2.5f, 0.18f, 0.38f);

            _gridView.PlayOperatorPunch();
            yield return new WaitForSecondsRealtime(ScoreStartDelaySec);

            foreach (var (r, c) in cells)
            {
                if (gen != _scoreGen) yield break;
                var ok = _state.GradeCellAtScore(r, c);
                _scoreMarks[(r, c)] = ok;
                _gridView.PlayGradeStamp(r, c, ok, stampSec);
                UpdateSidebar();
                yield return new WaitForSecondsRealtime(perCell);
            }

            if (gen != _scoreGen) yield break;
            var allCorrect = _state.WrongCount() == 0 && _state.EmptyCount() == 0;
            if (allCorrect)
            {
                _gridView.PlayOperatorPunch();
                yield return PlayBoardWave(MasuTheme.CursorGold, 0.7f, 0.45f, 0.035f, punchMarks: true);
            }
            else
                yield return new WaitForSecondsRealtime(0.4f);

            if (gen != _scoreGen) yield break;
            _scoreAnimating = false;
            FinalizeScore();
            ShowScoreResults();
        }

        private IEnumerator RunQuickClearCelebration()
        {
            var gen = _scoreGen;
            _gridView.PlayOperatorPunch();
            yield return new WaitForSecondsRealtime(0.15f);
            if (gen != _scoreGen) yield break;
            yield return PlayBoardWave(MasuTheme.CursorGold, 0.7f, 0.45f, 0.035f, punchMarks: true);
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
            _resultRoot.SetAsLastSibling();
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
            ForgetListCursor(_resultMenuLabels);
        }

        /// <summary>3択メニューへ戻る（選択項目をスライドインさせる）。</summary>
        private void ShowResultMenuView()
        {
            _resultView = ResultView.Menu;
            ForgetListCursor(_resultMenuLabels);
            ShowResultPanel();
        }

        /// <summary>名前入力へ。キーボードは画面下からスライドアップ。</summary>
        private void EnterNameEntryView()
        {
            _recordConfirmEnterOpensKeyboard = false;
            _nameKeyboard = new MasuNameKeyboard();
            _resultView = ResultView.Entry;
            ShowResultPanel();
            PlayKeyboardSlide(show: true, then: null);
        }

        private void PlayKeyboardSlide(bool show, Action then)
        {
            if (_keyboardRoot == null)
            {
                then?.Invoke();
                return;
            }
            if (_keyboardSlideRoutine != null)
                StopCoroutine(_keyboardSlideRoutine);
            _keyboardSlideRoutine = StartCoroutine(KeyboardSlideRoutine(show, then));
        }

        private IEnumerator KeyboardSlideRoutine(bool show, Action then)
        {
            var hiddenY = -MasuNameKeyboardLayout.SlideHiddenOffset;
            var fromY = show ? hiddenY : _keyboardRoot.anchoredPosition.y;
            var toY = show ? 0f : hiddenY;
            var seconds = show ? MasuNameKeyboardLayout.SlideInSeconds : MasuNameKeyboardLayout.SlideOutSeconds;
            Func<float, float> ease = show ? MasuUiAnimations.EaseOutCubic : MasuUiAnimations.EaseInCubic;
            yield return MasuUiAnimations.SlideY(_keyboardRoot, fromY, toY, seconds, ease);
            _keyboardSlideRoutine = null;
            then?.Invoke();
        }

        private void ShowResultPanel()
        {
            var isMenu = _resultView == ResultView.Menu;
            _resultMenuBox.SetActive(isMenu);
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

        private void HighlightResultMenu() =>
            HighlightList(_resultMenuLabels, _resultMenu, ResultMenuItems);

        private void RefreshRecordConfirmPanel()
        {
            _resultConfirmOp.text = _operation.LabelJa();
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
                _resultPreviewRows[i].color = i + 1 == rank ? MasuTheme.CursorGold : MasuTheme.Text;
            }

            var played = string.IsNullOrEmpty(_lastPlayedAt) ? "----/--/-- --:--" : _lastPlayedAt;
            _resultSessionSummary.text =
                $"{GameLogic.FormatTime(_lastElapsedSeconds)}  {played}  {_lastAccuracy:F1}%";
        }

        private void RefreshNameEntryPanel()
        {
            var played = string.IsNullOrEmpty(_lastPlayedAt) ? "----/--/-- --:--" : _lastPlayedAt;
            _resultEntryLine.text =
                $"{Leaderboard.FormatRankPrefix(_entryPreviewRank)}{MasuNameKeyboard.FormatNameSlots(_entryName, Leaderboard.NameDisplayMax)}  " +
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
                _keyboardKeyLabels[r][c].color = sel ? MasuTheme.CursorGold : MasuTheme.Text;
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
            UpdateSidebar();

            if (_resultView != ResultView.Entry)
            {
                ShowResultMenuView();
                return;
            }

            // キーボードを下へ退場させてから 3択メニューへ
            _menuBusy = true;
            PlayKeyboardSlide(show: false, then: () =>
            {
                _menuBusy = false;
                ShowResultMenuView();
            });
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
            if (_keyboardSlideRoutine != null)
            {
                StopCoroutine(_keyboardSlideRoutine);
                _keyboardSlideRoutine = null;
            }
            _menuBusy = false;
            ResetResultFlow();
            DismissGameOverlays();
            _titleMenu = 0;
            _layoutGen++;
            _clockPausedAt = null;
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
                MasuDisplaySettings.SelectDisplayOptionIndex(_launcher.SelectedIndex);
                _launcher.Refresh();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _launcher.SelectedIndex = NavVertical(_launcher.SelectedIndex, 1, n);
                MasuDisplaySettings.SelectDisplayOptionIndex(_launcher.SelectedIndex);
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

        private void OpenPauseMenu()
        {
            if (_screen != AppScreen.Game || _pauseVisible) return;
            if (_scoreAnimating || _state.Finished) return; // 採点演出中は割り込ませない
            _pauseMenu = 0;
            _pauseVisible = true;
            _pauseRoot.gameObject.SetActive(true);
            _pauseRoot.SetAsLastSibling();
            ForgetListCursor(_pauseLabels);
            RefreshPauseMenu();
            if (!_state.Finished && !_state.ScoringPlayElapsed.HasValue && _state.StartRealtime.HasValue)
                _clockPausedAt = Time.realtimeSinceStartupAsDouble;
        }

        private void ClosePauseMenu()
        {
            if (!_pauseVisible) return;
            _pauseVisible = false;
            if (_pauseRoot != null)
                _pauseRoot.gameObject.SetActive(false);
            if (_clockPausedAt.HasValue && _state.StartRealtime.HasValue)
            {
                var pausedFor = Time.realtimeSinceStartupAsDouble - _clockPausedAt.Value;
                _state.StartRealtime = _state.StartRealtime.Value + pausedFor;
                _clockPausedAt = null;
                UpdateSidebarTime();
            }
        }

        private void RefreshPauseMenu() =>
            HighlightList(_pauseLabels, _pauseMenu, PauseMenuItems);

        private void HandlePauseInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _pauseMenu = NavVertical(_pauseMenu, -1, 2);
                RefreshPauseMenu();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _pauseMenu = NavVertical(_pauseMenu, 1, 2);
                RefreshPauseMenu();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
                ClosePauseMenu();
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                var choice = _pauseMenu;
                Decide(_pauseLabels[choice], () =>
                {
                    ClosePauseMenu();
                    if (choice != 0) ReturnToTitle();
                });
            }
        }

        private void ConfirmLauncherAndStartGame()
        {
            MasuDisplaySettings.CommitSession(_launcher.SelectedIndex, _launcher.AskEveryTime);
            _displayGuard.Recapture();
            NotifyDisplayLayoutChanged();
            _launcher.Close();
            _menuRoot.gameObject.SetActive(true);
            _titleMenu = 0;
            ShowScreen(AppScreen.Title);
        }

        private void OpenOptionsScreen()
        {
            _optionsCursor = 0;
            ForgetListCursor(_optionsLabels);
            ShowScreen(AppScreen.Options);
        }

        private void RefreshOptionsMenuHighlights() =>
            HighlightList(_optionsLabels, _optionsCursor, OptionsMenuItems);

        private void HandleOptionsInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                ShowScreen(AppScreen.Title);
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                Decide(_optionsLabels[0], () => ShowScreen(AppScreen.Title));
        }

        private void HandleTitleInput()
        {
            const int titleCount = 4;
            if (Input.GetKeyDown(KeyCode.UpArrow)) { _titleMenu = NavVertical(_titleMenu, -1, titleCount); RefreshMenuHighlights(); }
            else if (Input.GetKeyDown(KeyCode.DownArrow)) { _titleMenu = NavVertical(_titleMenu, 1, titleCount); RefreshMenuHighlights(); }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                var choice = _titleMenu;
                Decide(_titleLabels[choice], () =>
                {
                    if (choice == 0) { _playStyleCursor = 0; ShowScreen(AppScreen.PlayStyle); }
                    else if (choice == 1) ShowScreen(AppScreen.Help);
                    else if (choice == 2) { _rankingOpCursor = 0; _rankingBoardCursor = 0; ShowScreen(AppScreen.ScoreRanking); }
                    else OpenOptionsScreen();
                });
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
                var choice = _modeCursor;
                Decide(_modeLabels[choice], () =>
                {
                    _pendingOperation = Operations[choice];
                    _gridCursor = 0;
                    ShowScreen(AppScreen.Grid);
                });
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
                ShowScreen(AppScreen.PlayStyle);
        }

        private void HandleGridInput()
        {
            var n = GameConstants.GridSizes.Length;
            if (Input.GetKeyDown(KeyCode.UpArrow)) { _gridCursor = NavVertical(_gridCursor, -1, n); RefreshMenuHighlights(); }
            else if (Input.GetKeyDown(KeyCode.DownArrow)) { _gridCursor = NavVertical(_gridCursor, 1, n); RefreshMenuHighlights(); }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                var choice = _gridCursor;
                var op = _pendingOperation;
                var mode = _pendingPlayMode;
                Decide(_gridLabels[choice], () =>
                {
                    _pendingGridSize = GameConstants.GridSizes[choice];
                    StartCoroutine(GameStartTransition(op, new PlayBoard(_pendingGridSize, mode)));
                });
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
                var choice = _playStyleCursor;
                Decide(_playStyleLabels[choice], () =>
                {
                    _pendingPlayMode = choice == 0 ? GamePlayMode.Classic : GamePlayMode.Quick;
                    _modeCursor = 0;
                    ShowScreen(AppScreen.Mode);
                });
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                ShowScreen(AppScreen.Title);
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
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OpenPauseMenu();
                return;
            }

            if (_state.Finished || _introAnimating || _scoreAnimating) return;

            if (TryMoveFocus()) return;
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                var hadText = !string.IsNullOrEmpty(_state.Buffer);
                ClearQuickMarkOnEdit();
                _state.HandleBackspace();
                if (hadText) _gridView.PlayEraseFeedback(_state.FocusRow, _state.FocusCol);
                else _gridView.PlayRejectShake(_state.FocusRow, _state.FocusCol);
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
                TypeIntoFocusedCell('-');
                return;
            }
            if (_state.Operation == Operation.Div && Input.GetKeyDown(KeyCode.Period))
            {
                TypeIntoFocusedCell('.');
                return;
            }
            for (var d = 0; d <= 9; d++)
            {
                var kc = KeyCode.Alpha0 + d;
                if (Input.GetKeyDown(kc) || Input.GetKeyDown(KeyCode.Keypad0 + d))
                {
                    TypeIntoFocusedCell((char)('0' + d));
                    return;
                }
            }
        }

        /// <summary>1 文字入力。受理されたら文字がポンと弾み、弾かれたら赤く小さく震える。</summary>
        private void TypeIntoFocusedCell(char ch)
        {
            var before = _state.Buffer;
            ClearQuickMarkOnEdit();
            _state.HandleChar(ch);
            if (_state.Buffer != before)
                _gridView.PlayInputPunch(_state.FocusRow, _state.FocusCol);
            else
                _gridView.PlayRejectShake(_state.FocusRow, _state.FocusCol);
            RefreshGrid();
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

            var prevRow = _state.FocusRow;
            var prevCol = _state.FocusCol;
            _state.MoveFocus(dr, dc);
            if (_state.FocusRow != prevRow || _state.FocusCol != prevCol)
                _gridView.PlayFocusMoved(_state.FocusRow, _state.FocusCol);
            ApplyQuickGradeFeedback();
            RefreshGrid();
            UpdateSidebar();
            TryCompleteQuickMode();
            return true;
        }

        /// <summary>クイック採点のフィードバック: 離れたマスに ○× をスタンプ。</summary>
        private void ApplyQuickGradeFeedback()
        {
            if (_playMode != GamePlayMode.Quick) return;
            var ev = _state.TakeQuickGradeFeedback();
            if (!ev.HasValue) return;
            _quickMarks[(ev.Row, ev.Col)] = ev.Correct;
            _gridView.PlayGradeStamp(ev.Row, ev.Col, ev.Correct, 0.3f);
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
            {
                _endConfirmChoice = 1 - _endConfirmChoice;
                RefreshEndConfirmHighlight();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                var choice = _endConfirmChoice;
                Decide(_endConfirmLabels[choice], () =>
                {
                    SetEndConfirmVisible(false);
                    if (choice == 0) FinishWithScore();
                    else UpdateEndGuide();
                });
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetEndConfirmVisible(false);
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
                    var (newName, submitAndExit) = _nameKeyboard.ApplyKey(_entryName, Leaderboard.NameDisplayMax);
                    _entryName = newName;
                    if (submitAndExit)
                    {
                        SubmitScoreEntry();
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
                    ShowResultMenuView();
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    if (!_recordConfirmEnterOpensKeyboard && !string.IsNullOrWhiteSpace(_entryName))
                        SubmitScoreEntry();
                    else
                        EnterNameEntryView();
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
                var choice = _resultMenu;
                Decide(_resultMenuLabels[choice], () =>
                {
                    if (choice == 0) ReturnToTitle();
                    else if (choice == 1) BeginScoreRecord();
                    else QuitApplication();
                });
            }
        }
    }
}
