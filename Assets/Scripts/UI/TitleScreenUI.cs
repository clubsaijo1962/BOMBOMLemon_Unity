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

        [Header("Background & Fade")]
        public Image     background;
        public CanvasGroup contentGroup;

        [Header("Lemon Rain")]
        public GameObject    rainingLemonPrefab;
        public RectTransform lemonRainParent;
        private const int LemonCount = 10;

        [Header("Main Buttons")]
        public Button startButton;
        public Button roomCreateButton;
        public Button roomJoinButton;

        [Header("Top Nav Buttons")]
        public Button rulesButton;
        public Button topicButton;

        [Header("Hell Mode")]
        public Button hellModeButton;
        public Image   hellModeIndicator;
        public Text    hellModeLabel;

        [Header("Info Text")]
        public Text infoTextJa;
        public Text infoTextEn;
        public Text hellModeInfoJa;
        public Text hellModeInfoEn;

        [Header("Panels")]
        public GameObject rulesPanel;
        public GameObject topicManagerPanel;

        private static readonly Color BgColor     = new Color(1.00f, 0.96f, 0.60f);
        private static readonly Color LimeColor   = new Color(0.25f, 0.60f, 0.08f);
        private static readonly Color LimeBgColor = new Color(0.65f, 0.92f, 0.28f);
        private static readonly Color SubColor    = new Color(0.50f, 0.36f, 0.00f);

        void Start()
        {
            if (background != null) background.color = BgColor;
            SetupButtons();
            SpawnLemonRain();
            StartCoroutine(PlayIntro());
            SoundManager.Instance?.PlayBGM("title_music");
        }

        void OnEnable()
        {
            RefreshHellMode();
        }

        // ── Buttons ──────────────────────────────────────────────────────────

        void SetupButtons()
        {
            startButton?.onClick.AddListener(OnStart);
            roomCreateButton?.onClick.AddListener(OnRoomCreate);
            roomJoinButton?.onClick.AddListener(OnRoomJoin);
            rulesButton?.onClick.AddListener(OnRules);
            topicButton?.onClick.AddListener(OnTopic);
            hellModeButton?.onClick.AddListener(OnHellToggle);
        }

        void OnStart()
        {
            SoundManager.Instance?.PlaySE("click");
            SoundManager.Instance?.StopBGM();
            GameManager.Instance?.SetPhase(GamePhase.Setup);
        }

        void OnRoomCreate()
        {
            SoundManager.Instance?.PlaySE("click");
            // TODO: room creation flow
        }

        void OnRoomJoin()
        {
            SoundManager.Instance?.PlaySE("click");
            // TODO: room join flow
        }

        void OnRules()
        {
            SoundManager.Instance?.PlaySE("click");
            rulesPanel?.SetActive(true);
        }

        void OnTopic()
        {
            SoundManager.Instance?.PlaySE("click");
            topicManagerPanel?.SetActive(true);
        }

        void OnHellToggle()
        {
            SoundManager.Instance?.PlaySE("click");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LimeMode = !GameManager.Instance.LimeMode;
                RefreshHellMode();
            }
        }

        void RefreshHellMode()
        {
            bool hell = GameManager.Instance?.LimeMode ?? false;

            if (hellModeIndicator != null)
                hellModeIndicator.color = hell ? LimeColor : new Color(0.75f, 0.75f, 0.75f);

            if (hellModeLabel != null)
            {
                hellModeLabel.text  = hell ? "地獄モード中" : "地獄モード";
                hellModeLabel.color = hell ? new Color(0.14f, 0.40f, 0.03f) : SubColor;
            }

            if (hellModeButton != null)
            {
                var img = hellModeButton.GetComponent<Image>();
                if (img != null)
                    img.color = hell ? LimeBgColor : new Color(1f, 1f, 1f, 0.92f);
            }

            if (hellModeInfoJa != null)
            {
                hellModeInfoJa.text  = hell ? "地獄モード：ライフ½・ヘルプなし" : "";
                hellModeInfoJa.color = LimeColor;
            }
            if (hellModeInfoEn != null)
            {
                hellModeInfoEn.text  = hell ? "Hell Mode: ½ life · no help cards" : "";
                hellModeInfoEn.color = LimeColor;
            }
        }

        // ── Lemon Rain ────────────────────────────────────────────────────────

        void SpawnLemonRain()
        {
            if (rainingLemonPrefab == null || lemonRainParent == null) return;

            foreach (Transform c in lemonRainParent) Destroy(c.gameObject);

            float w = lemonRainParent.rect.width;
            float h = lemonRainParent.rect.height;
            if (w <= 0) { w = 1080f; h = 1920f; }

            for (int i = 0; i < LemonCount; i++)
            {
                float size   = Random.Range(55f, 90f);
                float xOff   = Random.Range(-w * 0.44f, w * 0.44f);
                float speed  = Random.Range(65f, 120f);
                float startY = h * 0.5f + size + Random.Range(0f, h);

                var obj  = Instantiate(rainingLemonPrefab, lemonRainParent);
                var item = obj.GetComponent<RainingLemonItem>();
                item?.Init(speed, w, h, startY, xOff, size);
            }
        }

        // ── Intro Animation ───────────────────────────────────────────────────

        IEnumerator PlayIntro()
        {
            // fade in whole content
            if (contentGroup != null)
            {
                contentGroup.alpha = 0f;
                yield return Fade(contentGroup, 1f, 0.35f, 0f);
            }

            // bubble: fade + gentle float loop
            if (titleBubble != null)
            {
                var cg = Cg(titleBubble.gameObject);
                StartCoroutine(Fade(cg, 1f, 0.5f, 0f));
                StartCoroutine(FloatLoop(titleBubble, 10f, 4.0f, 0.2f));
            }

            // word: slide up then float + gentle rock
            if (titleWord != null)
            {
                var cg = Cg(titleWord.gameObject);
                StartCoroutine(Fade(cg, 1f, 0.3f, 0.2f));
                StartCoroutine(Slide(titleWord, new Vector2(0f, -30f), Vector2.zero, 0.55f, 0.2f));
                yield return new WaitForSeconds(0.75f);
                StartCoroutine(FloatLoop(titleWord, 7f, 1.6f, 0f));
                StartCoroutine(RockLoop(titleWord, 2.5f, 3.2f, 0f));
            }

            // lemon: spring drop then float + squish pulse
            if (titleLemon != null)
            {
                var cg = Cg(titleLemon.gameObject);
                StartCoroutine(Fade(cg, 1f, 0.25f, 0.4f));
                StartCoroutine(SpringDrop(titleLemon, -50f, 0f, 0.45f, 0.5f, 0.4f));
                yield return new WaitForSeconds(1.1f);
                StartCoroutine(FloatLoop(titleLemon, 11f, 1.4f, 0f));
                StartCoroutine(RockLoop(titleLemon, 1.8f, 3.5f, 0f));
                StartCoroutine(SquishLoop(titleLemon, 0.055f, 1.9f, 0f));
            }

            // start button: pulse scale
            if (startButton != null)
                StartCoroutine(SquishLoop(startButton.transform as RectTransform, 0.05f, 1.6f, 0.8f));
        }

        // ── Coroutine helpers ─────────────────────────────────────────────────

        static CanvasGroup Cg(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            return cg;
        }

        IEnumerator Fade(CanvasGroup cg, float to, float dur, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            float from = cg.alpha, t = 0f;
            while (t < dur) { t += Time.deltaTime; cg.alpha = Mathf.Lerp(from, to, t / dur); yield return null; }
            cg.alpha = to;
        }

        IEnumerator Slide(RectTransform rt, Vector2 from, Vector2 to, float dur, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                rt.anchoredPosition = Vector2.Lerp(from, to, 1f - Mathf.Pow(1f - t / dur, 3f));
                yield return null;
            }
            rt.anchoredPosition = to;
        }

        IEnumerator SpringDrop(RectTransform rt, float fromY, float toY,
                                float response, float damping, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            float vel = 0f, cur = fromY, elapsed = 0f;
            while ((Mathf.Abs(cur - toY) > 0.5f || Mathf.Abs(vel) > 0.5f) && elapsed < 3f)
            {
                float force = -(cur - toY) / (response * response);
                vel   += force * Time.deltaTime;
                vel   *= Mathf.Pow(1f - damping, Time.deltaTime * 60f);
                cur   += vel * Time.deltaTime * 60f;
                elapsed += Time.deltaTime;
                var p = rt.anchoredPosition; p.y = cur; rt.anchoredPosition = p;
                yield return null;
            }
            var fp = rt.anchoredPosition; fp.y = toY; rt.anchoredPosition = fp;
        }

        IEnumerator FloatLoop(RectTransform rt, float amp, float period, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            Vector2 origin = rt.anchoredPosition;
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime;
                rt.anchoredPosition = origin + Vector2.up * Mathf.Sin(t * Mathf.PI * 2f / period) * amp;
                yield return null;
            }
        }

        IEnumerator RockLoop(RectTransform rt, float deg, float period, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime;
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 2f / period) * deg);
                yield return null;
            }
        }

        IEnumerator SquishLoop(RectTransform rt, float amt, float period, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime;
                float s = Mathf.Sin(t * Mathf.PI * 2f / period) * amt;
                rt.localScale = new Vector3(1f + s, 1f - s * 0.7f, 1f);
                yield return null;
            }
        }
    }
}
