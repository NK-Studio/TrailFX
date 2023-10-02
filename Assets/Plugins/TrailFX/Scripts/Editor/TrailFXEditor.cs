using System.IO;
using System.Linq;
using System.Text;
using FX.NKStudio;
using UnityEditor;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Serialization;
using UnityEditor.UIElements;
using UnityEngine;
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
                text = "Renderer의 Texture Mode가 Tile이여야 합니다."
            };
            helpBox.SetActive(false);

            var (fixHelpBox, fixButton) = FixHelpBox();
            fixHelpBox.SetActive(false);

            _root.Add(title);
            _root.Add(group);
            group.Add(_moveObjectField);
            group.Add(_materialDataField);
            group.Add(TrailFXEditorUtility.Space(3));
            group.Add(helpBox);
            group.Add(fixHelpBox);

            _root.schedule.Execute(() =>
            {
                helpBox.SetActive(false);
                fixHelpBox.SetActive(false);

                // 트레일 렌더러의 텍스쳐 모드가 Tile인지 체크
                int notTileIndex = CheckTrailRendererTile();
                if (notTileIndex != -1)
                {
                    string message = $"{notTileIndex}번 Renderer의 Texture Mode가 Tile이 아닙니다.";
                    helpBox.text = message;
                    helpBox.SetActive(true);

                    return;
                }

                // 트레일 렌더러에 머티리얼이 있는지 체크
                int notFindMaterialIndex = CheckTrailRendererShader();
                if (notFindMaterialIndex != -1)
                {
                    helpBox.text = $"{notFindMaterialIndex}번째 Renderer의 Material이 없습니다.";
                    helpBox.SetActive(true);
                }

                int notExposedParameter = CheckExposedParameter();
                if (notExposedParameter != -1)
                {
                    fixHelpBox.text =
                        $"{notExposedParameter}번째 Renderer의 Material에 _MoveToMaterialUV가 식별됩니다.";
                    fixHelpBox.SetActive(true);
                }
            }).Every(100);

            title.RegisterCallback<ClickEvent>(_ =>
            {
                var scriptAsset = MonoScript.FromMonoBehaviour(_trailFX);
                var path = AssetDatabase.GetAssetPath(scriptAsset);

                TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                AssetDatabase.OpenAsset(textAsset);
            });

            fixButton.RegisterCallback<ClickEvent>(_ =>
            {
                int targetIndex = -1;
                string targetName = "_MoveToMaterialUV";
                int count = _trailFX.MultipleMaterialData.Length;

                #region 머티리얼에 _MoveToMaterialUV가 있는지 체크

                // 트레일 렌더러에 있는 머터리얼에 각각 접근해서 _MoveToMaterialUV가 있는지 체크
                for (int i = 0; i < count; i++)
                {
                    SerializedObject so =
                        new(_trailFX.MultipleMaterialData[i].trailRenderer.sharedMaterial);
                    SerializedProperty property = so.FindProperty("m_SavedProperties.m_Floats");

                    // property에서 _MoveToMaterialUV을 가진 property를 찾는다.
                    for (int j = 0; j < property.arraySize; j++)
                    {
                        string propertyName = property.GetArrayElementAtIndex(j).FindPropertyRelative("first")
                            .stringValue;
                        if (propertyName == targetName)
                        {
                            targetIndex = i;
                            break;
                        }
                    }
                }

                #endregion

                // 셰이더 그래프에서 해당 프로퍼티의 Expose를 끈다.
                var material = _trailFX.MultipleMaterialData[targetIndex].trailRenderer.sharedMaterial;
                OffExposed(material, targetName);
                
                // 저장된 프로퍼티 제거
                RemoveSavedProperties(targetIndex, targetName);

                // 새로 고침
                AssetDatabase.Refresh();
            });

            return _root;
        }

        /// <summary>
        /// 해당 트레일 렌더러의 머티리얼에 있는 _MoveToMaterialUV를 제거합니다.
        /// </summary>
        /// <param name="trailRenderIndex"></param>
        /// <param name="parameterName"></param>
        private void RemoveSavedProperties(int trailRenderIndex, string parameterName)
        {
            SerializedObject so =
                new(_trailFX.MultipleMaterialData[trailRenderIndex].trailRenderer.sharedMaterial);
            SerializedProperty property = so.FindProperty("m_SavedProperties.m_Floats");

            // property에서 targetName을 가진 property를 찾아서 제거한다.
            int targetIndex = -1;
            for (int j = 0; j < property.arraySize; j++)
            {
                string propertyName = property.GetArrayElementAtIndex(j).FindPropertyRelative("first")
                    .stringValue;
                
                if (propertyName != parameterName) continue;
                targetIndex = j;
                break;
            }

            Debug.Log(targetIndex);
            if (targetIndex != -1) 
                property.DeleteArrayElementAtIndex(targetIndex);
            
            property.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 인자로 들어온 머티리얼을 통해 사용하고 있는 셰이더 그래프에 접근하여 해당 파라미터의 Exposed를 끕니다.
        /// </summary>
        /// <param name="material">타겟 머티리얼</param>
        /// <param name="parameterName">Exposed를 끌 파라미터 이름</param>
        private void OffExposed(Material material, string parameterName)
        {
            if (!material.IsShaderGraph())
                return;

            // 셰이더 그래프 가져오기
            string shaderPath = AssetDatabase.GetAssetPath(material.shader);

            // // 셰이더 그래프 가져오기
            var textGraph = File.ReadAllText(shaderPath, Encoding.UTF8);

            var graph = new GraphData
            {
                messageManager = new MessageManager(),
                assetGuid = AssetDatabase.AssetPathToGUID(shaderPath)
            };

            // 디코딩 처리
            MultiJson.Deserialize(graph, textGraph);

            foreach (AbstractShaderProperty property in graph.properties)
            {
                if (property.referenceName == parameterName)
                    property.generatePropertyBlock = false;
            }

            // 덮어씌우기

            var nextSerialize = MultiJson.Serialize(graph);
            File.WriteAllText(shaderPath, nextSerialize, Encoding.UTF8);
        }

        /// <summary>
        /// Fix Button이 있는 HelpBox를 반환합니다.
        /// </summary>
        /// <returns></returns>
        private (HelpBox helpBox, Button fixButton) FixHelpBox()
        {
            var helpBox = new HelpBox
            {
                messageType = HelpBoxMessageType.Warning,
                style =
                {
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

        private int CheckExposedParameter()
        {
            int count = _trailFX.MultipleMaterialData.Length;

            for (int i = 0; i < count; i++)
            {
                SerializedObject so = new(_trailFX.MultipleMaterialData[i].trailRenderer.sharedMaterial);
                SerializedProperty property = so.FindProperty("m_SavedProperties.m_Floats");

                string targetName = "_MoveToMaterialUV";

                // property에서 targetName을 가진 property를 찾아서 제거한다.
                for (int j = 0; j < property.arraySize; j++)
                {
                    string propertyName = property.GetArrayElementAtIndex(j).FindPropertyRelative("first").stringValue;
                    if (propertyName == targetName)
                        return i;
                }
            }

            return -1;
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