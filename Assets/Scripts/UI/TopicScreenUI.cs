using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    // TopicDisplay screen: current player sees the topic, can change it, then proceeds
    public class TopicScreenUI : MonoBehaviour
    {
        public Text playerLabel;      // "{name}さんへ"
        public Text categoryLabel;    // category name
        public Text topicText;        // topic body
        public Button changeButton;
        public Button nextButton;

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

            if (playerLabel)   playerLabel.text   = $"{gm.CurrentPlayerName} さんへ";
            if (categoryLabel) categoryLabel.text =
                $"{CategoryLabels.LabelLowJa(gm.CurrentTopic.Category)}  ←→  {CategoryLabels.LabelHighJa(gm.CurrentTopic.Category)}";
            if (topicText)     topicText.text     = gm.CurrentTopic.Japanese ?? "";
        }

        public void OnChange()
        {
            SoundManager.Instance?.PlaySE("click");
            GameManager.Instance?.ChangeCurrentTopic();
        }

        public void OnNext()
        {
            SoundManager.Instance?.PlaySE("click");
            GameManager.Instance?.ProceedToSecretReveal();
        }
    }
}
