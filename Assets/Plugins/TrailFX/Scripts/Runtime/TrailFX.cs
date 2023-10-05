using System;

#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
#endif
using UnityEngine;

namespace NKStudio
{
    // Trail Renderer의 Head가 움직일 때 이동한 거리를 재질의 스크롤 UV로 전달하는 스크립트
    // Trail Renderer의 Texture Mode가 Tile이여야함
    // 재질에 전달되는 값은 0~1 사이의 값이다.
    //[ExecuteAlways]
    public class TrailFX : MonoBehaviour
    {
        public Transform MoveObject;

        //public int ShaderPropertyID = Shader.PropertyToID("_MoveToMaterialUV");

        public MaterialData[] MultipleMaterialData;
        private Vector3 _beforePosition = Vector3.zero;

        // 에디터에서 플레이 시킬 때 사용
        [SerializeField]
        private bool editorPlay;

        private void Start()
        {
#if UNITY_EDITOR
            StartCoroutine(EnsureSystemStability());
#endif

            if (MultipleMaterialData == null || MultipleMaterialData.Length == 0)
                return;

            foreach (MaterialData materialData in MultipleMaterialData)
            {
                // Move 값 초기화
                materialData.Move = 0f;

                if (materialData.Origin)
                {
                    // 초기 uvTiling 값을 초기화합니다.
                    materialData.uvTiling = materialData.Origin.mainTextureScale;
                    
                    // 머티리얼을 인스턴싱을 합니다.
                    materialData.CashMaterial = Instantiate(materialData.Origin);
                    materialData.trailRender.sharedMaterial = materialData.CashMaterial;
                }
            }
        }

         private void LateUpdate()
         {
             if (!MoveObject)
             {
                 Debug.LogWarning("MoveObject가 연결되어있지 않습니다.");
                 return;
             }

             if (MultipleMaterialData == null || MultipleMaterialData.Length == 0)
                 return;

             Vector3 currentPosition = MoveObject.transform.position;

             // 위치 변화가 없으면 아무 작업도 안함
             if (currentPosition == _beforePosition)
                 return;

             float distance = Vector3.Distance(currentPosition, _beforePosition);
             _beforePosition = currentPosition;

             foreach (MaterialData materialData in MultipleMaterialData)
             {
                 if (!materialData.trailRender)
                     continue;

                 materialData.Move += distance * materialData.uvTiling.x;

                 // m_move 값이 지나치게 커지지 않도록 하기 위해 1 이상은 나머지 값만 전달. (이미 m_uvTiling.x 가 곱해진 값이어야함)
                 if (materialData.Move > 1f)
                     materialData.Move %= 1f;

                 // 프로퍼티 존재 체크 없이 기록. 프로퍼티가 존재하면 재질 버전이 계속 변경된 것으로 처리되는 문제가 있음.
                 if (materialData.CashMaterial)
                 {
                     materialData.CashMaterial.SetFloat("_MoveToMaterialUV", materialData.Move);
                 }
             }
         }

        private void OnDisable()
        {
            // 모든 재질의 _MoveToMaterialUV 값을 0으로 리셋.
            // 이렇게 하는 이유? 셰이더 프로퍼티에 존재하지 않더라도 경우에 따라 재질의 Saved Property에 존재할 수 있어서 자꾸 Dirty 상태가 됨. 셰이더나 셰이더그래프에서 한 번이라도 인스펙터에 Expose 되면 재질에는 프로퍼티가 저장됨.
            // 이렇게 해도 Saved Property가 존재하면 사용자 조작에 따라서 Dirty 되는 경우를 피할 수 없음. 유니티 API에서 재질의 Saved Property에 접근하는 방법을 아직 알아내지 못함.
            foreach (MaterialData materialData in MultipleMaterialData)
                if (materialData.CashMaterial)
                    materialData.CashMaterial.SetFloat("_MoveToMaterialUV", 0f);
        }

        private void OnDestroy()
        {
            int count = MultipleMaterialData.Length;
            for (int i = 0; i < count; i++)
                Destroy(MultipleMaterialData[i].CashMaterial);
        }
        
#if UNITY_EDITOR
        private IEnumerator EnsureSystemStability()
        {
            yield return new WaitForSeconds(0.1f);

            if (editorPlay)
            {
                EditorApplication.isPlaying = false;
                Debug.LogError($"{gameObject.name}에 Editor Play 모드가 켜져있습니다.");
            }
        }
#endif
    }
}
