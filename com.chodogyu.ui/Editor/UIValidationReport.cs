using System;
using System.Collections.Generic;

namespace CDG.UI.Editor
{
    /// <summary>
    /// 하나의 Validation 실행에서 발견한 문제 목록을 보관합니다.
    /// 외부에서는 읽기 전용으로 조회하며 Validator만 문제를 추가할 수 있습니다.
    /// </summary>
    internal sealed class UIValidationReport
    {
        private readonly List<UIValidationIssue> issues = new List<UIValidationIssue>();

        internal int Count => issues.Count;
        internal IReadOnlyList<UIValidationIssue> Issues => issues;

        /// <summary>
        /// 하나 이상의 Error 심각도 문제가 포함되어 있는지 여부를 반환합니다.
        /// </summary>
        internal bool HasErrors
        {
            get
            {
                foreach (UIValidationIssue issue in issues)
                {
                    if (issue.Severity == UIValidationSeverity.Error)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal void Add(UIValidationIssue issue)
        {
            if (issue == null)
            {
                throw new ArgumentNullException(nameof(issue));
            }

            issues.Add(issue);
        }

        internal void AddRange(UIValidationReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            issues.AddRange(report.issues);
        }
    }
}