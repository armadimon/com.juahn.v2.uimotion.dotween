using System;
using DG.Tweening;

namespace Juahn.UiMotion.DoTween
{
    /// <summary>
    /// DOTween을 시간 공급자로 쓰는 트윈 러너.
    ///
    /// <b>내장 러너와 근본적으로 다른 점</b> — DOTween은 자기 업데이트 루프에서 스스로
    /// 진행한다. 스코프가 넣어 주는 델타를 쓰지 않는다. 그래서 이 러너의 핸들은
    /// <c>Tick</c>에서 아무것도 하지 않고 <c>IsDone</c>으로 DOTween의 상태를 되비칠 뿐이다.
    ///
    /// 그 대가로 얻는 것은 DOTween의 트윈 풀링이다. 유한 트윈을 초당 여러 번 재발사하는
    /// 연출이 많으면 할당이 줄어든다. 유지 연출(<c>Float</c>·<c>Bounce</c>)은 내장 러너도
    /// 이미 프레임당 할당이 0이라 차이가 없다.
    ///
    /// <b>시간 스케일은 인스턴스가 정한다.</b> 러너 인터페이스가 그것을 나르지 않고,
    /// DOTween은 스코프의 델타를 보지 않기 때문이다. 부트스트랩이 그래프의
    /// <c>UseUnscaledTime</c>에 맞는 인스턴스를 꽂는다.
    /// </summary>
    public sealed class DoTweenRunner : IMotionTweenRunner
    {
        /// <summary>UI 기본값 — 일시정지 중에도 팝업은 열리고 닫혀야 한다.</summary>
        public static readonly DoTweenRunner Unscaled = new DoTweenRunner(true);

        public static readonly DoTweenRunner Scaled = new DoTweenRunner(false);

        private readonly bool _unscaled;

        public DoTweenRunner(bool useUnscaledTime)
        {
            _unscaled = useUnscaledTime;
        }

        public bool UseUnscaledTime => _unscaled;

        public IMotionHandle Run(float duration, EaseKind ease, Action<float> onEased)
        {
            // 지속시간 0은 "즉시"다. DOTween에 0짜리 트윈을 맡기면 콜백이 이번 프레임에
            // 오지 않거나 아예 오지 않을 수 있다. 계약은 "즉시 1을 한 번 보내고 끝난다"이므로
            // 여기서 직접 처리한다.
            if (duration <= 0f)
            {
                if (onEased != null)
                {
                    onEased(1f);
                }

                return MotionHandle.Completed;
            }

            if (onEased == null)
            {
                // 진행률이 필요 없는 경우(Delay 등). 그래도 시간은 흘러야 한다.
                Tweener empty = DOVirtual.Float(0f, 1f, duration, DoNothing)
                    .SetEase(Ease.Linear)
                    .SetUpdate(_unscaled);

                return new DoTweenHandle(empty);
            }

            Action<float> captured = onEased;

            // DOVirtual.Float은 이징이 적용된 값을 준다. 오버슈트하는 이징(OutBack,
            // OutElastic)에서는 1을 넘는 값이 나오고, 끝값은 정확히 1이다 —
            // 내장 러너와 같은 계약이다.
            Tweener tween = DOVirtual.Float(0f, 1f, duration, delegate(float eased) { captured(eased); })
                .SetEase(DoTweenEase.Map(ease))
                .SetUpdate(_unscaled);

            return new DoTweenHandle(tween);
        }

        private static void DoNothing(float value)
        {
        }

        /// <summary>
        /// DOTween 트윈 하나를 코어의 핸들 계약으로 감싼다.
        ///
        /// <b>왜 트윈의 상태를 직접 묻지 않고 플래그를 두는가</b> — DOTween은 트윈을
        /// 풀링한다. 끝난 트윈은 <c>autoKill</c>로 죽어 풀로 돌아가고, 다음에 누가
        /// 트윈을 만들면 <b>같은 인스턴스가 그대로 재사용된다.</b> 그러면 이 핸들이
        /// 들고 있는 참조가 남의 살아 있는 트윈을 가리키게 되어
        /// <c>IsActive()</c>가 다시 true가 된다 — 한 번 끝났다고 답한 핸들이 도로
        /// "안 끝났다"로 뒤집히고, <c>Cancel</c>은 남의 트윈을 죽인다.
        ///
        /// 그래서 완료·강제종료를 DOTween의 콜백으로 받아 이쪽 플래그에 적고
        /// 참조를 즉시 놓는다. 풀에 돌아간 인스턴스를 두 번 다시 만지지 않는다.
        /// </summary>
        private sealed class DoTweenHandle : IMotionHandle
        {
            private Tween _tween;
            private bool _done;

            public DoTweenHandle(Tween tween)
            {
                _tween = tween;

                // 정상 완료(OnComplete)와 죽음(OnKill) 양쪽을 다 받는다. autoKill이
                // 켜져 있으면 둘 다 오고, 프로젝트가 defaultAutoKill을 꺼 두었으면
                // OnComplete만 온다. 어느 쪽이든 이 핸들은 끝이다.
                tween.OnComplete(MarkDone);
                tween.OnKill(MarkDone);
            }

            /// <summary>
            /// DOTween이 끝냈거나, 죽었거나, 우리가 죽였으면 끝이다.
            ///
            /// <c>IsComplete()</c>는 부르지 않는다. 죽은 트윈에 부르면 DOTween이 경고를
            /// 내는데, 방치형에서 그 경고가 쌓이면 콘솔을 덮는다. 완료는 이미
            /// <c>MarkDone</c>이 잡았다.
            ///
            /// <c>_tween.IsActive()</c>는 콜백이 끝내 오지 않는 경로
            /// (<c>DOTween.Clear</c> 같은 일괄 정리)를 위한 그물이다.
            /// </summary>
            public bool IsDone => _done || _tween == null || !_tween.IsActive();

            /// <summary>
            /// 아무것도 하지 않는다. DOTween이 자기 루프에서 이미 진행시켰다.
            ///
            /// 스코프가 넣어 주는 델타를 여기에 더하면 <b>시간이 두 번 흐른다.</b>
            /// </summary>
            public void Tick(float deltaSeconds)
            {
            }

            public void Cancel()
            {
                // 참조를 먼저 놓는다. Kill이 OnKill을 동기로 부르든 다음 프레임으로
                // 미루든, 그 뒤에 이 핸들이 트윈을 다시 만지는 일이 없어야 한다.
                Tween tween = _tween;
                _tween = null;
                _done = true;

                if (tween != null && tween.IsActive())
                {
                    // complete: false — 취소는 "중간에 끊겼다"이지 "끝났다"가 아니다.
                    // true로 두면 마지막 값이 한 번 더 적용돼 원상 복구와 싸운다.
                    tween.Kill(false);
                }
            }

            private void MarkDone()
            {
                _done = true;
                _tween = null;
            }
        }
    }
}
