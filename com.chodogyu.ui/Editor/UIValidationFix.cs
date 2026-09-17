using System;
using CDG.Core.Results;

namespace CDG.UI.Editor
{
    /// <summary>
    /// Validation에서 발견한 문제에 대해 안전하게 적용할 수 있는 자동 수정 작업을 나타냅니다.
    /// 실제 수정 가능 여부는 각 Validator가 명확하게 결정할 수 있는 경우에만 제공합니다.
    /// </summary>
    internal sealed class UIValidationFix
    {
        private readonly Func<Result> applyAction;

        internal string Label { get; }

        internal UIValidationFix(string label, Func<Result> applyAction)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("Validation fix label cannot be null or empty.", nameof(label));
            }

            this.applyAction = applyAction ?? throw new ArgumentNullException(nameof(applyAction));
            Label = label;
        }

        /// <summary>
        /// 등록된 자동 수정 작업을 실행하고 결과를 반환합니다.
        /// </summary>
        internal Result Apply()
        {
            return applyAction();
        }
    }
}