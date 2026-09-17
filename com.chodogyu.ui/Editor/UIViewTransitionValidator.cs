using System;

namespace CDG.UI.Editor
{
    /// <summary>
    /// UIView에 연결된 Transition의 배치와 설정값을 검사합니다.
    /// Runtime에서 무시되는 자식 Transition과 잘못된 Fade Duration을 보고합니다.
    /// </summary>
    internal static class UIViewTransitionValidator
    {
        /// <summary>
        /// 지정한 View의 Transition 구성을 검사합니다.
        /// Transition이 없는 View는 유효한 구성으로 간주합니다.
        /// </summary>
        /// <param name="view">Transition 구성을 검사할 UIView입니다.</param>
        /// <returns>발견한 모든 Transition 문제를 포함하는 Validation Report입니다.</returns>
        internal static UIValidationReport Validate(UIView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            UIValidationReport report = new UIValidationReport();

            ValidateTransitionPlacement(view, report);

            UIViewTransition rootTransition = view.GetComponent<UIViewTransition>();

            if (rootTransition is UIFadeTransition fadeTransition)
            {
                ValidateFadeTransition(fadeTransition, report);
            }

            return report;
        }

        private static void ValidateTransitionPlacement(UIView view, UIValidationReport report)
        {
            UIViewTransition[] transitions = view.GetComponentsInChildren<UIViewTransition>(true);

            foreach (UIViewTransition transition in transitions)
            {
                if (transition.gameObject == view.gameObject)
                {
                    continue;
                }

                report.Add(new UIValidationIssue(
                    UIValidationCodes.TransitionNotOnViewRoot,
                    UIValidationSeverity.Warning,
                    $"'{transition.name}' Transition은 UIView와 같은 Prefab Root에 있어야 Runtime에서 사용됩니다.",
                    transition));
            }
        }

        private static void ValidateFadeTransition(UIFadeTransition fadeTransition, UIValidationReport report)
        {
            if (fadeTransition.OpeningDuration < 0f)
            {
                report.Add(new UIValidationIssue(
                    UIValidationCodes.FadeOpeningDurationNegative,
                    UIValidationSeverity.Warning,
                    $"'{fadeTransition.name}' Fade Transition의 Opening Duration은 0 이상이어야 합니다.",
                    fadeTransition,
                    UIValidationFixUtility.CreateClampOpeningDurationFix(fadeTransition)));
            }

            if (fadeTransition.ClosingDuration < 0f)
            {
                report.Add(new UIValidationIssue(
                    UIValidationCodes.FadeClosingDurationNegative,
                    UIValidationSeverity.Warning,
                    $"'{fadeTransition.name}' Fade Transition의 Closing Duration은 0 이상이어야 합니다.",
                    fadeTransition,
                    UIValidationFixUtility.CreateClampClosingDurationFix(fadeTransition)));
            }
        }
    }
}