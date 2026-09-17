namespace CDG.UI.Editor
{
    /// <summary>
    /// UI Framework Validation에서 발견한 문제를 식별하기 위한 안정적인 코드를 정의합니다.
    /// </summary>
    internal static class UIValidationCodes
    {
        internal const string RegistryMissingPrefab = "CDG.UI.Validation.RegistryMissingPrefab";
        internal const string RegistryBlankId = "CDG.UI.Validation.RegistryBlankId";
        internal const string RegistryDuplicateId = "CDG.UI.Validation.RegistryDuplicateId";

        internal const string ViewMissingCanvasGroup = "CDG.UI.Validation.ViewMissingCanvasGroup";
        internal const string ViewRootActive = "CDG.UI.Validation.ViewRootActive";
        internal const string ViewNotPrefabRoot = "CDG.UI.Validation.ViewNotPrefabRoot";
        internal const string ViewUnsupportedType = "CDG.UI.Validation.ViewUnsupportedType";
        internal const string OverlayInvalidLayer = "CDG.UI.Validation.OverlayInvalidLayer";

        internal const string TransitionNotOnViewRoot = "CDG.UI.Validation.TransitionNotOnViewRoot";
        internal const string FadeOpeningDurationNegative = "CDG.UI.Validation.FadeOpeningDurationNegative";
        internal const string FadeClosingDurationNegative = "CDG.UI.Validation.FadeClosingDurationNegative";

        internal const string ControllerMissingRegistry = "CDG.UI.Validation.ControllerMissingRegistry";
        internal const string ControllerMissingLayer = "CDG.UI.Validation.ControllerMissingLayer";
        internal const string ControllerDuplicateLayer = "CDG.UI.Validation.ControllerDuplicateLayer";
    }
}