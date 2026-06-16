using Tarot.Cards;

namespace Tarot.RuntimeDeck
{
    public readonly struct TarotRuntimeCard
    {
        public TarotRuntimeCard(string cardId, string chineseName, string englishName, ArcanaType arcanaType, TarotSuit suit, int number)
        {
            CardId = cardId;
            ChineseName = chineseName;
            EnglishName = englishName;
            ArcanaType = arcanaType;
            Suit = suit;
            Number = number;
        }

        public string CardId { get; }
        public string ChineseName { get; }
        public string EnglishName { get; }
        public ArcanaType ArcanaType { get; }
        public TarotSuit Suit { get; }
        public int Number { get; }
    }
}
