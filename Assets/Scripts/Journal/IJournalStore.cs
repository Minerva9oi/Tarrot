using System.Collections.Generic;

namespace Tarot.Journal
{
    public interface IJournalStore
    {
        IReadOnlyList<JournalEntry> LoadEntries();
        void SaveEntry(JournalEntry entry);
    }
}

