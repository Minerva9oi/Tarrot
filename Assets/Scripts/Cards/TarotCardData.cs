using System;
using UnityEngine;

namespace Tarot.Cards
{
    [CreateAssetMenu(fileName = "TarotCard", menuName = "Tarot/Card Data")]
    public sealed class TarotCardData : ScriptableObject
    {
        [SerializeField] private string cardId = string.Empty;
        [SerializeField] private ArcanaType arcanaType;
        [SerializeField] private TarotSuit suit;
        [SerializeField] private int number;
        [SerializeField] private string localizationKey = string.Empty;
        [SerializeField] private Sprite placeholderArt;

        public string CardId => cardId;
        public ArcanaType ArcanaType => arcanaType;
        public TarotSuit Suit => suit;
        public int Number => number;
        public string LocalizationKey => localizationKey;
        public Sprite PlaceholderArt => placeholderArt;

        private void OnValidate()
        {
            cardId = cardId.Trim();
            localizationKey = localizationKey.Trim();
        }
    }

    [Serializable]
    public readonly struct DrawnCard
    {
        public DrawnCard(TarotCardData card, TarotOrientation orientation)
        {
            Card = card;
            Orientation = orientation;
        }

        public TarotCardData Card { get; }
        public TarotOrientation Orientation { get; }
    }
}

