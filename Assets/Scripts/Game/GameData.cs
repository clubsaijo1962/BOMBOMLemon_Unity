using System.Collections.Generic;

namespace BOMBOMLemon
{
    public enum GamePhase
    {
        Title,
        Setup,
        TopicDisplay,
        SecretReveal,
        Discussion,
        InputAnswer,
        Result,
        LastTurnWarning,
        GameClear,
        GameOver
    }

    public struct RoundResult
    {
        public int Diff;
        public int LemonsChanged;
        public bool UsedHelpCard;
        public bool IsPerfect;
        public int ChosenAnswer;
    }

    public struct Topic
    {
        public int Id;
        public string Japanese;
        public string English;
        public string LabelLowJa;
        public string LabelLowEn;
        public string LabelHighJa;
        public string LabelHighEn;
    }

    public static class TopicData
    {
        public static readonly List<Topic> AllTopics = new List<Topic>
        {
            new Topic { Id = 0,  Japanese = "この食べ物の辛さ",          English = "Spiciness of this food",         LabelLowJa = "全然辛くない",  LabelLowEn = "Not spicy at all",     LabelHighJa = "激辛",       LabelHighEn = "Super spicy" },
            new Topic { Id = 1,  Japanese = "この人の朝起きる時間",       English = "Time this person wakes up",      LabelLowJa = "深夜",         LabelLowEn = "Late night",            LabelHighJa = "早朝",       LabelHighEn = "Very early" },
            new Topic { Id = 2,  Japanese = "今日の気分",                English = "Mood today",                     LabelLowJa = "最悪",         LabelLowEn = "Terrible",              LabelHighJa = "最高",       LabelHighEn = "Amazing" },
            new Topic { Id = 3,  Japanese = "この人の運動量",             English = "Exercise level of this person",  LabelLowJa = "全くしない",   LabelLowEn = "None at all",           LabelHighJa = "毎日激しく", LabelHighEn = "Very active daily" },
            new Topic { Id = 4,  Japanese = "今の眠気",                  English = "How sleepy right now",           LabelLowJa = "全然眠くない", LabelLowEn = "Not sleepy",            LabelHighJa = "爆睡寸前",   LabelHighEn = "Almost asleep" },
            new Topic { Id = 5,  Japanese = "この人のお酒の強さ",         English = "This person's alcohol tolerance", LabelLowJa = "下戸",        LabelLowEn = "Very weak",             LabelHighJa = "酒豪",       LabelHighEn = "Very strong" },
            new Topic { Id = 6,  Japanese = "この人の几帳面さ",           English = "How organized this person is",   LabelLowJa = "超大雑把",     LabelLowEn = "Very messy",            LabelHighJa = "超几帳面",   LabelHighEn = "Super organized" },
            new Topic { Id = 7,  Japanese = "この人のケチさ",             English = "How frugal this person is",      LabelLowJa = "大盤振る舞い", LabelLowEn = "Very generous",         LabelHighJa = "超ケチ",     LabelHighEn = "Very frugal" },
            new Topic { Id = 8,  Japanese = "この人の寂しがり度",         English = "How lonely this person gets",    LabelLowJa = "一人が好き",   LabelLowEn = "Loves being alone",     LabelHighJa = "常に誰かと", LabelHighEn = "Always needs someone" },
            new Topic { Id = 9,  Japanese = "この人のドジ度",             English = "How clumsy this person is",      LabelLowJa = "完璧",         LabelLowEn = "Very graceful",         LabelHighJa = "超ドジ",     LabelHighEn = "Super clumsy" },
            new Topic { Id = 10, Japanese = "この人の怒りやすさ",         English = "How easily angered",             LabelLowJa = "温厚",         LabelLowEn = "Very calm",             LabelHighJa = "短気",       LabelHighEn = "Very hot-tempered" },
            new Topic { Id = 11, Japanese = "この人の負けず嫌い度",       English = "How competitive this person is", LabelLowJa = "全然気にしない", LabelLowEn = "Not competitive",     LabelHighJa = "絶対負けない", LabelHighEn = "Must win" },
            new Topic { Id = 12, Japanese = "この人の料理の腕",           English = "Cooking skill of this person",  LabelLowJa = "全く作れない", LabelLowEn = "Cannot cook at all",    LabelHighJa = "プロ級",     LabelHighEn = "Professional level" },
            new Topic { Id = 13, Japanese = "この人の涙もろさ",           English = "How easily this person cries",  LabelLowJa = "全然泣かない", LabelLowEn = "Never cries",           LabelHighJa = "すぐ泣く",   LabelHighEn = "Cries easily" },
            new Topic { Id = 14, Japanese = "この人の人見知り度",         English = "How shy this person is",        LabelLowJa = "誰とでも仲良し", LabelLowEn = "Very outgoing",       LabelHighJa = "超人見知り", LabelHighEn = "Very shy" },
            new Topic { Id = 15, Japanese = "この人の方向音痴度",         English = "How bad at directions",         LabelLowJa = "GPS不要",      LabelLowEn = "Never gets lost",       LabelHighJa = "超方向音痴", LabelHighEn = "Always lost" },
            new Topic { Id = 16, Japanese = "この人の節約度",             English = "How much this person saves",    LabelLowJa = "浪費家",       LabelLowEn = "Big spender",           LabelHighJa = "超節約家",   LabelHighEn = "Extreme saver" },
            new Topic { Id = 17, Japanese = "この人の甘党度",             English = "Sweet tooth level",             LabelLowJa = "全く甘いものが嫌い", LabelLowEn = "Hates sweets",   LabelHighJa = "甘いもの大好き", LabelHighEn = "Loves sweets" },
            new Topic { Id = 18, Japanese = "この人のコーヒー消費量",     English = "Coffee consumption level",      LabelLowJa = "飲まない",     LabelLowEn = "Never drinks",          LabelHighJa = "毎日大量",   LabelHighEn = "Large amount daily" },
            new Topic { Id = 19, Japanese = "この人の心配性度",           English = "How much of a worrier",         LabelLowJa = "楽観的",       LabelLowEn = "Very optimistic",       LabelHighJa = "超心配性",   LabelHighEn = "Extreme worrier" },
        };
    }
}
