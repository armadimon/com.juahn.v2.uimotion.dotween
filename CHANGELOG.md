# Changelog

## [0.1.0] - 미출시

### 추가

DOTween 백엔드 패키지 뼈대.

- `juahn.v2.UiMotion.DoTween` 어셈블리 (런타임). `juahn.v2.UiMotion` ·
  `juahn.v2.UiMotion.Core`를 참조하고 `DOTween.dll`을
  `precompiledReferences`로 명시한다
- **`defineConstraints: ["UIMOTION_DOTWEEN"]`** — DOTween은 에셋스토어 플러그인이라
  UPM 패키지가 아니고, 그래서 `versionDefines`가 발동하지 않는다. 사용자가 Player
  Settings에 심볼을 한 번 추가하면 이 어셈블리가 켜지고, 없으면 통째로 컴파일에서
  빠진다. DOTween이 없는 프로젝트에 이 패키지를 넣어도 아무것도 깨지지 않는다
- `com.demigiant.dotween`을 `package.json`의 의존성에 **적지 않는다** — UPM이
  해석할 수 없는 이름이다. asmdef가 DLL 이름으로 참조한다
- `DoTweenEase` — 코어의 `EaseKind` 13개를 `DG.Tweening.Ease`로 1:1 대응시킨다.
  모르는 값은 `Linear`로, 코어의 `EaseLibrary.Evaluate`와 같은 규칙이다
- 컴파일 게이트 (`Tools~/compile-check`). 설치된 Unity의 매니지드 DLL과
  형제 프로젝트의 `DOTween.dll`을 참조해 런타임 패키지의 `Runtime` 전체와 이
  백엔드를 `dotnet build`로 컴파일한다. DOTween이 유료 에셋이라 CI가 아니라
  로컬이다. `DefineConstants`에 `UIMOTION_DOTWEEN`을 넣어 asmdef의 조건과 맞춘다
- 게이트가 **통짜 `UnityEngine.dll`과 `UnityEngine.AnimationModule.dll`을 함께
  참조한다.** DOTween의 `SetEase` 오버로드 하나가 `AnimationCurve`를 받는데
  그 타입이 통짜 어셈블리에도 있어, 빠지면 `CS0012`로 깨진다
- CI 게이트 — 매니페스트 형식 · DOTween을 의존성에 적지 않았는지 · 런타임
  asmdef와 그 참조 · `UIMOTION_DOTWEEN` 제약과 `DOTween.dll` 참조 · `Runtime`
  폴더 존재 · `.meta` 누락 · GUID 중복

백엔드 본체.

- `DoTweenRunner : IMotionTweenRunner` — `DOVirtual.Float`으로 이징된 진행률을
  흘린다. 시간 스케일이 다른 두 인스턴스(`Unscaled` · `Scaled`)를 미리 만들어 둔다.
  러너 인터페이스가 시간 스케일을 나르지 않고 DOTween이 스코프의 델타를 보지
  않으므로, 스케일은 인스턴스가 정할 수밖에 없다
- **핸들의 `Tick`은 아무것도 하지 않는다.** DOTween은 자기 업데이트 루프에서
  스스로 진행한다. 여기서 델타를 더하면 시간이 두 번 흐른다
- 지속시간 0은 러너가 직접 처리한다 — `onEased(1f)` 한 번 뒤 `MotionHandle.Completed`.
  DOTween에 0짜리 트윈을 맡기면 콜백이 이번 프레임에 오지 않거나 아예 오지 않는다
- **완료를 트윈 상태가 아니라 `OnComplete`/`OnKill` 콜백의 플래그로 판정한다.**
  DOTween은 트윈을 풀링한다. 끝난 트윈은 풀로 돌아가고 다음 요청이 같은 인스턴스를
  재사용하므로, 핸들이 참조를 계속 들고 있으면 `IsActive()`가 도로 true가 되어
  "끝났다"가 뒤집히고 `Cancel`이 남의 트윈을 죽인다. 콜백에서 참조를 즉시 놓아
  풀에 돌아간 인스턴스를 두 번 다시 만지지 않는다
- `IsComplete()`를 부르지 않는다 — 죽은 트윈에 부르면 DOTween이 경고를 낸다.
  방치형에서 그 경고가 쌓이면 콘솔을 덮는다
- `Cancel`은 `Kill(false)` — 취소는 "중간에 끊겼다"이지 "끝났다"가 아니다.
  `true`로 두면 마지막 값이 한 번 더 적용돼 스코프의 원상 복구와 싸운다

부트스트랩.

- `MotionDoTweenBootstrap.Apply(MotionPlayer)` — 그래프의 `UseUnscaledTime`에 맞는
  러너 인스턴스를 꽂는다. 그래프를 갈아 끼운 뒤에는 다시 불러야 한다
- **`Application.isPlaying`일 때만 꽂는다.** DOTween의 업데이트 루프는 런타임에
  만들어지는 MonoBehaviour라 에디트 모드에서 돌지 않는다. 프리뷰에 DOTween 핸들이
  걸리면 `IsDone`이 영원히 false가 되어 프리뷰가 멈춘 채 끝나지 않는다.
  에디터 프리뷰는 언제나 내장 러너를 쓴다
- `MotionDoTweenBootstrap.ApplyToLoadedScenes()` — 로드된 씬 전체를 훑는다.
  나중에 만들어지는 플레이어(풀·UiService가 로드하는 팝업)는 잡지 못한다
- `MotionDoTweenScope` — 프리팹에 붙이는 컴포넌트. 활성화될 때마다 꽂는다.
  **`[DefaultExecutionOrder(-100)]`이 이 컴포넌트의 핵심이다** — `MotionPlayer.OnEnable`은
  `PlayOnEnable`이 켜져 있으면 그 자리에서 `Start`를 발사하고, 발사는 노드의 `Play`를
  동기로 부른다. 노드가 러너를 읽는 시점이 바로 거기라, 실행 순서를 앞당기지 않으면
  첫 연출만 조용히 내장 러너로 도는 어긋남이 생긴다
- `ApplyToLoadedScenes`의 `FindObjectsByType` 호출을 `#pragma warning disable 618`로
  감쌌다. 6000.5에서 이 오버로드에 Obsolete가 붙었지만 대체 오버로드는 6000.5에
  새로 생긴 것이라 이 패키지의 최소 버전(6000.0)에는 없다. 6000.3에서 `CS1503`으로
  확인했다

문서.

- README 맨 위에 **설치가 두 단계**라는 것과, 두 번째(`UIMOTION_DOTWEEN` 심볼)를
  빼먹으면 **오류 없이 조용히 아무 일도 일어나지 않는다**는 것을 굵게 적었다.
  이 패키지의 유일한 조용한 실패 모양이다
- 러너를 꽂는 세 가지 방법(전역 부트스트랩 · `MotionDoTweenScope` · 직접 `Apply`)과
  **언제 꽂아도 되는가** — 러너는 노드의 `Play` 시점에 한 번 읽히고 `Play`는 `Fire`
  스택 안에서 동기로 일어나므로, 이미 돌고 있는 연출은 백엔드가 바뀌지 않는다
- **에디터 프리뷰는 언제나 내장 러너를 쓴다**는 것과 그 이유
- **언제 이것을 쓰는가** — 계획 2의 실측을 인용했다. 유지 연출은 내장 러너도 프레임당
  할당이 0이라 차이가 없고, 유한 트윈을 초당 여러 번 재발사할 때만 의미가 있다.
  그리고 대개의 병목은 백엔드가 아니라 캔버스 리빌드(0.1~1 ms)다. 그냥 켜는 것이
  아니라 프로파일러에 발사 할당이 잡힐 때만 켠다
- 시간 스케일이 인스턴스로 갈리는 이유 — 러너 인터페이스가 그것을 나르지 않고
  DOTween은 스코프의 델타 대신 `SetUpdate(bool)`로 시계를 정하기 때문이다
- **두 백엔드가 다르게 동작하는 지점**을 한 절로 모았다 — `Tick`이 비어 있다 ·
  효과 적용 시점이 프레임 안에서 다르다 · `Delay`도 트윈을 소모한다 · 지속시간 0은
  DOTween을 거치지 않는다 · 이징은 13개가 1:1이라 차이가 없다
- `docs/unity-verification.md` — 컴파일 게이트가 지키지 못하는 동작 확인 목록.
  프리뷰가 걸리는 실패, 풀 재사용으로 연출이 멈춰 서는 실패, 첫 연출만 내장 러너로
  도는 실패를 각각 "어떻게 눈에 띄는가"와 함께 적었다
- 런타임 패키지의 설계 스펙 3.4를 이 패키지의 실제 구현에 맞췄다. 스펙은 `versionDefines`로
  DOTween이 없으면 컴파일에서 빠진다고 적고 있었는데 **그것은 동작하지 않는다** —
  DOTween이 UPM 패키지가 아니라 걸 이름이 없다. 실제로 쓴 `defineConstraints` +
  `overrideReferences` + `precompiledReferences: ["DOTween.dll"]`과, 그 대가인
  조용한 실패, 그리고 **에디터 프리뷰에서 이 백엔드를 쓰지 않는 이유**가 스펙에 들어갔다.
  런타임 패키지 README에도 두 단계 설치와 `UIMOTION_DOTWEEN` 심볼 경고가 실렸다
