using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    // Result screen: shows outcome, handles help card decision and game over confirmation
    public class ResultScreenUI : MonoBehaviour
    {
        [Header("Result Info")]
        public Text secretLabel;      // "秘密の数字：XX"
        public Text answerLabel;      // "答え：XX"
        public Text diffLabel;        // "差：XX"
        public Text lemonsLabel;      // "🍋 ×{count}"
        public Text resultMessageLabel; // "ピッタリ！" / "+X 🍋" / "-X 🍋"

        [Header("Help Card Panel")]
        public GameObject helpCardPanel;
        public Text helpCardInfoText;
        public Button useHelpCardButton;
        public Button skipHelpCardButton;

        [Header("Game Over Panel")]
        public GameObject gameOverConfirmPanel;
        public Text gameOverConfirmText;
        public Button gameOverConfirmButton;

        [Header("Normal Next")]
        public Button nextTurnButton;

        void OnEnable()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.OnPhaseChanged += OnStateChanged;
            Refresh();
        }

        void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPhaseChanged -= OnStateChanged;
        }

        void OnStateChanged(GamePhase _) => Refresh();

        public void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.LastResult == null) return;
            var r = gm.LastResult.Value;

            // Core result
            if (secretLabel) secretLabel.text = $"秘密の数字：{gm.SecretNumber}";
            if (answerLabel) answerLabel.text  = $"答え：{r.ChosenAnswer}";
            if (diffLabel)   diffLabel.text    = $"差：{r.Diff}";
            if (lemonsLabel) lemonsLabel.text  = $"🍋 × {gm.LifeLemons}";

            if (resultMessageLabel)
            {
                if (r.IsPerfect)
                    resultMessageLabel.text = $"ぴったり！  +{Mathf.Abs(r.LemonsChanged)} 🍋";
                else if (r.LemonsChanged < 0)
                    resultMessageLabel.text = $"{r.LemonsChanged} 🍋";
                else
                    resultMessageLabel.text = $"+{r.LemonsChanged} 🍋";
            }

            // Panel visibility
            bool showHelp     = gm.PendingHelpCardDecision;
            bool showGameOver = gm.PendingGameOver && !showHelp;
            bool showNext     = !showHelp && !showGameOver;

            if (helpCardPanel)        helpCardPanel.SetActive(showHelp);
            if (gameOverConfirmPanel) gameOverConfirmPanel.SetActive(showGameOver);
            if (nextTurnButton)       nextTurnButton.gameObject.SetActive(showNext);

            if (showHelp && helpCardInfoText)
            {
                int capped = Mathf.Min(r.Diff, 4);
                helpCardInfoText.text =
                    $"ヘルプカード使用で {r.Diff} → {capped} (-{capped} 🍋)";
            }

            if (showGameOver && gameOverConfirmText)
                gameOverConfirmText.text = $"ライフが 0 になりました…";
        }

        // ── Callbacks ───────────────────────────────────────────────────────
        public void OnUseHelpCard()
        {
            SoundManager.Instance?.PlaySE("good");
            GameManager.Instance?.UseHelpCard();
        }

        public void OnSkipHelpCard()
        {
            SoundManager.Instance?.PlaySE("bad");
            GameManager.Instance?.SkipHelpCard();
        }

        public void OnConfirmGameOver()
        {
            SoundManager.Instance?.PlaySE("bomb");
            GameManager.Instance?.ConfirmGameOver();
        }

        public void OnNextTurn()
        {
            SoundManager.Instance?.PlaySE("click");
            GameManager.Instance?.NextTurnAfterResult();
        }
    }
}
