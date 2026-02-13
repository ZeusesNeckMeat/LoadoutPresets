using System;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

using ButtonNames = LoadoutPresets.Constants.ButtonNames;
using Main = LoadoutPresets.LoadoutPresets;
using UObject = UnityEngine.Object;

namespace LoadoutPresets;

/// <summary>
/// Handles cloning of native game menus (like Credits) and converting them
/// for custom use while preserving visual styling.
/// Similar to ButtonFactory's "Native Clone Architecture" but for full menus.
/// </summary>
internal static class LoadoutsMenuFactory
{
    private static GameObject _buttonTemplate;

    /// <summary>
    /// Clones the Credits menu and prepares it as a Loadouts menu.
    /// KEEPS: Visual components (Images, RectTransforms, TextMeshPro, Scrollbars)
    /// DESTROYS: Game-specific logic components
    /// MODIFIES: Text content, hierarchy, behavior
    /// </summary>
    public static GameObject CloneCreditsAsLoadoutsMenu(GameObject creditsTemplate, Transform newParent)
    {
        Main.Logger.LogDebug("MenuFactory: Starting Credits → Loadouts menu clone.");

        CacheButtonTemplate(creditsTemplate);

        var clonedMenu = UObject.Instantiate(creditsTemplate, newParent);
        clonedMenu.name = "W_LoadoutPresets";
        clonedMenu.SetActive(false);

        clonedMenu.GetComponent<RectTransform>().pivot = new Vector2(0.2f, 0.5f);

        StripGameLogicFromMenu(clonedMenu);

        ModifyLoadoutsMenuStructure(clonedMenu);

        Main.Logger.LogDebug("MenuFactory: Clone complete.");
        return clonedMenu;
    }

    /// <summary>
    /// Finds and caches a button template from the main menu.
    /// Uses the Settings button which has proper text components.
    /// </summary>
    private static void CacheButtonTemplate(GameObject creditsTemplate)
    {
        var creditsParent = creditsTemplate.transform.parent;
        if (!creditsParent)
        {
            Main.Logger.LogWarning("MenuFactory: Could not find Credits parent (Tabs).");
            return;
        }

        var menuTransform = creditsParent.Find("Menu/Content/Main/ExtraButtons");
        if (!menuTransform)
        {
            Main.Logger.LogWarning("MenuFactory: Could not find Menu/Content/Main/ExtraButtons.");
            return;
        }

        var settingsButton = menuTransform.Find(ButtonNames.SETTINGS_BUTTON);
        if (settingsButton)
        {
            _buttonTemplate = settingsButton.gameObject;
            if (_buttonTemplate)
                LoadoutListFactory.SetButtonTemplate(_buttonTemplate);

            Main.Logger.LogDebug("MenuFactory: Using Settings button as button template (has text components).");
            return;
        }

        var backButton = creditsTemplate.transform.Find("Header/Header/B_Back");
        if (backButton)
        {
            _buttonTemplate = backButton.gameObject;
            Main.Logger.LogDebug("MenuFactory: Fallback to B_Back as button template.");
            return;
        }

        Main.Logger.LogWarning("MenuFactory: Could not find any suitable button template.");
    }

    /// <summary>
    /// Recursively strips game-specific components while preserving visual components.
    /// MINIMAL DESTRUCTION APPROACH: Only remove what breaks our custom behavior.
    /// </summary>
    private static void StripGameLogicFromMenu(GameObject menuRoot)
    {
        var backButton = menuRoot.transform.Find("Header/Header/B_Back");

        var allTransforms = menuRoot.GetComponentsInChildren<Transform>(true);
        Main.Logger.LogDebug($"MenuFactory: Processing {allTransforms.Length} GameObjects in hierarchy.");

        foreach (var transform in allTransforms)
        {
            var allComponents = transform.GetComponents<Component>();

            foreach (var component in allComponents)
            {
                if (component == null) continue;

                var typeName = component.GetIl2CppType().Name;

                if (typeName.Contains("Localize"))
                {
                    Main.Logger.LogDebug($"MenuFactory: Destroying localization component '{typeName}' on '{transform.name}'.");
                    UObject.DestroyImmediate(component);
                    continue;
                }

                if (typeName.Contains("Credits") || typeName.Contains("Tab"))
                {
                    Main.Logger.LogDebug($"MenuFactory: Destroying game component '{typeName}' on '{transform.name}'.");
                    UObject.DestroyImmediate(component);
                }
            }
        }

        if (backButton)
        {
            var button = backButton.GetComponent<Button>();
            if (button)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(new Action(LoadoutsMenu.CloseMenu));
                Main.Logger.LogDebug("MenuFactory: Configured B_Back button with close handler.");
            }
        }
    }

    /// <summary>
    /// Modifies the cloned menu structure to fit Loadouts purpose.
    /// </summary>
    private static void ModifyLoadoutsMenuStructure(GameObject menu)
    {
        var titleTransform = menu.transform.Find("Header/Header/T_Title");
        if (titleTransform)
        {
            var titleText = titleTransform.GetComponent<TextMeshProUGUI>();
            if (titleText)
            {
                titleText.text = "Loadout Manager";
                Main.Logger.LogDebug("MenuFactory: Updated menu title.");
            }
        }

        var containerTransform = menu.transform.Find("WindowLayers/Content/ScrollRect/ContentEntries");
        if (containerTransform)
        {
            ClearChildren(containerTransform);
            Main.Logger.LogDebug("MenuFactory: Cleared content container.");
        }
        else
        {
            Main.Logger.LogWarning("MenuFactory: Could not find expected container path. Trying alternative method.");
            var scrollRect = menu.GetComponentInChildren<ScrollRect>();
            if (scrollRect && scrollRect.content)
            {
                ClearChildren(scrollRect.content);
                Main.Logger.LogDebug("MenuFactory: Cleared content using ScrollRect.content.");

                Main.Logger.LogDebug("Setting HorizontalLayoutGroup on ScrollRect content to active (if exists) to ensure proper layout after clearing.");
                scrollRect.gameObject.GetComponent<HorizontalLayoutGroup>()?.gameObject.SetActive(true);
            }
        }

        var tabButtons = menu.transform.Find("TabButtons");
        if (tabButtons)
        {
            UObject.Destroy(tabButtons.gameObject);
            Main.Logger.LogDebug("MenuFactory: Removed tab buttons.");
        }

        AddActionControls(menu);
    }

    /// <summary>
    /// Adds input field and action buttons ABOVE the scrollable content area.
    /// Creates a fixed "sub-header" section with clean anchor-based layout.
    /// </summary>
    private static void AddActionControls(GameObject menu)
    {
        if (!_buttonTemplate)
        {
            Main.Logger.LogWarning("MenuFactory: No button template available for action buttons.");
            return;
        }

        var contentTransform = menu.transform.Find("WindowLayers/Content");
        if (!contentTransform)
        {
            Main.Logger.LogWarning("MenuFactory: Could not find WindowLayers/Content for action controls.");
            return;
        }

        var horizontalLayoutGroup = contentTransform.GetComponent<HorizontalLayoutGroup>();
        if (horizontalLayoutGroup)
        {
            Main.Logger.LogDebug("MenuFactory: Found HorizontalLayoutGroup on Content - destroying it.");
            UObject.DestroyImmediate(horizontalLayoutGroup);
        }

        var verticalLayoutGroup = contentTransform.gameObject.AddComponent<VerticalLayoutGroup>();
        verticalLayoutGroup.childControlWidth = true;
        verticalLayoutGroup.childControlHeight = true;
        verticalLayoutGroup.childForceExpandWidth = true;
        verticalLayoutGroup.childForceExpandHeight = false;
        verticalLayoutGroup.spacing = 5f;
        verticalLayoutGroup.padding = new RectOffset
        {
            top = 0,
            left = 0,
            right = 0,
            bottom = 20
        };
        verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;

        Main.Logger.LogDebug("MenuFactory: Added VerticalLayoutGroup to Content.");

        var subHeaderContainer = new GameObject("SubHeader");
        subHeaderContainer.transform.SetParent(contentTransform, false);
        subHeaderContainer.transform.SetAsFirstSibling();

        var subHeaderRect = subHeaderContainer.AddComponent<RectTransform>();

        var subHeaderLayout = subHeaderContainer.AddComponent<LayoutElement>();
        subHeaderLayout.minHeight = 70f;
        subHeaderLayout.preferredHeight = 70f;
        subHeaderLayout.flexibleHeight = 0f;

        var subHeaderBg = subHeaderContainer.AddComponent<Image>();
        subHeaderBg.color = new Color(0.05f, 0.05f, 0.05f, 0.5f);

        Main.Logger.LogDebug("MenuFactory: Created SubHeader with fixed 20px height.");

        var scrollRectTransform = menu.transform.Find("WindowLayers/Content/ScrollRect");
        if (scrollRectTransform)
        {
            scrollRectTransform.SetAsLastSibling();

            var scrollLayout = scrollRectTransform.gameObject.GetOrAddComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.minHeight = 100f;

            Main.Logger.LogDebug("MenuFactory: Configured ScrollRect to take remaining vertical space.");
        }

        var titleText = menu.transform.Find("Header/Header/T_Title")?.GetComponent<TextMeshProUGUI>();

        var inputFieldObj = new GameObject("LoadoutNameInput");
        inputFieldObj.transform.SetParent(subHeaderContainer.transform, false);

        var inputRect = inputFieldObj.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.03f, 0.15f);
        inputRect.anchorMax = new Vector2(0.48f, 0.85f);
        inputRect.anchoredPosition = Vector2.zero;
        inputRect.sizeDelta = Vector2.zero;

        var inputImage = inputFieldObj.AddComponent<Image>();
        inputImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        inputImage.type = Image.Type.Sliced;

        var borderObj = new GameObject("Border");
        borderObj.transform.SetParent(inputFieldObj.transform, false);
        var borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = new Vector2(2, 2);
        borderObj.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        borderObj.transform.SetAsFirstSibling();

        var inputField = inputFieldObj.AddComponent<TMP_InputField>();
        inputField.textComponent = CreateInputText(inputFieldObj.transform, titleText);
        inputField.placeholder = CreatePlaceholder(inputFieldObj.transform, titleText);
        inputField.text = "";
        inputField.characterLimit = 50;

        LoadoutsMenu.SetLoadoutNameInput(inputField);

        var saveButton = ButtonFactory.CreateNativeButton(
            _buttonTemplate,
            "B_SaveLoadout",
            "SAVE",
            "icon_health",
            subHeaderContainer.transform
        );
        saveButton.onClick.RemoveAllListeners();
        saveButton.onClick.AddListener(new Action(() => Main.SaveLoadoutFromInput(inputField)));

        var saveButtonIcon = saveButton.transform.Find("Icon")?.GetComponent<RawImage>();
        if (saveButtonIcon)
            saveButtonIcon.color = Color.green;

        var saveRect = saveButton.GetComponent<RectTransform>();
        saveRect.anchorMin = new Vector2(0.50f, 0.05f);
        saveRect.anchorMax = new Vector2(0.67f, 0.95f);
        saveRect.anchoredPosition = Vector2.zero;
        saveRect.sizeDelta = Vector2.zero;

        var openButton = ButtonFactory.CreateNativeButton(
            _buttonTemplate,
            "B_OpenFolder",
            "OPEN FOLDER",
            "Balance Tome",
            subHeaderContainer.transform
        );
        openButton.onClick.RemoveAllListeners();
        openButton.onClick.AddListener(new Action(Main.OpenLoadoutPresetsFolder));

        var openRect = openButton.GetComponent<RectTransform>();
        openRect.anchorMin = new Vector2(0.69f, 0.05f);
        openRect.anchorMax = new Vector2(0.98f, 0.95f);
        openRect.anchoredPosition = Vector2.zero;
        openRect.sizeDelta = Vector2.zero;

        Main.Logger.LogDebug("MenuFactory: Fixed sub-header section created successfully.");
    }

    /// <summary>
    /// Creates the text component for the input field, matching game font style.
    /// </summary>
    private static TextMeshProUGUI CreateInputText(Transform parent, TextMeshProUGUI referenceText)
    {
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(0.97f, 1);
        textRect.sizeDelta = Vector2.zero;

        var text = textObj.AddComponent<TextMeshProUGUI>();

        if (referenceText)
        {
            text.font = referenceText.font;
            text.fontSize = 24;
            text.color = referenceText.color;
        }
        else
        {
            text.fontSize = 24;
            text.color = Color.white;
        }

        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.margin = new Vector4(10, 0, 10, 0);

        return text;
    }

    /// <summary>
    /// Creates the placeholder text for the input field, matching game style.
    /// </summary>
    private static TextMeshProUGUI CreatePlaceholder(Transform parent, TextMeshProUGUI referenceText)
    {
        var placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(parent, false);

        var placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchorMin = new Vector2(0, 0);
        placeholderRect.anchorMax = new Vector2(0.97f, 1);
        placeholderRect.sizeDelta = Vector2.zero;

        var placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholder.text = "New loadout name...";

        if (referenceText)
        {
            placeholder.font = referenceText.font;
            placeholder.fontSize = 24;
        }
        else
        {
            placeholder.fontSize = 24;
        }

        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.margin = new Vector4(10, 0, 10, 0);

        return placeholder;
    }

    private static void ClearChildren(Transform parent)
    {
        Main.Logger.LogDebug($"MenuFactory: Clearing {parent.childCount} children from '{parent.name}'.");
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            UObject.Destroy(parent.GetChild(i).gameObject);
        }
    }
}