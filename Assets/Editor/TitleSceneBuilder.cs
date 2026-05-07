using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using BOMBOMLemon;

namespace BOMBOMLemon.Editor
{
    public static class TitleSceneBuilder
    {
        // Reference resolution (portrait)
        const float W = 1080f, H = 1920f;

        static readonly Color BgColor    = new Color(1.00f, 0.96f, 0.60f);
        static readonly Color Dark       = new Color(0.15f, 0.10f, 0.00f);
        static readonly Color Sub        = new Color(0.50f, 0.36f, 0.00f);
        static readonly Color Lime       = new Color(0.25f, 0.60f, 0.08f);
        static readonly Color BtnYellow  = new Color(1.00f, 0.85f, 0.10f);
        static readonly Color BtnShadow  = new Color(0.55f, 0.40f, 0.00f, 0.5f);
        static readonly Color NavBg      = new Color(1.00f, 1.00f, 1.00f, 0.92f);

        // ─────────────────────────────────────────────────────────────────────
        [MenuItem("BOMBOMLemon/Build Title Scene")]
        public static void BuildTitleScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity");

            // Clear non-essential root objects
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponent<Camera>()      != null) continue;
                if (root.GetComponent<EventSystem>() != null) continue;
                Object.DestroyImmediate(root);
            }

            FixEventSystem(scene);
            var lemonPrefab = BuildLemonPrefab();

            // ── Managers ─────────────────────────────────────────────────────
            var gmGo = new GameObject("GameManager");
            gmGo.AddComponent<GameManager>();

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
            canvas.sortingOrder = 0;
            var cs = canvasGo.AddComponent<CanvasScaler>();
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(W, H);
            cs.matchWidthOrHeight  = 0f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // ── Background (full canvas, behind everything) ───────────────────
            var bgImg = MakeStretchImage(canvasGo, "Background", null);
            bgImg.color = BgColor;

            // ── Lemon Rain layer (full canvas, behind SafeArea content) ───────
            var rainParentGo = MakeStretchPanel(canvasGo, "LemonRainParent");

            // ── Safe Area ─────────────────────────────────────────────────────
            var safeAreaGo = MakeStretchPanel(canvasGo, "SafeArea");
            safeAreaGo.AddComponent<SafeAreaFitter>();

            // ── Content (fades in on start) ───────────────────────────────────
            var content = MakeStretchPanel(safeAreaGo, "Content");
            var contentCG = content.AddComponent<CanvasGroup>();

            // ── Title Logo (integrated 3-layer, slightly above center) ─────────
            // Layer order: Bubble (back) → Lemon (mid) → Word (front)
            var bubbleGo = MakeSpriteImage(content, "TitleBubble",
                "Assets/Sprites/Title_Bubble.png",
                new Vector2(0f, 200f), new Vector2(980f, 520f));

            var lemonGo = MakeSpriteImage(content, "TitleLemon",
                "Assets/Sprites/Title_Lemon.png",
                new Vector2(20f, 430f), new Vector2(570f, 570f));

            var wordGo = MakeSpriteImage(content, "TitleWord",
                "Assets/Sprites/Title_Word.png",
                new Vector2(-10f, 80f), new Vector2(900f, 265f));

            // ── Main Buttons ──────────────────────────────────────────────────
            // ゲームスタート (sprite)
            var startBtnGo = MakeSpriteButton(content, "StartButton",
                "Assets/Sprites/start.png",
                new Vector2(0f, -360f), new Vector2(820f, 200f));

            // 部屋作成 / 部屋参加 (yellow, side by side)
            var roomCreateGo = MakeYellowButton(content, "RoomCreateButton", "部屋作成",
                new Vector2(-213f, -535f), new Vector2(385f, 108f));
            var roomJoinGo   = MakeYellowButton(content, "RoomJoinButton",   "部屋参加",
                new Vector2( 213f, -535f), new Vector2(385f, 108f));

            // ── Info Text (below room buttons) ────────────────────────────────
            var infoJa = MakeLabel(content, "InfoTextJa",
                "2〜24人のパーティーゲーム", 28,
                new Vector2(0f, -665f), new Vector2(760f, 55f), Sub);
            var infoEn = MakeLabel(content, "InfoTextEn",
                "A party game for 2-24 players", 23,
                new Vector2(0f, -715f), new Vector2(820f, 45f), Sub);
            var hellJa = MakeLabel(content, "HellModeInfoJa", "", 24,
                new Vector2(0f, -768f), new Vector2(760f, 50f), Lime);
            var hellEn = MakeLabel(content, "HellModeInfoEn", "", 20,
                new Vector2(0f, -815f), new Vector2(760f, 42f), Lime);

            // ── Top Navigation (anchored to TOP edge) ─────────────────────────
            //   ルール  [left=22, top=25, w=205, h=85]
            var rulesBtnGo = MakeTopNavBtn(content, "RulesButton", "? ルール",
                false, 22f, 25f, new Vector2(205f, 85f));
            //   お題    [left=239, top=25, w=188, h=85]
            var topicBtnGo = MakeTopNavBtn(content, "TopicButton", "≡ お題",
                false, 239f, 25f, new Vector2(188f, 85f));
            //   地獄モード [right=22, top=25, w=295, h=85]
            Image hellImg; Text hellLbl;
            var hellBtnGo = MakeHellNavBtn(content, out hellImg, out hellLbl,
                22f, 25f, new Vector2(295f, 85f));

            // ── Overlay Panels (hidden) ───────────────────────────────────────
            var rulesPanel = MakeOverlayPanel(safeAreaGo, "RulesPanel",        "ルール");
            var topicPanel = MakeOverlayPanel(safeAreaGo, "TopicManagerPanel", "お題管理");
            rulesPanel.SetActive(false);
            topicPanel.SetActive(false);

            // ── TitleScreenUI wiring ──────────────────────────────────────────
            var uiGo = MakeStretchPanel(safeAreaGo, "TitleScreenUI");
            var ui   = uiGo.AddComponent<TitleScreenUI>();

            ui.titleBubble      = bubbleGo.GetComponent<RectTransform>();
            ui.titleLemon       = lemonGo.GetComponent<RectTransform>();
            ui.titleWord        = wordGo.GetComponent<RectTransform>();
            ui.background       = bgImg;
            ui.contentGroup     = contentCG;
            ui.rainingLemonPrefab = lemonPrefab;
            ui.lemonRainParent  = rainParentGo.GetComponent<RectTransform>();
            ui.startButton      = startBtnGo.GetComponent<Button>();
            ui.roomCreateButton = roomCreateGo.GetComponent<Button>();
            ui.roomJoinButton   = roomJoinGo.GetComponent<Button>();
            ui.rulesButton      = rulesBtnGo.GetComponent<Button>();
            ui.topicButton      = topicBtnGo.GetComponent<Button>();
            ui.hellModeButton   = hellBtnGo.GetComponent<Button>();
            ui.hellModeIndicator= hellImg;
            ui.hellModeLabel    = hellLbl;
            ui.infoTextJa       = infoJa.GetComponent<Text>();
            ui.infoTextEn       = infoEn.GetComponent<Text>();
            ui.hellModeInfoJa   = hellJa.GetComponent<Text>();
            ui.hellModeInfoEn   = hellEn.GetComponent<Text>();
            ui.rulesPanel       = rulesPanel;
            ui.topicManagerPanel= topicPanel;
            EditorUtility.SetDirty(uiGo);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BOMBOMLemon] TitleScene built successfully!");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Font
        // ─────────────────────────────────────────────────────────────────────

        static Font _jpFont;
        static Font GetJPFont()
        {
            if (_jpFont != null) return _jpFont;
            // Try Japanese system fonts in order (macOS → iOS → Android fallback)
            _jpFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Hiragino Sans", "HiraKakuProN-W3", "HiraginoSans-W3",
                        "Noto Sans CJK JP", "NotoSansCJKjp-Regular", "Arial" }, 14);
            return _jpFont;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Layout helpers
        // ─────────────────────────────────────────────────────────────────────

        static RectTransform CenterRT(GameObject go, Vector2 pos, Vector2 size)
        {
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            return rt;
        }

        static RectTransform StretchRT(GameObject go)
        {
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        static GameObject MakeStretchPanel(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            StretchRT(go);
            return go;
        }

        static Image MakeStretchImage(GameObject parent, string name, Sprite spr)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            StretchRT(go);
            var img = go.AddComponent<Image>();
            if (spr != null) img.sprite = spr;
            return img;
        }

        // Sprite image (center-anchored)
        static GameObject MakeSpriteImage(GameObject parent, string name,
            string spritePath, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            CenterRT(go, pos, size);
            var img = go.AddComponent<Image>();
            var spr = LoadSprite(spritePath);
            if (spr != null) { img.sprite = spr; img.preserveAspect = true; }
            img.raycastTarget = false;
            return go;
        }

        // Sprite button (center-anchored, Image + Button, no separate label)
        static GameObject MakeSpriteButton(GameObject parent, string name,
            string spritePath, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            CenterRT(go, pos, size);
            var img = go.AddComponent<Image>();
            var spr = LoadSprite(spritePath);
            if (spr != null) { img.sprite = spr; img.preserveAspect = true; }
            var btn = go.AddComponent<Button>();
            NoNav(btn);
            return go;
        }

        // Yellow text button with drop shadow (center-anchored)
        static GameObject MakeYellowButton(GameObject parent, string name,
            string label, Vector2 pos, Vector2 size)
        {
            // Shadow
            var shadow = new GameObject(name + "_Shadow");
            shadow.transform.SetParent(parent.transform, false);
            CenterRT(shadow, pos + new Vector2(5f, -7f), size + new Vector2(10f, 10f));
            var shadowImg = shadow.AddComponent<Image>();
            shadowImg.color = BtnShadow;
            shadowImg.raycastTarget = false;

            // Button
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            CenterRT(go, pos, size);
            var img = go.AddComponent<Image>();
            img.color = BtnYellow;
            var btn = go.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor      = BtnYellow;
            cols.highlightedColor = new Color(1f, 0.95f, 0.3f);
            cols.pressedColor     = new Color(0.85f, 0.70f, 0.05f);
            btn.colors = cols;
            NoNav(btn);

            // Label
            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lblRT = lblGo.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
            var t = lblGo.AddComponent<Text>();
            t.text      = label;
            t.font      = GetJPFont();
            t.fontSize  = 36;
            t.color     = Dark;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            return go;
        }

        // Legacy Text label (center-anchored, for info texts)
        static GameObject MakeLabel(GameObject parent, string name, string text,
            int fontSize, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            CenterRT(go, pos, size);
            var t = go.AddComponent<Text>();
            t.text      = text;
            t.font      = GetJPFont();
            t.fontSize  = fontSize;
            t.color     = color;
            t.alignment = TextAnchor.MiddleCenter;
            return go;
        }

        // ── Top Navigation: anchored to TOP edge ──────────────────────────────

        // fromRight=false: xOffset from left edge
        // fromRight=true : xOffset from right edge
        static GameObject MakeTopNavBtn(GameObject parent, string name, string label,
            bool fromRight, float xOffset, float yFromTop, Vector2 size)
        {
            // Shadow
            var shadow = new GameObject(name + "_Shadow");
            shadow.transform.SetParent(parent.transform, false);
            var srt = shadow.AddComponent<RectTransform>();
            float ax = fromRight ? 1f : 0f;
            srt.anchorMin = new Vector2(ax, 1f); srt.anchorMax = new Vector2(ax, 1f);
            srt.pivot     = new Vector2(ax, 1f);
            srt.anchoredPosition = fromRight
                ? new Vector2(-xOffset + 3f, -yFromTop - 5f)
                : new Vector2( xOffset + 3f, -yFromTop - 5f);
            srt.sizeDelta = size + new Vector2(6f, 6f);
            var sImg = shadow.AddComponent<Image>();
            sImg.color = new Color(0f, 0f, 0f, 0.12f);
            sImg.raycastTarget = false;

            // Button
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(ax, 1f); rt.anchorMax = new Vector2(ax, 1f);
            rt.pivot     = new Vector2(ax, 1f);
            rt.anchoredPosition = fromRight
                ? new Vector2(-xOffset, -yFromTop)
                : new Vector2( xOffset, -yFromTop);
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = NavBg;
            var btn = go.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor      = NavBg;
            cols.highlightedColor = Color.white;
            cols.pressedColor     = new Color(0.80f, 0.80f, 0.80f);
            btn.colors = cols;
            NoNav(btn);

            // Stretch-fill label
            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lblRT = lblGo.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
            var t = lblGo.AddComponent<Text>();
            t.text      = label;
            t.font      = GetJPFont();
            t.fontSize  = 30;
            t.color     = Dark;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            return go;
        }

        // 地獄モード toggle button (top-right)
        static GameObject MakeHellNavBtn(GameObject parent,
            out Image indicatorImg, out Text labelText,
            float xFromRight, float yFromTop, Vector2 size)
        {
            // Shadow
            var shadow = new GameObject("HellModeButton_Shadow");
            shadow.transform.SetParent(parent.transform, false);
            var srt = shadow.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(1f, 1f); srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot     = new Vector2(1f, 1f);
            srt.anchoredPosition = new Vector2(-xFromRight + 3f, -yFromTop - 5f);
            srt.sizeDelta = size + new Vector2(6f, 6f);
            var sImg = shadow.AddComponent<Image>();
            sImg.color = new Color(0f, 0f, 0f, 0.12f);
            sImg.raycastTarget = false;

            // Button body
            var go = new GameObject("HellModeButton");
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-xFromRight, -yFromTop);
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = NavBg;
            var btn = go.AddComponent<Button>();
            NoNav(btn);

            // Indicator dot (left side)
            var dotGo = new GameObject("HellModeIndicator");
            dotGo.transform.SetParent(go.transform, false);
            var dotRT = dotGo.AddComponent<RectTransform>();
            dotRT.anchorMin = new Vector2(0f, 0.5f); dotRT.anchorMax = new Vector2(0f, 0.5f);
            dotRT.pivot     = new Vector2(0f, 0.5f);
            dotRT.anchoredPosition = new Vector2(18f, 0f);
            dotRT.sizeDelta = new Vector2(20f, 20f);
            indicatorImg = dotGo.AddComponent<Image>();
            indicatorImg.color = new Color(0.75f, 0.75f, 0.75f);

            // Label
            var lblGo = new GameObject("HellModeLabel");
            lblGo.transform.SetParent(go.transform, false);
            var lblRT = lblGo.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = new Vector2(44f, 0f); lblRT.offsetMax = Vector2.zero;
            var t = lblGo.AddComponent<Text>();
            t.text      = "地獄モード";
            t.font      = GetJPFont();
            t.fontSize  = 28;
            t.color     = Sub;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            labelText = t;
            return go;
        }

        // ── Overlay panel (dark modal) ────────────────────────────────────────

        static GameObject MakeOverlayPanel(GameObject parent, string name, string title)
        {
            var go = MakeStretchPanel(parent, name);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.04f, 0.00f, 0.88f);
            bg.raycastTarget = true;

            // Title label
            var ttlGo = new GameObject("Title");
            ttlGo.transform.SetParent(go.transform, false);
            CenterRT(ttlGo, new Vector2(0f, 700f), new Vector2(900f, 90f));
            var ttlT = ttlGo.AddComponent<Text>();
            ttlT.text      = title;
            ttlT.font      = GetJPFont();
            ttlT.fontSize  = 52;
            ttlT.color     = Color.white;
            ttlT.fontStyle = FontStyle.Bold;
            ttlT.alignment = TextAnchor.MiddleCenter;

            // Body placeholder
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(go.transform, false);
            CenterRT(bodyGo, new Vector2(0f, 0f), new Vector2(800f, 600f));
            var bodyT = bodyGo.AddComponent<Text>();
            bodyT.text      = "（準備中）";
            bodyT.font      = GetJPFont();
            bodyT.fontSize  = 32;
            bodyT.color     = new Color(0.9f, 0.9f, 0.9f);
            bodyT.alignment = TextAnchor.MiddleCenter;

            // Close button
            MakeYellowButton(go, "CloseButton", "閉じる",
                new Vector2(0f, -750f), new Vector2(320f, 95f));

            return go;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Lemon rain prefab (always rebuilt to reflect latest sprite)
        // ─────────────────────────────────────────────────────────────────────

        static GameObject BuildLemonPrefab()
        {
            const string path = "Assets/Prefabs/RainingLemonItem.prefab";
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

            var go  = new GameObject("RainingLemonItem");
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(60f, 60f);
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

        // ─────────────────────────────────────────────────────────────────────
        // Utilities
        // ─────────────────────────────────────────────────────────────────────

        static T Load<T>(string path) where T : Object
            => AssetDatabase.LoadAssetAtPath<T>(path);

        static Sprite LoadSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType    = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void NoNav(Button btn)
        {
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
        }

        static void FixEventSystem(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var es = root.GetComponent<EventSystem>();
                if (es == null) continue;
                var old = root.GetComponent<StandaloneInputModule>();
                if (old != null) Object.DestroyImmediate(old);
                if (root.GetComponent<InputSystemUIInputModule>() == null)
                    root.AddComponent<InputSystemUIInputModule>();
                foreach (var mb in root.GetComponents<MonoBehaviour>())
                    if (mb == null) Object.DestroyImmediate(mb);
                break;
            }
        }
    }
}
