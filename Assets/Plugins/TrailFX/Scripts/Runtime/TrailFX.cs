using System;

#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

namespace NKStudio
{
    // Trail Renderer의 Head가 움직일 때 이동한 거리를 재질의 스크롤 UV로 전달하는 스크립트
    // Trail Renderer의 Texture Mode가 Tile이여야함
    // 재질에 전달되는 값은 0~1 사이의 값이다.
    [ExecuteAlways]
    public class TrailFX : MonoBehaviour
    {
        public Transform MoveObject;

        private static readonly int MoveToMaterialUV = Shader.PropertyToID("_MoveToMaterialUV");

        public MaterialData[] MultipleMaterialData;
        [SerializeField]
        private Vector3 beforePosition = Vector3.zero;

        // 에디터에서 플레이 시킬 때 사용
        [SerializeField]
        private bool editorPlayMode;
        
        private void Start()
        {
#if UNITY_EDITOR
            // 유니티의 플레이 모드 상태에서 TrailFX의 플레이 모드가 켜져있으면 안된다.
            if (TrailFXUtility.IsPlayMode)
                StartCoroutine(EnsureSystemStability());

            if (editorPlayMode)
                return;
#endif
            if (MultipleMaterialData == null || MultipleMaterialData.Length == 0)
                return;

            if (TrailFXUtility.IsPlayMode)
            {
                for (int index = 0; index < MultipleMaterialData.Length; index++)
                {
                    // Move 값 초기화
                    MultipleMaterialData[index].Move = 0f;

                    // 트레일 렌더러가 연결되어 있다면,
                    if (MultipleMaterialData[index].trailRender)
                    {
                        // 인스턴싱 머티리얼을 트레일 렌더러에 적용
                        MultipleMaterialData[index].trailRender.material = MultipleMaterialData[index].trailRender.material;
                        
                        // 머티리얼 데이터.uvTiling에 해당 머티리얼의 텍스쳐 스케일링 값을 적용하여 초기 값을 설정함.
                        MultipleMaterialData[index].uvTiling = MultipleMaterialData[index].trailRender.material.mainTextureScale;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (TrailFXUtility.IsEditorMode)
            {
                if (!editorPlayMode)
                    return;
            }
        
            if (!MoveObject)
            {
                Debug.LogError("MoveObject가 연결되어있지 않습니다.");
                return;
            }
        
            if (MultipleMaterialData == null || MultipleMaterialData.Length == 0)
            {
                Debug.LogWarning("MultipleMaterialData에 아무것도 없습니다.");
                return;
            }

            Vector3 currentPosition = MoveObject.position;
        
            // 위치 변화가 없으면 아무 작업도 안함
            if (currentPosition == beforePosition)
                return;

            float distance = Vector3.Distance(currentPosition, beforePosition);
            beforePosition = currentPosition;

            for (int index = 0; index < MultipleMaterialData.Length; index++)
            {
                if (!MultipleMaterialData[index].trailRender)
                    continue;
                
                MultipleMaterialData[index].Move += distance*MultipleMaterialData[index].uvTiling.x;

                // _move 값이 지나치게 커지지 않도록 하기 위해 1 이상은 나머지 값만 전달. (이미 _uvTiling.x 가 곱해진 값이어야함)
                if (MultipleMaterialData[index].Move > 1f)
                    MultipleMaterialData[index].Move %= 1f;

                if (TrailFXUtility.IsPlayMode)
                {
                    if (MultipleMaterialData[index].trailRender.material)
                    {
                        MultipleMaterialData[index].trailRender.material.SetFloat(MoveToMaterialUV, MultipleMaterialData[index].Move);
                    }
                }
                else
                {
                    if (MultipleMaterialData[index].trailRender.sharedMaterial)
                        MultipleMaterialData[index].trailRender.sharedMaterial.SetFloat(MoveToMaterialUV, MultipleMaterialData[index].Move);
                }
            }
        }

        private void OnDisable()
        {
            if (MultipleMaterialData == null || MultipleMaterialData.Length == 0)
                return;

            if (TrailFXUtility.IsPlayMode)
            {
                for (int index = 0; index < MultipleMaterialData.Length; index++)
                {
                    if (MultipleMaterialData[index].trailRender.material)
                    {
                        MultipleMaterialData[index].trailRender.material.SetFloat(MoveToMaterialUV, 0f);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (TrailFXUtility.IsPlayMode)
            {
                int count = MultipleMaterialData.Length;
                
                for (int index = 0; index < count; index++)
                    CoreUtils.Destroy(MultipleMaterialData[index].trailRender.material);
                
                MultipleMaterialData = null;
            }
        }
        
        
#if UNITY_EDITOR
        private IEnumerator EnsureSystemStability()
        {
            yield return new WaitForSeconds(0.1f);

            if (editorPlayMode)
            {
                EditorApplication.isPlaying = false;
                Debug.LogError($"{gameObject.name}에 Editor Play 모드가 켜져있습니다.");
            }
        }
#endif
    }
}
