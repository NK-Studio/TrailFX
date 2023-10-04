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
        }
    }
}
#endif