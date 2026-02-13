using System;
using System.Collections.Generic;
using System.Linq;

using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Main = LoadoutPresets.LoadoutPresets;
using UObject = UnityEngine.Object;

namespace LoadoutPresets;

/// <summary>
/// Factory for creating loadout list items in the scrollable view.
/// Each item shows: Loadout Name | Character Selector | Load Button | Delete Button
/// Works WITH VerticalLayoutGroup - uses LayoutElement instead of manual positioning.
/// </summary>
internal static class LoadoutListFactory
{
    private static GameObject _buttonTemplate;

    private static readonly Dictionary<string, GameObject> _cachedLoadoutItems = [];

    /// <summary>
    /// Sets the button template to use for creating Load/Delete buttons.
    /// Called by MenuFactory after caching the Settings button.
    /// </summary>
    public static void SetButtonTemplate(GameObject template)
    {
        _buttonTemplate = template;
    }

    public static bool TryUpdateLoadoutListItemCharacter(string loadoutName, ECharacter? linkedCharacter)
    {
        if (_cachedLoadoutItems.TryGetValue(loadoutName, out var cachedItem) && cachedItem != null)
        {
            Main.Logger.LogDebug($"LoadoutListFactory: Updating character for cached item '{loadoutName}'.");
            UpdateLoadoutItem(cachedItem, loadoutName, linkedCharacter);
            return true;
        }
        else
        {
            Main.Logger.LogWarning($"LoadoutListFactory: Cannot update character for '{loadoutName}' - item not found in cache.");
            return false;
        }
    }

    public static bool TryRemoveLoadoutListItem(string loadoutName)
    {
        if (_cachedLoadoutItems.TryGetValue(loadoutName, out var cachedItem) && cachedItem != null)
        {
            var buttonsToRemove = cachedItem.GetComponentsInChildren<MyButton>(true);
            Main.Logger.LogDebug($"LoadoutListFactory: Removing cached item '{loadoutName}' with {buttonsToRemove.Length} buttons. | Names - {string.Join(", ", buttonsToRemove?.Select(x => x.name) ?? [])}");

            foreach (var button in buttonsToRemove)
            {
                Main.Logger.LogDebug($"Attempting to remove button '{button.name}' from active window's button list...");
                if (WindowManager.activeWindow.allButtons.Contains(button))
                {
                    Main.Logger.LogDebug($"Removing button '{button.name}' from active window's button list.");
                    WindowManager.activeWindow.allButtons.Remove(button);
                }
            }

            Main.Logger.LogDebug($"LoadoutListFactory: Removing cached item '{loadoutName}'.");
            cachedItem.SetActive(false);
            UObject.Destroy(cachedItem);
            _cachedLoadoutItems.Remove(loadoutName);

            return true;
        }
        else
        {
            Main.Logger.LogWarning($"LoadoutListFactory: Cannot remove '{loadoutName}' - item not found in cache.");
            return false;
        }
    }

    /// <summary>
    /// Creates or retrieves a cached loadout list item.
    /// Layout: [Loadout Name Text] [Character Selector Button] [LOAD] [DELETE]
    /// </summary>
    public static GameObject CreateLoadoutListItem(
        string loadoutName,
        ECharacter? linkedCharacter,
        Transform container,
        TextMeshProUGUI titleTextReference)
    {
        Main.Logger.LogDebug($"LoadoutListFactory: Request to create/update item for '{loadoutName}' with linked character '{linkedCharacter}'.");
        if (!_buttonTemplate)
        {
            Main.Logger.LogWarning("LoadoutListFactory: No button template available.");
            return null;
        }

        Main.Logger.LogDebug($"LoadoutListFactory: Checking cache for '{loadoutName}'...");
        if (_cachedLoadoutItems.TryGetValue(loadoutName, out var cachedItem) && cachedItem != null)
        {
            Main.Logger.LogDebug($"LoadoutListFactory: Using cached item for '{loadoutName}'.");

            cachedItem.transform.SetParent(container, false);
            cachedItem.SetActive(true);

            UpdateLoadoutItem(cachedItem, loadoutName, linkedCharacter);

            return cachedItem;
        }

        Main.Logger.LogDebug($"LoadoutListFactory: Creating new item for '{loadoutName}'.");
        var itemObj = CreateNewLoadoutItem(loadoutName, linkedCharacter, container, titleTextReference);

        _cachedLoadoutItems[loadoutName] = itemObj;

        return itemObj;
    }

    /// <summary>
    /// Creates a brand new loadout item with all components.
    /// </summary>
    private static GameObject CreateNewLoadoutItem(
        string loadoutName,
        ECharacter? linkedCharacter,
        Transform container,
        TextMeshProUGUI titleTextReference)
    {
        var itemObj = new GameObject($"LoadoutItem_{loadoutName}");
        itemObj.transform.SetParent(container, false);

        var itemRect = itemObj.AddComponent<RectTransform>();
        var itemLayout = itemObj.AddComponent<LayoutElement>();
        itemLayout.minHeight = 70f;
        itemLayout.preferredHeight = 70f;
        itemLayout.flexibleHeight = 0f;
        itemLayout.minWidth = -1;
        itemLayout.preferredWidth = -1;
        itemLayout.flexibleWidth = 1f;

        var itemBg = itemObj.AddComponent<Image>();
        itemBg.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);

        var nameTextObj = new GameObject("LoadoutName");
        nameTextObj.transform.SetParent(itemObj.transform, false);

        var nameRect = nameTextObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.02f, 0);
        nameRect.anchorMax = new Vector2(0.30f, 1);
        nameRect.anchoredPosition = Vector2.zero;
        nameRect.sizeDelta = Vector2.zero;

        var nameText = nameTextObj.AddComponent<TextMeshProUGUI>();
        nameText.text = loadoutName;
        nameText.fontSize = 20;
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        nameText.color = Color.white;
        nameText.margin = new Vector4(10, 0, 5, 0);

        if (titleTextReference)
            nameText.font = titleTextReference.font;

        var characterData = linkedCharacter.GetCharacterData();
        var characterDisplayName = linkedCharacter.GetDisplayName() ?? "None";
        var characterIconName = characterData?.icon?.name;

        var characterButton = ButtonFactory.CreateNativeButton(
            _buttonTemplate,
            "B_CharacterSelector",
            characterDisplayName,
            characterIconName,
            itemObj.transform
        );

        var eventTrigger = characterButton.gameObject.GetComponent<EventTrigger>();

        EventTrigger.Entry clickTrigger = null;
        for (int i = 0; i < eventTrigger.triggers.Count; i++)
        {
            if (eventTrigger.triggers[i].eventID == EventTriggerType.PointerClick)
            {
                clickTrigger = eventTrigger.triggers[i];
                break;
            }
        }

        clickTrigger?.callback.AddListener(new Action<BaseEventData>((eventData) =>
            {
                var pointerData = eventData.TryCast<PointerEventData>();
                if (pointerData == null)
                    return;

                if (pointerData.button == PointerEventData.InputButton.Left)
                {
                    Main.Logger.LogDebug($"LoadoutListFactory: Character selector left-clicked for '{loadoutName}'.");
                    OnCharacterSelectorClicked(loadoutName);
                }
                else if (pointerData.button == PointerEventData.InputButton.Right)
                {
                    Main.Logger.LogDebug($"LoadoutListFactory: Character selector right-clicked for '{loadoutName}'.");
                    OnCharacterSelectorRightClicked(loadoutName);
                }
            }));

        var charRect = characterButton.GetComponent<RectTransform>();
        charRect.anchorMin = new Vector2(0.32f, 0.1f);
        charRect.anchorMax = new Vector2(0.63f, 0.90f);
        charRect.anchoredPosition = Vector2.zero;
        charRect.sizeDelta = Vector2.zero;

        var loadButton = ButtonFactory.CreateNativeButton(
            _buttonTemplate,
            "B_Load",
            "LOAD",
            "psd_checkmark",
            itemObj.transform
        );
        loadButton.onClick.RemoveAllListeners();
        loadButton.onClick.AddListener(new Action(() => OnLoadClicked(loadoutName)));

        var loadRect = loadButton.GetComponent<RectTransform>();
        loadRect.anchorMin = new Vector2(0.63f, 0.1f);
        loadRect.anchorMax = new Vector2(0.79f, 0.90f);
        loadRect.anchoredPosition = Vector2.zero;
        loadRect.sizeDelta = Vector2.zero;

        var loadIconTransform = loadButton.transform.Find("Icon");
        var loadIcon = loadIconTransform?.GetComponent<RawImage>();
        if (loadIcon)
        {
            loadIcon.color = new Color(0.3f, 0.8f, 0.3f);
            loadIconTransform.gameObject.GetOrAddComponent<RectTransform>().sizeDelta = new Vector2(30, 30);
        }

        var deleteButton = ButtonFactory.CreateNativeButton(
            _buttonTemplate,
            "B_Delete",
            "DELETE",
            "Prototype_symbol_cross_32x32px",
            itemObj.transform
        );
        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(new Action(() => OnDeleteClicked(loadoutName)));

        var deleteRect = deleteButton.GetComponent<RectTransform>();
        deleteRect.anchorMin = new Vector2(0.79f, 0.1f);
        deleteRect.anchorMax = new Vector2(0.98f, 0.90f);
        deleteRect.anchoredPosition = Vector2.zero;
        deleteRect.sizeDelta = Vector2.zero;

        var deleteIconTransform = deleteButton.transform.Find("Icon");
        var deleteIcon = deleteIconTransform?.GetComponent<RawImage>();
        if (deleteIcon)
        {
            deleteIcon.color = new Color(0.9f, 0.3f, 0.3f);
            deleteIconTransform.gameObject.GetOrAddComponent<RectTransform>().sizeDelta = new Vector2(30, 30);
        }

        itemObj.SetActive(true);

        return itemObj;
    }

    private static void OnCharacterSelectorClicked(string loadoutName)
    {
        Main.Logger.LogDebug($"LoadoutListFactory: Character selector clicked for '{loadoutName}'.");
        LoadoutsMenu.OpenCharacterSelect(loadoutName);
    }

    private static void OnCharacterSelectorRightClicked(string loadoutName)
    {
        Main.Logger.LogDebug($"LoadoutListFactory: Character selector right-clicked for '{loadoutName}' - removing linked character.");
        Main.RemoveLoadoutCharacter(loadoutName);
    }

    private static void OnLoadClicked(string loadoutName)
    {
        Main.Logger.LogDebug($"LoadoutListFactory: Loading loadout '{loadoutName}'...");
        Main.ActivateLoadout(loadoutName);
        LoadoutsMenu.CloseMenu();
    }

    private static void OnDeleteClicked(string loadoutName)
    {
        Main.Logger.LogDebug($"LoadoutListFactory: Deleting loadout '{loadoutName}'...");
        Main.DeleteLoadout(loadoutName);

        TryRemoveLoadoutListItem(loadoutName);
    }

    /// <summary>
    /// Updates an existing cached loadout item's linked character text and icon.
    /// </summary>
    private static void UpdateLoadoutItem(GameObject itemObj, string loadoutName, ECharacter? linkedCharacter)
    {
        var characterButton = itemObj.transform.Find("B_CharacterSelector");
        if (!characterButton)
        {
            Main.Logger.LogError($"UpdateLoadoutItem: Could not find B_CharacterSelector for '{loadoutName}'");
            return;
        }

        Main.Logger.LogDebug($"UpdateLoadoutItem: Found character button for '{loadoutName}'");

        CharacterData characterData = null;
        if (linkedCharacter.HasValue)
            DataManager.Instance.characterData.TryGetValue(linkedCharacter.Value, out characterData);

        var textTransform = characterButton.Find("B_CharacterSelector_Text_Protected");
        if (textTransform)
        {
            var tmpText = textTransform.GetComponent<TextMeshProUGUI>();
            var newText = characterData?.GetName() ?? "None";
            if (tmpText && !string.Equals(tmpText.text, newText, StringComparison.OrdinalIgnoreCase))
            {
                Main.Logger.LogDebug($"UpdateLoadoutItem: Updating text from '{tmpText.text}' to '{newText}' for '{loadoutName}'");
                tmpText.text = newText;
            }
            else if (!tmpText)
            {
                Main.Logger.LogError($"UpdateLoadoutItem: No TextMeshProUGUI on B_CharacterSelector_Text_Protected");
            }
        }
        else
        {
            Main.Logger.LogError($"UpdateLoadoutItem: Could not find B_CharacterSelector_Text_Protected");
        }

        TrySetIcon(characterButton, characterData, loadoutName);
        Main.Logger.LogInfo($"UpdateLoadoutItem: Completed for '{loadoutName}'");
    }

    private static void TrySetIcon(Transform transform, CharacterData characterData, string loadoutName)
    {
        var iconTransform = transform.Find("Icon");
        if (!iconTransform)
        {
            Main.Logger.LogError($"UpdateLoadoutItem: Could not find Icon transform");
            return;
        }

        var rawImage = iconTransform.GetComponent<RawImage>();
        if (!rawImage)
        {
            Main.Logger.LogError($"UpdateLoadoutItem: No RawImage on Icon");
            return;
        }

        if (characterData == null)
        {
            iconTransform.gameObject.SetActive(false);
            Main.Logger.LogDebug($"UpdateLoadoutItem: No icon for '{loadoutName}' (no character linked)");
            return;
        }

        var iconTexture = characterData.icon;
        var iconName = iconTexture?.name ?? "NULL";

        if (iconTexture != null)
        {
            rawImage.texture = iconTexture;
            rawImage.color = Color.white;
            iconTransform.gameObject.SetActive(true);
            Main.Logger.LogDebug($"UpdateLoadoutItem: Updated icon to '{iconName}' for '{loadoutName}'");
        }
        else
        {
            iconTransform.gameObject.SetActive(false);
            Main.Logger.LogWarning($"UpdateLoadoutItem: Icon texture is null for '{loadoutName}'");
        }
    }
}