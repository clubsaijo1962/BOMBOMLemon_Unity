using UnityEngine;
using UnityEngine.UI;

namespace BOMBOMLemon
{
    // InputAnswer screen: input player picks a number 0–99
    public class InputAnswerScreenUI : MonoBehaviour
    {
        public Text   playerLabel;     // "{name}さんが入力"
        public Text   topicText;       // topic reminder (small)
        public Text   numberDisplay;   // big number
        public Slider slider;
        public Button minusTenBtn;
        public Button minusOneBtn;
        public Button plusOneBtn;
        public Button plusTenBtn;
        public Button submitButton;

        int _answer = 50;

        void OnEnable()
        {
            _answer = 50;
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

            if (playerLabel)   playerLabel.text = $"{gm.InputPlayerName} さんが入力";
            if (topicText)     topicText.text   = gm.CurrentTopic.Japanese ?? "";
            if (numberDisplay) numberDisplay.text = _answer.ToString();
            if (slider)        slider.value = _answer;
        }

        // ── Step buttons ─────────────────────────────────────────────────────
        public void OnMinusTen()  { SetAnswer(_answer - 10); SoundManager.Instance?.PlaySE("click"); }
        public void OnMinusOne()  { SetAnswer(_answer -  1); SoundManager.Instance?.PlaySE("click"); }
        public void OnPlusOne()   { SetAnswer(_answer +  1); SoundManager.Instance?.PlaySE("click"); }
        public void OnPlusTen()   { SetAnswer(_answer + 10); SoundManager.Instance?.PlaySE("click"); }

        public void OnSliderChanged(float val) => SetAnswer(Mathf.RoundToInt(val));

        void SetAnswer(int val)
        {
            _answer = Mathf.Clamp(val, 0, 99);
            if (numberDisplay) numberDisplay.text = _answer.ToString();
            if (slider)        slider.value = _answer;
        }

        public void OnSubmit()
        {
            SoundManager.Instance?.PlaySE("piyo");
            GameManager.Instance?.SubmitAnswer(_answer);
        }
    }
}
