using System;
using System.Collections.Generic;

namespace CDG.UI.Editor
{
    /// <summary>
    /// 개별 UI Validator를 조율하여 Controller 또는 Registry 기준의 전체 Validation을 실행합니다.
    /// Registry에 등록된 각 View는 중복 참조 여부와 관계없이 한 번만 구조 및 Transition 검사를 수행합니다.
    /// </summary>
    internal static class UIValidationRunner
    {
        /// <summary>
        /// UIController와 연결된 Registry, View Prefab, Transition까지 전체 구성을 검사합니다.
        /// Registry가 지정되지 않은 경우 Controller 문제만 보고합니다.
        /// </summary>
        internal static UIValidationReport Validate(UIController controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            UIValidationReport report = UIControllerValidator.Validate(controller);

            if (controller.Registry != null)
            {
                report.AddRange(ValidateRegistryAndViews(controller.Registry));
            }

            return report;
        }

        /// <summary>
        /// UIRegistry와 Registry에 등록된 모든 View Prefab 및 Transition을 검사합니다.
        /// </summary>
        internal static UIValidationReport Validate(UIRegistry registry)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            return ValidateRegistryAndViews(registry);
        }

        private static UIValidationReport ValidateRegistryAndViews(UIRegistry registry)
        {
            UIValidationReport report = UIRegistryValidator.Validate(registry);
            HashSet<UIView> validatedViews = new HashSet<UIView>();

            foreach (UIView view in registry.Prefabs)
            {
                if (view == null || !validatedViews.Add(view))
                {
                    continue;
                }

                report.AddRange(UIViewValidator.Validate(view));
                report.AddRange(UIViewTransitionValidator.Validate(view));
            }

            return report;
        }
    }
}