namespace CDG.UI
{
    /// <summary>
    /// 현재 열린 UI의 표시 우선순위와 입력 차단 정책을 기준으로 각 View의 입력 가능 상태를 계산합니다.
    /// Topmost Overlay, Popup, Normal Overlay, Screen 순서로 평가하여 상위 Blocking View 아래의 입력을 차단합니다.
    /// </summary>
    internal sealed class UIInputCoordinator
    {
        internal void Refresh(UIInstanceStore instanceStore, UIPopupStack popupStack, UIOverlayOrder overlayOrder, UIScreen currentScreen)
        {
            popupStack.Cleanup(instanceStore);
            overlayOrder.Cleanup(instanceStore);

            bool lowerInputAllowed = true;

            ApplyOverlayInputState(
                UIOverlayLayer.Topmost,
                instanceStore,
                overlayOrder,
                ref lowerInputAllowed);

            ApplyPopupInputState(
                instanceStore,
                popupStack,
                ref lowerInputAllowed);

            ApplyOverlayInputState(
                UIOverlayLayer.Normal,
                instanceStore,
                overlayOrder,
                ref lowerInputAllowed);

            if (currentScreen != null && currentScreen.State != UIViewState.Closed)
            {
                ApplyViewInputState(
                    currentScreen,
                    false,
                    ref lowerInputAllowed);
            }
        }

        private void ApplyOverlayInputState(UIOverlayLayer layer, UIInstanceStore instanceStore, UIOverlayOrder overlayOrder, ref bool lowerInputAllowed)
        {
            foreach (UIId id in overlayOrder.EnumerateTopToBottom(layer, instanceStore))
            {
                if (!instanceStore.TryGet(id, out UIView instance))
                {
                    continue;
                }

                if (instance is not UIOverlay overlay)
                {
                    continue;
                }

                ApplyViewInputState(
                    overlay,
                    overlay.BlocksInput,
                    ref lowerInputAllowed);
            }
        }

        private void ApplyPopupInputState(UIInstanceStore instanceStore, UIPopupStack popupStack, ref bool lowerInputAllowed)
        {
            foreach (UIId id in popupStack.EnumerateTopToBottom())
            {
                if (!instanceStore.TryGet(id, out UIView instance))
                {
                    continue;
                }

                if (instance is not UIPopup popup)
                {
                    continue;
                }

                ApplyViewInputState(
                    popup,
                    popup.BlocksInput,
                    ref lowerInputAllowed);
            }
        }

        private void ApplyViewInputState(UIView view, bool blocksLowerInput, ref bool lowerInputAllowed)
        {
            bool inputEnabled =
                lowerInputAllowed &&
                view.State == UIViewState.Open;

            view.SetInputEnabled(inputEnabled);

            if (lowerInputAllowed &&
                view.State != UIViewState.Closed &&
                blocksLowerInput)
            {
                lowerInputAllowed = false;
            }
        }
    }
}