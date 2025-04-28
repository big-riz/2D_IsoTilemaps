using UnityEditor;
using UnityEngine;

namespace Gentleland.Utils.IsometricDarkFantasy
{
    public class WelcomeWindow : EditorWindow
    {
        const string imagePath = "Assets/Gentleland/IsometricDarkFantasy/Previews/cover_3_420_280.png";
        GUIStyle textStyle;
        GUIStyle linkStyle;
        Texture image;
        bool initialized = false;


        public static void OpenWindow()
        {
            PackageSettings settings = AssetDatabase.LoadAssetAtPath<PackageSettings>(PackageSettings.PackageSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PackageSettings>();
                AssetDatabase.CreateAsset(settings, PackageSettings.PackageSettingsPath);
            }
            if (!settings.isFirstTimeUsingTheAsset)
            {
                return;
            }
            WelcomeWindow wnd = GetWindow<WelcomeWindow>(true);
            wnd.titleContent = new GUIContent("Gentleland : Isometric Dark Fantasy");
            wnd.minSize = new Vector2(600, 650);
            wnd.maxSize = wnd.minSize;
            settings.isFirstTimeUsingTheAsset = false;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        public void OnGUI()
        {
            if (!initialized)
            {
                image = AssetDatabase.LoadAssetAtPath<Texture>(imagePath);
                textStyle = new GUIStyle(EditorStyles.label);
                textStyle.wordWrap = true;
                textStyle.margin = new RectOffset(20, 20, 20, 20);
                textStyle.alignment = TextAnchor.UpperLeft;
                linkStyle = new GUIStyle(textStyle);
                linkStyle.hover.textColor = linkStyle.normal.textColor * 0.5f;
                initialized = true;
            }
            float imageSize = 300;
            float margin = 20;
            if (image != null)
            {
                GUI.DrawTexture(new Rect(position.width / 2 - imageSize / 2, margin, imageSize, imageSize), image, ScaleMode.ScaleToFit);
            }
            GUILayout.BeginArea(new Rect(20, imageSize + 2 * margin, position.width - margin * 2, position.height - imageSize + 2 * margin));
            GUILayout.Label(
@"Hello dear developer! 

Thank you for acquiring this asset pack from the Unity Asset Store!

We are a growing art outsourcing agency and are very thankful for your support!
If you like this asset pack, please consider leaving a review on the Unity Asset Store or recommend us to your friends! This would help us greatly!


If you encounter problems of any kind, checkout the documentation and feel free to reach out!
We will help you further.

Jacky Martin
CEO - Gentleland"
            , textStyle);
            if (GUILayout.Button("jacky@gentleland.net", linkStyle))
            {
                Application.OpenURL("mailto:jacky@gentleland.net");
            }
            if (GUILayout.Button("Documentation.pdf"))
            {
                Application.OpenURL(Application.dataPath + PackageSettings.PackageDocumentationPath);
            }
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            EditorApplication.delayCall += CheckIsometricOrderingWindow.OpenWindow;
        }

    }
}
