using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BOMBOMLemon.Editor
{
    // Auto-builds TitleScene on first editor load if the scene is empty
    [InitializeOnLoad]
    public static class TitleSceneAutoBuilder
    {
        static TitleSceneAutoBuilder()
        {
            // Fix invalid activeInputHandler before Input System reads it
            TitleSceneBuilder.FixActiveInputHandler();
            EditorApplication.update += OnFirstUpdate;
        }

        static void OnFirstUpdate()
        {
            // Keep waiting until we are in a safe editor state (not play mode)
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EditorApplication.update -= OnFirstUpdate;

            const string scenePath = "Assets/Scenes/TitleScene.unity";
            if (!System.IO.File.Exists(scenePath)) return;

            // Open additively just to count root objects
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            int rootCount = scene.rootCount;
            EditorSceneManager.CloseScene(scene, false);

            if (rootCount <= 2)
            {
                Debug.Log("[BOMBOMLemon] Scene is empty — auto-building Title Scene...");
                TitleSceneBuilder.Build();
            }
        }
    }

    public static class TitleSceneBuilder
    {
        // ── Reference resolution = iPhone logical points ───────────────────
        const float W = 390f, H = 844f;

        // ── Colour palette ─────────────────────────────────────────────────
        static readonly Color BgYellow  = new Color(1.00f, 0.96f, 0.60f);
        static readonly Color DarkBrown = new Color(0.15f, 0.10f, 0.00f);
        static readonly Color SubBrown  = new Color(0.50f, 0.36f, 0.00f);
        static readonly Color BtnFg     = new Color(0.45f, 0.30f, 0.00f);
        static readonly Color BtnCap    = new Color(1f, 1f, 1f, 0.65f);
        static readonly Color LimeGreen = new Color(0.25f, 0.60f, 0.08f);

        [MenuItem("BOMBOMLemon/Build Title Scene")]
        public static void Build()
        {
            FixActiveInputHandler();

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity");

            // Clear everything except Camera & EventSystem
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<Camera>()      != null) continue;
                if (root.GetComponent<EventSystem>() != null) continue;
                Object.DestroyImmediate(root);
            }
            EnsureEventSystem(scene);

            // ── Persistent managers ─────────────────────────────────────────
            new GameObject("GameManager").AddComponent<GameManager>();

            var smGo   = new GameObject("SoundManager");
            var bgmSrc = smGo.AddComponent<AudioSource>(); bgmSrc.playOnAwake = false;
            var seSrc  = smGo.AddComponent<AudioSource>(); seSrc.playOnAwake  = false;
            var sm     = smGo.AddComponent<SoundManager>();
            var smSO   = new SerializedObject(sm);
            smSO.FindProperty("bgmSource").objectReferenceValue   = bgmSrc;
            smSO.FindProperty("seSource").objectReferenceValue    = seSrc;
            smSO.FindProperty("titleMusic").objectReferenceValue  = Load<AudioClip>("Assets/Audio/title_music.mp3");
            smSO.FindProperty("fireMusic").objectReferenceValue   = Load<AudioClip>("Assets/Audio/fire_music.mp3");
            smSO.FindProperty("clickSE").objectReferenceValue     = Load<AudioClip>("Assets/Audio/click.mp3");
            smSO.FindProperty("piyoSE").objectReferenceValue      = Load<AudioClip>("Assets/Audio/piyo.mp3");
            smSO.FindProperty("goodSE").objectReferenceValue      = Load<AudioClip>("Assets/Audio/good.mp3");
            smSO.FindProperty("badSE").objectReferenceValue       = Load<AudioClip>("Assets/Audio/bad.mp3");
            smSO.FindProperty("perfectSE").objectReferenceValue   = Load<AudioClip>("Assets/Audio/perfect.mp3");
            smSO.FindProperty("showSE").objectReferenceValue      = Load<AudioClip>("Assets/Audio/show.mp3");
            smSO.FindProperty("lemonGetSE").objectReferenceValue  = Load<AudioClip>("Assets/Audio/lemonget.mp3");
            smSO.FindProperty("gameClearSE").objectReferenceValue = Load<AudioClip>("Assets/Audio/gameclear.mp3");
            smSO.FindProperty("gameOverSE").objectReferenceValue  = Load<AudioClip>("Assets/Audio/gameover.mp3");
            smSO.ApplyModifiedPropertiesWithoutUndo();

            // ── Canvas ──────────────────────────────────────────────────────
            var canvasGo = new GameObject("Canvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = canvasGo.AddComponent<CanvasScaler>();
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(W, H);
            cs.matchWidthOrHeight  = 0f;
            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGo.AddComponent<JPFontLoader>();

            // GameUIManager lives on the canvas
            var uiMgr = canvasGo.AddComponent<GameUIManager>();

            // ── Shared background (always visible, colour changes with LimeMode) ──
            var bgGo = Stretch(canvasGo, "Background");
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = BgYellow;
            uiMgr.sharedBackground = bgImg;

            // ══════════════════════════════════════════════════════════════════
            // TITLE PANEL (active only during GamePhase.Title)
            // ══════════════════════════════════════════════════════════════════
            var titlePanel = Stretch(canvasGo, "TitlePanel");
            uiMgr.titlePanel = titlePanel;

            // Lemon rain layer
            var rainParent = Stretch(titlePanel, "LemonRainParent");

            // Logo group
            const float zy = 1f - 0.42f;
            var bubbleGo = AnchorImage(titlePanel, "TitleBubble",
                "Assets/Sprites/Title_Bubble.png",
                new Vector2(0.5f, zy), new Vector2(386f, 225f));
            var lemonGo = AnchorImage(titlePanel, "TitleLemon",
                "Assets/Sprites/Title_Lemon.png",
                new Vector2(0.5f, zy + 70f / H), new Vector2(236f, 236f));
            var wordGo = AnchorImage(titlePanel, "TitleWord",
                "Assets/Sprites/Title_Word.png",
                new Vector2(0.5f, zy - 47f / H), new Vector2(332f, 78f));

            // Content group (start button + info texts)
            var contentGo = Stretch(titlePanel, "ContentGroup");
            var contentCG = contentGo.AddComponent<CanvasGroup>();
            var startBtnGo = AnchorSpriteButton(contentGo, "StartButton",
                "Assets/Sprites/start.png",
                new Vector2(0.5f, 0.193f), new Vector2(281f, 107f));
            var infoJaGo  = AnchorText(contentGo, "InfoTextJa",
                "2〜24人のパーティーゲーム", 14, FontStyle.Bold, DarkBrown,
                new Vector2(0.5f, 0.115f), new Vector2(360f, 18f));
            var infoEnGo  = AnchorText(contentGo, "InfoTextEn",
                "A party game for 2–24 players", 11, FontStyle.Normal, SubBrown,
                new Vector2(0.5f, 0.095f), new Vector2(360f, 14f));
            var hellJaGo  = AnchorText(contentGo, "HellModeInfoJa",
                "", 12, FontStyle.Bold, LimeGreen,
                new Vector2(0.5f, 0.074f), new Vector2(360f, 20f));
            var hellEnGo  = AnchorText(contentGo, "HellModeInfoEn",
                "", 10, FontStyle.Normal, LimeGreen,
                new Vector2(0.5f, 0.053f), new Vector2(360f, 14f));

            // Top overlay (nav buttons)
            var topOverlay = Stretch(titlePanel, "TopOverlay");
            var leftGroup = new GameObject("LeftButtons");
            leftGroup.transform.SetParent(topOverlay.transform, false);
            var lgRT = leftGroup.AddComponent<RectTransform>();
            lgRT.anchorMin = lgRT.anchorMax = new Vector2(0f, 1f);
            lgRT.pivot            = new Vector2(0f, 1f);
            lgRT.anchoredPosition = new Vector2(18f, -54f);
            lgRT.sizeDelta        = new Vector2(148f, 28f);
            var hLayout = leftGroup.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing              = 8f;
            hLayout.childForceExpandWidth  = false;
            hLayout.childForceExpandHeight = false;
            hLayout.childAlignment       = TextAnchor.MiddleLeft;

            var rulesBtnGo = CapsuleButton(leftGroup, "RulesButton",
                "？ ルール", 11, BtnFg, BtnCap, new Vector2(72f, 28f));
            var topicBtnGo = CapsuleButton(leftGroup, "TopicButton",
                "≡ お題", 11, BtnFg, BtnCap, new Vector2(60f, 28f));

            var hellBtnGo = new GameObject("HellModeButton");
            hellBtnGo.transform.SetParent(topOverlay.transform, false);
            var hellRT = hellBtnGo.AddComponent<RectTransform>();
            hellRT.anchorMin = hellRT.anchorMax = new Vector2(1f, 1f);
            hellRT.pivot     = new Vector2(1f, 1f);
            hellRT.anchoredPosition = new Vector2(-18f, -54f);
            hellRT.sizeDelta        = new Vector2(105f, 28f);
            var hellBtnImg = hellBtnGo.AddComponent<Image>();
            hellBtnImg.sprite = CapsuleSprite(); hellBtnImg.type = Image.Type.Sliced;
            hellBtnImg.color  = BtnCap;
            var hellBtn = hellBtnGo.AddComponent<Button>(); NoNav(hellBtn);

            var dotGo = new GameObject("HellModeIndicator");
            dotGo.transform.SetParent(hellBtnGo.transform, false);
            var dotRT = dotGo.AddComponent<RectTransform>();
            dotRT.anchorMin = dotRT.anchorMax = new Vector2(0.5f, 0.5f);
            dotRT.pivot = new Vector2(0.5f, 0.5f);
            dotRT.anchoredPosition = new Vector2(-37f, 0f);
            dotRT.sizeDelta = new Vector2(8f, 8f);
            var dotImg = dotGo.AddComponent<Image>();
            dotImg.sprite = KnobSprite();
            dotImg.color  = new Color(0.75f, 0.75f, 0.75f);

            var hellLblGo = new GameObject("HellModeLabel");
            hellLblGo.transform.SetParent(hellBtnGo.transform, false);
            var hellLblRT = hellLblGo.AddComponent<RectTransform>();
            hellLblRT.anchorMin = hellLblRT.anchorMax = new Vector2(0.5f, 0.5f);
            hellLblRT.pivot = new Vector2(0.5f, 0.5f);
            hellLblRT.anchoredPosition = new Vector2(7f, 0f);
            hellLblRT.sizeDelta = new Vector2(70f, 20f);
            var hellLbl = hellLblGo.AddComponent<Text>();
            hellLbl.text = "地獄モード"; hellLbl.fontSize = 11;
            hellLbl.fontStyle = FontStyle.Bold;
            hellLbl.color = new Color(0.40f, 0.30f, 0.05f);
            hellLbl.alignment = TextAnchor.MiddleLeft;

            // Sheet panels
            var rulesPanel = SheetPanel(titlePanel, "RulesPanel", "ルール", BuildRulesContent);
            var topicSheetPanel = SheetPanel(titlePanel, "TopicSheetPanel", "お題", null);
            rulesPanel.SetActive(false);
            topicSheetPanel.SetActive(false);

            // Lemon prefab
            var lemonPrefab = EnsureLemonPrefab();

            // TitleScreenUI wiring
            var uiHolder = new GameObject("TitleScreenUI");
            uiHolder.transform.SetParent(titlePanel.transform, false);
            uiHolder.AddComponent<RectTransform>();
            var ui = uiHolder.AddComponent<TitleScreenUI>();
            ui.titleBubble       = bubbleGo.GetComponent<RectTransform>();
            ui.titleLemon        = lemonGo.GetComponent<RectTransform>();
            ui.titleWord         = wordGo.GetComponent<RectTransform>();
            ui.contentGroup      = contentCG;
            ui.startButton       = startBtnGo.GetComponent<Button>();
            ui.startSprite       = LoadSprite("Assets/Sprites/start.png");
            ui.startLimeSprite   = LoadSprite("Assets/Sprites/startlime.png");
            ui.rulesButton       = rulesBtnGo.GetComponent<Button>();
            ui.topicButton       = topicBtnGo.GetComponent<Button>();
            ui.hellModeButton    = hellBtnGo.GetComponent<Button>();
            ui.hellModeIndicator = dotImg;
            ui.hellModeLabel     = hellLbl;
            ui.hellModeBg        = hellBtnImg;
            ui.infoTextJa        = infoJaGo.GetComponent<Text>();
            ui.infoTextEn        = infoEnGo.GetComponent<Text>();
            ui.hellModeInfoJa    = hellJaGo.GetComponent<Text>();
            ui.hellModeInfoEn    = hellEnGo.GetComponent<Text>();
            ui.rulesPanel        = rulesPanel;
            ui.topicManagerPanel = topicSheetPanel;
            ui.rainingLemonPrefab= lemonPrefab;
            ui.lemonRainParent   = rainParent.GetComponent<RectTransform>();
            EditorUtility.SetDirty(uiHolder);

            // ══════════════════════════════════════════════════════════════════
            // SETUP PANEL
            // ══════════════════════════════════════════════════════════════════
            var setupPanel = Stretch(canvasGo, "SetupPanel");
            uiMgr.setupPanel = setupPanel;
            uiMgr.setupPanel.SetActive(false);
            BuildSetupPanel(setupPanel);

            // ══════════════════════════════════════════════════════════════════
            // TOPIC DISPLAY PANEL
            // ══════════════════════════════════════════════════════════════════
            var topicDisplayPanel = Stretch(canvasGo, "TopicDisplayPanel");
            uiMgr.topicDisplayPanel = topicDisplayPanel;
            topicDisplayPanel.SetActive(false);
            BuildTopicDisplayPanel(topicDisplayPanel);

            // ══════════════════════════════════════════════════════════════════
            // SECRET REVEAL PANEL
            // ══════════════════════════════════════════════════════════════════
            var secretRevealPanel = Stretch(canvasGo, "SecretRevealPanel");
            uiMgr.secretRevealPanel = secretRevealPanel;
            secretRevealPanel.SetActive(false);
            BuildSecretRevealPanel(secretRevealPanel);

            // ══════════════════════════════════════════════════════════════════
            // DISCUSSION PANEL
            // ══════════════════════════════════════════════════════════════════
            var discussionPanel = Stretch(canvasGo, "DiscussionPanel");
            uiMgr.discussionPanel = discussionPanel;
            discussionPanel.SetActive(false);
            BuildDiscussionPanel(discussionPanel);

            // ══════════════════════════════════════════════════════════════════
            // INPUT ANSWER PANEL
            // ══════════════════════════════════════════════════════════════════
            var inputAnswerPanel = Stretch(canvasGo, "InputAnswerPanel");
            uiMgr.inputAnswerPanel = inputAnswerPanel;
            inputAnswerPanel.SetActive(false);
            BuildInputAnswerPanel(inputAnswerPanel);

            // ══════════════════════════════════════════════════════════════════
            // RESULT PANEL
            // ══════════════════════════════════════════════════════════════════
            var resultPanel = Stretch(canvasGo, "ResultPanel");
            uiMgr.resultPanel = resultPanel;
            resultPanel.SetActive(false);
            BuildResultPanel(resultPanel);

            // ══════════════════════════════════════════════════════════════════
            // LAST TURN WARNING PANEL
            // ══════════════════════════════════════════════════════════════════
            var lastTurnPanel = Stretch(canvasGo, "LastTurnPanel");
            uiMgr.lastTurnPanel = lastTurnPanel;
            lastTurnPanel.SetActive(false);
            BuildLastTurnPanel(lastTurnPanel);

            // ══════════════════════════════════════════════════════════════════
            // GAME CLEAR PANEL
            // ══════════════════════════════════════════════════════════════════
            var gameClearPanel = Stretch(canvasGo, "GameClearPanel");
            uiMgr.gameClearPanel = gameClearPanel;
            gameClearPanel.SetActive(false);
            BuildGameClearPanel(gameClearPanel);

            // ══════════════════════════════════════════════════════════════════
            // GAME OVER PANEL
            // ══════════════════════════════════════════════════════════════════
            var gameOverPanel = Stretch(canvasGo, "GameOverPanel");
            uiMgr.gameOverPanel = gameOverPanel;
            gameOverPanel.SetActive(false);
            BuildGameOverPanel(gameOverPanel);

            EditorUtility.SetDirty(canvasGo);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BOMBOMLemon] Scene built successfully (Title + all game screens).");
        }

        // ════════════════════════════════════════════════════════════════════
        // PANEL BUILDERS
        // ════════════════════════════════════════════════════════════════════

        static void BuildSetupPanel(GameObject panel)
        {
            var ui = panel.AddComponent<SetupScreenUI>();

            // Header bar
            var header = MakeRect(panel, "Header", 0, 1, 0, 1,
                new Vector2(0, -H + 80), new Vector2(0, 0));
            OverrideRect(header, Vector2.zero, Vector2.one, new Vector2(0, H - 80), new Vector2(0, H));
            var headerImg = header.AddComponent<Image>();
            headerImg.color = new Color(1, 1, 1, 0.2f);

            // Title text
            var titleGo = SimpleText(panel, "HeaderText", "プレイヤー設定",
                22, FontStyle.Bold, DarkBrown, TextAnchor.MiddleCenter);
            var titleRT = titleGo.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 1f); titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0, -44f);
            titleRT.sizeDelta = new Vector2(0, 44f);
            ui.headerText = titleGo.GetComponent<Text>();

            // Player count label (top right)
            var countGo = SimpleText(panel, "PlayerCountLabel", "4人",
                16, FontStyle.Bold, SubBrown, TextAnchor.MiddleRight);
            var countRT = countGo.GetComponent<RectTransform>();
            countRT.anchorMin = new Vector2(1f, 1f); countRT.anchorMax = new Vector2(1f, 1f);
            countRT.pivot = new Vector2(1f, 1f);
            countRT.anchoredPosition = new Vector2(-16f, -50f);
            countRT.sizeDelta = new Vector2(80f, 30f);
            ui.playerCountLabel = countGo.GetComponent<Text>();

            // Scroll view for player list
            var scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRT = scrollGo.AddComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero; scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = new Vector2(16f, 100f);
            scrollRT.offsetMax = new Vector2(-16f, -100f);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var vpRT = viewportGo.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
            viewportGo.AddComponent<Image>().color = Color.clear;
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vpRT;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRT = contentGo.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero; contentRT.sizeDelta = Vector2.zero;
            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6; vlg.padding = new RectOffset(0, 0, 4, 4);
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRT;
            ui.playerListContent = contentRT;

            // Add player button
            var addGo = MakeButton(panel, "AddButton", "+ プレイヤーを追加",
                15, DarkBrown, new Color(1, 1, 1, 0.6f), new Vector2(200f, 40f));
            var addRT = addGo.GetComponent<RectTransform>();
            addRT.anchorMin = addRT.anchorMax = new Vector2(0.5f, 0f);
            addRT.pivot = new Vector2(0.5f, 0f);
            addRT.anchoredPosition = new Vector2(0, 56f);
            addRT.sizeDelta = new Vector2(200f, 40f);
            ui.addButton = addGo.GetComponent<Button>();
            ui.addButton.onClick.AddListener(() =>
            {
                if (GameUIManager.Instance != null)
                {
                    var setupUI = panel.GetComponent<SetupScreenUI>();
                    setupUI?.OnAddPlayer();
                }
            });

            // Start button
            var startGo = MakeButton(panel, "StartButton", "スタート！",
                18, Color.white, new Color(0.95f, 0.75f, 0.05f, 1f), new Vector2(200f, 52f));
            var startRT = startGo.GetComponent<RectTransform>();
            startRT.anchorMin = startRT.anchorMax = new Vector2(0.5f, 0f);
            startRT.pivot = new Vector2(0.5f, 0f);
            startRT.anchoredPosition = new Vector2(0, 8f);
            startRT.sizeDelta = new Vector2(200f, 52f);
            ui.startButton = startGo.GetComponent<Button>();
            ui.startButtonLabel = startGo.GetComponentInChildren<Text>();
            ui.startButton.onClick.AddListener(() => panel.GetComponent<SetupScreenUI>()?.OnStart());

            EditorUtility.SetDirty(panel);
        }

        static void BuildTopicDisplayPanel(GameObject panel)
        {
            var ui = panel.AddComponent<TopicScreenUI>();
            var vl = AddVStack(panel, 16, 60, 60, 20, 20);

            ui.playerLabel  = AddVStackText(vl, "PlayerLabel", "{name}さんへ",
                18, FontStyle.Bold, DarkBrown, 40);
            ui.categoryLabel = AddVStackText(vl, "CategoryLabel", "",
                13, FontStyle.Normal, SubBrown, 24);
            ui.topicText    = AddVStackText(vl, "TopicText", "",
                26, FontStyle.Bold, DarkBrown, 120);
            ui.topicText.horizontalOverflow = HorizontalWrapMode.Wrap;
            ui.topicText.verticalOverflow   = VerticalWrapMode.Overflow;

            // Bottom buttons
            ui.changeButton = AddBottomButton(panel, "ChangeBtn", "お題を変える",
                14, SubBrown, new Color(1,1,1,0.5f), new Vector2(160f,38f), 68f);
            ui.changeButton.onClick.AddListener(() => panel.GetComponent<TopicScreenUI>()?.OnChange());

            ui.nextButton = AddBottomButton(panel, "NextBtn", "次へ →",
                17, Color.white, new Color(0.95f, 0.75f, 0.05f), new Vector2(160f,48f), 16f);
            ui.nextButton.onClick.AddListener(() => panel.GetComponent<TopicScreenUI>()?.OnNext());

            EditorUtility.SetDirty(panel);
        }

        static void BuildSecretRevealPanel(GameObject panel)
        {
            var ui = panel.AddComponent<SecretRevealScreenUI>();
            var vl = AddVStack(panel, 16, 80, 60, 20, 20);

            ui.playerLabel = AddVStackText(vl, "PlayerLabel", "{name}さんだけ見てください",
                16, FontStyle.Bold, DarkBrown, 40);

            var numGo = new GameObject("SecretNumber");
            numGo.transform.SetParent(vl.transform, false);
            var numLE = numGo.AddComponent<LayoutElement>(); numLE.minHeight = 100;
            ui.secretNumberText = numGo.AddComponent<Text>();
            ui.secretNumberText.text = "？？？"; ui.secretNumberText.fontSize = 72;
            ui.secretNumberText.fontStyle = FontStyle.Bold;
            ui.secretNumberText.color = DarkBrown; ui.secretNumberText.alignment = TextAnchor.MiddleCenter;

            ui.revealButton = AddBottomButton(panel, "RevealBtn", "タップして確認",
                16, Color.white, new Color(0.95f, 0.75f, 0.05f), new Vector2(200f, 48f), 80f);
            ui.revealButton.onClick.AddListener(() => panel.GetComponent<SecretRevealScreenUI>()?.OnReveal());

            ui.confirmButton = AddBottomButton(panel, "ConfirmBtn", "確認しました",
                16, Color.white, new Color(0.3f, 0.7f, 0.2f), new Vector2(200f, 48f), 24f);
            ui.confirmButton.gameObject.SetActive(false);
            ui.confirmButton.onClick.AddListener(() => panel.GetComponent<SecretRevealScreenUI>()?.OnConfirm());

            EditorUtility.SetDirty(panel);
        }

        static void BuildDiscussionPanel(GameObject panel)
        {
            var ui = panel.AddComponent<DiscussionScreenUI>();
            var vl = AddVStack(panel, 16, 80, 120, 20, 20);

            ui.playerLabel  = AddVStackText(vl, "PlayerLabel", "{name}さんが答えます",
                17, FontStyle.Bold, DarkBrown, 40);
            ui.categoryLabel = AddVStackText(vl, "CategoryLabel", "",
                13, FontStyle.Normal, SubBrown, 24);
            ui.topicText    = AddVStackText(vl, "TopicText", "",
                24, FontStyle.Bold, DarkBrown, 100);
            ui.topicText.horizontalOverflow = HorizontalWrapMode.Wrap;
            ui.topicText.verticalOverflow   = VerticalWrapMode.Overflow;
            ui.lifeLabel    = AddVStackText(vl, "LifeLabel", "🍋 × ?",
                20, FontStyle.Bold, new Color(0.8f, 0.4f, 0f), 40);

            ui.inputButton = AddBottomButton(panel, "InputBtn", "答えを入力する →",
                17, Color.white, new Color(0.95f, 0.75f, 0.05f), new Vector2(220f, 52f), 16f);
            ui.inputButton.onClick.AddListener(() => panel.GetComponent<DiscussionScreenUI>()?.OnProceed());

            EditorUtility.SetDirty(panel);
        }

        static void BuildInputAnswerPanel(GameObject panel)
        {
            var ui = panel.AddComponent<InputAnswerScreenUI>();

            // Player label (top)
            ui.playerLabel = SimpleText(panel, "PlayerLabel", "{name}さんが入力",
                16, FontStyle.Bold, DarkBrown, TextAnchor.MiddleCenter);
            PositionFromTop(ui.playerLabel.gameObject, 70, 40);

            // Topic (small, below player label)
            ui.topicText = SimpleText(panel, "TopicText", "",
                14, FontStyle.Normal, SubBrown, TextAnchor.MiddleCenter);
            ui.topicText.horizontalOverflow = HorizontalWrapMode.Wrap;
            PositionFromTop(ui.topicText.gameObject, 118, 40);

            // Big number display
            var numGo = new GameObject("NumberDisplay");
            numGo.transform.SetParent(panel.transform, false);
            var numRT = numGo.AddComponent<RectTransform>();
            numRT.anchorMin = numRT.anchorMax = new Vector2(0.5f, 0.55f);
            numRT.pivot = new Vector2(0.5f, 0.5f);
            numRT.anchoredPosition = Vector2.zero; numRT.sizeDelta = new Vector2(200, 100);
            ui.numberDisplay = numGo.AddComponent<Text>();
            ui.numberDisplay.text = "50"; ui.numberDisplay.fontSize = 80;
            ui.numberDisplay.fontStyle = FontStyle.Bold;
            ui.numberDisplay.color = DarkBrown; ui.numberDisplay.alignment = TextAnchor.MiddleCenter;

            // Slider
            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(panel.transform, false);
            var sliderRT = sliderGo.AddComponent<RectTransform>();
            sliderRT.anchorMin = new Vector2(0.1f, 0.35f); sliderRT.anchorMax = new Vector2(0.9f, 0.35f);
            sliderRT.pivot = new Vector2(0.5f, 0.5f);
            sliderRT.anchoredPosition = Vector2.zero; sliderRT.sizeDelta = new Vector2(0, 40);
            var slider = BuildSlider(sliderGo);
            ui.slider = slider;
            slider.onValueChanged.AddListener(val => panel.GetComponent<InputAnswerScreenUI>()?.OnSliderChanged(val));

            // Step buttons row
            var btnRow = new GameObject("BtnRow");
            btnRow.transform.SetParent(panel.transform, false);
            var btnRowRT = btnRow.AddComponent<RectTransform>();
            btnRowRT.anchorMin = new Vector2(0.05f, 0.2f); btnRowRT.anchorMax = new Vector2(0.95f, 0.2f);
            btnRowRT.pivot = new Vector2(0.5f, 0.5f);
            btnRowRT.anchoredPosition = Vector2.zero; btnRowRT.sizeDelta = new Vector2(0, 48);
            var hg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hg.spacing = 8; hg.childForceExpandWidth = true; hg.childForceExpandHeight = true;

            ui.minusTenBtn = AddHRowButton(btnRow, "-10", 16, DarkBrown, new Color(1,1,1,0.6f));
            ui.minusTenBtn.onClick.AddListener(() => panel.GetComponent<InputAnswerScreenUI>()?.OnMinusTen());
            ui.minusOneBtn = AddHRowButton(btnRow, "-1", 16, DarkBrown, new Color(1,1,1,0.6f));
            ui.minusOneBtn.onClick.AddListener(() => panel.GetComponent<InputAnswerScreenUI>()?.OnMinusOne());
            ui.plusOneBtn  = AddHRowButton(btnRow, "+1", 16, DarkBrown, new Color(1,1,1,0.6f));
            ui.plusOneBtn.onClick.AddListener(() => panel.GetComponent<InputAnswerScreenUI>()?.OnPlusOne());
            ui.plusTenBtn  = AddHRowButton(btnRow, "+10", 16, DarkBrown, new Color(1,1,1,0.6f));
            ui.plusTenBtn.onClick.AddListener(() => panel.GetComponent<InputAnswerScreenUI>()?.OnPlusTen());

            // Submit button
            ui.submitButton = AddBottomButton(panel, "SubmitBtn", "決定",
                18, Color.white, new Color(0.95f, 0.75f, 0.05f), new Vector2(180f, 52f), 16f);
            ui.submitButton.onClick.AddListener(() => panel.GetComponent<InputAnswerScreenUI>()?.OnSubmit());

            EditorUtility.SetDirty(panel);
        }

        static void BuildResultPanel(GameObject panel)
        {
            var ui = panel.AddComponent<ResultScreenUI>();

            var vl = AddVStack(panel, 12, 70, 180, 20, 20);
            ui.secretLabel = AddVStackText(vl, "SecretLabel", "秘密の数字：?",
                18, FontStyle.Bold, DarkBrown, 36);
            ui.answerLabel = AddVStackText(vl, "AnswerLabel", "答え：?",
                18, FontStyle.Normal, DarkBrown, 36);
            ui.diffLabel = AddVStackText(vl, "DiffLabel", "差：?",
                16, FontStyle.Normal, SubBrown, 30);
            ui.resultMessageLabel = AddVStackText(vl, "ResultMsg", "",
                28, FontStyle.Bold, new Color(0.9f, 0.4f, 0.1f), 48);
            ui.lemonsLabel = AddVStackText(vl, "LemonsLabel", "🍋 × ?",
                22, FontStyle.Bold, new Color(0.8f, 0.4f, 0f), 40);

            // Help card overlay panel
            var hcPanel = Stretch(panel, "HelpCardPanel");
            hcPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            ui.helpCardPanel = hcPanel;
            hcPanel.SetActive(false);

            var hcVl = AddVStack(hcPanel, 16, 0, 0, 30, 30);
            hcVl.GetComponent<RectTransform>().anchorMin = new Vector2(0.05f, 0.3f);
            hcVl.GetComponent<RectTransform>().anchorMax = new Vector2(0.95f, 0.7f);

            var hcTitle = AddVStackText(hcVl, "HcTitle", "ヘルプカードを使いますか？",
                18, FontStyle.Bold, Color.white, 44);
            ui.helpCardInfoText = AddVStackText(hcVl, "HcInfo", "",
                14, FontStyle.Normal, new Color(0.9f, 0.9f, 0.9f), 32);

            var hcBtnRow = new GameObject("HcBtnRow");
            hcBtnRow.transform.SetParent(hcVl.transform, false);
            var hcBtnLE = hcBtnRow.AddComponent<LayoutElement>(); hcBtnLE.minHeight = 52;
            var hcHG = hcBtnRow.AddComponent<HorizontalLayoutGroup>();
            hcHG.spacing = 16; hcHG.childForceExpandWidth = true; hcHG.childForceExpandHeight = true;

            ui.useHelpCardButton  = AddHRowButton(hcBtnRow, "使う",   16, Color.white, new Color(0.2f,0.6f,0.2f));
            ui.skipHelpCardButton = AddHRowButton(hcBtnRow, "使わない", 14, Color.white, new Color(0.5f,0.3f,0.1f));
            ui.useHelpCardButton.onClick.AddListener(() => panel.GetComponent<ResultScreenUI>()?.OnUseHelpCard());
            ui.skipHelpCardButton.onClick.AddListener(() => panel.GetComponent<ResultScreenUI>()?.OnSkipHelpCard());

            // Game over confirm overlay
            var goPanel = Stretch(panel, "GameOverConfirmPanel");
            goPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.55f);
            ui.gameOverConfirmPanel = goPanel;
            goPanel.SetActive(false);

            ui.gameOverConfirmText = SimpleText(goPanel, "GoText", "ライフが 0 になりました…",
                20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            var goTextRT = ui.gameOverConfirmText.gameObject.GetComponent<RectTransform>();
            goTextRT.anchorMin = new Vector2(0f, 0.5f); goTextRT.anchorMax = new Vector2(1f, 0.5f);
            goTextRT.pivot = new Vector2(0.5f, 0.5f);
            goTextRT.anchoredPosition = new Vector2(0, 30); goTextRT.sizeDelta = new Vector2(0, 40);
            ui.gameOverConfirmText.horizontalOverflow = HorizontalWrapMode.Wrap;

            ui.gameOverConfirmButton = AddBottomButton(goPanel, "GoConfirmBtn", "確認",
                17, Color.white, new Color(0.8f, 0.2f, 0.2f), new Vector2(160f, 48f), 100f);
            ui.gameOverConfirmButton.onClick.AddListener(() => panel.GetComponent<ResultScreenUI>()?.OnConfirmGameOver());

            // Normal next turn button
            ui.nextTurnButton = AddBottomButton(panel, "NextTurnBtn", "次のターンへ →",
                17, Color.white, new Color(0.95f, 0.75f, 0.05f), new Vector2(200f, 52f), 16f);
            ui.nextTurnButton.onClick.AddListener(() => panel.GetComponent<ResultScreenUI>()?.OnNextTurn());

            EditorUtility.SetDirty(panel);
        }

        static void BuildLastTurnPanel(GameObject panel)
        {
            var ui = panel.AddComponent<LastTurnScreenUI>();
            var vl = AddVStack(panel, 20, 0, 100, 30, 30);

            ui.playerLabel  = AddVStackText(vl, "PlayerLabel", "最後のターン！",
                26, FontStyle.Bold, DarkBrown, 80);
            ui.playerLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            ui.helpCardText = AddVStackText(vl, "HelpCardText", "",
                17, FontStyle.Normal, SubBrown, 40);

            ui.proceedButton = AddBottomButton(panel, "ProceedBtn", "OK",
                18, Color.white, new Color(0.95f, 0.75f, 0.05f), new Vector2(160f, 52f), 16f);
            ui.proceedButton.onClick.AddListener(() => panel.GetComponent<LastTurnScreenUI>()?.OnProceed());

            EditorUtility.SetDirty(panel);
        }

        static void BuildGameClearPanel(GameObject panel)
        {
            var ui = panel.AddComponent<GameClearScreenUI>();
            var vl = AddVStack(panel, 20, 0, 120, 30, 30);

            ui.clearMessageText = AddVStackText(vl, "ClearMsg", "ゲームクリア！🎉",
                34, FontStyle.Bold, new Color(0.1f, 0.55f, 0.1f), 60);
            ui.lemonsText = AddVStackText(vl, "LemonsText", "残りライフ：🍋 × ?",
                22, FontStyle.Bold, new Color(0.8f, 0.4f, 0f), 48);

            ui.retryButton = AddBottomButton(panel, "RetryBtn", "もう一度",
                16, DarkBrown, new Color(1,1,1,0.7f), new Vector2(160f, 48f), 72f);
            ui.retryButton.onClick.AddListener(() => panel.GetComponent<GameClearScreenUI>()?.OnRetry());

            ui.titleButton = AddBottomButton(panel, "TitleBtn", "タイトルへ",
                16, Color.white, new Color(0.6f, 0.4f, 0.1f), new Vector2(160f, 48f), 16f);
            ui.titleButton.onClick.AddListener(() => panel.GetComponent<GameClearScreenUI>()?.OnTitle());

            EditorUtility.SetDirty(panel);
        }

        static void BuildGameOverPanel(GameObject panel)
        {
            var ui = panel.AddComponent<GameOverScreenUI>();
            var vl = AddVStack(panel, 16, 0, 120, 30, 30);

            ui.gameOverText = AddVStackText(vl, "GameOverText", "ゲームオーバー…",
                30, FontStyle.Bold, new Color(0.7f, 0.1f, 0.1f), 56);
            ui.playerNameText = AddVStackText(vl, "PlayerNameText", "",
                18, FontStyle.Normal, DarkBrown, 40);
            ui.lemonsText = AddVStackText(vl, "LemonsText", "",
                20, FontStyle.Bold, new Color(0.8f, 0.4f, 0f), 40);

            ui.retryButton = AddBottomButton(panel, "RetryBtn", "もう一度",
                16, DarkBrown, new Color(1,1,1,0.7f), new Vector2(160f, 48f), 72f);
            ui.retryButton.onClick.AddListener(() => panel.GetComponent<GameOverScreenUI>()?.OnRetry());

            ui.titleButton = AddBottomButton(panel, "TitleBtn", "タイトルへ",
                16, Color.white, new Color(0.6f, 0.4f, 0.1f), new Vector2(160f, 48f), 16f);
            ui.titleButton.onClick.AddListener(() => panel.GetComponent<GameOverScreenUI>()?.OnTitle());

            EditorUtility.SetDirty(panel);
        }

        // ════════════════════════════════════════════════════════════════════
        // Title scene helpers (unchanged from original)
        // ════════════════════════════════════════════════════════════════════

        static void BuildRulesContent(GameObject panel)
        {
            string[] rules =
            {
                "全員でライフレモンを守るチームゲーム",
                "出番のプレイヤーだけ0〜99の数字を見る",
                "お題に合う答えを言う",
                "全員で相談して数字を予想する",
                "差の分だけライフレモンが減る",
                "ぴったりならライフレモンが増える",
                "全員のターンが終わるまでレモンを守りきれば勝利",
            };
            var bodyGo = new GameObject("RulesBody");
            bodyGo.transform.SetParent(panel.transform, false);
            var bodyRT = bodyGo.AddComponent<RectTransform>();
            bodyRT.anchorMin = Vector2.zero; bodyRT.anchorMax = Vector2.one;
            bodyRT.offsetMin = new Vector2(24f, 80f); bodyRT.offsetMax = new Vector2(-24f, -110f);
            var vl = bodyGo.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 14f; vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
            foreach (var rule in rules)
            {
                var rGo = new GameObject("Rule"); rGo.transform.SetParent(bodyGo.transform, false);
                rGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 32);
                var le = rGo.AddComponent<LayoutElement>(); le.minHeight = 32;
                var txt = rGo.AddComponent<Text>();
                txt.text = "● " + rule; txt.fontSize = 15; txt.fontStyle = FontStyle.Bold;
                txt.color = DarkBrown; txt.alignment = TextAnchor.MiddleLeft;
                txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                txt.verticalOverflow   = VerticalWrapMode.Overflow;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // UI Helpers
        // ════════════════════════════════════════════════════════════════════

        static GameObject Stretch(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        static GameObject AnchorImage(GameObject parent, string name,
            string spritePath, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            var spr = LoadSprite(spritePath);
            if (spr != null) img.sprite = spr;
            img.preserveAspect = true; img.raycastTarget = false;
            return go;
        }

        static GameObject AnchorSpriteButton(GameObject parent, string name,
            string spritePath, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            var spr = LoadSprite(spritePath);
            if (spr != null) img.sprite = spr;
            img.preserveAspect = true;
            var btn = go.AddComponent<Button>(); NoNav(btn);
            return go;
        }

        static GameObject AnchorText(GameObject parent, string name,
            string text, int fontSize, FontStyle style, Color color,
            Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = size;
            var txt = go.AddComponent<Text>();
            txt.text = text; txt.fontSize = fontSize; txt.fontStyle = style;
            txt.color = color; txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;
            return go;
        }

        static GameObject CapsuleButton(GameObject parent, string name,
            string label, int fontSize, Color textColor, Color bgColor, Vector2 size)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = CapsuleSprite(); img.type = Image.Type.Sliced; img.color = bgColor;
            var btn = go.AddComponent<Button>(); NoNav(btn);
            var lblGo = new GameObject("Label"); lblGo.transform.SetParent(go.transform, false);
            var lblRT = lblGo.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = new Vector2(10f, 7f); lblRT.offsetMax = new Vector2(-10f, -7f);
            var txt = lblGo.AddComponent<Text>();
            txt.text = label; txt.fontSize = fontSize; txt.fontStyle = FontStyle.Bold;
            txt.color = textColor; txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;
            return go;
        }

        static GameObject SheetPanel(GameObject parent, string name, string title,
            System.Action<GameObject> buildContent)
        {
            var go = Stretch(parent, name);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(1.00f, 0.96f, 0.60f, 0.97f); bg.raycastTarget = true;

            var titleGo = new GameObject("PanelTitle"); titleGo.transform.SetParent(go.transform, false);
            var titleRT = titleGo.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 1f); titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.offsetMin = new Vector2(24f, -(40f + 60f)); titleRT.offsetMax = new Vector2(-60f, -40f);
            var titleTxt = titleGo.AddComponent<Text>();
            titleTxt.text = title; titleTxt.fontSize = 26; titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = DarkBrown; titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;

            var closeGo = new GameObject("CloseButton"); closeGo.transform.SetParent(go.transform, false);
            var closeRT = closeGo.AddComponent<RectTransform>();
            closeRT.anchorMin = closeRT.anchorMax = new Vector2(1f, 1f); closeRT.pivot = new Vector2(1f, 1f);
            closeRT.anchoredPosition = new Vector2(-16f, -40f); closeRT.sizeDelta = new Vector2(44f, 44f);
            closeGo.AddComponent<Image>().color = Color.clear;
            var closeBtn = closeGo.AddComponent<Button>(); NoNav(closeBtn);

            var closeXGo = new GameObject("X"); closeXGo.transform.SetParent(closeGo.transform, false);
            var closeXRT = closeXGo.AddComponent<RectTransform>();
            closeXRT.anchorMin = Vector2.zero; closeXRT.anchorMax = Vector2.one;
            closeXRT.offsetMin = closeXRT.offsetMax = Vector2.zero;
            var closeX = closeXGo.AddComponent<Text>();
            closeX.text = "×"; closeX.fontSize = 24; closeX.color = SubBrown;
            closeX.alignment = TextAnchor.MiddleCenter;

            buildContent?.Invoke(go);
            return go;
        }

        // Vertical stack container anchored within a panel
        static GameObject AddVStack(GameObject panel, float spacing,
            float topPad, float bottomPad, float leftPad, float rightPad)
        {
            var go = new GameObject("VStack"); go.transform.SetParent(panel.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(leftPad, bottomPad);
            rt.offsetMax = new Vector2(-rightPad, -topPad);
            var vl = go.AddComponent<VerticalLayoutGroup>();
            vl.spacing = spacing; vl.childAlignment = TextAnchor.MiddleCenter;
            vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
            vl.padding = new RectOffset(0, 0, 8, 8);
            return go;
        }

        static Text AddVStackText(GameObject vstack, string name, string text,
            int fontSize, FontStyle style, Color color, float minHeight)
        {
            var go = new GameObject(name); go.transform.SetParent(vstack.transform, false);
            var le = go.AddComponent<LayoutElement>(); le.minHeight = minHeight;
            var txt = go.AddComponent<Text>();
            txt.text = text; txt.fontSize = fontSize; txt.fontStyle = style;
            txt.color = color; txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;
            return txt;
        }

        static Button AddBottomButton(GameObject panel, string name, string label,
            int fontSize, Color textColor, Color bgColor, Vector2 size, float fromBottom)
        {
            var go = MakeButton(panel, name, label, fontSize, textColor, bgColor, size);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0, fromBottom);
            rt.sizeDelta = size;
            return go.GetComponent<Button>();
        }

        static Button AddHRowButton(GameObject parent, string label,
            int fontSize, Color textColor, Color bgColor)
        {
            var go = new GameObject(label); go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>(); img.color = bgColor;
            var btn = go.AddComponent<Button>(); NoNav(btn);
            var lblGo = new GameObject("L"); lblGo.transform.SetParent(go.transform, false);
            var lblRT = lblGo.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
            var txt = lblGo.AddComponent<Text>();
            txt.text = label; txt.fontSize = fontSize; txt.fontStyle = FontStyle.Bold;
            txt.color = textColor; txt.alignment = TextAnchor.MiddleCenter;
            return btn;
        }

        static GameObject MakeButton(GameObject parent, string name, string label,
            int fontSize, Color textColor, Color bgColor, Vector2 size)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>(); rt.sizeDelta = size;
            var img = go.AddComponent<Image>(); img.color = bgColor;
            var btn = go.AddComponent<Button>(); NoNav(btn);
            var lblGo = new GameObject("Label"); lblGo.transform.SetParent(go.transform, false);
            var lblRT = lblGo.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = new Vector2(4, 4); lblRT.offsetMax = new Vector2(-4, -4);
            var txt = lblGo.AddComponent<Text>();
            txt.text = label; txt.fontSize = fontSize; txt.fontStyle = FontStyle.Bold;
            txt.color = textColor; txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            return go;
        }

        static Text SimpleText(GameObject parent, string name, string text,
            int fontSize, FontStyle style, Color color, TextAnchor align)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            var txt = go.AddComponent<Text>();
            txt.text = text; txt.fontSize = fontSize; txt.fontStyle = style;
            txt.color = color; txt.alignment = align;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;
            return txt;
        }

        // Position a GO from top anchor
        static void PositionFromTop(GameObject go, float fromTop, float height)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 1f); rt.anchorMax = new Vector2(0.9f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -fromTop); rt.sizeDelta = new Vector2(0, height);
        }

        static void OverrideRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        }

        static GameObject MakeRect(GameObject parent, string name,
            float ax, float ay, float bx, float by,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(bx, by);
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return go;
        }

        static Slider BuildSlider(GameObject go)
        {
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 99; slider.value = 50;
            slider.wholeNumbers = true;

            var bgGo = new GameObject("Background"); bgGo.transform.SetParent(go.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0, 0.25f); bgRT.anchorMax = new Vector2(1, 0.75f);
            bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>(); bgImg.color = new Color(0.8f, 0.8f, 0.8f);
            slider.targetGraphic = bgImg;

            var fillAreaGo = new GameObject("FillArea"); fillAreaGo.transform.SetParent(go.transform, false);
            var fillAreaRT = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0, 0.25f); fillAreaRT.anchorMax = new Vector2(1, 0.75f);
            fillAreaRT.offsetMin = new Vector2(5, 0); fillAreaRT.offsetMax = new Vector2(-5, 0);

            var fillGo = new GameObject("Fill"); fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRT = fillGo.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>(); fillImg.color = new Color(0.95f, 0.75f, 0.05f);
            slider.fillRect = fillRT;

            var handleAreaGo = new GameObject("HandleArea"); handleAreaGo.transform.SetParent(go.transform, false);
            var handleAreaRT = handleAreaGo.AddComponent<RectTransform>();
            handleAreaRT.anchorMin = Vector2.zero; handleAreaRT.anchorMax = Vector2.one;
            handleAreaRT.offsetMin = new Vector2(10, 0); handleAreaRT.offsetMax = new Vector2(-10, 0);

            var handleGo = new GameObject("Handle"); handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleRT = handleGo.AddComponent<RectTransform>();
            handleRT.anchorMin = handleRT.anchorMax = new Vector2(0.5f, 0.5f);
            handleRT.pivot = new Vector2(0.5f, 0.5f); handleRT.sizeDelta = new Vector2(24, 24);
            var handleImg = handleGo.AddComponent<Image>(); handleImg.color = DarkBrown;
            handleImg.sprite = KnobSprite();
            slider.handleRect = handleRT;

            return slider;
        }

        // ── Lemon prefab ────────────────────────────────────────────────────
        static GameObject EnsureLemonPrefab()
        {
            const string path = "Assets/Prefabs/RainingLemonItem.prefab";
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            var existing = Load<GameObject>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);

            var go  = new GameObject("RainingLemonItem");
            var rt  = go.AddComponent<RectTransform>(); rt.sizeDelta = new Vector2(50f, 50f);
            var img = go.AddComponent<Image>();
            var spr = LoadSprite("Assets/Sprites/Title_Lemon.png");
            if (spr != null) { img.sprite = spr; img.preserveAspect = true; }
            img.raycastTarget = false;
            go.AddComponent<RainingLemonItem>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            AssetDatabase.Refresh();
            return prefab;
        }

        // ── Input System fix ────────────────────────────────────────────────
        internal static void FixActiveInputHandler()
        {
            const string path = "ProjectSettings/ProjectSettings.asset";
            if (!System.IO.File.Exists(path)) return;
            var text = System.IO.File.ReadAllText(path);
            if (text.Contains("m_ActiveInputHandler: -1"))
            {
                System.IO.File.WriteAllText(path,
                    text.Replace("m_ActiveInputHandler: -1", "m_ActiveInputHandler: 1"));
                Debug.Log("[BOMBOMLemon] Fixed activeInputHandler -1 → 1 on disk");
            }
            try
            {
                var ps = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
                if (ps != null)
                {
                    var so = new SerializedObject(ps);
                    var prop = so.FindProperty("activeInputHandler");
                    if (prop != null && prop.intValue == -1)
                    {
                        prop.intValue = 1;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        Debug.Log("[BOMBOMLemon] Fixed activeInputHandler -1 → 1 in memory");
                    }
                }
            }
            catch { /* non-critical */ }
        }

        // ── EventSystem ─────────────────────────────────────────────────────
        static void EnsureEventSystem(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var es = root.GetComponent<EventSystem>();
                if (es == null) continue;
                var old = root.GetComponent<StandaloneInputModule>();
                if (old != null) Object.DestroyImmediate(old);
                if (root.GetComponent<InputSystemUIInputModule>() == null)
                    root.AddComponent<InputSystemUIInputModule>();
                break;
            }
        }

        // ── Asset loaders ───────────────────────────────────────────────────
        static T Load<T>(string path) where T : Object
            => AssetDatabase.LoadAssetAtPath<T>(path);

        static Sprite LoadSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter ti &&
                ti.textureType != TextureImporterType.Sprite)
            {
                ti.textureType      = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Sprite CapsuleSprite()
            => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        static Sprite KnobSprite()
            => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        static void NoNav(Button btn)
        {
            var n = btn.navigation; n.mode = Navigation.Mode.None; btn.navigation = n;
        }
    }
}
