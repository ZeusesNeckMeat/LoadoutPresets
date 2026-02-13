using UnityEngine;
using UnityEngine.UI;

using Main = LoadoutPresets.LoadoutPresets;
using UObject = UnityEngine.Object;

namespace LoadoutPresets;

internal static class CharacterSelectFactory
{
    public static GameObject CloneCharacterSelect(GameObject characterSelectTemplate, Transform newParent)
    {
        if (!characterSelectTemplate)
        {
            Main.Logger.LogError("CharacterSelectFactory: Character grid template is null.");
            return null;
        }

        var clonedCharacterSelect = UObject.Instantiate(characterSelectTemplate, newParent);
        clonedCharacterSelect.name = "W_LoadoutCharacterSelector";
        clonedCharacterSelect.SetActive(false);

        StripGameLogicFromMenu(clonedCharacterSelect);

        ModifyCharacterSelectStructure(clonedCharacterSelect);

        Main.Logger.LogDebug("CharacterSelectFactory: Successfully cloned character grid for Loadouts menu.");
        return clonedCharacterSelect;
    }

    private static void StripGameLogicFromMenu(GameObject characterSelect)
    {
        var backButton = characterSelect.transform.Find("Header/Header/B_Back");
        var characterPrefabUI = characterSelect.GetComponentInChildren<CharacterPrefabUI>(true);

        // Deactivate W_Stats panel since it's part of the clone and we aren't using it (destroying it breaks things)
        var rootTransform = characterSelect.transform;
        for (int i = 0; i < rootTransform.childCount; i++)
        {
            var child = rootTransform.GetChild(i);
            if (child.name.Contains("W_Stats"))
            {
                child.gameObject.SetActive(false);
                Main.Logger.LogDebug($"CharacterSelectFactory: Deactivated W_Stats panel");
                break;
            }
        }

        var allTransforms = characterSelect.GetComponentsInChildren<Transform>(true);
        Main.Logger.LogDebug($"CharacterSelectFactory: Processing {allTransforms.Length} GameObjects in hierarchy.");

        foreach (var transform in allTransforms)
        {
            if (transform == null)
                continue;

            // Don't destroy components in CharacterPrefabUI subtree
            if (characterPrefabUI && (transform == characterPrefabUI.transform || transform.IsChildOf(characterPrefabUI.transform)))
            {
                Main.Logger.LogDebug($"CharacterSelectFactory: Skipping CharacterPrefabUI subtree for '{transform.name}'.");
                continue;
            }

            // Destroy localization components
            var allComponents = transform.GetComponents<Component>();
            foreach (var component in allComponents)
            {
                if (component == null) continue;

                var typeName = component.GetIl2CppType().Name;

                // ONLY destroy localization components (these prevent our custom text)
                if (typeName.Contains("Localize"))
                {
                    Main.Logger.LogDebug($"CharacterSelectFactory: Destroying localization component '{typeName}' on '{transform.name}'.");
                    UObject.DestroyImmediate(component);
                }
            }
        }

        if (!backButton)
            return;

        // Configure B_Back button instead of recreating it
        var button = backButton.GetComponent<Button>();
        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(new System.Action(LoadoutsMenu.CloseCharacterSelect));
            Main.Logger.LogDebug("CharacterSelectFactory: Configured B_Back button with close handler.");
        }
    }

    /// <summary>
    /// Modifies the Character Select menu to appear on the LEFT side of Loadout Menu.
    /// Matches height and positions as a sidebar.
    /// </summary>
    private static void ModifyCharacterSelectStructure(GameObject characterSelectMenu)
    {
        // ========== POSITION AS LEFT SIDEBAR ==========
        var menuRect = characterSelectMenu.GetComponent<RectTransform>();
        if (menuRect)
        {
            // Position the Character Select menu to the LEFT of its parent (Loadout Menu)
            // Using anchors to the left edge for resolution independence

            menuRect.anchorMin = new Vector2(0, 0.5f);     // Left edge, vertically centered
            menuRect.anchorMax = new Vector2(0, 0.5f);     // Same point (not stretched)
            menuRect.pivot = new Vector2(1, 0.5f);         // Pivot at RIGHT edge (so it grows leftward)

            // Position it to the left of the parent
            // X: 0 means touching the left edge of parent (but pivot is on right, so menu extends left)
            // You can add negative offset to add gap: e.g., -10 for 10px gap
            menuRect.anchoredPosition = new Vector2(-10, 0); // 10px gap to the left

            // Match height to parent (or set specific height)
            // Get parent's height to match
            var parentRect = characterSelectMenu.transform.parent.GetComponent<RectTransform>();
            if (parentRect)
            {
                var parentHeight = parentRect.rect.height;
                menuRect.sizeDelta = new Vector2(650, parentHeight); // 400px wide, same height as parent
                Main.Logger.LogDebug($"CharacterSelect: Positioned to left - Width: 400px, Height: {parentHeight}px");
            }
            else
            {
                // Fallback to fixed size
                menuRect.sizeDelta = new Vector2(650, 800);
                Main.Logger.LogDebug("CharacterSelect: Positioned to left - Width: 400px, Height: 600px (fixed)");
            }
        }
    }
}
