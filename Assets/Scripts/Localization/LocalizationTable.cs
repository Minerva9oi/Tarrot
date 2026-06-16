using System.Collections.Generic;
using UnityEngine;

namespace Tarot.Localization
{
    [CreateAssetMenu(fileName = "LocalizationTable", menuName = "Tarot/Localization Table")]
    public sealed class LocalizationTable : ScriptableObject
    {
        [SerializeField] private List<LocalizedTextEntry> entries = new();

        public string GetText(string key, LocaleId locale)
        {
            foreach (var entry in entries)
            {
                if (entry.Key != key)
                {
                    continue;
                }

                return locale == LocaleId.SimplifiedChinese
                    ? entry.SimplifiedChinese
                    : entry.English;
            }

            return key;
        }
    }
}

