using System.Collections;
using UnityEngine;

namespace CDG.UI
{
    /// <summary>
    /// UIView의 열기 및 닫기 시각 전환 효과를 정의하는 기본 클래스입니다.
    /// 사용자 정의 Transition은 이 클래스를 상속하고 Opening과 Closing Coroutine을 구현할 수 있습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class UIViewTransition : MonoBehaviour
    {
        /// <summary>
        /// View가 Opening 상태가 된 후 실행할 전환 효과를 구현합니다.
        /// Framework가 관리하는 CanvasGroup을 사용하며 입력 활성화 여부는 변경하지 않아야 합니다.
        /// </summary>
        /// <param name="canvasGroup">전환 효과를 적용할 View의 CanvasGroup입니다.</param>
        protected abstract IEnumerator OnPlayOpening(CanvasGroup canvasGroup);

        /// <summary>
        /// View가 Closing 상태가 된 후 실행할 전환 효과를 구현합니다.
        /// Framework가 관리하는 CanvasGroup을 사용하며 입력 활성화 여부는 변경하지 않아야 합니다.
        /// </summary>
        /// <param name="canvasGroup">전환 효과를 적용할 View의 CanvasGroup입니다.</param>
        protected abstract IEnumerator OnPlayClosing(CanvasGroup canvasGroup);

        internal IEnumerator PlayOpening(CanvasGroup canvasGroup)
        {
            IEnumerator routine = OnPlayOpening(canvasGroup);

            if (routine == null)
            {
                yield break;
            }

            while (routine.MoveNext())
            {
                yield return routine.Current;
            }
        }

        internal IEnumerator PlayClosing(CanvasGroup canvasGroup)
        {
            IEnumerator routine = OnPlayClosing(canvasGroup);

            if (routine == null)
            {
                yield break;
            }

            while (routine.MoveNext())
            {
                yield return routine.Current;
            }
        }
    }
}