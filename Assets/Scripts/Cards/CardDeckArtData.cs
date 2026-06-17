using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tarot.Cards
{
    [CreateAssetMenu(fileName = "CardDeckArtData", menuName = "Tarot/Card Deck Art Data")]
    public sealed class CardDeckArtData : ScriptableObject
    {
        private const string DefaultResourcePath = "CardDecks/DefaultCardDeckArt";

        [SerializeField] private Sprite cardBackSprite;
        [SerializeField] private List<CardSpriteEntry> frontSprites = new();

        private Dictionary<string, Sprite> frontSpritesByCardId;

        public Sprite CardBackSprite => cardBackSprite;

        public static CardDeckArtData LoadDefault()
        {
            return Resources.Load<CardDeckArtData>(DefaultResourcePath);
        }

        public Sprite GetFrontSprite(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                return null;
            }

            frontSpritesByCardId ??= BuildLookup();
            return frontSpritesByCardId.TryGetValue(cardId, out var sprite) ? sprite : null;
        }

        private Dictionary<string, Sprite> BuildLookup()
        {
            var lookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            foreach (var entry in frontSprites)
            {
                if (!string.IsNullOrWhiteSpace(entry.CardId) && entry.FrontSprite != null)
                {
                    lookup[entry.CardId] = entry.FrontSprite;
                }
            }

            return lookup;
        }
    }

    [Serializable]
    public sealed class CardSpriteEntry
    {
        [SerializeField] private string cardId;
        [SerializeField] private Sprite frontSprite;

        public CardSpriteEntry(string cardId, Sprite frontSprite)
        {
            this.cardId = cardId;
            this.frontSprite = frontSprite;
        }

        public string CardId => cardId;
        public Sprite FrontSprite => frontSprite;
    }
}
