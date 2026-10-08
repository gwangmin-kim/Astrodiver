# 스테이지 마스터 생성기와 스폰 규칙 분리 계획

작성: 2026-10-01. 상태: 구현 전 계획. [이전 스폰 보장 계획](2026-10-01-spawn-clearing-and-floor-plan.md)의 단일 컴포넌트 설계를 이 문서로 대체한다.

## 1. 확정된 목표

- Unity 씬에는 **마스터 `StageGenerator` MonoBehaviour 한 개**를 둔다. 이 컴포넌트가 씬 참조, Inspector 설정, 시드, 셀 생성 범위, 규칙 실행 순서, 최종 타일 적용을 소유한다.
- 펄린 노이즈 기반 지형 계산과 우주선 귀환 영역 보장은 서로 다른 일반 C# 규칙 클래스로 분리한다. 하위 규칙은 씬에서 직접 `Awake`/`Start`를 실행하지 않고 Tilemap도 수정하지 않는다.
- `ReturnArea` 트리거와 겹치는 셀은 항상 빈 공간이다. 그 아래 바닥은 켜고 끌 수 있다. 켠 경우 바닥의 **가로 너비(셀)**와 **두께(셀)**를 트리거와 독립적으로 설정한다. 바닥은 생성 직후만 보장하며 기존 채굴로 파괴할 수 있다.
- 필수 영역 바깥에서는 보장 효과가 점차 줄고 기존 다층 노이즈 결과가 돌아온다. 시작 위치 보정, 연결성 보장, 시드 재시도, 생물 배치 정책은 이번 범위에 넣지 않는다.

## 2. 모듈과 데이터 소유권

| 위치/클래스 | 종류 | 책임 |
|---|---|---|
| `Assets/Scripts/Stage/Generation/StageGenerator.cs` | 유일한 생성용 MonoBehaviour | `StageMap`, `WorldBounds2D`, 우주선 `ReturnArea` 콜라이더, 타일 정의, 모든 노이즈·스폰 설정과 시드 보유. 범위 계산, 규칙 순서, 최종 `TileBase[]` 작성, `StageMap.ApplyGeneratedTiles` 1회 호출. |
| `Assets/Scripts/Stage/Generation/PerlinTerrainRule.cs` | 일반 C# 클래스 | 기존 기준층 + 넓은 굴곡층 + 세부 윤곽층으로 셀마다 `0..1`의 지형 필드를 작성. 기존 시드의 기준층 오프셋 추출 순서를 유지. 씬/Tilemap에는 접근하지 않음. |
| `Assets/Scripts/Stage/Generation/SpawnGuaranteeRule.cs` | 일반 C# 클래스 | 마스터가 전달한 귀환 트리거 셀 영역, 바닥 설정과 원래 지형 필드를 사용해 필수 빈 공간·선택적 바닥·전환부의 암반 여부를 덮어씀. 씬/Tilemap에는 접근하지 않음. |
| `Assets/Scripts/Stage/Generation/StageGenerationBuffer.cs` | 일반 C# 데이터 클래스 | 생성 `BoundsInt`, 셀 인덱스 변환, 원본 `float[]` 필드와 최종 `bool[]` 암반 상태. 규칙 간 데이터를 전달하고 범위 밖 쓰기를 막는 데 사용. |
| `Assets/Scripts/Stage/Map/StageMap.cs` | 기존 MonoBehaviour | 완성된 타일 배열을 단일 충돌 Tilemap에 적용하고 채굴 HP/AutoTile/콜라이더를 갱신. 규칙이나 설정을 소유하지 않음. |

설정용 직렬화 자료형(`NoiseLayerSettings`, `SpawnGuaranteeSettings` 등)은 마스터 파일 안에 두어도 된다. **직렬화 필드는 마스터에만** 존재하며 규칙 객체는 생성 시 검증된 값의 스냅샷을 받는다. 규칙이 마스터 컴포넌트나 ScriptableObject를 다시 참조하지 않도록 한다. 세 번째 규칙의 요구가 생기기 전에는 범용 규칙 등록 목록과 실행 순서 Inspector를 도입하지 않는다. 마스터가 명시적으로 두 규칙을 순서대로 호출한다.

## 3. 생성 파이프라인

1. `StageGenerator.Awake`에서 기존처럼 `StageMap`을 확인하고 `WorldBounds2D`의 포함 셀에 사방 한 셀을 더한 `BoundsInt`를 계산한다. `ReturnArea`의 활성 `BoxCollider2D`와 설정값도 검사한다. `DefaultExecutionOrder(-1100)`을 유지해 `SessionManager.Awake(-1000)`의 플레이어 생성 전 타일·콜라이더 처리를 끝낸다.
2. 마스터가 고정/임의 시드를 하나 선택하고 버퍼를 만든다. `PerlinTerrainRule`이 기존 세 노이즈 층의 합성값을 버퍼의 필드에 기록한다. 기준층의 두 오프셋을 먼저 뽑아 기존 고정 시드 결과를 보존한다.
3. 마스터가 기존 밀도 규칙(`density=0`은 빈 셀, `density=1`은 암반, 그 외 `field < density`)으로 `bool[]` 암반 상태를 만든다.
4. `SpawnGuaranteeRule`이 귀환 트리거 셀을 강제로 비운다. 바닥 보장이 켜져 있으면 지정한 바닥 셀을 강제로 채운다. 각 보장 영역 밖의 전환부는 원본 필드에 적용하는 임계값을 거리로 보간해 최종 암반 상태를 갱신한다. 전환부 밖의 `bool[]`은 건드리지 않는다.
5. 마스터가 암반 셀을 기존 `_terrainTile`, 빈 셀을 `null`로 변환하고 `StageMap.ApplyGeneratedTiles`를 한 번 호출한다. 생성 중 채굴 파괴 이벤트는 발생하지 않는다.

바닥을 끄면 바닥 강제 채움과 바닥 전환부를 모두 생략한다. 트리거 빈 공간은 계속 보장한다. 빈 공간 전환부는 트리거 위와 좌우에만 적용하고, 바닥 시작 높이 아래로 퍼지지 않게 한다. 바닥 전환부는 바닥의 좌우와 아래쪽에서만 적용한다. 두 전환부가 같은 셀에 서로 다른 판정을 요구하지 않도록 Y 높이로 영역을 나눈다.

## 4. 귀환 영역과 바닥의 셀 계약

마스터가 우주선 자식 `ReturnArea`의 `BoxCollider2D`를 **명시적 직렬화 참조**로 가진다. 현재 프리팹의 트리거는 7×3, 오프셋 0이고 두 씬의 우주선은 원점이다. 1×1 Grid에서 `Stage_a_1`의 보장 빈 공간은 X `[-4,4)`, Y `[-2,2)`로 8열×4행이다. 월드 트리거와 면적이 겹치는 셀은 전부 빈 셀로 만든다. 트리거가 이동하면 월드 AABB를 다시 셀로 바꾸며, 첫 구현은 축 정렬된 트리거만 지원한다.

바닥이 켜진 경우:

```text
airMinX, airMaxExclusiveX, airMinY = 트리거와 겹치는 셀 영역
floorTopY = airMinY - 1                   // 바로 아래 첫 행
floorCenterX = (airMinX + airMaxExclusiveX) / 2
floorMinX = floor(floorCenterX - floorWidthCells / 2)
floorMaxExclusiveX = floorMinX + floorWidthCells
floorY = [airMinY - floorThicknessCells, airMinY)
```

위 식의 나눗셈은 실수 계산이다. 홀수 너비를 짝수 너비의 빈 공간 아래에 놓으면 정확한 중심 정렬이 불가능하므로 왼쪽 셀을 선택하는 고정 규칙을 쓴다. 바닥 상단은 전체 지정 너비에서 수평이다. 현재 트리거 아래에서 기존과 같은 너비를 원하면 `floorWidthCells=8`을 넣는다. 더 좁거나 넓은 바닥도 허용한다. `floorWidthCells`와 `floorThicknessCells`는 각각 1 이상이고, 필수 바닥 전체가 생성 범위 안에 있어야 한다.

트리거 하단이 Y=-1.5인 현재 씬에서는 마지막 빈 셀 행이 Y=-2, 바닥 첫 행이 Y=-3이다. 바닥 윗면은 Y=-2이며 트리거 하단과 0.5 유닛 떨어진다. 스폰 좌표에 바닥을 맞추는 별도 오프셋은 추가하지 않는다.

## 5. 전환부 계산과 Inspector 설정

필수 셀 영역은 타일 점유 여부를 직접 덮어써 시드·밀도와 무관하게 보장한다. 바깥의 셀 중심에서 필수 직사각형까지의 유클리드 거리 `d`를 구하고 `t = SmoothStep(0,1, Clamp01(d / transitionCells))`를 사용한다.

| 전환부 | 셀별 임계값 | 거리 0 → 전환 폭 끝 |
|---|---|---|
| 빈 공간 | `Lerp(0, density, t)` | 빈 공간 편향 → 기존 밀도 |
| 바닥(켜짐) | `Lerp(1, density, t)` | 암반 편향 → 기존 밀도 |

전환 폭 밖은 원래 노이즈 암반 상태와 정확히 같다. 임계값 0과 1은 각각 항상 빈 셀/항상 암반으로 판정한다. 전환부 모서리는 유클리드 거리를 써서 원형으로 물러난다. 노이즈 분포에 따라 실제 윤곽이 계단형으로 남을 수 있으므로 결과를 보며 폭을 조정한다.

| 마스터의 설정 | 의미 | 첫 값 제안 |
|---|---|---|
| 기존 `WorldBounds2D`, `StageMap`, `_terrainTile`, 노이즈 층/밀도/시드 설정 | 기존 생성 계약 | 씬 값 그대로 이관 |
| `_returnArea` | 보장 빈 공간의 위치와 크기 | 각 씬 우주선 자식 트리거 연결 |
| `_airTransitionCells` | 빈 공간 보장 효과가 사라지는 거리 | 3셀 |
| `_guaranteeFloor` | 바닥 강제 생성 여부 | 켬 |
| `_floorWidthCells` | 트리거와 독립적인 평평한 바닥 너비 | 8셀(현재 트리거 셀 너비와 같음) |
| `_floorThicknessCells` | 바닥의 보장 두께 | 2셀 |
| `_floorTransitionCells` | 바닥 보장 효과가 사라지는 거리 | 3셀, 바닥이 켜질 때만 사용 |

빈 공간의 필수 크기를 따로 조절하는 필드는 없다. 트리거 크기가 권위값이다. 바닥 전용 타일도 없다. 기존 `MiningTileDefinition`을 사용해 이후 채굴 가능하게 둔다. 바닥 보장을 꺼도 저장된 너비·두께·전환 폭은 유지해 다시 켰을 때 복원되며, 꺼진 동안 결과에는 영향을 주지 않는다.

## 6. 기존 컴포넌트와 씬 이관

현재 씬의 `PerlinStageGenerator`가 가진 `_horizontalScale`, `_verticalScale`, `_density`, `_broadLayer`, `_detailLayer`, `_useFixedSeed`, `_fixedSeed`, `_terrainTile`, `_worldBounds`를 보존해야 한다. `PerlinStageGenerator.cs`를 `StageGenerator.cs`로 옮기고 클래스명을 바꾸되 **`.meta` GUID를 유지**해 두 씬의 컴포넌트 참조를 보존하는 방식을 우선 사용한다. 새 규칙의 파일은 별도로 만든다. 새 직렬화 필드만 추가하고 두 씬의 기존 노이즈 값을 덮어쓰지 않는다. 코드에서 구 클래스명을 쓰는 곳을 검색해 갱신한다.

`Stage_a_1.unity`와 `EmptyStageTemplate.unity`의 생성기 컴포넌트에 각 씬의 `ReturnArea`를 연결하고 바닥 설정을 지정한다. 우주선 프리팹과 `SessionManager`의 스폰 참조는 변경하지 않는다. 이관 직후 Editor에서 컴포넌트 유형·기존 값·새 참조가 모두 정상인지 직접 확인한다. 기존 `StageMap.ApplyGeneratedTiles`와 채굴 이벤트 소유권은 유지한다.

## 7. 구현 순서와 완료 기준

1. 마스터와 버퍼를 분리하면서 **스폰 규칙을 적용하지 않은 결과가 기존 고정 시드와 셀 단위로 동일한지** 확인한다. 범위의 사방 한 셀 확장, 시드 오프셋 순서, 밀도 0/1 동작도 유지한다.
2. 트리거의 정수/비정수·음수 좌표를 셀로 바꾸고, 바닥 너비의 홀짝 및 바닥 켜짐/꺼짐을 확인한다. 잘못된 참조, 회전, 생성 범위 밖의 필수 셀은 설정 오류로 보고하되 지형 모양에 따라 재시도하지 않는다.
3. 같은 시드에서 빈 공간 전환부와 바닥 전환부 밖이 기존 지형과 같은지 확인한다. 밀도 0/1에서도 필수 빈 공간과 켜진 바닥이 유지돼야 한다.
4. 두 씬의 컴파일·직렬화·Tilemap/AutoTile/CompositeCollider를 확인한다. PlayMode에서는 귀환 트리거 전체가 비고 바닥이 평평하며, 채굴 후 바닥 타일이 재생성되지 않는지 확인한다.

이번 범위는 노이즈 지형에 **국소 보장 규칙을 조합하는 기반**까지다. 생성 준비 완료 신호, 생물 안전 배치, 필수 이동 경로, 복수 타일 재질, 저장 가능한 지형 변경은 별도 단계로 둔다.
