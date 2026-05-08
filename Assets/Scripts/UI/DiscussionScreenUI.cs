using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    // Discussion screen: current player answers the topic, everyone discusses the number
    public class DiscussionScreenUI : MonoBehaviour
    {
        public Text playerLabel;    // "{name}さんが答えます"
        public Text categoryLabel;
        public Text topicText;
        public Text lifeLabel;      // life lemons display
        public Button inputButton;  // → InputAnswer

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

            if (playerLabel)   playerLabel.text   = $"{gm.CurrentPlayerName} さんが答えます";
            if (categoryLabel) categoryLabel.text =
                $"{CategoryLabels.LabelLowJa(gm.CurrentTopic.Category)}  ←→  {CategoryLabels.LabelHighJa(gm.CurrentTopic.Category)}";
            if (topicText)     topicText.text     = gm.CurrentTopic.Japanese ?? "";
            if (lifeLabel)     lifeLabel.text     = $"🍋 × {gm.LifeLemons}";
        }

        public void OnProceed()
        {
            SoundManager.Instance?.PlaySE("click");
            GameManager.Instance?.ProceedToInput();
        }
    }
}
