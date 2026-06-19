using UnityEngine;

namespace Tarot.Appearance
{
    public static class CardBackGalleryCatalog
    {
        private const string SelectedIndexKey = "Tarot.SelectedCardBackIndex";
        private const int TextureWidth = 180;
        private const int TextureHeight = 309;
        private const float PixelsPerUnit = 180f;

        private static readonly CardBackStyleDefinition[] definitions =
        {
            new(
                "astral-gold",
                "星盘蓝金",
                "深蓝星盘与金色线条，适合当前默认仪式感。",
                new Color(0.045f, 0.1f, 0.22f, 1f),
                new Color(0.17f, 0.08f, 0.26f, 1f),
                new Color(0.09f, 0.34f, 0.42f, 1f),
                new Color(0.92f, 0.72f, 0.32f, 1f),
                0),
            new(
                "moon-white",
                "月白圣印",
                "白色底面与浅金符号，方便观察明亮粒子。",
                new Color(0.92f, 0.91f, 0.86f, 1f),
                new Color(0.72f, 0.78f, 0.86f, 1f),
                new Color(0.82f, 0.88f, 0.9f, 1f),
                new Color(0.86f, 0.68f, 0.34f, 1f),
                1),
            new(
                "deep-teal",
                "深海绿松",
                "冷绿色层次更明显，适合测试卡背色彩尘化。",
                new Color(0.025f, 0.11f, 0.13f, 1f),
                new Color(0.04f, 0.24f, 0.28f, 1f),
                new Color(0.1f, 0.5f, 0.46f, 1f),
                new Color(0.76f, 0.66f, 0.42f, 1f),
                2),
            new(
                "violet-oracle",
                "紫夜神谕",
                "暗紫与冷蓝渐变，边缘会有更柔和的暗纹。",
                new Color(0.08f, 0.04f, 0.16f, 1f),
                new Color(0.22f, 0.08f, 0.3f, 1f),
                new Color(0.22f, 0.22f, 0.54f, 1f),
                new Color(0.82f, 0.62f, 0.92f, 1f),
                3),
            new(
                "ember-black",
                "黑曜赤金",
                "黑色底与赤金光纹，视觉重量更强。",
                new Color(0.03f, 0.025f, 0.025f, 1f),
                new Color(0.18f, 0.055f, 0.035f, 1f),
                new Color(0.4f, 0.11f, 0.065f, 1f),
                new Color(0.95f, 0.56f, 0.22f, 1f),
                4)
        };

        private static CardBackGalleryItem[] items;

        public static int Count => definitions.Length;

        public static int SelectedIndex
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(SelectedIndexKey, 0), 0, Count - 1);
            set
            {
                PlayerPrefs.SetInt(SelectedIndexKey, Mathf.Clamp(value, 0, Count - 1));
                PlayerPrefs.Save();
            }
        }

        public static CardBackGalleryItem GetItem(int index)
        {
            EnsureItems();
            return items[Mathf.Clamp(index, 0, items.Length - 1)];
        }

        public static Sprite GetSelectedSprite()
        {
            return GetItem(SelectedIndex).Sprite;
        }

        public static CardBackGalleryItem[] GetItems()
        {
            EnsureItems();
            return items;
        }

        private static void EnsureItems()
        {
            if (items != null)
            {
                return;
            }

            items = new CardBackGalleryItem[definitions.Length];
            for (var index = 0; index < definitions.Length; index++)
            {
                var definition = definitions[index];
                items[index] = new CardBackGalleryItem(
                    definition.Id,
                    definition.Title,
                    definition.Description,
                    CreateCardBackSprite(definition));
            }
        }

        private static Sprite CreateCardBackSprite(CardBackStyleDefinition definition)
        {
            var texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < TextureHeight; y++)
            {
                for (var x = 0; x < TextureWidth; x++)
                {
                    texture.SetPixel(x, y, SampleCardBackPixel(definition, x, y));
                }
            }

            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, TextureWidth, TextureHeight),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit);
        }

        private static Color SampleCardBackPixel(CardBackStyleDefinition definition, int x, int y)
        {
            var u = x / (float)(TextureWidth - 1);
            var v = y / (float)(TextureHeight - 1);
            var dx = x - TextureWidth * 0.5f;
            var dy = y - TextureHeight * 0.5f;
            var distance = Mathf.Sqrt(dx * dx + dy * dy);
            var radial = Mathf.Clamp01(distance / 116f);
            var wave = Mathf.Sin((u * (2.4f + definition.PatternSeed * 0.21f) + v * 2.8f) * Mathf.PI) * 0.5f + 0.5f;
            var color = Color.Lerp(definition.BaseColor, definition.SecondaryColor, Mathf.Clamp01(v * 0.72f + radial * 0.24f));
            color = Color.Lerp(color, definition.AccentColor, wave * 0.18f);

            var outerBorder = x < 4 || x >= TextureWidth - 4 || y < 4 || y >= TextureHeight - 4;
            var innerVertical = (x >= 18 && x < 20 || x >= TextureWidth - 20 && x < TextureWidth - 18) && y >= 14 && y < TextureHeight - 14;
            var innerHorizontal = (y >= 14 && y < 16 || y >= TextureHeight - 16 && y < TextureHeight - 14) && x >= 18 && x < TextureWidth - 18;
            var outerDiamond = Mathf.Abs(Mathf.Abs(dx) / 62f + Mathf.Abs(dy) / 104f - 1f) < 0.026f;
            var innerDiamond = Mathf.Abs(Mathf.Abs(dx) / 34f + Mathf.Abs(dy) / 58f - 1f) < 0.03f;
            var centerRing = distance > 25f && distance < 30f;
            var outerRing = distance > 53f && distance < 56f;
            var verticalRay = Mathf.Abs(dx) < 1.8f && Mathf.Abs(dy) < 92f;
            var horizontalRay = Mathf.Abs(dy) < 1.8f && Mathf.Abs(dx) < 54f;
            var diagonalRay = Mathf.Abs(Mathf.Abs(dy) - Mathf.Abs(dx) * (1.22f + definition.PatternSeed * 0.08f)) < 1.8f && distance < 72f;
            var cornerSpark = Mathf.Sin((x * 0.23f + y * 0.17f + definition.PatternSeed * 0.37f) * Mathf.PI) > 0.965f && radial > 0.52f;

            if (outerBorder || innerVertical || innerHorizontal || outerDiamond || centerRing)
            {
                color = definition.LineColor;
            }
            else if (innerDiamond || outerRing || verticalRay || horizontalRay || diagonalRay)
            {
                color = Color.Lerp(definition.LineColor, Color.white, 0.16f);
            }
            else if (cornerSpark)
            {
                color = Color.Lerp(definition.AccentColor, definition.LineColor, 0.45f);
            }

            color.a = 1f;
            return color;
        }

        private readonly struct CardBackStyleDefinition
        {
            public CardBackStyleDefinition(
                string id,
                string title,
                string description,
                Color baseColor,
                Color secondaryColor,
                Color accentColor,
                Color lineColor,
                int patternSeed)
            {
                Id = id;
                Title = title;
                Description = description;
                BaseColor = baseColor;
                SecondaryColor = secondaryColor;
                AccentColor = accentColor;
                LineColor = lineColor;
                PatternSeed = patternSeed;
            }

            public string Id { get; }
            public string Title { get; }
            public string Description { get; }
            public Color BaseColor { get; }
            public Color SecondaryColor { get; }
            public Color AccentColor { get; }
            public Color LineColor { get; }
            public int PatternSeed { get; }
        }
    }

    public sealed class CardBackGalleryItem
    {
        public CardBackGalleryItem(string id, string title, string description, Sprite sprite)
        {
            Id = id;
            Title = title;
            Description = description;
            Sprite = sprite;
        }

        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public Sprite Sprite { get; }
    }
}
