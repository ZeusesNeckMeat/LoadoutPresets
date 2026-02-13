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

            if (characterPrefabUI && (transform == characterPrefabUI.transform || transform.IsChildOf(characterPrefabUI.transform)))
            {
                Main.Logger.LogDebug($"CharacterSelectFactory: Skipping CharacterPrefabUI subtree for '{transform.name}'.");
                continue;
            }

            var allComponents = transform.GetComponents<Component>();
            foreach (var component in allComponents)
            {
                if (component == null)
                    continue;

                var typeName = component.GetIl2CppType().Name;
                if (typeName.Contains("Localize"))
                {
                    Main.Logger.LogDebug($"CharacterSelectFactory: Destroying localization component '{typeName}' on '{transform.name}'.");
                    UObject.DestroyImmediate(component);
                }
            }
        }

        if (!backButton)
            return;

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
        var menuRect = characterSelectMenu.GetComponent<RectTransform>();
        if (menuRect)
        {
            menuRect.anchorMin = new Vector2(0, 0.5f);
            menuRect.anchorMax = new Vector2(0, 0.5f);
            menuRect.pivot = new Vector2(1, 0.5f);
            menuRect.anchoredPosition = new Vector2(-10, 0);

            var parentRect = characterSelectMenu.transform.parent.GetComponent<RectTransform>();
            if (parentRect)
            {
                var parentHeight = parentRect.rect.height;
                menuRect.sizeDelta = new Vector2(650, parentHeight); 
                Main.Logger.LogDebug($"CharacterSelect: Positioned to left - Width: 400px, Height: {parentHeight}px");
            }
            else
            {
                menuRect.sizeDelta = new Vector2(650, 800);
                Main.Logger.LogDebug("CharacterSelect: Positioned to left - Width: 400px, Height: 600px (fixed)");
            }
        }
    }
}
