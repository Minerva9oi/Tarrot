using UnityEngine;

namespace Tarot.Appearance
{
    [CreateAssetMenu(fileName = "AppearanceTheme", menuName = "Tarot/Appearance Theme")]
    public sealed class AppearanceThemeData : ScriptableObject
    {
        [SerializeField] private string themeId = string.Empty;
        [SerializeField] private string localizationKey = string.Empty;
        [SerializeField] private Sprite cardBack;
        [SerializeField] private Sprite background;
        [SerializeField] private Color accentColor = new(0.78f, 0.68f, 0.52f);

        public string ThemeId => themeId;
        public string LocalizationKey => localizationKey;
        public Sprite CardBack => cardBack;
        public Sprite Background => background;
        public Color AccentColor => accentColor;

        private void OnValidate()
        {
            themeId = themeId.Trim();
            localizationKey = localizationKey.Trim();
        }
    }
}

