using System;
using System.Collections.Generic;
using UnityEngine;

namespace BOMBOMLemon
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GamePhase Phase { get; private set; } = GamePhase.Title;

        public int PlayerCount = 4;
        public List<string> PlayerNames = new List<string>();
        public int LifeLemons = 0;
        public List<int> PlayerOrder = new List<int>();
        public int CurrentTurnIndex = 0;
        public Topic CurrentTopic;
        public int SecretNumber = 0;
        public int HelpCardsRemaining = 0;
        public RoundResult? LastResult = null;
        public bool LimeMode = false;
        public bool IsPostSplash = false;

        public event Action<GamePhase> OnPhaseChanged;

        private static readonly System.Random _rng = new System.Random();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitDefaultNames();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SetPhase(GamePhase newPhase)
        {
            Phase = newPhase;
            OnPhaseChanged?.Invoke(newPhase);
        }

        public void StartGame()
        {
            int initialLemons = LimeMode ? PlayerCount / 2 : PlayerCount;
            initialLemons = Mathf.Max(1, initialLemons);
            LifeLemons = initialLemons;

            HelpCardsRemaining = LimeMode ? 0 : Mathf.Max(1, PlayerCount / 4);

            PlayerOrder = new List<int>();
            for (int i = 0; i < PlayerCount; i++) PlayerOrder.Add(i);
            Shuffle(PlayerOrder);

            CurrentTurnIndex = 0;
            CurrentTopic = PickRandomTopic();
            SecretNumber = _rng.Next(0, 100);

            SetPhase(GamePhase.TopicDisplay);
        }

        public void SubmitAnswer(int guessedNumber, bool usedHelpCard)
        {
            int diff = Mathf.Abs(guessedNumber - SecretNumber);
            bool isPerfect = diff == 0;
            int lemonsChanged;

            if (isPerfect)
            {
                lemonsChanged = 1;
                LifeLemons += 1;
            }
            else if (usedHelpCard && HelpCardsRemaining > 0)
            {
                lemonsChanged = -Mathf.Min(diff, 4);
                LifeLemons += lemonsChanged;
                HelpCardsRemaining--;
            }
            else
            {
                lemonsChanged = -diff;
                LifeLemons += lemonsChanged;
            }

            LastResult = new RoundResult
            {
                Diff = diff,
                LemonsChanged = lemonsChanged,
                UsedHelpCard = usedHelpCard && !isPerfect,
                IsPerfect = isPerfect,
                ChosenAnswer = guessedNumber
            };

            if (LifeLemons <= 0)
            {
                LifeLemons = 0;
                SetPhase(GamePhase.GameOver);
                return;
            }

            SetPhase(GamePhase.Result);
        }

        public void AdvanceTurn()
        {
            CurrentTurnIndex++;
            if (CurrentTurnIndex >= PlayerCount)
            {
                SetPhase(GamePhase.GameClear);
                return;
            }

            if (CurrentTurnIndex == PlayerCount - 1)
                SetPhase(GamePhase.LastTurnWarning);
            else
            {
                CurrentTopic = PickRandomTopic();
                SecretNumber = _rng.Next(0, 100);
                SetPhase(GamePhase.TopicDisplay);
            }
        }

        public void ResetToTitle()
        {
            Phase = GamePhase.Title;
            CurrentTurnIndex = 0;
            LastResult = null;
        }

        public int CurrentPlayerIndex => PlayerOrder.Count > 0 ? PlayerOrder[CurrentTurnIndex] : 0;

        public string CurrentPlayerName =>
            PlayerNames.Count > CurrentPlayerIndex ? PlayerNames[CurrentPlayerIndex] : $"プレイヤー{CurrentPlayerIndex + 1}";

        private Topic PickRandomTopic()
        {
            var list = TopicData.AllTopics;
            return list[_rng.Next(0, list.Count)];
        }

        private void InitDefaultNames()
        {
            PlayerNames.Clear();
            for (int i = 1; i <= PlayerCount; i++)
                PlayerNames.Add($"プレイヤー{i}");
        }

        private static void Shuffle<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }
}
