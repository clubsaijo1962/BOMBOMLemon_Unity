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

    public struct PlayerRemoveOption
    {
        public int Id;          // playerNames index
        public string Name;
        public bool HasAnswered;
    }

    public struct RoundResult
    {
        public int Diff;
        public int LemonsChanged;
        public bool UsedHelpCard;
        public bool IsPerfect;
        public int ChosenAnswer;
    }

    public enum TopicCategory
    {
        Necessary,
        Popular,
        Delicious,
        Expensive,
        Sad,
        Surprising,
        Annoying,
        Happy,
        Big,
        Cute,
    }

    public static class CategoryLabels
    {
        public static string LabelLowJa(TopicCategory c) => c switch
        {
            TopicCategory.Necessary  => "なくても平気",
            TopicCategory.Popular    => "全然人気なさそう",
            TopicCategory.Delicious  => "全然食べたくない",
            TopicCategory.Expensive  => "わりとお手頃",
            TopicCategory.Sad        => "全然悲しくない",
            TopicCategory.Surprising => "全然驚かない",
            TopicCategory.Annoying   => "全然気にならない",
            TopicCategory.Happy      => "全然嬉しくない",
            TopicCategory.Big        => "かなり小さめ",
            TopicCategory.Cute       => "全然可愛くない",
            _                        => ""
        };

        public static string LabelHighJa(TopicCategory c) => c switch
        {
            TopicCategory.Necessary  => "ないと困る",
            TopicCategory.Popular    => "すごく人気がありそう",
            TopicCategory.Delicious  => "すごく食べたい",
            TopicCategory.Expensive  => "かなり高め",
            TopicCategory.Sad        => "すごく悲しい",
            TopicCategory.Surprising => "すごく驚く",
            TopicCategory.Annoying   => "すごく気になる",
            TopicCategory.Happy      => "すごく嬉しい",
            TopicCategory.Big        => "かなり大きめ",
            TopicCategory.Cute       => "すごく可愛い",
            _                        => ""
        };

        public static string LabelLowEn(TopicCategory c) => c switch
        {
            TopicCategory.Necessary  => "Fine without it",
            TopicCategory.Popular    => "Not popular at all",
            TopicCategory.Delicious  => "Don't want it at all",
            TopicCategory.Expensive  => "Pretty affordable",
            TopicCategory.Sad        => "Not sad at all",
            TopicCategory.Surprising => "Not surprising at all",
            TopicCategory.Annoying   => "Not bothered at all",
            TopicCategory.Happy      => "Not happy at all",
            TopicCategory.Big        => "Quite small",
            TopicCategory.Cute       => "Not cute at all",
            _                        => ""
        };

        public static string LabelHighEn(TopicCategory c) => c switch
        {
            TopicCategory.Necessary  => "Can't live without it",
            TopicCategory.Popular    => "Super popular",
            TopicCategory.Delicious  => "Want it so much",
            TopicCategory.Expensive  => "Quite expensive",
            TopicCategory.Sad        => "Incredibly sad",
            TopicCategory.Surprising => "Super surprising",
            TopicCategory.Annoying   => "Really bothered",
            TopicCategory.Happy      => "Incredibly happy",
            TopicCategory.Big        => "Quite large",
            TopicCategory.Cute       => "Incredibly cute",
            _                        => ""
        };
    }

    public struct Topic
    {
        public int Id;
        public string Japanese;
        public string English;
        public TopicCategory Category;

        public string LabelLowJa  => CategoryLabels.LabelLowJa(Category);
        public string LabelHighJa => CategoryLabels.LabelHighJa(Category);
        public string LabelLowEn  => CategoryLabels.LabelLowEn(Category);
        public string LabelHighEn => CategoryLabels.LabelHighEn(Category);
    }

    [System.Serializable]
    public class UserTopic
    {
        public int           Id;       // always negative to distinguish from defaults
        public string        Japanese;
        public string        English;
        public TopicCategory Category;

        public Topic ToTopic() => new Topic
        {
            Id       = Id,
            Japanese = Japanese ?? "",
            English  = English  ?? "",
            Category = Category,
        };
    }

    public static class TopicData
    {
        public static readonly List<Topic> AllTopics = new List<Topic>
        {
            new Topic { Id =  0, Japanese = "一人暮らしを始めるときに買う必要なもの",           English = "Things you buy when starting to live alone",                              Category = TopicCategory.Necessary  },
            new Topic { Id =  1, Japanese = "遊園地で人気だと思うもの",                         English = "Things popular in amusement parks",                                       Category = TopicCategory.Popular    },
            new Topic { Id =  2, Japanese = "誰かに作ってもらうと特に嬉しい美味しいもの",       English = "Delicious foods especially when made by someone",                          Category = TopicCategory.Delicious  },
            new Topic { Id =  3, Japanese = "想像以上に満足できる高いもの",                     English = "Expensive things more satisfying than expected",                           Category = TopicCategory.Expensive  },
            new Topic { Id =  4, Japanese = "恋人パートナーで悲しいこと",                       English = "Sad things related to lovers or partners",                                 Category = TopicCategory.Sad        },
            new Topic { Id =  5, Japanese = "食べ物で驚くこと",                                 English = "Surprising foods",                                                         Category = TopicCategory.Surprising },
            new Topic { Id =  6, Japanese = "友達関係でムカつくこと",                           English = "Annoying things in friendships",                                           Category = TopicCategory.Annoying   },
            new Topic { Id =  7, Japanese = "学生時代の嬉しいこと",                             English = "Happy school memories",                                                    Category = TopicCategory.Happy      },
            new Topic { Id =  8, Japanese = "友達との関係で悲しいこと",                         English = "Sad things in friendships",                                                Category = TopicCategory.Sad        },
            new Topic { Id =  9, Japanese = "仕事中に驚くこと",                                 English = "Surprising things at work",                                                Category = TopicCategory.Surprising },
            new Topic { Id = 10, Japanese = "家族に対してムカつくこと",                         English = "Annoying things about family",                                             Category = TopicCategory.Annoying   },
            new Topic { Id = 11, Japanese = "人から褒められて嬉しいこと",                       English = "Things that make you happy when praised",                                  Category = TopicCategory.Happy      },
            new Topic { Id = 12, Japanese = "家族に関して悲しいこと",                           English = "Sad things about family",                                                  Category = TopicCategory.Sad        },
            new Topic { Id = 13, Japanese = "初めて訪れた場所で驚くこと",                       English = "Surprising things when visiting a place for the first time",               Category = TopicCategory.Surprising },
            new Topic { Id = 14, Japanese = "恋人パートナーにムカつくこと",                     English = "Annoying things about lovers or partners",                                 Category = TopicCategory.Annoying   },
            new Topic { Id = 15, Japanese = "プレゼントでもらって嬉しいもの",                   English = "Things that make you happy to receive as gifts",                           Category = TopicCategory.Happy      },
            new Topic { Id = 16, Japanese = "引っ越しで悲しいこと",                             English = "Sad things about moving",                                                  Category = TopicCategory.Sad        },
            new Topic { Id = 17, Japanese = "思わぬ偶然で驚くこと",                             English = "Surprising coincidences",                                                  Category = TopicCategory.Surprising },
            new Topic { Id = 18, Japanese = "店員さんやサービスでムカつくこと",                 English = "Annoying things about clerks or service",                                  Category = TopicCategory.Annoying   },
            new Topic { Id = 19, Japanese = "家族にされて嬉しいこと",                           English = "Things that make you happy when done by family",                           Category = TopicCategory.Happy      },
            new Topic { Id = 20, Japanese = "裏切られて悲しいこと",                             English = "Sad things about betrayal",                                                Category = TopicCategory.Sad        },
            new Topic { Id = 21, Japanese = "プレゼントで驚くこと",                             English = "Surprising things about gifts",                                            Category = TopicCategory.Surprising },
            new Topic { Id = 22, Japanese = "食事中にムカつくこと",                             English = "Annoying things during meals",                                             Category = TopicCategory.Annoying   },
            new Topic { Id = 23, Japanese = "恋人パートナーにされて嬉しいこと",                 English = "Things that make you happy when done by lovers or partners",                Category = TopicCategory.Happy      },
            new Topic { Id = 24, Japanese = "仲直りできなくて悲しいこと",                       English = "Sad things when you can't reconcile",                                      Category = TopicCategory.Sad        },
            new Topic { Id = 25, Japanese = "初対面で驚くこと",                                 English = "Surprising first meetings",                                                Category = TopicCategory.Surprising },
            new Topic { Id = 26, Japanese = "匂いでムカつくこと",                               English = "Annoying smells",                                                          Category = TopicCategory.Annoying   },
            new Topic { Id = 27, Japanese = "初めて出来るようになって嬉しいこと",               English = "Things that make you happy when you can do something for the first time",  Category = TopicCategory.Happy      },
            new Topic { Id = 28, Japanese = "楽しみにしていたことが中止で悲しいこと",           English = "Sad things when anticipated plans are canceled",                            Category = TopicCategory.Sad        },
            new Topic { Id = 29, Japanese = "友達の言動で驚くこと",                             English = "Surprising things in friends' behavior",                                   Category = TopicCategory.Surprising },
            new Topic { Id = 30, Japanese = "マナーが悪くてムカつくこと",                       English = "Annoying bad manners",                                                     Category = TopicCategory.Annoying   },
            new Topic { Id = 31, Japanese = "努力が報われて嬉しいこと",                         English = "Things that make you happy when efforts are rewarded",                     Category = TopicCategory.Happy      },
            new Topic { Id = 32, Japanese = "映画やドラマで悲しいこと",                         English = "Sad things in movies or dramas",                                           Category = TopicCategory.Sad        },
            new Topic { Id = 33, Japanese = "家族について驚くこと",                             English = "Surprising things about family",                                           Category = TopicCategory.Surprising },
            new Topic { Id = 34, Japanese = "意地悪されてムカつくこと",                         English = "Annoying bullying",                                                        Category = TopicCategory.Annoying   },
            new Topic { Id = 35, Japanese = "お金に関して嬉しいこと",                           English = "Happy things about money",                                                 Category = TopicCategory.Happy      },
            new Topic { Id = 36, Japanese = "旅行で悲しいこと",                                 English = "Sad things about travel",                                                  Category = TopicCategory.Sad        },
            new Topic { Id = 37, Japanese = "恋愛で驚くこと",                                   English = "Surprising things in romance",                                             Category = TopicCategory.Surprising },
            new Topic { Id = 38, Japanese = "お金のことでムカつくこと",                         English = "Annoying money problems",                                                  Category = TopicCategory.Annoying   },
            new Topic { Id = 39, Japanese = "知識や特技を活かせて嬉しいこと",                   English = "Happy things about using knowledge or skills",                             Category = TopicCategory.Happy      },
            new Topic { Id = 40, Japanese = "自分のミスで悲しいこと",                           English = "Sad things caused by your own mistakes",                                   Category = TopicCategory.Sad        },
            new Topic { Id = 41, Japanese = "学生時代で驚くこと",                               English = "Surprising things in school days",                                         Category = TopicCategory.Surprising },
            new Topic { Id = 42, Japanese = "旅行中にムカつくこと",                             English = "Annoying things during travel",                                            Category = TopicCategory.Annoying   },
            new Topic { Id = 43, Japanese = "知らない人にされて嬉しいこと",                     English = "Happy things done by strangers",                                           Category = TopicCategory.Happy      },
            new Topic { Id = 44, Japanese = "準備が無駄になって悲しいこと",                     English = "Sad things when preparations go to waste",                                 Category = TopicCategory.Sad        },
            new Topic { Id = 45, Japanese = "子どもの頃に驚くこと",                             English = "Surprising things in childhood",                                           Category = TopicCategory.Surprising },
            new Topic { Id = 46, Japanese = "音でムカつくこと",                                 English = "Annoying noises",                                                          Category = TopicCategory.Annoying   },
            new Topic { Id = 47, Japanese = "部活や習い事で嬉しいこと",                         English = "Happy things in club activities or lessons",                               Category = TopicCategory.Happy      },
            new Topic { Id = 48, Japanese = "リーダーに求められる必要なもの",                   English = "Things required for leaders",                                              Category = TopicCategory.Necessary  },
            new Topic { Id = 49, Japanese = "BBQで人気だと思うもの",                            English = "Things popular at BBQs",                                                   Category = TopicCategory.Popular    },
            new Topic { Id = 50, Japanese = "お土産でよくもらう美味しいもの",                   English = "Delicious treats you often receive as souvenirs",                          Category = TopicCategory.Delicious  },
            new Topic { Id = 51, Japanese = "アクセサリーやファッションで高いもの",             English = "Expensive accessories or fashion",                                         Category = TopicCategory.Expensive  },
            new Topic { Id = 52, Japanese = "成功するために欠かせない必要なもの",               English = "Things essential for success",                                             Category = TopicCategory.Necessary  },
            new Topic { Id = 53, Japanese = "クラブ活動で人気だと思うもの",                     English = "Things popular in club activities",                                        Category = TopicCategory.Popular    },
            new Topic { Id = 54, Japanese = "大人になって好きになる美味しいもの",               English = "Delicious foods you like as an adult",                                     Category = TopicCategory.Delicious  },
            new Topic { Id = 55, Japanese = "人によって意見が分かれる高いもの",                 English = "Expensive things with divided opinions",                                   Category = TopicCategory.Expensive  },
            new Topic { Id = 56, Japanese = "災害時に役立つ必要なもの",                         English = "Things useful in disasters",                                               Category = TopicCategory.Necessary  },
            new Topic { Id = 57, Japanese = "お祭りで人気だと思うもの",                         English = "Things popular at festivals",                                              Category = TopicCategory.Popular    },
            new Topic { Id = 58, Japanese = "誰かにプレゼントしたくなる美味しいもの",           English = "Delicious foods you want to give as gifts",                                Category = TopicCategory.Delicious  },
            new Topic { Id = 59, Japanese = "高い食べ物や飲み物",                               English = "Expensive foods or drinks",                                                Category = TopicCategory.Expensive  },
            new Topic { Id = 60, Japanese = "ペットと暮らすうえで必要なもの",                   English = "Things essential for living with pets",                                    Category = TopicCategory.Necessary  },
            new Topic { Id = 61, Japanese = "水族館で人気だと思うもの",                         English = "Things popular in aquariums",                                              Category = TopicCategory.Popular    },
            new Topic { Id = 62, Japanese = "お酒と一緒に食べたい美味しいもの",                 English = "Delicious foods you want to eat with alcohol",                             Category = TopicCategory.Delicious  },
            new Topic { Id = 63, Japanese = "友達と盛り上がる高いもの",                         English = "Expensive things to enjoy with friends",                                   Category = TopicCategory.Expensive  },
            new Topic { Id = 64, Japanese = "朝の準備で必要なもの",                             English = "Things needed for morning preparation",                                    Category = TopicCategory.Necessary  },
            new Topic { Id = 65, Japanese = "日本で人気だと思うもの",                           English = "Things popular in Japan",                                                  Category = TopicCategory.Popular    },
            new Topic { Id = 66, Japanese = "子どもの頃によく食べる美味しいもの",               English = "Delicious foods often eaten in childhood",                                 Category = TopicCategory.Delicious  },
            new Topic { Id = 67, Japanese = "自分へのご褒美として買う高いもの",                 English = "Expensive things you buy as a reward for yourself",                        Category = TopicCategory.Expensive  },
            new Topic { Id = 68, Japanese = "旅行に持って行きたい必要なもの",                   English = "Things you want to bring on a trip",                                       Category = TopicCategory.Necessary  },
            new Topic { Id = 69, Japanese = "趣味で人気だと思うもの",                           English = "Things popular as hobbies",                                                Category = TopicCategory.Popular    },
            new Topic { Id = 70, Japanese = "落ち込んだ時に食べたくなる美味しいもの",           English = "Delicious foods you want to eat when feeling down",                        Category = TopicCategory.Delicious  },
            new Topic { Id = 71, Japanese = "プレゼントとして渡す高いもの",                     English = "Expensive things given as gifts",                                          Category = TopicCategory.Expensive  },
            new Topic { Id = 72, Japanese = "冬を快適に過ごすために必要なもの",                 English = "Things needed to spend winter comfortably",                                Category = TopicCategory.Necessary  },
            new Topic { Id = 73, Japanese = "アニメで人気だと思うもの",                         English = "Things popular in anime",                                                  Category = TopicCategory.Popular    },
            new Topic { Id = 74, Japanese = "朝ごはんに食べたい美味しいもの",                   English = "Delicious foods you want for breakfast",                                   Category = TopicCategory.Delicious  },
            new Topic { Id = 75, Japanese = "あなたの地元で有名な大きいもの",                   English = "Big things famous in your hometown",                                       Category = TopicCategory.Big        },
            new Topic { Id = 76, Japanese = "勉強や仕事に集中するために必要なもの",             English = "Things needed to concentrate on study or work",                            Category = TopicCategory.Necessary  },
            new Topic { Id = 77, Japanese = "コンビニで人気だと思うもの",                       English = "Things popular in convenience stores",                                     Category = TopicCategory.Popular    },
            new Topic { Id = 78, Japanese = "夜食で食べると最高に美味しいもの",                 English = "Delicious foods that taste best as midnight snacks",                       Category = TopicCategory.Delicious  },
            new Topic { Id = 79, Japanese = "写真に撮って自慢したくなる可愛いもの",             English = "Cute things you want to brag about with photos",                           Category = TopicCategory.Cute       },
            new Topic { Id = 80, Japanese = "子どもの頃から大事にしている必要なもの",           English = "Things you have cherished since childhood",                                Category = TopicCategory.Necessary  },
            new Topic { Id = 81, Japanese = "居酒屋で人気だと思うもの",                         English = "Things popular in Izakaya",                                                Category = TopicCategory.Popular    },
            new Topic { Id = 82, Japanese = "夏祭りで食べたい美味しいもの",                     English = "Delicious foods you want to eat at summer festivals",                      Category = TopicCategory.Delicious  },
            new Topic { Id = 83, Japanese = "つい衝動買いしそうになる高いもの",                 English = "Expensive things you might impulsively buy",                               Category = TopicCategory.Expensive  },
            new Topic { Id = 84, Japanese = "デートで忘れてはいけない必要なもの",               English = "Things you must not forget on a date",                                     Category = TopicCategory.Necessary  },
            new Topic { Id = 85, Japanese = "スイーツで人気だと思うもの",                       English = "Popular sweets",                                                           Category = TopicCategory.Popular    },
            new Topic { Id = 86, Japanese = "旅行先で出会う忘れられない美味しいもの",           English = "Unforgettable delicious foods you encounter while traveling",              Category = TopicCategory.Delicious  },
            new Topic { Id = 87, Japanese = "壊したら泣きたくなる高いもの",                     English = "Expensive things that make you cry if broken",                             Category = TopicCategory.Expensive  },
            new Topic { Id = 88, Japanese = "長生きするために必要なもの",                       English = "Things needed for long life",                                              Category = TopicCategory.Necessary  },
            new Topic { Id = 89, Japanese = "動物園で人気だと思うもの",                         English = "Things popular in zoos",                                                   Category = TopicCategory.Popular    },
            new Topic { Id = 90, Japanese = "コンビニでつい買ってしまう美味しいもの",           English = "Delicious foods you tend to buy at convenience stores",                    Category = TopicCategory.Delicious  },
            new Topic { Id = 91, Japanese = "買って後悔する高いもの",                           English = "Expensive things you regret buying",                                       Category = TopicCategory.Expensive  },
        };
    }
}
