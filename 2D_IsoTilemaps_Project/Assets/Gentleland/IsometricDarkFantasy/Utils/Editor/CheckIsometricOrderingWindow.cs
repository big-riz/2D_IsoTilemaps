using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gentleland.Utils.IsometricDarkFantasy
{
    public class CheckIsometricOrderingWindow : EditorWindow
    {

        GUIStyle textStyle;
        bool initialized = false;

        public static void OpenWindow()
        {
            PackageSettings settings = AssetDatabase.LoadAssetAtPath<PackageSettings>(PackageSettings.PackageSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PackageSettings>();
                settings.isFirstTimeUsingTheAsset = false;
                AssetDatabase.CreateAsset(settings, PackageSettings.PackageSettingsPath);
            }
            if (settings.doNotCheckIsometricOrderingOnLoad)
            {
                return;
            }
            if (GraphicsSettings.transparencySortMode != UnityEngine.TransparencySortMode.CustomAxis || GraphicsSettings.transparencySortAxis != new UnityEngine.Vector3(0, 1, 0))
            {
                CheckIsometricOrderingWindow wnd = GetWindow<CheckIsometricOrderingWindow>(true);
                wnd.titleContent = new GUIContent("Gentleland : Isometric Ordering");
                wnd.minSize = new Vector2(350, 200);
                wnd.maxSize = wnd.minSize;
            }
        }

        public void OnGUI()
        {
            if (!initialized)
            {
                textStyle = new GUIStyle(EditorStyles.label);
                textStyle.wordWrap = true;
                textStyle.margin = new RectOffset(20, 20, 20, 20);
                initialized = true;
            }
            EditorGUILayout.LabelField(
@"We have detect that Isometric Ordering is not setup in the way our samples intend it.
Sprite Ordering may not work as intended.
To see how to setup Isometric Ordering look in the documentation."
            , textStyle);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Documentation.pdf"))
            {
                Application.OpenURL(Application.dataPath+ PackageSettings.PackageDocumentationPath);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            PackageSettings settings = AssetDatabase.LoadAssetAtPath<PackageSettings>(PackageSettings.PackageSettingsPath);
            bool doNotCheckIsometricOrderingOnLoad = GUILayout.Toggle(settings.doNotCheckIsometricOrderingOnLoad, "do not show again");
            if (settings.doNotCheckIsometricOrderingOnLoad != doNotCheckIsometricOrderingOnLoad)
            {
                settings.doNotCheckIsometricOrderingOnLoad = doNotCheckIsometricOrderingOnLoad;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
            EditorGUILayout.EndHorizontal();
        }

    }
}
