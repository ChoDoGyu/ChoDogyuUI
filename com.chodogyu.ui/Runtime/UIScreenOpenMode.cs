namespace CDG.UI
{
    /// <summary>
    /// 새로운 Screen을 열 때 기존 Screen History를 어떻게 처리할지 지정합니다.
    /// </summary>
    public enum UIScreenOpenMode
    {
        /// <summary>
        /// 현재 Screen을 History에 보관하고 새로운 Screen을 엽니다.
        /// </summary>
        Push = 0,

        /// <summary>
        /// 현재 Screen을 History에 추가하지 않고 새로운 Screen으로 교체합니다.
        /// 기존 History는 유지합니다.
        /// </summary>
        Replace = 1,

        /// <summary>
        /// 기존 Screen History를 모두 제거하고 새로운 Screen을 엽니다.
        /// </summary>
        Reset = 2
    }
}