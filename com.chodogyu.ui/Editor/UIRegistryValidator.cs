using System;
using System.Collections.Generic;

namespace CDG.UI.Editor
{
    /// <summary>
    /// UIRegistry의 Prefab 등록 상태를 검사합니다.
    /// 누락된 Prefab, 비어 있는 UI ID, 중복 UI ID를 모두 수집하여 Validation Report로 반환합니다.
    /// </summary>
    internal static class UIRegistryValidator
    {
        /// <summary>
        /// 지정한 Registry의 모든 등록 항목을 검사합니다.
        /// 하나의 문제가 발견되어도 검사를 중단하지 않고 나머지 항목까지 계속 확인합니다.
        /// </summary>
        /// <param name="registry">검사할 UIRegistry입니다.</param>
        /// <returns>Registry에서 발견한 모든 문제를 포함하는 Validation Report입니다.</returns>
        internal static UIValidationReport Validate(UIRegistry registry)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            UIValidationReport report = new UIValidationReport();
            Dictionary<UIId, UIView> viewsById = new Dictionary<UIId, UIView>();

            for (int i = 0; i < registry.Prefabs.Count; i++)
            {
                UIView prefab = registry.Prefabs[i];

                if (prefab == null)
                {
                    report.Add(new UIValidationIssue(
                        UIValidationCodes.RegistryMissingPrefab,
                        UIValidationSeverity.Error,
                        $"Registry의 {i}번 항목에 Prefab이 지정되지 않았습니다.",
                        registry));

                    continue;
                }

                if (prefab.Id.IsEmpty)
                {
                    report.Add(new UIValidationIssue(
                        UIValidationCodes.RegistryBlankId,
                        UIValidationSeverity.Error,
                        $"Registry의 '{prefab.name}' Prefab에 UI ID가 지정되지 않았습니다.",
                        prefab));

                    continue;
                }

                if (!viewsById.TryAdd(prefab.Id, prefab))
                {
                    report.Add(new UIValidationIssue(
                        UIValidationCodes.RegistryDuplicateId,
                        UIValidationSeverity.Error,
                        $"Registry에 중복된 UI ID가 등록되어 있습니다: '{prefab.Id}'",
                        prefab));
                }
            }

            return report;
        }
    }
}