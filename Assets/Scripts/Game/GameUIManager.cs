using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    public class GameUIManager : MonoBehaviour
    {
        public static GameUIManager Instance { get; private set; }

        [Header("Shared")]
        public Image sharedBackground;

        [Header("Screens")]
        public GameObject titlePanel;
        public GameObject setupPanel;
        public GameObject topicDisplayPanel;
        public GameObject secretRevealPanel;
        public GameObject discussionPanel;
        public GameObject inputAnswerPanel;
        public GameObject resultPanel;
        public GameObject lastTurnPanel;
        public GameObject gameClearPanel;
        public GameObject gameOverPanel;

        [Header("Theme – Primary color images (start/next buttons etc.)")]
        public Image[] primaryColorImages;

        [Header("Theme – Dark color images (secondary buttons etc.)")]
        public Image[] darkColorImages;

        void Awake() => Instance = this;

        void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.OnPhaseChanged += OnPhaseChanged;
            OnPhaseChanged(gm.Phase);
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        }

        void OnPhaseChanged(GamePhase phase)
        {
            var gm = GameManager.Instance;

            // ── Background ───────────────────────────────────────────────────
            if (sharedBackground != null && gm != null)
                sharedBackground.color = gm.BgColor;

            // ── Theme-colour images ──────────────────────────────────────────
            if (gm != null)
            {
                var pc = gm.PrimaryColor;
                var dc = gm.DarkColor;
                if (primaryColorImages != null)
                    foreach (var img in primaryColorImages) if (img) img.color = pc;
                if (darkColorImages != null)
                    foreach (var img in darkColorImages)   if (img) img.color = dc;
            }

            // ── Panel visibility ─────────────────────────────────────────────
            SetActive(titlePanel,        phase == GamePhase.Title);
            SetActive(setupPanel,        phase == GamePhase.Setup);
            SetActive(topicDisplayPanel, phase == GamePhase.TopicDisplay);
            SetActive(secretRevealPanel, phase == GamePhase.SecretReveal);
            SetActive(discussionPanel,   phase == GamePhase.Discussion);
            SetActive(inputAnswerPanel,  phase == GamePhase.InputAnswer);
            SetActive(resultPanel,       phase == GamePhase.Result);
            SetActive(lastTurnPanel,     phase == GamePhase.LastTurnWarning);
            SetActive(gameClearPanel,    phase == GamePhase.GameClear);
            SetActive(gameOverPanel,     phase == GamePhase.GameOver);
        }

        static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }
    }
}
