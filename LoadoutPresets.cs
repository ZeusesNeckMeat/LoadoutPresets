using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

using HarmonyLib;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

using TMPro;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using UObject = UnityEngine.Object;

namespace LoadoutPresets;

[BepInPlugin(_GUID, _MODNAME, _VERSION)]
public class LoadoutPresets : BasePlugin
{
    private const string _MODNAME = "LoadoutPresets";
    private const string _AUTHOR = "ZeusesNeckMeat";
    private const string _GUID = _AUTHOR + "_" + _MODNAME;
    private const string _VERSION = "0.1.3";

    private static ManualLogSource _logger;
    private static string _LoadoutPresetsFolder;
    private static GameObject _loadoutMenuPanel;
    private static Transform _loadoutListContainer;
    private static GameObject _saveDialogPanel;
    private static TMP_InputField _loadoutNameInput;
    private static GameObject _loadoutButton;
    private static Button _referenceButton;

    public LoadoutPresets()
    {
        _logger = Log;
    }

    public override void Load()
    {
        _logger.LogInfo($"Loading {_MODNAME} v{_VERSION} by {_AUTHOR}");

        // Set up the LoadoutPresets folder
        _LoadoutPresetsFolder = Path.Combine(Paths.ConfigPath, "LoadoutPresets");
        if (!Directory.Exists(_LoadoutPresetsFolder))
        {
            Directory.CreateDirectory(_LoadoutPresetsFolder);
            _logger.LogInfo($"Created LoadoutPresets folder at: {_LoadoutPresetsFolder}");
        }

        var harmony = new Harmony(_GUID);
        harmony.PatchAll();

        SceneManager.sceneLoaded += new Action<Scene, LoadSceneMode>(SceneManager_sceneLoaded);
    }

    private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _logger.LogInfo($"Scene loaded: {scene.name}");

        if (scene.name != "MainMenu")
            return;

        var objects = scene.GetRootGameObjects();
        var uiRoot = objects.FirstOrDefault(x => x.name == "UI");
        if (!uiRoot)
        {
            _logger.LogError("UI root object not found!");
            return;
        }

        var extraButtons = uiRoot.transform.Find("Tabs/Menu/Content/Main/ExtraButtons");
        if (!extraButtons)
        {
            _logger.LogError("ExtraButtons ui component not found!");
            return;
        }

        var existingButton = extraButtons.Find("B_LoadoutPresets");
        if (existingButton)
        {
            _logger.LogInfo("LoadoutPresets button already exists, skipping creation");
            return;
        }

        var settingsTransform = extraButtons.Find("B_Settings");
        if (!settingsTransform)
        {
            _logger.LogError("Settings button not found!");
            return;
        }

        var settingsButton = settingsTransform.GetComponent<Button>();
        CreateLoadoutButton(extraButtons, settingsButton);
    }

    private void CreateLoadoutButton(Transform parentTransform, Button settingsButton)
    {
        _logger.LogInfo("Creating LoadoutPresets button...");

        _referenceButton = settingsButton;

        // Get reference data from Settings button
        var settingsRect = settingsButton.GetComponent<RectTransform>();
        var settingsImage = settingsButton.GetComponent<Image>();
        var settingsTMProText = settingsButton.GetComponentInChildren<TextMeshProUGUI>();
        var settingsIconTransform = settingsButton.transform.Find("Icon");
        var settingsDisabledOverlay = settingsButton.transform.Find("DisabledOverlay");

        var settingsIndex = settingsButton.transform.GetSiblingIndex();

        // Create new button
        _loadoutButton = new GameObject("B_LoadoutPresets");
        _loadoutButton.transform.SetParent(parentTransform, false);
        _loadoutButton.transform.SetSiblingIndex(settingsIndex);

        // Add RectTransform with same settings as Settings button
        var loadoutRect = _loadoutButton.AddComponent<RectTransform>();
        loadoutRect.sizeDelta = settingsRect.sizeDelta;
        loadoutRect.anchorMin = settingsRect.anchorMin;
        loadoutRect.anchorMax = settingsRect.anchorMax;
        loadoutRect.pivot = settingsRect.pivot;

        // Add Image component
        var buttonImage = _loadoutButton.AddComponent<Image>();
        if (settingsImage)
        {
            buttonImage.sprite = settingsImage.sprite;
            buttonImage.color = settingsImage.color;
            buttonImage.type = settingsImage.type;
        }

        // Add Button component
        var button = _loadoutButton.AddComponent<Button>();
        button.colors = settingsButton.colors;
        button.transition = settingsButton.transition;
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(new Action(OnLoadoutPresetsButtonClicked));

        if (settingsTMProText)
        {
            var textObj = new GameObject("T_Text");
            textObj.transform.SetParent(_loadoutButton.transform, false);

            // Copy RectTransform settings from the original
            var textRect = textObj.AddComponent<RectTransform>();
            var settingsTextRect = settingsTMProText.GetComponent<RectTransform>();
            textRect.anchorMin = settingsTextRect.anchorMin;
            textRect.anchorMax = settingsTextRect.anchorMax;
            textRect.anchoredPosition = settingsTextRect.anchoredPosition;
            textRect.sizeDelta = settingsTextRect.sizeDelta;
            textRect.pivot = settingsTextRect.pivot;

            // Create new TextMeshProUGUI component with settings from original
            var tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = "Loadouts";
            tmpText.fontSize = settingsTMProText.fontSize;
            tmpText.font = settingsTMProText.font;
            tmpText.color = settingsTMProText.color;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.fontStyle = settingsTMProText.fontStyle;
            tmpText.fontSizeMin = settingsTMProText.fontSizeMin;
            tmpText.fontSizeMax = settingsTMProText.fontSizeMax;
            tmpText.autoSizeTextContainer = true;
            tmpText.margin = settingsTMProText.margin;
        }

        _logger.LogInfo("LoadoutPresets button created successfully!");

        // Create menu panels
        if (!_loadoutMenuPanel)
        {
            CreateLoadoutMenu(parentTransform.root);
        }
    }

    private void CreateLoadoutMenu(Transform canvasRoot)
    {
        _loadoutMenuPanel = new GameObject("LoadoutMenuPanel");
        _loadoutMenuPanel.transform.SetParent(canvasRoot, false);

        // Add the Canvas component for proper layering
        var menuCanvas = _loadoutMenuPanel.AddComponent<Canvas>();
        menuCanvas.overrideSorting = true;
        menuCanvas.sortingOrder = 100;

        // Add GraphicRaycaster for button clicks
        _loadoutMenuPanel.AddComponent<GraphicRaycaster>();

        // Configure RectTransform
        var rectTransform = _loadoutMenuPanel.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;

        // Add background
        var bgImage = _loadoutMenuPanel.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);

        // Add close button on a background
        var bgButton = _loadoutMenuPanel.AddComponent<Button>();
        bgButton.onClick.AddListener(new Action(CloseLoadoutMenu));
        bgButton.navigation = new Navigation
        {
            mode = Navigation.Mode.None
        };

        // Create content panel
        var contentPanel = new GameObject("ContentPanel");
        contentPanel.transform.SetParent(_loadoutMenuPanel.transform, false);

        var contentRect = contentPanel.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.2f, 0.2f);
        contentRect.anchorMax = new Vector2(0.8f, 0.8f);
        contentRect.sizeDelta = Vector2.zero;

        var contentImage = contentPanel.AddComponent<Image>();
        contentImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        // Create title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(contentPanel.transform, false);

        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.9f);
        titleRect.anchorMax = new Vector2(1, 1f);
        titleRect.sizeDelta = Vector2.zero;

        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Loadout Manager";
        titleText.fontSize = 36;
        titleText.alignment = TextAlignmentOptions.Center;

        // Create buttons
        CreateMenuButton(contentPanel.transform, "Save Current Loadout",
            new Vector2(0.05f, 0.82f), new Vector2(0.35f, 0.88f),
            ShowSaveDialog, _referenceButton);

        CreateMenuButton(contentPanel.transform, "Open Folder",
            new Vector2(0.37f, 0.82f), new Vector2(0.63f, 0.88f),
            OpenLoadoutPresetsFolder, _referenceButton);

        CreateMenuButton(contentPanel.transform, "Close",
            new Vector2(0.65f, 0.82f), new Vector2(0.95f, 0.88f),
            CloseLoadoutMenu, _referenceButton);

        // Create a scroll view
        var scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(contentPanel.transform, false);

        var scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.05f, 0.05f);
        scrollRect.anchorMax = new Vector2(0.95f, 0.78f);
        scrollRect.sizeDelta = Vector2.zero;

        var maskImage = scrollObj.AddComponent<Image>();
        maskImage.color = new Color(0.05f, 0.05f, 0.05f, 1f);
        var mask = scrollObj.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        // Create container
        var containerObj = new GameObject("Container");
        containerObj.transform.SetParent(scrollObj.transform, false);

        var containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.pivot = new Vector2(0.5f, 1);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = new Vector2(0, 0);

        _loadoutListContainer = containerObj.transform;

        // Add ScrollRect component for scrolling functionality
        var scrollRectComponent = scrollObj.AddComponent<ScrollRect>();
        scrollRectComponent.content = containerRect;
        scrollRectComponent.viewport = scrollRect;
        scrollRectComponent.horizontal = false;
        scrollRectComponent.vertical = true;
        scrollRectComponent.movementType = ScrollRect.MovementType.Clamped;
        scrollRectComponent.scrollSensitivity = 20f;
        scrollRectComponent.inertia = true;
        scrollRectComponent.decelerationRate = 0.135f;

        // Create vertical scrollbar
        var scrollbarObj = new GameObject("Scrollbar Vertical");
        scrollbarObj.transform.SetParent(scrollObj.transform, false);

        var scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 1);
        scrollbarRect.sizeDelta = new Vector2(20, 0);
        scrollbarRect.anchoredPosition = Vector2.zero;

        var scrollbarImage = scrollbarObj.AddComponent<Image>();
        scrollbarImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);

        var scrollbar = scrollbarObj.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        // Create scrollbar handle
        var handleObj = new GameObject("Sliding Area");
        handleObj.transform.SetParent(scrollbarObj.transform, false);

        var handleAreaRect = handleObj.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = new Vector2(-20, -20);
        handleAreaRect.anchoredPosition = Vector2.zero;

        var handleChildObj = new GameObject("Handle");
        handleChildObj.transform.SetParent(handleObj.transform, false);

        var handleRect = handleChildObj.AddComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.sizeDelta = Vector2.zero;

        var handleImage = handleChildObj.AddComponent<Image>();
        handleImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);

        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;

        // Link scrollbar to ScrollRect
        scrollRectComponent.verticalScrollbar = scrollbar;
        scrollRectComponent.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        _loadoutMenuPanel.SetActive(false);

        CreateSaveDialog(canvasRoot);
    }

    private void CreateSaveDialog(Transform canvasRoot)
    {
        // Create dialog panel
        _saveDialogPanel = new GameObject("SaveDialogPanel");
        _saveDialogPanel.transform.SetParent(canvasRoot, false);

        // Add Canvas component with HIGHER sorting order than the loadout menu
        var dialogCanvas = _saveDialogPanel.AddComponent<Canvas>();
        dialogCanvas.overrideSorting = true;
        dialogCanvas.sortingOrder = 200; // Higher than loadout menu's 100

        // Add GraphicRaycaster for button clicks
        _saveDialogPanel.AddComponent<GraphicRaycaster>();

        var rectTransform = _saveDialogPanel.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;

        var bgImage = _saveDialogPanel.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.9f);

        // Create dialog content
        var dialogPanel = new GameObject("DialogContent");
        dialogPanel.transform.SetParent(_saveDialogPanel.transform, false);

        var dialogRect = dialogPanel.AddComponent<RectTransform>();
        dialogRect.anchorMin = new Vector2(0.3f, 0.4f);
        dialogRect.anchorMax = new Vector2(0.7f, 0.6f);
        dialogRect.sizeDelta = Vector2.zero;

        var dialogImage = dialogPanel.AddComponent<Image>();
        dialogImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // Create title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(dialogPanel.transform, false);

        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.7f);
        titleRect.anchorMax = new Vector2(0.95f, 0.9f);
        titleRect.sizeDelta = Vector2.zero;

        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Enter Loadout Name";
        titleText.fontSize = 28;
        titleText.alignment = TextAlignmentOptions.Center;

        // Create an input field
        var inputObj = new GameObject("InputField");
        inputObj.transform.SetParent(dialogPanel.transform, false);

        var inputRect = inputObj.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.1f, 0.45f);
        inputRect.anchorMax = new Vector2(0.9f, 0.65f);
        inputRect.sizeDelta = Vector2.zero;

        var inputImage = inputObj.AddComponent<Image>();
        inputImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        _loadoutNameInput = inputObj.AddComponent<TMP_InputField>();
        _loadoutNameInput.textComponent = CreateInputTextComponent(inputObj.transform);
        _loadoutNameInput.placeholder = CreatePlaceholderComponent(inputObj.transform);
        _loadoutNameInput.text = "";
        _loadoutNameInput.navigation = new Navigation
        {
            mode = Navigation.Mode.Automatic
        };

        // Create buttons
        CreateMenuButton(dialogPanel.transform, "Save",
            new Vector2(0.1f, 0.1f), new Vector2(0.45f, 0.35f),
            SaveLoadoutFromDialog, _referenceButton);

        CreateMenuButton(dialogPanel.transform, "Cancel",
            new Vector2(0.55f, 0.1f), new Vector2(0.9f, 0.35f),
            CloseSaveDialog, _referenceButton);

        _saveDialogPanel.SetActive(false);
    }

    private static TextMeshProUGUI CreateInputTextComponent(Transform parent)
    {
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0);
        textRect.anchorMax = new Vector2(0.95f, 1);
        textRect.sizeDelta = Vector2.zero;

        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 24;
        text.alignment = TextAlignmentOptions.Left;
        text.color = Color.white;

        return text;
    }

    private static TextMeshProUGUI CreatePlaceholderComponent(Transform parent)
    {
        var placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(parent, false);

        var placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchorMin = new Vector2(0.05f, 0);
        placeholderRect.anchorMax = new Vector2(0.95f, 1);
        placeholderRect.sizeDelta = Vector2.zero;

        var placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholder.text = "My Loadout";
        placeholder.fontSize = 24;
        placeholder.alignment = TextAlignmentOptions.Left;
        placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        return placeholder;
    }

    private static void CreateMenuButton(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax, Action onClick, Button referenceButton = null)
    {
        var buttonObj = new GameObject($"Button_{text}");
        buttonObj.transform.SetParent(parent, false);

        var rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;

        var image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.4f, 0.6f, 1f);

        var button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        // Copy color settings from the reference button
        if (referenceButton)
        {
            button.colors = referenceButton.colors;
            button.transition = referenceButton.transition;
        }
        else
        {
            var colors = new ColorBlock
            {
                normalColor = new Color(1f, 1f, 1f, 1f),
                highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f),
                pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f),
                selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f),
                disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };

            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;
        }

        button.navigation = new Navigation
        {
            mode = Navigation.Mode.Automatic
        };

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private static void ShowSaveDialog()
    {
        if (_saveDialogPanel)
        {
            _saveDialogPanel.SetActive(true);
            _loadoutNameInput.text = "Loadout_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            _loadoutNameInput.Select();
            _loadoutNameInput.ActivateInputField();
        }
    }

    private static void CloseSaveDialog()
    {
        if (_saveDialogPanel)
        {
            _saveDialogPanel.SetActive(false);
        }
    }

    private static void SaveLoadoutFromDialog()
    {
        var loadoutName = _loadoutNameInput.text.Trim();

        if (string.IsNullOrEmpty(loadoutName))
        {
            loadoutName = "Loadout_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        }

        // Sanitize filename
        var invalidChars = Path.GetInvalidFileNameChars();
        loadoutName = invalidChars.Aggregate(loadoutName, (current, c) => current.Replace(c, '_'));

        SaveCurrentLoadout(loadoutName);
        CloseSaveDialog();
        RefreshLoadoutList();
    }

    private static void OnLoadoutPresetsButtonClicked()
    {
        OpenLoadoutMenu();
    }

    private static void OpenLoadoutMenu()
    {
        if (!_loadoutMenuPanel)
            return;

        _loadoutMenuPanel.SetActive(true);
        RefreshLoadoutList();

        var saveButton = _loadoutMenuPanel.transform.Find("ContentPanel/Button_Save Current Loadout");
        if (!saveButton)
            return;

        var button = saveButton.GetComponent<Button>();
        if (button)
            button.Select();
    }

    private static void CloseLoadoutMenu()
    {
        if (_loadoutMenuPanel)
            _loadoutMenuPanel.SetActive(false);
    }

    private static void RefreshLoadoutList()
    {
        // Clear existing items
        for (int i = _loadoutListContainer.childCount - 1; i >= 0; i--)
        {
            UObject.Destroy(_loadoutListContainer.GetChild(i).gameObject);
        }

        if (!Directory.Exists(_LoadoutPresetsFolder))
            return;

        var files = Directory.GetFiles(_LoadoutPresetsFolder, "*.json");

        var yPos = 0f;
        foreach (var file in files)
        {
            var loadoutName = Path.GetFileNameWithoutExtension(file);
            CreateLoadoutListItem(loadoutName, yPos);
            yPos -= 60f;
        }

        var containerRect = _loadoutListContainer.GetComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(0, Math.Max(0, -yPos));
    }

    private static void CreateLoadoutListItem(string loadoutName, float yPos)
    {
        var itemObj = new GameObject($"LoadoutItem_{loadoutName}");
        itemObj.transform.SetParent(_loadoutListContainer, false);

        var rect = itemObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.sizeDelta = new Vector2(-10, 50);
        rect.anchoredPosition = new Vector2(0, yPos - 5);

        var image = itemObj.AddComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // Loadout name
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(itemObj.transform, false);

        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0);
        nameRect.anchorMax = new Vector2(0.5f, 1);
        nameRect.sizeDelta = Vector2.zero;

        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = loadoutName;
        nameText.fontSize = 20;
        nameText.alignment = TextAlignmentOptions.Left;
        nameText.margin = new Vector4(10, 0, 0, 0);

        // Load button
        var loadBtn = new GameObject("LoadButton");
        loadBtn.transform.SetParent(itemObj.transform, false);

        var loadRect = loadBtn.AddComponent<RectTransform>();
        loadRect.anchorMin = new Vector2(0.52f, 0.1f);
        loadRect.anchorMax = new Vector2(0.72f, 0.9f);
        loadRect.sizeDelta = Vector2.zero;

        var loadImage = loadBtn.AddComponent<Image>();
        loadImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);

        var loadButton = loadBtn.AddComponent<Button>();
        loadButton.targetGraphic = loadImage;
        loadButton.onClick.AddListener(new Action(() =>
        {
            LoadLoadout(loadoutName);
            CloseLoadoutMenu();
        }));

        loadButton.navigation = new Navigation
        {
            mode = Navigation.Mode.Automatic
        };

        var loadTextObj = new GameObject("Text");
        loadTextObj.transform.SetParent(loadBtn.transform, false);
        var loadTextRect = loadTextObj.AddComponent<RectTransform>();
        loadTextRect.anchorMin = Vector2.zero;
        loadTextRect.anchorMax = Vector2.one;
        loadTextRect.sizeDelta = Vector2.zero;
        var loadText = loadTextObj.AddComponent<TextMeshProUGUI>();
        loadText.text = "Load";
        loadText.fontSize = 18;
        loadText.alignment = TextAlignmentOptions.Center;

        // Delete button
        var deleteBtn = new GameObject("DeleteButton");
        deleteBtn.transform.SetParent(itemObj.transform, false);

        var deleteRect = deleteBtn.AddComponent<RectTransform>();
        deleteRect.anchorMin = new Vector2(0.75f, 0.1f);
        deleteRect.anchorMax = new Vector2(0.95f, 0.9f);
        deleteRect.sizeDelta = Vector2.zero;

        var deleteImage = deleteBtn.AddComponent<Image>();
        deleteImage.color = new Color(0.6f, 0.2f, 0.2f, 1f);

        var deleteButton = deleteBtn.AddComponent<Button>();
        deleteButton.targetGraphic = deleteImage;
        deleteButton.onClick.AddListener(new Action(() =>
        {
            DeleteLoadout(loadoutName);
            RefreshLoadoutList();
        }));

        deleteButton.navigation = new Navigation
        {
            mode = Navigation.Mode.Automatic
        };

        var deleteTextObj = new GameObject("Text");
        deleteTextObj.transform.SetParent(deleteBtn.transform, false);
        var deleteTextRect = deleteTextObj.AddComponent<RectTransform>();
        deleteTextRect.anchorMin = Vector2.zero;
        deleteTextRect.anchorMax = Vector2.one;
        deleteTextRect.sizeDelta = Vector2.zero;
        var deleteText = deleteTextObj.AddComponent<TextMeshProUGUI>();
        deleteText.text = "Delete";
        deleteText.fontSize = 18;
        deleteText.alignment = TextAlignmentOptions.Center;
    }

    private static void DeleteLoadout(string loadoutName)
    {
        var filePath = Path.Combine(_LoadoutPresetsFolder, $"{loadoutName}.json");
        if (!File.Exists(filePath))
            return;

        File.Delete(filePath);
        _logger.LogInfo($"Deleted loadout: {loadoutName}");
    }

    private static void SaveCurrentLoadout(string loadoutName)
    {
        if (!SaveManager.Instance)
        {
            _logger.LogError("SaveManager is null, cannot save loadout");
            return;
        }

        var loadout = new LoadoutData
        {
            Name = loadoutName,
            SavedAt = DateTime.Now,
            //ShopItems = new Dictionary<string, int>(),
            InactivatedUnlockables = []
        };

        //foreach (var shopItemPair in SaveManager.Instance.progression.shopItems)
        //{
        //    loadout.ShopItems[shopItemPair.Key.ToString()] = shopItemPair.Value;
        //}

        foreach (var inactivatedItem in SaveManager.Instance.progression.inactivated)
        {
            loadout.InactivatedUnlockables.Add(inactivatedItem);
        }

        var filePath = Path.Combine(_LoadoutPresetsFolder, $"{loadoutName}.json");
        var json = JsonSerializer.Serialize(loadout, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);

        _logger.LogInfo($"Loadout saved: {loadoutName}");
    }

    private static void LoadLoadout(string loadoutName)
    {
        var filePath = Path.Combine(_LoadoutPresetsFolder, $"{loadoutName}.json");
        if (!File.Exists(filePath))
        {
            _logger.LogError($"Loadout file not found: {filePath}");
            return;
        }

        var json = File.ReadAllText(filePath);
        var loadout = JsonSerializer.Deserialize<LoadoutData>(json);

        var dataManager = DataManager.Instance;
        if (!dataManager || !SaveManager.Instance)
        {
            _logger.LogError("DataManager or SaveManager is null");
            return;
        }

        // Apply shop items
        //foreach (var itemPair in loadout.ShopItems)
        //{
        //    if (!Enum.TryParse<EShopItem>(itemPair.Key, out var shopItemEnum))
        //        continue;

        //    if (!dataManager.shopItems.ContainsKey(shopItemEnum))
        //        continue;

        //    var shopItem = dataManager.shopItems[shopItemEnum];
        //    var currentLevel = shopItem.GetLevel();
        //    var targetLevel = itemPair.Value;

        //    while (currentLevel < targetLevel && shopItem.CanBuy())
        //    {
        //        SaveManager.Instance.progression.PurchaseShopItem(shopItem);
        //        currentLevel++;
        //    }

        //    while (currentLevel > targetLevel && shopItem.CanRefund())
        //    {
        //        SaveManager.Instance.progression.RefundShopItem(shopItem);
        //        currentLevel--;
        //    }
        //}

        // Restore inactivated items
        SaveManager.Instance.progression.inactivated.Clear();
        foreach (var inactivatedItem in loadout.InactivatedUnlockables)
        {
            SaveManager.Instance.progression.inactivated.Add(inactivatedItem);
        }

        SaveManager.Instance.SaveProgression();
        _logger.LogInfo($"Loadout loaded: {loadoutName}");
    }

    private static void OpenLoadoutPresetsFolder()
    {
        try
        {
            if (!Directory.Exists(_LoadoutPresetsFolder))
            {
                Directory.CreateDirectory(_LoadoutPresetsFolder);
            }

            Application.OpenURL(_LoadoutPresetsFolder);

            _logger.LogInfo("Opened LoadoutPresets folder");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to open LoadoutPresets folder: {ex.Message}");
        }
    }
}

[Serializable]
public class LoadoutData
{
    public string Name { get; set; }
    public DateTime SavedAt { get; set; }
    //public Dictionary<string, int> ShopItems { get; set; }
    public List<string> InactivatedUnlockables { get; set; }
}