# 데모 이후 UI·채굴 전환 작업 계획

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
