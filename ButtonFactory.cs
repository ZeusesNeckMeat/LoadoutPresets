using System;
using System.Linq;

using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Main = LoadoutPresets.LoadoutPresets;
using UObject = UnityEngine.Object;

namespace LoadoutPresets;

/* * SUMMARY: THE "NATIVE CLONE" ARCHITECTURE
 * * This factory creates a "Hybrid Button" composed of two primary scripts 
 * residing on a single GameObject:
 * * 1. UnityEngine.UI.Button (The "Input"): 
 * Provides the standard Unity event system support for onClick listeners.
 * * 2. MyButtonNormal (The "Logic/Zombie"): 
 * A game-specific component that handles the visual identity of the menu.
 * Since this is added at runtime, we must manually re-link its "anatomy":
 * - .background:   Assigns the Image to be tinted (Color 0.340).
 * - .scaleOnHover: Assigns the Transform to be scaled during hover.
 * * By using both, we gain standard UI functionality while perfectly 
 * replicating the game's custom hover, arrow, and scaling animations.
 */
internal static class ButtonFactory
{
    /// <summary>
    /// Creates a native-styled button by cloning and configuring a template button.
    /// </summary>
    /// <param name="templateButton">The native button to clone (e.g., Settings button)</param>
    /// <param name="buttonName">Name for the new button GameObject</param>
    /// <param name="buttonText">Text to display on the button</param>
    /// <param name="iconName">Optional icon name from Resources (empty for no icon)</param>
    /// <param name="customParent">Optional custom parent transform (null to use template's parent)</param>
    /// <returns>Configured Button component</returns>
    public static Button CreateNativeButton(
        GameObject templateButton,
        string buttonName,
        string buttonText,
        string iconName,
        Transform customParent = null)
    {
        Main.Logger.LogDebug($"ButtonFactory: Starting creation of '{buttonName}' using template '{templateButton.name}'.");

        var clonedButton = UObject.Instantiate(templateButton);
        clonedButton.name = buttonName;

        var targetParent = customParent ?? templateButton.transform.parent;
        clonedButton.transform.SetParent(targetParent, false);

        // Set sibling index (only if using template's parent for consistency)
        if (customParent == null)
        {
            clonedButton.transform.SetSiblingIndex(templateButton.transform.GetSiblingIndex());
            Main.Logger.LogDebug($"ButtonFactory: Set hierarchy for '{buttonName}' at SiblingIndex {clonedButton.transform.GetSiblingIndex()} under '{targetParent.name}'.");
        }
        else
        {
            Main.Logger.LogDebug($"ButtonFactory: Set custom parent for '{buttonName}' under '{targetParent.name}'.");
        }

        StripNonVisualComponents(clonedButton);

        var iconTransform = clonedButton.transform.Find("Icon");
        if (iconTransform)
        {
            if (!string.IsNullOrEmpty(iconName))
            {
                SetButtonIcon(iconTransform, iconName);
            }
            else
            {
                iconTransform.gameObject.SetActive(false);
                Main.Logger.LogDebug($"ButtonFactory: No icon requested, hiding icon.");
            }
        }

        var buttonComponent = clonedButton.GetOrAddComponent<Button>();
        SetupNativeBehavior(buttonComponent, templateButton, buttonText);

        Main.Logger.LogDebug($"ButtonFactory: Successfully created and configured '{buttonName}'.");

        return buttonComponent;
    }

    private static void StripNonVisualComponents(GameObject targetGameObject)
    {
        var allComponents = targetGameObject.GetComponents<Component>();
        Main.Logger.LogDebug($"ButtonFactory: Analyzing {allComponents.Length} components for stripping on '{targetGameObject.name}'.");

        foreach (var component in allComponents)
        {
            var typeName = component.GetIl2CppType().Name;

            if (typeName == "RectTransform" ||
                typeName == "CanvasRenderer" ||
                typeName == "Image" ||
                typeName == "Mask")
            {
                continue;
            }

            Main.Logger.LogDebug($"ButtonFactory: Stripping native component '{typeName}'.");
            UObject.DestroyImmediate(component);
        }
    }

    private static void SetButtonIcon(Transform iconTransform, string textureName)
    {
        if (iconTransform == null)
            return;

        var rawImage = iconTransform.GetComponent<RawImage>();
        if (rawImage == null)
            return;

        var healthIconTexture = Resources.FindObjectsOfTypeAll<Texture2D>()
            .FirstOrDefault(t => t.name == textureName);

        if (healthIconTexture != null)
        {
            rawImage.texture = healthIconTexture;
            rawImage.color = Color.white;
            iconTransform.gameObject.SetActive(true);
            Main.Logger.LogDebug($"ButtonFactory: Assigned texture '{textureName}' to icon and set active.");
        }
        else
        {
            iconTransform.gameObject.SetActive(false);
            Main.Logger.LogWarning($"Texture {textureName} not found. Hiding icon.");
        }
    }

    private static void SetupNativeBehavior(Button targetButtonComponent, GameObject templateSourceGameObject, string newTextValue)
    {
        var targetGameObject = targetButtonComponent.gameObject;
        var zombieButtonComponent = targetGameObject.AddComponent<MyButtonNormal>();
        var templateButtonComponent = templateSourceGameObject.GetComponent<MyButtonNormal>();
        var buttonImageComponent = targetButtonComponent.GetComponent<Image>();

        // Ensure the Image has a sprite to prevent NullReferenceException during layout calculations
        if (buttonImageComponent && buttonImageComponent.sprite == null)
        {
            var templateImage = templateSourceGameObject.GetComponent<Image>();
            if (templateImage && templateImage.sprite != null)
            {
                buttonImageComponent.sprite = templateImage.sprite;
                Main.Logger.LogDebug($"ButtonFactory: Assigned sprite from template to prevent layout errors.");
            }
            else
            {
                var anySprite = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault();
                if (anySprite != null)
                {
                    buttonImageComponent.sprite = anySprite;
                    Main.Logger.LogDebug($"ButtonFactory: Assigned fallback sprite '{anySprite.name}' to prevent layout errors.");
                }
            }
        }

        zombieButtonComponent.background = buttonImageComponent;
        targetButtonComponent.transition = Selectable.Transition.None;
        Main.Logger.LogDebug($"ButtonFactory: Linked background image to '{targetGameObject.name}' and disabled standard transitions.");

        var textTransform = targetButtonComponent.transform.Find("T_Text");
        if (textTransform)
        {
            zombieButtonComponent.scaleOnHover = textTransform;

            if (templateButtonComponent)
            {
                zombieButtonComponent.hoverScale = templateButtonComponent.hoverScale;
                Main.Logger.LogDebug($"ButtonFactory: Linked scaleOnHover to '{textTransform.name}' with scale factor {zombieButtonComponent.hoverScale}.");
            }
        }
        else
        {
            Main.Logger.LogWarning($"ButtonFactory: Could not find 'T_Text' child for scaling on '{targetGameObject.name}'.");
        }

        if (templateButtonComponent)
        {
            zombieButtonComponent.defaultColor = templateButtonComponent.defaultColor;
            zombieButtonComponent.hoverColor = templateButtonComponent.hoverColor;
            zombieButtonComponent.colorInited = true;
            Main.Logger.LogDebug($"ButtonFactory: Copied colors from template (Hover: {zombieButtonComponent.hoverColor}).");
        }

        UpdateTextComponent(targetButtonComponent.transform, newTextValue);
        SetupEventTriggers(targetButtonComponent);
    }

    private static void UpdateTextComponent(Transform buttonTransform, string newTextValue)
    {
        var textTransform = buttonTransform.Find("T_Text");
        if (!textTransform)
        {
            Main.Logger.LogError($"ButtonFactory: UpdateTextComponent failed - 'T_Text' not found under '{buttonTransform.gameObject.name}'.");
            return;
        }

        var textGameObject = textTransform.gameObject;
        textGameObject.name = $"{buttonTransform.gameObject.name}_Text_Protected";

        var textMeshComponent = textGameObject.GetComponent<TextMeshProUGUI>();
        if (textMeshComponent)
        {
            textMeshComponent.text = newTextValue;
            Main.Logger.LogDebug($"ButtonFactory: Updated text value to '{newTextValue}' and protected object name.");
        }

        var textComponents = textGameObject.GetComponents<Component>();
        foreach (var textComponent in textComponents)
        {
            var componentTypeName = textComponent.GetIl2CppType().Name;

            if (componentTypeName.Contains("Localize"))
            {
                Main.Logger.LogDebug($"ButtonFactory: Destroying localization component '{componentTypeName}' to prevent text reverts.");
                UObject.DestroyImmediate(textComponent);
            }
        }
    }

    private static void SetupEventTriggers(Button targetButtonComponent)
    {
        var eventTriggerComponent = targetButtonComponent.gameObject.AddComponent<EventTrigger>();

        AddTrigger(eventTriggerComponent, EventTriggerType.Select, eventData =>
        {
            if (targetButtonComponent == null || targetButtonComponent.WasCollected)
            {
                Main.Logger.LogDebug("Button Event [Select]: Button was destroyed, skipping.");
                return;
            }

            Main.Logger.LogDebug($"Button Event: '{targetButtonComponent.gameObject.name}' Selected.");

            var myButton = targetButtonComponent.GetComponent<MyButtonNormal>();
            if (myButton && !myButton.WasCollected)
            {
                myButton.StartHover();
            }
        });

        AddTrigger(eventTriggerComponent, EventTriggerType.Deselect, eventData =>
        {
            if (targetButtonComponent == null || targetButtonComponent.WasCollected)
            {
                Main.Logger.LogDebug("Button Event [Deselect]: Button was destroyed, skipping.");
                return;
            }

            Main.Logger.LogDebug($"Button Event: '{targetButtonComponent.gameObject.name}' Deselected.");

            var myButton = targetButtonComponent.GetComponent<MyButtonNormal>();
            if (myButton && !myButton.WasCollected)
            {
                myButton.StopHover();
            }

            var overlayTransform = targetButtonComponent.transform.Find("DisabledOverlay");
            if (overlayTransform)
            {
                overlayTransform.gameObject.SetActive(false);
            }
        });

        AddTrigger(eventTriggerComponent, EventTriggerType.PointerClick, eventData =>
        {
            if (targetButtonComponent == null || targetButtonComponent.WasCollected)
            {
                Main.Logger.LogDebug("Button Event [PointerClick]: Button was destroyed, skipping.");
                return;
            }

            Main.Logger.LogDebug($"Button Event: '{targetButtonComponent.gameObject.name}' PointerClick.");

            var myButton = targetButtonComponent.GetComponent<MyButtonNormal>();
            if (myButton && !myButton.WasCollected)
            {
                myButton.StartHover();
            }
        });

        Main.Logger.LogDebug($"ButtonFactory: Configured EventTriggers (Select, Deselect, PointerClick) for '{targetButtonComponent.gameObject.name}'.");
    }

    private static void AddTrigger(EventTrigger triggerComponent, EventTriggerType triggerType, Action<BaseEventData> triggerAction)
    {
        var eventEntry = new EventTrigger.Entry
        {
            eventID = triggerType
        };

        eventEntry.callback.AddListener(triggerAction);
        triggerComponent.triggers.Add(eventEntry);
    }
}