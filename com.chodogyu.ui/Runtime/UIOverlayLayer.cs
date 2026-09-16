namespace CDG.UI
{
    /// <summary>
    /// Overlay가 Screen과 Popup 사이에서 어느 표시 계층에 배치될지 지정합니다.
    /// </summary>
    public enum UIOverlayLayer
    {
        /// <summary>
        /// Screen보다 위이면서 Popup보다 아래에 표시합니다.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Popup보다 위의 최상위 Overlay 계층에 표시합니다.
        /// </summary>
        Topmost = 1
    }
}