using System;
using System.Collections.Generic;
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
        private SerializedProperty _editorPlayProperty;

        // VisualElement
        private VisualElement _root;
        private PropertyField _moveObjectField;
        private ListView _materialDataField;

        private StyleSheet _styleSheet;

        // Target
        private TrailFX _trailFX;

        [SerializeField] private VisualTreeAsset trailList;
        [SerializeField] private VisualTreeAsset trailTemplate;

        private void OnEnable()
        {
            _trailFX = target as TrailFX;
            string path = AssetDatabase.GUIDToAssetPath("b94f253bde590554885ec4369af52afd");
            _styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
        }
        
        public override VisualElement CreateInspectorGUI()
        {
            InitIcon();
            FindProperties();

            _root = new VisualElement();
            _root.styleSheets.Add(_styleSheet);

            // MoveObject
            _moveObjectField = new PropertyField();
            _moveObjectField.BindProperty(_moveObjectProperty);

            Button editorPlayButton = new Button();
            editorPlayButton.text = "Editor Play";
            editorPlayButton.style.marginTop = 5;
            editorPlayButton.style.height = 32;
            const int radius = 12;
            editorPlayButton.style.borderTopLeftRadius = radius;
            editorPlayButton.style.borderTopRightRadius = radius;
            editorPlayButton.style.borderBottomLeftRadius = radius;
            editorPlayButton.style.borderBottomRightRadius = radius;
            editorPlayButton.tooltip = "에디터에서 작업 및 테스트하려면 이 기능을 활성화 해주세요.\n단 실제로 런타임 모드일 때는 이 기능을 해제 하십시오.";
            ButtonStyle();
           
            // MaterialData
            _materialDataField = trailList.CloneTree().Q<ListView>();
            _materialDataField.bindingPath = "MultipleMaterialData";
            _materialDataField.selectionType = SelectionType.Single;
            _materialDataField.itemsSource = _trailFX.MultipleMaterialData;
            _materialDataField.showBoundCollectionSize = false;
            _materialDataField.makeItem = MakeItem;
            _materialDataField.bindItem = BindItem;
            _materialDataField.itemsAdded += AddItem;
            EnsureSystemStability();
            
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
            group.Add(TrailFXEditorUtility.Space(0, 5));
            group.Add(_materialDataField);
            group.Add(TrailFXEditorUtility.Space(0, 3));
            group.Add(helpBox);
            group.Add(fixHelpBox);
            _root.Add(editorPlayButton);

            // _root.schedule.Execute(() =>
            // {
            //     helpBox.SetActive(false);
            //     fixHelpBox.SetActive(false);
            //
            //     // 트레일 렌더러의 텍스쳐 모드가 Tile인지 체크
            //     int notTileIndex = CheckTrailRendererTile();
            //     if (notTileIndex != -1)
            //     {
            //         string message = $"{notTileIndex}번 Renderer의 Texture Mode가 Tile이 아닙니다.";
            //         helpBox.text = message;
            //         helpBox.SetActive(true);
            //
            //         return;
            //     }
            //
            //     // 트레일 렌더러에 머티리얼이 있는지 체크
            //     int notFindMaterialIndex = CheckTrailRendererShader();
            //     if (notFindMaterialIndex != -1)
            //     {
            //         helpBox.text = $"{notFindMaterialIndex}번째 Renderer의 Material이 없습니다.";
            //         helpBox.SetActive(true);
            //     }
            //
            //     int notExposedParameter = CheckExposedParameter();
            //     if (notExposedParameter != -1)
            //     {
            //         fixHelpBox.text =
            //             $"{notExposedParameter}번째 Renderer의 Material에 _MoveToMaterialUV가 식별됩니다.";
            //         fixHelpBox.SetActive(true);
            //     }
            // }).Every(100);

            title.RegisterCallback<ClickEvent>(_ =>
            {
                var scriptAsset = MonoScript.FromMonoBehaviour(_trailFX);
                var path = AssetDatabase.GetAssetPath(scriptAsset);

                TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                AssetDatabase.OpenAsset(textAsset);
            });

            // fixButton.RegisterCallback<ClickEvent>(_ =>
            // {
            //     int targetIndex = -1;
            //     string targetName = "_MoveToMaterialUV";
            //     int count = _trailFX.MultipleMaterialData.Length;
            //
            //     #region 머티리얼에 _MoveToMaterialUV가 있는지 체크
            //
            //     // 트레일 렌더러에 있는 머터리얼에 각각 접근해서 _MoveToMaterialUV가 있는지 체크
            //     for (int i = 0; i < count; i++)
            //     {
            //         SerializedObject so =
            //             new(_trailFX.MultipleMaterialData[i].trailRender.sharedMaterial);
            //         SerializedProperty property = so.FindProperty("m_SavedProperties.m_Floats");
            //
            //         // property에서 _MoveToMaterialUV을 가진 property를 찾는다.
            //         for (int j = 0; j < property.arraySize; j++)
            //         {
            //             string propertyName = property.GetArrayElementAtIndex(j).FindPropertyRelative("first")
            //                 .stringValue;
            //             if (propertyName == targetName)
            //             {
            //                 targetIndex = i;
            //                 break;
            //             }
            //         }
            //     }
            //
            //     #endregion
            //
            //     // 셰이더 그래프에서 해당 프로퍼티의 Expose를 끈다.
            //     var material = _trailFX.MultipleMaterialData[targetIndex].trailRender.sharedMaterial;
            //     OffExposed(material, targetName);
            //
            //     // 저장된 프로퍼티 제거
            //     RemoveSavedProperties(targetIndex, targetName);
            //
            //     // 새로 고침
            //     AssetDatabase.Refresh();
            // });

            editorPlayButton.clicked += () =>
            {
                _editorPlayProperty.boolValue = !_editorPlayProperty.boolValue;
                ButtonStyle();
                EnsureSystemStability();
                serializedObject.ApplyModifiedProperties();
            };

            void ButtonStyle()
            {
                // True라면,
                bool activeButton = _editorPlayProperty.boolValue;
                if (activeButton)
                {
                    if (EditorGUIUtility.isProSkin)
                        editorPlayButton.AddToClassList("ButtonActive_dark");
                    else
                        editorPlayButton.AddToClassList("ButtonActive_white");
                }
                else
                {
                    if (EditorGUIUtility.isProSkin)
                        editorPlayButton.RemoveFromClassList("ButtonActive_dark");
                    else
                        editorPlayButton.RemoveFromClassList("ButtonActive_white");
                }
            }

            return _root;
        }

        private void EnsureSystemStability()
        {
            bool activeButton = _editorPlayProperty.boolValue;
            if (activeButton)
            {
                _materialDataField.SetEnabled(true);
            }else
                _materialDataField.SetEnabled(false);
        }

        private VisualElement MakeItem()
        {
            TemplateContainer item = trailTemplate.Instantiate();
            var trailRendererField = item.Q<ObjectField>();
            trailRendererField.bindingPath = "trailRender";
            trailRendererField.objectType = typeof(TrailRenderer);

            trailRendererField.RegisterValueChangedCallback(evt =>
            {
                if (trailRendererField.userData != null)
                {
                    // 수정된 인덱스 번호를 가져온다.
                    int index = int.Parse(trailRendererField.userData.ToString());

                    if (evt.newValue == null)
                    {
                        TrailRenderer previousTrailRender = ((TrailRenderer)evt.previousValue);

                        // 인스턴스 머티리얼을 사용하고 있다면 제거한다.
                        var isInstanceMaterial = AssetDatabase.GetAssetPath(previousTrailRender.sharedMaterial);
                        if (string.IsNullOrWhiteSpace(isInstanceMaterial))
                            DestroyImmediate(previousTrailRender.sharedMaterial);

                        // 프리팹 오브젝트라면 원래대로 되돌리고, 일반 오브젝트라면 원래 머티리얼로 되돌린다.
                        if (PrefabUtility.GetPrefabInstanceStatus(evt.previousValue) != PrefabInstanceStatus.NotAPrefab)
                            PrefabUtility.RevertObjectOverride(evt.previousValue, InteractionMode.AutomatedAction);
                        else
                        {
                            // 원래 머티리얼로 되돌리기
                            Material originMaterial = (Material)_materialDataProperty.GetArrayElementAtIndex(index)
                                .FindPropertyRelative("Origin").objectReferenceValue;
                            ((TrailRenderer)evt.previousValue).sharedMaterial = originMaterial;
                        }

                        // 값 초기화
                        _materialDataProperty.GetArrayElementAtIndex(index).FindPropertyRelative("trailRender")
                            .objectReferenceValue = null;
                        _materialDataProperty.GetArrayElementAtIndex(index).FindPropertyRelative("CashMaterial")
                            .objectReferenceValue = null;
                        _materialDataProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Origin")
                            .objectReferenceValue = null;
                    }
                    else
                    {
                        // 트레일 렌더러 
                        TrailRenderer trailRenderer = evt.newValue as TrailRenderer;

                        if (!trailRenderer)
                            return;

                        // 트레일 렌더러 변수에 트레일 렌더러 참조
                        _materialDataProperty.GetArrayElementAtIndex(index).FindPropertyRelative("trailRender")
                            .objectReferenceValue = trailRenderer;

                        if (trailRenderer.sharedMaterial != null)
                        {
                            // 오리진에 기록을 해놓는다.
                            _materialDataProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Origin")
                                .objectReferenceValue = trailRenderer.sharedMaterial;

                            // 머티리얼 공간 접근

                            SerializedObject trailRenderMaterials = new SerializedObject(_materialDataProperty
                                .GetArrayElementAtIndex(index)
                                .FindPropertyRelative("trailRender").objectReferenceValue);

                            var materialsProperty = trailRenderMaterials.FindProperty("m_Materials");
                            
                            // 머티리얼 공간이 없으면 Return
                            if (materialsProperty.arraySize == 0)
                                return;
                            
                            // 0번째 머리티얼을 인스턴스 머티리얼로 변경한다.
                            materialsProperty.GetArrayElementAtIndex(0).objectReferenceValue = Instantiate(
                                _materialDataProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Origin")
                                    .objectReferenceValue);
                            materialsProperty.serializedObject.ApplyModifiedProperties();
                        }
                    }
                }

                _materialDataProperty.serializedObject.ApplyModifiedProperties();
            });

            return item;
        }

        private void AddItem(IEnumerable<int> obj)
        {
            // 마지막 요소에 선택
            _materialDataField.selectedIndex = _materialDataProperty.arraySize - 1;

            // 마지막 요소 접근 및 값 수정
            SerializedProperty element = _materialDataProperty.GetArrayElementAtIndex(_materialDataField.selectedIndex);
            element.FindPropertyRelative("trailRender").objectReferenceValue = null;
            element.FindPropertyRelative("CashMaterial").objectReferenceValue = null;

            // 변경 된 값 적용
            _materialDataProperty.serializedObject.ApplyModifiedProperties();
        }

        // private void RemoveItem(IEnumerable<int> obj)
        // {
        //     if (_materialDataProperty.arraySize > 0)
        //     {
        //         int index = _materialDataField.selectedIndex;
        //         _materialDataProperty.DeleteArrayElementAtIndex(index);
        //         _materialDataField.RemoveFromSelection(index);
        //         _materialDataProperty.serializedObject.ApplyModifiedProperties();
        //     }
        //     
        //     _materialDataField.Rebuild();
        // }

        private void BindItem(VisualElement element, int index)
        {
            var trailRenderField = element.Q<ObjectField>();
            trailRenderField.userData = index;

            SerializedProperty materialData = _materialDataProperty.GetArrayElementAtIndex(index);
            SerializedProperty trailRenderer = materialData.FindPropertyRelative("trailRender");

            // RegisterCall을 발생시키지 않고 동작시킴
            trailRenderField.SetValueWithoutNotify(trailRenderer.objectReferenceValue);

            _materialDataProperty.serializedObject.ApplyModifiedProperties();
        }


        private void FindProperties()
        {
            _materialDataProperty = serializedObject.FindProperty("MultipleMaterialData");
            _moveObjectProperty = serializedObject.FindProperty("MoveObject");
            _editorPlayProperty = serializedObject.FindProperty("_editorPlay");
        }


        /// <summary>
        /// 해당 트레일 렌더러의 머티리얼에 있는 _MoveToMaterialUV를 제거합니다.
        /// </summary>
        /// <param name="trailRenderIndex"></param>
        /// <param name="parameterName"></param>
        private void RemoveSavedProperties(int trailRenderIndex, string parameterName)
        {
            SerializedObject so =
                new(_trailFX.MultipleMaterialData[trailRenderIndex].trailRender.sharedMaterial);
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

        private int CheckExposedParameter()
        {
            int count = _trailFX.MultipleMaterialData.Length;

            for (int i = 0; i < count; i++)
            {
                if (!_trailFX.MultipleMaterialData[i].trailRender)
                    continue;

                if (!_trailFX.MultipleMaterialData[i].trailRender.sharedMaterial)
                    continue;

                SerializedObject so = new(_trailFX.MultipleMaterialData[i].trailRender.sharedMaterial);
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
                if (!_trailFX.MultipleMaterialData[i].trailRender)
                    continue;

                if (_trailFX.MultipleMaterialData[i].trailRender.textureMode != LineTextureMode.Tile)
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
                TrailRenderer trailRenderer = _trailFX.MultipleMaterialData[i].trailRender;

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