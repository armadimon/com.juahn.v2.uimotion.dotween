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
