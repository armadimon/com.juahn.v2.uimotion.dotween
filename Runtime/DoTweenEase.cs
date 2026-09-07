using DG.Tweening;

namespace Juahn.UiMotion.DoTween
{
    /// <summary>
    /// 코어의 <see cref="EaseKind"/>를 DOTween의 <see cref="Ease"/>로 옮긴다.
    ///
    /// 두 백엔드가 같은 이징 이름에 대해 <b>눈에 띄게 다른 곡선</b>을 그리면
    /// 백엔드를 갈아 끼우는 순간 프로젝트 전체의 연출이 미묘하게 바뀐다.
    /// 다행히 13개가 전부 1:1로 대응한다.
    /// </summary>
    internal static class DoTweenEase
    {
        /// <summary>
        /// 모르는 값은 <see cref="Ease.Linear"/>다. 코어의
        /// <c>EaseLibrary.Evaluate</c>가 같은 규칙을 쓴다 — 두 백엔드가 같은
        /// 그래프에 대해 다르게 실패하면 안 된다.
        /// </summary>
        public static Ease Map(EaseKind kind)
        {
            switch (kind)
            {
                case EaseKind.Linear:     return Ease.Linear;

                case EaseKind.InQuad:     return Ease.InQuad;
                case EaseKind.OutQuad:    return Ease.OutQuad;
                case EaseKind.InOutQuad:  return Ease.InOutQuad;

                case EaseKind.InCubic:    return Ease.InCubic;
                case EaseKind.OutCubic:   return Ease.OutCubic;
                case EaseKind.InOutCubic: return Ease.InOutCubic;

                case EaseKind.InSine:     return Ease.InSine;
                case EaseKind.OutSine:    return Ease.OutSine;
                case EaseKind.InOutSine:  return Ease.InOutSine;

                case EaseKind.OutBack:    return Ease.OutBack;
                case EaseKind.OutElastic: return Ease.OutElastic;
                case EaseKind.OutBounce:  return Ease.OutBounce;

                default:                  return Ease.Linear;
            }
        }
    }
}
