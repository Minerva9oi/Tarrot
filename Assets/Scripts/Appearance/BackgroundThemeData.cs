using UnityEngine;

namespace Tarot.Appearance
{
    [CreateAssetMenu(fileName = "BackgroundTheme", menuName = "Tarot/Background Theme")]
    public sealed class BackgroundThemeData : ScriptableObject
    {
        [SerializeField] private string themeId = "default_starfield";
        [SerializeField] private string localizationKey = "background.default_starfield";
        [SerializeField] private bool supportsReadingEffects = true;
        [SerializeField] private Color baseColor = Color.black;
        [SerializeField] private Color accentColor = new(0.83f, 0.88f, 1f);
        [SerializeField] private Sprite preview;

        public string ThemeId => themeId;
        public string LocalizationKey => localizationKey;
        public bool SupportsReadingEffects => supportsReadingEffects;
        public Color BaseColor => baseColor;
        public Color AccentColor => accentColor;
        public Sprite Preview => preview;

        private void OnValidate()
        {
            themeId = themeId.Trim();
            localizationKey = localizationKey.Trim();
        }
    }
}
