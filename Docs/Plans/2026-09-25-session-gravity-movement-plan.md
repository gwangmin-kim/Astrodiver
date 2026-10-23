# 스테이지 중력 및 SessionPlayer 이동 전환 실행 계획

작성: 2026-09-25. 상태: 구현 착수용 계획, 구현 미실행.

## 1. 문서 목적과 적용 범위

이 문서만 읽고 작은 작업 단위로 구현할 수 있도록 동작 계약, 변경 위치, 순서, 검증을 정리한다. 사용자는 문서화를 요청했으며, 이 문서 작성 자체가 코드 구현 완료나 구현 착수 지시는 아니다. 후속 작업에서 이 계획의 구현을 요청받으면 아래 순서대로 진행한다.

사용자가 확정한 요구:

- HubPlayer는 그대로 유지한다.
- SessionPlayer를 높은 관성의 상하좌우 부유 이동에서 낮은 관성의 좌우 이동과 지속 상승으로 변경한다.
- 스테이지 정의에 중력 스케일을 저장하고 플레이어 입장 시 Rigidbody2D에 적용한다.
- 상승은 단발성 impulse가 아니라 누르는 동안 지속되며, 중력이 달라도 상승 조작감이 유지되도록 속도를 제어한다.

이 문서가 제안하는 실행 기본값은 §3에 명시한다. 사용자 확정 사항과 구분하되, 구현 요청 시 별도 변경 지시가 없다면 그 기본값으로 착수할 수 있다. 수치 조정은 가능하지만 연료·새 업그레이드 같은 기능을 임의로 추가하지 않는다.

관련 문서: [스테이지 생성기 논의 초안](2026-09-25-stage-generator-discussion.md). 생성기 구현은 이번 이동 전환에 포함하지 않는다. 과거 `2026-09-10-demo-revision-plan.md`의 무중력/동적 생성 제외 범위는 당시 마이그레이션의 범위다. 이번 요구와 충돌하는 과거 범위는 새 요청에 한해 대체하며, 기존 채굴·드롭 소유권은 유지한다.

## 2. 확인한 현재 구조

경로는 저장소 루트 `C:\Unity\Astrodiver` 기준이다. 2026-09-25 소스/직렬화 자산을 확인했다. Unity 컴파일 및 PlayMode는 실행하지 않았다.

| 파일 | 현재 상태 / 구현 시 주의점 |
|---|---|
| `Assets/Scripts/Player/Actions/PlayerMovementController.cs` | Update에서 Vector2.SmoothDamp 후 linearVelocity 전체를 덮어쓴다. FixedUpdate에는 월드 경계 보정이 있다. 같은 파일에 PlayerMovementData가 선언된다. |
| `Assets/Scripts/Player/PlayerInputHandler.cs` | MoveInput과 버퍼형 DashInput/ConsumeDashInput이 있다. InteractHeld처럼 직접 누름 상태를 제공하는 선례가 있다. |
| `Assets/Scripts/Player/Hub/HubPlayerMovementController.cs` | 별도 HubPlayerMovementData를 사용하고 DashInput을 단발 점프로 소비한다. 수정 대상 아님. |
| `Assets/Prefabs/Player/SessionPlayer.prefab` | gravityScale=0, linearDamping=0, 회전 고정. |
| `Assets/Scripts/Stage/StageDefinition.cs` | 정의 및 StageRuntimeConfig가 한 파일에 있다. 현재 중력 설정 없음. |
| `Assets/Scripts/Core/ExploreSession/SessionManager.cs` | 실행 순서 -1000, Awake에서 플레이어 생성/기존 PlayerContext 재사용, Start에서 BeginExploreSession 호출. |
| `Assets/Scripts/Stage/StagePopulationManager.cs` | Definition 프로퍼티를 제공하며, Start에서 생물 배치 및 자체 런타임 설정 생성. 이 초기화 결과를 기다려 중력을 설정하면 안 된다. |
| `Assets/Data/GameDataDefaults.asset` | 기본 이동 속도 3.5, 이동 감쇠 0.3초, 정지 감쇠 0.8초. |
| `Assets/Scripts/Core/SaveSystem/GameDataManager.cs` | GetMovement()는 PlayerMovementData 값 복사 반환. |
| `Assets/Scripts/Upgrades/Effects/NumericUpgrades/NumericUpgradeEffect.cs` | 이동 속도 비율 업그레이드가 런타임 movement 값을 갱신한다. |
| `Assets/Scripts/Player/PlayerLegAnimationController.cs` | 입력으로 자세를 교대한다. 지상/공중 상태를 구분하지 않는다. |
| `Assets/InputActions.inputactions` | Player/Dash에 Keyboard/space가 연결되어 있다. 주석에 적힌 게임패드 지원을 실제 바인딩 확인 없이 지원 완료로 간주하지 않는다. |

## 3. 구현할 동작 계약과 기본값

### 3.1 입력과 이동

- 좌우: 기존 MoveInput.x를 사용한다. 세션 이동에서 MoveInput.y는 무시한다. 조준·공격·포획 입력은 유지한다.
- 상승: 기존 Dash 액션의 누름 상태를 `DashHeld`로 노출하고 세션 이동에서 사용한다. 액션 이름 변경이나 Hub의 버퍼 소비 방식 변경은 하지 않는다.
- `DashHeld`는 InputEnabled=false일 때 반드시 false다. 액션 참조가 아직 없을 때도 false다.
- 무제한 지속 상승을 첫 구현 기본값으로 삼는다. 연료, 배터리 소모, 과열, 공중 횟수 제한은 추가하지 않는다.
- 공중에서도 상승을 시작할 수 있다. 지면 판정은 상승 허용 조건이 아니다.
- 상승 해제 시 Y 속도를 0으로 자르지 않는다. 남은 상승 속도가 중력으로 감소하고 낙하한다.
- 입력 비활성화는 추진 중단과 수평 감속을 의미한다. 일시정지/세션 종료의 정지는 기존 timeScale 정책을 유지한다.

### 3.2 파라미터

아래 수치는 사용자 지정값이 아닌 최초 튜닝값이다. 기존 기본 이동 속도와 이동 속도 비율의 의미는 유지한다.

| 값 | 초기값 | 소유 위치 |
|---|---:|---|
| stage gravityScale | 1.0 | StageDefinition, 0보다 큰 유한값 |
| moveDampingTime | 0.08초 | 기존 PlayerMovementData 필드 |
| stopDampingTime | 0.10초 | 기존 PlayerMovementData 필드 |
| 상승 목표 속도 | 4.0 unit/s | PlayerMovementData 고정 베이스 × 변동 비율 |
| 상승 속도 변화율 | 30 unit/s² | PlayerMovementData 고정 베이스 × 변동 비율 |
| 최대 낙하 속도 | 12 unit/s | 동일 컴포넌트, 양수 |

상승 목표 속도와 추진 가속도는 각각 고정 베이스와 변동 비율을 PlayerMovementData에 저장한다. NumericUpgradeEffect 업그레이드는 베이스가 아닌 비율만 변경한다. 수평 이동 속도 업그레이드는 수평에만 적용한다. StageRuntimeConfig에는 읽기 전용 GravityScale을 전달해 정의와 런타임 스냅샷을 일치시킨다. 기존 생성자 호출부를 검색하고 함께 갱신한다.

중력 0, 역중력, 중력 방향 회전, 같은 스테이지 안의 국소 중력 구역은 이번 지원 범위 밖이다. 잘못된 중력값은 TryValidate에서 오류로 보고하며 정상값으로 조용히 숨기지 않는다.

### 3.3 물리 처리

Update에서는 입력을 읽고, Rigidbody 속도 변경은 FixedUpdate에서 처리한다. 수평 속도와 수직 속도를 분리한다. 이전 프레임의 자체 Vector2 속도를 기준으로 충돌 전 속도를 복원하지 말고 실제 Rigidbody2D.linearVelocity를 읽는다.

수직 속도 제어의 구현 기준:

1. `dt = Time.fixedDeltaTime`, `g = Physics2D.gravity.y * body.gravityScale`.
2. 추진 중 순 가속도는 `riseAcceleration + g`다. 중력이 강해질수록 상승 가속도가 낮아지며, 추진 가속도가 중력보다 작으면 상승하지 못할 수 있다.
3. 다음 물리 단계 목표 속도는 `min(vy + (riseAcceleration + g) * dt, riseSpeed)`로 계산한다. Rigidbody 적분에서 중력이 다시 더해지므로 실제로 설정할 속도는 `desiredNextVy - g * dt`로 둔다. 이 보정은 최고 상승 속도 제한에만 맞추며, 상한에 닿지 않은 구간에서 중력 효과를 상쇄하지 않는다.
4. 추진하지 않을 때는 자연 중력을 그대로 받는다. 다음 물리 단계에서 최대 낙하 속도를 넘을 때만 그 한계에 맞게 보정한다.
5. 위 방식은 현재 수직 중력, linearDamping=0 전제다. 실제 Unity 물리 스텝 이후의 속도를 테스트해 보정의 정확성을 확인한다. 충돌 시에는 솔버의 결과가 우선이다.
6. 기존 월드 경계 보정을 보존하고 충돌한 축의 감쇠 캐시를 초기화한다. 경계 보정 뒤 같은 스텝에서 다시 바깥 방향 속도를 쓰지 않도록 처리 순서를 정한다.

추진 중 중력을 끄지 않는다. Rigidbody.gravityScale은 스테이지 값으로 유지하고 Physics2D.gravity 전역 설정은 변경하지 않는다. 같은 시작 속도와 입력에서도 중력이 강할수록 상승 가속도와 최고 속도 도달 양상이 달라야 한다. 서로 다른 낙하 속도에서 추진을 시작한 경우 상승 속도에 도달하는 시간이 다른 것은 정상이다.

### 3.4 중력 적용과 참조 소유권

이번에는 생성기용 초기화 프레임워크를 먼저 만들지 않는다.

- SessionManager에 같은 씬의 StagePopulationManager를 직렬화 참조로 연결한다. 이 컴포넌트의 Definition을 중력 원본으로 사용한다. 이미 존재하는 정의 참조를 재사용하고 중복 StageDefinition 필드를 두지 않는다.
- StagePopulationManager.Start 또는 RuntimeConfig 생성 완료에 의존하지 않는다. Definition의 직렬화 데이터는 Awake에서도 읽을 수 있다.
- SessionManager에서 SpawnPlayer 직후, 최초 물리 스텝 전에 이동 컴포넌트의 명시적인 초기화 메서드를 호출해 중력을 적용한다.
- PlayerContext를 재사용하는 경로에도 동일하게 적용한다. 재시도/재입장에서 이전 스테이지 값이 남지 않아야 한다.
- 필수 참조 누락 또는 잘못된 중력값은 구체적인 오류를 출력하고 중력 초기화 함수만 종료한다. 물리 시뮬레이션/이동 컴포넌트를 끄거나 값을 보정하지 않는다. 플레이어가 존재하면 중력 적용 성공 여부와 별개로 탐사 세션을 진행한다.
- 씬 참조는 Stage_a_1과 EmptyStageTemplate에 모두 연결한다. 전역 Find로 다른 씬의 정의를 선택하지 않는다.

## 4. 수정 범위

### 필수 변경

1. `Assets/Scripts/Stage/StageDefinition.cs`: 중력 필드/프로퍼티/검증, 런타임 설정 복사.
2. `Assets/Scripts/Core/ExploreSession/SessionManager.cs`: 정의 제공자 연결, 중력 초기화, 성공 여부에 따른 세션 시작 보호.
3. `Assets/Scripts/Player/PlayerInputHandler.cs`: DashHeld 추가. 기존 DashInput 의미 유지.
4. `Assets/Scripts/Player/Actions/PlayerMovementController.cs`: FixedUpdate 이동, 축 분리, 지속 상승, 낙하 제한, 초기화 API, PlayerMovementData 베이스/비율 필드.
5. `Assets/Scripts/Upgrades/Effects/NumericUpgrades/NumericUpgradeTarget.cs`, `NumericUpgradeEffect.cs`: 상승 속도/가속도 비율 업그레이드 대상과 미리보기/적용.
6. `Assets/Data/GameDataDefaults.asset`: 이동 기본값과 상승 베이스/비율.
7. `Assets/Prefabs/Player/SessionPlayer.prefab`: 물리 설정 직렬화. 기본 gravityScale도 1로 맞추되 실제 입장 설정이 권위값이다.
8. `Assets/Data/Definitions/Stages/stage_000_a_1.asset`: 중력값 명시.
9. `Assets/Scenes/Stage_a_1.unity`, `Assets/Scenes/Templates/EmptyStageTemplate.unity`: 참조 연결 및 입장 위치의 충돌 안전성 확인.

### 필요한 경우에만 변경

- `PlayerMovementControllerEditor.cs`: 새 필드는 DrawPropertiesExcluding으로 자동 노출된다. 기존 bounds 핸들 유지. 불필요한 재작성 금지.
- 이동 데이터 초기화/복사 코드: 감쇠값이 실제 런타임에 반영되는지 확인하고 필요한 경로만 갱신한다. 저장 포맷 전체 개편 금지.
- `StageTemplateSceneUtility.cs`: 템플릿 생성 기능이 새 필수 참조를 누락하면 보완한다.
- 다리 애니메이션: 공유 기존 동작을 기본으로 유지하고 SessionPlayer에서만 접지/공중 상태 사용을 선택하도록 한다. 공중은 Fly, 접지 정지는 Stand, 접지 이동은 기존 교대 표현. 접지 판정은 콜라이더 접촉 법선 기반으로 벽/천장을 제외한다. 새 애니메이션 자산 제작은 하지 않는다.
- `PlayerVisualTiltController.cs`: 실제 확인에서 지상 이동 시 과도한 기울기가 있을 때만 세션 설정을 조정한다.
- 입력 안내 문구: 기존 부유 이동 안내가 실제로 존재하면 좌우+Space 유지 상승으로 갱신한다. UI 구조 개편은 하지 않는다.

### 제외

HubPlayer 프리팹/이동 구현, 생성기, 생물 물리·AI, 새 타일 정의, 광맥 배치, 자원 드롭 규칙, 연료 UI, 배경 아트 교체, 영구 지형 저장, 낙하 피해.

## 5. 실행 순서

### A. 시작 점검

- git status와 적용 가능한 AGENTS.md를 확인한다. 기록 당시 폰트 SDF 3개 변경과 미추적 AgentScripts/가 있었다. 사용자의 기존 변경은 보존한다.
- 위 필수 파일과 StageRuntimeConfig 생성자 호출부를 다시 확인한다. 계획 작성 이후 변경이 있으면 현재 코드를 기준으로 좁게 조정한다.
- Unity 조작/검증에는 프로젝트의 unity-cli 또는 unity-pipeline 스킬을 읽고 따른다. 단순 서버 실행을 Editor 준비 완료로 간주하지 않는다.

### B. 정의와 입장 초기화

- 중력 데이터/검증/런타임 복사를 구현한다.
- 초기화 API와 SessionManager 연결을 구현한다. 플레이어 신규 생성/재사용 경로 모두 처리한다.
- 자산/씬/템플릿 참조를 연결한다. 컴파일과 누락 참조를 확인한다.

### C. 입력과 물리 이동

- Player 맵의 `Dash` 액션을 `Jump`로 이름 변경한다. PlayerInputHandler의 기본 액션 이름과 사용처를 함께 갱신한다.
- Player 맵의 버튼 입력 액션(Interact, Attack, Capture, Jump, Cancel)을 `Button`으로 통일한다. `Move`/`Aim`처럼 연속 값 액션은 기존 Value로 둔다. UI 맵의 포인터/축/클릭 입력은 이 버튼 통일 범위에 포함하지 않는다.
- PlayerInputHandler에서 버튼별 입력 시작/종료 이벤트와 캐시된 `bool` 홀드 프로퍼티를 제공한다. 입력 비활성화 및 ResetInputState 때 홀드 상태를 정리하고, 비활성/누락된 액션의 홀드는 false가 되게 한다. Update의 WasPressedThisFrame 폴링 및 버퍼 타이머를 제거하고 액션 started/canceled 콜백으로 상태를 갱신한다.
- Interact의 Hold interaction과 `_inputBufferTime`, `_interactBufferTimer`, `_dashBufferTimer`, ConsumeInteractInput/ConsumeDashInput 의존을 제거한다. `PlayerInteractionController`는 Interact 시작 이벤트에서 즉시 상호작용을 시작하고 홀드 프로퍼티로 반복 실행을 유지한다. 기존 반복 대상/정지 정책은 보존한다.
- HubPlayer는 이름 변경된 Jump의 입력 시작 이벤트에서만 단발 점프를 시도한다. 이벤트 시점에 grounded/coyote 허용 조건을 확인하며 입력 버퍼링은 제거한다. 좌우 이동, 지면 판정, 코요테 시간, 점프 높이는 유지한다.
- Capture와 Attack은 Button 액션의 시작/종료 이벤트로 기존 발사·충전 동작을 구동하고, 홀드 프로퍼티는 실제 소비처를 확인해 제공한다. 기존 공개 구독자와 소비처를 검색해 API 변경을 함께 반영한다.
- 이동을 FixedUpdate로 이전하고 §3 동작을 구현한다.
- 매 물리 스텝 또는 런타임 데이터 변경 시 최신 GetMovement() 값을 반영한다. Awake에서 받은 struct 복사본만 계속 사용해 업그레이드가 무시되지 않도록 한다.
- 기본값과 프리팹을 갱신한다. 경계 처리와 캐시 리셋을 검토한다.

### D. 표현과 검증

- 기존 표현을 활용해 세션 지상/공중 상태를 연결한다.
- 아래 검증을 수행하고 실패한 항목을 해당 범위 안에서 수정한다.
- 결과를 이 문서 말미에 추가한다. 실제로 실행하지 않은 검증은 미실행으로 표기한다.

## 6. 완료 기준과 검증

| 검증 | 합격 기준 |
|---|---|
| 컴파일/자산 | 컴파일 오류 0, 변경한 씬·프리팹의 Missing Script/필수 참조 누락 0 |
| 중력별 추진 | gravityScale 0.5/1/2, 동일 초기 속도와 입력으로 충돌 없는 구간 테스트. 상승 정상 속도 4.0 ±0.1 unit/s, 상승 속도 곡선 차이 0.1 unit/s 이내 |
| 해제/낙하 | 해제 순간 속도 강제 0 없음, 높은 중력에서 더 빠르게 낙하 가속, 물리 스텝 후 낙하 속도 12.1 unit/s 초과 없음 |
| 좌우 | 입력 해제 후 빠르게 정지, 대각선 입력이 수평 최고 속도를 의도치 않게 바꾸는지 확인. 필요 시 수평 축 입력 처리에서 보완 |
| 충돌 | 지면 착지, 벽을 누른 채 상승, 천장 접촉 후 이동, 모서리 통과, 월드 경계에서 누적 속도/튕김/관통 없음 |
| 입력 수명 | Space 유지/해제, 입력 비활성화, Pause/복귀, 세션 종료 시 추진이 남지 않음 |
| 재입장 | 새 생성/기존 PlayerContext 경로/재시도에서 중력 적용 확인 |
| Hub 회귀 | 기존 좌우/단발 점프 및 입력 소비 그대로 동작 |
| 업그레이드 | 이동 속도 비율은 수평 속도에 적용되고, 상승 속도/가속도 업그레이드는 각자의 비율만 갱신해 실제 상승 동작에 반영됨 |
| 기존 탐사 | 채굴·드롭·수집·포획·귀환 저장을 대상으로 좁은 회귀 확인 |

자동 검증은 입력 유지와 중력별 실제 물리 결과를 다루는 타깃 PlayMode smoke 중심으로 작성한다. 숫자를 되읽는 테스트만으로 합격 처리하지 않는다. 테스트용 중력 변경은 종료 시 복구하고, 사용자의 플레이 데이터/자산을 남겨 바꾸지 않는다. 조작감과 화면 표현은 수동 검증 결과를 별도로 남긴다.

## 7. 후속 모델용 작업 지시 예시

> Docs/Plans/2026-09-25-session-gravity-movement-plan.md를 읽고 이 계획의 스테이지 중력과 SessionPlayer 이동 전환을 구현해줘. §3의 실행 기본값을 사용하고, A→B→C→D 순서로 진행해줘. HubPlayer 및 스테이지 생성기는 변경하지 마. 기존 작업을 보존하고, 검증 결과와 미실행 항목을 문서 말미에 기록해줘.

## 8. 실행 기록

- 2026-09-25: 계획 문서 작성. 코드·씬·프리팹 수정 및 Unity 실행 검증 없음.
- 2026-09-25: 사용자 요청에 따라 A 시작 점검과 B(정의/입장 초기화)까지 구현. `StageDefinition`에 기본 중력 1.0과 유한 양수 검증을 추가하고 `StageRuntimeConfig.GravityScale`로 복사했다. `SessionManager`는 같은 씬의 직렬화 `StagePopulationManager.Definition`을 통해 신규/재사용 플레이어 모두 최초 물리 스텝 전에 이동 컴포넌트 초기화를 시도한다. 초기 구현에서는 참조·정의·컴포넌트 검증 실패 시 Rigidbody 시뮬레이션과 이동을 중단하고 세션 시작도 막았으나, 2026-09-26 수정에서 이 정책을 제거했다. Stage_a_1과 EmptyStageTemplate의 SessionManager 참조를 연결하고 Stage_a_1 정의에 중력 1.0을 명시했다. 생성자 호출부(smoke test 포함) 검색 결과 StageRuntimeConfig 직접 생성 호출은 코드상 1곳이며 갱신했다.
- B 정적 확인: `git diff --check` 통과. 정의 필드/프로퍼티/런타임 복사와 두 씬의 직렬화 참조를 텍스트로 확인. Unity 컴파일, 씬/프리팹 Missing Reference 검사, PlayMode 재입장 물리 확인은 실행하지 않음.
- 미실행/후속: B 완료로 중단했으므로 C 입력·FixedUpdate 이동·프리팹/이동 기본값 갱신과 D 표현/행동 검증은 미실행. EmptyStageTemplate은 StagePopulationManager의 `_definition`이 비어 있으므로 현재 상태에서 직접 실행하면 SessionManager가 중력 초기화를 거부하고 세션을 시작하지 않는다. 생성기 변경은 요청 범위 밖이며, 생성된 씬의 정의 설정은 템플릿/생성기 작업에서 별도로 해결해야 한다.
- 2026-09-25: C 세부 조정 반영을 위해 InputActions와 입력 소비처를 점검했다. Player 맵은 Attack만 PassThrough였고 Interact/Dash/Capture/Cancel은 Button, Move/Aim은 Value였다. Dash는 Jump로 개명하고 Player 버튼 입력은 Button으로 통일하되 UI 맵의 포인터/축/클릭 액션은 성격이 달라 기존 타입을 유지하도록 최종 C 계획에 반영했다. 버튼 입력은 시작/종료 이벤트와 캐시된 홀드 프로퍼티로 통일하며 버퍼와 소비 API를 제거한다. HubPlayer는 Jump 시작 이벤트 순간에만 기존 grounded/coyote 조건으로 단발 점프를 시도한다. PlayerInteractionController의 소비/홀드 사용도 새 이벤트 구조로 이관한다. 이번 점검에서는 코드와 인풋 에셋을 수정하지 않았고 C 구현 및 검증은 미실행이다.
- 2026-09-25: 사용자 승인으로 C 구현. InputActions Player 맵에서 Dash를 Jump로 개명하고 바인딩을 함께 갱신했으며 모든 버튼 액션을 Button으로 맞추고 Interact의 Hold interaction을 제거했다. UI 맵의 포인터/축/클릭 PassThrough는 유지했다. PlayerInputHandler를 시작/종료 이벤트와 캐시 bool 홀드 상태로 전환하고 버퍼 및 Consume API를 제거했다. PlayerInteractionController, PlayerAttackController, PauseMenuController를 새 이벤트로 이관했다. HubPlayer는 Jump 시작 이벤트에서 grounded/coyote 조건을 즉시 확인해 단발 점프한다. SessionPlayer는 좌우 입력과 상승 입력을 분리하고 FixedUpdate에서 실제 linearVelocity와 중력 보정식으로 상승을 유지하며 자연 낙하/최대 낙하 속도를 처리한다. 이동 데이터는 매 프레임/물리 스텝에 새로 읽는다. 경계 위치 보정을 속도 계산 전후에 호출해 이미 경계 밖인 경우 재차 바깥 속도가 쓰이지 않도록 했다. 감쇠 기본값은 0.08/0.10초, SessionPlayer 튜닝값은 상승 속도 4, 가속 30, 최대 낙하 속도 12, 기본 gravityScale 1이다. HubPlayer 이동 로직은 이벤트 전환 외에는 변경하지 않았다.
- C 검증: InputActions JSON 파싱 성공, Player 맵 액션 타입 및 이름 정적 확인, 구버전 Dash/소비 API 참조 0건 확인, `git diff --check` 통과, Unity 6000.5.1f1 재컴파일 성공(오류 0), Editor 준비/비컴파일 및 콘솔 오류 0 확인. PlayMode 테스트 목록 조회는 Unity MCP 연결 reset으로 실패했다. 실제 입력 유지/해제, gravityScale 0.5/1/2 물리 곡선, 낙하 제한, 충돌/경계, Hub 점프, Pause/입력 비활성화, 업그레이드/탐사 회귀는 실행하지 않았다. 해당 동작용 기존 타깃 PlayMode smoke가 확인되지 않아 새 테스트는 추가하지 않았다.
- 미실행/후속: D의 접지/공중 애니메이션 표현과 문서의 전체 PlayMode 및 수동 행동 검증은 미실행이다. B에서 기록한 EmptyStageTemplate의 미할당 StageDefinition 및 생성된 씬 설정 문제도 생성기 범위 제외로 미해결이다.
- 2026-09-25: 허브 빠른 연속 점프 문제 수정. HubPlayer 점프 허용 상태를 점프 실행 전에 즉시 false로 소비하고, 지면 접촉(normal.y 임계 초과)에서만 다시 true로 한다. 코요테 타이머, Update 기반 코요테 갱신, 충돌 종료 유예 처리를 제거하고 프리팹의 `_coyoteTime` 직렬화 필드를 삭제했다. 점프 수직 속도는 매 허용 점프마다 동일한 중력/점프 높이 공식으로 설정한다. 재컴파일 성공, Editor 준비/비컴파일, 콘솔 오류 0, `git diff --check` 통과. 빠른 입력 PlayMode 재현은 실행하지 않았다. 이 세션 중 HubPlayer 프리팹 diff에 콜라이더 offset/size 변경도 함께 나타났으며, 해당 값은 이번 수정에서 편집하지 않아 보존했다.
- 2026-09-26: 사용자 지시에 따라 중력 초기화 오류 정책을 단순화했다. SessionManager에서 실패 시 시뮬레이션/이동을 비활성화하던 `DisablePlayerSimulation`을 제거하고, 플레이어 생성 성공과 중력 적용 성공을 분리했다. StagePopulationManager/Definition/PlayerMovementController 참조 누락 시 구체적인 오류를 남기고 중력 초기화 함수만 반환한다. 플레이어가 있으면 `BeginExploreSession`은 계속 진행한다. StageDefinition 전체 `TryValidate`를 초기화에서 호출하지 않아 무관한 생물 설정 오류가 중력 적용을 막지 않게 했다. `PlayerMovementController.InitializeForStage`는 중력값과 Rigidbody/입력 참조 및 linearDamping을 확인하며 오류 시 값을 변경하거나 시뮬레이션을 끄지 않고 로그 후 반환한다. 유효한 중력만 Rigidbody에 적용하고 `simulated`/enabled 상태는 건드리지 않는다. 검증 결과는 후속 실행 기록에 추가한다.
- 오류 정책 변경 검증: Unity 재컴파일 성공(오류 0), 콘솔 오류 조회 0, `git diff --check` 통과. 정적 검색으로 `DisablePlayerSimulation`, `simulated=false`, 이동 컴포넌트 강제 비활성화, 초기화 성공 가드 및 중력 초기화 내 StageDefinition 전체 검증 호출이 제거된 것을 확인했다. Editor 상태 재조회는 Unity MCP 연결 reset으로 실패했다. 잘못된 참조/중력값을 주입하는 PlayMode 확인은 실행하지 않았다.
- 2026-09-27: 사용자 승인으로 SessionPlayer 상승 가속도 모델 수정. 추진 중 목표 다음 속도를 `min(vy + (riseAcceleration + gravityAcceleration) * dt, riseSpeed)`로 계산하고 Rigidbody 적분의 중력 중복 적용만 제거한다. 따라서 속도 상한에 이르기 전에는 중력과 추진 가속도가 합산되어 중력이 강할수록 상승이 어려워지며 최고 상승 속도 제한은 유지된다. §3.3 공식을 이 모델에 맞게 갱신했다. `git diff --check` 및 변경 식 정적 검토 통과. 이번 세션에는 Unity 재컴파일 도구가 제공되지 않아 컴파일 및 중력별 PlayMode 비교는 미실행이다.
- 2026-09-27: 상승 최고 속도와 추진 가속도를 PlayerMovementData DTO로 이전했다. 각 값은 `baseRiseSpeed × riseSpeedRatio`, `baseRiseAcceleration × riseAccelerationRatio`로 계산하며 기본 자산은 4×1, 30×1이다. SessionPlayer 이동 컴포넌트의 상승 속도/가속도 직렬화 필드를 제거하고 FixedUpdate에서 최신 PlayerMovementData를 읽도록 했다. NumericUpgradeTarget에 RiseSpeedRatio(1001), RiseAccelerationRatio(1002)를 추가하고 NumericUpgradeEffect의 미리보기 및 적용 경로에서 해당 변동 비율만 조정하도록 연결했다. §3와 필수 변경 파일 및 업그레이드 완료 기준을 갱신했다. 정적 검색과 `git diff --check` 실행; Unity 컴파일/upgrade smoke 검증은 이 세션에 Unity compile 도구가 노출되지 않아 미실행이다. 실제 업그레이드 노드 자산은 레벨별 수치·비용·표시 기획값이 지정되지 않아 만들지 않았다.
- DTO/업그레이드 검증: `dotnet build Assembly-CSharp.csproj --no-restore` 성공(오류 0, 경고 1). 경고는 기존 `SessionAmmoSupplementHUD.NetAmmoStack._iconSpacing` 미사용 경고다. `git diff --check` 통과. NumericUpgradeEffect의 미리보기/현재값/적용 경로와 기본 DTO 값을 정적으로 확인했다. 업그레이드 적용 후 실제 상승 동작 및 세이브 재구성은 런타임 검증하지 않았다.
- SessionManager `SpawnPlayer` 정리: 반환형을 `PlayerContext`로 바꾸고 `Awake`에서 반환값을 `SpawnedPlayer`에 저장한다. 이미 PlayerContext 인스턴스가 있으면 경고를 남기고 그 인스턴스를 반환한다. 새 생성 경로는 필수 참조 및 프리팹의 PlayerContext를 생성 전에 확인한 후 플레이어를 생성한다. 중력 초기화는 `Awake`에서 플레이어를 받은 직후 수행한다. 중력 정의 획득 방식은 이번 변경 범위에서 유지했다. `dotnet build Assembly-CSharp.csproj --no-restore` 성공(경고 0, 오류 0), `git diff --check` 통과. 실제 씬 진입/재사용 PlayMode 확인은 실행하지 않았다.
- 2026-09-27: `StageSceneSettings`를 스테이지 정의의 단일 씬 소유자로 추가했다. Stage_a_1의 정의 에셋 참조와 EmptyStageTemplate의 미할당 정의 필드를 이 컴포넌트로 옮기고, 두 씬의 `SessionManager` 및 `StagePopulationManager`가 이를 참조하도록 연결했다. `SessionManager`는 이제 `StagePopulationManager` 없이 중력 정의를 읽는다. `SpaceBackgroundController`도 같은 씬의 `StageSceneSettings`에서 정의의 배경 프로필을 읽도록 변경했으며 기존 `SpaceBackgroundSceneSettings` 우선순위를 유지했다. 템플릿의 정의는 의도적으로 미할당 상태이며, 새 스테이지 생성 후 설정이 필요하다. `dotnet build Assembly-CSharp.csproj --no-restore` 성공(경고 0, 오류 0), 변경 파일 `git diff --check` 통과, 두 씬의 참조와 정의 소유 위치를 정적으로 확인했다. Unity 씬 로딩, Missing Reference 검사 및 PlayMode 중력·개체 생성·배경 동작 검증은 실행하지 않았다.
