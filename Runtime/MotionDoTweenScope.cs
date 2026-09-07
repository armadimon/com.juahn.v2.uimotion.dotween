using UnityEngine;

namespace Juahn.UiMotion.DoTween
{
    /// <summary>
    /// 프리팹에 붙여 두면 그 플레이어가 활성화될 때마다 러너를 꽂는다.
    ///
    /// 전역 부트스트랩이 잡지 못하는 것들 — 풀에서 꺼내는 아이템, UiService가 나중에
    /// 로드하는 팝업 — 을 위한 것이다. 붙이는 비용이 <c>OnEnable</c> 한 번이라
    /// 전역 순회보다 오히려 싸다.
    ///
    /// <b><see cref="DefaultExecutionOrder"/>가 이 컴포넌트의 전부다.</b>
    /// <c>MotionPlayer.OnEnable</c>은 <c>PlayOnEnable</c>이 켜져 있으면 그 자리에서
    /// <c>Start</c>를 발사하고, 발사는 노드의 <c>Play</c>를 <b>동기로</b> 부른다.
    /// 노드가 러너를 읽는 시점이 바로 거기다. 그러니 이 컴포넌트가 늦게 돌면
    /// 첫 연출만 조용히 내장 러너로 도는 — 오류도 나지 않는 — 어긋남이 생긴다.
    /// 실행 순서를 앞당겨 그것을 막는다.
    /// </summary>
    [AddComponentMenu("Juahn/UI Motion/Motion DOTween Scope")]
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MotionPlayer))]
    public sealed class MotionDoTweenScope : MonoBehaviour
    {
        [SerializeField] private MotionPlayer _player;

        private void Reset()
        {
            _player = GetComponent<MotionPlayer>();
        }

        private void OnEnable()
        {
            if (_player == null)
            {
                _player = GetComponent<MotionPlayer>();
            }

            MotionDoTweenBootstrap.Apply(_player);
        }
    }
}
