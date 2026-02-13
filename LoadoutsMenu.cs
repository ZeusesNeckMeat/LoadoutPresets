using Il2CppInterop.Runtime.InteropTypes.Arrays;

using System;
using System.Linq;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

using Main = LoadoutPresets.LoadoutPresets;
using ObjectNames = LoadoutPresets.Constants.ObjectNames;

namespace LoadoutPresets;

internal static class LoadoutsMenu
{
    private static GameObject _loadoutsMenuPanel;
    private static GameObject _mainMenuPanel;
    private static Transform _loadoutListContainer;
    private static TMP_InputField _loadoutNameInput;
    private static GameObject _characterSelect;
    private static string _loadoutBeingEdited;

    public static void CreateLoadoutsMenu(Il2CppReferenceArray<GameObject> rootGameObjects)
    {
        var uiRoot = rootGameObjects.FirstOrDefault(obj => string.Equals(obj.name, ObjectNames.UI, StringComparison.OrdinalIgnoreCase));
        if (!uiRoot)
        {
            Main.Logger.LogError("LoadoutsMenu: Could not find UI root.");
            return;
        }

        var creditsMenuTransform = uiRoot.transform.Find("Tabs/W_Credits");
        if (!creditsMenuTransform)
        {
            Main.Logger.LogError("LoadoutsMenu: Could not find Credits menu template.");
            return;
        }

        _loadoutsMenuPanel = LoadoutsMenuFactory.CloneCreditsAsLoadoutsMenu(
            creditsMenuTransform.gameObject,
            uiRoot.transform.Find("Tabs")
        );
        _mainMenuPanel = _loadoutsMenuPanel.transform.parent.Find("Menu")?.gameObject;

        _loadoutListContainer = _loadoutsMenuPanel.transform.Find("WindowLayers/Content/ScrollRect/ContentEntries");
        if (_loadoutListContainer)
        {
            Main.Logger.LogDebug($"LoadoutsMenu: Found ContentEntries container");
        }
        else
        {
            Main.Logger.LogWarning("LoadoutsMenu: Could not find container. Using fallback.");
            var scrollRect = _loadoutsMenuPanel.GetComponentInChildren<ScrollRect>();
            if (scrollRect && scrollRect.content)
            {
                _loadoutListContainer = scrollRect.content;
                Main.Logger.LogDebug("LoadoutsMenu: Using ScrollRect.content as container.");
            }
        }

        _loadoutListContainer.gameObject.GetOrAddComponent<VerticalLayoutGroup>().childControlHeight = true;

        var characterSelectTransform = uiRoot.transform.Find("Tabs/Character/W_Character");
        _characterSelect = CharacterSelectFactory.CloneCharacterSelect(
            characterSelectTransform.gameObject,
            _loadoutsMenuPanel.transform
        );

        Main.Logger.LogDebug("LoadoutsMenu: Menu created successfully.");
    }

    /// <summary>
    /// Stores reference to the loadout name input field.
    /// Called by MenuFactory during menu creation.
    /// </summary>
    public static void SetLoadoutNameInput(TMP_InputField inputField)
    {
        _loadoutNameInput = inputField;
    }

    /// <summary>
    /// Opens the Loadouts menu and populates it with saved loadouts.
    /// </summary>
    public static void OpenMenu()
    {
        if (!_loadoutsMenuPanel)
        {
            Main.Logger.LogError("LoadoutsMenu: Cannot open menu - panel not initialized.");
            return;
        }

        Main.Logger.LogDebug($"LoadoutsMenu: Attempting to open menu. Current active state: {_loadoutsMenuPanel.activeSelf}");
        Main.Logger.LogDebug($"_characterSelect has value: {_characterSelect != null}. Active state: {_characterSelect?.activeSelf}");
        _characterSelect.SetActive(false);
        
        Main.Logger.LogDebug($"_loadoutsMenuPanel parent: {_loadoutsMenuPanel.transform.parent?.name}. Attempting to hide parent menu if exists.");
        _loadoutsMenuPanel.transform.parent.Find("Menu")?.gameObject.SetActive(false);

        Main.Logger.LogDebug("LoadoutsMenu: Clearing loadout name input field.");
        if (_loadoutNameInput)
        {
            _loadoutNameInput.text = "";
            var placeholderText = _loadoutNameInput.placeholder?.GetComponent<TextMeshProUGUI>();
            if (placeholderText)
            {
                placeholderText.text = "New loadout name...";
                placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }

        RefreshLoadoutList();

        Main.Logger.LogDebug($"LoadoutsMenu: Deactivating main menu panel if exists. Current active state: {_mainMenuPanel?.activeSelf}, {_mainMenuPanel?.name}");
        if (_mainMenuPanel)
        {
            _mainMenuPanel.SetActive(false);
        }

        Main.Logger.LogDebug($"LoadoutsMenu: List Container Exists: {_loadoutListContainer != null}. Active state: {_loadoutListContainer?.gameObject.activeSelf}");
        var containerPointer = _loadoutListContainer.Pointer;
        var panelScrollRectPointer = _loadoutsMenuPanel.GetComponentInChildren<ScrollRect>()?.Pointer;
        Main.Logger.LogDebug($"LoadoutsMenu: Container Pointer: {containerPointer}, Panel ScrollRect Pointer: {panelScrollRectPointer}");

        Main.Logger.LogDebug("LoadoutsMenu: Activating loadouts menu panel.");
        _loadoutsMenuPanel.SetActive(true);

        Main.Logger.LogDebug($"LoadoutsMenu: Menu activated. New active state: {_loadoutsMenuPanel.activeSelf}");
    }

    /// <summary>
    /// Closes the Loadouts menu.
    /// </summary>
    public static void CloseMenu()
    {
        if (_loadoutsMenuPanel)
        {
            _loadoutsMenuPanel.SetActive(false);
            Main.Logger.LogDebug("LoadoutsMenu: Menu closed.");
        }

        if (_mainMenuPanel)
        {
            _mainMenuPanel.SetActive(true);
        }

        _loadoutsMenuPanel.transform.parent.Find("Menu")?.gameObject.SetActive(true);   
    }

    public static void OpenCharacterSelect(string loadoutName)
    {
        if (!_characterSelect)
        {
            Main.Logger.LogError("LoadoutsMenu: Cannot open character select - not initialized.");
            return;
        }

        _loadoutBeingEdited = loadoutName;
        if (_characterSelect.activeSelf)
        {
            Main.Logger.LogDebug("LoadoutsMenu: Character select already open.");
            return;
        }

        Main.Logger.LogDebug($"LoadoutsMenu: Opening character select for loadout '{loadoutName}'.");

        if (!_loadoutsMenuPanel.activeSelf)
        {
            Main.Logger.LogDebug("LoadoutsMenu: Loadouts menu not open. Opening menu first.");
            OpenMenu();
        }

        _characterSelect.SetActive(true);
    }

    /// <summary>
    /// Called when a character is selected from the Character Select menu.
    /// Links the selected character to the currently editing loadout.
    /// </summary>
    /// <param name="selectedCharacter">The character that was selected.</param>
    public static void OnCharacterSelected(ECharacter selectedCharacter)
    {
        if (string.IsNullOrEmpty(_loadoutBeingEdited))
        {
            Main.Logger.LogError("LoadoutsMenu: No loadout is currently being edited.");
            return;
        }

        Main.Logger.LogDebug($"LoadoutsMenu: Character '{selectedCharacter}' selected for loadout '{_loadoutBeingEdited}'.");

        Main.UpdateLoadoutCharacter(_loadoutBeingEdited, selectedCharacter);
        CloseCharacterSelect();

        _loadoutBeingEdited = null;
    }

    public static void CloseCharacterSelect()
    {
        if (_characterSelect && _characterSelect.activeSelf)
        {
            _characterSelect.SetActive(false);
            Main.Logger.LogDebug("LoadoutsMenu: Character select closed.");
        }

        _loadoutBeingEdited = null;
    }

    public static string GetCurrentEditingLoadout() => _loadoutBeingEdited;

    public static void RefreshLoadoutList()
    {
        if (!_loadoutListContainer)
        {
            Main.Logger.LogWarning("LoadoutsMenu: Cannot refresh - container not found.");
            return;
        }

        var titleText = _loadoutsMenuPanel.transform.Find("Header/Header/T_Title")?.GetComponent<TextMeshProUGUI>();
        var allLoadouts = Main.LoadoutDatasCache().OrderBy(x => x.Name);

        foreach (var loadout in allLoadouts)
        {
            Main.Logger.LogDebug($"LoadoutsMenu: Processing loadout '{loadout.Name}' with linked character '{loadout.LinkedCharacter}'");
            ECharacter? linkedCharacter = null;
            
            for (var i = 0; i < DataManager.Instance.unsortedCharacterData.Count; i++)
            {
                var characterData = DataManager.Instance.unsortedCharacterData[i];
                if (characterData.GetName() == loadout.LinkedCharacter)
                {
                    linkedCharacter = characterData.eCharacter;
                    break;
                }
            }

            LoadoutListFactory.CreateLoadoutListItem(
                loadout.Name,
                linkedCharacter,
                _loadoutListContainer,
                titleText
            );
        }

        Main.Logger.LogDebug("LoadoutsMenu: Loadout list refreshed.");
    }

    public static void AddLoadoutListItem(string loadoutName)
    {
        LoadoutListFactory.CreateLoadoutListItem(
            loadoutName,
            null,
            _loadoutListContainer,
            _loadoutsMenuPanel.transform.Find("Header/Header/T_Title")?.GetComponent<TextMeshProUGUI>()
        );

        Main.Logger.LogDebug($"LoadoutsMenu: Added loadout list item for '{loadoutName}'.");
    }

    public static void UpdateLoadoutListItemCharacter(string loadoutName, ECharacter? linkedCharacter)
    {
        if (!LoadoutListFactory.TryUpdateLoadoutListItemCharacter(loadoutName, linkedCharacter))
            RefreshLoadoutList();
    }
}