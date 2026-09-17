using System.Collections;
using UnityEngine;

namespace CDG.UI
{
    /// <summary>
    /// CanvasGroup의 Alpha를 변경하여 UIView의 열기 및 닫기 Fade 효과를 제공합니다.
    /// Transition 시간은 Time.timeScale의 영향을 받지 않는 Unscaled Time을 기준으로 계산합니다.
    /// </summary>
    public sealed class UIFadeTransition : UIViewTransition
    {
        [SerializeField]
        [Min(0f)]
        private float openingDuration = 0.2f;

        [SerializeField]
        [Min(0f)]
        private float closingDuration = 0.2f;

        /// <summary>
        /// Fade In에 사용할 시간을 초 단위로 반환합니다.
        /// 0이면 Opening Transition을 즉시 완료합니다.
        /// </summary>
        public float OpeningDuration => openingDuration;

        /// <summary>
        /// Fade Out에 사용할 시간을 초 단위로 반환합니다.
        /// 0이면 Closing Transition을 즉시 완료합니다.
        /// </summary>
        public float ClosingDuration => closingDuration;

        protected override IEnumerator OnPlayOpening(CanvasGroup canvasGroup)
        {
            canvasGroup.alpha = 0f;

            if (openingDuration <= 0f)
            {
                canvasGroup.alpha = 1f;
                yield break;
            }

            float elapsedTime = 0f;

            while (elapsedTime < openingDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(
                    elapsedTime / openingDuration);

                canvasGroup.alpha = Mathf.Lerp(
                    0f,
                    1f,
                    progress);

                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        protected override IEnumerator OnPlayClosing(CanvasGroup canvasGroup)
        {
            float startAlpha = canvasGroup.alpha;

            if (closingDuration <= 0f)
            {
                canvasGroup.alpha = 0f;
                yield break;
            }

            float elapsedTime = 0f;

            while (elapsedTime < closingDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(
                    elapsedTime / closingDuration);

                canvasGroup.alpha = Mathf.Lerp(
                    startAlpha,
                    0f,
                    progress);

                yield return null;
            }

            canvasGroup.alpha = 0f;
        }

        internal void SetDurations(float openingDuration, float closingDuration)
        {
            this.openingDuration = Mathf.Max(
                0f,
                openingDuration);

            this.closingDuration = Mathf.Max(
                0f,
                closingDuration);
        }
    }
}