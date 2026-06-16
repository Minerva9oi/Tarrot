using System;
using System.Collections.Generic;
using Tarot.Cards;
using Tarot.Readings;

namespace Tarot.Journal
{
    [Serializable]
    public sealed class JournalEntry
    {
        public string EntryId = string.Empty;
        public string CreatedAtUtc = string.Empty;
        public ReadingModeId ModeId;
        public string Question = string.Empty;
        public List<JournalCardEntry> Cards = new();
        public string Interpretation = string.Empty;
        public string DeckId = string.Empty;
        public string CardBackId = string.Empty;
        public string BackgroundId = string.Empty;
    }

    [Serializable]
    public sealed class JournalCardEntry
    {
        public string CardId = string.Empty;
        public TarotOrientation Orientation;
    }
}

