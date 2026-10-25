# CSV 텍스트 관리

## 기본 구조

문구의 편집 원본은 `Assets/Localization/CSV/`의 UTF-8 CSV이다.
`Common.csv`는 재사용하는 안내 문구를 관리한다. 기능별 문구는 별도 CSV와 테이블을 추가해 관리한다.
실행 중에는 CSV를 파싱하지 않고 Unity Localization의 String Table을 읽는다.
현재 기본 언어는 한국어(`ko`)이며 실제 번역과 언어 선택 UI는 아직 포함하지 않는다.

각 `Assets/Localization/Sources/*CsvSource.asset`가 CSV 하나와 대상 String Table Collection 하나를 연결한다.
CSV 파일 이름을 바꾸어도 이 매핑을 유지하면 된다. 하나의 CSV/Collection을 여러 Source에 중복 연결할 수 없다.

## 문구 편집 절차

1. CSV의 `ko` 셀을 수정하고 저장한다.
2. Unity 메뉴 `Astrodiver > Localization > Validate CSV`로 검증한다.
3. `Astrodiver > Localization > Synchronize CSV`로 모든 등록된 CSV를 함께 반영한다.
4. 문구를 사용하는 UI의 에디터 미리보기 또는 Play Mode에서 확인한다.

검증 성공만으로 테이블을 수정하지 않는다. 동기화 성공 후에는 CSV와 테이블의 항목이 일치한다.
Unity의 기본 CSV 가져오기나 테이블 직접 편집은 이 검증을 우회하므로 사용하지 않는다.
빌드 전에 CSV와 테이블의 키·ID·문구·Smart 설정·설명이 일치하는지 다시 검사하며, 불일치하면 빌드를 중단한다.

## 열과 ID

현재 헤더: `Key,Id,Comment,Smart,ko`.

- `Key`: 의미가 드러나는 고유 이름. 문구를 바꿀 때 키는 유지한다.
- `Id`: 기존 항목의 영구 식별자. 스프레드시트에서는 반드시 텍스트 형식으로 보존한다.
- `Comment`: 사용 맥락을 설명하는 번역자/개발자 메모.
- `Smart`: 일반 문구는 `false`, 변수 치환 문구는 `true`.
- `ko`: 한국어 원문. 셀 안에 실제 줄바꿈, 쉼표, 따옴표를 포함할 수 있다.

새 행은 Id를 비우거나 0으로 둔다. 동기화 성공 시 Unity 키 생성기로 발급한 ID가 CSV에 기록된다.
기존 키와 일치하는 빈 Id는 기존 ID를 유지한다. 키 이름을 바꾸려면 기존 Id를 반드시 유지한다.
키/ID 충돌이나 음수·과학적 표기 ID, 중복 헤더, 잘못된 열 수, 빈 번역, 알 수 없는 언어 열은 오류다.
글꼴, 글자 크기, 타이핑 속도, 컷 길이는 CSV에 넣지 않고 기존 UI/연출 설정에서 관리한다.

## 행 삭제와 일괄 반영

CSV에서 행을 지우면 대상 Collection의 모든 언어 테이블과 Shared Data에서도 삭제한다.
단, 해당 항목을 사용하는 직렬화된 LocalizedString 참조가 남아 있으면 동기화 전체를 중단하고 위치를 출력한다.
씬(비활성 오브젝트 포함), 프리팹, ScriptableObject, Timeline 서브에셋을 검사한다.
현재 열려 있는 씬은 저장되지 않은 현재 상태를 검사하며, 닫힌 씬은 읽기 전용 Preview Scene으로 검사한다.
검사를 위해 사용자의 씬을 저장하거나 일반 씬으로 열어 교체하지 않는다.

모든 CSV의 검증이 끝나기 전에는 어떤 테이블도 변경하지 않는다.
반영 중 예외가 나면 테이블과 CSV의 이전 파일 및 메모리 상태를 복원한다.
CSV 변경 자체를 취소하는 것은 아니다. 검증 오류 시 수정한 CSV는 그대로 두고 테이블만 기존 상태를 유지한다.

문구 참조는 Inspector의 LocalizedString을 사용하고 코드에서 문자열 키를 조립하지 않는다.
런타임에 임의로 만들어 낸 키, 외부 파일·서버의 참조는 정적 에셋 검사로 검증할 수 없다.
새 시스템의 조회 API도 LocalizedString을 받도록 구성했다.

## 일반 TMP UI 연결

1. TMP_Text가 있는 GameObject를 선택한다.
2. `Astrodiver > Localization > Bind Selected TMP`를 실행한다.
3. 추가된 LocalizeStringEvent의 String Reference에서 테이블과 항목을 선택한다.

이 메뉴는 TMP의 공개 text setter를 영구 이벤트로 연결한다. Editor/Runtime 모두 갱신하며 중복으로 연결하지 않는다.
일반 UI는 이 컴포넌트가 문구를 소유하게 하고, 별도 스크립트에서 같은 TMP에 하드코딩 문구를 덮어쓰지 않는다.
동적 수량 등은 LocalizedString의 Arguments/Smart String 변수를 사용한다.

타이핑 연출처럼 별도 표시 코드가 문자열과 maxVisibleCharacters를 관리하는 대상에는
이 컴포넌트를 중복 연결하지 않는다. GameText를 통해 문구를 준비한 뒤 해당 연출이 표시를 담당한다.

## 코드에서 조회하기

문구 참조는 직렬화된 `LocalizedString` 필드로 보관한다. 테이블 이름이나 키를 코드에서 조립하지 않는다.
`GameText.Prepare`로 필요한 테이블을 먼저 비동기로 준비하고 `TryResolve` 또는 `ResolvePrepared`로 조회한다.
일반 UI의 언어 변경 갱신은 `LocalizeStringEvent`가 담당한다.
자체 표시 코드는 `LocalizationSettings.SelectedLocaleChanged`를 받아 표시된 문구를 갱신한다.

여러 언어를 사전 로딩하려면 `allLocales`를 사용한다. 테이블을 명시적으로 유지해야 하는 호출자는
`retainedTables` 목록을 넘길 수 있다. 이 경우 추가로 획득한 Addressables 핸들은 호출자가
사용 종료 시 `Addressables.Release`로 반환해야 한다. 일반적인 조회에는 `null`을 전달하면 된다.

## 언어 확장

추후 Locale과 각 Collection의 해당 언어 String Table을 만든 뒤 CSV에 동일 언어 코드 열(`en`, `ja` 등)을 추가한다.
현재 동기화는 Collection의 모든 언어에 값을 요구한다. 미번역 빈 셀을 자동 번역하거나 한국어로 채우지 않는다.
게임의 언어 선택은 `LocalizationSettings.SelectedLocale`을 사용한다.
폰트 교체, 언어 선택 저장, 실제 번역, 언어별 타이밍/레이아웃 검증은 다음 작업 범위다.

## 검증

EditMode 테스트 어셈블리: `Astrodiver.Localization.Editor.Tests`.
CSV 인코딩/줄바꿈/따옴표, 중복 키·ID, ID 발급과 재사용, 삭제 차단과 무변경,
미사용 행의 실제 삭제, 동일 ID 이름 변경, CSV 미반영 빌드 차단, 일반 TMP의 영구 연결을 검사한다.
테스트는 한국어 Locale을 사용하는 전용 임시 CSV·Collection·직렬화된 참조 에셋을 만들고
각 테스트 종료 시 제거한다. 실제 기능별 문구나 씬·Timeline이 없어도 실행할 수 있다.

기존 메뉴·튜토리얼 등의 텍스트는 이 구조를 이용해 순차 이전한다.
