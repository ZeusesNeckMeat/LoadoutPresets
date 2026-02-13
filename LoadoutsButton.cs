using Il2CppInterop.Runtime.InteropTypes.Arrays;

using System;
using System.Linq;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using Main = LoadoutPresets.LoadoutPresets;
using ObjectNames = LoadoutPresets.Constants.ObjectNames;

namespace LoadoutPresets;

internal static class LoadoutsButton
{
    private static Button _loadoutsButton;

    public static void CreateLoadoutsButton(Il2CppReferenceArray<GameObject> rootGameObjects)
    {
        var uiRoot = rootGameObjects.FirstOrDefault(gameObject => string.Equals(gameObject.name, ObjectNames.UI, StringComparison.OrdinalIgnoreCase));
        var findSuccess = TryGetSettingsButtonToClone(
            uiRoot,
            out var settingsButtonTransform,
            out var settingsButtonComponent
        );

        if (!findSuccess)
            return;

        _loadoutsButton = ButtonFactory.CreateNativeButton(
            settingsButtonComponent.gameObject,
            Constants.ButtonNames.LOADOUTS_BUTTON,
            "LOADOUTS",
            "toggler"
        );

        _loadoutsButton.onClick.AddListener((UnityAction)OpenLoadoutPresetsMenu);
    }

    private static bool TryGetSettingsButtonToClone(GameObject uiRootGameObject, out Transform settingsTransform, out Button settingsButtonComponent)
    {
        settingsTransform = null;
        settingsButtonComponent = null;

        if (!uiRootGameObject)
            return false;

        var extraButtonsTransform = uiRootGameObject.transform.Find("Tabs/Menu/Content/Main/ExtraButtons");
        if (!extraButtonsTransform)
            return false;

        var existingLoadoutsTransform = extraButtonsTransform.Find(Constants.ButtonNames.LOADOUTS_BUTTON);
        if (existingLoadoutsTransform)
            return false;

        settingsTransform = extraButtonsTransform.Find(Constants.ButtonNames.SETTINGS_BUTTON);
        if (!settingsTransform)
            return false;

        settingsButtonComponent = settingsTransform.GetComponent<Button>();
        return settingsButtonComponent;
    }

    private static void OpenLoadoutPresetsMenu()
    {
        Main.Logger.LogDebug("Opening Loadout Presets Menu...");
        LoadoutsMenu.OpenMenu();
        //LoadoutsMenu.Open();
    }
}