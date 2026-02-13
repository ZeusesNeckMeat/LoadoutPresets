namespace LoadoutPresets;

internal static class Constants
{
    public const string MODNAME = "LoadoutPresets";
    public const string AUTHOR = "ZeusesNeckMeat";
    public const string GUID = $"{AUTHOR}_{MODNAME}";
    public const string VERSION = "1.0.0";

    public static class Scenes
    {
        public const string MAINMENU = "MainMenu";
    }

    public static class ObjectNames
    {
        public const string UI = "UI";
    }

    public static class ButtonNames
    {
        public const string LOADOUTS_BUTTON = "B_LoadoutPresets";
        public const string SETTINGS_BUTTON = "B_Settings";
    }

    public static class ComponentNames
    {
        public const string ICON = "Icon";
        public const string TEXT = "T_Text";
        public const string DISABLED_OVERLAY = "DisabledOverlay";
        public const string CHARACTER_SELECTED_OVERLAY = "CharacterSelectedOverlay";
        public const string RANK_TEXT = "T_Rank";
        public const string STAR = "Star";
        public const string REQUIRES_PURCHASE = "RequiresPurchase (1)";
    }

    public static class UnityComponentTypes
    {
        public const string RECT_TRANSFORM = "RectTransform";
        public const string CANVAS_RENDERER = "CanvasRenderer";
        public const string IMAGE = "Image";
        public const string MASK = "Mask";
        public const string LOCALIZE = "Localize"; // For .Contains() check
    }

    public static class TransformPaths
    {
        public const string TABS = "Tabs";
        public const string MENU = "Menu";
        public const string CREDITS_MENU = "Tabs/W_Credits";
        public const string CHARACTER_SELECT = "Tabs/Character/W_Character";
        public const string HEADER_TITLE = "Header/Header/T_Title";
        public const string HEADER_BACK_BUTTON = "Header/Header/B_Back";
        public const string WINDOW_CONTENT = "WindowLayers/Content";
        public const string CONTENT_SCROLL_RECT = "WindowLayers/Content/ScrollRect";
        public const string CONTENT_ENTRIES = "WindowLayers/Content/ScrollRect/ContentEntries";
        public const string MENU_EXTRA_BUTTONS = "Menu/Content/Main/ExtraButtons";
        public const string TAB_BUTTONS = "TabButtons";
    }

    public static class MenuNames
    {
        public const string LOADOUT_PRESETS = "W_LoadoutPresets";
        public const string LOADOUT_CHARACTER_SELECTOR = "W_LoadoutCharacterSelector";
    }

    public static class UIText
    {
        public const string LOADOUT_MANAGER_TITLE = "Loadout Manager";
        public const string NEW_LOADOUT_PLACEHOLDER = "New loadout name...";
        public const string NAME_REQUIRED = "Name Required";
        public const string TEXT_PROTECTED_SUFFIX = "_Text_Protected";
    }
}