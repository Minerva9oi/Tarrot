using System.Collections.Generic;
using UnityEngine;

namespace Tarot.RuntimeDeck
{
    public static class TarotDeckShuffler
    {
        public static List<TarotRuntimeCard> CreateShuffledCopy(IReadOnlyList<TarotRuntimeCard> sourceCards)
        {
            var cards = sourceCards != null
                ? new List<TarotRuntimeCard>(sourceCards)
                : new List<TarotRuntimeCard>();

            for (var index = cards.Count - 1; index > 0; index--)
            {
                var swapIndex = Random.Range(0, index + 1);
                (cards[index], cards[swapIndex]) = (cards[swapIndex], cards[index]);
            }

            return cards;
        }
    }
}
