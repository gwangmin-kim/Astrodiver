# 데모 이후 UI·충돌 타일맵 전환 계획

최초 작성: 2026-09-10 / 구조 재정리: 2026-09-12 / Unity 6000.5.1f1.

이 문서가 현재 작업 기준이다. [이전 계획과 상세 완료 기록](2026-09-12-demo-revision-history.md)은 이력으로만 보존하며, 그 안의 과거 설계·다음 단계 지시는 적용하지 않는다. 이번 재구성에서는 현재 소스·직렬화된 프리팹·완료 기록을 확인했다. Unity 실행이나 과거 테스트를 재실행하지 않았으며 구현 파일은 수정하지 않았다.

## 1. 확정된 설계 변경

기존 Logic/Visual 분리는 논리적으로 동일한 파괴 불가 벽에 서로 다른 외형을 입히기 위한 구조였다. 이제 **타일의 외형은 타일 정의에 종속**되므로 외형만 별도로 교체하는 구조를 종료한다.

- 충돌 플랫폼은 단일 Tilemap이 배치·렌더링·충돌의 기준이 된다. Logic/Visual 쌍을 유지하지 않는다.
- 정의가 외형/AutoTile 규칙, 파괴 가능 여부, 최대 HP, 자원 드롭 설정을 소유한다. 셀에 정의에 대응하는 타일을 배치하면 외형과 게임 속성이 함께 결정된다.
- 현재 DecorationBack/DecorationFront 블록은 모두 제거한다. 씬 데이터, 컴포넌트 참조, 레이어 enum/mask, 배치·채우기·외형 교체 UI, 생성/변환 유틸리티와 전용 자산 참조도 정리 대상이다.
- 꾸밈용 타일 시스템은 나중에 별도로 만든다. 충돌 타일맵의 레이어·정의·런타임 관리자·편집 모드에 다시 넣지 않는다. 꾸밈에는 충돌·피해·HP·드롭·채굴 이벤트가 없다.
- 충돌 타일맵은 셀 파괴, 파괴 시 자원 드롭, 구조 변경 시 주변 AutoTile 재계산을 지원한다. 현재 작업은 먼저 구조 단순화를 끝내고 무기·드롭을 연결한다.

2026-09-14 확정: 플라즈마는 타일만 타격한다. 기존 부유물의 역할은 MiningTile로 이전하며 `IDamagable`은 5A에서 완전히 제거한다. 향후 컴포넌트 기반 타격 대상이 필요하면 공통 추상 클래스를 검토하되, 이번에는 범용 대상 계층을 만들지 않는다. 별도의 파괴 불가 외곽벽 정책이나 전용 정의는 도입하지 않는다. 기존 `IsDestructible` 필드의 제거는 이번 확정 범위에 포함하지 않으며, 이를 새로운 벽 종류나 연쇄 차폐 정책으로 확장하지 않는다.

## 2. 현재 상태와 보존할 결과

아래의 “구현됨”은 소스/자산 또는 완료 기록으로 확인한 범위다. 실제 화면·PlayMode 미검증을 전체 완료로 간주하지 않는다.

| 단계 | 현재 상태 | 이후 처리 |
|---|---|---|
| 1 — 닫기 입력 | UI Cancel Esc/X, 업그레이드·스테이지 선택·문서 닫기 경로 구현 및 당시 입력 주입 검증 기록 | 유지. 장시간 키 유지·보고서/Pause 충돌은 통합 검증 |
| 2 — HUD | InventoryHUD 좌하단 및 자원 리스트 상단 배치, 긴 하단 배터리, 기본 잔탄+별도 보조 표시 구현 | 재구현하지 않고 최신 구조 유지. 해상도·실제 발사 검증 남음 |
| 3A — 안내 표현 | TutorialGuideTextUI 중심 팝/목록 이동, 별도 머터리얼 연출 컴포넌트 구현 | 유지. 아이콘/키 힌트는 후속 결정으로 제거된 상태이므로 복구하지 않음 |
| 3B — 안내 세분화 | 완료 기록 없음. 현재 Definition은 텍스트/진행 조건 중심 | 채굴 연결 후 별도 진행. 3A 연출 재작성 금지 |
| 4R — 채굴 기반 | 현재 소스는 MiningTileDefinition : AutoTile, 단일 Tilemap, 셀별 HP/파괴 이벤트. stone/iron/obsidian/crystal 정의 존재 | 현 구조를 이어서 사용. 물리 쿼리 문제는 사용자 해결 확인, 남은 실제 동작은 5A/5B에서 회귀 검증 |
| 5A — 무기 | 구현 완료: MiningHit/PlasmaMiningTargeting, 타일 전용 피해, 거리 연쇄, IDamagable 제거 | Editor/PlayMode Smoke 및 프리팹 검증 통과. 수동 입력/화면·성능 검증은 후속 |
| 5B — 타일/드롭 | 구현 완료: MiningFragmentDrops를 Stage_a_1/EmptyStageTemplate에 연결 | PlayMode 생성/회수/귀환 저장 검증 통과. 전체 플레이 동선은 후속 |
| 6 — 업그레이드 이전 | 2026-09-22 구현·전용 Editor/PlayMode 검증 완료 | 수동 구매 화면 확인은 8단계 |
| 7 — 부유물 제거 | 스폰·정의·프리팹·전용 표현이 남아 있음 | 공유 기능을 보존하며 제거 |
| 8 — 통합 검증 | 전체 시연 검증 미완료 | 구조/무기/드롭/맵 전환 후 수행 |

### UI의 최신 기준

- InventoryHUD: 좌하단 anchor/pivot, 인벤토리 위 자원 리스트, 슬롯은 좌→우 순서. Hub와 Session의 공통 프리팹 사용을 보존한다.
- 배터리: 하단 가로 stretch. 현재 SessionHUD 직렬화는 좌우 합계 여백 72, 높이 18, 하단 위치 20이며 퍼센트 텍스트는 중앙에 배치돼 있다. 요청 기준은 길고 얇은 바와 바 위 중앙 퍼센트다. 실제 겹침/가독성은 화면 검증한다.
- 기본 우하단 무기 아이콘·current/total·선택 tint는 유지한다. 인벤토리와 기본 잔탄은 배터리 공간을 확보하도록 위로 이동한 구성을 유지한다.
- SessionAmmoSupplementHUD는 **우측 중앙에서 현재 무기 하나만** 표시한다. 네트는 VerticalLayoutGroup, 반전은 아이콘 루트가 아닌 Visual 자식, 플라즈마는 세로 바다. 과거의 “기존 숫자 UI를 교체” 또는 “두 보조 표시를 동시에 상단 배치” 지시는 폐기한다.
- 안내는 텍스트 중심이며 로드/씬 동기화는 즉시 목록 배치, 실제 진행 이벤트로 새 안내가 열릴 때만 중앙 연출한다. TutorialGuideTextMaterialAnimation이 연출용 머터리얼의 생성·복원·폐기를 소유한다.

### 현재 검증의 한계와 작업 트리

기존 4단계 기록에는 Editor SmokeTest 성공과 등록 EditMode 테스트 0개가 명시돼 있다. 실제 CompositeCollider 갱신, 화면 AutoTile, 씬 재진입, 무기 타격은 검증 완료가 아니다. 새 구조에서 관련 검증을 다시 수행한다.

2026-09-14 계획 갱신 시 작업 트리는 깨끗했다. 기존 채굴 구현과 완료 UI를 현재 소스에서 이어서 수정하며 과거 미커밋 상태를 복원하지 않는다. 기존 StageMapEditorWindow/StageMapSetupUtility/StageMapMiningSmokeTest는 현재 소스 검색에서 발견되지 않았다. 폐기 도구를 복원하지 않고 실제 남아 있는 편집기/테스트 호출부만 맞춘다. 각 구현 시작 시 최신 git status를 다시 확인하고 사용자 변경을 보존한다.

물리 쿼리 문제는 사용자 확인으로 해결됐다. 원인은 플라즈마건 타겟 LayerMask에 타일 레이어가 포함되지 않았던 것이며 현재 타일은 `Damagable` 레이어를 사용한다. 이 문제를 5A 착수 차단 사유로 유지하지 않는다. 이번 문서 갱신에서 Unity 실행을 재검증한 것은 아니므로, 실제 발사·셀 제거 후 다음 셀 타격·물리 갱신은 구현 검증에 포함한다.

## 3. 목표 구조와 책임

| 구성 | 책임 | 제외 |
|---|---|---|
| 충돌 타일 정의 | 외형/AutoTile 규칙, HP, 파괴 여부, 드롭 자원·수량 | 현재 HP, 특정 셀 위치 |
| MiningTileDefinition : AutoTile | 정의 에셋 자체를 배치하며 외형·충돌·채굴 데이터를 함께 제공 | 별도 논리 타일 에셋, 독립 외형 덮어쓰기, 셀별 상태 |
| StageMap | 단일 Tilemap 참조, 셀 조회/피해/제거, 셀별 HP, 주변 갱신·물리 갱신, 파괴 이벤트 | 꾸밈 관리, 무기 탐색, 드롭 직접 생성 |
| 플라즈마 타격 탐색 | 물리 접촉을 StageMap+셀로 해석, 별도 함수에서 거리 기반 다음 셀 탐색 | IDamagable, 비타일 피해, 가시선/노출면 조건 |
| 드롭 구독자 | 파괴 이벤트를 받아 정의의 기본 수량으로 파편 생성. 업그레이드 보정은 6에서 연결 완료 | 타일 제거 재실행, 무기 코드 의존 |
| 배치 편집기 | 정의 선택 → 배치/채우기/삭제, Undo, 유효성 검사 | Logic/Visual 선택, Decoration 레이어, 별도 Auto Texture 덧칠 |
| 추후 꾸밈 시스템 | 독립적인 비상호작용 장식 배치 | 충돌 타일 정의/HP/드롭/채굴 관리자 의존 |

현재 `MiningTileDefinition : AutoTile`을 유지한다. 동일한 HP/드롭 정보를 가진 두 정의 체계를 병존시키지 않으며 셀마다 외형을 따로 지정하지 않는다. 타격 결과와 드롭 구독 컴포넌트의 구체적인 타입명은 구현 시 확정한다.

셀별 HP는 런타임 사전에 두고 공유 에셋에는 기록하지 않는다. 셀마다 GameObject를 만들지 않는다. 단일 Tilemap에 TilemapRenderer와 TilemapCollider2D/CompositeCollider2D를 구성한다. AutoTile 구현을 직접 확인해 렌더링 규칙과 Grid 충돌이 함께 작동하도록 한다. 기존 “Visual 타일은 Collider=None” 검증은 새 시스템에 그대로 적용하지 않는다.

잠정 유지 정책: 재입장 시 원본 맵 복원, 세션 중 파괴 유지, 영구 파괴 저장/재성장/지역 중력/생물 발견 조건/펫·합성·인벤토리 통합은 후속 범위다. 꾸밈 시스템도 이번에는 구현하지 않는다.

## 4. 변경 계획 — 5A/5B/6 구현 완료, 후속 7/8

4R-A/B/C는 기존 구조 전환의 범위와 검증 기준으로 남긴다. 현재 단일 맵 구조를 다시 구현하거나 전체 씬 변환을 반복하지 않는다. 아래 확정된 5A/5B는 2026-09-15 구현했으며 단계별 실제 검증과 미검증은 작업 기록을 따른다.

### 4R-A — 정의 중심 단일 타일맵과 런타임 전환

대상: Assets/Scripts/Stage/Map/의 StageMap.cs, MiningDefinition.cs, MiningLogicalTile.cs, StageTileSet.cs 및 영향받는 편집기/SmokeTest 호출부.

- 기존 HP·드롭 정의를 새 충돌 타일 정의로 확장한다. 외형/AutoTile 규칙을 필수 연결한다.
- 현재 8종 Mining 정의와 자산 식별·참조를 가능한 보존한다. 8종에 대응하는 외형이 모두 존재한다고 가정하지 않는다. 현재 AutoTiles 폴더에서 확인한 이름은 iron/crystal 및 템플릿이며 나머지 외형은 별도 매핑 확인이 필요하다.
- 구조 검증용 정의부터 완결한다. 임시 외형이 필요하면 정의에 명시적으로 지정하고 기록한다. 누락 외형을 조용히 다른 광종으로 해석하지 않는다.
- StageMap을 Grid+단일 충돌 Tilemap으로 바꾸고 셀별 HP/조회/피해를 새 배치 타일에서 읽도록 옮긴다. 기존 TryGetMiningCell/TryApplyMiningDamage/MiningCellDestroyed 계약은 의미가 맞으면 재사용한다.
- 한 셀 제거 → 주변 AutoTile 갱신 → 물리 갱신 → 이벤트 한 번의 흐름을 유지한다. 갱신 범위는 실제 AutoTile 이웃 규칙에 맞춘다.
- 충돌 타일을 단순 비활성화/재활성화했을 때 HP가 초기화되는 현재 OnDisable 동작도 세션 정책과 맞는지 검토한다. 씬 재생성에 의한 리셋과 단순 비활성화를 구별한다.
- 기존 편집기·SmokeTest의 깨진 API 호출은 함께 수정한다. 옛 씬은 아직 변환 전임을 명시하고 변환 대상으로 진단한다. 폐기한 6개 맵 구조를 새 정식 API의 호환 분기로 유지하지 않는다.

완료 기준: 정의 변경으로 외형/속성이 함께 결정됨, 단일 맵에서 렌더링·충돌, 독립 HP/공유 에셋 불변/파괴 이벤트 1회. 새로운 임시 씬으로 기능을 확인하고 실제 자산 전환은 4R-B에서 수행한다. 무기·드롭 연결은 제외한다.

### 4R-B — 편집기 단순화와 기존 씬/에셋 전환

대상: StageMapEditorWindow.cs, StageMapSetupUtility.cs, StageMapDefaultTiles.cs, StageMapMiningSmokeTest.cs, Assets/Data/StageMaps, Assets/Data/Definitions/Mining, Stage 씬 및 EmptyStageTemplate.

- 편집 흐름을 “타일 정의 선택 → 배치/채우기/삭제” 하나로 정리한다. 기존 Mining 모드와 외형 Auto Texture 모드를 통합/대체한다.
- Decoration 레이어·마스크·블록·타일맵 자식과 전용 편집 UI/기본 타일 생성 경로를 제거한다. 추후 장식을 위한 빈 슬롯/enum/옵션도 남기지 않는다.
- 변환 전에 모든 StageMap 사용 씬/프리팹/템플릿을 조사하고 변환 표를 만든다. 표에는 셀 수, 기존 논리 종류, 기존 외형, 새 정의, Decoration 제거 수, 해소되지 않은 매핑을 기록한다.
- 기존 Mining 셀은 이미 지정된 HP/드롭 의미를 보존하고 새 정의가 정한 외형을 사용한다. 공통 Platform은 확정된 변환 정책에 따라 mining_000_stone에 대응시킨다.
- 기존 논리+시각 조합에 상충하는 매핑이 있으면 해당 자산을 명시한다. 광종을 추측해 덮어쓰지 말고 확정 가능한 매핑부터 처리한다.
- Unity Editor 도구로 대표 씬에서 변환·저장·재로드를 검증한 뒤 전체 사용처와 템플릿에 적용한다. 기존 논리 셀의 좌표/점유/충돌 경계가 유지돼야 한다.
- Decoration 블록 제거는 확정 범위다. 다만 스프라이트/텍스처/AutoTile 원본은 충돌 정의나 다른 기능이 사용할 수 있으므로 참조를 확인하고, 미사용 Decoration 전용 에셋만 정리한다.
- Logic/Visual 쌍, MiningLogicalTile의 옛 표현, 외형 교체용 StageTileSet/기본 타일 및 레이어 API의 남은 참조를 제거한다. 이름 변경은 GUID와 직렬화 참조를 함께 처리한다.
- 변환 도구는 Editor 전용 일회성 작업으로 제한한다. 런타임 이중 시스템을 남기지 않으며, 변환 후 재실행으로 셀/드롭 정의가 다시 바뀌지 않게 한다.

완료 기준: 모든 사용 Stage 씬/템플릿이 단일 맵 구조, Decoration 블록/참조 없음, 배치/채우기/삭제/Undo/저장·재로드 정상, 셀 누락과 Missing Script 없음. 과거의 Logic+Visual 동시 제거 SmokeTest는 단일 셀 제거와 정의 기반 외형 검증으로 교체한다.

### 4R-C — 새 구조 검증과 무기 연결 준비

- 실제 PlayMode에서 임시 피해 호출로 셀 파괴 후 외형 경계, CompositeCollider 통과 여부, 인접 정의 경계의 AutoTile을 확인한다.
- 0/음수 피해, 이미 파괴된 셀, 같은 정의의 복수 셀, 연속 파괴, 음수 좌표, 씬 언로드→재진입을 검증한다.
- 같은 정의/다른 정의가 붙었을 때의 AutoTile 연결 규칙을 명시하고 검증한다. 기본안은 같은 정의끼리 연결하되 서로 다른 광종 경계의 실제 외형을 확인한다.
- “StageMap+셀로 피해 전달, 이벤트에 셀/월드 위치/정의 제공” 계약과 실제 Collider 참조/LayerMask를 다음 단계에 기록한다.
- Editor SmokeTest 성공과 등록된 테스트 건수를 구분하고, 실행하지 못한 PlayMode 항목은 미검증으로 남긴다.

완료 기준: 단일 맵에서 채굴 기반이 검증되고 더 이상 기존 Logic/Visual 쌍을 요구하지 않는다. 물리 쿼리 누락은 2026-09-14 사용자 해결 확인으로 착수 차단에서 해제했다. 남은 회귀 검증은 5A/5B 검증에 포함하고 과거 미실행 항목을 완료로 바꾸지 않는다.

### 5A — 무기 측: 플라즈마 타일 전용 타격과 셀 연쇄

대상: `Assets/Scripts/Equipments/PlasmaGun/PlasmaGunController.cs`, `PlasmaGunParticleEffects.cs`, 신규 타일 타격 결과/탐색 코드, 필요한 `StageMap` 조회 API와 실제 편집기/테스트 호출부. `PlasmaGunLaserVisual.Show(start, end)`는 위치 기반 API를 재사용한다.

1. Transform 목록을 `StageMap + Vector3Int 셀` 식별자와 월드 표시 위치·접촉 법선을 가진 타일 전용 타격 결과 목록으로 교체한다. 피해는 해당 셀의 `TryApplyMiningDamage`에 전달하며 IDamagable/Floatage 우회 경로를 남기지 않는다.
2. 첫 타격은 기존 CircleCast 반경/사거리를 유지한다. `Damagable` 레이어의 결과 중 실제 StageMap 타일을 해석하며 남아 있는 부유물 Collider가 타일 후보를 가로채지 않도록 비타일 결과를 걸러낸다. 접촉점과 법선으로 경계 안쪽 셀을 판별하고 CircleCast 중심이나 Tilemap Transform 위치를 셀 위치로 사용하지 않는다. 모서리/경계 오차 보정은 접촉 부근으로 제한한다.
3. 다음 셀 찾기는 별도 함수(예: `TryFindNextMiningTarget`)로 분리한다. 직전 셀 중심에서 후보 셀 중심까지의 월드 거리로 반경 내 가장 가까운 셀을 선택한다. 이미 선택한 `StageMap+셀`은 제외하고, 동일 거리에서는 안정적인 순서로 선택한다. 가시선, 노출면, 벽 차폐, 내부 침투 검사나 제한은 추가하지 않는다. 첫 타격의 접촉점은 표시용이며 연쇄 거리 기준과 구분한다.
4. 후보는 연쇄 반경을 포함하는 지역 셀 범위만 조사하고 실제 거리로 걸러낸다. 매 발사 전체 Tilemap의 cellBounds를 순회하지 않는다. Collider 하나를 타일 하나로 간주하는 기존 OverlapCircle/Transform 중복 판정은 교체한다.
5. 한 공격 틱의 타격 목록을 먼저 정한 뒤 순서대로 피해를 적용한다. 같은 셀 재타격 금지는 해당 목록 안에서 적용하며 HP가 남은 셀은 다음 틱에 다시 타격할 수 있다. 기존 탄약 소비·차지·틱 간격·피해 반올림·연쇄 수/감쇠/거리 업그레이드와 AmmoChanged 계약을 보존한다.
6. 직격 레이저/파티클은 실제 접촉점, 연쇄 레이저/파티클은 선택한 셀 중심을 사용한다. 연쇄는 물리 가시선을 요구하지 않으므로 내부 셀 중심도 유효한 표시 위치다. 제거된 셀이나 이전 조준 결과를 계속 표시하지 않도록 표시 갱신 수명을 정한다.
7. `Assets/Scripts/DamagableObjects/IDamagable.cs`와 meta, 구현 선언, 탐색/호출 참조를 완전히 제거한다. 현재 구현체는 FloatageController뿐이고 호출자는 PlasmaGunController다. 같은 파일에 있는 AttackData/DamageSource도 전체 참조를 확인해 사용이 끝나면 함께 제거한다. FloatageController의 폐기된 피해 진입점과 그 전용 파괴/드롭 경로는 정리하되, 스폰·애니메이션·체력바가 참조하는 상태/이벤트와 업그레이드 계산 유틸리티는 남은 참조를 보존한다. 새 추상 클래스나 임시 인터페이스는 만들지 않는다.

완료 기준:
- 실제 플라즈마 발사로 벽면·모서리·음수 좌표의 올바른 셀에 피해를 주며, 연속 조사로 제거 후 다음 셀을 맞춘다.
- 같은 틱에 중복 셀 없음, 가장 가까운 셀 선택, 거리 밖 제외, 가려진 내부 셀도 거리 조건만으로 선택됨, 후보 소진 시 정상 종료.
- 탄약/차지/연쇄 업그레이드 계산 유지, 타격 결과와 레이저/파티클 위치 일치, 비타일 대상 피해 없음.
- Assets의 코드/직렬화 참조에서 IDamagable 잔존 없음, 컴파일 성공. 영향받는 실제 편집기/SmokeTest 호출부 확인. 이번 단계에서는 파편 생성을 연결하지 않는다.

### 5B — 타일 측: 파괴 이벤트와 자원 드롭 연결

대상: `Assets/Scripts/Stage/Map/StageMap.cs`, 신규 드롭 구독 컴포넌트, 필요한 `FragmentParticleManager` 연결부와 대표 Stage/템플릿의 구독 컴포넌트 배선. 기존 `FragmentMagnetManager`와 `PlayerInventoryController`를 재사용한다.

1. 기존 셀 HP/제거 구현을 재사용하고 `MiningCellDestroyed` 계약을 보완한다. 셀·셀 중심 월드 위치·정의를 제공하며 송신 StageMap이 필요하면 이벤트 데이터 또는 구독 참조로 식별한다. 무기 타격 결과를 이벤트의 필수 입력으로 만들지 않는다.
2. HP가 0이 된 셀만 제거 → 인접 AutoTile 갱신 → Collider 갱신 → 이벤트 한 번의 순서를 보장한다. 0/음수 피해, 이미 제거된 셀 재호출, HP가 남은 타격은 파괴 이벤트/드롭을 발생시키지 않는다.
3. 전용 컴포넌트가 OnEnable/OnDisable에서 해당 StageMap의 이벤트를 구독/해제하고 정의의 DropResource/BaseDropAmount를 `FragmentParticleManager.DropFragment`에 전달한다. 중복 컴포넌트/중복 구독을 막는다. StageMap과 무기는 드롭을 직접 생성하지 않는다.
4. 파편은 제거된 셀 중심의 셀 안쪽 작은 범위에 생성한다. 거리 연쇄로 내부 셀이 제거된 경우도 포함해 기존 자석 경로로 실제 회수되는지 확인한다. 회수 문제는 파편 생성/회수 연결에서 해결하며 연쇄에 가시선 제한을 역으로 추가하지 않는다.
5. 기본 드롭만 연결한다. 부유물 전용 배율·업그레이드 자산/저장 이전은 6에서 수행한다. 인벤토리 추가와 첫 자원 획득 이벤트, 귀환 시 자원 반영은 기존 경로를 유지한다.
6. 구독 컴포넌트 배선에 필요한 대표 Stage/빈 템플릿 변경만 수행한다. 전체 씬 변환이나 자원 분포 재설계는 포함하지 않는다.

완료 기준:
- 독립 셀 HP/공유 정의 불변, 파괴 이벤트와 파편 수량이 셀당 정확히 한 번이며 정의의 자원/기본 수량과 일치.
- 연속/연쇄 파괴, 내부 셀 파괴, 구독 비활성화→재활성화, 씬 전환에서 중복 생성/구독 누수 없음.
- 파괴→파편 생성→자석 회수→인벤토리→귀환 자원 보존, 기존 첫 자원 획득 이벤트 정상.
- 컴파일/실제 PlayMode 결과와 미실행 항목을 분리 기록. 등록 테스트 0개를 동작 검증 성공으로 표시하지 않는다.

### 5A/5B 공통 범위와 인계

- 완료 UI와 현재 정의 중심 단일 Tilemap을 보존한다. 전체 씬 변환, 별도 꾸밈, 범용 타격 추상화, 부유물 전체 스폰/자산 제거, 업그레이드 이전은 이번 범위에서 제외한다.
- 부유물 역할의 최종 이전은 확정이다. 5A에서 타격 인터페이스와 기존 무기 연결을 끊고, 5B에서 타일 기본 자원 공급을 연결한다. 남은 부유물 업그레이드는 6, 스폰/프리팹/전용 표현과 자산 제거는 7에서 끝낸다. 5A만 끝난 중간 상태에서는 부유물도 타격되지 않고 타일 드롭도 아직 없음을 인계에 명시한다.
- 권장 실행은 5A 검증 후 같은 작업에서 5B를 이어서 수행하는 방식이다. 별도 단계로 진행해도 두 완료 기준을 섞지 않는다. 2026-09-15에는 두 단계를 순차 구현했다. 후속 작업에서 완료 코드를 다시 작성하지 않는다.
- 단계 완료마다 수정 파일, 실제 검증 명령/환경/결과, 미검증 또는 실패, 남은 부유물 의존성, 다음 단계 시작점을 상태표와 작업 기록에 반영한다.

### 3B — 안내 세분화와 채굴 이벤트 연결

5B 이후 실제 채굴/회수 이벤트를 기준으로 진행한다. UI 자체를 다시 만드는 작업이 아니다.

- 이동/플라즈마 사용/첫 채굴/첫 자원 회수/복귀/포획 등 안내 문구·조건을 정리한다.
- 기존 진행 이벤트 숫자와 저장 의미를 보존하고 새 이벤트가 필요하면 실제 발생 지점에 연결한다.
- 텍스트 중심 표현, 씬/로드 즉시 동기화, 이벤트 기반 중앙 연출, 머터리얼 책임 분리는 유지한다.
- 아이콘·입력 키 표시는 후속 UI 결정 전까지 추가하지 않는다.

완료 기준: 새 게임 진행, 기존 저장 재진입, 연출 도중 완료/씬 전환, 안내 순서 확인. MemoryOnly에서 안내가 꺼지는 기존 정책을 고려해 정상 저장 세션으로 검증한다.

### 6 — 채굴 업그레이드 이전 (2026-09-22 완료)

- `FloatageUpgrades`를 `MiningTileUpgrades`로 이전했다. Bonus는 배율 가산, Multiplier는 배율 곱셈이라는 기존 의미를 보존한다.
- `MiningTileDropMultiplierRuntimeData`가 채굴 정의별 배율을 보관하고 `GameRuntimeData`가 소유한다. 구매 내역으로 재구성하며 별도로 저장하지 않는다.
- `MiningFragmentDrops`가 파괴 이벤트의 정의로 배율을 조회하고 정수 수량을 계산해 기존 파편 생성/회수 흐름에 전달한다. StageMap과 무기는 드롭 계산을 소유하지 않는다.
- 기존 041/042 업그레이드의 효과 8개를 stone/iron/obsidian/crystal 채굴 정의로 이전했다. 노드 ID·Key·에셋 GUID·레벨·비용·부모 관계·보너스 값은 보존한다. 저장 식별자 `upgrade.floatage_drop_1/2`와 Key는 이름 정리를 이유로 변경하지 않는다.
- 카탈로그의 채굴 정의 목록·검증·Editor 수집, 효과 추가 Inspector, 툴팁과 저장 SmokeTest의 관련 호출을 전환했다.
- 기존 부유물 에셋 전체에 대응하는 채굴 정의를 만들지 않는다. 자원/업그레이드 콘텐츠 완성과 생물 리스폰 확률 보너스 변경은 이 단계의 범위가 아니다.

완료 기준: 효과/직렬화 참조 정상, 보너스 중첩·정의별 독립성·툴팁, 구매/저장 재로드, 보정된 드롭/회수/귀환 저장 확인. 실제 통과 범위와 전체 저장 SmokeTest의 기존 기대값 불일치는 아래 기록을 따른다.

### 7 — 부유물 시스템 완전 제거

- 남은 부유물 정의·프리팹·생성/리스폰/수동 배치·이동·체력바·애니메이션과 전용 에셋을 참조 확인 후 제거한다.
- StageDefinition/StagePopulationManager/스폰 영역·카테고리, 전용 편집기, 카탈로그/데이터 작업창/테스트, 씬/템플릿의 코드 및 직렬화 참조를 함께 정리한다.
- 6에서 이전한 채굴 업그레이드와 저장 식별자는 보존한다. 생물 스폰·포획·저장, 공유 자원/파편 회수, 생물 리스폰 보너스는 보존한다.
- 새 광종 제작·광물 분포 설계·필수 업그레이드 자원 공급 완성·생물 스폰 빈 공간 검사와 재시도 기능 추가는 제외한다.

완료 기준: 부유물 전용 실행 경로와 누락 참조가 없음. MaxCount=0만 설정한 상태를 최종 제거 완료로 보지 않는다. 과거 기록/일회성 이전 스크립트/보존한 저장 식별자는 런타임 의존성과 구분한다.

### 8 — 통합 점검 (구현 추가 없음)

- 컴파일, Missing Script/자산 참조, 폐기된 Decoration/Logic/Visual/Floatage 실행 의존성, git diff --check를 확인한다.
- 현재 배치된 타일의 타격→HP 감소→파괴→드롭→회수와 업그레이드, 귀환→재출격을 확인한다. 재입장 시 저장된 원본 스테이지 배치와 초기 HP로 시작한다.
- 기존 UI/생물/저장 흐름의 회귀와 남은 실제 입력·화면·성능 검증을 수행한다. 자원 콘텐츠 미완성을 이유로 정의·배치를 추가하지 않는다.
- 타일별 HP·파괴 상태 영구 저장, 세션별 랜덤 생성, 동적 타일맵 생성·재성장은 이번 브랜치에서 구현하지 않는다. 기존 자원·구매 내역 저장은 유지한다.
- 발견한 결함은 원인·영향·필요 수정 범위를 기록하고 점검 단계에서 임의로 수정 범위를 확대하지 않는다. 실행한 검증과 미검증을 구분한다.

## 5. 후속 꾸밈 시스템 경계

추후 별도 요구로 시작한다. 이번에 Decoration을 삭제하면서 대체 장식 시스템을 선행 구현하지 않는다.

독립 컴포넌트/정의/편집 흐름을 사용하고 충돌 타일맵을 참조하지 않아도 배치·표시가 가능해야 한다. 공통 Unity Grid/Tilemap 기술의 사용 여부와 별개로 게임 책임·상태·이벤트는 공유하지 않는다. 충돌 지형의 파괴·드롭은 장식에 영향을 주지 않는다. 과거 Decoration 레이어를 StageMap에 다시 추가하는 방식은 사용하지 않는다.

## 6. 실행 순서와 새 채팅 요청

구현 완료: **5A → 5B → 6**. 이번 채굴 전환의 후속 순서: **7 → 8**. 3B 안내 세분화는 별도 범위이며 채굴 전환의 선행 조건으로 두지 않는다. 4R의 현 구조를 재사용하고 5A/5B의 남은 수동 검증은 통합 검증에서 확인한다.

아래는 완료한 5A/5B의 범위 참고용 요청문이다. 다음 작업에서 반복 구현 지시로 사용하지 않는다:

> C:\Unity\Astrodiver 프로젝트에서 Docs/Plans/2026-09-10-demo-revision-plan.md의 확정된 5A와 5B를 순차 구현해줘. 현재 단일 Tilemap과 완료 UI를 보존해. 5A는 타일 전용 플라즈마 타격, 셀 중심 거리 기반 최근접 연쇄, 같은 틱의 중복 셀 제외, 별도 다음 셀 탐색 함수, IDamagable 완전 제거를 수행해. 5B는 기존 셀 파괴 이벤트와 기본 자원 드롭/회수를 연결해. 가시선이나 벽 내부 침투 제한은 추가하지 마. 전체 씬 변환·부유물 전체 제거·업그레이드 이전·별도 꾸밈은 제외해. 단계별 수정 파일, 실제 검증/미검증, 후속 인계를 이 문서에 기록해. 과거 인계 지시는 적용하지 마.

5A만 진행하려면 위 요청의 범위를 “5A만”으로 지정한다. 이때 드롭은 연결하지 않고 5B 연결 지점과 중간 플레이 상태를 기록한다.
각 단계가 끝나면 상태표와 아래 기록을 함께 갱신한다. 과거 기록을 본문 위에 추가하거나 현재 계획을 옛 버전으로 되돌리지 않는다. 실제 씬·프리팹·자산 작업은 해당 Unity 스킬과 Editor 도구로 수행한다.

## 7. 재구성 이후 작업 기록

이하 이전 날짜의 실패/차단 및 인계는 당시 기록이다. 현재 실행 지시는 본문 5A/5B와 최신 확정 기록을 따른다.

### 2026-09-13 — 4R-C 검증: 채굴 상태 통과, 물리 충돌 차단

- 수정 파일: 이 기록 문서만 변경했다. PlayMode 검증용 임시 C# 스크립트는 `AgentScripts/`에서 실행 뒤 제거했으며, 게임 코드·정의 에셋·씬·프리팹은 변경하지 않았다.
- 실제 PlayMode 검증: `StageMap.TryValidate()` 성공. 음수 좌표의 임시 셀에서 0/음수 피해, 이미 제거된 셀 피해, 파괴 불가 정의 피해를 모두 거부했다. 같은 정의의 인접 셀은 한 셀 제거 후에도 독립적으로 남았고, HP 2의 런타임 복제 정의는 첫 타격에 HP 1로 유지되고 두 번째 타격에만 제거됐다. 셀 제거 이벤트는 좌표·월드 위치·정의를 정확히 제공했다. stone/iron/obsidian/crystal의 임시 배치 모두 AutoTile 스프라이트를 생성했다. PlayMode에서 제거한 기존 셀은 종료 후 재진입 시 원래 정의와 HP 1로 복원됐다.
- 실패/차단: 실제 배치 셀을 지나는 `Physics2D.Raycast`가 `CompositeCollider2D`를 한 번도 맞추지 못했다. Composite의 shape/path는 존재하지만 물리 쿼리에 응답하지 않아, 셀 제거 뒤 실제 통과 가능 여부를 판정할 수 없다. `TilemapCollider2D` 자체 shape 수 0은 Composite 병합 구성에서는 단독 결함으로 단정하지 않았지만, Composite까지 비응답인 현상은 완료 기준을 막는다.
- 별도 환경 오류: PlayMode 시작 시 `GameDefinitionCatalog`의 stage respawn probability 효과가 stage definition을 요구한다는 기존 카탈로그 오류가 Console에 기록됐다. 채굴 호출은 실행됐지만 깨끗한 PlayMode Console 검증은 이 오류를 해소하거나 범위를 분리한 뒤 다시 해야 한다.
- 필요한 후속 범위(이번 작업에서는 미수정): `Stage_a_1`과 `EmptyStageTemplate`의 Rigidbody2D/TilemapCollider2D/CompositeCollider2D 설정, 레이어 및 Physics2D 쿼리·충돌 설정을 조사해 실제 플레이어 물리 충돌과 레이캐스트가 Composite를 맞추도록 복구한다. 필요하면 `StageMap.TryValidate()`가 단순 컴포넌트 존재 여부뿐 아니라 그 유효 구성도 진단하도록 보완한다. 카탈로그 오류는 별도 데이터 정합성 범위로 분리한다.
- 인계: 물리 충돌 결함이 해소되고 동일 PlayMode 검증에서 셀 제거 전 충돌·제거 후 통과가 확인되기 전에는 4R-C 완료 또는 5A 착수를 선언하지 않는다.

### 2026-09-14 — PlayMode Catalog 정합성 복구

- 현재 유지하는 `stage_000_a_1`의 리스폰 확률 효과는 보존하고, `upgrade_044_stage_a_respawn_probability`에서 삭제된 Stage를 가리켜 null이 된 효과 두 개만 제거했다. 이후 남은 Stage 효과는 `stage.a_1` 하나다.
- 검증: `GameDefinitionCatalog.TryValidate()` 성공. Console을 비운 뒤 `Stage_a_1` PlayMode를 다시 시작했으며, 기존 stage definition 누락 오류는 재발하지 않았다(상호작용 대상 변경 일반 로그 1건만 확인).
- 수정 파일: `Assets/Data/Definitions/Upgrades/upgrade_044_stage_a_respawn_probability.asset`, 이 계획 문서. 임시 Editor 실행 스크립트는 제거했다.
- 인계: 이제 5A에서 `RaycastHit2D`의 접촉점으로 `StageMap + 셀`을 해석하고, Transform 기반 PlasmaGun 타격/레이저/파티클/연쇄 표현을 셀 단위 타격 결과로 전환한다. 드롭은 계속 5B 범위다.

### 2026-09-16 — MiningTileDefinition Inspector 보완

- `MiningTileDefinitionEditor`가 AutoTile Inspector를 유지한 채 하단 `Mining Properties` 접기 섹션에 `MiningTileDefinition`이 직접 선언한 Unity 직렬화 필드를 자동 표시하도록 확장했다. 이후 `[SerializeField]`·`[SerializeReference]` 필드 추가/제거는 별도 Editor 수정 없이 반영되며, 기본 `PropertyField` 바인딩으로 Undo와 에셋 저장 경로를 그대로 따른다.
- 검증: Unity 재컴파일 성공·Console 오류 없음. Editor API로 `mining_000_stone` Inspector를 생성해 전용 Editor 적용과 `Mining Properties` 존재를 확인했고, 현재 선언된 네 필드와 실제 표시 필드의 순서·목록이 일치함을 검증했다.

### 2026-09-12 — 4R-B 진행 중: 단일 miningTile 편집 구조

- 수정: 기존 다계층 Stage Map Editor/Setup/Default Tile 코드를 제거하고 단일 `miningTile` 생성 유틸리티와 최소 편집 창으로 교체했다. 공통 Platform 셀 변환 정책은 모두 `mining_000_stone` 정의를 배치하는 것으로 확정했다.
- 수정: `StageMap`의 Logic/Visual·Decoration·레이어 호환 API와 `StageTileSet`을 제거했다. 새 Setup 유틸리티는 기존 `Platform` 셀만 stone으로 옮기고 나머지 구 타일맵은 제거한다.
- 검증: Unity 6000.5.1f1 재컴파일 성공 및 `git diff --check` 통과.
- 미검증/차단: 씬 일괄 변환 runner가 Editor 전용 Setup 유틸리티 reflection을 찾지 못해 실행되지 않았다. 따라서 대표 씬/전체 Stage/EmptyStageTemplate 변환·저장·재로드, 셀 수/충돌 경계, SmokeTest, Missing Script 검증은 아직 완료되지 않았다. 이 상태에서 4R-C/5A로 진행하지 않는다.

### 2026-09-12 — 4R-B 보완: 유지 Stage와 빈 템플릿 변환

- `Stage_a_1`과 `EmptyStageTemplate`에서 기존 `Platform` 점유 셀을 `mining_000_stone`으로 단일 `miningTile`에 옮기고, 이전 PlatformVisual·Decoration Tilemap 자식을 제거한 뒤 Editor로 저장했다. `miningTile`에는 TilemapRenderer, Static Rigidbody2D, TilemapCollider2D, CompositeCollider2D를 구성하고 StageMap이 참조하도록 했다.

### 2026-09-12 — 4R-B 보완: AutoTile 규칙의 정의 통합

- `MiningTileDefinition`이 `AutoTile`을 직접 상속하도록 바꾸고, 기존 iron/crystal AutoTile의 직렬화된 규칙·스프라이트 데이터를 4개 MiningTileDefinition 에셋으로 복사했다. stone/iron/obsidian은 iron 규칙, crystal은 crystal 규칙을 소유하며 Grid 충돌과 채굴 데이터도 같은 에셋에 남는다. 별도 AutoTile 원본은 아직 참조 검사 전이므로 제거하지 않았다.
- 보완: AutoTile 전용 Inspector는 하위 타입에 자동 적용되지 않아 `MiningTileDefinitionEditor : AutoTileEditor`를 추가했다. 정의 에셋이 AutoTile을 상속한 뒤 규칙을 다시 복사해 Texture List와 템플릿 Load/Save UI가 표시되도록 했다.

### 2026-09-12 — 4R-A: 단일 충돌 Tilemap 전환

- 보완: 별도 `MiningLogicalTile` 에셋을 제거했다. 기존 `MiningDefinition` 스크립트 GUID를 유지한 `MiningTileDefinition : TileBase`가 AutoTile 외형, Grid 충돌, HP, 파괴 가능 여부, 드롭 설정을 한 SO 에셋에서 함께 소유한다.

- 수정: `MiningDefinition`이 AutoTile 외형을 소유하도록 확장했고, `MiningLogicalTile`은 정의의 AutoTile 렌더 규칙을 전달하면서 Grid 충돌을 제공하는 단일 배치 Tile이 됐다. 초기 4종 정의는 유지했으며 crystal은 `autotile_002_crystal`, stone/iron/obsidian은 임시로 `autotile_001_iron`을 명시적으로 참조한다.
- 수정: `StageMap`은 Grid와 단일 Tilemap만 소유하며 셀 HP·피해·제거·3x3 이웃 갱신·TilemapCollider 갱신·단일 파괴 이벤트를 그 Tilemap에서 처리한다. 무기·드롭·기존 씬 변환·꾸밈은 변경하지 않았다. 세션 간 HP 보존/복원 정책도 추가하지 않았다.
- 호환: 기존 StageMap 편집기/설정 도구가 컴파일되도록 제한적 Platform 호환 호출만 남겼다. 기존 씬의 Logic/Visual 구조를 새 런타임 분기로 지원하지 않으며 실제 변환과 편집기 단순화는 4R-B 범위다.
- 검증: Unity 6000.5.1f1 재컴파일 성공, Editor ready/비컴파일, `git diff --check` 통과. Editor API로 4개 정의의 AutoTile 참조를 저장했다.
- 미검증: 기존 SmokeTest는 단일 Tilemap API로 아직 완전히 재작성되지 않아 Pipeline 실행이 main-thread timeout으로 끝났다. 새 임시 씬의 렌더·CompositeCollider·독립 HP·이벤트 검증과 실제 씬 변환은 4R-B/4R-C에서 재검증한다.
- 4R-B 인계: `StageMapEditorWindow`, `StageMapSetupUtility`, `StageMapDefaultTiles`, SmokeTest를 정의 선택→배치/채우기/삭제의 단일 흐름으로 재작성하고, 대표 씬부터 기존 Logic/Visual·Decoration을 Editor로 변환·저장·재로드한다. 임시 iron 매핑은 개별 타일셋이 준비될 때 해당 정의만 교체한다.

### 2026-09-12 — 계획 재구성

- 소스/프리팹 및 기존 완료 기록을 확인하여 1/2/3A 구현, 3B 미완료, 기존 4의 부분 재사용 범위를 구분했다.
- 기존 4 이후에 4R-A/B/C를 추가하고 5 이후의 의존성을 단일 충돌 타일맵 기준으로 변경했다.
- 과거 본문의 외형 독립/Logic+Visual/Decoration 유지 지시를 폐기했다. UI 후속 수정도 현재 기준에 반영했다.
- 상세 과거 기록은 별도 이력 파일에 원문 보존했다. 이번 변경은 문서만이며 런타임 기능이나 씬 데이터를 변경하지 않았다.

### 2026-09-14 — 5A/5B 범위와 연쇄 정책 확정

- 사용자 확인: 물리 쿼리 문제는 플라즈마 타겟 LayerMask의 타일 레이어 누락으로 발생했으며 현재 Damagable 레이어 설정으로 해결됐다. 이전 물리 실패 기록을 현재 착수 차단으로 적용하지 않는다. 이번 작업에서 물리 테스트를 재실행한 것은 아니다.
- 확정: 5A는 무기의 타일 전용 타격/거리 기반 연쇄와 IDamagable 제거, 5B는 타일 파괴 이벤트/기본 드롭 연결이다. 연쇄는 같은 틱에 같은 셀을 다시 선택하지 않고 거리만으로 최근접 셀을 고르며, 다음 셀 탐색 함수는 별도로 둔다. 외곽벽 예외나 가시선/노출면 제한을 도입하지 않는다.
- 확정: 기존 부유물 타격은 유지하지 않는다. 범용 인터페이스/추상 클래스 대체물도 이번에 만들지 않는다. 남은 부유물 업그레이드 이전과 전체 스폰/자산 제거는 6/7에 남긴다.
- 수정 파일: Docs/Plans/2026-09-10-demo-revision-plan.md만 수정. 상태표, 현재 소스/검증 한계, 단계별 책임·대상·완료 기준, 실행 요청 예시를 갱신했다. 이력 문서는 수정하지 않았다.
- 실제 확인: git status, IDamagable 및 ApplyDamage 참조, FloatageController의 상태/드롭 의존성, 현재 StageMap 관련 소스와 테스트 파일 목록을 확인했다. IDamagable.cs에는 AttackData/DamageSource도 함께 정의되어 있어 삭제 시 함께 참조 정리가 필요하다.
- 검증: 문서 변경 내용을 확인했고 git diff --check가 통과했다. 런타임 코드/씬/프리팹은 수정하지 않았고 컴파일·PlayMode·발사·드롭 회수는 실행하지 않았다.
- 다음 단계: 구현 요청을 받으면 5A부터 진행하고 그 완료 기준을 검증한다. 5A/5B를 함께 요청한 경우 이어서 5B를 진행하되 단계별 결과를 각각 남긴다.

### 2026-09-15 — 5A 구현: 타일 전용 플라즈마 타격

- 변경 파일: `Assets/Scripts/Equipments/PlasmaGun/PlasmaGunController.cs`, `PlasmaGunParticleEffects.cs`, 신규 `MiningHit.cs`/`PlasmaMiningTargeting.cs` 및 meta, `Assets/Scripts/Stage/Map/StageMap.cs`, `Assets/Scripts/DamagableObjects/FloatageController.cs`. `IDamagable.cs`와 meta를 삭제했다. 같은 파일의 더 이상 사용하지 않는 AttackData/DamageSource도 삭제했다.
- 타격 계약: MiningHit는 StageMap+셀 식별자와 표시 위치/법선을 보관한다. 직격은 CircleCast 결과 중 타일을 실제 소유한 StageMap/Tilemap만 대상으로 삼으며 비타일 Collider를 제외한다. 접촉점의 법선 안쪽 0.002 보정과 접촉 인접 셀 한정 경계 판별을 사용한다. 직격 이펙트는 접촉점, 연쇄는 셀 중심이다.
- 연쇄 계약: PlasmaMiningTargeting.TryFindNextMiningTarget에서 활성 맵의 거리 반경에 해당하는 셀 범위만 검색한다. 셀 중심 간 최근접 거리, 같은 틱의 StageMap+셀 중복 제외, 동일 거리에서는 맵 등록 순서와 y/x 순서다. 가시선/노출면/벽 내부 제한은 없다. 맵 전체 cellBounds 검색이나 셀별 GameObject는 추가하지 않았다. StageMap.GetMiningDefinition은 HP 사전을 채우지 않는 조회 API이며 ActiveMaps는 활성 맵 등록/해제를 제공한다.
- 무기 계약: 차지/틱/탄약/업그레이드 계산과 AmmoChanged를 유지했다. 발사 중 프레임마다 타격 목록을 갱신하고 한 틱의 전체 목록을 확정한 뒤 피해를 적용한다. 파괴된 셀의 해당 틱 이펙트 위치는 스냅샷으로 유지하며 다음 프레임에는 새 타격 결과로 갱신한다. 비활성화 시 타격 목록과 이펙트를 정리한다. PlasmaGunLaserVisual의 기존 위치 기반 API와 UI 파일은 변경하지 않았다.
- 부유물 경계: FloatageController의 인터페이스 선언, ApplyDamage, 그 전용 ResolveDestroy/드롭 경로를 제거했다. 스폰·애니메이션·체력바에서 쓰는 상태/이벤트는 유지했고, 기존 GameDataSaveSystemSmokeTest가 참조하는 CalculateDropCount도 유지했다. 부유물 스폰과 자산은 아직 남지만 플라즈마 타격/드롭 공급원이 아니다.
- 실제 검증: Unity 6000.5.1f1 재컴파일 완료/failed=false/errors=[] 확인. 첫 MCP recompile 요청은 도메인 재로드 중 시간 초과됐으나 CLI 재접속 후 완료 상태를 확인했다. `MiningCombatSmoke.Run5A`는 Editor와 PlayMode에서 통과했다: 실제 CircleCast 벽면·모서리·음수 좌표, 비타일 Collider 제외, 접촉 위치, 최근접 연쇄/거리 밖 제외/중복 제외/후보 소진, 틱당 탄약 1, 4→2→1 감쇠 피해, 독립 HP/공유 정의 불변, 파괴 이벤트 1회, Collider 갱신 뒤 다음 셀 타격. 추가 Editor 검사에서 사방이 채워진 내부 셀의 선택과 Manual Composite 갱신도 통과했다.
- 실제 프리팹 검증: PlayMode에서 PlasmaGun.prefab을 복제하고 임시 데이터를 넣어 실제 Update를 호출했다. 차지 시작/취소/발사, 두 틱 탄약 소비→셀 파괴→기본 드롭, 실제 타일 접촉, 레이저 midpoint/끝점과 파티클 위치, 탄약 0에서 표시 중단을 확인했다. 키보드/마우스 입력 주입이나 사람의 화면 판독을 수행한 것은 아니다.

### 2026-09-15 — 5B 구현: 파괴 이벤트와 기본 자원 드롭

- 변경 파일: 신규 `Assets/Scripts/Stage/Map/MiningFragmentDrops.cs` 및 meta, `StageMap.cs`, `Assets/Scenes/Stage_a_1.unity`, `Assets/Scenes/Templates/EmptyStageTemplate.unity`. 이 계획 문서의 상태표/실행 순서/기록을 함께 갱신했다.
- 이벤트/생성: 기존 셀·월드 중심·정의 이벤트 계약을 재사용한다. StageMap의 셀 제거→이웃 갱신→Collider 갱신→이벤트 순서를 유지하고 Manual Composite일 때 GenerateGeometry도 수행한다. 드롭은 동일 GameObject의 전용 MiningFragmentDrops가 OnEnable/OnDisable에서 구독/해제한다. RequireComponent/DisallowMultipleComponent로 배선을 제한하며 정의의 DropResource/BaseDropAmount와 반경 0.15를 기존 FragmentParticleManager에 전달한다. 무기나 StageMap이 파편을 직접 생성하지 않는다.
- 연결/보존: Unity Editor API로 대표 Stage와 빈 템플릿에 구독자만 추가했다. 저장 후 PreviewScene으로 다시 읽어 Stage_a_1의 기존 52셀, 템플릿 0셀, 각 StageMap/드롭 구독자 1개, Missing Script 0개를 확인했다. 시작 전 존재하던 Stage_a_1 직렬화 변경, TMP 폰트 3개, 계획 변경을 보존했다. 전체 씬 변환·UI 재작성·업그레이드 이전·별도 꾸밈은 수행하지 않았다.
- 실제 PlayMode 검증: 대표 Stage의 임시 셀에서 0/음수/부분 피해의 드롭 없음, 셀 파괴당 기본 수량 3개, 제거 셀 재피해의 중복 없음, 구독 비활성화 중 미생성, 반복 재활성화 후 단일 구독을 확인했다. 생성 위치는 셀 안쪽이었다. 기존 FragmentParticleManager.Update→FragmentMagnetManager→PlayerInventoryController 경로로 총 6개를 회수했고 GetFirstResource 이벤트는 한 번 발생했다.
- 귀환/저장 검증: CompleteExploreSession(0) 호출 뒤 인벤토리+보관함 합계가 6개 증가한 상태로 유지되고, GameDataFileStore.TryLoad로 읽은 저장 데이터에서도 동일 자원 합계와 첫 획득 이벤트를 확인했다. 테스트는 세이브 본문/.bak/.tmp의 원본 바이트를 보관한 뒤 finally에서 복원했고 런타임 데이터도 복원했다. 이후 PlayMode를 종료했다. 실제 귀환 문 상호작용/Hub 장면 전환을 수행한 것은 아니다.
- 재진입: PlayMode 종료→재진입 후 프리팹 및 드롭 검증을 다시 통과했다. 마지막 Console Error/Exception/Assert는 0개였다. 기존 FragmentParticleManager/FragmentMagnetManager/인벤토리/저장 코드는 수정하지 않았다.

### 2026-09-15 — 재현 방법, 남은 검증과 인계

검증/설정 스크립트는 재현 가능하도록 `AgentScripts/`에 보존한다. Unity의 등록 Test Runner 테스트가 아니라 Pipeline run_script로 실행하는 SmokeTest이며 등록 테스트 건수/전체 테스트 실행 성공을 주장하지 않는다.

| 스크립트 / entry | 실행 조건과 결과 |
|---|---|
| MiningCombatSmoke.cs / MiningCombatSmoke.Run5A | Editor 또는 PlayMode. 독립 임시 맵/정의/무기를 생성하고 finally에서 제거. Editor/PlayMode 통과; 마지막 Manual Composite/완전 내부 셀 보강은 Editor에서 통과 |
| MiningGunPrefabSmoke.cs / MiningGunPrefabSmoke.Run | Stage_a_1 PlayMode, 생성된 파편이 없는 테스트 세션. 실제 프리팹 Update/표시/파괴/드롭 검증 통과 |
| MiningDropSmoke.cs / MiningDropSmoke.Run5B | Stage_a_1 PlayMode, 파편이 없는 테스트 세션. 구독/회수/귀환 저장 검증 통과. 테스트 중 세이브를 쓰고 finally에서 원본 복원 |
| MiningSceneValidation.cs / MiningSceneValidation.Run | Editor. 저장된 두 씬을 PreviewScene으로 읽고 배선/셀 수/Missing Script 검사 통과 |
| MiningDropSetup.cs / MiningDropSetup.Install | Editor. 두 씬에 구독자가 없을 때만 추가·저장. 이미 설정한 씬의 변환을 반복하지 않음 |

호출 형식: `unity command run_script --file AgentScripts/MiningCombatSmoke.cs --entry MiningCombatSmoke.Run5A --json` (다른 검증은 표의 파일/entry로 교체). PlayMode 진입 전 사용자 세션을 종료하고 테스트 환경에서 수행한다.

- 정적 확인: Assets 소스에서 IDamagable/AttackData/DamageSource 참조 0개. 기존 Editor 스크립트가 포함된 Unity 컴파일 성공. 최종 소스/문서 diff 검사와 meta/참조를 확인했다. 전체 git diff --check는 Unity가 새 컴포넌트에 직렬화한 두 씬의 `m_Name: ` 공백 2곳만 지적한다. 사용자 YAML을 광범위하게 정규화하지 않았으며 이 항목을 통과로 보고하지 않는다.
- 미검증: 실제 입력으로 출격→채굴→회수→귀환 문→Hub→재출격을 연속 플레이하는 화면 검증, 실제 업그레이드 구매 전후 발사 비교, 고밀도/큰 연쇄 반경의 Profiler 비용, 화면상 AutoTile 경계/레이저 감각, 플레이어 몸체로 제거 셀을 통과하는 수동 물리 확인. 프로그램 검증과 이를 혼동하지 않는다.
- 다음 단계: 완료 UI/단일 맵/5A·5B 계약을 보존하고 3B 안내 이벤트 연결 또는 요청된 6 업그레이드 이전을 진행한다. 6에서는 남아 있는 FloatageDropMultiplierRuntimeData 및 정의/저장 노드 의미를 채굴 정의로 이전한다. 7에서 부유물 스폰·프리팹·전용 표현/자산을 참조 조사 후 제거한다. 새 추상 타격 대상 계층이나 차폐 규칙을 임의로 추가하지 않는다.

### 2026-09-22 — 6 구현: 채굴 업그레이드 이전

- 수정 범위: `Assets/Scripts/Upgrades/Effects/MiningTileUpgrades/`의 Bonus/Multiplier 효과, `Core/SaveSystem/GameData/MiningTileDropMultiplierRuntimeData.cs`와 `GameRuntimeData.cs`, `Stage/Map/MiningFragmentDrops.cs`, `Data/GameDefinitionCatalog.cs`, `Editor/GameDefinitionCatalogEditor.cs`·`UpgradeNodeDefinitionEditor.cs`·`GameDataSaveSystemSmokeTest.cs`, `UI/Upgrades/UpgradeTooltipDataBuilder.cs`. 기존 FloatageUpgrades/런타임 배율 타입을 제거하고 FloatageController에서 수량 계산만 채굴 드롭 쪽으로 옮겼다.
- 자산: Editor API로 `upgrade_041_mining_tile_drop_1.asset`, `upgrade_042_mining_tile_drop_2.asset`로 이름과 효과 8개를 이전했다. GUID/ID/Key·비용·부모·최대 레벨·0.35/0.5 보너스를 보존했다. `Assets/Data/GameDefinitionCatalog.asset`에는 기존 4개 채굴 정의 목록만 추가했다. 씬/프리팹/채굴 정의/자원 정의는 수정하지 않았다.
- 표현: 툴팁은 동일한 현재/다음 배율을 묶고 서로 다른 배율은 별도 표시한다. 같은 노드의 같은 대상에 효과가 반복되면 배열 순서대로 계산한다. 대상 이름은 드롭 자원의 표시명을 사용한다.
- 검증: Unity 6000.5.1f1 재컴파일 failed=false/errors=[]; 마지막 Editor ready/비컴파일/PlayMode 종료, Console Error 0. `git diff --check` 통과. Assets의 옛 FloatageDrop 타입 참조 0개. 사용자 기존 TMP 폰트 3개와 기존 AgentScripts를 보존했다.
- Editor 전용 검증: 카탈로그 정상, 에셋 재import 후 8개 managed reference/ID/Key, 가산·곱셈 순서·동일 자원을 가진 서로 다른 정의의 독립성·반올림/상한·잘못된 값 거부·툴팁 동일/상이/반복 대상 표시 통과.
- PlayMode 구매 검증: 실제 UpgradeService로 041 노드를 0→1→2 구매하고 1→1.35→1.7 배율을 확인했다. 기존 ID로 저장 재로드 후 레벨 2/배율 1.7, 반복 재로드의 중복 적용 없음 확인. 첫 실행은 테스트가 2레벨 비용의 철을 준비하지 않아 실패했으며 테스트 준비만 보완한 후 통과했다.
- PlayMode 드롭 검증: 대표 Stage 임시 정의의 기본 수량 3에 보너스 0.5를 적용해 셀당 4개 생성, 부분/0/음수/재피해 중복 없음, 구독 해제·재등록, 생성 위치, 기존 자석/인벤토리로 총 8개 회수, 최초 자원 이벤트 1회, 귀환 처리와 저장 파일 재로드의 8개 증가를 확인했다. 테스트는 finally에서 임시 셀/정의/파편과 런타임/세이브 본문·bak·tmp를 복원했다.
- 별도 실패: 기존 전체 `GameDataSaveSystemSmokeTest.Run`은 채굴 검사 전에 기본 생물 중첩 수 기대값에서 중단된다. 테스트 기대값 10, 현재 `GameDataDefaults.asset` 값 4이며 이번 변경 전부터의 불일치다. 무관한 기본값이나 테스트 기대값은 수정하지 않았다. 따라서 전체 저장 SmokeTest 성공을 주장하지 않는다.
- 미검증: 사람이 실제 UI로 구매/발사하는 화면 확인, 실제 귀환 문/Hub 전환, 전체 플레이 동선·성능. 8단계에서 확인한다. 이번 검증은 Pipeline run_script SmokeTest이며 등록 Test Runner 전체 통과를 의미하지 않는다.
- 재현: `AgentScripts/MiningUpgradeSmoke.cs`의 `RunEditor`(Editor), `RunPurchase`(Stage_a_1 PlayMode), `AgentScripts/MiningUpgradeDropSmoke.cs`의 `Run6`(Stage_a_1 PlayMode, 기존 파편이 없는 테스트 세션). 호출은 `unity command run_script --file <파일> --entry <타입.메서드> --json`. `MigrateMiningUpgrades.cs / Run`은 일회성 Editor 이전 기록이며 기존 대상은 이미 이전되어 재실행 시 효과 0개다.
- 인계: 다음은 7단계 부유물 전체 제거다. 현재 부유물 스폰·정의·프리팹·체력바·표현·관련 Editor/Stage 참조는 의도적으로 남아 있다. 새 채굴 정의 제작, 동적 맵 생성, 셀 상태 영구 저장은 추가하지 않는다.
