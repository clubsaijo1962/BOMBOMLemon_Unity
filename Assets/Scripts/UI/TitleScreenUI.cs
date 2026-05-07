using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BOMBOMLemon
{
    public class TitleScreenUI : MonoBehaviour
    {
        [Header("Logo Images")]
        public RectTransform titleBubble;
        public RectTransform titleLemon;
        public RectTransform titleWord;

        [Header("Background")]
        public Image background;

        [Header("Lemon Rain")]
        public GameObject rainingLemonPrefab;
        public RectTransform lemonRainParent;
        private const int LemonRainCount = 14;

        [Header("Buttons")]
        public Button startButton;
        public Button rulesButton;
        public Button topicButton;

        [Header("Hell Mode")]
        public Button hellModeButton;
        public Image hellModeIndicator;
        public TextMeshProUGUI hellModeLabel;

        [Header("Info Text")]
        public TextMeshProUGUI infoTextJa;
        public TextMeshProUGUI infoTextEn;
        public TextMeshProUGUI hellModeInfoJa;
        public TextMeshProUGUI hellModeInfoEn;

        [Header("Panels")]
        public GameObject rulesPanel;
        public GameObject topicManagerPanel;

        // Theme colors matching the iOS original
        private static readonly Color BgColor     = new Color(1.00f, 0.96f, 0.60f);
        private static readonly Color DarkColor   = new Color(0.15f, 0.10f, 0.00f);
        private static readonly Color SubColor    = new Color(0.50f, 0.36f, 0.00f);
        private static readonly Color LimeColor   = new Color(0.25f, 0.60f, 0.08f);
        private static readonly Color LimeBgColor = new Color(0.65f, 0.92f, 0.28f);

        private CanvasGroup _contentGroup;
        private bool _initialized;

        void OnEnable()
        {
            if (!_initialized) return;
            RefreshHellMode();
            SoundManager.Instance?.PlayBGM("title_music");
            SpawnLemonRain();
        }

        void Start()
        {
            _initialized = true;
            ApplyThemeColors();
            SetupButtons();
            SpawnLemonRain();
            StartCoroutine(PlayIntroAnimations());
            SoundManager.Instance?.PlayBGM("title_music");
        }

        // ──── Setup ────────────────────────────────────────────────────────────

        private void ApplyThemeColors()
        {
            if (background != null)
                background.color = BgColor;
        }

        private void SetupButtons()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartPressed);
            if (rulesButton != null)
                rulesButton.onClick.AddListener(OnRulesPressed);
            if (topicButton != null)
                topicButton.onClick.AddListener(OnTopicPressed);
            if (hellModeButton != null)
                hellModeButton.onClick.AddListener(OnHellModeToggled);
        }

        // ──── Lemon Rain ───────────────────────────────────────────────────────

        private void SpawnLemonRain()
        {
            if (rainingLemonPrefab == null || lemonRainParent == null) return;

            // Clear existing
            foreach (Transform child in lemonRainParent)
                Destroy(child.gameObject);

            float w = lemonRainParent.rect.width;
            float h = lemonRainParent.rect.height;

            for (int i = 0; i < LemonRainCount; i++)
            {
                float size  = 40f + (i * 7 + 13) % 21;
                float rot   = (i * 37 + 17) % 61 - 30f;
                float xOff  = (i * 67 + 31) % (int)w - w * 0.5f;
                float speed = 180f + (i * 23 + 7) % 80;
                float startY = h * 0.5f + size + i * (h / LemonRainCount);

                var obj = Instantiate(rainingLemonPrefab, lemonRainParent);
                var item = obj.GetComponent<RainingLemonItem>();
                item?.Init(speed, h, startY, xOff, size, rot);
            }
        }

        // ──── Intro Animations ─────────────────────────────────────────────────

        private IEnumerator PlayIntroAnimations()
        {
            // Fade-in content
            if (_contentGroup != null)
            {
                _contentGroup.alpha = 0f;
                yield return FadeTo(_contentGroup, 1f, 0.4f, 0f);
            }

            // Title_Bubble: fade in
            if (titleBubble != null)
            {
                var cg = GetOrAddCanvasGroup(titleBubble.gameObject);
                StartCoroutine(FadeTo(cg, 1f, 0.5f, 0f));
                StartCoroutine(LoopFloat(titleBubble, Vector2.up, 12f, 3.5f, 0.1f));
            }

            // Title_Word: slide up from below, then bounce
            if (titleWord != null)
            {
                var cg = GetOrAddCanvasGroup(titleWord.gameObject);
                StartCoroutine(FadeTo(cg, 1f, 0.25f, 0.3f));
                StartCoroutine(SlideIn(titleWord, new Vector2(0, -40), Vector2.zero, 0.6f, 0.3f));
                yield return new WaitForSeconds(0.95f);
                StartCoroutine(LoopFloat(titleWord, Vector2.up, 6f, 1.4f, 0f));
                StartCoroutine(LoopRotate(titleWord, 3f, 2.8f, 0f));
            }

            // Title_Lemon: drop from above with spring, then float
            if (titleLemon != null)
            {
                var cg = GetOrAddCanvasGroup(titleLemon.gameObject);
                StartCoroutine(FadeTo(cg, 1f, 0.2f, 0.5f));
                StartCoroutine(SpringDrop(titleLemon, -60f, 0f, 0.52f, 0.48f, 0.5f));
                yield return new WaitForSeconds(1.2f);
                StartCoroutine(LoopFloat(titleLemon, Vector2.up, 10f, 1.2f, 0f));
                StartCoroutine(LoopRotate(titleLemon, 2f, 3.0f, 0f));
                StartCoroutine(LoopScale(titleLemon, 1.06f, 1.8f, 0f));
            }

            // Start button pulse
            if (startButton != null)
                StartCoroutine(LoopScale(startButton.transform as RectTransform, 1.06f, 1.8f, 0.6f));
        }

        // ──── Animation Coroutines ─────────────────────────────────────────────

        private IEnumerator FadeTo(CanvasGroup cg, float target, float duration, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            float start = cg.alpha;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            cg.alpha = target;
        }

        private IEnumerator SlideIn(RectTransform rt, Vector2 from, Vector2 to, float duration, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = EaseOut(t / duration);
                rt.anchoredPosition = Vector2.Lerp(from, to, p);
                yield return null;
            }
            rt.anchoredPosition = to;
        }

        private IEnumerator SpringDrop(RectTransform rt, float fromY, float toY, float response, float damping, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            float elapsed = 0;
            float vel = 0;
            float cur = fromY;
            while (Mathf.Abs(cur - toY) > 0.5f || Mathf.Abs(vel) > 0.5f)
            {
                float force = -(cur - toY) / (response * response);
                vel += force * Time.deltaTime;
                vel *= Mathf.Pow(1f - damping, Time.deltaTime * 60f);
                cur += vel * Time.deltaTime * 60f;
                var pos = rt.anchoredPosition;
                pos.y = cur;
                rt.anchoredPosition = pos;
                elapsed += Time.deltaTime;
                if (elapsed > 3f) break;
                yield return null;
            }
            var finalPos = rt.anchoredPosition;
            finalPos.y = toY;
            rt.anchoredPosition = finalPos;
        }

        private IEnumerator LoopFloat(RectTransform rt, Vector2 direction, float amplitude, float period, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            float t = 0;
            Vector2 basePos = rt.anchoredPosition;
            while (true)
            {
                t += Time.deltaTime;
                float offset = Mathf.Sin(t * Mathf.PI * 2f / period) * amplitude;
                rt.anchoredPosition = basePos + direction.normalized * offset;
                yield return null;
            }
        }

        private IEnumerator LoopRotate(RectTransform rt, float degrees, float period, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            float t = 0;
            while (true)
            {
                t += Time.deltaTime;
                float rot = Mathf.Sin(t * Mathf.PI * 2f / period) * degrees;
                rt.localRotation = Quaternion.Euler(0, 0, rot);
                yield return null;
            }
        }

        private IEnumerator LoopScale(RectTransform rt, float maxScale, float period, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            float t = 0;
            while (true)
            {
                t += Time.deltaTime;
                float s = 1f + (maxScale - 1f) * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / period));
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        // ──── Button Handlers ──────────────────────────────────────────────────

        private void OnStartPressed()
        {
            SoundManager.Instance?.PlaySE("click");
            SoundManager.Instance?.StopBGM();
            GameManager.Instance?.SetPhase(GamePhase.Setup);
        }

        private void OnRulesPressed()
        {
            SoundManager.Instance?.PlaySE("click");
            if (rulesPanel != null) rulesPanel.SetActive(true);
        }

        private void OnTopicPressed()
        {
            SoundManager.Instance?.PlaySE("click");
            if (topicManagerPanel != null) topicManagerPanel.SetActive(true);
        }

        private void OnHellModeToggled()
        {
            SoundManager.Instance?.PlaySE("click");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LimeMode = !GameManager.Instance.LimeMode;
                RefreshHellMode();
            }
        }

        private void RefreshHellMode()
        {
            bool hell = GameManager.Instance?.LimeMode ?? false;

            if (hellModeIndicator != null)
                hellModeIndicator.color = hell ? LimeColor : new Color(0.75f, 0.75f, 0.75f);

            if (hellModeLabel != null)
            {
                hellModeLabel.text = hell ? "地獄モード中" : "地獄モード";
                hellModeLabel.color = hell
                    ? new Color(0.14f, 0.40f, 0.03f)
                    : new Color(0.40f, 0.30f, 0.05f);
            }

            if (hellModeButton != null)
            {
                var img = hellModeButton.GetComponent<Image>();
                if (img != null) img.color = hell ? LimeBgColor : new Color(1f, 1f, 1f, 0.65f);
            }

            if (hellModeInfoJa != null)
            {
                hellModeInfoJa.text = hell ? "地獄モード：ライフ½・ヘルプなし" : "";
                hellModeInfoJa.color = LimeColor;
            }

            if (hellModeInfoEn != null)
            {
                hellModeInfoEn.text = hell ? "Hell Mode: ½ life · no help cards" : "";
                hellModeInfoEn.color = LimeColor;
            }
        }

        // ──── Utility ──────────────────────────────────────────────────────────

        private static CanvasGroup GetOrAddCanvasGroup(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            return cg;
        }
    }
}
