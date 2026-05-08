using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    // GameOver screen: life lemons hit 0
    public class GameOverScreenUI : MonoBehaviour
    {
        public Text gameOverText;
        public Text playerNameText;   // who caused it
        public Text lemonsText;
        public Button retryButton;
        public Button titleButton;

        void OnEnable()
        {
            SoundManager.Instance?.PlaySE("gameover");
            Refresh();
        }

        public void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (gameOverText)    gameOverText.text    = "ゲームオーバー…";
            if (playerNameText)  playerNameText.text  = $"{gm.GameOverPlayerName} さんのターンで終了";
            if (lemonsText)      lemonsText.text      = $"残りライフ：🍋 × {gm.LifeLemons}";
        }

        public void OnRetry()
        {
            SoundManager.Instance?.PlaySE("click");
            SoundManager.Instance?.StopBGM();
            GameManager.Instance?.SetPhase(GamePhase.Setup);
        }

        public void OnTitle()
        {
            SoundManager.Instance?.PlaySE("click");
            GameManager.Instance?.ResetToTitle();
        }
    }
}
