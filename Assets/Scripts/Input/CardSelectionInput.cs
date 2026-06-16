namespace Tarot.Input
{
    public enum CardSelectionAction
    {
        BrowsePrevious,
        BrowseNext,
        Highlight,
        Select,
        Cancel,
        Confirm
    }

    public readonly struct CardSelectionInput
    {
        public CardSelectionInput(CardSelectionAction action, int cardIndex)
        {
            Action = action;
            CardIndex = cardIndex;
        }

        public CardSelectionAction Action { get; }
        public int CardIndex { get; }
    }

    public interface ICardSelectionInputSource
    {
        bool TryReadInput(out CardSelectionInput input);
    }
}

