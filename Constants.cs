using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadoutPresets;

internal static class Constants
{
    public const string MODNAME = "LoadoutPresets";
    public const string AUTHOR = "ZeusesNeckMeat";
    public const string GUID = $"{AUTHOR}_{MODNAME}";
    public const string VERSION = "0.1.3";

    public class Scenes
    {
        public const string MAINMENU = "MainMenu";
    }

    public class ObjectNames
    {
        public const string UI = "UI";
    }

    public class ButtonNames
    {
        public const string LOADOUTS_BUTTON = "B_LoadoutPresets";
        public const string SETTINGS_BUTTON = "B_Settings";
    }
}