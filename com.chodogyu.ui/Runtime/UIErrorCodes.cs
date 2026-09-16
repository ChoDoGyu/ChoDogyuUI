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
    }
}