namespace CDG.UI.Editor
{
    /// <summary>
    /// UI Framework Editor 기능에서 발생하는 실패 원인을 식별하기 위한 오류 코드를 정의합니다.
    /// Runtime 오류 코드와 분리하여 Editor Tool 전용 실패를 명확하게 구분합니다.
    /// </summary>
    internal static class UIEditorErrorCodes
    {
        internal const string RootAlreadyExists = "CDG.UI.Editor.RootAlreadyExists";
        internal const string InvalidAssetPath = "CDG.UI.Editor.InvalidAssetPath";
        internal const string AssetAlreadyExists = "CDG.UI.Editor.AssetAlreadyExists";
    }
}