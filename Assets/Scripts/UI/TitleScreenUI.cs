using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    public class TitleScreenUI : MonoBehaviour
    {
        [Header("Logo Images")]
        public RectTransform titleBubble;
        public RectTransform titleLemon;
        public RectTransform titleWord;

        [Header("Content Group (fade-in on open)")]
        public CanvasGroup contentGroup;

        [Header("Start Button")]
        public Button    startButton;
        public Sprite    startSprite;
        public Sprite    startLimeSprite;

        [Header("Top Navigation Buttons")]
        public Button rulesButton;
        public Button topicButton;

        [Header("Hell Mode Toggle")]
        public Button hellModeButton;
        public Image  hellModeIndicator;  // the 8pt circle dot
        public Text   hellModeLabel;      // "地獄モード" / "地獄モード中"
        public Image  hellModeBg;         // capsule background of hell-mode button

        [Header("Info Texts (below start button)")]
        public Text infoTextJa;       // "2〜24人のパーティーゲーム"
        public Text infoTextEn;       // "A party game for 2–24 players"
        public Text hellModeInfoJa;   // "地獄モード：ライフ½・ヘルプなし"
        public Text hellModeInfoEn;   // "Hell Mode: ½ life · no help cards"

        [Header("Sheet Panels")]
        public GameObject rulesPanel;
        public GameObject topicManagerPanel;

        [Header("Lemon Rain")]
        public GameObject   rainingLemonPrefab;
        public RectTransform lemonRainParent;

        // ── iOS colour palette ─────────────────────────────────────────────
        static readonly Color DarkBrown   = new Color(0.15f, 0.10f, 0.00f);
        static readonly Color SubBrown    = new Color(0.50f, 0.36f, 0.00f);
        static readonly Color LimeGreen   = new Color(0.25f, 0.60f, 0.08f);
        static readonly Color LimeBg      = new Color(0.65f, 0.92f, 0.28f);
        static readonly Color GreyDot     = new Color(0.75f, 0.75f, 0.75f);
        static readonly Color BtnNormalBg = new Color(1f, 1f, 1f, 0.65f);
        static readonly Color HellText    = new Color(0.14f, 0.40f, 0.03f);
        static readonly Color NormalHellT = new Color(0.40f, 0.30f, 0.05f);

        // ── animation state ────────────────────────────────────────────────
        private bool  _loopRunning;
        private float _loopTime;

        // ══════════════════════════════════════════════════════════════════

        void Start()
        {
            SpawnLemonRain();
            SetupButtons();
            StartCoroutine(EntranceAnimation());
            SoundManager.Instance?.PlayBGM("title_music");
        }

        void Update()
        {
            if (!_loopRunning) return;
            _loopTime += Time.deltaTime;

            // Bubble: float Y + drift X + breathe scale  (iOS durations: 3.5s / 4.2s / 5.0s)
            if (titleBubble)
            {
                float y = 12f * Mathf.Sin(2f * Mathf.PI * _loopTime / 3.5f);
                float x =  4f * Mathf.Sin(2f * Mathf.PI * _loopTime / 4.2f);
                float s =  1f + 0.03f * Mathf.Sin(2f * Mathf.PI * _loopTime / 5.0f);
                titleBubble.anchoredPosition = new Vector2(x, y);
                titleBubble.localScale       = Vector3.one * s;
            }

            // Word: bounce Y + rock rotation  (iOS: 1.4s / 2.8s)
            if (titleWord)
            {
                float y = 6f * Mathf.Sin(2f * Mathf.PI * _loopTime / 1.4f);
                float r = 3f * Mathf.Sin(2f * Mathf.PI * _loopTime / 2.8f);
                titleWord.anchoredPosition  = new Vector2(0, y);
                titleWord.localEulerAngles  = new Vector3(0, 0, r);
            }

            // Lemon: float Y + breathe scale + rock rotation  (iOS: 2.5s / 1.8s / 3.0s)
            if (titleLemon)
            {
                float y = 10f * Mathf.Sin(2f * Mathf.PI * _loopTime / 2.5f);
                float s =  1f + 0.06f * Mathf.Sin(2f * Mathf.PI * _loopTime / 1.8f);
                float r =  2f * Mathf.Sin(2f * Mathf.PI * _loopTime / 3.0f);
                titleLemon.anchoredPosition = new Vector2(0, y);
                titleLemon.localScale       = new Vector3(s, s, 1f);
                titleLemon.localEulerAngles = new Vector3(0, 0, r);
            }

            // Start button: purupuru scale pulse  (iOS: 1.8s)
            if (startButton)
            {
                float s = 1f + 0.06f * Mathf.Sin(2f * Mathf.PI * (_loopTime - 0.6f) / 1.8f);
                startButton.transform.localScale = Vector3.one * s;
            }
        }

        // ── Entrance animations ────────────────────────────────────────────

        private IEnumerator EntranceAnimation()
        {
            // All invisible at start
            SetImgAlpha(titleBubble, 0f);
            SetImgAlpha(titleWord,   0f);
            SetImgAlpha(titleLemon,  0f);
            if (contentGroup) contentGroup.alpha = 0f;

            // Word starts 40pt below resting; Lemon starts 60pt above resting
            if (titleWord)  titleWord.anchoredPosition  = new Vector2(0, -40f);
            if (titleLemon) titleLemon.anchoredPosition = new Vector2(0, +60f);

            float t0 = Time.time;
            while (true)
            {
                float t = Time.time - t0;

                // Bubble: fade-in 0 → 0.5 s
                SetImgAlpha(titleBubble, Mathf.Clamp01(t / 0.5f));

                // Word: fade 0.3 → 0.55 s, slide up 0.3 → 0.9 s
                SetImgAlpha(titleWord, Mathf.Clamp01((t - 0.3f) / 0.25f));
                if (titleWord)
                    titleWord.anchoredPosition = new Vector2(0,
                        -40f * (1f - EaseOut(Mathf.Clamp01((t - 0.3f) / 0.6f))));

                // Lemon: fade 0.5 → 0.7 s, spring drop 0.5 → 1.1 s
                SetImgAlpha(titleLemon, Mathf.Clamp01((t - 0.5f) / 0.2f));
                if (titleLemon)
                    titleLemon.anchoredPosition = new Vector2(0,
                        +60f * (1f - SpringOut(Mathf.Clamp01((t - 0.5f) / 0.7f))));

                // Content group fade 0.4 → 0.9 s
                if (contentGroup)
                    contentGroup.alpha = Mathf.Clamp01((t - 0.4f) / 0.5f);

                if (t >= 1.3f) break;
                yield return null;
            }

            // Snap to rest
            SetImgAlpha(titleBubble, 1f);
            SetImgAlpha(titleWord,   1f);
            SetImgAlpha(titleLemon,  1f);
            if (titleWord)  titleWord.anchoredPosition  = Vector2.zero;
            if (titleLemon) titleLemon.anchoredPosition = Vector2.zero;
            if (contentGroup) contentGroup.alpha = 1f;

            _loopRunning = true;
        }

        // ── Lemon rain ─────────────────────────────────────────────────────
        // Exact parameters from SwiftUI TitleView (screen = 390×844 reference pts)

        private void SpawnLemonRain()
        {
            if (rainingLemonPrefab == null || lemonRainParent == null) return;
            foreach (Transform c in lemonRainParent) Destroy(c.gameObject);

            const float w = 390f, h = 844f;

            for (int i = 0; i < 14; i++)
            {
                float size     = 40f + (i * 7 + 13) % 21;                    // 53,60,46 …
                float rot      = (float)((i * 37 + 17) % 61) - 30f;
                float xOffset  = (i * 67 + 31) % (int)w - w * 0.5f;
                float duration = 2.8f + (i % 2 == 0 ? 0f : 1.0f);            // 2.8 or 3.8 s
                float delay    = i * 0.35f;
                float speed    = (h + size * 2f) / duration;

                // Pre-advance the lemon by its delay so they're spread across the screen
                float fallDist = h + size * 2f;
                float cyclePos = (speed * delay) % fallDist;
                float startY   = h * 0.5f + size - cyclePos;

                var go   = Instantiate(rainingLemonPrefab, lemonRainParent);
                var item = go.GetComponent<RainingLemonItem>();
                item?.Init(speed, h, startY, xOffset, size, rot);
            }
        }

        // ── Button setup ───────────────────────────────────────────────────

        private void SetupButtons()
        {
            startButton?.onClick.AddListener(OnStart);
            rulesButton?.onClick.AddListener(OnRules);
            topicButton?.onClick.AddListener(OnTopic);
            hellModeButton?.onClick.AddListener(OnHellToggle);
            RefreshHellMode();
        }

        private void OnStart()
        {
            SoundManager.Instance?.PlaySE("click");
            SoundManager.Instance?.StopBGM();
            GameManager.Instance?.SetPhase(GamePhase.Setup);
        }

        private void OnRules()
        {
            SoundManager.Instance?.PlaySE("click");
            rulesPanel?.SetActive(true);
        }

        private void OnTopic()
        {
            SoundManager.Instance?.PlaySE("click");
            topicManagerPanel?.SetActive(true);
        }

        private void OnHellToggle()
        {
            SoundManager.Instance?.PlaySE("click");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LimeMode = !GameManager.Instance.LimeMode;
                RefreshHellMode();
            }
        }

        public void RefreshHellMode()
        {
            bool hell = GameManager.Instance?.LimeMode ?? false;

            if (hellModeIndicator)
                hellModeIndicator.color = hell ? LimeGreen : GreyDot;

            if (hellModeLabel)
            {
                hellModeLabel.text  = hell ? "地獄モード中" : "地獄モード";
                hellModeLabel.color = hell ? HellText : NormalHellT;
            }

            if (hellModeBg)
                hellModeBg.color = hell ? LimeBg : BtnNormalBg;

            // Swap start button image
            var startImg = startButton?.GetComponent<Image>();
            if (startImg && startSprite && startLimeSprite)
                startImg.sprite = hell ? startLimeSprite : startSprite;

            if (hellModeInfoJa)
            {
                hellModeInfoJa.text  = hell ? "地獄モード：ライフ½・ヘルプなし" : "";
                hellModeInfoJa.color = LimeGreen;
            }
            if (hellModeInfoEn)
            {
                hellModeInfoEn.text  = hell ? "Hell Mode: ½ life · no help cards" : "";
                hellModeInfoEn.color = LimeGreen;
            }
        }

        // ── Utilities ──────────────────────────────────────────────────────

        private static void SetImgAlpha(RectTransform rt, float a)
        {
            if (rt == null) return;
            var img = rt.GetComponent<Image>();
            if (img) img.color = new Color(img.color.r, img.color.g, img.color.b, a);
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        private static float SpringOut(float t)
        {
            // Under-damped spring: response≈0.52, dampingFraction≈0.48
            float v = Mathf.Clamp01(t);
            return 1f - Mathf.Exp(-v * 6f) * (Mathf.Cos(v * 11f) + 0.5f * Mathf.Sin(v * 11f));
        }
    }
}
