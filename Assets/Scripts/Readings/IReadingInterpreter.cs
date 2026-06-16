using System.Collections.Generic;
using Tarot.Cards;

namespace Tarot.Readings
{
    public interface IReadingInterpreter
    {
        string CreateInterpretation(ReadingRequest request, IReadOnlyList<DrawnCard> cards);
    }
}

