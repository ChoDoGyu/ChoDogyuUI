namespace CDG.UI
{
    /// <summary>
    /// UI Framework에서 사용하는 안정적인 오류 코드 모음입니다.
    /// 외부 코드에서는 오류 메시지보다 오류 코드를 기준으로 실패 원인을 구분할 수 있습니다.
    /// </summary>
    public static class UIErrorCodes
    {
        /// <summary>
        /// UI ID가 비어 있거나 유효하지 않을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string InvalidId = "UI_INVALID_ID";

        /// <summary>
        /// 요청한 UI ID가 Registry에 등록되어 있지 않을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string NotFound = "UI_NOT_FOUND";

        /// <summary>
        /// Registry에 null Prefab 또는 잘못된 항목이 포함되어 있을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string InvalidRegistry = "UI_INVALID_REGISTRY";

        /// <summary>
        /// Registry에 동일한 UI ID가 두 번 이상 등록되어 있을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string DuplicateId = "UI_DUPLICATE_ID";

        /// <summary>
        /// UIController에 Registry가 지정되지 않았을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string MissingRegistry = "UI_MISSING_REGISTRY";

        /// <summary>
        /// UI Runtime Instance를 배치할 Layer가 지정되지 않았을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string MissingLayer = "UI_MISSING_LAYER";

        /// <summary>
        /// 등록된 View 타입과 요청한 View 타입이 호환되지 않을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string InvalidType = "UI_INVALID_TYPE";

        /// <summary>
        /// UI Lifecycle과 입력 상태를 관리하는 데 필요한 CanvasGroup이 없을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string MissingCanvasGroup = "UI_MISSING_CANVAS_GROUP";

        /// <summary>
        /// 이미 완전히 열린 UI에 다시 Open을 요청했을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string AlreadyOpen = "UI_ALREADY_OPEN";

        /// <summary>
        /// 이미 완전히 닫힌 UI에 다시 Close를 요청했을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string AlreadyClosed = "UI_ALREADY_CLOSED";

        /// <summary>
        /// UI가 Opening 또는 Closing 상태여서 새로운 Lifecycle 요청을 처리할 수 없을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string Busy = "UI_BUSY";

        /// <summary>
        /// Blocking Overlay가 현재 Back 처리를 차단하고 있을 때 사용하는 오류 코드입니다.
        /// Overlay 자체는 Back으로 닫히지 않습니다.
        /// </summary>
        public const string BackBlocked = "UI_BACK_BLOCKED";

        /// <summary>
        /// 닫을 Popup이나 복원할 Screen History가 없어 Back으로 처리할 대상이 없을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string NoBackTarget = "UI_NO_BACK_TARGET";
    }
}