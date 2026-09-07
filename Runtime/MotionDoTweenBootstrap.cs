using UnityEngine;

namespace Juahn.UiMotion.DoTween
{
    /// <summary>
    /// <see cref="MotionPlayer"/>에 DOTween 러너를 꽂는다.
    ///
    /// <b>플레이 모드에서만 꽂는다.</b> DOTween의 업데이트 루프는 런타임에 만들어지는
    /// MonoBehaviour라 에디트 모드에서 돌지 않는다. 에디터 프리뷰에 DOTween 핸들이 걸리면
    /// <c>IsDone</c>이 영원히 false가 되어 <b>프리뷰가 멈춘 채 걸린다.</b>
    /// 프리뷰는 언제나 내장 러너를 쓴다 — 두 러너가 같은 이징 표를 쓰므로 타이밍 확인에는
    /// 차이가 없다.
    /// </summary>
    public static class MotionDoTweenBootstrap
    {
        /// <summary>
        /// 플레이어 하나에 러너를 꽂는다. 그래프의 시간 스케일에 맞는 인스턴스를 고른다.
        ///
        /// 그래프를 바꾼 뒤에는 다시 불러야 한다 — 시간 스케일이 그래프마다 다를 수 있다.
        ///
        /// <b>이미 돌고 있는 연출은 바꾸지 못한다.</b> 러너는 노드가 Play될 때 한 번 읽히고,
        /// Play는 <c>Fire</c> 호출 스택 안에서 동기로 일어난다. 늦게 꽂으면 다음 발사부터
        /// 적용된다.
        /// </summary>
        public static void Apply(MotionPlayer player)
        {
            if (player == null || !Application.isPlaying)
            {
                return;
            }

            bool unscaled = player.Graph == null || player.Graph.UseUnscaledTime;
            player.TweenRunner = unscaled ? DoTweenRunner.Unscaled : DoTweenRunner.Scaled;
        }

        /// <summary>
        /// 씬에 있는 모든 플레이어에 꽂는다. 프로젝트 부트스트랩에서 씬 로드 뒤에 부른다.
        ///
        /// <b>나중에 만들어지는 플레이어는 잡지 못한다.</b> 풀에서 꺼내거나 UiService가
        /// 로드하는 팝업이 그렇다. 그런 경우는 만들어지는 자리에서 <see cref="Apply"/>를
        /// 부르거나, <see cref="MotionDoTweenScope"/>를 프리팹에 붙인다.
        /// </summary>
        public static int ApplyToLoadedScenes()
        {
            if (!Application.isPlaying)
            {
                return 0;
            }

            // 6000.5부터 이 오버로드에 Obsolete가 붙었다. 정렬 인자가 없는 쪽을 쓰라는
            // 뜻인데 그것은 6000.5에 새로 생긴 오버로드라, 이 패키지가 선언한 최소
            // 버전(6000.0)에서는 아예 존재하지 않는다(6000.3에서 CS1503로 확인).
            // 최소 버전을 올릴 때까지는 이쪽이 유일하게 양쪽에서 컴파일되는 형태다.
#pragma warning disable 618
            MotionPlayer[] players = Object.FindObjectsByType<MotionPlayer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
#pragma warning restore 618

            for (int i = 0; i < players.Length; i++)
            {
                Apply(players[i]);
            }

            return players.Length;
        }
    }
}
