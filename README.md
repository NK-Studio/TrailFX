# TrailFX
### UnityPackage -> [UnityPackage](https://github.com/NK-Studio/Unity-AnimationPreview-Patcher/releases/tag/1.0.0) 다운로드

[![미리보기](https://github.com/NK-Studio/TrailFX/blob/main/GitHub/Image.png)](https://youtu.be/lULnVezdp_Q?si=IERdIPvhoRB91H_1)  

# MoveToTrailUV와 무엇이 달라졌나요?
-   Editor를 그려내는 프로세스를 IMGUI -> UI Toolkit으로 변경
-	인스턴스 머티리얼로 변경하여 값을 변경할 때 씬에 있는 동일한 머티리얼이 같이 수정되는 이슈를 수정
-	컴포넌트내에서 Edit Mode와 Play모드로 분리하여 수정에 대한 프로세스와 재생에 대한 프로세스 분리
-	(핵심) 셰이더 그래프에서 _MoveToMaterialUV가 Exposed가 활성화 되어있을 때 버튼 원클릭으로 Exposed를 꺼주고 머티리얼에서 SavedProerty에 추가된 해당 프로퍼티를 제거함
-	셰이더 파일 UTF-8로 변경

# 호환 엔진 버전
2022.3 LTS  
2021.3 LTS

# 사용 방법
![컴포넌트](https://github.com/NK-Studio/TrailFX/blob/main/GitHub/Component.png)  
1.	Trail FX 스크립트를 오브젝트에 적용합니다.  
   * 스크립트를 적용하는 오브젝트는 Trail Renderer가 아니어도 됩니다.
2.	Move Object를 지정합니다.
   * Move Object의 해당 오브젝트가 World 이동 값이 UV로 전달됩니다.
3.	Material Data의 Size를 Trail Renderer 수만큼 설정한 뒤 이동 값이 전달될 Trail Renderer들을 등록합니다.
4.	트레일 FX를 에디터에서 동작하려면 Play를 눌러서 테스트합니다.
   * (주의) 실제 Runtime에서는 Play모드가 Off되어 있어야합니다.

# Trail Renderer 설정
![트레일 렌더러](https://github.com/NK-Studio/TrailFX/blob/main/GitHub/Tile.png)  
Trail Renderer의 Texture Mode는 Tile로 설정해야 합니다.

# ShaderGraph와 연동
![셰이더 그래프 연동](https://github.com/NK-Studio/TrailFX/blob/main/GitHub/ShaderGraphInfo.png)  
셰이더그래프 블랙보드의 레퍼런스 이름을 규칙에 맞게 지정해야 합니다.  
1.	메인 텍스처는 _MainTex로 합니다.
2.	타일링과 오프셋 조절용 Vector4는 _MainTex_ST로 합니다.
3.	Trail FX 스크립트에서 받아들일 이동 값은 _MoveToMaterialUV로 합니다.
4.	_MoveToMaterialUV의 Exposed는 반드시 Off로 꺼주세요.
* 이 프로퍼티가 켜진 채로 적용된 재질에는 Saved Property에 MoveToMaterialUV가 기록되고, 이후 블랙보드에서 Expose를 끄더라도 재질의 Saved Property에는 남아있게 되어서 지속적으로 Dirty 상태를 유발하는 원인이 됩니다.
* 다시 강조하지만 _MoveToMaterialUV 프로퍼티의 Exposed 옵션은 잠시라도 켜면 안됩니다.  
이 옵션이 켜진 채로 만들어진 재질은 지속적으로 버전 관리 이슈가 발생할 수 있습니다.
* 만약에 머티리얼에 저장된 _MoveToMaterialUV 프로퍼티를 지우려면 TileFX 컴포넌트에서 셰이더 그래프가 적용된 머티리얼을 적용된 트레일 렌더러를 연결하고 Fix 버튼을 클릭해주세요.
 
# Custom Shader와 연동
![커스텀 셰이더 연동](https://github.com/NK-Studio/TrailFX/blob/main/GitHub/hlsl.png)
HLSL 코드에서 _MoveToMaterialUV 변수를 선언하고 uv 연산 결과에 빼 주면 됩니다. (o.uv.x -= … 등으로 뺄셈인 점 주의)

![메인 텍스쳐](https://github.com/NK-Studio/TrailFX/blob/main/GitHub/mainTex.png)
Trail FX 스크립트의 이동 값은 메인 텍스처에 전달됩니다.  
유니티 셰이더에서 메인 텍스처를 구분하는 규칙은 프로퍼티 이름이 _MainTex 이거나 [MainTexture] 어트리뷰트를 사용해야합니다.  
이렇게 지정된 메인 텍스처 이름 + _ST 이름규칙으로 구성된 Tiling & Offset 변수 (half4 혹은 float4)가 선언되어야 합니다.  
참고 : 메인 텍스처 설명 문서 - https://docs.unity3d.com/ScriptReference/Material-mainTexture.html 

# _MoveToMaterialUV 프로퍼티의 Dirty 이슈
Dirty이슈는 Git 등의 버전관리 도구에서 재질의 _MoveToMaterialUV 값이 자꾸 변경되었다고 나오는 상황을 말합니다.  
이 상황은 셰이더그래프의 블랙보드에서 _MoveToMaterialUV 프로퍼티를 추가할 때 주로 발생합니다.  
커스텀 셰이더에서는 재질에 _MoveToMaterialUV 프로퍼티가 저장될 이유가 없지만 셰이더그래프의 블랙보드에서는 프로퍼티의 Expose 기본값이 켜져 있어서 재질에 _MoveToMaterialUV 프로퍼티가 저장될 수 있습니다.  
![커스텀 셰이더 연동](https://github.com/NK-Studio/TrailFX/blob/main/GitHub/ShaderGraph-Exposed.png)  
_MoveToMaterialUV 프로퍼티가 재질에 저장되면 Trail FX에서 다음과 같이 에러 메시지가 보여집니다.  
Fix 버튼을 누르면 자동으로 셰이더 그래프에서 _MoveToMaterialUV의 Exposed가 off되고, 머티리얼에 SavedProperties에서 _MoveToMaterialUV가 자동으로 제거됩니다. 
