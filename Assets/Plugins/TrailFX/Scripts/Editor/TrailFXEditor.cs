using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        private HelpBox _infoBox;
        private Button _editorPlayButton;

        private StyleSheet _styleSheet;

        // Target
        private TrailFX _trailFX;

        [SerializeField] private VisualTreeAsset trailList;
        [SerializeField] private VisualTreeAsset trailTemplate;

        private const string TargetShaderParameterReference = "_MoveToMaterialUV";

        private void OnEnable()
        {
            _trailFX = target as TrailFX;
            string path = AssetDatabase.GUIDToAssetPath("b94f253bde590554885ec4369af52afd");
            _styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
        }
        
        public override VisualElement CreateInspectorGUI()
        {
            // Init
            InitIcon();
            FindProperties();

            // Root
            _root = new VisualElement();
            _root.styleSheets.Add(_styleSheet);

            // Element
            _moveObjectField = new PropertyField();
            _moveObjectField.BindProperty(_moveObjectProperty);

            _editorPlayButton = new Button();
            _editorPlayButton.style.marginTop = 5;
            _editorPlayButton.style.height = 32;
            const int radius = 12;
            _editorPlayButton.style.borderTopLeftRadius = radius;
            _editorPlayButton.style.borderTopRightRadius = radius;
            _editorPlayButton.style.borderBottomLeftRadius = radius;
            _editorPlayButton.style.borderBottomRightRadius = radius;
            _editorPlayButton.tooltip = "에디터에서 작업 및 테스트하려면 이 기능을 활성화 해주세요.\n단, 실제로 런타임 모드일 때는 이 기능을 해제 하십시오.";

            // MaterialData
            _materialDataField = trailList.CloneTree().Q<ListView>();
            _materialDataField.bindingPath = "MultipleMaterialData";
            _materialDataField.selectionType = SelectionType.Single;
            _materialDataField.reorderable = false;
            _materialDataField.reorderMode = ListViewReorderMode.Simple;
            _materialDataField.itemsSource = _trailFX.MultipleMaterialData;
            _materialDataField.showBoundCollectionSize = false;
            _materialDataField.makeItem = MakeItem;
            _materialDataField.bindItem = BindItem;
            _materialDataField.itemsAdded += AddItem;
            _materialDataField.itemsRemoved += RemoveItem;

            var title = new Label("Trail FX");
            title.AddToClassList("TitleStyle");

            var group = new GroupBox();
            group.AddToClassList("GroupBoxStyle");

            _infoBox = new HelpBox
            {
                messageType = HelpBoxMessageType.Warning,
                text = "Editor Play는 에디터에서 작업할 때만 활성화 해주세요.\n런타임 모드에서는 비활성화 해주세요.",
            };
            _infoBox.SetActive(false);

            var (fixHelpBox, fixButton) = TrailFXEditorUtility.FixHelpBox();
            fixHelpBox.SetActive(false);

            _root.Add(title);
            _root.Add(group);
            group.Add(_moveObjectField);
            group.Add(TrailFXEditorUtility.Space(0, 5));
            group.Add(_materialDataField);
            group.Add(TrailFXEditorUtility.Space(0, 3));
            group.Add(fixHelpBox);
            group.Add(_infoBox);
            _root.Add(_editorPlayButton);

            RefreshMoveObjectField();
            RefreshListViewStyle();
            RefreshButtonStyle(_editorPlayButton);
            RefreshActiveHelpBox();

            _root.schedule.Execute(() =>
            {
                fixHelpBox.SetActive(false);

                int notExposedParameter = CheckExposedParameter();
                if (notExposedParameter != -1)
                {
                    fixHelpBox.text =
                        $"{notExposedParameter}번째 Renderer의 Material에 _MoveToMaterialUV가 식별됩니다.";
                    fixHelpBox.SetActive(true);
                }
            }).Every(100);

            title.RegisterCallback<ClickEvent>(_ => TrailFXEditorUtility.OpenBehaviour(_trailFX));
            fixButton.RegisterCallback<ClickEvent>(OnFixShaderGraph);
            _editorPlayButton.clicked += () => { OnClickPlayModeButton(_editorPlayButton); };

            return _root;
        }

        /// <summary>
        /// MoveObject를 리프래쉬 합니다.
        /// </summary>
        private void RefreshMoveObjectField()
        {
            if (EditorApplication.isPlaying)
                _moveObjectField.SetEnabled(false);
            else
                _moveObjectField.SetEnabled(true);
        }
        
        /// <summary>
        /// 플레이 모드 버튼을 눌렀을 때 처리
        /// </summary>
        /// <param name="element"></param>
        private void OnClickPlayModeButton(VisualElement element)
        {
            // 값을 반전 합니다.
            _editorPlayProperty.boolValue = !_editorPlayProperty.boolValue;

            // 기타 처리
            EnsureSystemStability();
            RefreshButtonStyle(element);
            RefreshActiveHelpBox();
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 플레이 모드가 되면 경고  박스를 띄운다.
        /// </summary>
        private void RefreshActiveHelpBox()
        {
            _infoBox.SetActive(IsEditorPlayMode());
        }

        /// <summary>
        /// 버튼에 대한 스타일 처리
        /// </summary>
        /// <param name="element"></param>
        private void RefreshButtonStyle(VisualElement element)
        {
            // True라면,
            if (IsEditorPlayMode())
            {
                _editorPlayButton.text = "Exit";
                if (EditorGUIUtility.isProSkin)
                    element.AddToClassList("ButtonActive_dark");
                else
                    element.AddToClassList("ButtonActive_white");
            }
            else
            {
                _editorPlayButton.text = "Play";
                if (EditorGUIUtility.isProSkin)
                    element.RemoveFromClassList("ButtonActive_dark");
                else
                    element.RemoveFromClassList("ButtonActive_white");
            }

            // 유니티가 프레이 모드라면 버튼을 비활성화 시킴
            if (EditorApplication.isPlaying)
                element.SetEnabled(false);
            else
                element.SetEnabled(true);
        }

        /// <summary>
        /// 리스트 뷰를 새로고침 합니다.
        /// </summary>
        private void RefreshListViewStyle()
        {
            // 에디터 플레이 모드면 리스트 뷰는 비활성화 되어야 한다.
            bool active = !IsEditorPlayMode();
            _materialDataField.SetEnabled(active);
            
            // 유니티가 프레이 모드라면 버튼을 비활성화 시킴
            if (EditorApplication.isPlaying)
                _materialDataField.SetEnabled(false);
            else
                _materialDataField.SetEnabled(true);
        }

        /// <summary>
        /// 셰이더 그래프에서 _MoveToMaterialUV가 Exposed되어 있으면 그것에 대한 처리를 합니다. 
        /// </summary>
        /// <param name="evt"></param>
        private void OnFixShaderGraph(ClickEvent evt)
        {
            if (IsEditorPlayMode())
            {
                Debug.LogError("Play Mode에서는 동작하지 않습니다.");
                return;
            }

            int targetIndex = -1;
            int count = _materialDataProperty.arraySize;
            TrailRenderer targetRenderer = null;

            #region 머티리얼에 _MoveToMaterialUV가 있는지 체크

            // 트레일 렌더러에 있는 머터리얼에 각각 접근해서 _MoveToMaterialUV가 있는지 체크
            for (int i = 0; i < count; i++)
            {
                targetRenderer = (TrailRenderer)_materialDataProperty.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("trailRender").objectReferenceValue;

                if (!targetRenderer)
                    continue;

                // 해당 머티리얼에 접근합니다.
                SerializedObject materialObject = new(targetRenderer.sharedMaterial);
                SerializedProperty savedProperties = materialObject.FindProperty("m_SavedProperties.m_Floats");

                // property에서 _MoveToMaterialUV을 가진 property를 찾는다.
                for (int j = 0; j < savedProperties.arraySize; j++)
                {
                    string propertyName = savedProperties.GetArrayElementAtIndex(j).FindPropertyRelative("first")
                        .stringValue;

                    if (propertyName == TargetShaderParameterReference)
                    {
                        targetIndex = i;
                        break;
                    }
                }
            }

            #endregion

            #region 해당 셰이더 그래프에서 프로퍼티를 Exposed를 off 처리

            if (targetRenderer)
                OffExposed(targetRenderer.sharedMaterial);

            #endregion

            // 저장된 프로퍼티 제거
            RemoveSavedProperties(targetIndex);

            // 새로 고침
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 플레이 모드가 되면 인스턴스를 생성하고, 플레이모드를 종료하면 인스턴스를 생성한 것을 삭제한다.
        /// </summary>
        private void EnsureSystemStability()
        {
            // true라면 에디터에서 작업을 시작합니다.
            bool playMode = _editorPlayProperty.boolValue;

            // 플레이 모드라면 
            if (playMode)
            {
                // 여기에서는 인스턴스를 만들어야 한다.
                _materialDataField.SetEnabled(false);

                for (int i = 0; i < _materialDataProperty.arraySize; i++)
                {
                    // 인스턴스 머티리얼로 변경합니다.
                    ChangeInstanceMaterial(i);
                }
            }
            // 플레이 모드가 아니라면,
            else
            {
                // 여기서는 인스턴스를 삭제하고 원래의 머티리얼이 동작되도록 해야한다.
                _materialDataField.SetEnabled(true);
                for (int i = 0; i < _materialDataProperty.arraySize; i++)
                {
                    // 트레일 렌더러에 있는 머티리얼을 원래대로 되돌린다.
                    // 만약 리스트 공간은 있는데, 트레일이 연결이 안되어 있을 수 도 있다.
                    TrailRenderer targetRender = (TrailRenderer)_materialDataProperty.GetArrayElementAtIndex(i)
                        .FindPropertyRelative("trailRender").objectReferenceValue;

                    if (targetRender)
                    {
                        // 머티리얼을 리셋
                        RestMaterial(targetRender, i);
                    }
                }
            }
        }

        /// <summary>
        /// 머티리얼 데이터 리스트에 Index로 이동하여 해당 트레일 렌더러에 접근하고,
        /// m_Materials에 접근해서 0번째 머티리얼을 인스턴스 머티리얼로 변경한다.
        /// </summary>
        /// <param name="index">인스턴스로 만들 머티리얼의 리스트 번호</param>
        private void ChangeInstanceMaterial(int index)
        {
            // 타겟 렌더러
            TrailRenderer targetRenderer = (TrailRenderer)_materialDataProperty
                .GetArrayElementAtIndex(index)
                .FindPropertyRelative("trailRender").objectReferenceValue;

            // 없으면 리턴
            if (targetRenderer == null)
                return;

            // 머티리얼 프로퍼티 접근
            _materialDataProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Origin")
                .objectReferenceValue = targetRenderer.sharedMaterial;

            // 0번째 머리티얼을 인스턴스 머티리얼로 변경한다.
            targetRenderer.sharedMaterial = Instantiate(targetRenderer.sharedMaterial);

            // 캐쉬 머티리얼을 만들어서 저장한다.
            _materialDataProperty.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 트레일 렌더러에 있는 머티리얼을 Origin으로 다시 되돌립니다.
        /// </summary>
        /// <param name="targetRenderer"></param>
        /// <param name="index"></param>
        private void ResetMaterialToOriginInTrailRenderer(TrailRenderer targetRenderer, int index)
        {
            // 머티리얼을 리셋
            RestMaterial(targetRenderer, index);

            // 값 초기화
            _materialDataProperty.GetArrayElementAtIndex(index).FindPropertyRelative("trailRender")
                .objectReferenceValue = null;
            _materialDataProperty.GetArrayElementAtIndex(index).FindPropertyRelative("CachedMaterial")
                .objectReferenceValue = null;
            _materialDataProperty.GetArrayElementAtIndex(index).FindPropertyRelative("Origin")
                .objectReferenceValue = null;
        }

        /// <summary>
        /// 머티리얼을 원래 머티리얼로 되돌립니다.
        /// 인스턴스 머티리얼을 제거하고, 프리팹 오브젝트라면 원래대로 되돌리고, 일반 오브젝트라면 원래 머티리얼로 되돌립니다.
        /// </summary>
        /// <param name="targetRenderer">원래대로 되돌릴 트레일 렌더러</param>
        /// <param name="index">되돌릴 머티리얼의 타겟에 대한 리스트 번호</param>
        private void RestMaterial(TrailRenderer targetRenderer, int index)
        {
            // 인스턴스 머티리얼을 사용하고 있다면 제거한다.
            var isInstanceMaterial = AssetDatabase.GetAssetPath(targetRenderer.sharedMaterial);
            if (string.IsNullOrWhiteSpace(isInstanceMaterial))
            {
                DestroyImmediate(targetRenderer.sharedMaterial);
                targetRenderer.sharedMaterial = null;
            }

            // 프리팹 오브젝트라면 원래대로 되돌리고, 일반 오브젝트라면 원래 머티리얼로 되돌린다.
            if (PrefabUtility.GetPrefabInstanceStatus(targetRenderer) != PrefabInstanceStatus.NotAPrefab)
                PrefabUtility.RevertObjectOverride(targetRenderer, InteractionMode.AutomatedAction);
            else
            {
                // Origin 시리얼라이즈 프로퍼티 
                SerializedProperty originProperty = _materialDataProperty.GetArrayElementAtIndex(index)
                    .FindPropertyRelative("Origin");

                // 원래 머티리얼로 되돌리기
                targetRenderer.sharedMaterial = (Material)originProperty.objectReferenceValue;
                originProperty.objectReferenceValue = null;
            }
        }

        /// <summary>
        /// 아이템 요소를 만들때
        /// </summary>
        /// <returns></returns>
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

                    // 데이터가 바인딩 되었다면,
                    bool isBindData = evt.newValue != null;
                    if (isBindData)
                    {
                        // 트레일 렌더러 
                        TrailRenderer trailRenderer = evt.newValue as TrailRenderer;

                        if (!trailRenderer)
                            return;

                        // 만약 타일이 아니라면,
                        if (trailRenderer.textureMode != LineTextureMode.Tile)
                        {
                            Debug.LogError(evt.newValue.name + "의 Texture Mode가 Tile이 아닙니다.");
                            _materialDataField.Rebuild();
                            return;
                        }

                        if (trailRenderer.sharedMaterial != null)
                        {
                            // 트레일 렌더러 변수에 트레일 렌더러 참조
                            _materialDataProperty.GetArrayElementAtIndex(index).FindPropertyRelative("trailRender")
                                .objectReferenceValue = trailRenderer;

                            // uvTiling을 기록한다.
                            _materialDataProperty.GetArrayElementAtIndex(index).FindPropertyRelative("uvTiling")
                                    .vector2Value =
                                trailRenderer.sharedMaterial.mainTextureScale;
                        }
                        else
                        {
                            Debug.LogError(evt.newValue.name + "에 머티리얼이 없습니다.");
                            _materialDataField.Rebuild();
                        }
                    }
                    else
                    {
                        TrailRenderer previousTrailRender = (TrailRenderer)evt.previousValue;

                        // 트레일 렌더러에 있는 머티리얼을 원래대로 되돌린다.
                        ResetMaterialToOriginInTrailRenderer(previousTrailRender, index);
                    }
                }

                _materialDataProperty.serializedObject.ApplyModifiedProperties();
            });

            return item;
        }

        /// <summary>
        ///  아이템이 바인딩 될 때
        /// </summary>
        /// <param name="element"></param>
        /// <param name="index"></param>
        private void BindItem(VisualElement element, int index)
        {
            var trailRenderField = element.Q<ObjectField>();
            trailRenderField.userData = index;

            if (_materialDataProperty.arraySize == 0)
                return;

            SerializedProperty materialData = _materialDataProperty.GetArrayElementAtIndex(index);
            SerializedProperty trailRenderer = materialData.FindPropertyRelative("trailRender");

            // RegisterCall을 발생시키지 않고 동작시킴
            trailRenderField.SetValueWithoutNotify(trailRenderer.objectReferenceValue);

            _materialDataProperty.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 아이템을 추가했을 때
        /// </summary>
        /// <param name="obj"></param>
        private void AddItem(IEnumerable<int> obj)
        {
            // 마지막 요소에 선택
            _materialDataField.selectedIndex = _materialDataProperty.arraySize - 1;

            // 마지막 요소 접근 및 값 수정
            SerializedProperty element = _materialDataProperty.GetArrayElementAtIndex(_materialDataField.selectedIndex);
            element.FindPropertyRelative("trailRender").objectReferenceValue = null;
            element.FindPropertyRelative("Origin").objectReferenceValue = null;

            // 변경 된 값 적용
            _materialDataProperty.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 아이템을 삭제했을 때
        /// </summary>
        /// <param name="obj"></param>
        private void RemoveItem(IEnumerable<int> obj)
        {
            if (_materialDataField.selectedIndex == -1)
                return;

            // 마지막 요소 접근 및 값 수정
            SerializedProperty element = _materialDataProperty.GetArrayElementAtIndex(_materialDataField.selectedIndex);
            SerializedProperty trailRenderProperty = element.FindPropertyRelative("trailRender");

            TrailRenderer trailRenderer = (TrailRenderer)trailRenderProperty.objectReferenceValue;

            // 인스턴스 머티리얼을 사용하고 있다면 제거한다.
            var isInstanceMaterial = AssetDatabase.GetAssetPath(trailRenderer.sharedMaterial);
            if (string.IsNullOrWhiteSpace(isInstanceMaterial))
            {
                DestroyImmediate(trailRenderer);
                trailRenderer.sharedMaterial = null;
            }

            // 트레일 렌더러의 머티리얼을 원래대로 되돌리기
            if (trailRenderer)
            {
                SerializedProperty originProperty = element.FindPropertyRelative("Origin");
                if (originProperty.objectReferenceValue)
                    ((TrailRenderer)trailRenderProperty.objectReferenceValue).sharedMaterial =
                        (Material)originProperty.objectReferenceValue;
            }

            // 변경 된 값 적용
            _materialDataProperty.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 프로퍼티를 찾습니다.
        /// </summary>
        private void FindProperties()
        {
            _materialDataProperty = serializedObject.FindProperty("MultipleMaterialData");
            _moveObjectProperty = serializedObject.FindProperty("MoveObject");
            _editorPlayProperty = serializedObject.FindProperty("editorPlayMode");
        }

        /// <summary>
        /// 해당 트레일 렌더러의 머티리얼에 있는 _MoveToMaterialUV를 제거합니다.
        /// </summary>
        /// <param name="trailRenderIndex"></param>
        private void RemoveSavedProperties(int trailRenderIndex)
        {
            var active = !IsEditorPlayMode();

            if (!active)
            {
                Debug.LogWarning("Play Mode에서는 동작하지 않습니다.");
                return;
            }

            SerializedObject so =
                new(_trailFX.MultipleMaterialData[trailRenderIndex].trailRender.sharedMaterial);
            SerializedProperty property = so.FindProperty("m_SavedProperties.m_Floats");

            // property에서 targetName을 가진 property를 찾아서 제거한다.
            int targetIndex = -1;
            for (int j = 0; j < property.arraySize; j++)
            {
                string propertyName = property.GetArrayElementAtIndex(j).FindPropertyRelative("first")
                    .stringValue;

                if (propertyName != TargetShaderParameterReference) continue;
                targetIndex = j;
                break;
            }

            if (targetIndex != -1)
                property.DeleteArrayElementAtIndex(targetIndex);

            property.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 인자로 들어온 머티리얼을 통해 사용하고 있는 셰이더 그래프에 접근하여 해당 파라미터의 Exposed를 끕니다.
        /// </summary>
        /// <param name="material">타겟 머티리얼</param>
        private void OffExposed(Material material)
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
                if (property.referenceName == TargetShaderParameterReference)
                    property.generatePropertyBlock = false;
            }

            // 덮어씌우기

            var nextSerialize = MultiJson.Serialize(graph);
            File.WriteAllText(shaderPath, nextSerialize, Encoding.UTF8);
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
        /// 해당 머티리얼에 _MoveToMaterialUV가 있는지 체크합니다.
        /// </summary>
        /// <returns>있으면 해당 리스트 번호를 반환하고, 없으면 -1을 반환합니다.</returns>
        private int CheckExposedParameter()
        {
            if (_trailFX.MultipleMaterialData == null)
                return -1;

            int count = _materialDataProperty.arraySize;

            for (int i = 0; i < count; i++)
            {
                // 머티리얼이 없으면 Continue 합니다.
                TrailRenderer targetMaterial = (TrailRenderer)_materialDataProperty.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("trailRender").objectReferenceValue;

                if (!targetMaterial)
                    continue;

                // 해당 머티리얼에 접근합니다.
                SerializedObject materialObject = new(targetMaterial.sharedMaterial);
                SerializedProperty savedProperties = materialObject.FindProperty("m_SavedProperties.m_Floats");

                // property에서 targetName을 가진 property를 찾아서 제거한다.
                for (int j = 0; j < savedProperties.arraySize; j++)
                {
                    string propertyName = savedProperties.GetArrayElementAtIndex(j).FindPropertyRelative("first")
                        .stringValue;
                    if (propertyName == TargetShaderParameterReference)
                        return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 에디터 플레이 모드가 실행되어 있는지 체크
        /// </summary>
        /// <returns>켜져있으면 true를 반환, 아닐시 false를 반환</returns>
        private bool IsEditorPlayMode() => _editorPlayProperty.boolValue;
    }
}