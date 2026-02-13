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
}