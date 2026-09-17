using System;

namespace CDG.UI.Editor
{
    /// <summary>
    /// UI Validation에서 발견한 하나의 문제를 나타냅니다.
    /// 문제 코드, 심각도, 설명, 관련 Unity Object와 선택적 Safe Fix를 함께 제공합니다.
    /// </summary>
    internal sealed class UIValidationIssue
    {
        internal string Code { get; }
        internal UIValidationSeverity Severity { get; }
        internal string Message { get; }
        internal UnityEngine.Object Context { get; }
        internal UIValidationFix Fix { get; }
        internal bool CanFix => Fix != null;

        internal UIValidationIssue(
            string code,
            UIValidationSeverity severity,
            string message,
            UnityEngine.Object context = null,
            UIValidationFix fix = null)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Validation issue code cannot be null or empty.", nameof(code));
            }

            Code = code;
            Severity = severity;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Context = context;
            Fix = fix;
        }
    }
}