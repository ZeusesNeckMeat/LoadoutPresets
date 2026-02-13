using System.Text;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

using Main = LoadoutPresets.LoadoutPresets;

namespace LoadoutPresets;

internal static class UIDebugHelper
{
    public static void LogGameObjectHierarchy(GameObject root, int maxDepth = 5)
    {
        Main.Logger.LogInfo($"=== Hierarchy for '{root.name}' ===");
        LogRecursive(root.transform, 0, maxDepth);
        Main.Logger.LogInfo("=== End Hierarchy ===");
    }

    private static void LogRecursive(Transform transform, int depth, int maxDepth)
    {
        if (depth > maxDepth)
            return;

        var indent = new string(' ', depth * 2);
        var components = GetComponentSummary(transform.gameObject);

        Main.Logger.LogInfo($"{indent}├─ {transform.name} {components}");

        for (int i = 0; i < transform.childCount; i++)
        {
            LogRecursive(transform.GetChild(i), depth + 1, maxDepth);
        }
    }

    private static string GetComponentSummary(GameObject obj)
    {
        var sb = new StringBuilder("[");
        var components = obj.GetComponents<Component>();

        foreach (var component in components)
        {
            var typeName = component.GetIl2CppType().Name;

            if (typeName == "RectTransform" || typeName == "CanvasRenderer")
                continue;

            sb.Append(typeName);

            // Add relevant details
            if (component is Image img)
            {
                sb.Append($"(color:{img.color}, sprite:{img.sprite?.name ?? "null"})");
            }
            else if (component is RawImage raw)
            {
                sb.Append($"(tex:{raw.texture?.name ?? "null"})");
            }
            else if (component is TextMeshProUGUI tmp)
            {
                sb.Append($"(\"{tmp.text}\")");
            }

            sb.Append(", ");
        }

        if (sb.Length > 1)
            sb.Length -= 2; // Remove trailing ", "

        sb.Append("]");
        return sb.ToString();
    }

    public static void LogComponentDetails(GameObject obj)
    {
        Main.Logger.LogInfo($"=== Component Details for '{obj.name}' ===");

        var components = obj.GetComponents<Component>();
        foreach (var component in components)
        {
            var typeName = component.GetIl2CppType().Name;
            Main.Logger.LogInfo($"  • {typeName}");

            if (component is Image img)
            {
                Main.Logger.LogInfo($"    - Color: {img.color}");
                Main.Logger.LogInfo($"    - Sprite: {img.sprite?.name ?? "null"}");
                Main.Logger.LogInfo($"    - Material: {img.material?.name ?? "null"}");
                Main.Logger.LogInfo($"    - Type: {img.type}");
            }
            else if (component is RawImage raw)
            {
                Main.Logger.LogInfo($"    - Texture: {raw.texture?.name ?? "null"}");
                Main.Logger.LogInfo($"    - Color: {raw.color}");
            }
            else if (component is TextMeshProUGUI tmp)
            {
                Main.Logger.LogInfo($"    - Text: \"{tmp.text}\"");
                Main.Logger.LogInfo($"    - Font: {tmp.font?.name ?? "null"}");
                Main.Logger.LogInfo($"    - FontSize: {tmp.fontSize}");
                Main.Logger.LogInfo($"    - Color: {tmp.color}");
            }
            else if (component is RectTransform rect)
            {
                Main.Logger.LogInfo($"    - AnchorMin: {rect.anchorMin}");
                Main.Logger.LogInfo($"    - AnchorMax: {rect.anchorMax}");
                Main.Logger.LogInfo($"    - SizeDelta: {rect.sizeDelta}");
            }
        }

        Main.Logger.LogInfo("=== End Component Details ===");
    }
}