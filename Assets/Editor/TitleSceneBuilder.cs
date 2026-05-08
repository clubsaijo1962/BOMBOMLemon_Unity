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
            EditorApplication.update += OnFirstUpdate;
        }

        static void OnFirstUpdate()
        {
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
        // ── Reference resolution = iPhone logical points (matches SwiftUI geo.size) ──
        const float W = 390f, H = 844f;

        // ── iOS colour palette (from GameModel / TitleView) ────────────────
        static readonly Color BgYellow  = new Color(1.00f, 0.96f, 0.60f);
        static readonly Color DarkBrown = new Color(0.15f, 0.10f, 0.00f);
        static readonly Color SubBrown  = new Color(0.50f, 0.36f, 0.00f);
        static readonly Color BtnFg     = new Color(0.45f, 0.30f, 0.00f); // icon + text
        static readonly Color BtnCap    = new Color(1f, 1f, 1f, 0.65f);   // capsule bg
        static readonly Color LimeGreen = new Color(0.25f, 0.60f, 0.08f);

        [MenuItem("BOMBOMLemon/Build Title Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity");

            // Clear everything except Camera & EventSystem
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<Camera>()      != null) continue;
                if (root.GetComponent<EventSystem>() != null) continue;
                Object.DestroyImmediate(root);
            }
            EnsureEventSystem(scene);

            // ── Persistent managers ──────────────────────────────────────────
            new GameObject("GameManager").AddComponent<GameManager>();

            var smGo    = new GameObject("SoundManager");
            var bgmSrc  = smGo.AddComponent<AudioSource>(); bgmSrc.playOnAwake = false;
            var seSrc   = smGo.AddComponent<AudioSource>(); seSrc.playOnAwake  = false;
            var sm      = smGo.AddComponent<SoundManager>();
            var smSO    = new SerializedObject(sm);
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

            // ── Canvas (reference resolution matches iPhone logical points) ──
            var canvasGo = new GameObject("Canvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = canvasGo.AddComponent<CanvasScaler>();
            cs.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(W, H);
            cs.matchWidthOrHeight  = 0f;   // width-based scaling for portrait
            canvasGo.AddComponent<GraphicRaycaster>();
            canvasGo.AddComponent<JPFontLoader>();

            // ── Background ───────────────────────────────────────────────────
            var bgGo = Stretch(canvasGo, "Background");
            bgGo.AddComponent<Image>().color = BgYellow;

            // ── Lemon rain layer (fullscreen, rendered first = behind) ────────
            var rainParent = Stretch(canvasGo, "LemonRainParent");

            // ════════════════════════════════════════════════════════════════
            // LOGO GROUP
            // SwiftUI: ZStack at .position(x:cx, y: h*0.42)
            //   frame(width:w, height:320)
            //
            // In Unity (y up, anchor y = 1 - 0.42 = 0.58 from bottom):
            //   TitleBubble  →  anchor y = 0.580,  offset y = +0  (ZStack centre)
            //   TitleLemon   →  anchor y = 0.663,  offset y = +70 (-w*0.18 up in SwiftUI)
            //   TitleWord    →  anchor y = 0.524,  offset y = -47 (+w*0.12 down in SwiftUI)
            // ════════════════════════════════════════════════════════════════
            const float zy = 1f - 0.42f;  // 0.58

            // Image sizes derived from actual pixel dimensions + iOS frame:
            //   Bubble : 676×394 → iOS width=w*0.99=386 → height=386/1.716=225
            //   Lemon  : 1126×944 → iOS frame 236×236 (square with scaledToFit)
            //   Word   : 707×165 → iOS width=w*0.85=332 → height=332/4.285=78
            var bubbleGo = AnchorImage(canvasGo, "TitleBubble",
                "Assets/Sprites/Title_Bubble.png",
                new Vector2(0.5f, zy),                 // anchor
                new Vector2(386f, 225f));               // size

            var lemonGo = AnchorImage(canvasGo, "TitleLemon",
                "Assets/Sprites/Title_Lemon.png",
                new Vector2(0.5f, zy + 70f / H),      // +70 pt upward
                new Vector2(236f, 236f));

            var wordGo = AnchorImage(canvasGo, "TitleWord",
                "Assets/Sprites/Title_Word.png",
                new Vector2(0.5f, zy - 47f / H),      // -47 pt downward
                new Vector2(332f, 78f));

            // ════════════════════════════════════════════════════════════════
            // CONTENT GROUP  (start button + info text)
            // SwiftUI: VStack centred at .position(y: h*0.85)
            //   = anchor y 0.15 from bottom
            //
            // VStack total height ≈ 179 pt  →  half = 89.5 pt
            // StartButton  centre from bottom: (127 + 89.5) − 53.5 = 163  → y=0.193
            // InfoTextJa   centre from bottom: 163 − 53.5 − 4 − 8.5  =  97  → y=0.115
            // InfoTextEn   centre from bottom:  97 −  8.5 − 1 − 7    =  80.5 → y=0.095
            // HellInfoJa   centre from bottom:  80.5 − 7 − 1 − 10   =  62.5 → y=0.074
            // HellInfoEn   centre from bottom:  62.5 − 10 − 1 − 7   =  44.5 → y=0.053
            // ════════════════════════════════════════════════════════════════
            var contentGo = Stretch(canvasGo, "ContentGroup");
            var contentCG = contentGo.AddComponent<CanvasGroup>();

            // start.png: 1238×471 → iOS width=w*0.72=281 → height=281/2.629=107
            var startBtnGo = AnchorSpriteButton(contentGo, "StartButton",
                "Assets/Sprites/start.png",
                new Vector2(0.5f, 0.193f),
                new Vector2(281f, 107f));

            var infoJaGo = AnchorText(contentGo, "InfoTextJa",
                "2〜24人のパーティーゲーム", 14, FontStyle.Bold, DarkBrown,
                new Vector2(0.5f, 0.115f), new Vector2(360f, 18f));

            var infoEnGo = AnchorText(contentGo, "InfoTextEn",
                "A party game for 2–24 players", 11, FontStyle.Normal, SubBrown,
                new Vector2(0.5f, 0.095f), new Vector2(360f, 14f));

            var hellJaGo = AnchorText(contentGo, "HellModeInfoJa",
                "", 12, FontStyle.Bold, LimeGreen,
                new Vector2(0.5f, 0.074f), new Vector2(360f, 20f));

            var hellEnGo = AnchorText(contentGo, "HellModeInfoEn",
                "", 10, FontStyle.Normal, LimeGreen,
                new Vector2(0.5f, 0.053f), new Vector2(360f, 14f));

            // ════════════════════════════════════════════════════════════════
            // TOP CONTROLS OVERLAY
            // SwiftUI: VStack { HStack { leftBtns   Spacer   hellBtn } Spacer }
            //   .padding(.top, 54)  .padding(.leading/trailing, 18)
            // In Unity:  anchor to top edge, anchoredPosition y = -54
            // ════════════════════════════════════════════════════════════════
            var topOverlay = Stretch(canvasGo, "TopOverlay");

            // ── Left buttons (HStack spacing:8) ─────────────────────────────
            // anchor top-left, pivot top-left
            var leftGroup = new GameObject("LeftButtons");
            leftGroup.transform.SetParent(topOverlay.transform, false);
            var lgRT = leftGroup.AddComponent<RectTransform>();
            lgRT.anchorMin        = new Vector2(0f, 1f);
            lgRT.anchorMax        = new Vector2(0f, 1f);
            lgRT.pivot            = new Vector2(0f, 1f);
            lgRT.anchoredPosition = new Vector2(18f, -54f);
            lgRT.sizeDelta        = new Vector2(148f, 28f);
            var hLayout = leftGroup.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing              = 8f;
            hLayout.childForceExpandWidth  = false;
            hLayout.childForceExpandHeight = false;
            hLayout.childAlignment       = TextAnchor.MiddleLeft;

            // "？ ルール" button  (width 72, height 28)
            var rulesBtnGo = CapsuleButton(leftGroup, "RulesButton",
                "？ ルール", 11, BtnFg, BtnCap, new Vector2(72f, 28f));

            // "≡ お題" button  (width 60, height 28)
            var topicBtnGo = CapsuleButton(leftGroup, "TopicButton",
                "≡ お題", 11, BtnFg, BtnCap, new Vector2(60f, 28f));

            // ── 地獄モード toggle (top-right) ─────────────────────────────
            // anchor top-right, pivot top-right
            var hellBtnGo = new GameObject("HellModeButton");
            hellBtnGo.transform.SetParent(topOverlay.transform, false);
            var hellRT = hellBtnGo.AddComponent<RectTransform>();
            hellRT.anchorMin        = new Vector2(1f, 1f);
            hellRT.anchorMax        = new Vector2(1f, 1f);
            hellRT.pivot            = new Vector2(1f, 1f);
            hellRT.anchoredPosition = new Vector2(-18f, -54f);
            hellRT.sizeDelta        = new Vector2(105f, 28f);
            var hellBtnImg = hellBtnGo.AddComponent<Image>();
            hellBtnImg.sprite    = CapsuleSprite();
            hellBtnImg.type      = Image.Type.Sliced;
            hellBtnImg.color     = BtnCap;
            var hellBtn = hellBtnGo.AddComponent<Button>();
            NoNav(hellBtn);

            // Dot indicator  (8×8, circle via Knob sprite)
            var dotGo = new GameObject("HellModeIndicator");
            dotGo.transform.SetParent(hellBtnGo.transform, false);
            var dotRT = dotGo.AddComponent<RectTransform>();
            dotRT.anchorMin = dotRT.anchorMax = new Vector2(0.5f, 0.5f);
            dotRT.pivot            = new Vector2(0.5f, 0.5f);
            dotRT.anchoredPosition = new Vector2(-37f, 0f);
            dotRT.sizeDelta        = new Vector2(8f, 8f);
            var dotImg = dotGo.AddComponent<Image>();
            dotImg.sprite = KnobSprite();
            dotImg.color  = new Color(0.75f, 0.75f, 0.75f);

            // Label
            var hellLblGo = new GameObject("HellModeLabel");
            hellLblGo.transform.SetParent(hellBtnGo.transform, false);
            var hellLblRT = hellLblGo.AddComponent<RectTransform>();
            hellLblRT.anchorMin = hellLblRT.anchorMax = new Vector2(0.5f, 0.5f);
            hellLblRT.pivot            = new Vector2(0.5f, 0.5f);
            hellLblRT.anchoredPosition = new Vector2(7f, 0f);
            hellLblRT.sizeDelta        = new Vector2(70f, 20f);
            var hellLbl = hellLblGo.AddComponent<Text>();
            hellLbl.text      = "地獄モード";
            hellLbl.fontSize  = 11;
            hellLbl.fontStyle = FontStyle.Bold;
            hellLbl.color     = new Color(0.40f, 0.30f, 0.05f);
            hellLbl.alignment = TextAnchor.MiddleLeft;

            // ── Overlay sheet panels (hidden) ────────────────────────────────
            var rulesPanel = SheetPanel(canvasGo, "RulesPanel",    "ルール",    BuildRulesContent);
            var topicPanel = SheetPanel(canvasGo, "TopicPanel",    "お題",      null);
            rulesPanel.SetActive(false);
            topicPanel.SetActive(false);

            // ── Lemon prefab ─────────────────────────────────────────────────
            var lemonPrefab = EnsureLemonPrefab();

            // ── TitleScreenUI ────────────────────────────────────────────────
            var uiHolder = new GameObject("TitleScreenUI");
            uiHolder.transform.SetParent(canvasGo.transform, false);
            uiHolder.AddComponent<RectTransform>(); // needed for scene serialisation
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
            ui.topicManagerPanel = topicPanel;
            ui.rainingLemonPrefab= lemonPrefab;
            ui.lemonRainParent   = rainParent.GetComponent<RectTransform>();

            EditorUtility.SetDirty(uiHolder);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BOMBOMLemon] Title Scene built successfully.");
        }

        // ════════════════════════════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════════════════════════════

        // Full-stretch child panel
        static GameObject Stretch(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        // Image pinned to a proportional anchor (anchoredPosition = 0,0 = resting pos for animations)
        static GameObject AnchorImage(GameObject parent, string name,
            string spritePath, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = size;
            var img = go.AddComponent<Image>();
            var spr = LoadSprite(spritePath);
            if (spr != null) img.sprite = spr;
            img.preserveAspect = true;
            img.raycastTarget  = false;
            return go;
        }

        // Sprite-based button pinned to a proportional anchor
        static GameObject AnchorSpriteButton(GameObject parent, string name,
            string spritePath, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = size;
            var img = go.AddComponent<Image>();
            var spr = LoadSprite(spritePath);
            if (spr != null) img.sprite = spr;
            img.preserveAspect = true;
            var btn = go.AddComponent<Button>();
            NoNav(btn);
            return go;
        }

        // Text at a proportional anchor
        static GameObject AnchorText(GameObject parent, string name,
            string text, int fontSize, FontStyle style, Color color,
            Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = size;
            var txt = go.AddComponent<Text>();
            txt.text      = text;
            txt.fontSize  = fontSize;
            txt.fontStyle = style;
            txt.color     = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;
            return go;
        }

        // Capsule pill button (for top nav: ルール / お題)
        // Uses Unity's built-in UISprite (rounded rect) as background
        static GameObject CapsuleButton(GameObject parent, string name,
            string label, int fontSize, Color textColor, Color bgColor, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.sprite = CapsuleSprite();
            img.type   = Image.Type.Sliced;
            img.color  = bgColor;

            var btn = go.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor      = bgColor;
            cols.highlightedColor = new Color(bgColor.r * 1.05f, bgColor.g * 1.05f, bgColor.b * 1.05f, bgColor.a);
            cols.pressedColor     = new Color(bgColor.r * 0.85f, bgColor.g * 0.85f, bgColor.b * 0.85f, bgColor.a);
            btn.colors = cols;
            NoNav(btn);

            // Label child (stretch with padding 10H 7V)
            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lblRT = lblGo.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = new Vector2(10f,  7f);
            lblRT.offsetMax = new Vector2(-10f, -7f);
            var txt = lblGo.AddComponent<Text>();
            txt.text      = label;
            txt.fontSize  = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.color     = textColor;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;
            return go;
        }

        // Simple full-screen sheet panel with title + close button
        static GameObject SheetPanel(GameObject parent, string name, string title,
            System.Action<GameObject> buildContent)
        {
            var go = Stretch(parent, name);
            var bg = go.AddComponent<Image>();
            bg.color         = new Color(1.00f, 0.96f, 0.60f, 0.97f);
            bg.raycastTarget = true;

            // Title bar
            var titleGo = new GameObject("PanelTitle");
            titleGo.transform.SetParent(go.transform, false);
            var titleRT = titleGo.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 1f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.pivot     = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -40f);
            titleRT.sizeDelta        = new Vector2(0f, 60f);
            // Padding: left 24pt, right 60pt (space for close button), keep height via offsetMin/Max Y
            // With anchor top-edge (anchorMin.y=anchorMax.y=1):
            //   offsetMax.y = distance from top anchor to top edge = -(top padding)
            //   offsetMin.y = distance from top anchor to bottom edge = -(top padding + height)
            titleRT.offsetMin = new Vector2(24f,   -(40f + 60f));  // left=24, bottom at 100pt below top
            titleRT.offsetMax = new Vector2(-60f,  -40f);           // right=60, top at 40pt below top

            var titleTxt = titleGo.AddComponent<Text>();
            titleTxt.text      = title;
            titleTxt.fontSize  = 26;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color     = DarkBrown;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Close button (×) top-right
            var closeGo = new GameObject("CloseButton");
            closeGo.transform.SetParent(go.transform, false);
            var closeRT = closeGo.AddComponent<RectTransform>();
            closeRT.anchorMin = closeRT.anchorMax = new Vector2(1f, 1f);
            closeRT.pivot     = new Vector2(1f, 1f);
            closeRT.anchoredPosition = new Vector2(-16f, -40f);
            closeRT.sizeDelta        = new Vector2(44f, 44f);
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = Color.clear;
            var closeBtn = closeGo.AddComponent<Button>();
            NoNav(closeBtn);
            // onClick listener is wired at runtime by TitleScreenUI.WireCloseButton()

            var closeXGo = new GameObject("X");
            closeXGo.transform.SetParent(closeGo.transform, false);
            var closeXRT = closeXGo.AddComponent<RectTransform>();
            closeXRT.anchorMin = Vector2.zero;
            closeXRT.anchorMax = Vector2.one;
            closeXRT.offsetMin = closeXRT.offsetMax = Vector2.zero;
            var closeX = closeXGo.AddComponent<Text>();
            closeX.text      = "×";
            closeX.fontSize  = 24;
            closeX.color     = SubBrown;
            closeX.alignment = TextAnchor.MiddleCenter;

            buildContent?.Invoke(go);
            return go;
        }

        // Builds the rules text inside the rules panel
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
            bodyRT.anchorMin = Vector2.zero;
            bodyRT.anchorMax = Vector2.one;
            bodyRT.offsetMin = new Vector2(24f, 80f);
            bodyRT.offsetMax = new Vector2(-24f, -110f);
            var vl = bodyGo.AddComponent<VerticalLayoutGroup>();
            vl.spacing              = 14f;
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;

            foreach (var rule in rules)
            {
                var rGo = new GameObject("Rule");
                rGo.transform.SetParent(bodyGo.transform, false);
                var rRT = rGo.AddComponent<RectTransform>();
                rRT.sizeDelta = new Vector2(0f, 32f);
                var le = rGo.AddComponent<LayoutElement>();
                le.minHeight = 32f;
                var txt = rGo.AddComponent<Text>();
                txt.text      = "● " + rule;
                txt.fontSize  = 15;
                txt.fontStyle = FontStyle.Bold;
                txt.color     = DarkBrown;
                txt.alignment = TextAnchor.MiddleLeft;
                txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                txt.verticalOverflow   = VerticalWrapMode.Overflow;
            }
        }

        // ── Lemon prefab ─────────────────────────────────────────────────────
        static GameObject EnsureLemonPrefab()
        {
            const string path = "Assets/Prefabs/RainingLemonItem.prefab";
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            // Always recreate so sprite is current
            var existing = Load<GameObject>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            var go  = new GameObject("RainingLemonItem");
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(50f, 50f);
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

        // ── EventSystem: switch to InputSystem module ─────────────────────
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

        // ── Asset loaders ─────────────────────────────────────────────────
        static T Load<T>(string path) where T : Object
            => AssetDatabase.LoadAssetAtPath<T>(path);

        static Sprite LoadSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter ti &&
                ti.textureType != TextureImporterType.Sprite)
            {
                ti.textureType    = TextureImporterType.Sprite;
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
            var n = btn.navigation;
            n.mode = Navigation.Mode.None;
            btn.navigation = n;
        }
    }
}
