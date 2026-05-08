using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    // GameClear screen: all turns cleared, show final lemons and options
    public class GameClearScreenUI : MonoBehaviour
    {
        public Text clearMessageText;
        public Text lemonsText;
        public Button retryButton;
        public Button titleButton;

        void OnEnable()
        {
            SoundManager.Instance?.PlayBGM("title_music");
            Refresh();
        }

        public void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (clearMessageText) clearMessageText.text = "ゲームクリア！🎉";
            if (lemonsText)       lemonsText.text       = $"残りライフ：🍋 × {gm.LifeLemons}";
        }

        public void OnRetry()
        {
            SoundManager.Instance?.PlaySE("click");
            SoundManager.Instance?.StopBGM();
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.SetPhase(GamePhase.Setup);
        }

        public void OnTitle()
        {
            SoundManager.Instance?.PlaySE("click");
            GameManager.Instance?.ResetToTitle();
        }
    }
}
