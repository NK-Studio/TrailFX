using System.Reflection;
using FX.NKStudio;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace NKStudio
{
    // 초기에 상황에 맞게 똑똑한 Sync 방향을 할 수 있도록 했었으나
    // 재질의 타일링 값과 Material Data의 타일링 값을 바꿔도 잘 작동하며, Revert라던가, 렌더러 변경 등 다양한 예외상황을 모두 코딩으로 하려나 너무 지저분해져서
    // 작업자가 수동으로 Sync 방향을 정하도록 함.
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TrailFX))]
    public class TrailFXEditor : Editor
    {
        // SerializedProperty
        private SerializedProperty _moveObjectProperty;
        private SerializedProperty _materialDataProperty;

        // VisualElement
        private VisualElement _root;
        private PropertyField _moveObjectField;
        private PropertyField _materialDataField;

        private StyleSheet _styleSheet;

        // Target
        private TrailFX _trailFX;

        private void OnEnable()
        {
            _trailFX = target as TrailFX;
            string path = AssetDatabase.GUIDToAssetPath("b94f253bde590554885ec4369af52afd");
            _styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            FindProperty();
        }
        
        public override VisualElement CreateInspectorGUI()
        {
            InitIcon();

            _root = new VisualElement();
            _root.styleSheets.Add(_styleSheet);

            _moveObjectField = new PropertyField(_moveObjectProperty);
            _materialDataField = new PropertyField(_materialDataProperty);
            _materialDataField.style.paddingLeft = 16;

            var title = new Label("Trail FX");
            title.AddToClassList("TitleStyle");

            var group = new GroupBox();
            group.AddToClassList("GroupBoxStyle");

            var helpBox = new HelpBox
            {
                messageType = HelpBoxMessageType.Warning,
                text = "Trail Renderer의 Texture Mode가 Tile이여야 합니다."
            };
            helpBox.SetActive(false);

            _root.Add(title);

            _root.Add(group);
            group.Add(_moveObjectField);
            group.Add(_materialDataField);
            group.Add(TrailFXEditorUtility.Space(3));
            group.Add(helpBox);

            _root.schedule.Execute(() =>
            {
                helpBox.SetActive(false);
                int notTileIndex = CheckTrailRendererTile();
                if (notTileIndex != -1)
                {
                    string message = $"Element {notTileIndex}번 Trail Renderer의 Texture Mode가 Tile이 아닙니다.";
                    helpBox.text = message;
                    helpBox.SetActive(true);

                    return;
                }

                int notFindMaterialIndex = CheckTrailRendererShader();
                if (notFindMaterialIndex != -1)
                {
                    helpBox.text = $"Element {notFindMaterialIndex}번째 Trail Renderer의 Material이 없습니다.";
                    helpBox.SetActive(true);
                }
            }).Every(100);

            title.RegisterCallback<ClickEvent>(_ =>
            {
                var scriptAsset = MonoScript.FromMonoBehaviour(_trailFX);
                var path = AssetDatabase.GetAssetPath(scriptAsset);

                TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                AssetDatabase.OpenAsset(textAsset);
            });

            return _root;
        }

        /// <summary>
        /// 아이콘을 설정합니다.
        /// </summary>
        private void InitIcon()
        {
            // 아이콘 폴더 경로
            string iconDirectory = AssetDatabase.GUIDToAssetPath("d6991e3c2438ec8489a3e8f30251b21d");
            string iconPath = $"{iconDirectory}/{(EditorGUIUtility.isProSkin ? "d_" : "")}TrailFXIcon.png";
            TrailFXEditorUtility.ApplyIcon(iconPath, _trailFX);
        }
        
        /// <summary>
        /// 프로퍼티를 찾습니다.
        /// </summary>
        private void FindProperty()
        {
            _moveObjectProperty = serializedObject.FindProperty("MoveObject");
            _materialDataProperty = serializedObject.FindProperty("MultipleMaterialData");
        }

        /// <summary>
        /// MultipleMaterialData에서 Trail Renderer의 Texture Mode가 Tile로 안되어있는 녀석의 인덱스를 반환합니다.
        /// </summary>
        /// <returns>Tile으로 안되있는 인덱스 번호</returns>
        private int CheckTrailRendererTile()
        {
            if (_trailFX.MultipleMaterialData.Length == 0)
                return -1;

            int count = _trailFX.MultipleMaterialData.Length;

            for (int i = 0; i < count; i++)
            {
                if (!_trailFX.MultipleMaterialData[i].trailRenderer)
                    continue;

                if (_trailFX.MultipleMaterialData[i].trailRenderer.textureMode != LineTextureMode.Tile)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// 연결된 트레인 렌더러에 머티리얼이 비어있는지 체크
        /// </summary>
        /// <returns>비어있는 인덱스 번호를 반환</returns>
        private int CheckTrailRendererShader()
        {
            if (_trailFX.MultipleMaterialData.Length == 0)
                return -1;

            int count = _trailFX.MultipleMaterialData.Length;
            for (int i = 0; i < count; i++)
            {
                TrailRenderer trailRenderer = _trailFX.MultipleMaterialData[i].trailRenderer;

                if (!trailRenderer)
                    continue;

                // 재질이 비었는지 검사
                if (trailRenderer.sharedMaterial == null)
                    return i;

                // 재질이 셰이더를 가지고 있는지 검사
            }

            return -1;
        }
    }
}