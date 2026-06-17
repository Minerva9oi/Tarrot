using System.IO;
using Tarot.Cards;
using Tarot.RuntimeDeck;
using UnityEditor;
using UnityEngine;

namespace Tarot.EditorTools
{
    public static class DefaultCardDeckArtBuilder
    {
        private const string FrontsFolder = "Assets/Art/CardDecks/Default/Fronts";
        private const string OutputFolder = "Assets/Resources/CardDecks";
        private const string OutputPath = OutputFolder + "/DefaultCardDeckArt.asset";
        private const float CardPixelsPerUnit = 960f;

        public static CardDeckArtData CreateOrUpdate()
        {
            Directory.CreateDirectory(OutputFolder);

            var deckArt = AssetDatabase.LoadAssetAtPath<CardDeckArtData>(OutputPath);
            if (deckArt == null)
            {
                deckArt = ScriptableObject.CreateInstance<CardDeckArtData>();
                AssetDatabase.CreateAsset(deckArt, OutputPath);
            }

            var serializedDeck = new SerializedObject(deckArt);
            serializedDeck.FindProperty("cardBackSprite").objectReferenceValue = null;

            var entries = serializedDeck.FindProperty("frontSprites");
            entries.ClearArray();

            var cards = TarotRuntimeDeck.Cards;
            for (var index = 0; index < cards.Count; index++)
            {
                var sprite = LoadFrontSprite(cards[index]);
                if (sprite == null)
                {
                    Debug.LogWarning($"Missing default card art for {cards[index].CardId}");
                    continue;
                }

                entries.InsertArrayElementAtIndex(entries.arraySize);
                var entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                entry.FindPropertyRelative("cardId").stringValue = cards[index].CardId;
                entry.FindPropertyRelative("frontSprite").objectReferenceValue = sprite;
            }

            serializedDeck.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(deckArt);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return deckArt;
        }

        private static Sprite LoadFrontSprite(TarotRuntimeCard card)
        {
            var assetPath = $"{FrontsFolder}/{GetDefaultArtFileName(card)}.png";
            ConfigureTexture(assetPath);
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void ConfigureTexture(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            {
                return;
            }

            var changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, CardPixelsPerUnit))
            {
                importer.spritePixelsPerUnit = CardPixelsPerUnit;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static string GetDefaultArtFileName(TarotRuntimeCard card)
        {
            if (card.ArcanaType == ArcanaType.Major)
            {
                return $"major_{card.Number:00}";
            }

            return $"{GetSuitPrefix(card.Suit)}_{card.Number:00}";
        }

        private static string GetSuitPrefix(TarotSuit suit)
        {
            return suit switch
            {
                TarotSuit.Wands => "wands",
                TarotSuit.Cups => "cups",
                TarotSuit.Swords => "swords",
                TarotSuit.Pentacles => "pentacles",
                _ => "unknown"
            };
        }
    }
}
