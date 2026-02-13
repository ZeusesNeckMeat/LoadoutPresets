using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Managers;

using HarmonyLib;

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;

using Main = LoadoutPresets.LoadoutPresets;

namespace LoadoutPresets;

[HarmonyPatch(typeof(GameManager), nameof(GameManager.OnPlayerInit))]
internal static class GameManager_OnPlayerInit_Patch
{
    [HarmonyPostfix]
    private static void Postfix(GameManager __instance, PlayerInventory inventory)
    {
        try
        {
            if (!MapController.IsFirstStage())
            {
                Main.Logger.LogDebug("Not first stage, skipping loadout activation.");
                return;
            }

            Main.Logger.LogDebug("GameManager.OnPlayerInit");
            Main.Logger.LogDebug($"Current unlockables count: {RunUnlockables.availableItems?.Count}");

            Main.Logger.LogDebug($"Player instance from GameManager: {__instance?.player?.character}");
            Main.Logger.LogDebug($"CharacterData from Inventory: {inventory?.characterData?.eCharacter}");

            Main.ActivateLoadoutFromCharacter(__instance?.player?.character);
        }
        catch (Exception ex)
        {
            Main.Logger.LogError($"Error in GameManager_OnPlayerInit_Patch: {ex.Message}\n{ex.StackTrace}");
        }
    }
}

[HarmonyPatch(typeof(MyButtonCharacter), nameof(MyButtonCharacter.SetCharacter))]
internal static class MyButtonCharacter_SetCharacter_Patch
{
    private static readonly HashSet<IntPtr> _hookedButtons = new();
    public static readonly Dictionary<IntPtr, ECharacter> CharacterCache = new();

    [HarmonyPostfix]
    private static void Postfix(MyButtonCharacter __instance, CharacterData data)
    {
        try
        {
            // Null check the instance first
            if (__instance == null)
            {
                Main.Logger.LogWarning("MyButtonCharacter.SetCharacter: __instance is null");
                return;
            }

            var instancePtr = __instance.Pointer;

            if (__instance.WasCollected)
            {
                Main.Logger.LogWarning($"MyButtonCharacter.SetCharacter: Skipping button (already collected) - {data?.icon?.name}");
                return;
            }

            // Only hook each button once
            if (_hookedButtons.Contains(instancePtr))
                return;

            // Check if transform is valid before traversing hierarchy
            if (__instance.transform == null)
            {
                Main.Logger.LogWarning($"MyButtonCharacter.SetCharacter: transform is null for {data?.icon?.name}");
                return;
            }

            // CHECK: Is this button inside OUR cloned character select menu?
            Transform current = __instance.transform;
            bool isOurButton = false;

            while (current != null)
            {
                if (current.name == "W_LoadoutCharacterSelector")
                {
                    isOurButton = true;
                    break;
                }
                current = current.parent;
            }

            // If this button is NOT in our menu, skip it
            if (!isOurButton)
            {
                Main.Logger.LogDebug($"MyButtonCharacter.SetCharacter: Skipping button (not in our menu) - {data?.icon?.name}");
                return;
            }

            _hookedButtons.Add(instancePtr);

            Main.Logger.LogDebug($"MyButtonCharacter.SetCharacter: Hooking LoadoutPresets button for {data?.icon?.name}");

            // Store the character enum in cache
            var eCharacter = data?.eCharacter ?? ECharacter.SirOofie;
            CharacterCache[instancePtr] = eCharacter;

            // Add a Unity event listener to intercept clicks
            var eventTrigger = __instance.gameObject.GetOrAddComponent<EventTrigger>();
            Main.Logger.LogDebug($"MyButtonCharacter.SetCharacter: Adding EventTrigger listener for {data?.icon?.name} - EventTrigger component: {eventTrigger?.name ?? "null"}");
            if (eventTrigger != null)
            {
                // Create a new PointerClick entry
                var entry = new EventTrigger.Entry
                {
                    eventID = EventTriggerType.PointerClick
                };

                // Inject callback
                entry.callback.AddListener(new Action<BaseEventData>((eventData) =>
                {
                    try
                    {
                        var currentLoadout = LoadoutsMenu.GetCurrentEditingLoadout();
                        if (!string.IsNullOrEmpty(currentLoadout))
                        {
                            Main.Logger.LogDebug($"EventTrigger: Character button clicked while editing loadout '{currentLoadout}' - Character: {eCharacter}");
                            LoadoutsMenu.OnCharacterSelected(eCharacter);
                        }
                    }
                    catch (Exception ex)
                    {
                        Main.Logger.LogError($"Error in EventTrigger callback: {ex}");
                    }
                }));

                // Add the entry at the BEGINNING so it runs first
                eventTrigger.triggers.Insert(0, entry);
                Main.Logger.LogDebug($"Added EventTrigger listener for character: {eCharacter}");

                // Deactivate unneeded components
                var instanceTransform = __instance.transform;
                Main.Logger.LogDebug($"Cleaning up button components for {eCharacter} - Parent: {instanceTransform?.name}");
                var characterSelectedOverlayTransform = instanceTransform.Find("CharacterSelectedOverlay");
                if (characterSelectedOverlayTransform?.gameObject)
                {
                    Main.Logger.LogDebug("Disabling CharacterSelectedOverlay game object");
                    characterSelectedOverlayTransform.gameObject.SetActive(false);
                }

                var rankTransform = instanceTransform.Find("T_Rank");
                if (rankTransform?.gameObject)
                {
                    Main.Logger.LogDebug("Disabling T_Rank game object");
                    rankTransform.gameObject.SetActive(false);
                }

                var starTransform = instanceTransform.Find("Star");
                if (starTransform?.gameObject)
                {
                    Main.Logger.LogDebug("Disabling Star game object");
                    starTransform.gameObject.SetActive(false);
                }

                var requiresPurchaseTransform = instanceTransform.Find("RequiresPurchase (1)");
                if (requiresPurchaseTransform?.gameObject)
                {
                    Main.Logger.LogDebug("Disabling RequiresPurchase game object");
                    requiresPurchaseTransform.gameObject.SetActive(false);
                }
            }
            else
            {
                Main.Logger.LogDebug($"No EventTrigger component found, falling back to detecting via cache");
            }

            Main.Logger.LogDebug($"Cached character {eCharacter} for button {instancePtr}");
        }
        catch (Exception ex)
        {
            Main.Logger.LogError($"Error in MyButtonCharacter_SetCharacter_Patch: {ex.Message}\n{ex.StackTrace}");
        }
    }
}