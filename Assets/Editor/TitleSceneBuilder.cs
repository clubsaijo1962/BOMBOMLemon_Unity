using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace BOMBOMLemon.Editor
{
    public static class TitleSceneBuilder
    {
        const float W = 1080f, H = 1920f;

        static readonly Color BgColor   = new Color(1.00f, 0.96f, 0.60f);
        static readonly Color Dark      = new Color(0.15f, 0.10f, 0.00f);
        static readonly Color Sub       = new Color(0.50f, 0.36f, 0.00f);
        static readonly Color Lime      = new Color(0.25f, 0.60f, 0.08f);
        static readonly Color BtnYellow = new Color(1.00f, 0.88f, 0.20f);

        [MenuItem("BOMBOMLemon/Build Title Scene")]
        public static void BuildTitleScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity");

            // Clear root objects (keep Camera & EventSystem)
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<Camera>()      != null) continue;
                if (root.GetComponent<EventSystem>() != null) continue;
                Object.DestroyImmediate(root);
            }

            FixEventSystem(scene);

            var lemonPrefab = EnsureRainingLemonPrefab();

            // ── Persistent Managers ───────────────────────────────────────────
            var gmGo = new GameObject("GameManager");
            gmGo.AddComponent<GameManager>();

            var smGo   = new GameObject("SoundManager");
            var bgmSrc = smGo.AddComponent<AudioSource>();
            bgmSrc.playOnAwake = false;
            var seSrc  = smGo.AddComponent<AudioSource>();
            seSrc.playOnAwake  = false;
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
            cs.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(W, H);
            cs.matchWidthOrHeight  = 0f; // portrait: scale by width
            canvasGo.AddComponent<GraphicRaycaster>();

            // ── Background ────────────────────────────────────────────────────
            var bgImg = MakeStretchImage(canvasGo, "Background", null);
            bgImg.color = BgColor;

            // ── Lemon Rain layer (fullscreen, behind content) ─────────────────
            var rainParent = MakeStretchPanel(canvasGo, "LemonRainParent");

            // ── Safe-area container (respects iPhone notch / Android cutout) ────
            var safeAreaGo = MakeStretchPanel(canvasGo, "SafeArea");
            safeAreaGo.AddComponent<SafeAreaFitter>();

            // ── Content group (CanvasGroup for fade-in) ───────────────────────
            var content = MakeStretchPanel(safeAreaGo, "Content");
            content.AddComponent<CanvasGroup>();

            // ── Logo images (lemon on top of bubble/text) ─────────────────────
            var bubbleGo = MakeSpriteImage(content, "TitleBubble",
                "Assets/Sprites/Title_Bubble.png", new Vector2(0, -90), new Vector2(940, 400));

            var wordGo = MakeSpriteImage(content, "TitleWord",
                "Assets/Sprites/Title_Word.png",   new Vector2(-20, -115), new Vector2(860, 290));

            var lemonGo = MakeSpriteImage(content, "TitleLemon",
                "Assets/Sprites/Title_Lemon.png",  new Vector2(20, 290), new Vector2(620, 620));

            // ── Start button (large, bottom area) ────────────────────────────
            var startBtnGo = MakeSpriteButton(content, "StartButton",
                "Assets/Sprites/start.png", new Vector2(0, -480), new Vector2(800, 205));

            // ── Top navigation buttons (white pill, top of screen) ────────────
            var rulesBtnGo = MakeTopButton(content, "RulesButton", "? ルール",
                new Vector2(-375, 800), new Vector2(205, 74));

            var topicBtnGo = MakeTopButton(content, "TopicButton", "≡ お題",
                new Vector2(-138, 800), new Vector2(185, 74));

            // ── Hell Mode button (top right) ──────────────────────────────────
            Image    hellIndicatorImg;
            TextMeshProUGUI hellLabelTmp;
            var hellBtnGo = MakeHellModeButton(content,
                out hellIndicatorImg, out hellLabelTmp,
                new Vector2(328, 800), new Vector2(272, 74));

            // ── Info texts (below START) ──────────────────────────────────────
            var infoJa  = MakeTMP(content, "InfoTextJa",
                "2〜24人のパーティーゲーム", 30, new Vector2(0, -635), new Vector2(760, 60), Sub);
            var infoEn  = MakeTMP(content, "InfoTextEn",
                "A party game for 2-24 players", 24, new Vector2(0, -690), new Vector2(820, 48), Sub);
            var hellJa  = MakeTMP(content, "HellModeInfoJa",
                "", 24, new Vector2(0, -745), new Vector2(760, 52), Lime);
            var hellEn  = MakeTMP(content, "HellModeInfoEn",
                "", 20, new Vector2(0, -795), new Vector2(760, 42), Lime);

            // ── Overlay panels (hidden) ───────────────────────────────────────
            var rulesPanel = MakeOverlayPanel(safeAreaGo, "RulesPanel", "ルール");
            var topicPanel = MakeOverlayPanel(safeAreaGo, "TopicManagerPanel", "お題管理");
            rulesPanel.SetActive(false);
            topicPanel.SetActive(false);

            // ── TitleScreenUI wiring ──────────────────────────────────────────
            var uiGo = MakeStretchPanel(safeAreaGo, "TitleScreenUI");
            var ui   = uiGo.AddComponent<TitleScreenUI>();
            ui.titleBubble        = bubbleGo.GetComponent<RectTransform>();
            ui.titleLemon         = lemonGo.GetComponent<RectTransform>();
            ui.titleWord          = wordGo.GetComponent<RectTransform>();
            ui.background         = bgImg;
            ui.rainingLemonPrefab = lemonPrefab;
            ui.lemonRainParent    = rainParent.GetComponent<RectTransform>();
            ui.startButton        = startBtnGo.GetComponent<Button>();
            ui.rulesButton        = rulesBtnGo.GetComponent<Button>();
            ui.topicButton        = topicBtnGo.GetComponent<Button>();
            ui.hellModeButton     = hellBtnGo.GetComponent<Button>();
            ui.hellModeIndicator  = hellIndicatorImg;
            ui.hellModeLabel      = hellLabelTmp;
            ui.infoTextJa         = infoJa.GetComponent<TextMeshProUGUI>();
            ui.infoTextEn         = infoEn.GetComponent<TextMeshProUGUI>();
            ui.hellModeInfoJa     = hellJa.GetComponent<TextMeshProUGUI>();
            ui.hellModeInfoEn     = hellEn.GetComponent<TextMeshProUGUI>();
            ui.rulesPanel         = rulesPanel;
            ui.topicManagerPanel  = topicPanel;
            EditorUtility.SetDirty(uiGo);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BOMBOMLemon] TitleScene built successfully!");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        static void FixEventSystem(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var es = root.GetComponent<EventSystem>();
                if (es == null) continue;

                var standalone = root.GetComponent<StandaloneInputModule>();
                if (standalone != null)
                    Object.DestroyImmediate(standalone);

                if (root.GetComponent<InputSystemUIInputModule>() == null)
                    root.AddComponent<InputSystemUIInputModule>();

                // Remove any MonoBehaviours whose backing script has been deleted
                foreach (var mb in root.GetComponents<MonoBehaviour>())
                {
                    if (mb == null)
                        Object.DestroyImmediate(mb);
                }
                break;
            }
        }

        static T Load<T>(string path) where T : Object
            => AssetDatabase.LoadAssetAtPath<T>(path);

        static Sprite LoadSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static RectTransform SetupRT(GameObject go, Vector2 pos, Vector2 size)
        {
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            return rt;
        }

        static RectTransform SetupStretchRT(GameObject go)
        {
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        static Image MakeStretchImage(GameObject parent, string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            SetupStretchRT(go);
            var img = go.AddComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            return img;
        }

        static GameObject MakeStretchPanel(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            SetupStretchRT(go);
            return go;
        }

        static GameObject MakeSpriteImage(GameObject parent, string name,
            string spritePath, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            SetupRT(go, pos, size);
            var img = go.AddComponent<Image>();
            var spr = LoadSprite(spritePath);
            if (spr != null) { img.sprite = spr; img.preserveAspect = true; }
            img.raycastTarget = false;
            return go;
        }

        static GameObject MakeSpriteButton(GameObject parent, string name,
            string spritePath, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            SetupRT(go, pos, size);
            var img = go.AddComponent<Image>();
            var spr = LoadSprite(spritePath);
            if (spr != null) { img.sprite = spr; img.preserveAspect = true; }
            var btn = go.AddComponent<Button>();
            DisableNavigation(btn);
            return go;
        }

        static GameObject MakeTopButton(GameObject parent, string name, string label,
            Vector2 pos, Vector2 size)
        {
            var bgColor = new Color(1f, 1f, 1f, 0.92f);
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            SetupRT(go, pos, size);
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor      = bgColor;
            cols.highlightedColor = Color.white;
            cols.pressedColor     = new Color(0.80f, 0.80f, 0.80f);
            btn.colors = cols;
            DisableNavigation(btn);
            var txt = MakeTMP(go, "Label", label, 30, Vector2.zero, size, Dark);
            txt.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            return go;
        }

        static GameObject MakeTextButton(GameObject parent, string name, string label,
            Vector2 pos, Vector2 size, Color bgColor, Color textColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            SetupRT(go, pos, size);
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor      = bgColor;
            cols.highlightedColor = bgColor * 1.1f;
            cols.pressedColor     = bgColor * 0.82f;
            btn.colors = cols;
            DisableNavigation(btn);

            var txt = MakeTMP(go, "Label", label, 32, Vector2.zero, size, textColor);
            txt.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            return go;
        }

        static GameObject MakeHellModeButton(GameObject parent,
            out Image indicatorImg, out TextMeshProUGUI labelTmp,
            Vector2 pos, Vector2 size)
        {
            var go = new GameObject("HellModeButton");
            go.transform.SetParent(parent.transform, false);
            SetupRT(go, pos, size);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.65f);
            var btn = go.AddComponent<Button>();
            DisableNavigation(btn);

            // Indicator dot
            var indGo = new GameObject("HellModeIndicator");
            indGo.transform.SetParent(go.transform, false);
            var indRT = indGo.AddComponent<RectTransform>();
            indRT.anchorMin = indRT.anchorMax = new Vector2(0.5f, 0.5f);
            indRT.anchoredPosition = new Vector2(-108f, 0);
            indRT.sizeDelta        = new Vector2(22, 22);
            indicatorImg = indGo.AddComponent<Image>();
            indicatorImg.color = new Color(0.75f, 0.75f, 0.75f);

            // Label
            var lblGo = MakeTMP(go, "HellModeLabel", "地獄モード", 26,
                new Vector2(18f, 0), new Vector2(290, 56), Sub);
            labelTmp = lblGo.GetComponent<TextMeshProUGUI>();
            labelTmp.fontStyle = FontStyles.Bold;

            return go;
        }

        static GameObject MakeTMP(GameObject parent, string name, string text,
            int fontSize, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            SetupRT(go, pos, size);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text             = text;
            tmp.fontSize         = fontSize;
            tmp.color            = color;
            tmp.alignment        = TextAlignmentOptions.Center;
#pragma warning disable CS0618
            tmp.enableWordWrapping = false;
#pragma warning restore CS0618
            tmp.overflowMode     = TextOverflowModes.Overflow;
            return go;
        }

        static GameObject MakeOverlayPanel(GameObject parent, string name, string titleText)
        {
            var go = MakeStretchPanel(parent, name);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.04f, 0.00f, 0.88f);
            bg.raycastTarget = true;

            MakeTMP(go, "Title", titleText, 52,
                new Vector2(0, 700), new Vector2(900, 90), Color.white)
                .GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            var placeholder = MakeTMP(go, "Body", "（準備中）", 32,
                new Vector2(0, 0), new Vector2(800, 600), new Color(0.9f, 0.9f, 0.9f));
#pragma warning disable CS0618
            placeholder.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;
#pragma warning restore CS0618

            MakeTextButton(go, "CloseButton", "閉じる",
                new Vector2(0, -750), new Vector2(320, 95), BtnYellow, Dark);

            return go;
        }

        static GameObject EnsureRainingLemonPrefab()
        {
            const string path = "Assets/Prefabs/RainingLemonItem.prefab";
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            // Always recreate so sprite changes are reflected
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

            var go  = new GameObject("RainingLemonItem");
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(50, 50);
            var img = go.AddComponent<Image>();
            var spr = LoadSprite("Assets/Sprites/Title_Lemon.png");
            if (spr != null) img.sprite = spr;
            img.raycastTarget = false;
            go.AddComponent<RainingLemonItem>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            AssetDatabase.Refresh();
            return prefab;
        }

        static void DisableNavigation(Button btn)
        {
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
        }
    }
}
