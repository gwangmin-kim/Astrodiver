# 인트로 컷씬

새 게임 전용 컷씬이다. 이어하기는 기존처럼 허브로 직접 진입한다.

## 재생과 허브 로딩

`MainMenuController`가 새 게임 데이터를 초기화하고 `Intro` 씬을 연다.
`IntroCutsceneController`는 메뉴에서 들어오는 전환이 끝나고 Localization 문구 준비가 완료되면 허브 비동기 로딩과 Timeline 재생을 시작한다.
허브는 `allowSceneActivation = false` 상태로 준비되며, 입력 전에는 Hub의 Awake/Start가 실행되지 않는다.
활성화를 보류하는 동안 씬 로딩 큐를 점유하므로 `SceneTransitionManager`가 다른 전환 요청을 차단한다.

Timeline 마지막 편지가 끝나면 화면을 유지한다. 기본 2초 뒤에 하단 안내 문구와 입력을 활성화한다.
활성화 전 입력, 활성화 프레임의 입력, 이미 누르고 있던 키는 무시한다. 키를 놓은 뒤 새로 눌러야 넘어간다.
키보드·마우스 버튼·게임패드 버튼을 받으며 포인터/스틱 이동은 입력으로 처리하지 않는다.

입력이 먼저 들어오고 허브가 아직 준비되지 않았다면 요청을 기억하고 `잠시만 기다려 주세요...`를 표시한다.
허브 준비가 끝나면 추가 입력 없이 기존 iris 전환으로 Hub의 StartPoint에 진입한다.
씬 활성화와 완료 처리는 영속 `SceneTransitionManager`가 담당하므로 Intro가 언로드되어도 전환이 완료된다.
인트로 재생 중에는 영속 튜토리얼 안내를 숨긴다.

## 편집 위치

- 씬: `Assets/Scenes/Intro.unity`
- Timeline: `Assets/Cutscenes/Intro/IntroTimeline.playable`
- 코드: `Assets/Scripts/Cutscenes/Intro/`
- 문구 원문: `Assets/Localization/CSV/Intro.csv`, `Common.csv` 수정 후 `Astrodiver/Localization/Synchronize CSV`
- 컷의 문구 선택: `Intro shots` 트랙의 각 `IntroShotClip.textReference` (테이블 항목 참조)
- 컷 길이: Timeline 클립 배치
- 입력 대기 시간: `Intro Director`의 `IntroCutsceneController._letterInputDelay`
- 타이핑 속도: `Intro Canvas`의 `IntroPresentation._charactersPerSecond`
- 최종 편지는 마지막 `Letter` 클립으로 유지한다. 클립 사이에 빈 구간이나 겹침을 만들지 않는다.
- 기본 재생 길이는 43초이며, 이후 2초 대기와 사용자의 입력 대기가 추가된다.

초기 진행 바는 Timeline 시간만 사용하는 연출이다. 실제 로딩 진척도에 연결되지 않는다.
글리치·진행 바·별·편지 테두리는 `IntroProceduralGraphic`의 메시로 생성하고 텍스트는 TMP의 표시 글자 수로 타이핑한다.
컷별 연출은 `IntroPresentation`에만 있으며 범용 컷씬 시스템으로 확장하지 않았다.

## 아트와 오디오

지구의 기억은 320×180 원본 정렬을 유지하는 배경1/배경2/창틀/로봇 과학자 레이어로 구성한다.
줌 아웃은 공통 부모 `Composition - Zoom Out`에 적용한다. 인물 클로즈업은 `intro_frame_2.png`를 사용한다.
사용하는 다섯 PNG의 필터는 Point, 압축은 None으로 설정했다.
`intro_frame_1_full.png`는 구성 참고 자료이며 씬·Timeline 의존성에 포함되지 않는다.

`Space` 하위의 `Ship - Sprite Slot`, `Near Planet - Sprite Slot`, `Far Planet - Sprite Slot`을 후속 아트 연결점으로 둔다.
우주선은 임시 사각형으로 표시한다. 실제 스프라이트를 연결할 때 임시 Solar Panels 자식을 제거하고 Image 색을 조정한다.
행성 슬롯은 현재 투명하다. 스프라이트를 연결한 뒤 Image 색의 alpha를 1로 바꾼다.
오디오는 `Music (asset slot)`/`SFX (asset slot)` 트랙과 바인딩된 AudioSource를 제공하며 현재 클립은 비어 있다.

Canvas는 16:9 구성을 유지하고 다른 화면 비율에서는 바깥에 검정 여백을 둔다.
Build Settings에는 Intro를 마지막에 추가해 기존 스테이지의 직렬화된 build index를 유지한다.

## 검증 범위

검증 시 실제 저장 파일을 별도로 보관하고 종료 후 복원한다.
새 게임 경로는 `TryStartNewGame`을 호출하므로 개발 중 직접 눌러 볼 때에도 기존 새 게임 저장 동작이 실행된다.
인트로만 미리 볼 때는 Intro 씬에서 Play할 수 있다. 이 경우 새 게임 데이터를 생성하지 않고 에디터 초기화 데이터를 사용한다.

컷 길이, 텍스트, 효과 강도, 임시 우주선/행성 및 오디오의 최종 연출은 후속 작업 대상이다.

CSV 삭제 동기화와 일반 TMP 연결, 다국어 확장 절차는 `Docs/Localization.md`를 참고한다.
