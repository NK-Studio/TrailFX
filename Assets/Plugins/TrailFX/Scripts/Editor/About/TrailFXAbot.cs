using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NKStudio
{
    public class TrailFXAbot : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset TrailFXAboutUXML;
        
        [SerializeField]
        private TextAsset packageJson;

        [MenuItem("Window/TrailFX/About", false)]
        public static void Init()
        {
            TrailFXAbot wnd = GetWindow<TrailFXAbot>();
            wnd.titleContent = new GUIContent("About");
            wnd.minSize = new Vector2(350, 120);
            wnd.maxSize = new Vector2(350, 120);
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Import UXML
            VisualTreeAsset visualTree = TrailFXAboutUXML;
            VisualElement container = visualTree.Instantiate();

            Label versionLabel = container.Q<Label>("version-label");
            PackageInfo info = JsonUtility.FromJson<PackageInfo>(packageJson.text);
            
            root.Add(container);
            
            if (info == null)
                return;
            
            versionLabel.text = $"Version : {info.version}";
            
            // 아이콘 폴더 경로
            string iconDirectory = AssetDatabase.GUIDToAssetPath("d6991e3c2438ec8489a3e8f30251b21d");
            string iconPath = $"{iconDirectory}/{(EditorGUIUtility.isProSkin ? "d_" : "")}TrailFXIcon.png";
            
            Texture2D iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            VisualElement icon = container.Q<VisualElement>("TrailFX-icon");
            icon.style.backgroundImage = iconTexture;
        }
    }

    [System.Serializable]
    public class PackageInfo
    {
        public string name;
        public string displayName;
        public string version;
        public string unity;
        public string description;
        public List<string> keywords;
        public string type;
    }
}