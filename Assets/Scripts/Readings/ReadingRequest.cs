using System;

namespace Tarot.Readings
{
    [Serializable]
    public sealed class ReadingRequest
    {
        public ReadingRequest(ReadingModeId modeId, string question)
        {
            ModeId = modeId;
            Question = question ?? string.Empty;
        }

        public ReadingModeId ModeId { get; }
        public string Question { get; }
    }
}

