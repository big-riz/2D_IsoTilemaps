using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Compilation;

namespace Gentleland.Utils.IsometricDarkFantasy
{
    [InitializeOnLoad]
    public static class OpenWindowsOnLoad
    {
        static OpenWindowsOnLoad()
        {

            PackageSettings settings = AssetDatabase.LoadAssetAtPath<PackageSettings>(PackageSettings.PackageSettingsPath);
            if (settings == null)
            {
                if (!AssetDatabase.IsValidFolder(PackageSettings.PackageSettingsFolderPath))
                {
                    AssetDatabase.CreateFolder("Assets",PackageSettings.PackageSettingsFolder);
                }
                settings = ScriptableObject.CreateInstance<PackageSettings>();
                AssetDatabase.CreateAsset(settings, PackageSettings.PackageSettingsPath);
            }
            if (settings.isFirstTimeUsingTheAsset)
            {
                EditorApplication.delayCall += WelcomeWindow.OpenWindow;
            }
            else
            {
                EditorApplication.delayCall += CheckIsometricOrderingWindow.OpenWindow;
            }
        }
    }
}
