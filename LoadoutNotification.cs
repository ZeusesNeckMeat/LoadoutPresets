using Il2CppInterop.Runtime.InteropTypes.Arrays;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using Main = LoadoutPresets.LoadoutPresets;
using UObject = UnityEngine.Object;

namespace LoadoutPresets
{
    internal static class LoadoutNotification
    {
        private static GameObject _popupClone;

        public static void Initialize(Il2CppReferenceArray<GameObject> rootGameObjects)
        {
            // Bailing for now as this will be implemented in the future
            return;
            
            //if (LoadoutNotification._popupClone && !LoadoutNotification._popupClone.WasCollected)
            //{
            //    Main.Logger.LogInfo("Loadout notification already initialized.");
            //    return;
            //}

            //var alwaysManagers = rootGameObjects.FirstOrDefault(go => string.Equals(go.name, "AlwaysManagers", StringComparison.OrdinalIgnoreCase));
            //if (!alwaysManagers)
            //{
            //    Main.Logger.LogWarning("Failed to find AlwaysManagers component. Loadout Notifications Disabled.");
            //    return;
            //}

            //var achievementsPopup = alwaysManagers.transform.Find("AlwaysUI/Canvas/AchievementPopup");
            //if (!achievementsPopup)
            //{
            //    Main.Logger.LogWarning("Failed to find AchievementPopup component. Loadout Notifications Disabled.");
            //    return;
            //}

            //_popupClone = UObject.Instantiate(achievementsPopup.gameObject);
            //_popupClone.name = "LoadoutNotifications";
            //_popupClone.transform.SetParent(achievementsPopup.parent, false);

            //var popupTransform = _popupClone.transform.Find("Popup");
        }
    }
}