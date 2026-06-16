using System.Collections.Generic;
using Tarot.Cards;

namespace Tarot.RuntimeDeck
{
    public static class TarotRuntimeDeck
    {
        public static IReadOnlyList<TarotRuntimeCard> Cards { get; } = CreateCards();

        private static IReadOnlyList<TarotRuntimeCard> CreateCards()
        {
            var cards = new List<TarotRuntimeCard>(78)
            {
                new("major_00_fool", "愚者", "The Fool", ArcanaType.Major, TarotSuit.None, 0),
                new("major_01_magician", "魔术师", "The Magician", ArcanaType.Major, TarotSuit.None, 1),
                new("major_02_high_priestess", "女祭司", "The High Priestess", ArcanaType.Major, TarotSuit.None, 2),
                new("major_03_empress", "皇后", "The Empress", ArcanaType.Major, TarotSuit.None, 3),
                new("major_04_emperor", "皇帝", "The Emperor", ArcanaType.Major, TarotSuit.None, 4),
                new("major_05_hierophant", "教皇", "The Hierophant", ArcanaType.Major, TarotSuit.None, 5),
                new("major_06_lovers", "恋人", "The Lovers", ArcanaType.Major, TarotSuit.None, 6),
                new("major_07_chariot", "战车", "The Chariot", ArcanaType.Major, TarotSuit.None, 7),
                new("major_08_strength", "力量", "Strength", ArcanaType.Major, TarotSuit.None, 8),
                new("major_09_hermit", "隐者", "The Hermit", ArcanaType.Major, TarotSuit.None, 9),
                new("major_10_wheel_of_fortune", "命运之轮", "Wheel of Fortune", ArcanaType.Major, TarotSuit.None, 10),
                new("major_11_justice", "正义", "Justice", ArcanaType.Major, TarotSuit.None, 11),
                new("major_12_hanged_man", "倒吊人", "The Hanged Man", ArcanaType.Major, TarotSuit.None, 12),
                new("major_13_death", "死神", "Death", ArcanaType.Major, TarotSuit.None, 13),
                new("major_14_temperance", "节制", "Temperance", ArcanaType.Major, TarotSuit.None, 14),
                new("major_15_devil", "恶魔", "The Devil", ArcanaType.Major, TarotSuit.None, 15),
                new("major_16_tower", "高塔", "The Tower", ArcanaType.Major, TarotSuit.None, 16),
                new("major_17_star", "星星", "The Star", ArcanaType.Major, TarotSuit.None, 17),
                new("major_18_moon", "月亮", "The Moon", ArcanaType.Major, TarotSuit.None, 18),
                new("major_19_sun", "太阳", "The Sun", ArcanaType.Major, TarotSuit.None, 19),
                new("major_20_judgement", "审判", "Judgement", ArcanaType.Major, TarotSuit.None, 20),
                new("major_21_world", "世界", "The World", ArcanaType.Major, TarotSuit.None, 21)
            };

            AddSuit(cards, TarotSuit.Wands, "权杖", "Wands", "wands");
            AddSuit(cards, TarotSuit.Cups, "圣杯", "Cups", "cups");
            AddSuit(cards, TarotSuit.Swords, "宝剑", "Swords", "swords");
            AddSuit(cards, TarotSuit.Pentacles, "星币", "Pentacles", "pentacles");

            return cards;
        }

        private static void AddSuit(List<TarotRuntimeCard> cards, TarotSuit suit, string chineseSuit, string englishSuit, string idPrefix)
        {
            cards.Add(new TarotRuntimeCard($"{idPrefix}_01_ace", $"{chineseSuit}王牌", $"Ace of {englishSuit}", ArcanaType.Minor, suit, 1));

            for (var number = 2; number <= 10; number++)
            {
                cards.Add(new TarotRuntimeCard($"{idPrefix}_{number:00}", $"{chineseSuit}{number}", $"{number} of {englishSuit}", ArcanaType.Minor, suit, number));
            }

            cards.Add(new TarotRuntimeCard($"{idPrefix}_11_page", $"{chineseSuit}侍从", $"Page of {englishSuit}", ArcanaType.Minor, suit, 11));
            cards.Add(new TarotRuntimeCard($"{idPrefix}_12_knight", $"{chineseSuit}骑士", $"Knight of {englishSuit}", ArcanaType.Minor, suit, 12));
            cards.Add(new TarotRuntimeCard($"{idPrefix}_13_queen", $"{chineseSuit}皇后", $"Queen of {englishSuit}", ArcanaType.Minor, suit, 13));
            cards.Add(new TarotRuntimeCard($"{idPrefix}_14_king", $"{chineseSuit}国王", $"King of {englishSuit}", ArcanaType.Minor, suit, 14));
        }
    }
}
