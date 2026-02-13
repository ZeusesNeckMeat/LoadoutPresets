using UnityEngine;

namespace LoadoutPresets
{
    internal static class GameObjectExtensions
    {
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
            return component;
        }
    }

    internal static class CharacterDataExtensions
    {
        public static ECharacter? GetEnumFromDisplayName(this string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return null;

            for (var i = 0; i < DataManager.Instance.unsortedCharacterData.Count; i++)
            {
                var characterData = DataManager.Instance.unsortedCharacterData[i];
                if (characterData.GetName() == displayName)
                {
                    return characterData.eCharacter;
                }
            }
            return null;
        }

        public static string GetDisplayName(this ECharacter? characterEnum)
        {
            if (!characterEnum.HasValue)
                return string.Empty;

            return characterEnum.Value.GetCharacterData()?.GetName() ?? string.Empty;
        }

        public static string GetDisplayName(this ECharacter characterEnum)
        {
            return characterEnum.GetCharacterData()?.GetName() ?? string.Empty;
        }

        public static CharacterData GetCharacterData(this ECharacter? characterEnum)
        {
            if (!characterEnum.HasValue)
                return null;

            return characterEnum.Value.GetCharacterData();
        }

        public static CharacterData GetCharacterData(this ECharacter characterEnum)
        {
            if (DataManager.Instance.characterData.TryGetValue(characterEnum, out var characterData))
                return characterData;
            
            return null;
        }
    }
}