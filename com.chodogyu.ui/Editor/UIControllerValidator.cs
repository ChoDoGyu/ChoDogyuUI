using System;
using System.Collections.Generic;
using UnityEngine;

namespace CDG.UI.Editor
{
    /// <summary>
    /// UIController의 Registry와 Runtime Layer 참조 구성을 검사합니다.
    /// View 생성에 필요한 필수 참조 누락과 Layer 중복 연결을 보고합니다.
    /// </summary>
    internal static class UIControllerValidator
    {
        /// <summary>
        /// 지정한 UIController의 Registry 및 Layer 구성을 검사합니다.
        /// 하나의 문제가 발견되어도 나머지 항목을 계속 검사합니다.
        /// </summary>
        /// <param name="controller">검사할 UIController입니다.</param>
        /// <returns>발견한 모든 Controller 구성 문제를 포함하는 Validation Report입니다.</returns>
        internal static UIValidationReport Validate(UIController controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            UIValidationReport report = new UIValidationReport();

            ValidateRegistry(controller, report);
            ValidateLayers(controller, report);

            return report;
        }

        private static void ValidateRegistry(UIController controller, UIValidationReport report)
        {
            if (controller.Registry != null)
            {
                return;
            }

            report.Add(new UIValidationIssue(
                UIValidationCodes.ControllerMissingRegistry,
                UIValidationSeverity.Error,
                $"'{controller.name}' UIController에 UIRegistry가 지정되지 않았습니다.",
                controller));
        }

        private static void ValidateLayers(UIController controller, UIValidationReport report)
        {
            Transform[] layers =
            {
                controller.ScreenLayer,
                controller.OverlayLayer,
                controller.PopupLayer,
                controller.TopOverlayLayer
            };

            string[] layerNames =
            {
                "Screen Layer",
                "Overlay Layer",
                "Popup Layer",
                "Top Overlay Layer"
            };

            HashSet<Transform> uniqueLayers = new HashSet<Transform>();
            bool hasDuplicate = false;

            for (int i = 0; i < layers.Length; i++)
            {
                Transform layer = layers[i];

                if (layer == null)
                {
                    report.Add(new UIValidationIssue(
                        UIValidationCodes.ControllerMissingLayer,
                        UIValidationSeverity.Error,
                        $"'{controller.name}' UIController에 {layerNames[i]}가 지정되지 않았습니다.",
                        controller));

                    continue;
                }

                if (!uniqueLayers.Add(layer))
                {
                    hasDuplicate = true;
                }
            }

            if (hasDuplicate)
            {
                report.Add(new UIValidationIssue(
                    UIValidationCodes.ControllerDuplicateLayer,
                    UIValidationSeverity.Error,
                    $"'{controller.name}' UIController의 Layer 참조는 서로 다른 Transform이어야 합니다.",
                    controller));
            }
        }
    }
}