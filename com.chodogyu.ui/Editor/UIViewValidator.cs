using System;
using UnityEngine;

namespace CDG.UI.Editor
{
    /// <summary>
    /// UIView Prefab이 Runtime Lifecycle과 입력 제어에 필요한 기본 구조와
    /// 지원되는 View 타입 구성을 갖추었는지 검사합니다.
    /// </summary>
    internal static class UIViewValidator
    {
        /// <summary>
        /// 지정한 UIView의 기본 Prefab 구조와 타입 구성을 검사합니다.
        /// 하나의 문제가 발견되어도 나머지 항목을 계속 검사합니다.
        /// </summary>
        /// <param name="view">검사할 UIView입니다.</param>
        /// <returns>View에서 발견한 모든 문제를 포함하는 Validation Report입니다.</returns>
        internal static UIValidationReport Validate(UIView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            UIValidationReport report = new UIValidationReport();

            ValidateStructure(view, report);
            ValidateViewType(view, report);

            return report;
        }

        private static void ValidateStructure(UIView view, UIValidationReport report)
        {
            if (view.GetComponent<CanvasGroup>() == null)
            {
                report.Add(new UIValidationIssue(
                    UIValidationCodes.ViewMissingCanvasGroup,
                    UIValidationSeverity.Error,
                    $"'{view.name}' View에 CanvasGroup이 없습니다.",
                    view,
                    UIValidationFixUtility.CreateAddCanvasGroupFix(view)));
            }

            if (view.gameObject.activeSelf)
            {
                report.Add(new UIValidationIssue(
                    UIValidationCodes.ViewRootActive,
                    UIValidationSeverity.Error,
                    $"'{view.name}' View Prefab은 기본 비활성 상태여야 합니다.",
                    view,
                    UIValidationFixUtility.CreateDeactivateRootFix(view)));
            }

            if (view.transform.parent != null)
            {
                report.Add(new UIValidationIssue(
                    UIValidationCodes.ViewNotPrefabRoot,
                    UIValidationSeverity.Error,
                    $"'{view.name}' UIView Component는 Prefab Root에 있어야 합니다.",
                    view));
            }
        }

        private static void ValidateViewType(UIView view, UIValidationReport report)
        {
            if (view is UIScreen || view is UIPopup)
            {
                return;
            }

            if (view is UIOverlay overlay)
            {
                ValidateOverlayLayer(overlay, report);
                return;
            }

            report.Add(new UIValidationIssue(
                UIValidationCodes.ViewUnsupportedType,
                UIValidationSeverity.Error,
                $"'{view.name}' View 타입 '{view.GetType().Name}'은 지원되는 Screen, Popup, Overlay 타입이 아닙니다.",
                view));
        }

        private static void ValidateOverlayLayer(UIOverlay overlay, UIValidationReport report)
        {
            if (Enum.IsDefined(typeof(UIOverlayLayer), overlay.OverlayLayer))
            {
                return;
            }

            report.Add(new UIValidationIssue(
                UIValidationCodes.OverlayInvalidLayer,
                UIValidationSeverity.Error,
                $"'{overlay.name}' Overlay에 유효하지 않은 Overlay Layer 값이 지정되어 있습니다: '{(int)overlay.OverlayLayer}'",
                overlay));
        }
    }
}