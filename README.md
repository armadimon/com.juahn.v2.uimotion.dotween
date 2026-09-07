# UI Motion — DOTween 백엔드

UI Motion의 내장 트윈 러너를 DOTween의 풀링된 트윈으로 갈아 끼우는 **선택적** 패키지다.
그래프도 노드도 그대로다. 바뀌는 것은 "시간이 어디서 흐르는가" 하나뿐이다.

---

## 설치는 두 단계다 — 두 번째를 빼먹으면 아무 일도 일어나지 않는다

1. 이 패키지를 넣는다. `com.juahn.v2.uimotion`과 DOTween이 먼저 있어야 한다.
2. **Player Settings > Other Settings > Scripting Define Symbols에 `UIMOTION_DOTWEEN`을 추가한다.**

**두 번째를 빼먹으면 오류가 하나도 나지 않는다.** 어셈블리가 통째로 컴파일에서 빠지고
UI Motion은 내장 러너로 계속 잘 돈다. 겉으로는 성공한 것처럼 보인다. 연출이 그대로인데
"DOTween을 켰다"고 믿게 되는 유일한 실패 모양이므로, 켰다고 생각하는데 아무것도 달라지지
않았다면 이 심볼부터 확인한다.

`asmdef`의 `defineConstraints`가 그렇게 만들어져 있다. DOTween은 에셋스토어 플러그인이라
UPM 패키지가 아니고, 그래서 패키지 이름에 걸리는 `versionDefines`는 발동하지 않는다.
심볼이 없으면 어셈블리가 빠진다는 것은 제약이 아니라 안전장치다 — DOTween이 없는
프로젝트에 이 패키지가 딸려 들어와도 그 프로젝트는 깨지지 않는다.

---

## 러너를 꽂는 세 가지 방법

러너를 만들어 두는 것만으로는 아무 일도 일어나지 않는다. `MotionPlayer.TweenRunner`에
꽂아야 한다. 비어 있으면 `BuiltinTweenRunner.Shared`로 폴백한다.

### 1. 전역 부트스트랩 — 씬에 이미 있는 것들

```csharp
using Juahn.UiMotion.DoTween;

// 씬 로드가 끝난 뒤에 한 번.
int count = MotionDoTweenBootstrap.ApplyToLoadedScenes();
```

로드된 씬의 모든 `MotionPlayer`를 훑어 꽂는다(비활성 포함). **나중에 만들어지는
플레이어는 잡지 못한다** — 풀에서 꺼내는 아이템, UiService가 나중에 로드하는 팝업이
그렇다. 그런 것들은 아래 두 방법을 쓴다.

### 2. `MotionDoTweenScope` 컴포넌트 — 프리팹에 붙인다

`Juahn/UI Motion/Motion DOTween Scope`. `MotionPlayer` 옆에 붙여 두면 그 플레이어가
활성화될 때마다 알아서 꽂는다. 풀링되는 오브젝트와 동적으로 로드되는 팝업의 정답이다.
비용은 `OnEnable` 한 번이라 씬 전체를 훑는 것보다 오히려 싸다.

### 3. 직접 부른다 — 만들어지는 자리에서

```csharp
MotionPlayer player = instance.GetComponent<MotionPlayer>();
MotionDoTweenBootstrap.Apply(player);
```

**`SetGraph`로 그래프를 갈아 끼웠으면 다시 불러야 한다.** 시간 스케일이 그래프마다 다를
수 있고, 러너 인스턴스가 그것을 들고 있기 때문이다.

### 언제 꽂아도 되는가

**이미 돌고 있는 연출은 백엔드를 바꾸지 못한다.** 러너는 노드가 `Play`될 때 한 번 읽히고,
`Play`는 `Fire` 호출 스택 안에서 동기로 일어난다. 늦게 꽂으면 다음 발사부터 적용된다.

특히 `PlayOnEnable`이 켜진 플레이어는 `MotionPlayer.OnEnable`이 그 자리에서 `Start`를
발사한다. 그래서 `MotionDoTweenScope`에 `[DefaultExecutionOrder(-100)]`이 붙어 있다 —
그것이 없으면 컴포넌트 순서에 따라 첫 연출만 조용히 내장 러너로 돈다. 오류는 나지 않는다.

---

## 에디터 프리뷰는 언제나 내장 러너를 쓴다

`MotionDoTweenBootstrap.Apply`는 `Application.isPlaying`이 아니면 아무것도 하지 않는다.
일부러 그렇다.

DOTween의 업데이트 루프는 런타임에 만들어지는 MonoBehaviour다. 에디트 모드에서는 돌지
않으므로, 프리뷰에 DOTween 핸들이 걸리면 `IsDone`이 영원히 false가 되어 **프리뷰가 멈춘
채 끝나지 않는다.**

두 러너가 같은 이징 표를 쓰므로 프리뷰의 목적(타이밍 확인)에는 차이가 없다.

---

## 언제 이것을 쓰는가 — 그냥 켜지 말고 측정하고 켠다

이 패키지가 주는 것은 **DOTween의 트윈 풀링** 하나다. 그 외에는 얻는 것이 없다.

| 연출의 모양 | 차이 |
|---|---|
| 유지 연출 (`Float` · `Bounce`) | **없다.** 내장 러너도 이미 프레임당 할당이 0이다 |
| 한 번 도는 유한 트윈 (팝업 열기 등) | 사실상 없다. 발사당 할당이 몇 개 줄어들 뿐이다 |
| 유한 트윈을 초당 여러 번 재발사 | **여기서만 의미가 있다** |

계획 2의 실측이 남긴 결론은 이렇다. 유지 연출은 프레임당 0 B다. 같은 연출을 `Scale`을
`Repeat`으로 감싸 만들면 300개 기준 초당 500 KB 안팎이 나가는데, **그것은 백엔드 문제가
아니라 그래프 저작 문제다.** `Bounce` 노드로 바꾸면 0 B가 된다. 백엔드를 갈아 끼우기 전에
그래프를 먼저 본다.

그리고 더 큰 맥락 — `anchoredPosition`을 만지면 캔버스가 dirty가 되고 리빌드는 보통
0.1~1 ms다. 플레이어 300개를 해석하는 12 us보다 10~80배 크다. 병목은 대개 "무엇으로
보간하는가"가 아니라 "몇 개를 얼마나 자주 dirty로 만드는가"다.

**요약: 프로파일러에 발사 할당이 실제로 잡힐 때만 켠다.**

---

## 시간 스케일이 인스턴스로 갈리는 이유

러너 인터페이스는 시간 스케일을 나르지 않는다.

```csharp
IMotionHandle Run(float duration, EaseKind ease, Action<float> onEased);
```

내장 러너에는 필요가 없었다 — `MotionPlayer.TickFromPump`가 그래프의 `UseUnscaledTime`을
보고 스케일된 델타와 안 된 델타 중 하나를 골라 넣어 주고, 핸들은 받은 것을 쓸 뿐이다.

DOTween은 그 델타를 보지 않는다. 자기 루프에서 스스로 진행하고, 어느 시계를 쓸지는
트윈을 만들 때 `SetUpdate(bool)`로 정해진다. 그래서 스케일을 아는 주체가 러너 자신일
수밖에 없다.

```csharp
DoTweenRunner.Unscaled   // UI 기본값. 일시정지 중에도 팝업은 열리고 닫혀야 한다
DoTweenRunner.Scaled     // 그래프의 UseUnscaledTime이 꺼져 있을 때
```

`Apply`가 그래프를 보고 둘 중 하나를 고른다. 그래프가 없으면 `Unscaled`다.

---

## 두 백엔드가 다르게 동작하는 지점

같은 그래프를 두 백엔드로 돌렸을 때 알아 둘 차이다.

- **핸들의 `Tick`이 아무것도 하지 않는다.** DOTween이 자기 루프에서 이미 진행시켰다.
  여기에 스코프의 델타를 더하면 시간이 두 번 흐른다.
- **효과가 적용되는 시점이 프레임 안에서 다르다.** 내장 러너는 `MotionPump`가 도는
  자리에서, DOTween은 DOTween의 업데이트 자리에서 값을 적용한다. 한 프레임 안에서
  순서가 어긋날 수 있다.
- **`Delay`처럼 진행률이 필요 없는 노드도 트윈을 하나 소모한다.** 시간은 흘러야 하기
  때문이다.
- **지속시간 0은 러너가 직접 처리한다.** DOTween에 0짜리 트윈을 맡기면 콜백이 이번
  프레임에 오지 않거나 아예 오지 않는다. 계약은 "즉시 1을 한 번 보내고 끝"이므로
  DOTween을 거치지 않는다.
- **이징 곡선은 13개가 전부 1:1로 대응한다.** 이 축에서는 차이가 없다.

---

## 검증

```bash
./Tools~/compile-check/run.sh
```

Unity 에디터 없이 런타임 패키지의 `Runtime` 전체와 이 백엔드를 `dotnet build`로
컴파일한다. DOTween은 유료 에셋이라 재배포할 수 없으므로 CI가 아니라 로컬 게이트다.
형제 프로젝트의 `Assets/Plugins/Demigiant/DOTween/DOTween.dll`을 자동으로 찾고,
못 찾으면 `DOTWEEN_DLL`로 지정한다.

컴파일 게이트가 지키지 못하는 것 — 실제 동작 — 은 `docs/unity-verification.md`에
목록으로 있다.

---

## 라이선스

MIT. DOTween은 별도의 라이선스를 따르며 이 패키지에 포함되지 않는다.
