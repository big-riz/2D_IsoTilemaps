using UnityEngine;

namespace Gentleland.Utils.IsometricDarkFantasy
{
    public class PackageSettings : ScriptableObject
    {
        public const string PackageSettingsPath = "Assets/GentlelandSettings/IsometricDarkFantasy.asset";
        public const string PackageSettingsFolder = "GentlelandSettings";
        public const string PackageSettingsFolderPath = "Assets/GentlelandSettings";
        public const string PackageDocumentationPath = "/Gentleland/IsometricDarkFantasy/Isometric Dark Fantasy Documentation.pdf";

        public bool isFirstTimeUsingTheAsset = true;
        public bool doNotCheckIsometricOrderingOnLoad=false;
    }
}
