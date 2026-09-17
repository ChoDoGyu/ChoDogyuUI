namespace CDG.UI.Editor
{
    /// <summary>
    /// UI Framework Editor 기능에서 공통으로 사용하는 메뉴 경로와 기본 이름을 정의합니다.
    /// Root 생성, Prefab 생성 및 Validation Tool이 동일한 이름 규칙을 공유하도록 합니다.
    /// </summary>
    internal static class UIEditorConstants
    {
        internal const string MenuRoot = "Tools/ChoDogyu/UI Framework";
        internal const string OpenWindowMenuPath = MenuRoot + "/Open Window";

        internal const string WindowTitle = "CDG UI Framework";

        internal const string RootObjectName = "CDG UI Root";
        internal const string ScreenLayerName = "Screen Layer";
        internal const string OverlayLayerName = "Overlay Layer";
        internal const string PopupLayerName = "Popup Layer";
        internal const string TopOverlayLayerName = "Top Overlay Layer";

        internal const string DefaultRegistryAssetName = "UIRegistry.asset";
    }
}