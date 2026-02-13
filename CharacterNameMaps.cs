using System;

namespace LoadoutPresets
{
    internal static class CharacterIcon
    {
        public static string GetIconNameForCharacter(ECharacter? character) =>
            // Map character names to icon names
            character switch
            {
                ECharacter.SirOofie => "Knight",
                ECharacter.Cl4nk => "RobotCowboy",
                ECharacter.TonyMcZoom => "Hoverguy",
                ECharacter.Spaceman => "Astronat",
                ECharacter.Vlad => "BloodMage",
                ECharacter.SirChadwell => "CorruptedKnight",
                null => "",
                _ => character.ToString()
            };
    }

    internal static class CharacterName
    {
        public static string GetDisplayNameForCharacter(ECharacter? character) =>
            // Map character names to display names
            character switch
            {
                ECharacter.SirOofie => "Sir Oofie",
                ECharacter.TonyMcZoom => "Tony McZoom",
                ECharacter.SirChadwell => "Sir Chadwell",
                null => "None",
                _ => character.ToString()
            };

        public static ECharacter? GetEnumFromDisplayName(string displayName) =>
            // Map display names back to character enums
            displayName switch
            {
                "Sir Oofie" => ECharacter.SirOofie,
                "Tony McZoom" => ECharacter.TonyMcZoom,
                "Sir Chadwell" => ECharacter.SirChadwell,
                "None" => null,
                _ when Enum.TryParse(displayName, out ECharacter result) => result,
                _ => null
            };
    }
}