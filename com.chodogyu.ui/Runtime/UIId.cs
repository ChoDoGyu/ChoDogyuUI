using System;
using UnityEngine;

namespace CDG.UI
{
    /// <summary>
    /// UI를 Registry와 Runtime에서 식별하기 위한 고유 ID입니다.
    /// 빈 문자열은 유효한 UI ID로 취급하지 않습니다.
    /// </summary>
    [Serializable]
    public struct UIId : IEquatable<UIId>
    {
        [SerializeField]
        private string value;

        /// <summary>
        /// ID의 문자열 값을 반환합니다.
        /// </summary>
        public string Value => value ?? string.Empty;

        /// <summary>
        /// 현재 ID가 비어 있는지 여부를 반환합니다.
        /// </summary>
        public bool IsEmpty => string.IsNullOrWhiteSpace(value);

        /// <summary>
        /// 지정된 문자열로 UI ID를 생성합니다.
        /// 문자열 정규화나 자동 변경은 수행하지 않습니다.
        /// </summary>
        public UIId(string value)
        {
            this.value = value;
        }

        /// <inheritdoc/>
        public bool Equals(UIId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is UIId other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(UIId left, UIId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UIId left, UIId right)
        {
            return !left.Equals(right);
        }
    }
}