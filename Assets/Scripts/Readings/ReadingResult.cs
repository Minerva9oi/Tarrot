using System;
using System.Collections.Generic;
using Tarot.Cards;

namespace Tarot.Readings
{
    [Serializable]
    public sealed class ReadingResult
    {
        public ReadingResult(ReadingModeId modeId, string question, IReadOnlyList<DrawnCard> cards, string interpretation)
        {
            ModeId = modeId;
            Question = question ?? string.Empty;
            Cards = cards;
            Interpretation = interpretation ?? string.Empty;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public ReadingModeId ModeId { get; }
        public string Question { get; }
        public IReadOnlyList<DrawnCard> Cards { get; }
        public string Interpretation { get; }
        public DateTime CreatedAtUtc { get; }
    }
}

