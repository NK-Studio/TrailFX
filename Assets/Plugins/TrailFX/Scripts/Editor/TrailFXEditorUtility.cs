using System.Linq;
#if UNITY_EDITOR
namespace FX
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    namespace NKStudio
    {
        public static class TrailFXEditorUtility
        {
            private static VisualElement _cachedContextWidthElement;
            private static VisualElement _cachedInspectorElement;
            
            public static VisualElement Space(float width, float height)
            {
                VisualElement space = new();
                space.style.width = width;
                space.style.height = height;
                return space;
            }

            public static void SetActive(this VisualElement field, bool active)
            {
                field.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
                field.style.visibility = active ? Visibility.Visible : Visibility.Hidden;
            }

            public static void ApplyIcon(string iconPath, Object targetObject)
            {
                var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);

                if (!icon)
                {
                    EditorGUIUtility.SetIconForObject(targetObject, null);
                    return;
                }

                EditorGUIUtility.SetIconForObject(targetObject, icon);
            }
            
            /// <summary>
            /// Fix Button이 있는 HelpBox를 반환합니다.
            /// </summary>
            /// <returns></returns>
            public static (HelpBox helpBox, Button fixButton) FixHelpBox()
            {
                var helpBox = new HelpBox {
                    messageType = HelpBoxMessageType.Warning,
                    style = {
                        alignItems = Align.FlexStart,
                        paddingLeft = 12,
                        height = 70
                    }
                };

                var helpLogo = helpBox.Children().ElementAt(0);
                helpLogo.style.marginTop = 5;

                var helpText = helpBox.Children().ElementAt(1);
                helpText.style.marginTop = 12;

                Button fixButton = new Button();
                fixButton.text = "Fix";

                // position을 Absolute로 처리
                fixButton.style.position = Position.Absolute;
                fixButton.style.bottom = 5;
                fixButton.style.right = 5;
                fixButton.style.paddingLeft = 12;
                fixButton.style.paddingRight = 12;
                fixButton.style.paddingTop = 3;
                fixButton.style.paddingBottom = 3;

                helpBox.contentContainer.Add(fixButton);

                return (helpBox, fixButton);
            }

            public static void OpenBehaviour(MonoBehaviour targetBehaviour)
            {
                var scriptAsset = MonoScript.FromMonoBehaviour(targetBehaviour);
                var path = AssetDatabase.GetAssetPath(scriptAsset);

                TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                AssetDatabase.OpenAsset(textAsset);
            }
        }
    }
}
#endif