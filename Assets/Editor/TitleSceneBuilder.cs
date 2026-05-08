using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BOMBOMLemon.Editor
{
    [InitializeOnLoad]
    public static class TitleSceneAutoBuilder
    {
        internal const string BuilderPath = "Assets/Editor/TitleSceneBuilder.cs";
        internal const string HashPrefKey = "BOMBOMLemon_BuilderHash";

        static TitleSceneAutoBuilder()
        {
            TitleSceneBuilder.FixActiveInputHandler();
            EditorApplication.update += OnFirstUpdate;
        }

        static void OnFirstUpdate()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            EditorApplication.update -= OnFirstUpdate;

            const string scenePath = "Assets/Scenes/TitleScene.unity";
            if (!System.IO.File.Exists(scenePath)) return;

            // Rebuild whenever the builder source code has changed
            string currentHash = FileHash(BuilderPath);
            if (currentHash != EditorPrefs.GetString(HashPrefKey, ""))
            {
                Debug.Log("[BOMBOMLemon] Builder changed — auto-rebuilding scene...");
                TitleSceneBuilder.Build();
                EditorPrefs.SetString(HashPrefKey, currentHash);
                return;
            }

            // Also rebuild if scene is still empty (first run)
            int sm = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int i = 0; i < sm; i++)
            {
                var loaded = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (loaded.path == scenePath)
                {
                    if (loaded.rootCount <= 2) TitleSceneBuilder.Build();
                    return;
                }
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            int rootCount = scene.rootCount;
            if (UnityEngine.SceneManagement.SceneManager.sceneCount > 1)
                EditorSceneManager.CloseScene(scene, false);

            if (rootCount <= 2)
            {
                Debug.Log("[BOMBOMLemon] Scene is empty — auto-building...");
                TitleSceneBuilder.Build();
            }
        }

        internal static string FileHash(string path)
        {
            if (!System.IO.File.Exists(path)) return "";
            using var md5    = System.Security.Cryptography.MD5.Create();
            using var stream = System.IO.File.OpenRead(path);
            return System.BitConverter.ToString(md5.ComputeHash(stream));
        }
    }

    public static class TitleSceneBuilder
    {
        const float W = 390f, H = 844f;

        // ── Colour palette (normal / hell shared) ─────────────────────────
        static readonly Color BgYellow   = new Color(1.00f, 0.96f, 0.60f);
        static readonly Color DarkBrown  = new Color(0.15f, 0.10f, 0.00f);
        static readonly Color SubBrown   = new Color(0.50f, 0.36f, 0.00f);
        static readonly Color BtnFg      = new Color(0.45f, 0.30f, 0.00f);
        static readonly Color BtnCap     = new Color(1f, 1f, 1f, 0.65f);
        static readonly Color LimeGreen  = new Color(0.25f, 0.60f, 0.08f);
        static readonly Color PrimaryYellow = new Color(0.95f, 0.75f, 0.05f);

        // Collected during Build() to wire into GameUIManager
        static readonly List<Image> _primaryImages = new();
        static readonly List<Image> _darkImages    = new();

        [MenuItem("BOMBOMLemon/Build Title Scene")]
        public static void Build()
        {
            FixActiveInputHandler();
            _primaryImages.Clear();
            _darkImages.Clear();

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity");
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<Camera>()      != null) continue;
                if (root.GetComponent<EventSystem>() != null) continue;
                Object.DestroyImmediate(root);
            }
            EnsureEventSystem(scene);

            // ── Managers ─────────────────────────────────────────────────────
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

            // ── Canvas ────────────────────────────────────────────────────────
            var canvasGo = new GameObject("Canvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = canvasGo.AddComponent<CanvasScaler>();
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(W, H);
            cs.matchWidthOrHeight  = 0f;
            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGo.AddComponent<JPFontLoader>();
            var uiMgr = canvasGo.AddComponent<GameUIManager>();

            // ── Shared background ─────────────────────────────────────────────
            var bgGo  = Stretch(canvasGo, "Background");
            var bgImg = bgGo.AddComponent<Image>(); bgImg.color = BgYellow;
            uiMgr.sharedBackground = bgImg;

            // ════════════════════════════════════════════════════════════════
            // TITLE PANEL
            // ════════════════════════════════════════════════════════════════
            var titlePanel = Stretch(canvasGo, "TitlePanel");
            uiMgr.titlePanel = titlePanel;

            var rainParent = Stretch(titlePanel, "LemonRainParent");
            const float zy = 1f - 0.42f;
            var bubbleGo = AnchorImage(titlePanel, "TitleBubble", "Assets/Sprites/Title_Bubble.png",
                new Vector2(0.5f, zy), new Vector2(386f, 225f));
            var lemonGo  = AnchorImage(titlePanel, "TitleLemon",  "Assets/Sprites/Title_Lemon.png",
                new Vector2(0.5f, zy + 70f / H), new Vector2(236f, 236f));
            var wordGo   = AnchorImage(titlePanel, "TitleWord",   "Assets/Sprites/Title_Word.png",
                new Vector2(0.5f, zy - 47f / H), new Vector2(332f, 78f));

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

            var topOverlay = Stretch(titlePanel, "TopOverlay");
            var leftGroup  = new GameObject("LeftButtons");
            leftGroup.transform.SetParent(topOverlay.transform, false);
            var lgRT = leftGroup.AddComponent<RectTransform>();
            lgRT.anchorMin = lgRT.anchorMax = new Vector2(0f, 1f);
            lgRT.pivot     = new Vector2(0f, 1f);
            lgRT.anchoredPosition = new Vector2(18f, -54f);
            lgRT.sizeDelta        = new Vector2(148f, 28f);
            var hLayout = leftGroup.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 8f; hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false; hLayout.childAlignment = TextAnchor.MiddleLeft;

            var rulesBtnGo = CapsuleButton(leftGroup, "RulesButton",  "？ ルール", 11, BtnFg, BtnCap, new Vector2(72f, 28f));
            var topicBtnGo = CapsuleButton(leftGroup, "TopicButton",  "≡ お題",   11, BtnFg, BtnCap, new Vector2(60f, 28f));

            var hellBtnGo = new GameObject("HellModeButton");
            hellBtnGo.transform.SetParent(topOverlay.transform, false);
            var hellRT = hellBtnGo.AddComponent<RectTransform>();
            hellRT.anchorMin = hellRT.anchorMax = new Vector2(1f, 1f);
            hellRT.pivot = new Vector2(1f, 1f);
            hellRT.anchoredPosition = new Vector2(-18f, -54f); hellRT.sizeDelta = new Vector2(105f, 28f);
            var hellBtnImg = hellBtnGo.AddComponent<Image>();
            hellBtnImg.sprite = CapsuleSprite(); hellBtnImg.type = Image.Type.Sliced; hellBtnImg.color = BtnCap;
            var hellBtn = hellBtnGo.AddComponent<Button>(); NoNav(hellBtn);

            var dotGo = new GameObject("HellModeIndicator"); dotGo.transform.SetParent(hellBtnGo.transform, false);
            var dotRT = dotGo.AddComponent<RectTransform>();
            dotRT.anchorMin = dotRT.anchorMax = new Vector2(0.5f, 0.5f); dotRT.pivot = new Vector2(0.5f, 0.5f);
            dotRT.anchoredPosition = new Vector2(-37f, 0f); dotRT.sizeDelta = new Vector2(8f, 8f);
            var dotImg = dotGo.AddComponent<Image>(); dotImg.sprite = KnobSprite(); dotImg.color = new Color(0.75f, 0.75f, 0.75f);

            var hellLblGo = new GameObject("HellModeLabel"); hellLblGo.transform.SetParent(hellBtnGo.transform, false);
            var hellLblRT = hellLblGo.AddComponent<RectTransform>();
            hellLblRT.anchorMin = hellLblRT.anchorMax = new Vector2(0.5f, 0.5f); hellLblRT.pivot = new Vector2(0.5f, 0.5f);
            hellLblRT.anchoredPosition = new Vector2(7f, 0f); hellLblRT.sizeDelta = new Vector2(70f, 20f);
            var hellLbl = hellLblGo.AddComponent<Text>();
            hellLbl.text = "地獄モード"; hellLbl.fontSize = 11; hellLbl.fontStyle = FontStyle.Bold;
            hellLbl.color = new Color(0.40f, 0.30f, 0.05f); hellLbl.alignment = TextAnchor.MiddleLeft;

            // Sheet panels (rules + topic)
            var rulesPanel      = SheetPanel(titlePanel, "RulesPanel",     "ルール", BuildRulesContent);
            var topicSheetPanel = SheetPanel(titlePanel, "TopicSheetPanel", "お題一覧", BuildTopicContent);
            rulesPanel.SetActive(false);
            topicSheetPanel.SetActive(false);

            var lemonPrefab = EnsureLemonPrefab();

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

            // ════════════════════════════════════════════════════════════════
            // GAME PANELS
            // ════════════════════════════════════════════════════════════════
            uiMgr.setupPanel        = BuildGamePanel(canvasGo, "SetupPanel",        BuildSetupContent);
            uiMgr.topicDisplayPanel = BuildGamePanel(canvasGo, "TopicDisplayPanel", BuildTopicDisplayContent);
            uiMgr.secretRevealPanel = BuildGamePanel(canvasGo, "SecretRevealPanel", BuildSecretRevealContent);
            uiMgr.discussionPanel   = BuildGamePanel(canvasGo, "DiscussionPanel",   BuildDiscussionContent);
            uiMgr.inputAnswerPanel  = BuildGamePanel(canvasGo, "InputAnswerPanel",  BuildInputAnswerContent);
            uiMgr.resultPanel       = BuildGamePanel(canvasGo, "ResultPanel",       BuildResultContent);
            uiMgr.lastTurnPanel     = BuildGamePanel(canvasGo, "LastTurnPanel",     BuildLastTurnContent);
            uiMgr.gameClearPanel    = BuildGamePanel(canvasGo, "GameClearPanel",    BuildGameClearContent);
            uiMgr.gameOverPanel     = BuildGamePanel(canvasGo, "GameOverPanel",     BuildGameOverContent);

            // ── Wire theme images into GameUIManager ───────────────────────
            var uiMgrSO = new SerializedObject(uiMgr);
            SetImageArray(uiMgrSO, "primaryColorImages", _primaryImages);
            SetImageArray(uiMgrSO, "darkColorImages",    _darkImages);
            uiMgrSO.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(canvasGo);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // Save builder hash so auto-rebuild doesn't fire again until code changes
            string hash = TitleSceneAutoBuilder.FileHash(TitleSceneAutoBuilder.BuilderPath);
            EditorPrefs.SetString(TitleSceneAutoBuilder.HashPrefKey, hash);

            Debug.Log($"[BOMBOMLemon] Scene built. {_primaryImages.Count} primary + {_darkImages.Count} dark theme images wired.");
        }

        // ═══════════════════════════════════════════════════════════════════
        // Generic game-panel helper: Stretch + callback to fill content
        // ═══════════════════════════════════════════════════════════════════
        static GameObject BuildGamePanel(GameObject canvas, string name,
            System.Action<GameObject> builder)
        {
            var panel = Stretch(canvas, name);
            builder(panel);
            panel.SetActive(false);
            return panel;
        }

        // ═══════════════════════════════════════════════════════════════════
        // SETUP
        // ═══════════════════════════════════════════════════════════════════
        static void BuildSetupContent(GameObject p)
        {
            var ui = p.AddComponent<SetupScreenUI>();

            // Header
            var hdr = MakeFullWidthFromTop(p, "Header", 0, 88);
            hdr.AddComponent<Image>().color = new Color(1, 1, 1, 0.15f);
            ui.headerText = MakePinnedText(p, "Title", "プレイヤー設定",
                22, FontStyle.Bold, DarkBrown, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0, -88), new Vector2(0, 88));

            ui.playerCountLabel = MakePinnedText(p, "Count", "4人",
                15, FontStyle.Bold, SubBrown, TextAnchor.MiddleRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16, -88), new Vector2(80, 30));

            // Scroll view
            var (scroll, contentRT) = MakeScrollView(p, 88, 90, 16);
            ui.playerListContent = contentRT;

            // Add player button (dark-coloured)
            var addGo = MakePrimaryButton(p, "AddButton", "+ プレイヤーを追加",
                14, DarkBrown, new Color(1f, 1f, 1f, 0.6f), new Vector2(220f, 40f));
            Pin(addGo, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 52), new Vector2(220, 40));
            _darkImages.Add(addGo.GetComponent<Image>());
            ui.addButton = addGo.GetComponent<Button>();
            ui.addButton.onClick.AddListener(() => p.GetComponent<SetupScreenUI>()?.OnAddPlayer());

            // Start button (primary-coloured)
            var startGo = MakePrimaryButton(p, "StartButton", "スタート！",
                18, Color.white, PrimaryYellow, new Vector2(220f, 52f));
            Pin(startGo, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 8), new Vector2(220, 52));
            _primaryImages.Add(startGo.GetComponent<Image>());
            ui.startButton      = startGo.GetComponent<Button>();
            ui.startButtonLabel = startGo.GetComponentInChildren<Text>();
            ui.startButton.onClick.AddListener(() => p.GetComponent<SetupScreenUI>()?.OnStart());

            EditorUtility.SetDirty(p);
        }

        // ═══════════════════════════════════════════════════════════════════
        // TOPIC DISPLAY
        // ═══════════════════════════════════════════════════════════════════
        static void BuildTopicDisplayContent(GameObject p)
        {
            var ui = p.AddComponent<TopicScreenUI>();
            var vl = VStack(p, 20, 80, 100, 20, 20);

            ui.playerLabel   = VText(vl, "PlayerLabel",   "{name}さんへ",      18, FontStyle.Bold,   DarkBrown, 40);
            ui.categoryLabel = VText(vl, "CategoryLabel", "",                   13, FontStyle.Normal, SubBrown,  32);
            ui.topicText     = VText(vl, "TopicText",     "",                   26, FontStyle.Bold,   DarkBrown, 140);
            ui.topicText.horizontalOverflow = HorizontalWrapMode.Wrap;
            ui.topicText.verticalOverflow   = VerticalWrapMode.Overflow;

            var changeGo = BottomBtn(p, "ChangeBtn", "お題を変える", 14, SubBrown,    new Color(1,1,1,0.5f), new Vector2(170f,40f), 66f);
            var nextGo   = BottomBtn(p, "NextBtn",   "次へ →",       17, Color.white, PrimaryYellow,         new Vector2(170f,48f), 14f);
            _primaryImages.Add(nextGo.GetComponent<Image>());

            ui.changeButton = changeGo.GetComponent<Button>();
            ui.nextButton   = nextGo.GetComponent<Button>();
            ui.changeButton.onClick.AddListener(() => p.GetComponent<TopicScreenUI>()?.OnChange());
            ui.nextButton.onClick.AddListener(()   => p.GetComponent<TopicScreenUI>()?.OnNext());
            EditorUtility.SetDirty(p);
        }

        // ═══════════════════════════════════════════════════════════════════
        // SECRET REVEAL
        // ═══════════════════════════════════════════════════════════════════
        static void BuildSecretRevealContent(GameObject p)
        {
            var ui = p.AddComponent<SecretRevealScreenUI>();
            var vl = VStack(p, 24, 80, 130, 20, 20);

            ui.playerLabel = VText(vl, "PlayerLabel", "{name}さんだけ見てください",
                16, FontStyle.Bold, DarkBrown, 44);

            // Number display
            var numGo = new GameObject("SecretNumber"); numGo.transform.SetParent(vl.transform, false);
            numGo.AddComponent<LayoutElement>().minHeight = 110;
            ui.secretNumberText = numGo.AddComponent<Text>();
            ui.secretNumberText.text = "？？？"; ui.secretNumberText.fontSize = 80;
            ui.secretNumberText.fontStyle = FontStyle.Bold;
            ui.secretNumberText.color = DarkBrown; ui.secretNumberText.alignment = TextAnchor.MiddleCenter;

            var revealGo  = BottomBtn(p, "RevealBtn",  "タップして確認", 16, Color.white, PrimaryYellow,              new Vector2(210f, 52f), 80f);
            var confirmGo = BottomBtn(p, "ConfirmBtn", "確認しました",   16, Color.white, new Color(0.3f,0.7f,0.2f), new Vector2(210f, 52f), 80f);
            confirmGo.SetActive(false);
            _primaryImages.Add(revealGo.GetComponent<Image>());

            ui.revealButton  = revealGo.GetComponent<Button>();
            ui.confirmButton = confirmGo.GetComponent<Button>();
            ui.revealButton.onClick.AddListener(()  => p.GetComponent<SecretRevealScreenUI>()?.OnReveal());
            ui.confirmButton.onClick.AddListener(() => p.GetComponent<SecretRevealScreenUI>()?.OnConfirm());
            EditorUtility.SetDirty(p);
        }

        // ═══════════════════════════════════════════════════════════════════
        // DISCUSSION
        // ═══════════════════════════════════════════════════════════════════
        static void BuildDiscussionContent(GameObject p)
        {
            var ui = p.AddComponent<DiscussionScreenUI>();
            var vl = VStack(p, 16, 80, 130, 20, 20);

            ui.playerLabel   = VText(vl, "PlayerLabel",   "{name}さんが答えます", 17, FontStyle.Bold,   DarkBrown, 40);
            ui.categoryLabel = VText(vl, "CategoryLabel", "",                      13, FontStyle.Normal, SubBrown,  28);
            ui.topicText     = VText(vl, "TopicText",     "",                      24, FontStyle.Bold,   DarkBrown, 120);
            ui.topicText.horizontalOverflow = HorizontalWrapMode.Wrap;
            ui.topicText.verticalOverflow   = VerticalWrapMode.Overflow;
            ui.lifeLabel     = VText(vl, "LifeLabel",     "🍋 × ?",               20, FontStyle.Bold,   new Color(0.7f,0.35f,0f), 44);

            var inputGo = BottomBtn(p, "InputBtn", "答えを入力する →", 17, Color.white, PrimaryYellow, new Vector2(230f, 52f), 14f);
            _primaryImages.Add(inputGo.GetComponent<Image>());
            ui.inputButton = inputGo.GetComponent<Button>();
            ui.inputButton.onClick.AddListener(() => p.GetComponent<DiscussionScreenUI>()?.OnProceed());
            EditorUtility.SetDirty(p);
        }

        // ═══════════════════════════════════════════════════════════════════
        // INPUT ANSWER
        // ═══════════════════════════════════════════════════════════════════
        static void BuildInputAnswerContent(GameObject p)
        {
            var ui = p.AddComponent<InputAnswerScreenUI>();

            // Player / topic labels at top
            var plGo = MakePinnedText(p, "PlayerLabel", "{name}さんが入力",
                16, FontStyle.Bold, DarkBrown, TextAnchor.MiddleCenter,
                new Vector2(0.1f,1f), new Vector2(0.9f,1f), new Vector2(0,-68), new Vector2(0,36));
            ui.playerLabel = plGo;

            var tpGo = MakePinnedText(p, "TopicText", "",
                13, FontStyle.Normal, SubBrown, TextAnchor.MiddleCenter,
                new Vector2(0.1f,1f), new Vector2(0.9f,1f), new Vector2(0,-112), new Vector2(0,30));
            tpGo.horizontalOverflow = HorizontalWrapMode.Wrap;
            ui.topicText = tpGo;

            // Big number
            var numGo = new GameObject("NumberDisplay"); numGo.transform.SetParent(p.transform, false);
            var numRT  = numGo.AddComponent<RectTransform>();
            numRT.anchorMin = numRT.anchorMax = new Vector2(0.5f, 0.58f);
            numRT.pivot = new Vector2(0.5f, 0.5f); numRT.sizeDelta = new Vector2(200, 100);
            ui.numberDisplay = numGo.AddComponent<Text>();
            ui.numberDisplay.text = "50"; ui.numberDisplay.fontSize = 80;
            ui.numberDisplay.fontStyle = FontStyle.Bold;
            ui.numberDisplay.color = DarkBrown; ui.numberDisplay.alignment = TextAnchor.MiddleCenter;

            // Slider
            var slGo = new GameObject("Slider"); slGo.transform.SetParent(p.transform, false);
            var slRT = slGo.AddComponent<RectTransform>();
            slRT.anchorMin = new Vector2(0.08f, 0.37f); slRT.anchorMax = new Vector2(0.92f, 0.37f);
            slRT.pivot = new Vector2(0.5f, 0.5f); slRT.sizeDelta = new Vector2(0, 40);
            ui.slider = BuildSlider(slGo);
            ui.slider.onValueChanged.AddListener(v => p.GetComponent<InputAnswerScreenUI>()?.OnSliderChanged(v));

            // ±1 / ±10 row
            var rowGo = new GameObject("StepRow"); rowGo.transform.SetParent(p.transform, false);
            var rowRT = rowGo.AddComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0.05f, 0.21f); rowRT.anchorMax = new Vector2(0.95f, 0.21f);
            rowRT.pivot = new Vector2(0.5f, 0.5f); rowRT.sizeDelta = new Vector2(0, 48);
            var hg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hg.spacing = 8; hg.childForceExpandWidth = true; hg.childForceExpandHeight = true;

            ui.minusTenBtn = HRowBtn(rowGo, "-10", 15, DarkBrown, new Color(1,1,1,0.6f));
            ui.minusOneBtn = HRowBtn(rowGo, "-1",  15, DarkBrown, new Color(1,1,1,0.6f));
            ui.plusOneBtn  = HRowBtn(rowGo, "+1",  15, DarkBrown, new Color(1,1,1,0.6f));
            ui.plusTenBtn  = HRowBtn(rowGo, "+10", 15, DarkBrown, new Color(1,1,1,0.6f));
            ui.minusTenBtn.onClick.AddListener(() => p.GetComponent<InputAnswerScreenUI>()?.OnMinusTen());
            ui.minusOneBtn.onClick.AddListener(() => p.GetComponent<InputAnswerScreenUI>()?.OnMinusOne());
            ui.plusOneBtn.onClick.AddListener(()  => p.GetComponent<InputAnswerScreenUI>()?.OnPlusOne());
            ui.plusTenBtn.onClick.AddListener(()  => p.GetComponent<InputAnswerScreenUI>()?.OnPlusTen());

            var submitGo = BottomBtn(p, "SubmitBtn", "決定", 18, Color.white, PrimaryYellow, new Vector2(180f, 52f), 14f);
            _primaryImages.Add(submitGo.GetComponent<Image>());
            ui.submitButton = submitGo.GetComponent<Button>();
            ui.submitButton.onClick.AddListener(() => p.GetComponent<InputAnswerScreenUI>()?.OnSubmit());
            EditorUtility.SetDirty(p);
        }

        // ═══════════════════════════════════════════════════════════════════
        // RESULT
        // ═══════════════════════════════════════════════════════════════════
        static void BuildResultContent(GameObject p)
        {
            var ui = p.AddComponent<ResultScreenUI>();
            var vl = VStack(p, 12, 70, 190, 24, 24);

            ui.secretLabel        = VText(vl, "Secret",    "秘密の数字：?", 18, FontStyle.Bold,   DarkBrown,                  36);
            ui.answerLabel        = VText(vl, "Answer",    "答え：?",       18, FontStyle.Normal,  DarkBrown,                  36);
            ui.diffLabel          = VText(vl, "Diff",      "差：?",         15, FontStyle.Normal,  SubBrown,                   28);
            ui.resultMessageLabel = VText(vl, "Msg",       "",              30, FontStyle.Bold,    new Color(0.9f,0.4f,0.1f), 52);
            ui.lemonsLabel        = VText(vl, "Lemons",    "🍋 × ?",       22, FontStyle.Bold,    new Color(0.7f,0.35f,0f),  44);

            // Help-card overlay
            var hcP = Stretch(p, "HelpCardPanel");
            hcP.AddComponent<Image>().color = new Color(0,0,0,0.55f);
            ui.helpCardPanel = hcP; hcP.SetActive(false);
            var hcVl = VStack(hcP, 14, 0, 0, 30, 30);
            hcVl.GetComponent<RectTransform>().anchorMin = new Vector2(0.04f, 0.28f);
            hcVl.GetComponent<RectTransform>().anchorMax = new Vector2(0.96f, 0.72f);
            hcVl.GetComponent<RectTransform>().offsetMin = hcVl.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            VText(hcVl, "HcTitle", "ヘルプカードを使いますか？", 18, FontStyle.Bold, Color.white, 44);
            ui.helpCardInfoText = VText(hcVl, "HcInfo", "", 14, FontStyle.Normal, new Color(0.88f,0.88f,0.88f), 32);
            var hcRow = new GameObject("HcRow"); hcRow.transform.SetParent(hcVl.transform, false);
            hcRow.AddComponent<LayoutElement>().minHeight = 52;
            var hcHG = hcRow.AddComponent<HorizontalLayoutGroup>();
            hcHG.spacing = 12; hcHG.childForceExpandWidth = true; hcHG.childForceExpandHeight = true;
            ui.useHelpCardButton  = HRowBtn(hcRow, "使う",    16, Color.white, new Color(0.2f,0.65f,0.2f));
            ui.skipHelpCardButton = HRowBtn(hcRow, "使わない", 14, Color.white, new Color(0.5f,0.3f,0.1f));
            ui.useHelpCardButton.onClick.AddListener(()  => p.GetComponent<ResultScreenUI>()?.OnUseHelpCard());
            ui.skipHelpCardButton.onClick.AddListener(() => p.GetComponent<ResultScreenUI>()?.OnSkipHelpCard());

            // GameOver confirm overlay
            var goP = Stretch(p, "GameOverConfirmPanel");
            goP.AddComponent<Image>().color = new Color(0,0,0,0.58f);
            ui.gameOverConfirmPanel = goP; goP.SetActive(false);
            var goTxtGo = MakePinnedText(goP, "GoText", "ライフが 0 になりました…",
                20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.52f), new Vector2(0, 0), new Vector2(0, 44));
            goTxtGo.horizontalOverflow = HorizontalWrapMode.Wrap;
            ui.gameOverConfirmText = goTxtGo;
            var goCfmGo = BottomBtn(goP, "GoConfirmBtn", "確認", 17,
                Color.white, new Color(0.82f,0.18f,0.18f), new Vector2(160f,48f), 100f);
            ui.gameOverConfirmButton = goCfmGo.GetComponent<Button>();
            ui.gameOverConfirmButton.onClick.AddListener(() => p.GetComponent<ResultScreenUI>()?.OnConfirmGameOver());

            // Normal next turn
            var nxtGo = BottomBtn(p, "NextTurnBtn", "次のターンへ →", 17, Color.white, PrimaryYellow, new Vector2(210f,52f), 14f);
            _primaryImages.Add(nxtGo.GetComponent<Image>());
            ui.nextTurnButton = nxtGo.GetComponent<Button>();
            ui.nextTurnButton.onClick.AddListener(() => p.GetComponent<ResultScreenUI>()?.OnNextTurn());
            EditorUtility.SetDirty(p);
        }

        // ═══════════════════════════════════════════════════════════════════
        // LAST TURN WARNING
        // ═══════════════════════════════════════════════════════════════════
        static void BuildLastTurnContent(GameObject p)
        {
            var ui = p.AddComponent<LastTurnScreenUI>();
            var vl = VStack(p, 20, 0, 110, 28, 28);

            ui.playerLabel  = VText(vl, "PlayerLabel", "最後のターン！", 28, FontStyle.Bold, DarkBrown, 90);
            ui.playerLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            ui.helpCardText = VText(vl, "HelpCard", "", 17, FontStyle.Normal, SubBrown, 44);

            var okGo = BottomBtn(p, "ProceedBtn", "OK →", 18, Color.white, PrimaryYellow, new Vector2(160f,52f), 14f);
            _primaryImages.Add(okGo.GetComponent<Image>());
            ui.proceedButton = okGo.GetComponent<Button>();
            ui.proceedButton.onClick.AddListener(() => p.GetComponent<LastTurnScreenUI>()?.OnProceed());
            EditorUtility.SetDirty(p);
        }

        // ═══════════════════════════════════════════════════════════════════
        // GAME CLEAR
        // ═══════════════════════════════════════════════════════════════════
        static void BuildGameClearContent(GameObject p)
        {
            var ui = p.AddComponent<GameClearScreenUI>();
            var vl = VStack(p, 20, 0, 130, 28, 28);

            ui.clearMessageText = VText(vl, "ClearMsg", "ゲームクリア！🎉", 34, FontStyle.Bold, new Color(0.1f,0.55f,0.1f), 64);
            ui.lemonsText       = VText(vl, "Lemons",   "残りライフ：🍋 × ?", 22, FontStyle.Bold, new Color(0.7f,0.35f,0f), 52);

            var retryGo = BottomBtn(p, "RetryBtn", "もう一度",  16, DarkBrown,    new Color(1,1,1,0.7f),        new Vector2(160f,48f), 72f);
            var titleGo = BottomBtn(p, "TitleBtn", "タイトルへ", 16, Color.white, new Color(0.55f,0.35f,0.08f), new Vector2(160f,48f), 14f);
            ui.retryButton = retryGo.GetComponent<Button>();
            ui.titleButton = titleGo.GetComponent<Button>();
            ui.retryButton.onClick.AddListener(() => p.GetComponent<GameClearScreenUI>()?.OnRetry());
            ui.titleButton.onClick.AddListener(() => p.GetComponent<GameClearScreenUI>()?.OnTitle());
            EditorUtility.SetDirty(p);
        }

        // ═══════════════════════════════════════════════════════════════════
        // GAME OVER
        // ═══════════════════════════════════════════════════════════════════
        static void BuildGameOverContent(GameObject p)
        {
            var ui = p.AddComponent<GameOverScreenUI>();
            var vl = VStack(p, 16, 0, 130, 28, 28);

            ui.gameOverText   = VText(vl, "GameOver",   "ゲームオーバー…",    30, FontStyle.Bold,   new Color(0.7f,0.1f,0.1f), 60);
            ui.playerNameText = VText(vl, "PlayerName", "",                    18, FontStyle.Normal,  DarkBrown,                 44);
            ui.lemonsText     = VText(vl, "Lemons",     "",                    20, FontStyle.Bold,    new Color(0.7f,0.35f,0f),  44);

            var retryGo = BottomBtn(p, "RetryBtn", "もう一度",  16, DarkBrown,    new Color(1,1,1,0.7f),        new Vector2(160f,48f), 72f);
            var titleGo = BottomBtn(p, "TitleBtn", "タイトルへ", 16, Color.white, new Color(0.55f,0.35f,0.08f), new Vector2(160f,48f), 14f);
            ui.retryButton = retryGo.GetComponent<Button>();
            ui.titleButton = titleGo.GetComponent<Button>();
            ui.retryButton.onClick.AddListener(() => p.GetComponent<GameOverScreenUI>()?.OnRetry());
            ui.titleButton.onClick.AddListener(() => p.GetComponent<GameOverScreenUI>()?.OnTitle());
            EditorUtility.SetDirty(p);
        }

        // ═══════════════════════════════════════════════════════════════════
        // TITLE sheet panel content
        // ═══════════════════════════════════════════════════════════════════
        static void BuildRulesContent(GameObject panel)
        {
            // (ja, en) pairs matching iOS TitleView.rules
            var rules = new (string ja, string en)[]
            {
                ("全員でライフレモンを守るチームゲーム",               "Team game to protect life lemons"),
                ("出番のプレイヤーだけ0〜99の数字を見る",             "Only the active player sees a number 0–99"),
                ("お題に合う答えを言う",                               "Say an answer that matches the topic"),
                ("全員で相談して数字を予想する",                       "Discuss and guess the number together"),
                ("差の分だけライフレモンが減る",                       "Life lemons decrease by the difference"),
                ("ぴったりならライフレモンが増える",                   "Perfect match increases life lemons"),
                ("全員のターンが終わるまでレモンを守りきれば勝利",     "Win by surviving all turns"),
            };

            var (_, contentRT) = MakeScrollView(panel, 110, 16, 16);
            var vlg = contentRT.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(0, 0, 4, 8);

            for (int i = 0; i < rules.Length; i++)
            {
                var (ja, en) = rules[i];

                // Row container (vertical: ja + en stacked)
                var rowGo = new GameObject($"Rule{i + 1}");
                rowGo.transform.SetParent(contentRT, false);
                var rowLE = rowGo.AddComponent<LayoutElement>();
                rowLE.minHeight = 58;
                var rowVL = rowGo.AddComponent<VerticalLayoutGroup>();
                rowVL.childForceExpandWidth = true; rowVL.childForceExpandHeight = false;
                rowVL.padding = new RectOffset(4, 4, 6, 6); rowVL.spacing = 2;

                // Optional: subtle card background for odd rows
                if (i % 2 == 0)
                {
                    var bg = rowGo.AddComponent<Image>();
                    bg.color = new Color(1f, 1f, 1f, 0.25f);
                }

                // Japanese text  ● {ja}
                var jaGo = new GameObject("Ja"); jaGo.transform.SetParent(rowGo.transform, false);
                jaGo.AddComponent<LayoutElement>().minHeight = 28;
                var jaTxt = jaGo.AddComponent<Text>();
                jaTxt.text = $"● {ja}"; jaTxt.fontSize = 15; jaTxt.fontStyle = FontStyle.Bold;
                jaTxt.color = DarkBrown; jaTxt.alignment = TextAnchor.MiddleLeft;
                jaTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
                jaTxt.verticalOverflow   = VerticalWrapMode.Overflow;

                // English text
                var enGo = new GameObject("En"); enGo.transform.SetParent(rowGo.transform, false);
                enGo.AddComponent<LayoutElement>().minHeight = 20;
                var enTxt = enGo.AddComponent<Text>();
                enTxt.text = $"  {en}"; enTxt.fontSize = 11; enTxt.fontStyle = FontStyle.Normal;
                enTxt.color = SubBrown; enTxt.alignment = TextAnchor.MiddleLeft;
                enTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
                enTxt.verticalOverflow   = VerticalWrapMode.Overflow;
            }
        }

        static void BuildTopicContent(GameObject panel)
        {
            // Search field at top
            var sfGo = MakeSearchField(panel, "SearchField", "🔍 お題を検索…");

            // Scroll view for the dynamic topic list
            var (_, contentRT) = MakeScrollView(panel, 116, 16, 0);
            var vlg = contentRT.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(0, 0, 2, 8);

            // + button (floating, bottom-right)
            var addGo = new GameObject("AddButton"); addGo.transform.SetParent(panel.transform, false);
            var addRT = addGo.AddComponent<RectTransform>();
            addRT.anchorMin = addRT.anchorMax = new Vector2(1, 0); addRT.pivot = new Vector2(1, 0);
            addRT.anchoredPosition = new Vector2(-18, 18); addRT.sizeDelta = new Vector2(48, 48);
            var addImg = addGo.AddComponent<Image>(); addImg.color = new Color(0.18f, 0.55f, 0.12f);
            var addBtn = addGo.AddComponent<Button>(); NoNav(addBtn);
            _primaryImages.Add(addImg);
            var addLGo = new GameObject("L"); addLGo.transform.SetParent(addGo.transform, false);
            var addLRT = addLGo.AddComponent<RectTransform>();
            addLRT.anchorMin = Vector2.zero; addLRT.anchorMax = Vector2.one; addLRT.offsetMin = addLRT.offsetMax = Vector2.zero;
            var addTxt = addLGo.AddComponent<Text>(); addTxt.text = "+"; addTxt.fontSize = 28;
            addTxt.fontStyle = FontStyle.Bold; addTxt.color = Color.white; addTxt.alignment = TextAnchor.MiddleCenter;

            // Topic form overlay panel
            var formPanelGo = Stretch(panel, "TopicFormPanel");
            formPanelGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.55f);
            formPanelGo.SetActive(false);
            var formUi = BuildTopicFormContent(formPanelGo);

            // Wire TopicManagerUI
            var mgr = panel.AddComponent<TopicManagerUI>();
            mgr.listContent  = contentRT;
            mgr.searchField  = sfGo.GetComponent<InputField>();
            mgr.addButton    = addBtn;
            mgr.formPanel    = formUi;
            EditorUtility.SetDirty(panel);
        }

        static TopicFormUI BuildTopicFormContent(GameObject formPanel)
        {
            // Card background
            var cardGo = new GameObject("Card"); cardGo.transform.SetParent(formPanel.transform, false);
            var cardRT = cardGo.AddComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.04f, 0.22f); cardRT.anchorMax = new Vector2(0.96f, 0.78f);
            cardRT.offsetMin = cardRT.offsetMax = Vector2.zero;
            cardGo.AddComponent<Image>().color = new Color(1f, 0.97f, 0.70f, 0.98f);

            // Title text
            var titleGo = new GameObject("Title"); titleGo.transform.SetParent(cardGo.transform, false);
            var titleRT = titleGo.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0.5f, 1f); titleRT.anchoredPosition = new Vector2(0, -12);
            titleRT.sizeDelta = new Vector2(-32, 36);
            var titleTxt = titleGo.AddComponent<Text>(); titleTxt.text = "カスタムお題を追加";
            titleTxt.fontSize = 18; titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = DarkBrown; titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Fields VStack
            var vl = new GameObject("Fields"); vl.transform.SetParent(cardGo.transform, false);
            var vlRT = vl.AddComponent<RectTransform>();
            vlRT.anchorMin = Vector2.zero; vlRT.anchorMax = Vector2.one;
            vlRT.offsetMin = new Vector2(16, 56); vlRT.offsetMax = new Vector2(-16, -56);
            var vlg = vl.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            var jpField  = BuildInputFieldRow(vl, "JpField",  "お題（日本語）", 14);
            var enField  = BuildInputFieldRow(vl, "EnField",  "Topic (English)", 13);
            var catDrop  = BuildDropdownRow(vl, "CategoryDrop");

            // Buttons
            var btnRow = new GameObject("BtnRow"); btnRow.transform.SetParent(cardGo.transform, false);
            var btnRT  = btnRow.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0, 0); btnRT.anchorMax = new Vector2(1, 0);
            btnRT.pivot = new Vector2(0.5f, 0); btnRT.anchoredPosition = new Vector2(0, 8);
            btnRT.sizeDelta = new Vector2(-32, 42);
            var hg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hg.spacing = 10; hg.childForceExpandWidth = true; hg.childForceExpandHeight = true;

            var cancelGo = MakePrimaryButton(btnRow, "CancelBtn", "キャンセル", 14, DarkBrown, new Color(1,1,1,0.65f), new Vector2(0,0));
            var saveGo   = MakePrimaryButton(btnRow, "SaveBtn",   "保存",       15, Color.white, new Color(0.18f,0.55f,0.12f), new Vector2(0,0));
            _primaryImages.Add(saveGo.GetComponent<Image>());

            // TopicFormUI component
            var formUi = formPanel.AddComponent<TopicFormUI>();
            formUi.panel            = formPanel;
            formUi.titleText        = titleTxt;
            formUi.jpField          = jpField;
            formUi.enField          = enField;
            formUi.categoryDropdown = catDrop;
            formUi.saveButton       = saveGo.GetComponent<Button>();
            formUi.cancelButton     = cancelGo.GetComponent<Button>();
            EditorUtility.SetDirty(formPanel);
            return formUi;
        }

        static InputField BuildInputFieldRow(GameObject parent, string name, string placeholder, int fontSize)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
            go.AddComponent<LayoutElement>().minHeight = 38;
            go.AddComponent<Image>().color = new Color(1,1,1,0.80f);
            var field = go.AddComponent<InputField>();

            var phGo = new GameObject("Placeholder"); phGo.transform.SetParent(go.transform, false);
            var phRT = phGo.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(6, 2); phRT.offsetMax = new Vector2(-6, -2);
            var phTxt = phGo.AddComponent<Text>(); phTxt.text = placeholder;
            phTxt.fontSize = fontSize; phTxt.color = new Color(0.5f,0.4f,0.2f,0.7f);
            phTxt.alignment = TextAnchor.MiddleLeft;
            phTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            field.placeholder = phTxt;

            var tGo = new GameObject("Text"); tGo.transform.SetParent(go.transform, false);
            var tRT = tGo.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
            tRT.offsetMin = new Vector2(6, 2); tRT.offsetMax = new Vector2(-6, -2);
            var txt = tGo.AddComponent<Text>(); txt.fontSize = fontSize;
            txt.color = DarkBrown; txt.alignment = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            field.textComponent = txt;
            return field;
        }

        static Dropdown BuildDropdownRow(GameObject parent, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
            go.AddComponent<LayoutElement>().minHeight = 38;
            go.AddComponent<Image>().color = new Color(1,1,1,0.80f);
            var dd = go.AddComponent<Dropdown>();

            // Label
            var lblGo = new GameObject("Label"); lblGo.transform.SetParent(go.transform, false);
            var lblRT = lblGo.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = new Vector2(0.85f, 1);
            lblRT.offsetMin = new Vector2(6, 2); lblRT.offsetMax = new Vector2(-4, -2);
            var lblTxt = lblGo.AddComponent<Text>(); lblTxt.text = "必要性 (Necessary)";
            lblTxt.fontSize = 13; lblTxt.color = DarkBrown; lblTxt.alignment = TextAnchor.MiddleLeft;
            dd.captionText = lblTxt;

            // Arrow
            var arrGo = new GameObject("Arrow"); arrGo.transform.SetParent(go.transform, false);
            var arrRT = arrGo.AddComponent<RectTransform>();
            arrRT.anchorMin = new Vector2(0.85f, 0); arrRT.anchorMax = Vector2.one;
            arrRT.offsetMin = arrRT.offsetMax = Vector2.zero;
            var arrTxt = arrGo.AddComponent<Text>(); arrTxt.text = "▼";
            arrTxt.fontSize = 12; arrTxt.color = SubBrown; arrTxt.alignment = TextAnchor.MiddleCenter;

            // Template (minimal, required by Dropdown)
            var tmplGo = new GameObject("Template"); tmplGo.transform.SetParent(go.transform, false);
            var tmplRT = tmplGo.AddComponent<RectTransform>();
            tmplRT.anchorMin = new Vector2(0,0); tmplRT.anchorMax = new Vector2(1,0);
            tmplRT.pivot = new Vector2(0.5f,1); tmplRT.anchoredPosition = Vector2.zero;
            tmplRT.sizeDelta = new Vector2(0, 120);
            var tmplImg = tmplGo.AddComponent<Image>(); tmplImg.color = new Color(1f, 0.97f, 0.70f);
            tmplGo.AddComponent<ScrollRect>().content = tmplRT;
            var itemGo = new GameObject("Item"); itemGo.transform.SetParent(tmplGo.transform, false);
            itemGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 30);
            var tog = itemGo.AddComponent<Toggle>();
            var itmLblGo = new GameObject("Item Label"); itmLblGo.transform.SetParent(itemGo.transform, false);
            var itmRT = itmLblGo.AddComponent<RectTransform>();
            itmRT.anchorMin = Vector2.zero; itmRT.anchorMax = Vector2.one; itmRT.offsetMin = new Vector2(6,0); itmRT.offsetMax = Vector2.zero;
            var itmTxt = itmLblGo.AddComponent<Text>(); itmTxt.fontSize = 13; itmTxt.color = DarkBrown; itmTxt.alignment = TextAnchor.MiddleLeft;
            dd.itemText = itmTxt;
            dd.template = tmplRT;
            tmplGo.SetActive(false);

            dd.targetGraphic = go.GetComponent<Image>();
            return dd;
        }

        static GameObject MakeSearchField(GameObject parent, string name, string placeholder)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -108); rt.sizeDelta = new Vector2(-32, 36);
            go.AddComponent<Image>().color = new Color(1,1,1,0.70f);
            var field = go.AddComponent<InputField>();

            var phGo = new GameObject("Placeholder"); phGo.transform.SetParent(go.transform, false);
            var phRT = phGo.AddComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(8, 2); phRT.offsetMax = new Vector2(-8, -2);
            var phTxt = phGo.AddComponent<Text>(); phTxt.text = placeholder;
            phTxt.fontSize = 13; phTxt.color = new Color(0.55f,0.42f,0.18f,0.8f);
            phTxt.alignment = TextAnchor.MiddleLeft;
            phTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            field.placeholder = phTxt;

            var tGo = new GameObject("Text"); tGo.transform.SetParent(go.transform, false);
            var tRT = tGo.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
            tRT.offsetMin = new Vector2(8, 2); tRT.offsetMax = new Vector2(-8, -2);
            var txt = tGo.AddComponent<Text>(); txt.fontSize = 13;
            txt.color = DarkBrown; txt.alignment = TextAnchor.MiddleLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            field.textComponent = txt;
            return go;
        }

        // ═══════════════════════════════════════════════════════════════════
        // Layout helpers
        // ═══════════════════════════════════════════════════════════════════

        static GameObject VStack(GameObject p, float spacing, float topPad, float bottomPad, float lPad, float rPad)
        {
            var go = new GameObject("VStack"); go.transform.SetParent(p.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(lPad, bottomPad); rt.offsetMax = new Vector2(-rPad, -topPad);
            var vl = go.AddComponent<VerticalLayoutGroup>();
            vl.spacing = spacing; vl.childAlignment = TextAnchor.MiddleCenter;
            vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
            vl.padding = new RectOffset(0,0,8,8);
            return go;
        }

        static Text VText(GameObject vstack, string name, string text,
            int size, FontStyle style, Color color, float minH)
        {
            var go = new GameObject(name); go.transform.SetParent(vstack.transform, false);
            go.AddComponent<LayoutElement>().minHeight = minH;
            var t = go.AddComponent<Text>();
            t.text = text; t.fontSize = size; t.fontStyle = style;
            t.color = color; t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            return t;
        }

        // Pinned text (anchor + position + size given explicitly)
        static Text MakePinnedText(GameObject p, string name, string text,
            int size, FontStyle style, Color color, TextAnchor align,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject(name); go.transform.SetParent(p.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos; rt.sizeDelta = sizeDelta;
            var t = go.AddComponent<Text>();
            t.text = text; t.fontSize = size; t.fontStyle = style;
            t.color = color; t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            return t;
        }

        // Button anchored at bottom with specified offset from bottom
        static GameObject BottomBtn(GameObject p, string name, string label,
            int size, Color textColor, Color bgColor, Vector2 sz, float fromBottom)
        {
            var go = MakePrimaryButton(p, name, label, size, textColor, bgColor, sz);
            Pin(go, new Vector2(0.5f,0f), new Vector2(0.5f,0f), new Vector2(0.5f,0f),
                new Vector2(0, fromBottom), sz);
            return go;
        }

        static GameObject MakePrimaryButton(GameObject parent, string name, string label,
            int size, Color textColor, Color bgColor, Vector2 sz)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>(); rt.sizeDelta = sz;
            var img = go.AddComponent<Image>(); img.color = bgColor;
            var btn = go.AddComponent<Button>(); NoNav(btn);
            var lblGo = new GameObject("L"); lblGo.transform.SetParent(go.transform, false);
            var lRT = lblGo.AddComponent<RectTransform>();
            lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
            lRT.offsetMin = new Vector2(4,4); lRT.offsetMax = new Vector2(-4,-4);
            var t = lblGo.AddComponent<Text>();
            t.text = label; t.fontSize = size; t.fontStyle = FontStyle.Bold;
            t.color = textColor; t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return go;
        }

        static Button HRowBtn(GameObject parent, string label, int size, Color textColor, Color bgColor)
        {
            var go = new GameObject(label); go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>().color = bgColor;
            var btn = go.AddComponent<Button>(); NoNav(btn);
            var lGo = new GameObject("L"); lGo.transform.SetParent(go.transform, false);
            var lRT = lGo.AddComponent<RectTransform>();
            lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one; lRT.offsetMin = lRT.offsetMax = Vector2.zero;
            var t = lGo.AddComponent<Text>();
            t.text = label; t.fontSize = size; t.fontStyle = FontStyle.Bold;
            t.color = textColor; t.alignment = TextAnchor.MiddleCenter;
            return btn;
        }

        static void Pin(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 sz)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = sz;
        }

        static GameObject MakeFullWidthFromTop(GameObject p, string name, float yFromTop, float height)
        {
            var go = new GameObject(name); go.transform.SetParent(p.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(1,1);
            rt.pivot = new Vector2(0.5f,1f);
            rt.anchoredPosition = new Vector2(0,-yFromTop); rt.sizeDelta = new Vector2(0, height);
            return go;
        }

        static (GameObject scroll, RectTransform contentRT) MakeScrollView(
            GameObject parent, float topOffset, float bottomOffset, float hPad)
        {
            var scrollGo = new GameObject("ScrollView"); scrollGo.transform.SetParent(parent.transform, false);
            var sRT = scrollGo.AddComponent<RectTransform>();
            sRT.anchorMin = Vector2.zero; sRT.anchorMax = Vector2.one;
            sRT.offsetMin = new Vector2(hPad, bottomOffset); sRT.offsetMax = new Vector2(-hPad, -topOffset);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;

            var vpGo = new GameObject("Viewport"); vpGo.transform.SetParent(scrollGo.transform, false);
            var vpRT = vpGo.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one; vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
            vpGo.AddComponent<Image>().color = Color.clear;
            vpGo.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vpRT;

            var cGo = new GameObject("Content"); cGo.transform.SetParent(vpGo.transform, false);
            var cRT = cGo.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0,1); cRT.anchorMax = new Vector2(1,1);
            cRT.pivot = new Vector2(0.5f,1f); cRT.anchoredPosition = Vector2.zero; cRT.sizeDelta = Vector2.zero;
            var csf = cGo.AddComponent<ContentSizeFitter>(); csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = cRT;
            return (scrollGo, cRT);
        }

        // ═══════════════════════════════════════════════════════════════════
        // Original Title panel helpers (unchanged)
        // ═══════════════════════════════════════════════════════════════════

        static GameObject Stretch(GameObject parent, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        static GameObject AnchorImage(GameObject parent, string name,
            string spritePath, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f,0.5f);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            var spr = LoadSprite(spritePath); if (spr) img.sprite = spr;
            img.preserveAspect = true; img.raycastTarget = false;
            return go;
        }

        static GameObject AnchorSpriteButton(GameObject parent, string name,
            string spritePath, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f,0.5f);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = size;
            var img = go.AddComponent<Image>(); var spr = LoadSprite(spritePath); if (spr) img.sprite = spr;
            img.preserveAspect = true;
            var btn = go.AddComponent<Button>(); NoNav(btn);
            return go;
        }

        static GameObject AnchorText(GameObject parent, string name, string text,
            int fontSize, FontStyle style, Color color, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f,0.5f);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = size;
            var txt = go.AddComponent<Text>();
            txt.text = text; txt.fontSize = fontSize; txt.fontStyle = style;
            txt.color = color; txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow; txt.verticalOverflow = VerticalWrapMode.Overflow;
            return go;
        }

        static GameObject CapsuleButton(GameObject parent, string name,
            string label, int fontSize, Color textColor, Color bgColor, Vector2 size)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f,0.5f); rt.pivot = new Vector2(0.5f,0.5f); rt.sizeDelta = size;
            var img = go.AddComponent<Image>(); img.sprite = CapsuleSprite(); img.type = Image.Type.Sliced; img.color = bgColor;
            var btn = go.AddComponent<Button>(); NoNav(btn);
            var lGo = new GameObject("Label"); lGo.transform.SetParent(go.transform, false);
            var lRT = lGo.AddComponent<RectTransform>();
            lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
            lRT.offsetMin = new Vector2(10,7); lRT.offsetMax = new Vector2(-10,-7);
            var txt = lGo.AddComponent<Text>();
            txt.text = label; txt.fontSize = fontSize; txt.fontStyle = FontStyle.Bold;
            txt.color = textColor; txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow; txt.verticalOverflow = VerticalWrapMode.Overflow;
            return go;
        }

        static GameObject SheetPanel(GameObject parent, string name, string title,
            System.Action<GameObject> buildContent)
        {
            var go = Stretch(parent, name);
            var bg = go.AddComponent<Image>(); bg.color = new Color(1f,0.96f,0.60f,0.97f); bg.raycastTarget = true;
            go.AddComponent<SheetPanelActivator>();

            var titleGo = new GameObject("PanelTitle"); titleGo.transform.SetParent(go.transform, false);
            var titleRT = titleGo.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0,1); titleRT.anchorMax = new Vector2(1,1); titleRT.pivot = new Vector2(0.5f,1f);
            titleRT.offsetMin = new Vector2(24,-(40+60)); titleRT.offsetMax = new Vector2(-60,-40);
            var titleTxt = titleGo.AddComponent<Text>();
            titleTxt.text = title; titleTxt.fontSize = 26; titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = DarkBrown; titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;

            var closeGo = new GameObject("CloseButton"); closeGo.transform.SetParent(go.transform, false);
            var closeRT = closeGo.AddComponent<RectTransform>();
            closeRT.anchorMin = closeRT.anchorMax = new Vector2(1,1); closeRT.pivot = new Vector2(1,1);
            closeRT.anchoredPosition = new Vector2(-16,-40); closeRT.sizeDelta = new Vector2(44,44);
            closeGo.AddComponent<Image>().color = Color.clear;
            var closeBtn = closeGo.AddComponent<Button>(); NoNav(closeBtn);
            var xGo = new GameObject("X"); xGo.transform.SetParent(closeGo.transform, false);
            var xRT = xGo.AddComponent<RectTransform>();
            xRT.anchorMin = Vector2.zero; xRT.anchorMax = Vector2.one; xRT.offsetMin = xRT.offsetMax = Vector2.zero;
            var xTxt = xGo.AddComponent<Text>(); xTxt.text = "×"; xTxt.fontSize = 24;
            xTxt.color = SubBrown; xTxt.alignment = TextAnchor.MiddleCenter;

            buildContent?.Invoke(go);
            return go;
        }

        static Slider BuildSlider(GameObject go)
        {
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 99; slider.value = 50; slider.wholeNumbers = true;

            var bgGo = new GameObject("Bg"); bgGo.transform.SetParent(go.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0,0.25f); bgRT.anchorMax = new Vector2(1,0.75f); bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>(); bgImg.color = new Color(0.78f,0.78f,0.78f); slider.targetGraphic = bgImg;

            var fillArea = new GameObject("FillArea"); fillArea.transform.SetParent(go.transform, false);
            var faRT = fillArea.AddComponent<RectTransform>();
            faRT.anchorMin = new Vector2(0,0.25f); faRT.anchorMax = new Vector2(1,0.75f);
            faRT.offsetMin = new Vector2(5,0); faRT.offsetMax = new Vector2(-5,0);
            var fillGo = new GameObject("Fill"); fillGo.transform.SetParent(fillArea.transform, false);
            var fillRT = fillGo.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one; fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
            fillGo.AddComponent<Image>().color = PrimaryYellow;
            slider.fillRect = fillRT;

            var handleArea = new GameObject("HandleArea"); handleArea.transform.SetParent(go.transform, false);
            var haRT = handleArea.AddComponent<RectTransform>();
            haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
            haRT.offsetMin = new Vector2(10,0); haRT.offsetMax = new Vector2(-10,0);
            var handleGo = new GameObject("Handle"); handleGo.transform.SetParent(handleArea.transform, false);
            var hRT = handleGo.AddComponent<RectTransform>();
            hRT.anchorMin = hRT.anchorMax = new Vector2(0.5f,0.5f); hRT.pivot = new Vector2(0.5f,0.5f); hRT.sizeDelta = new Vector2(26,26);
            handleGo.AddComponent<Image>().color = DarkBrown;
            slider.handleRect = hRT;
            return slider;
        }

        // ── Lemon prefab ──────────────────────────────────────────────────
        static GameObject EnsureLemonPrefab()
        {
            const string path = "Assets/Prefabs/RainingLemonItem.prefab";
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            var existing = Load<GameObject>(path); if (existing) AssetDatabase.DeleteAsset(path);
            var go = new GameObject("RainingLemonItem");
            var rt = go.AddComponent<RectTransform>(); rt.sizeDelta = new Vector2(50,50);
            var img = go.AddComponent<Image>();
            var spr = LoadSprite("Assets/Sprites/Title_Lemon.png");
            if (spr) { img.sprite = spr; img.preserveAspect = true; }
            img.raycastTarget = false;
            go.AddComponent<RainingLemonItem>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go); AssetDatabase.Refresh();
            return prefab;
        }

        // ── Input System fix ──────────────────────────────────────────────
        internal static void FixActiveInputHandler()
        {
            const string path = "ProjectSettings/ProjectSettings.asset";
            if (!System.IO.File.Exists(path)) return;
            var text = System.IO.File.ReadAllText(path);
            if (text.Contains("m_ActiveInputHandler: -1"))
            {
                System.IO.File.WriteAllText(path, text.Replace("m_ActiveInputHandler: -1", "m_ActiveInputHandler: 1"));
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
                        prop.intValue = 1; so.ApplyModifiedPropertiesWithoutUndo();
                        Debug.Log("[BOMBOMLemon] Fixed activeInputHandler -1 → 1 in memory");
                    }
                }
            }
            catch { }
        }

        // ── EventSystem ───────────────────────────────────────────────────
        static void EnsureEventSystem(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var es = root.GetComponent<EventSystem>(); if (es == null) continue;
                var old = root.GetComponent<StandaloneInputModule>(); if (old) Object.DestroyImmediate(old);
                if (!root.GetComponent<InputSystemUIInputModule>()) root.AddComponent<InputSystemUIInputModule>();
                break;
            }
        }

        // ── SerializedObject array helper ─────────────────────────────────
        static void SetImageArray(SerializedObject so, string propName, List<Image> images)
        {
            var prop = so.FindProperty(propName);
            if (prop == null) return;
            prop.arraySize = images.Count;
            for (int i = 0; i < images.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = images[i];
        }

        // ── Asset loaders ─────────────────────────────────────────────────
        static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        static Sprite LoadSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter ti && ti.textureType != TextureImporterType.Sprite)
            {
                ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Sprite CapsuleSprite() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        static Sprite KnobSprite()    => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        static void NoNav(Button btn) { var n = btn.navigation; n.mode = Navigation.Mode.None; btn.navigation = n; }
    }
}
