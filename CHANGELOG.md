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
