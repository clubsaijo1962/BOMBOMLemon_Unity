using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    // SecretReveal screen: input player privately sees the secret number
    public class SecretRevealScreenUI : MonoBehaviour
    {
        public Text playerLabel;      // "{name}さんだけ見てください"
        public Text secretNumberText; // the number (hidden until tapped)
        public Button revealButton;   // tap to reveal
        public Button confirmButton;  // "確認しました" → Discussion

        bool _revealed;

        void OnEnable()
        {
            _revealed = false;
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

            if (playerLabel) playerLabel.text = $"{gm.InputPlayerName} さんだけ見てください";

            // Show "???" until revealed
            if (secretNumberText)
                secretNumberText.text = _revealed ? gm.SecretNumber.ToString() : "？？？";

            if (revealButton)  revealButton.gameObject.SetActive(!_revealed);
            if (confirmButton) confirmButton.gameObject.SetActive(_revealed);
        }

        public void OnReveal()
        {
            SoundManager.Instance?.PlaySE("show");
            _revealed = true;
            Refresh();
        }

        public void OnConfirm()
        {
            SoundManager.Instance?.PlaySE("click");
            GameManager.Instance?.ProceedToDiscussion();
        }
    }
}
