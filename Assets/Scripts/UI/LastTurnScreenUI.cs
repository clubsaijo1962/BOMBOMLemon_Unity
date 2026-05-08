using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    // LastTurnWarning screen: warns that help cards convert to lemons before last turn
    public class LastTurnScreenUI : MonoBehaviour
    {
        public Text playerLabel;   // "{name}さんのターン（最後）"
        public Text helpCardText;  // help card → lemon info
        public Button proceedButton;

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
            if (gm == null) return;

            if (playerLabel)
                playerLabel.text = $"最後のターン！\n{gm.CurrentPlayerName} さんへ";

            if (helpCardText)
            {
                if (gm.HelpCardsRemaining > 0)
                    helpCardText.text =
                        $"ヘルプカード {gm.HelpCardsRemaining} 枚 → 🍋 +{gm.HelpCardsRemaining}";
                else
                    helpCardText.text = "";
            }
        }

        public void OnProceed()
        {
            SoundManager.Instance?.PlaySE("click");
            GameManager.Instance?.ProceedFromLastTurnWarning();
        }
    }
}
