# 데모 이후 UI·채굴 전환 작업 계획

### 2026-09-11 — 3A 보완: 텍스트 머터리얼 연출 책임 분리

- 수정 파일: `Assets/Scripts/TutorialGuides/TutorialGuideTextUI.cs`, `Assets/Scripts/TutorialGuides/TutorialGuideTextMaterialAnimation.cs`, `Assets/Prefabs/UI/TutorialGuides/TutorialGuideText.prefab`, `Docs/Plans/2026-09-10-demo-revision-plan.md`.
- 수정: TMP 공유 머터리얼의 런타임 복제·Outline/Underlay flash 값·원복·폐기와 fade Tween 생성을 `TutorialGuideTextMaterialAnimation`으로 분리했다. `TutorialGuideTextUI`는 새 컴포넌트의 시작·fade Tween·해제만 호출하며, 텍스트 바인딩과 위치·scale·alpha 연출만 보유한다.
- 수정: 프리팹 루트에 새 컴포넌트를 추가하고, 양 컴포넌트가 같은 `TextMeshProUGUI`를 참조하도록 Unity Editor PrefabContents API로 직렬화했다. 새 컴포넌트의 static readonly Shader property ID는 프로젝트 요청에 맞춰 `_camelCase` 이름을 사용한다.
- 검증: Unity 6000.5.1f1 Editor 재컴파일 성공, Editor ready/비컴파일 상태와 콘솔 오류 0개를 확인했다. Editor 직렬화 검사로 `TutorialGuideTextUI._materialAnimation`과 `TutorialGuideTextMaterialAnimation._text`가 모두 프리팹 루트의 유효한 컴포넌트를 참조함을 확인했다. `git diff --check`는 Unity가 새 컴포넌트에 직렬화한 빈 `m_Name: ` 한 줄의 trailing whitespace 때문에 실패하며, 기능과 무관한 프리팹 YAML 정규화는 하지 않았다.
- 미검증: 실제 PlayMode에서 발광 연출의 시각 결과와 안내 생성·취소·파괴 시 머터리얼 복원은 수동 확인이 필요하다.
- 다음 단계 인계: flash 수치 조정은 `TutorialGuideTextMaterialAnimation`만 수정한다. `TutorialGuideTextUI`의 중앙→목록 배치 흐름 또는 3B의 안내 정의·조건에는 변경을 가하지 않는다.

### 2026-09-11 — 3A 보완: 중앙 표시의 TMP 발광 전환

- 수정 파일: `Assets/Scripts/TutorialGuides/TutorialGuideTextUI.cs`, `Docs/Plans/2026-09-10-demo-revision-plan.md`.
- 수정: 중앙에서 새 안내가 표시될 때만 `TextMeshProUGUI.fontSharedMaterial`을 원본 Guide Material의 런타임 복제본으로 교체한다. 복제본의 Outline·Underlay를 밝은 흰색의 넓고 부드러운 값으로 즉시 올리고, 중앙→목록 이동 동안 PrimeTween으로 기존 머터리얼 값까지 보간한다. 이동 종료·연출 취소·오브젝트 파괴 때는 복제본을 폐기하고 원래 공유 머터리얼을 다시 할당한다.
- 결정: uGUI TMP는 CanvasRenderer 경로라 MaterialPropertyBlock을 안정적으로 적용할 수 없다. 따라서 공유 `M_PFStardust-ExtraBold_Outline_Underlay.mat`은 수정하지 않고, 항목별·연출 기간 한정 Material 인스턴스를 소유·파기하는 방식으로 분리했다.
- 검증: Unity 6000.5.1f1 Editor 재컴파일 성공, Editor ready/비컴파일 상태, 콘솔 오류 0개, `git diff --check` 성공을 확인했다. 현재 Guide Material이 Outline·Underlay 프로퍼티를 제공하는 TMP SDF-Mobile 계열임을 확인했고, 스크립트는 해당 프로퍼티가 모두 없으면 발광 인스턴스 생성을 건너뛴다.
- 미검증: 실제 PlayMode에서 이벤트 해금 시 첫 `0.18s`의 흰색 발광과 뒤이은 `0.55s` 감쇠, 빠른 씬 전환/안내 완료 중 인스턴스 복원, 배경별 가독성 및 수치 조정은 수동 확인이 필요하다.
- 다음 단계 인계: 발광 강도는 `TutorialGuideTextUI`의 Presentation Flash 직렬화 필드에서 조정한다. 3B의 안내 정의·조건 변경이나 공유 폰트 머터리얼 수정으로 범위를 넓히지 않는다.

작성: 2026-09-10. Unity 6000.5.1f1. 소스와 직렬화된 프리팹을 검토한 계획이며, Editor 실행·화면 검증·PlayMode 테스트는 하지 않았다. 이 문서 외 구현 변경은 하지 않았다.

## 1. 이번 작업 범위

- 기본 안내: 중앙 팝업 → 우측 상단 정착, 아이콘/입력 키 표시, 안내 단계 세분화.
- 배터리: 하단 중앙 배치를 우선안으로 적용하고 다른 HUD와 충돌 확인.
- 업그레이드: Esc/X 동일 닫기 동작과 안내 표시.
- 잔탄: 플라즈마 에너지 바, 네트건 남은 그물 개수 아이콘.
- 부유물 기반 자원 공급을 파괴 가능한 고정 타일맵 기반 채굴로 교체.

지역별 중력, 영구 파괴·자원 재성장, 생물 발견 조건, 펫·시설·원소 강화, 합성, 인벤토리 통합은 후속 범위다. 네트건 탄창 축소도 이번 HUD 구현과 분리해 수치 결정 후 적용한다. 레퍼런스 게임의 세부 동작은 이번 조사에서 별도로 검증하지 않았다.

## 2. 확인한 기존 구조와 영향

| 영역 | 현재 구현 | 변경 시 핵심 |
|---|---|---|
| 안내 | `Assets/Scripts/TutorialGuides/`의 System/Definition/ItemUI. 진행 이벤트에 따라 목록 생성, 완료 애니메이션 존재 | 정의는 현재 문자열 중심. 등장 큐와 중앙 연출은 새로 필요 |
| 안내 배치 | `Assets/Resources/Prefabs/DontDestroyOnLoad/TutorialGuideSystem.prefab`의 GuideList는 우측 상단 | 지속 객체이므로 씬 전환·로드 중 연출 취소 처리 필요 |
| 안내 진행 | `Assets/Scripts/Core/SaveSystem/GameProgressEventIds.cs`, `Assets/Data/Definitions/TutorialGuides/` | 기존 이벤트 숫자 유지. 새 이벤트와 실제 플레이 발생 지점 함께 추가 |
| 세션 HUD | `Assets/Prefabs/UI/HUD/SessionHUD.prefab` | 배터리 상단 중앙, 잔탄 우측 하단. 프리팹 인스턴스 override도 확인 |
| 잔탄 | `Assets/Scripts/UI/Session/SessionEquipmentAmmoHUD.cs` | 양쪽 모두 current/total 문자열. 무기의 AmmoChanged 및 선택 이벤트는 재사용 가능 |
| 닫기 | `Assets/Scripts/InteractableObjects/UpgradeInteractor.cs`, `Assets/Scripts/UI/UIInputHandler.cs` | Cancel 이벤트가 업그레이드/보고서 닫기를 담당. Pause도 같은 입력에 관여 |
| 타일맵 | `Assets/Scripts/Stage/Map/StageMap.cs` | Platform/DecorationBack/DecorationFront 각각 Logic/Visual 쌍 |
| 타일 물리 | `Assets/Scripts/Editor/StageMap/StageMapSetupUtility.cs` | Platform Logic에 TilemapCollider2D+CompositeCollider2D, Visual에는 물리 없음 |
| 타일 종류 | `StageTileSet.cs`, `StageMapEditorWindow.cs`, `StageMapDefaultTiles.cs` | Logic은 공통 타일, 외형은 Visual AutoTile. 현재 논리 타일만으로 광종 구분 불가 |
| 플라즈마 | `Assets/Scripts/Equipments/PlasmaGun/PlasmaGunController.cs` | CircleCast 후 Transform 목록으로 피해·연쇄·레이저·파티클 처리 |
| 피해 | `Assets/Scripts/DamagableObjects/IDamagable.cs` | AttackData는 damage/source만 보유. 셀과 충돌 위치 정보 없음 |
| 부유물 | `FloatageController.cs` | HP, FragmentParticleManager 드롭, 개체 제거 통지 담당 |
| 스폰 | `Assets/Scripts/Stage/StagePopulationManager.cs`, `StageDefinition.cs` | 생물/부유물 초기 생성 및 리스폰. 생물 유지하면서 부유물 경로 제거 필요 |
| 업그레이드 | `Assets/Scripts/Upgrades/Effects/FloatageUpgrades/`, `FloatageDropMultiplierRuntimeData.cs` | 부유물 정의별 보너스를 채굴 정의에 이전해야 구매 효과 유지 |
| 저장 | `Assets/Scripts/Core/SaveSystem/GameData/GameSaveData.cs` | schemaVersion 12, 업그레이드 ID/레벨·진행 이벤트 저장. 셀 파괴 저장은 현재 없음 |

기존 작업 트리에 PFStardust TMP 폰트 에셋 3개 수정이 있다. 이후 작업자는 이를 자기 변경으로 되돌리거나 섞지 않는다.

## 3. 구현에 사용할 잠정 정책

아래는 확정 기획이 아니라 첫 구현을 작게 만드는 제안이다. 다른 정책이 확정되면 해당 단계 전에 문서를 고친다.

1. **재입장 시 원본 맵 복원**, 같은 세션에서는 파괴 유지. 영구 파괴 저장은 만들지 않는다.
2. **Platform 중 채굴 정의가 지정된 셀만 파괴 가능**. 장식 및 외곽 보호벽은 유지한다.
3. 광종/HP/기본 드롭은 별도 채굴 정의가 소유하고, 외형 변경과 자원 종류는 분리한다.
4. 현재 플라즈마의 탄약 소비·차지·피해량·연쇄 업그레이드는 유지한다. 타일 연쇄의 기본안은 범위 안 노출된 서로 다른 셀을 선택하며 중복 타격·보호벽 관통을 막는 것이다. 깊은 벽 내부까지 연쇄 채굴할지 여부는 이 단계에서 확정한다.
5. X 동등화는 우선 업그레이드 화면과 그 안의 보고서 닫기 범위로 해석한다. 전역 Pause/모든 UI까지 확장하려면 별도 결정이 필요하다.
6. 기존 자원/업그레이드 식별자는 가능한 유지한다. 구매 내역 복원이 필요한 자산 매핑만 수행하고, 저장 파일 초기화는 하지 않는다.

## 4. 권장 작업 단위

한 번에 한 단계를 구현하고 완료 기준을 확인한다. 특히 4~7단계는 한 채팅에 합치지 않는다.

### 1단계 — 업그레이드 닫기 입력 (작음)

대상: `Assets/InputActions.inputactions`, UIInputHandler, UpgradeInteractor, 업그레이드 화면을 소유하는 Hub/프리팹.

- 실제 Player/UI Cancel 바인딩을 확인하고 Esc/X가 동일 닫기 경로로 들어오게 한다.
- UI Cancel에 단순히 X를 추가하면 다른 화면에도 적용될 수 있으므로 적용 범위를 점검한다.
- 화면에 `Esc / X 닫기`를 표시한다. 보고서가 위에 있으면 기존 우선순위대로 최상위 화면만 닫는다.
- 완료: Esc/X 각각 닫기, 재열기, 누른 채 유지, 보고서 표시 중 닫기 확인. 닫힌 프레임에 Pause가 열리거나 플레이 공격이 발생하지 않아야 한다.

### 2단계 — 배터리 위치와 잔탄 표시 (작음~중간)

대상: SessionHUD.prefab, SessionEquipmentAmmoHUD.cs, BatteryBarUI.cs.

- 배터리를 하단 중앙 anchor로 이동하고 잔탄·인벤토리·세션 종료 창과 겹침 확인.
- 플라즈마 슬롯은 RemainingAmmo / TotalAmmo 비율을 바에 표시. 차지 게이지와 구별되는 색/형태 사용.
- 네트건 슬롯은 남은 탄약만큼 그물 아이콘 표시. 최대 탄창 업그레이드에서도 넘치지 않도록 줄바꿈/간격 설계. 매 프레임 오브젝트 재생성 금지.
- 무기 해금 전 숨김, 현재 선택 강조, 초기 바인딩, 새 세션 탄약 복원은 유지한다.
- 완료: 0/1/최대 탄약, 최대값 0, 발사 직후, 무기 교체, 해금 전후, 새 세션 확인. 16:9와 좁은 화면비에서 배치 확인.

### 3단계 — 안내 표현과 진행 세분화 (중간, 필요하면 3A/3B로 분할)

대상: TutorialGuideSystem/Definition/ItemUI, 안내 프리팹·정의, GameProgressEventIds와 이벤트 발행 코드.

- 3A: Definition에 선택적 아이콘/키 표시 데이터를 추가하고 ItemUI가 렌더링하도록 한다. 실제 바인딩과 안내가 다르지 않도록 연결 기준을 정한다.
- 중앙 연출용 root를 목록 root와 분리한다. 팝 → 짧은 유지 → 우측 상단 이동 → 목록 편입 순서로 표시한다. 기존 LayoutGroup이 이동 연출을 덮어쓰지 않도록 한다.
- 동시 활성화 안내는 하나씩 재생. 도중 완료, 씬 전환, 비활성화, 재로드 시 큐와 애니메이션 안전 종료. 재정렬만으로 등장 연출을 반복하지 않는다.
- 3B: 이동 / 조준·플라즈마 사용 / 첫 채굴 / 자원 회수 / 배터리·복귀 / 네트건 선택·포획 등을 초안으로 정의한다. 실제 키와 해금 순서를 확인해 문구 및 완료 이벤트를 연결한다.
- 새 저장 이벤트의 기존 enum 값 변경 금지. 과거 세이브에서 완료된 안내가 다시 대량 표시되지 않도록 조건을 검토한다.
- 완료: 새 게임 첫 안내부터 순서 진행, 연출 중 완료, 여러 안내 활성화, Hub↔Stage 전환, 재로드 확인. MemoryOnly 세션은 기존에 안내를 끄므로 테스트는 정상 저장 세션으로 수행한다.
- 첫 채굴 이벤트는 5단계 채굴 성공 지점에 연결하여 최종 검증한다.

### 4단계 — 채굴 데이터와 셀 파괴 기능 (중간)

대상: StageMap/StageTileSet 및 StageMap 편집 도구. 새 타입 이름은 구현 시 확정한다.

- 채굴 정의: 최대 HP, 드롭 ResourceDefinition, 기본 수량, 파괴 가능 여부.
- 셀별 광종을 외형과 별도로 보존하도록 논리 타일 종류 또는 전용 셀 데이터 중 하나를 선택한다. **권장: 채굴 정의를 참조하는 논리 Tile 에셋**. 공통 Platform 타일은 보호 지형으로 남긴다.
- StageTileSet/편집기에 광종 지정 동작을 명시적으로 추가한다. 현재 외형 칠하기만 하던 동작이 무조건 게임 데이터를 바꾸게 만들지 않는다.
- 런타임 관리자가 셀별 현재 HP를 소유한다. 공유 Tile/ScriptableObject 에셋에 현재 HP를 쓰지 않는다. 셀마다 GameObject 생성하지 않는다.
- 파괴 시 Logic+Visual 같은 셀 제거, 주변 AutoTile 갱신, 물리 충돌 갱신 및 파괴 이벤트를 처리한다. 파괴 이벤트는 셀/월드 위치/채굴 정의를 포함한다.
- 편집기의 생성·채우기·지우기·기존 변환 유틸리티가 새 논리 타일 정보를 공통 타일로 덮어쓰지 않게 검토한다.
- 완료: 서로 다른 셀 HP 독립, 동일 정의 공유 안전, 보호벽 유지, 중복 파괴 이벤트 없음, 씬 재진입 원상 복원. 편집 저장 후 다시 열어 광종 유지.

### 5단계 — 플라즈마 연결과 자원 회수 (큼, 5A/5B 분할 권장)

대상: PlasmaGunController, PlasmaGunParticleEffects/레이저 관련 코드, 피해 계약, FragmentParticleManager/FragmentMagnetManager 연결.

- 5A: 타격 결과에 대상 식별자(맵+셀), 실제 표시 위치, 피해 전달 정보를 보관하도록 한다. Transform 목록만으로 타일을 표현하지 않는다.
- CompositeCollider 히트에서 셀을 판정한다. 접촉점·법선·경계 오차를 고려하며 CircleCast 중심점과 접촉점을 혼동하지 않는다. Tilemap 원점으로 레이저/파티클이 향하지 않게 한다.
- 첫 장애물을 기준으로 타격 판정하여 파괴 불가 벽 뒤 광물을 맞히지 않도록 한다.
- 5B: 연쇄 탐색을 서로 다른 셀 단위로 적용하고 기존 chainCount/감쇠/거리 효과를 유지한다. 탐색 범위를 제한하며 매 발사 전체 맵 검색을 하지 않는다.
- 파괴 이벤트의 드롭 구독자가 기존 FragmentParticleManager로 자원 파편을 생성한다. 실제 수집을 통해 기존 인벤토리와 첫 자원 이벤트로 이어지게 한다.
- 완료: 벽면·모서리·음수 좌표에서 정확한 셀 타격, 연속 조사 시 다음 셀 진행, 인접 셀 중복 피해 없음, 드롭 1회, 회수·귀환 자원 보존, 레이저·파티클 위치 확인.
- 이 단계까지 기존 부유물은 비교 검증에 사용할 수 있으나 최종 플레이에서는 7단계에 제거한다.

### 6단계 — 채굴 업그레이드 이전 (중간)

대상: FloatageUpgrades, FloatageDropMultiplierRuntimeData, GameRuntimeData, SceneFloatageDropMultiplier, 업그레이드 정의 및 프리뷰/편집기.

- 기존 부유물 정의→채굴 정의 매핑을 목록으로 작성한다. 같은 자원을 드롭하더라도 종류별 보너스가 다를 수 있으므로 ResourceDefinition 하나로 무조건 합치지 않는다.
- 보너스/배율의 계산 및 툴팁을 채굴 기준으로 이전한다. 직렬화된 효과 타입 변경 시 정의 에셋이 누락되지 않게 변환한다.
- 업그레이드 노드 ID/레벨을 유지해 기존 저장 구매 내역이 새 런타임 효과를 재구성하게 한다. 필요한 경우에만 제한적 변환을 추가한다.
- 스테이지 리스폰 확률 보너스는 생물에도 적용되므로 부유물 제거와 함께 무조건 삭제하지 않는다.
- 완료: 구매 전후 드롭 차이, 보너스/배율 중첩, 툴팁 일치, 기존 저장 불러오기 후 효과 유지, 정의 누락 오류 없음.

### 7단계 — 스테이지 전환과 부유물 제거 (중간~큼)

대상: StageDefinition/PopulationManager/SpawnedObject/편집기, Stage 씬과 EmptyStageTemplate, 부유물 프리팹·정의·카탈로그 참조.

- 먼저 초기 스테이지 한 곳에 시작 공간·귀환 동선·기본 광종을 배치하여 한 세션이 완결되는지 확인한다. 실제 배치 상태를 확인한 뒤 대표 씬을 선정한다.
- 광물이 벽이 되므로 기존 생물 랜덤 스폰에 공간/콜라이더 여유 검사를 추가한다. 재시도 횟수를 제한하고 빈 공간이 없으면 생성을 건너뛴다.
- 시작 위치, 생물, 귀환 지점이 벽에 묻히거나 보호벽으로 차단되지 않게 한다. 채굴 조각이 벽 안에 갇히는지도 확인한다.
- 부유물 초기 생성/리스폰 경로, 수동 배치, 전용 이동·체력바·애니메이션 및 사용하지 않는 정의를 참조 확인 후 제거한다. 생물 스폰/포획 경로는 보존한다.
- 대표 씬 검증 후 모든 사용 Stage 씬과 템플릿에 반영한다. 단순히 MaxCount=0만 설정한 상태를 최종 완료로 삼지 않는다.
- 완료: 부유물 없음, 채굴→수집→귀환→업그레이드→재출격 가능, 필요한 업그레이드 자원의 공급원 존재, 모든 씬/템플릿 참조 정상.

### 8단계 — 통합 시연 검증 (중간)

- 새 게임: 안내 → 업그레이드 → 출격 → 채굴 → 자원 회수 → 복귀 → 구매 → 재출격.
- 기존 저장: 구매 효과·해금·안내 진행이 유지되는지 확인.
- 실패 경로: 탄약 0, 배터리 소진, 세션 종료 중 안내, Pause 중 전환, 채굴 가능 셀 소진.
- 성능: 조밀한 맵 연쇄 채굴에서 충돌체 갱신과 파편 비용을 실제 Profiler로 측정. 요구가 확인되기 전에 청크 시스템을 선행 도입하지 않는다.
- 구조 검증: 컴파일 오류, Missing Script/직렬화 참조, obsolete Floatage 참조, `git diff --check` 확인.
- 테스트: 셀 판정·HP·드롭 1회·보호벽·연쇄 중복은 자동 검증 대상으로 삼고, 화면 연출·입력 충돌·실제 충돌 갱신·세션 흐름은 PlayMode로 확인한다. 테스트 실행 건수를 함께 보고한다.

## 5. 새 채팅 인계 방법

작업 순서: 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8. UI 1~3과 채굴 4~6은 기능상 분리되지만, 가벼운 모델에는 순차 작업으로 맡기는 편이 변경 추적이 쉽다.

각 단계 종료 시 이 문서에 완료 여부, 실제 수정 파일, 검증 결과, 다음 단계 의존성을 짧게 추가한다. 구현 중 새로운 기획 결정이 필요하면 해당 결정만 남기고 독립 작업은 계속한다.

새 채팅에 붙여 넣을 요청:

> C:\Unity\Astrodiver 프로젝트에서 Docs/Plans/2026-09-10-demo-revision-plan.md를 읽고 **1단계만** 구현해줘. 현재 코드와 작업 트리 변경을 먼저 확인하고, 문서의 완료 기준까지 검증해줘. 기존 사용자 변경은 보존하고 다음 단계로 범위를 넓히지 마. Unity 씬·프리팹 수정은 사용 가능한 Editor 도구와 해당 스킬을 이용해 수행해줘. 완료 후 수정 파일, 실행한 검증과 미검증 항목, 다음 단계 인계 내용을 문서에 기록해줘.

다음 채팅에서는 단계 번호만 변경한다. 5단계는 5A와 5B로 나눠 요청할 수 있다. 실제 모델은 사용자가 새 채팅에서 선택한다.

## 6. 단계 완료 기록

### 2026-09-11 — 3A 완료: 튜토리얼 표현·PrimeTween 전환

- 수정: `TutorialGuideDefinition`에 선택적 `Sprite` 아이콘과 `InputActionReference`를 추가했다. 입력 표시는 참조한 액션의 실제 binding display string으로 계산하므로, 3B에서 정의 자산에 액션만 지정하면 문구와 실제 키가 분리되지 않는다.
- 수정: `TutorialGuideItemUI`에서 Animator/`Complete` trigger/Rebind 의존성을 제거했다. PrimeTween으로 팝·페이드·유지·우측 상단 이동 및 완료 페이드·축소를 실행하며, 비활성화·재시작 시 진행 중 Sequence를 중단한다. 아이콘·입력 힌트가 없으면 각각 숨기고, 표시될 때 본문 여백을 조정한다.
- 수정: `TutorialGuideSystem`에 중앙 연출 큐와 별도 `Center Presentation Root`를 추가했다. 새로 활성화된 안내는 하나씩 중앙에 표시된 뒤 LayoutGroup의 목록으로 편입되며, 이미 목록에 있는 안내를 재정렬해도 재등장하지 않는다. 씬 전환/비활성화에서는 코루틴·큐·항목을 함께 정리한다.
- 프리팹: Unity 6000.5.1f1 Editor의 PrefabContents API로 `TutorialGuideSystem.prefab`에 stretch 중앙 root를, `TutorialGuideItem.prefab`에 선택적 Icon/Input Hint 자식과 참조를 추가했다. 기존 Animator 컴포넌트는 Item 프리팹에서 제거했고, 기존 AnimatorController 자산의 불필요한 Editor 위치 변경은 되돌렸다.
- 검증: Editor ready/비컴파일 상태에서 스크립트 재컴파일 성공, 두 프리팹의 `_centerPresentationRoot`·`_icon`·`_inputHint` 직렬화 참조 확인, TutorialGuides 소스의 Animator/Rebind/CompleteTrigger 참조 제거, Editor console error 0개를 확인했다. 등록된 Unity 테스트는 0개였다.
- 미검증: 정상 저장 세션으로 새 게임 첫 안내, 연출 중 완료, 여러 안내 큐, Hub↔Stage 전환·재로드의 실제 화면/PlayMode 동작은 자동 테스트가 없어 수동 검증하지 못했다. `git diff --check`는 Unity가 새 TMP/UI 컴포넌트에 기록한 기존 형식의 빈 `m_Name:`/`m_text:` trailing whitespace 때문에 실패한다.
- 다음 단계 인계: 3B에서만 실제 안내 정의·문구·InputActionReference 할당·완료 이벤트를 확정한다. 첫 채굴 완료 이벤트는 5단계 채굴 성공 지점까지 연결하지 않는다. 4단계의 채굴 데이터·타일맵에는 변경하지 않는다.

### 2026-09-11 — 3A 보완: 텍스트 중심 연출과 동적 높이

- 수정: 피드백에 따라 `TutorialGuideDefinition`의 아이콘·입력 액션 데이터와 `TutorialGuideItem.prefab`의 Icon/Input Hint 자식을 모두 제거했다. 조작 안내는 별도 UI 기획이 확정될 때까지 튜토리얼 항목에 포함하지 않는다.
- 수정: `TutorialGuideItemUI`가 TMP의 실제 preferred height를 측정해 `LayoutElement.preferredHeight`를 갱신한다. 긴 문구는 520 폭 안에서 줄바꿈되며, 목록의 우측 상단 VerticalLayoutGroup은 계산된 각 항목 높이를 사용한다.
- 수정: 중앙 표시 전에는 목록에 임시 배치·레이아웃 갱신하여 우측 상단의 실제 최종 world 위치와 동적 크기를 얻는다. 중앙에서는 base scale에서 빠르게 `1.16x`로 팝한 후, 그 위치로 이동하는 동안 base scale까지 서서히 돌아온다.
- 검증: Unity Editor PrefabContents API로 보조 UI 제거와 LayoutElement 연결을 적용했고, 재컴파일 성공을 확인했다. 실제 화면·긴 문구·해상도별 PlayMode 확인은 수동 검증이 필요하다.

### 2026-09-11 — 3A 보완: TutorialGuideText 단순화

- 수정: `Assets/Prefabs/UI/TutorialGuides` 폴더를 `Assets/Prefabs/UI/TutorialGuideText`로, `TutorialGuideItem.prefab`을 `TutorialGuideText.prefab`으로 Unity Editor AssetDatabase를 통해 이동·이름 변경했다. 기존 시스템 프리팹 참조는 GUID 유지로 자동 갱신된다.
- 수정: `TutorialGuideText` 프리팹은 루트 `TextMeshProUGUI`·`CanvasGroup`·`ContentSizeFitter`·`TutorialGuideItemUI`만 남긴다. 기존 Text/Background 자식 및 LayoutElement를 제거했으며 ContentSizeFitter는 가로·세로 모두 Preferred Size를 사용한다.
- 수정: GuideList VerticalLayoutGroup의 자식 width/height 제어를 해제해, 각 텍스트가 실제 preferred 크기로 우측 상단에 배치되도록 했다.
- 검증: Editor PrefabContents/AssetDatabase로 변환 및 이동 성공, 시스템의 `_itemPrefab`이 새 `TutorialGuideText.prefab`을 참조함, 루트 자식 0개, ContentSizeFitter 가로/세로 Preferred Size, GuideList 자식 크기 제어 해제를 확인했다. 실제 화면에서 긴 단일 행 텍스트의 화면 경계 처리와 해상도별 연출은 수동 PlayMode 검증이 필요하다.

### 2026-09-11 — 3A 보완: TutorialGuideTextUI 이름 정렬

- 수정: Unity Editor AssetDatabase로 `TutorialGuideItemUI.cs`를 `TutorialGuideTextUI.cs`로 GUID 유지 이동하고, 컴포넌트 클래스와 `TutorialGuideSystem`의 모든 타입 참조를 `TutorialGuideTextUI`로 변경했다. 프리팹의 기존 스크립트 GUID 참조는 유지된다.

### 2026-09-11 — 3A 보완: 씬 동기화와 이벤트 연출 분리

- 수정 파일: `Assets/Scripts/TutorialGuides/TutorialGuideSystem.cs`, `Assets/Scripts/TutorialGuides/TutorialGuideTextUI.cs`, `Docs/Plans/2026-09-10-demo-revision-plan.md`.
- 수정: `TutorialGuideSystem`의 목록 갱신을 즉시 동기화와 신규 항목 연출로 분리했다. 시스템 바인딩, 저장 데이터 로드, 비-MainMenu 씬 진입은 현재 조건을 만족하는 모든 안내를 우측 상단 목록에 즉시 배치한다.
- 수정: `ProgressEventCompleted`로 실제 진행 이벤트가 발생할 때만 새로 표시 가능한 안내를 중앙 팝·이동 연출 큐에 넣는다. 완료 애니메이션 뒤의 재동기화도 즉시 방식으로 처리해, 씬/저장 복원 항목이 다시 연출되지 않는다.
- 수정: 즉시 동기화는 대기 중인 연출 큐를 비우고, 중앙에서 이동 중인 항목도 `TutorialGuideTextUI`를 통해 목록의 최종 sibling 위치·기본 scale·불투명 상태로 정착시킨다.
- 검증: Unity 6000.5.1f1 Editor 재컴파일 성공, ready/비컴파일 상태, 콘솔 오류 0개, TutorialGuideSystem 프리팹의 `_itemPrefab` `TutorialGuideTextUI` 참조, `git diff --check` 성공을 확인했다.
- 미검증: 실제 PlayMode에서 MainMenu→Hub/Stage 진입 직후의 무연출 표시, 이벤트로 해금되는 신규 안내의 중앙→목록 연출, 전환 중 진행 중이던 연출의 즉시 정착은 수동 확인이 필요하다.
- 다음 단계 인계: 3B에서만 안내 정의/문구/완료 조건을 확정한다. 이 분리된 갱신 정책은 유지하고, 기존 안내 구현이나 4단계 이후 채굴 흐름으로 범위를 넓히지 않는다.

### 2026-09-11 — 3A 보완: 중앙→목록 이동 좌표 안정화

- 수정 파일: `Assets/Scripts/TutorialGuides/TutorialGuideTextUI.cs`, `Docs/Plans/2026-09-10-demo-revision-plan.md`.
- 수정: 중앙 연출의 이동 목적지를 읽기 전에 `GuideList`의 `VerticalLayoutGroup`을 즉시 재계산한다. 따라서 항목의 확정된 우측 상단 목록 위치를 중앙 Presentation Root 좌표로 변환해 Tween에 전달하며, 프레임 지연 레이아웃의 이전 좌표를 향해 이동하지 않는다.
- 검증: Unity 6000.5.1f1 Editor 재컴파일 성공, ready/비컴파일 상태 및 콘솔 오류 0개, `git diff --check` 성공을 확인했다. 실제 PlayMode에서 중앙→우측 상단 이동 경로는 수동 확인이 필요하다.

### 2026-09-11 — 3A 보완: 완료 코루틴의 연출 중단 제거

- 수정 파일: `Assets/Scripts/TutorialGuides/TutorialGuideSystem.cs`, `Docs/Plans/2026-09-10-demo-revision-plan.md`.
- 원인: 진행 이벤트는 완료될 기존 항목의 `0.2s` 완료 애니메이션과, 새로 표시될 항목의 `0.18s + 0.55s` 중앙→목록 연출을 동시에 시작한다. 기존 항목의 완료 코루틴이 끝난 직후 즉시 동기화를 호출하면서, 진행 중이던 새 항목의 Presentation Queue를 중단하고 목록 위치에 강제로 정착시켰다.
- 수정: 완료 코루틴은 자신을 제거하는 역할만 맡긴다. 진행 이벤트 직후의 `RefreshVisibleGuides(true)`가 신규 항목을 이미 큐잉하므로, 완료 뒤 별도 갱신은 필요하지 않다. 이로써 새 항목의 팝과 이동 Tween이 끝까지 유지된다.
- 검증: Unity 6000.5.1f1 Editor 재컴파일 성공, 콘솔 오류 0개를 확인했다. `git diff --check`는 이번 변경과 무관한 기존 `Assets/Scenes/Hub.unity`의 `m_Name:` trailing whitespace 3건으로 실패했으며 해당 사용자 변경은 보존했다. 실제 이벤트로 기존 안내 완료와 신규 안내 해금이 동시에 발생하는 PlayMode 시각 검증은 수동 확인이 필요하다.

### 2026-09-11 — 3A 보완: Unity Object null 비교와 연출 흐름 검토

- 수정 파일: `Assets/Scripts/TutorialGuides/TutorialGuideTextUI.cs`, `Docs/Plans/2026-09-10-demo-revision-plan.md`.
- 수정: `TutorialGuideTextUI.Initialize()`의 `_canvasGroup ??=`와 `_text ??=`를 명시적인 `== null` 검사로 교체했다. UnityEngine.Object의 파괴된 객체 판별 연산자를 우회하지 않는다. TutorialGuides 소스에는 null 조건 연산자(`?.`) 사용이 없다.
- 검토: `Bind()`의 TMP mesh 갱신과 항목 자신에 대한 `ForceRebuildLayoutImmediate`는 텍스트 변경 직후 ContentSizeFitter의 preferred size를 확보하기 위해 필요하다. `PlayPresentation()`의 GuideList 강제 rebuild는 중앙 이동 전에 최종 목록 좌표를 읽기 위해 필요하다. 이어지는 `Canvas.ForceUpdateCanvases()`는 앞 rebuild가 처리한 레이아웃과 중복이므로 실제 화면 확인 후 제거 후보이다.
- 검토: 기존 항목 완료와 신규 항목 해금이 동시에 일어날 때, 완료 중인 항목이 GuideList 레이아웃 공간을 계속 차지해 신규 항목이 한 칸 아래를 목표로 잡고, 완료 항목 제거 후 위로 튀는 구조다. 가장 단순한 해결은 신규 안내의 큐잉을 기존 완료 애니메이션 이후로 지연해, 목록에서 완료 항목이 제거된 뒤 최종 위치를 계산하는 방식이다. 이 흐름 변경은 이번 검토에서는 적용하지 않았다.

### 2026-09-11 — 3A 보완: 연출 레이아웃 간소화와 완료 항목 분리

- 수정 파일: `Assets/Scripts/TutorialGuides/TutorialGuideTextUI.cs`, `Assets/Scripts/TutorialGuides/TutorialGuideSystem.cs`, `Docs/Plans/2026-09-10-demo-revision-plan.md`.
- 수정: `Bind()`의 TMP `ForceMeshUpdate`와 항목 단위 강제 layout rebuild, 목록 rebuild 뒤의 `Canvas.ForceUpdateCanvases`를 제거했다. 중앙→목록 목적지 산정에 필요한 GuideList 단위의 즉시 rebuild만 유지한다.
- 수정: 완료 안내는 페이드·축소를 시작하기 전에 `_items`와 GuideList에서 즉시 제외하고, 현재 화면 위치를 유지한 채 Center Presentation Root로 옮긴다. 같은 이벤트에서 새로 해금된 안내는 제거된 항목의 레이아웃 공간을 포함하지 않은 최종 위치를 목표로 중앙 연출을 시작한다.
- 검증: Unity 6000.5.1f1 Editor 재컴파일 성공, ready/비컴파일 상태, 콘솔 오류 0개를 확인했다. TutorialGuides에는 목적지 산정용 GuideList `ForceRebuildLayoutImmediate` 하나만 남았다. `git diff --check`는 기존 사용자 `Assets/Scenes/Hub.unity`의 `m_Name:` trailing whitespace 3건으로 실패했으며 해당 변경은 보존했다. 실제 PlayMode에서 완료 항목 페이드와 신규 항목의 중앙→최종 목록 위치 연출은 수동 확인이 필요하다.

### 2026-09-11 — 3A 보완: 목록 anchor/pivot 복원

- 수정 파일: `Assets/Scripts/TutorialGuides/TutorialGuideTextUI.cs`, `Docs/Plans/2026-09-10-demo-revision-plan.md`.
- 원인: `TutorialGuideText` 프리팹의 목록용 RectTransform은 우측 중앙 anchor/pivot `(1, 0.5)`지만, 중앙 연출이 이를 중앙 `(0.5, 0.5)`로 변경한 뒤 목록에 돌려보낼 때 복원하지 않았다. Tween은 이전 목록 상태에서 계산한 목적지로 이동하고, 종료 후 VerticalLayoutGroup은 변경된 anchor/pivot으로 다시 배치해 위치가 튀었다.
- 수정: 인스턴스 초기화 시 목록용 anchorMin/anchorMax/pivot을 보존하고, 중앙 연출 시작 전·종료 후 및 즉시 목록 정착 경로에서 복원한다. 이제 이동 목적지 계산과 최종 LayoutGroup 배치가 같은 RectTransform 기준을 사용한다.
- 검증: Unity 6000.5.1f1 Editor 재컴파일 성공, ready/비컴파일 상태, 콘솔 오류 0개를 확인했다. `TutorialGuideText.prefab`의 목록용 anchorMin/anchorMax/pivot이 모두 `(1, 0.5)`임을 Editor에서 확인했다. `git diff --check`는 기존 사용자 `Assets/Scenes/Hub.unity`의 `m_Name:` trailing whitespace 3건으로 실패했으며 해당 변경은 보존했다. 실제 PlayMode에서 중앙→목록 이동 종료 시 위치 연속성은 수동 확인이 필요하다.

### 2026-09-11 — 3A 보완: 런타임 anchor 변경 제거

- 수정 파일: `Assets/Prefabs/UI/TutorialGuides/TutorialGuideText.prefab`, `Assets/Scripts/TutorialGuides/TutorialGuideTextUI.cs`, `Docs/Plans/2026-09-10-demo-revision-plan.md`.
- 수정: Unity Editor PrefabContents API로 `TutorialGuideText` 루트의 anchorMin/anchorMax/pivot을 모두 우측 상단 `(1, 1)`으로 설정했다. 이 값은 목록의 우측 상단 정렬만을 위한 값이며 중앙 연출에 맞춰 바꾸지 않는다.
- 수정: 연출 중 RectTransform anchor/pivot을 변경·복원하던 코드를 제거했다. 중앙 배치는 현재 RectTransform의 pivot/크기를 고려해 월드 좌표로 계산하고, 중앙→목록 이동도 `Tween.Position`으로 최종 목록의 월드 pivot 위치까지 보간한다. 목록 복귀는 `SetParent(..., true)`로 목표 월드 위치를 유지한다.
- 검증: Unity 6000.5.1f1 Editor 도메인 재로드 후 ready/비컴파일 상태와 콘솔 오류 0개를 확인했다. Editor 직렬화 확인으로 프리팹의 anchorMin/anchorMax/pivot이 모두 `(1, 1)`이며, TutorialGuideTextUI에는 runtime anchor/pivot 대입이 없고 `Tween.Position` 이동만 남았음을 확인했다. `git diff --check`는 기존 사용자 `Assets/Scenes/Hub.unity`의 `m_Name:` trailing whitespace 3건으로 실패했으며 해당 변경은 보존했다. 실제 PlayMode에서 중앙 표시 위치와 이동 시작 전 튐 제거는 수동 확인이 필요하다.

### 2026-09-11 — 3A 보완: 가이드 텍스트 가독성 Material Preset

- 수정 파일: `Assets/Arts/99_Fonts/PFStardust/TMP/PFStardust-ExtraBold TutorialGuide.mat`, `Assets/Prefabs/UI/TutorialGuides/TutorialGuideText.prefab`, `Docs/Plans/2026-09-10-demo-revision-plan.md`.
- 수정: `PFStardust-ExtraBold SDF`의 기본 머터리얼을 복제한 전용 `PFStardust-ExtraBold TutorialGuide` Material Preset을 생성했다. 어두운 Outline과 Underlay를 활성화해 밝은 배경·복잡한 배경 모두에서 가이드 문구의 가장자리를 분리한다. 기본 폰트 에셋/기본 머터리얼은 변경하지 않았다.
- 수정: `TutorialGuideText`의 `TextMeshProUGUI.fontSharedMaterial`에 이 전용 프리셋만 할당했다.
- 검증: Editor에서 프리셋 생성과 프리팹의 `fontSharedMaterial` 참조를 확인했다. 프리셋은 `OUTLINE_ON`·`UNDERLAY_ON` 키워드를 모두 사용하며, Unity 재컴파일은 변경할 스크립트가 없어 up-to-date였다. 콘솔의 오류 1건은 TMP FontAsset에 컴포넌트 조회를 잘못 요청한 검증 명령 기록이며 런타임/컴파일 오류는 아니다. `git diff --check`는 기존 사용자 `Assets/Scenes/Hub.unity`의 `m_Name:` trailing whitespace 3건으로 실패했고, 기존 PFStardust 폰트 에셋 변경도 보존했다. 실제 PlayMode에서 Outline/Underlay 강도와 화면별 가독성은 수동 확인이 필요하다.

### 2026-09-11 — 2단계 재구현: 공통 인벤토리 정렬과 보조 잔탄 표시

- 수정: `Assets/Prefabs/UI/HUD/InventoryHUD.prefab`의 Creature Inventory Bar와 Resource Fragment List를 좌하단 anchor/pivot으로 정렬했다. 슬롯 바는 `(32, 84)`, 자원 리스트는 그 위 `(32, 176)`을 기준으로 하며, GridLayoutGroup은 LowerLeft/가로 시작/한 줄로 고정했다. `SessionHUD`의 중첩 인스턴스와 `Hub` 씬 인스턴스에도 같은 설정을 Editor로 확인·적용했다.
- 수정: `CreatureInventoryBarUI.cs`가 각 슬롯을 숫자 순서의 sibling index로 정렬하게 해, 용량 증가/초기 배치 순서와 무관하게 왼쪽부터 오른쪽으로 채운다.
- 수정: 기존 `SessionEquipmentAmmoHUD`의 무기 아이콘, `current / total` 텍스트, 선택 tint, 잠금 처리에는 변경하지 않았다. 기존 우하단 잔탄 그룹만 배터리 영역을 피해 소폭 위로 이동했다.
- 추가: `SessionAmmoSupplementHUD.cs`와 `SessionHUD.prefab`의 별도 `Ammo Supplement Group`을 추가했다. 네트는 `net.png` 아이콘을 하단부터 위로 쌓아 표시하고, 기존 아이콘을 재사용한다. 플라즈마는 별도 BottomToTop Slider로 비율을 표시한다. 두 보조 표시는 기존 수치 UI 위쪽에만 배치한다.
- 검증: Unity 6000.5.1f1 Editor ready/비컴파일 상태에서 `InventoryHUD`, SessionHUD, Hub 씬을 저장했다. Editor 검증으로 두 InventoryHUD 사용처의 좌하단 anchor/pivot 및 자원 리스트 상단 위치, GridLayoutGroup의 좌→우 채움, 기존 두 Ammo Text 활성 상태, 보조 그룹의 상단 위치, 네트 3개 생성 및 하단→상단 위치, 플라즈마 BottomToTop Slider를 확인했다.
- 미검증: 실제 PlayMode에서 Hub↔세션 전환, 16:9/좁은 화면비, 실제 발사/해금/새 세션 및 세션 종료 창과의 겹침은 수동 검증하지 못했다. 콘솔에는 이번 작업 전 시각의 TutorialGuideSystem `IsMemoryOnlySession` 관련 오류가 남아 있다. `git diff --check`는 Unity가 새 UI 컴포넌트에 기록한 빈 `m_Name: ` 필드의 trailing whitespace 때문에 실패한다.
- 다음 단계 인계: 3단계에서는 TutorialGuideSystem 표현·진행만 다룬다. 공통 InventoryHUD 좌하단 좌표, 슬롯 sibling 순서 보정, 기존 잔탄 UI와 분리된 Ammo Supplement Group은 변경하지 않는다.

### 2026-09-11 — 2단계 보완: 단일 활성 보조 잔탄 표시

- 수정: `SessionHUD.prefab`의 `Ammo Supplement Group`을 우측 중앙 anchor/pivot `(1, 0.5)`로 옮기고 `360 x 440`으로 확대했다. 네트 아이콘 스택과 플라즈마 바는 이 그룹의 동일한 중앙 좌표를 공유한다.
- 수정: `SessionAmmoSupplementHUD`가 `PlayerAttackController.EquipmentSelected`를 구독한다. 활성 무기가 NetGun이면 잠금 해제된 네트 스택만, PlasmaGun이면 플라즈마 바만 표시하며, 둘은 동시에 표시하지 않는다. 기존 기본 잔탄 UI에는 영향을 주지 않는다.
- 검증: Unity Editor 재컴파일 완료 후 프리팹 검증으로 우측 중앙 anchor, 대형 영역, 두 보조 표시의 동일 좌표, 플라즈마 선택 시 네트 숨김/플라즈마 표시, 네트 선택 시 플라즈마 숨김을 확인했다.
- 미검증: 실제 PlayMode에서 NetGun이 해금된 상태의 네트 표시 전환, 해상도별 대형 UI 가독성, 발사 중 갱신은 수동 검증하지 못했다.

### 2026-09-11 — 2단계 보완: 네트 보조 표시 레이아웃 단순화

- 수정: `SessionAmmoSupplementHUD.cs`에서 네트 아이콘의 spacing/RectTransform 위치 계산을 제거했다. 코드는 필요한 아이콘의 생성·재사용·활성 상태만 관리한다.
- 수정: `SessionHUD.prefab`의 Net Ammo Stack에 VerticalLayoutGroup을 추가해 아이콘을 그룹 안의 우측 중앙에 정렬한다. LayoutGroup은 아이콘 크기를 제어하지 않으며, 아이콘 템플릿은 프리팹 내부 localScale X를 음수로 설정해 좌우 반전한다. 복제 아이콘도 이 반전을 상속한다.
- 검증: Unity Editor 재컴파일과 프리팹 검증으로 VerticalLayoutGroup의 MiddleRight 정렬, 아이콘 크기 제어 비활성, 템플릿의 좌우 반전을 확인했다.

### 2026-09-11 — 2단계 보완: 반전 아이콘의 LayoutGroup 위치 보존

- 수정: LayoutGroup이 배치하는 `Icon Template` 루트의 X scale을 정상값으로 되돌렸다. 이로써 사용자가 프리팹에서 조정한 아이콘 위치와 런타임 복제본의 레이아웃 위치가 반전되지 않는다.
- 수정: 템플릿 내부에 stretch된 `Visual` 자식을 추가하고, 이 자식의 X scale만 음수로 설정했다. 루트 Image는 렌더링하지 않고 Visual Image가 같은 스프라이트를 좌우 반전해 표시한다.
- 검증: Editor 프리팹 검증으로 템플릿 루트 정상 scale, 원본 Image 비표시, 내부 Visual의 반전 스프라이트를 확인했다.

### 2026-09-10 — 1단계 완료: 업그레이드/스테이지 선택 닫기 입력

- 수정: `Assets/InputActions.inputactions`의 UI 맵을 키보드/마우스용으로 정리했다. 기본 `*/{Submit}`은 `<Keyboard>/enter`로, 기본 `*/{Cancel}`은 `<Keyboard>/escape`와 `<Keyboard>/x`의 두 명시 바인딩으로 교체했다. UI Navigate의 게임패드 바인딩과 Pen/Touch/XR 포인터·클릭 바인딩은 제거했다. Player 맵의 기존 Esc/X 변경은 보존했다.
- 수정: `Assets/Scenes/Hub.unity`의 `Upgrade Input Prompts`와 `Stage Input Prompts`에 마지막 행 `Prompt - Close`를 추가했다. 기존 Zoom 프롬프트의 레이아웃을 복제해 `input_keyboard_esc` 아이콘과 `닫기` 텍스트를 표시한다. 보고서 UI에는 프롬프트를 추가하지 않았다.
- 입력 진입점: `UIInputHandler.CancelPressed`를 그대로 사용한다. `UpgradeInteractor`는 보고서가 열렸으면 보고서만 먼저 닫고, 아니면 업그레이드 패널을 닫는다. `StageSelectionUI`도 같은 이벤트를 구독해 닫는다. 별도 X 전용 경로는 추가하지 않았다.
- 검증: 연결된 Unity 6000.5.1f1 Editor가 ready/비컴파일 상태임을 확인했다. PlayMode에서 실제 UI `Cancel` 액션에 Esc/X 키 상태를 각각 주입해 스테이지 선택 및 업그레이드 UI의 닫기를 검증했다 (`stageEsc=True`, `stageX=True`, `upgradeEsc=True`, `upgradeX=True`). 해당 테스트 중 Player 입력은 비활성 상태여서 공격 입력으로 전달되지 않았다. UI 바인딩 JSON 파싱과 두 `Prompt - Close` 오브젝트의 Esc 스프라이트/텍스트도 확인했다.
- 미검증: 실제 마우스 상호작용으로 패널을 열어 장시간 키 유지, 보고서 표시 중 Esc/X, Pause UI와의 동시 입력을 수동 화면으로 검증하지 않았다. `git diff --check`는 기존 Hub 씬 직렬화 변경의 trailing whitespace 때문에 실패한다. 사용자 변경을 보존하기 위해 그 광범위한 씬 정리는 하지 않았다.
- 다음 단계 인계: 2단계는 `SessionHUD.prefab`, `SessionEquipmentAmmoHUD.cs`, `BatteryBarUI.cs`만 대상으로 배터리 하단 중앙 배치와 탄약 HUD를 진행한다. 이번 단계의 UI Cancel 흐름과 Prompt 스타일을 변경하지 않는다.

### 2026-09-10 — 1단계 보완: TutorialDocumentView 입력 진입점 통일

- 수정: `Assets/Scripts/UI/TutorialDocument/TutorialDocumentView.cs`에서 `Keyboard.current` 직접 폴링(X/좌·우 화살표)을 제거했다. 문서가 활성화된 동안 `UIInputHandler.CancelPressed`를 구독하고, 닫힐 때 구독을 해제한다. 좌·우 페이지 버튼의 UI 탐색/실행은 기존 EventSystem과 UI 액션 맵에 맡긴다.
- 검증: Unity Editor의 도메인 재로드 후 ready·비컴파일 및 콘솔 오류 0개를 확인했다. PlayMode에서 UI Cancel 액션에 X와 Esc를 각각 주입했으며, 열린 문서가 두 경우 모두 닫기 애니메이션으로 진입했다 (`DocumentX closes=True`, `DocumentEsc closes=True`).
- 미검증: 실제 키보드로 다중 페이지의 좌·우 버튼을 탐색하고 마지막 페이지에서 닫는 수동 화면 검증은 하지 않았다. PlayMode 테스트 중 문서가 즉시 닫힐 때 기존 PrimeTween의 동일 endValue 경고가 발생했으나, 이번 입력 변경에서 새 오류는 아니었다.
