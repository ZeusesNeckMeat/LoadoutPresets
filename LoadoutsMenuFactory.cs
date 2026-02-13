using System;
using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

using Main = LoadoutPresets.LoadoutPresets;
using UObject = UnityEngine.Object;
using ButtonNames = LoadoutPresets.Constants.ButtonNames;

namespace LoadoutPresets;

/// <summary>
/// Handles cloning of native game menus (like Credits) and converting them
/// for custom use while preserving visual styling.
/// Similar to ButtonFactory's "Native Clone Architecture" but for full menus.
/// </summary>
internal static class LoadoutsMenuFactory
{
    private static GameObject _buttonTemplate; // Cache button template

    /// <summary>
    /// Clones the Credits menu and prepares it as a Loadouts menu.
    /// KEEPS: Visual components (Images, RectTransforms, TextMeshPro, Scrollbars)
    /// DESTROYS: Game-specific logic components
    /// MODIFIES: Text content, hierarchy, behavior
    /// </summary>
    public static GameObject CloneCreditsAsLoadoutsMenu(GameObject creditsTemplate, Transform newParent)
    {
        Main.Logger.LogInfo("MenuFactory: Starting Credits → Loadouts menu clone.");

        // Cache button template BEFORE cloning - need to access main menu
        CacheButtonTemplate(creditsTemplate);

        // Clone the entire menu structure
        var clonedMenu = UObject.Instantiate(creditsTemplate, newParent);
        clonedMenu.name = "W_LoadoutPresets";
        clonedMenu.SetActive(false); // Start hidden

        clonedMenu.GetComponent<RectTransform>().pivot = new Vector2(0.2f, 0.5f);

        // Strip game-specific components recursively
        StripGameLogicFromMenu(clonedMenu);

        // Modify structure for our needs
        ModifyLoadoutsMenuStructure(clonedMenu);

        Main.Logger.LogInfo("MenuFactory: Clone complete.");
        return clonedMenu;
    }

    /// <summary>
    /// Finds and caches a button template from the main menu.
    /// Uses the Settings button which has proper text components.
    /// </summary>
    private static void CacheButtonTemplate(GameObject creditsTemplate)
    {
        // Instead of using B_Back (which has no text), find the Settings button from the main menu
        // We need to go up to the UI root and find the Settings button
        var creditsParent = creditsTemplate.transform.parent; // This should be "Tabs"
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

        // Fallback to B_Back if Settings not found
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
    /// Based on ButtonFactory.StripNonVisualComponents but for entire hierarchy.
    /// </summary>
    //private static void StripGameLogicFromMenu(GameObject menuRoot)
    //{
    //    var componentsToPreserve = new HashSet<string>
    //    {
    //        "RectTransform",
    //        "CanvasRenderer",
    //        "Image",
    //        "RawImage",
    //        "Mask",
    //        "TextMeshProUGUI",
    //        "ScrollRect",
    //        "Scrollbar",
    //        "LayoutElement",
    //        "LayoutGroup",
    //        "HorizontalLayoutGroup",
    //        "VerticalLayoutGroup",
    //        "GridLayoutGroup",
    //        "ContentSizeFitter",
    //        "Canvas",
    //        "GraphicRaycaster",
    //        "CanvasGroup"
    //    };

    //    // Find the B_Back button in the cloned menu to preserve it
    //    var backButton = menuRoot.transform.Find("Header/Header/B_Back");

    //    // Process all GameObjects in hierarchy
    //    var allTransforms = menuRoot.GetComponentsInChildren<Transform>(true);
    //    Main.Logger.LogDebug($"MenuFactory: Processing {allTransforms.Length} GameObjects in hierarchy.");

    //    foreach (var transform in allTransforms)
    //    {
    //        // Special handling for B_Back - only destroy its Button/EventTrigger components
    //        if (backButton && transform == backButton)
    //        {
    //            var components = transform.GetComponents<Component>();
    //            foreach (var component in components)
    //            {
    //                if (component == null) continue;
    //                var typeName = component.GetIl2CppType().Name;

    //                if (typeName == "Button" || typeName == "EventTrigger")
    //                {
    //                    Main.Logger.LogDebug($"MenuFactory: Destroying '{typeName}' on B_Back (will recreate).");
    //                    UObject.DestroyImmediate(component);
    //                }
    //            }
    //            continue;
    //        }

    //        var allComponents = transform.GetComponents<Component>();

    //        foreach (var component in allComponents)
    //        {
    //            if (component == null) continue;

    //            var typeName = component.GetIl2CppType().Name;

    //            // Keep essential Unity UI components
    //            if (componentsToPreserve.Contains(typeName))
    //                continue;

    //            // Destroy localization (prevents text reverts, same as ButtonFactory)
    //            if (typeName.Contains("Localize"))
    //            {
    //                Main.Logger.LogDebug($"MenuFactory: Destroying '{typeName}' on '{transform.name}'.");
    //                UObject.DestroyImmediate(component);
    //                continue;
    //            }

    //            // Destroy game-specific components
    //            if (typeName.Contains("Credits") ||
    //                typeName.Contains("Tab") ||
    //                typeName == "Button" ||
    //                typeName == "EventTrigger")
    //            {
    //                Main.Logger.LogDebug($"MenuFactory: Destroying game component '{typeName}' on '{transform.name}'.");
    //                UObject.DestroyImmediate(component);
    //            }
    //        }
    //    }
    //}

    /// <summary>
    /// Recursively strips game-specific components while preserving visual components.
    /// MINIMAL DESTRUCTION APPROACH: Only remove what breaks our custom behavior.
    /// </summary>
    private static void StripGameLogicFromMenu(GameObject menuRoot)
    {
        // Find the B_Back button - we'll configure it instead of destroying/recreating
        var backButton = menuRoot.transform.Find("Header/Header/B_Back");

        // Process all GameObjects in hierarchy
        var allTransforms = menuRoot.GetComponentsInChildren<Transform>(true);
        Main.Logger.LogDebug($"MenuFactory: Processing {allTransforms.Length} GameObjects in hierarchy.");

        foreach (var transform in allTransforms)
        {
            var allComponents = transform.GetComponents<Component>();

            foreach (var component in allComponents)
            {
                if (component == null) continue;

                var typeName = component.GetIl2CppType().Name;

                // ONLY destroy localization components (these prevent our custom text)
                if (typeName.Contains("Localize"))
                {
                    Main.Logger.LogDebug($"MenuFactory: Destroying localization component '{typeName}' on '{transform.name}'.");
                    UObject.DestroyImmediate(component);
                    continue;
                }

                // Destroy Credits/Tab specific components (but keep Button/MyButton/EventTrigger)
                if (typeName.Contains("Credits") || typeName.Contains("Tab"))
                {
                    Main.Logger.LogDebug($"MenuFactory: Destroying game component '{typeName}' on '{transform.name}'.");
                    UObject.DestroyImmediate(component);
                }
            }
        }

        // Configure B_Back button instead of recreating it
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
        // Update title
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

        // Remove tab buttons if they exist
        var tabButtons = menu.transform.Find("TabButtons");
        if (tabButtons)
        {
            UObject.Destroy(tabButtons.gameObject);
            Main.Logger.LogDebug("MenuFactory: Removed tab buttons.");
        }

        // Recreate the B_Back button
        //RecreateBackButton(menu);

        // Add action controls (Input field + Save button + Open Folder button)
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

        // Find the main content area
        var contentTransform = menu.transform.Find("WindowLayers/Content");
        if (!contentTransform)
        {
            Main.Logger.LogWarning("MenuFactory: Could not find WindowLayers/Content for action controls.");
            return;
        }

        // ========== REMOVE THE HORIZONTAL LAYOUT GROUP ==========
        var horizontalLayoutGroup = contentTransform.GetComponent<HorizontalLayoutGroup>();
        if (horizontalLayoutGroup)
        {
            Main.Logger.LogDebug("MenuFactory: Found HorizontalLayoutGroup on Content - destroying it.");
            UObject.DestroyImmediate(horizontalLayoutGroup);
        }

        // ========== ADD A VERTICAL LAYOUT GROUP TO STACK SUB-HEADER AND SCROLLRECT ==========
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

        // ========== CREATE FIXED SUB-HEADER SECTION ==========
        var subHeaderContainer = new GameObject("SubHeader");
        subHeaderContainer.transform.SetParent(contentTransform, false);
        subHeaderContainer.transform.SetAsFirstSibling();

        var subHeaderRect = subHeaderContainer.AddComponent<RectTransform>();

        // Add LayoutElement to control size within VerticalLayoutGroup
        var subHeaderLayout = subHeaderContainer.AddComponent<LayoutElement>();
        subHeaderLayout.minHeight = 70f;
        subHeaderLayout.preferredHeight = 70f;
        subHeaderLayout.flexibleHeight = 0f;

        // Optional: Add background to sub-header
        var subHeaderBg = subHeaderContainer.AddComponent<Image>();
        subHeaderBg.color = new Color(0.05f, 0.05f, 0.05f, 0.5f);

        Main.Logger.LogDebug("MenuFactory: Created SubHeader with fixed 20px height.");

        // ========== MAKE SCROLLRECT FLEXIBLE (TAKES REMAINING SPACE) ==========
        var scrollRectTransform = menu.transform.Find("WindowLayers/Content/ScrollRect");
        if (scrollRectTransform)
        {
            scrollRectTransform.SetAsLastSibling();

            var scrollLayout = scrollRectTransform.gameObject.GetOrAddComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.minHeight = 100f;

            Main.Logger.LogDebug("MenuFactory: Configured ScrollRect to take remaining vertical space.");
        }

        // Get title text style for reference
        var titleText = menu.transform.Find("Header/Header/T_Title")?.GetComponent<TextMeshProUGUI>();

        // ========== INPUT FIELD (Left side, ~46% width) ==========
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

        // Add border
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

        // ========== SAVE BUTTON (Middle, ~17% width) ==========
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

        // ========== OPEN FOLDER BUTTON (Right, ~27% width) ==========
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

        Main.Logger.LogInfo("MenuFactory: Fixed sub-header section created successfully.");
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

        // Match title font if available
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

        // Match title font if available
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