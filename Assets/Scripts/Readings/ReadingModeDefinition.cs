using UnityEngine;

namespace Tarot.Readings
{
    public enum ReadingModeId
    {
        Daily,
        ThreeCard
    }

    [CreateAssetMenu(fileName = "ReadingMode", menuName = "Tarot/Reading Mode")]
    public sealed class ReadingModeDefinition : ScriptableObject
    {
        [SerializeField] private ReadingModeId modeId;
        [SerializeField] private string localizationKey = string.Empty;
        [SerializeField] private int cardsToDraw = 1;
        [SerializeField] private bool requiresQuestion;

        public ReadingModeId ModeId => modeId;
        public string LocalizationKey => localizationKey;
        public int CardsToDraw => cardsToDraw;
        public bool RequiresQuestion => requiresQuestion;

        private void OnValidate()
        {
            localizationKey = localizationKey.Trim();
            cardsToDraw = Mathf.Max(1, cardsToDraw);
        }
    }
}

