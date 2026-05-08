using System;
using System.Collections.Generic;
using UnityEngine;

namespace BOMBOMLemon
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ── Phase ────────────────────────────────────────────────────────────
        private GamePhase _phase = GamePhase.Title;
        public GamePhase Phase
        {
            get => _phase;
            private set { _phase = value; OnPhaseChanged?.Invoke(value); }
        }

        // ── Core state ───────────────────────────────────────────────────────
        public int PlayerCount = 4;
        public List<string> PlayerNames = new List<string>();
        public int LifeLemons = 0;
        public List<int> PlayerOrder = new List<int>();
        public int CurrentTurnIndex = 0;
        public Topic CurrentTopic;
        public int SecretNumber = 0;
        public int HelpCardsRemaining = 0;
        public RoundResult? LastResult = null;
        public int PendingDiff = 0;
        public bool PendingHelpCardDecision = false;
        public bool PendingGameOver = false;
        public int GameOverPlayerIndex = 0;
        public bool LimeMode = false;
        public bool KeepSettings = false;

        // ── Topic settings ───────────────────────────────────────────────────
        public bool         UseDefaultTopics      = true;
        public HashSet<int> HiddenDefaultTopicIDs = new HashSet<int>();
        public List<UserTopic> UserTopics         = new List<UserTopic>();
        private int _nextUserTopicId = -1;

        // ── In-game player management ────────────────────────────────────────
        public bool RequestingAddPlayer = false;
        public string PlayerRemoveErrorMessage = null;
        public List<PlayerRemoveOption> PlayerRemoveOptions = null;
        public HashSet<int> HelpcardMilestonesGranted = new HashSet<int>();

        // ── Events ───────────────────────────────────────────────────────────
        // Fires on phase change AND on any state mutation (help card, add/remove player, etc.)
        public event Action<GamePhase> OnPhaseChanged;

        // ── Topic shuffling ──────────────────────────────────────────────────
        private List<int> _shuffledTopicQueue = new List<int>();
        private static readonly System.Random _rng = new System.Random();

        // ── Computed properties ──────────────────────────────────────────────
        public int CurrentPlayer =>
            PlayerOrder.Count > 0 && CurrentTurnIndex < PlayerOrder.Count
                ? PlayerOrder[CurrentTurnIndex] : 0;

        public string CurrentPlayerName
        {
            get { int i = CurrentPlayer; return i < PlayerNames.Count ? PlayerNames[i] : $"プレイヤー{i + 1}"; }
        }

        public int InputPlayer => PlayerCount > 0 ? (CurrentPlayer + 1) % PlayerCount : 0;

        public string InputPlayerName
        {
            get { int i = InputPlayer; return i < PlayerNames.Count ? PlayerNames[i] : $"プレイヤー{i + 1}"; }
        }

        public bool IsLastPlayer => CurrentTurnIndex == PlayerOrder.Count - 1;
        public int TotalTurns => PlayerOrder.Count;
        public int CurrentTurnNumber => CurrentTurnIndex + 1;

        public string GameOverPlayerName
        {
            get { int i = GameOverPlayerIndex; return i < PlayerNames.Count ? PlayerNames[i] : $"プレイヤー{i + 1}"; }
        }

        public bool CanAddPlayer => PlayerCount < 24;

        public bool CanRemovePlayer
        {
            get
            {
                if (PlayerCount <= 2) return false;
                int cost = LimeMode ? 2 : 4;
                for (int oi = 0; oi < PlayerOrder.Count; oi++)
                {
                    if (oi < CurrentTurnIndex) return true;      // answered: free
                    if (LifeLemons - cost > 0) return true;
                }
                return false;
            }
        }

        // ── Theme colours (matches GameModel) ────────────────────────────────
        public Color BgColor      => LimeMode ? new Color(0.88f, 0.97f, 0.68f) : new Color(1.00f, 0.96f, 0.60f);
        public Color PrimaryColor => LimeMode ? new Color(0.50f, 0.80f, 0.15f) : new Color(1.00f, 0.87f, 0.10f);
        public Color DarkColor    => LimeMode ? new Color(0.18f, 0.42f, 0.04f) : new Color(0.65f, 0.40f, 0.00f);
        public Color SubColor     => LimeMode ? new Color(0.28f, 0.48f, 0.07f) : new Color(0.50f, 0.36f, 0.00f);
        public Color PrimaryShadowColor => LimeMode ? new Color(0.22f, 0.55f, 0.05f) : new Color(0.75f, 0.55f, 0.00f);

        // ════════════════════════════════════════════════════════════════════
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitDefaultNames();
                LoadTopicSettings();
            }
            else Destroy(gameObject);
        }

        public void SetPhase(GamePhase p) => Phase = p;

        // Notify UI of state change without changing phase (e.g. after UseHelpCard)
        public void NotifyStateChanged() => OnPhaseChanged?.Invoke(Phase);

        // ── Help card calculation ─────────────────────────────────────────────
        public int CalculateHelpCards(int count) => count <= 2 ? 0 : (count + 2) / 3;

        // ── Game flow ─────────────────────────────────────────────────────────
        public void StartGame()
        {
            LifeLemons          = PlayerCount * (LimeMode ? 2 : 4);
            HelpCardsRemaining  = LimeMode ? 0 : CalculateHelpCards(PlayerCount);
            LastResult          = null;
            PendingHelpCardDecision = false;
            PendingGameOver     = false;

            PlayerOrder = new List<int>();
            for (int i = 0; i < PlayerCount; i++) PlayerOrder.Add(i);
            Shuffle(PlayerOrder);
            CurrentTurnIndex = 0;

            // Mark milestones up to starting count as already granted
            HelpcardMilestonesGranted = new HashSet<int>();
            for (int m = 3; m <= PlayerCount; m += 3)
                HelpcardMilestonesGranted.Add(m);

            RebuildTopicQueue();
            StartCurrentPlayerTurn();
        }

        public void StartCurrentPlayerTurn()
        {
            CurrentTopic = PickNextTopic();
            SecretNumber = _rng.Next(0, 100);
            Phase = GamePhase.TopicDisplay;
        }

        public void ChangeCurrentTopic()
        {
            var topics = GetActiveTopics();
            if (topics.Count == 0) return;
            if (_shuffledTopicQueue.Count == 0) RebuildTopicQueue();

            // Skip if same topic would repeat
            if (topics.Count > 1 && _shuffledTopicQueue.Count > 0 &&
                topics[Mathf.Min(_shuffledTopicQueue[0], topics.Count - 1)].Id == CurrentTopic.Id)
            {
                int first = _shuffledTopicQueue[0];
                _shuffledTopicQueue.RemoveAt(0);
                _shuffledTopicQueue.Add(first);
                if (_shuffledTopicQueue.Count == 0) RebuildTopicQueue();
            }
            if (_shuffledTopicQueue.Count == 0) RebuildTopicQueue();
            int idx = _shuffledTopicQueue[0];
            _shuffledTopicQueue.RemoveAt(0);
            CurrentTopic = topics[Mathf.Min(idx, topics.Count - 1)];
            NotifyStateChanged();
        }

        public void ProceedToSecretReveal() => Phase = GamePhase.SecretReveal;
        public void ProceedToDiscussion()   => Phase = GamePhase.Discussion;
        public void ProceedToInput()        => Phase = GamePhase.InputAnswer;

        public void SubmitAnswer(int answer)
        {
            int diff = Mathf.Abs(SecretNumber - answer);
            PendingDiff = diff;

            if (diff == 0)
            {
                LifeLemons += PlayerCount;
                LastResult = new RoundResult
                {
                    Diff = 0, LemonsChanged = PlayerCount,
                    UsedHelpCard = false, IsPerfect = true, ChosenAnswer = answer
                };
                PendingHelpCardDecision = false;
                PendingGameOver = false;
                Phase = GamePhase.Result;
            }
            else if (diff >= 5 && !IsLastPlayer && HelpCardsRemaining > 0)
            {
                // Defer lemon loss until player decides on help card
                LastResult = new RoundResult
                {
                    Diff = diff, LemonsChanged = -diff,
                    UsedHelpCard = false, IsPerfect = false, ChosenAnswer = answer
                };
                PendingHelpCardDecision = true;
                PendingGameOver = false;
                Phase = GamePhase.Result;
            }
            else
            {
                int prev = LifeLemons;
                LifeLemons -= diff;
                LastResult = new RoundResult
                {
                    Diff = diff, LemonsChanged = -Mathf.Min(prev, diff),
                    UsedHelpCard = false, IsPerfect = false, ChosenAnswer = answer
                };
                PendingHelpCardDecision = false;
                ApplyAndCheckGameOver();
            }
        }

        public void UseHelpCard()
        {
            if (LastResult == null) return;
            var r = LastResult.Value;
            HelpCardsRemaining--;
            int capped = Mathf.Min(r.Diff, 4);
            int prev = LifeLemons;
            LifeLemons -= capped;
            LastResult = new RoundResult
            {
                Diff = r.Diff, LemonsChanged = -Mathf.Min(prev, capped),
                UsedHelpCard = true, IsPerfect = false, ChosenAnswer = r.ChosenAnswer
            };
            PendingGameOver = LifeLemons <= 0;
            if (PendingGameOver) { LifeLemons = 0; GameOverPlayerIndex = CurrentPlayer; }
            PendingHelpCardDecision = false;
            NotifyStateChanged();
        }

        public void SkipHelpCard()
        {
            if (LastResult == null) return;
            var r = LastResult.Value;
            int prev = LifeLemons;
            LifeLemons -= r.Diff;
            LastResult = new RoundResult
            {
                Diff = r.Diff, LemonsChanged = -Mathf.Min(prev, r.Diff),
                UsedHelpCard = false, IsPerfect = false, ChosenAnswer = r.ChosenAnswer
            };
            PendingGameOver = LifeLemons <= 0;
            if (PendingGameOver) { LifeLemons = 0; GameOverPlayerIndex = CurrentPlayer; }
            PendingHelpCardDecision = false;
            NotifyStateChanged();
        }

        private void ApplyAndCheckGameOver()
        {
            if (LifeLemons <= 0)
            {
                LifeLemons = 0;
                GameOverPlayerIndex = CurrentPlayer;
                PendingGameOver = true;
            }
            else PendingGameOver = false;
            Phase = GamePhase.Result;
        }

        public void ConfirmGameOver()
        {
            PendingGameOver = false;
            Phase = GamePhase.GameOver;
        }

        public void NextTurnAfterResult()
        {
            if (PendingHelpCardDecision || PendingGameOver) return;
            if (IsLastPlayer)
            {
                Phase = GamePhase.GameClear;
            }
            else
            {
                CurrentTurnIndex++;
                if (CurrentTurnIndex == PlayerOrder.Count - 1 && HelpCardsRemaining > 0)
                    Phase = GamePhase.LastTurnWarning;
                else
                    StartCurrentPlayerTurn();
            }
        }

        public void ProceedFromLastTurnWarning()
        {
            LifeLemons += HelpCardsRemaining;
            HelpCardsRemaining = 0;
            StartCurrentPlayerTurn();
        }

        public void ResetToTitle()
        {
            SoundManager.Instance?.StopBGM();
            Phase = GamePhase.Title;
            LastResult = null;
            PendingHelpCardDecision = false;
            PendingGameOver = false;
        }

        // ── In-game player management ─────────────────────────────────────────
        public void RequestAddPlayer()
        {
            if (!CanAddPlayer) return;
            RequestingAddPlayer = true;
            NotifyStateChanged();
        }

        public void RequestRemovePlayer()
        {
            if (PlayerCount <= 2)
            {
                PlayerRemoveErrorMessage = "最低2人必要です";
                NotifyStateChanged();
                return;
            }
            int cost = LimeMode ? 2 : 4;
            var opts = new List<PlayerRemoveOption>();
            for (int oi = 0; oi < PlayerOrder.Count; oi++)
            {
                int pi = PlayerOrder[oi];
                bool answered = oi < CurrentTurnIndex;
                if (answered || LifeLemons - cost > 0)
                    opts.Add(new PlayerRemoveOption { Id = pi, Name = PlayerNames[pi], HasAnswered = answered });
            }
            if (opts.Count == 0)
            {
                PlayerRemoveErrorMessage = "削除できるプレイヤーがいません";
                NotifyStateChanged();
                return;
            }
            PlayerRemoveOptions = opts;
            NotifyStateChanged();
        }

        public void ConfirmRemovePlayer(int playerIdx)
        {
            PlayerRemoveOptions = null;
            int removeOrderIdx = PlayerOrder.IndexOf(playerIdx);
            if (removeOrderIdx < 0) return;
            bool answered = removeOrderIdx < CurrentTurnIndex;
            bool isCurrent = removeOrderIdx == CurrentTurnIndex;

            if (!answered) LifeLemons = Mathf.Max(0, LifeLemons - (LimeMode ? 2 : 4));

            PlayerOrder.RemoveAt(removeOrderIdx);
            PlayerNames.RemoveAt(playerIdx);
            PlayerCount--;
            for (int i = 0; i < PlayerOrder.Count; i++)
                if (PlayerOrder[i] > playerIdx) PlayerOrder[i]--;

            if (answered)
                CurrentTurnIndex = Mathf.Max(0, CurrentTurnIndex - 1);
            else if (isCurrent)
            {
                if (CurrentTurnIndex >= PlayerOrder.Count)
                    CurrentTurnIndex = Mathf.Max(0, PlayerOrder.Count - 1);
                StartCurrentPlayerTurn();
            }
            NotifyStateChanged();
        }

        public void ConfirmAddPlayer(string name)
        {
            if (!CanAddPlayer) return;
            string trimmed = name.Trim();
            string pName = string.IsNullOrEmpty(trimmed) ? $"プレイヤー{PlayerCount + 1}" : trimmed;
            PlayerNames.Add(pName);
            PlayerCount++;
            PlayerOrder.Add(PlayerCount - 1);
            LifeLemons += LimeMode ? 2 : 4;
            if (!LimeMode)
            {
                for (int m = 3; m <= PlayerCount; m += 3)
                {
                    if (!HelpcardMilestonesGranted.Contains(m))
                    {
                        HelpCardsRemaining++;
                        HelpcardMilestonesGranted.Add(m);
                    }
                }
            }
            RequestingAddPlayer = false;
            NotifyStateChanged();
        }

        // ── Setup-phase player management (free, no lemon/order changes) ─────
        public void SetupAddPlayer(string name = "")
        {
            if (PlayerCount >= 24) return;
            string pName = string.IsNullOrEmpty(name.Trim()) ? $"プレイヤー{PlayerCount + 1}" : name.Trim();
            PlayerNames.Add(pName);
            PlayerCount++;
            NotifyStateChanged();
        }

        public void SetupRemovePlayer(int idx)
        {
            if (PlayerCount <= 2 || idx < 0 || idx >= PlayerNames.Count) return;
            PlayerNames.RemoveAt(idx);
            PlayerCount--;
            NotifyStateChanged();
        }

        // ── Topic management ──────────────────────────────────────────────────
        public List<Topic> GetActiveTopics()
        {
            var result = new List<Topic>();
            if (UseDefaultTopics)
                foreach (var t in TopicData.AllTopics)
                    if (!HiddenDefaultTopicIDs.Contains(t.Id))
                        result.Add(t);
            foreach (var ut in UserTopics)
                result.Add(ut.ToTopic());
            return result;
        }

        public void SetUseDefaultTopics(bool value)
        {
            UseDefaultTopics = value;
            SaveTopicSettings();
            NotifyStateChanged();
        }

        public void SetHideDefaultTopic(int id, bool hide)
        {
            if (hide) HiddenDefaultTopicIDs.Add(id);
            else      HiddenDefaultTopicIDs.Remove(id);
            SaveTopicSettings();
            NotifyStateChanged();
        }

        public void ShowAllDefaultTopics()
        {
            HiddenDefaultTopicIDs.Clear();
            SaveTopicSettings();
            NotifyStateChanged();
        }

        public void HideAllDefaultTopics()
        {
            foreach (var t in TopicData.AllTopics) HiddenDefaultTopicIDs.Add(t.Id);
            SaveTopicSettings();
            NotifyStateChanged();
        }

        public void AddUserTopic(string jp, string en, TopicCategory cat)
        {
            UserTopics.Add(new UserTopic { Id = _nextUserTopicId--, Japanese = jp, English = en, Category = cat });
            SaveTopicSettings();
            NotifyStateChanged();
        }

        public void UpdateUserTopic(int id, string jp, string en, TopicCategory cat)
        {
            var ut = UserTopics.Find(t => t.Id == id);
            if (ut == null) return;
            ut.Japanese = jp; ut.English = en; ut.Category = cat;
            SaveTopicSettings();
            NotifyStateChanged();
        }

        public void DeleteUserTopic(int id)
        {
            UserTopics.RemoveAll(t => t.Id == id);
            SaveTopicSettings();
            NotifyStateChanged();
        }

        [System.Serializable]
        private class TopicSettingsData
        {
            public bool        useDefaultTopics = true;
            public int[]       hiddenIds        = new int[0];
            public UserTopic[] userTopics       = new UserTopic[0];
            public int         nextId           = -1;
        }

        private void SaveTopicSettings()
        {
            var data = new TopicSettingsData
            {
                useDefaultTopics = UseDefaultTopics,
                hiddenIds        = new int[HiddenDefaultTopicIDs.Count],
                userTopics       = UserTopics.ToArray(),
                nextId           = _nextUserTopicId
            };
            HiddenDefaultTopicIDs.CopyTo(data.hiddenIds);
            PlayerPrefs.SetString("TopicSettings", JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void LoadTopicSettings()
        {
            string json = PlayerPrefs.GetString("TopicSettings", "");
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var data = JsonUtility.FromJson<TopicSettingsData>(json);
                UseDefaultTopics      = data.useDefaultTopics;
                HiddenDefaultTopicIDs = new HashSet<int>(data.hiddenIds ?? new int[0]);
                UserTopics            = new List<UserTopic>(data.userTopics ?? new UserTopic[0]);
                _nextUserTopicId      = data.nextId;
            }
            catch { }
        }

        // ── Internal helpers ──────────────────────────────────────────────────
        private void RebuildTopicQueue()
        {
            var topics = GetActiveTopics();
            int n = topics.Count;
            _shuffledTopicQueue = new List<int>(n);
            for (int i = 0; i < n; i++) _shuffledTopicQueue.Add(i);
            Shuffle(_shuffledTopicQueue);
        }

        private Topic PickNextTopic()
        {
            var topics = GetActiveTopics();
            if (topics.Count == 0) return TopicData.AllTopics[0];
            if (_shuffledTopicQueue.Count == 0) RebuildTopicQueue();

            // Avoid back-to-back repeat
            if (_shuffledTopicQueue.Count > 1)
            {
                int fi = Mathf.Min(_shuffledTopicQueue[0], topics.Count - 1);
                if (topics[fi].Id == CurrentTopic.Id)
                {
                    int first = _shuffledTopicQueue[0];
                    _shuffledTopicQueue.RemoveAt(0);
                    _shuffledTopicQueue.Add(first);
                }
            }
            int idx = Mathf.Min(_shuffledTopicQueue[0], topics.Count - 1);
            _shuffledTopicQueue.RemoveAt(0);
            return topics[idx];
        }

        private void InitDefaultNames()
        {
            PlayerNames.Clear();
            for (int i = 1; i <= PlayerCount; i++) PlayerNames.Add($"プレイヤー{i}");
        }

        private static void Shuffle<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _rng.Next(n + 1);
                T tmp = list[k]; list[k] = list[n]; list[n] = tmp;
            }
        }
    }
}
