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

namespace LoadoutPresets;

[BepInPlugin(Constants.GUID, Constants.MODNAME, Constants.VERSION)]
public class LoadoutPresets : BasePlugin
{
    private static string _loadoutPresetsFolder;
    private static ManualLogSource _logger;
    public static ManualLogSource Logger => _logger;

    private static readonly Dictionary<string, LoadoutData> _loadoutCacheByLoadoutName = [];

    public LoadoutPresets()
    {
        _logger = Log;
    }

    public override void Load()
    {
        _logger.LogInfo($"Loading {Constants.MODNAME} v{Constants.VERSION} by {Constants.AUTHOR}");

        _loadoutPresetsFolder = Path.Combine(Paths.ConfigPath, "LoadoutPresets");
        if (!Directory.Exists(_loadoutPresetsFolder))
        {
            Directory.CreateDirectory(_loadoutPresetsFolder);
            _logger.LogInfo($"Created LoadoutPresets folder at: {_loadoutPresetsFolder}");
        }

        var harmony = new Harmony(Constants.GUID);
        harmony.PatchAll();

        SceneManager.sceneLoaded += new Action<Scene, LoadSceneMode>(SceneManager_sceneLoaded);
    }

    public static string GetLoadoutPresetsFolder() => _loadoutPresetsFolder;

    private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _logger.LogInfo($"Scene loaded: {scene.name}");

        if (scene.name == "MainMenu")
        {
            var objects = scene.GetRootGameObjects();

            LoadoutsButton.CreateLoadoutsButton(objects);
            LoadoutsMenu.CreateLoadoutsMenu(objects);
        }
    }

    public static void SaveLoadoutFromInput(TMP_InputField inputField)
    {
        var loadoutName = inputField.text.Trim();

        if (string.IsNullOrEmpty(loadoutName))
        {
            _logger.LogWarning("Cannot save loadout: No name provided.");
            var placeholderText = inputField.placeholder.GetComponent<TextMeshProUGUI>();
            placeholderText.text = "Name Required";
            placeholderText.color = Color.red;

            return;
        }
        else
        {
            var placeholderText = inputField.placeholder.GetComponent<TextMeshProUGUI>();
            if (placeholderText != null)
            {
                placeholderText.text = "New loadout name...";
                placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        loadoutName = invalidChars.Aggregate(loadoutName, (current, c) => current.Replace(c, '_'));

        SaveCurrentLoadout(loadoutName);

        inputField.text = "";

        _logger.LogInfo($"Loadout '{loadoutName}' saved successfully.");
    }

    public static void OpenLoadoutPresetsFolder()
    {
        try
        {
            if (!Directory.Exists(_loadoutPresetsFolder))
            {
                Directory.CreateDirectory(_loadoutPresetsFolder);
            }

            Application.OpenURL(_loadoutPresetsFolder);

            _logger.LogInfo("Opened LoadoutPresets folder");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to open LoadoutPresets folder: {ex.Message}");
        }
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
            InactivatedUnlockables = [..SaveManager.Instance.progression.inactivated]
        };

        var filePath = Path.Combine(_loadoutPresetsFolder, $"{loadoutName}.json");
        var json = JsonSerializer.Serialize(loadout, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);

        _logger.LogInfo($"Loadout saved: {loadoutName}");
        _loadoutCacheByLoadoutName[loadoutName] = loadout;

        _logger.LogDebug($"Added loadout '{loadoutName}' in cache.");

        LoadoutsMenu.AddLoadoutListItem(loadoutName);
    }

    /// <summary>
    /// Updates the linked character for an existing loadout.
    /// </summary>
    public static void UpdateLoadoutCharacter(string loadoutName, ECharacter character)
    {
        var loadoutData = LoadLoadoutData(loadoutName);
        if (loadoutData == null)
        {
            _logger.LogError($"Cannot update character - loadout '{loadoutName}' not found.");
            return;
        }

        var characterDisplayName = character.GetDisplayName();
        loadoutData.LinkedCharacter = characterDisplayName;

        var filePath = Path.Combine(_loadoutPresetsFolder, $"{loadoutName}.json");
        var json = JsonSerializer.Serialize(loadoutData, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);

        _logger.LogInfo($"Updated loadout '{loadoutName}' - linked character: {characterDisplayName ?? "None"}");

        LoadoutsMenu.UpdateLoadoutListItemCharacter(loadoutName, character);

        if (_loadoutCacheByLoadoutName.ContainsKey(loadoutName))
        {
            _loadoutCacheByLoadoutName[loadoutName].LinkedCharacter = characterDisplayName;
            _logger.LogDebug($"Updated linked character in cache for loadout '{loadoutName}'");
        }

        var loadoutsWithMatchingCharacter = _loadoutCacheByLoadoutName
            .Where(x => x.Value.LinkedCharacter == characterDisplayName)
            .Where(x => x.Key != loadoutName);

        if (!loadoutsWithMatchingCharacter.Any())
            return;

        foreach (var matchingLoadout in loadoutsWithMatchingCharacter)
        {
            matchingLoadout.Value.LinkedCharacter = null;
            filePath = Path.Combine(_loadoutPresetsFolder, $"{matchingLoadout.Key}.json");
            json = JsonSerializer.Serialize(matchingLoadout.Value, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
            LoadoutsMenu.UpdateLoadoutListItemCharacter(matchingLoadout.Key, null);
        }
    }

    /// <summary>
    /// Removes the linked character from an existing loadout.
    /// </summary>
    public static void RemoveLoadoutCharacter(string loadoutName)
    {
        var loadoutData = LoadLoadoutData(loadoutName);
        if (loadoutData == null)
        {
            _logger.LogError($"Cannot remove character - loadout '{loadoutName}' not found.");
            return;
        }

        loadoutData.LinkedCharacter = null;

        var filePath = Path.Combine(_loadoutPresetsFolder, $"{loadoutName}.json");
        var json = JsonSerializer.Serialize(loadoutData, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);

        _logger.LogInfo($"Removed linked character from loadout '{loadoutName}'");

        LoadoutsMenu.UpdateLoadoutListItemCharacter(loadoutName, null);

        if (_loadoutCacheByLoadoutName.ContainsKey(loadoutName))
        {
            _loadoutCacheByLoadoutName[loadoutName].LinkedCharacter = null;
            _logger.LogDebug($"Updated cache for loadout '{loadoutName}' - removed linked character");
        }
    }

    public static LoadoutData LoadLoadoutData(string loadoutName)
    {
        if (_loadoutCacheByLoadoutName.TryGetValue(loadoutName, out var cachedLoadout))
        {
            _logger.LogDebug($"Loadout data for '{loadoutName}' retrieved from cache.");
            return cachedLoadout;
        }

        var filePath = Path.Combine(_loadoutPresetsFolder, $"{loadoutName}.json");
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<LoadoutData>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to load loadout data for '{loadoutName}': {ex.Message}");
            return null;
        }
    }

    public static void ActivateLoadout(string loadoutName)
    {
        if (!SaveManager.Instance)
        {
            _logger.LogError("SaveManager is null, cannot load loadout");
            return;
        }

        var loadoutData = LoadLoadoutData(loadoutName);
        if (loadoutData == null)
        {
            _logger.LogError($"Failed to load loadout data for '{loadoutName}'");
            return;
        }

        SaveManager.Instance.progression.inactivated.Clear();
        foreach (var inactivatedItem in loadoutData.InactivatedUnlockables)
        {
            SaveManager.Instance.progression.inactivated.Add(inactivatedItem);
        }

        _logger.LogInfo($"Loadout '{loadoutName}' loaded successfully. Restored {loadoutData.InactivatedUnlockables.Count} inactivated items.");

        SaveManager.Instance.SaveProgression();
    }

    public static void DeleteLoadout(string loadoutName)
    {
        if (_loadoutCacheByLoadoutName.ContainsKey(loadoutName))
        {
            _loadoutCacheByLoadoutName.Remove(loadoutName);
            _logger.LogDebug($"Removed '{loadoutName}' from cache.");
        }

        var filePath = Path.Combine(_loadoutPresetsFolder, $"{loadoutName}.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"Cannot delete - loadout file not found: {filePath}");
            return;
        }

        File.Delete(filePath);
        _logger.LogInfo($"Deleted loadout: {loadoutName}");
    }

    public static IEnumerable<LoadoutData> LoadoutDatasCache()
    {
        if (_loadoutCacheByLoadoutName.Count > 0)
        {
            _logger.LogDebug($"Returning all loadouts from cache ({_loadoutCacheByLoadoutName.Count} loadouts).");
            return _loadoutCacheByLoadoutName.Values;
        }

        if (!Directory.Exists(_loadoutPresetsFolder))
            return [];

        var files = Directory.GetFiles(_loadoutPresetsFolder, "*.json");

        foreach (var file in files)
        {
            var loadoutName = Path.GetFileNameWithoutExtension(file);
            var loadoutData = LoadLoadoutData(loadoutName);
            if (loadoutData != null)
            {
                _loadoutCacheByLoadoutName[loadoutName] = loadoutData;
                _logger.LogDebug($"Loaded loadout '{loadoutName}' into collection.");
            }
        }
        return _loadoutCacheByLoadoutName.Values;
    }

    public static void ActivateLoadoutFromCharacter(ECharacter? eCharacter)
    {
        if (!eCharacter.HasValue)
        {
            Logger.LogWarning("ActivateLoadoutFromCharacter called with null character.");
            return;
        }

        var characterName = eCharacter.GetDisplayName();
        var characterLoadout = LoadoutDatasCache()
            .FirstOrDefault(ld => string.Equals(ld.LinkedCharacter, characterName, StringComparison.OrdinalIgnoreCase));

        if (characterLoadout == null)
        {
            Logger.LogDebug($"No loadout linked to character {characterName}, skipping loadout activation.");
            return;
        }

        Logger.LogInfo($"Activating loadout '{characterLoadout.Name}' linked to character '{characterName}'.");
        ActivateLoadout(characterLoadout.Name);
    }
}

[Serializable]
public class LoadoutData
{
    public string Name { get; set; }
    public DateTime SavedAt { get; set; }
    public string LinkedCharacter { get; set; }
    public List<string> InactivatedUnlockables { get; set; }
}