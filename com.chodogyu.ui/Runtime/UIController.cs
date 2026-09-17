using System;
using CDG.Core.Results;
using UnityEngine;

namespace CDG.UI
{
    /// <summary>
    /// Registry에 등록된 UI의 Runtime Instance와 화면 흐름을 관리하는 중심 Controller입니다.
    /// Runtime 관리 구성 요소와 Lifecycle Coordinator를 조율하고 외부에 일관된 UI 제어 진입점을 제공합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIController : MonoBehaviour
    {
        [SerializeField]
        private UIRegistry registry;

        [SerializeField]
        private Transform screenLayer;

        [SerializeField]
        private Transform overlayLayer;

        [SerializeField]
        private Transform popupLayer;

        [SerializeField]
        private Transform topOverlayLayer;

        private readonly UIInstanceStore instanceStore = new UIInstanceStore();
        private readonly UIScreenNavigator screenNavigator = new UIScreenNavigator();
        private readonly UIPopupStack popupStack = new UIPopupStack();
        private readonly UIOverlayOrder overlayOrder = new UIOverlayOrder();
        private readonly UIInputCoordinator inputCoordinator = new UIInputCoordinator();
        private readonly UIViewLifecycleCoordinator lifecycleCoordinator = new UIViewLifecycleCoordinator();

        /// <summary>
        /// Controller가 UI Prefab 조회에 사용하는 Registry를 반환합니다.
        /// </summary>
        public UIRegistry Registry => registry;

        /// <summary>
        /// Screen Runtime Instance가 배치되는 Transform을 반환합니다.
        /// </summary>
        public Transform ScreenLayer => screenLayer;

        /// <summary>
        /// 일반 Overlay Runtime Instance가 배치되는 Transform을 반환합니다.
        /// </summary>
        public Transform OverlayLayer => overlayLayer;

        /// <summary>
        /// Popup Runtime Instance가 배치되는 Transform을 반환합니다.
        /// </summary>
        public Transform PopupLayer => popupLayer;

        /// <summary>
        /// 최상위 Overlay Runtime Instance가 배치되는 Transform을 반환합니다.
        /// </summary>
        public Transform TopOverlayLayer => topOverlayLayer;

        /// <summary>
        /// 현재 Screen Navigation이 관리하는 Screen을 반환합니다.
        /// Transition 중에는 Closing 또는 Opening 상태의 Screen이 반환될 수 있습니다.
        /// </summary>
        public UIScreen CurrentScreen => screenNavigator.CurrentScreen;

        /// <summary>
        /// Back으로 복원할 수 있도록 보관 중인 이전 Screen의 개수를 반환합니다.
        /// History 컬렉션 자체는 외부에 노출하지 않습니다.
        /// </summary>
        public int ScreenHistoryCount => screenNavigator.HistoryCount;

        /// <summary>
        /// Screen Navigation 또는 하나 이상의 UI Transition이 현재 진행 중인지 여부를 반환합니다.
        /// Transition이 없는 즉시 Lifecycle 처리는 Busy 상태로 남지 않습니다.
        /// </summary>
        public bool IsBusy => screenNavigator.IsBusy || lifecycleCoordinator.IsBusy;

        /// <summary>
        /// 현재 Popup Stack의 최상단 Popup을 반환합니다.
        /// 유효한 Popup이 없으면 null을 반환하며, 파괴되거나 이미 닫힌 항목은 자동으로 Stack에서 정리합니다.
        /// </summary>
        public UIPopup TopPopup
        {
            get
            {
                CleanupRuntimeCollectionsAndRefreshInput();

                if (!popupStack.TryPeek(out UIId topId))
                {
                    return null;
                }

                return instanceStore.TryGet(topId, out UIView instance)
                    ? instance as UIPopup
                    : null;
            }
        }

        /// <summary>
        /// 현재 Popup Stack에 존재하는 유효한 Popup의 개수를 반환합니다.
        /// Closing 중인 Popup은 Transition 완료 전까지 Stack에 유지됩니다.
        /// </summary>
        public int PopupCount
        {
            get
            {
                CleanupRuntimeCollectionsAndRefreshInput();
                return popupStack.Count;
            }
        }

        /// <summary>
        /// 현재 UI 상태에서 Back 요청을 처리할 수 있는지 여부를 반환합니다.
        /// Blocking Overlay나 Lifecycle 또는 Screen Navigation 전환 중에는 처리 가능 여부가 제한됩니다.
        /// </summary>
        public bool CanBack
        {
            get
            {
                CleanupRuntimeCollectionsAndRefreshInput();

                if (overlayOrder.HasBlockingOverlay(
                    UIOverlayLayer.Topmost,
                    instanceStore))
                {
                    return false;
                }

                if (popupStack.TryPeek(out UIId popupId))
                {
                    if (!instanceStore.TryGet(popupId, out UIView popupInstance))
                    {
                        return false;
                    }

                    if (popupInstance is not UIPopup popup)
                    {
                        return false;
                    }

                    return popup.State == UIViewState.Open;
                }

                if (overlayOrder.HasBlockingOverlay(
                    UIOverlayLayer.Normal,
                    instanceStore))
                {
                    return false;
                }

                if (screenNavigator.IsBusy)
                {
                    return false;
                }

                if (screenNavigator.HistoryCount == 0)
                {
                    return false;
                }

                UIScreen currentScreen = screenNavigator.CurrentScreen;

                return currentScreen == null ||
                    currentScreen.State == UIViewState.Open;
            }
        }

        /// <summary>
        /// 현재 UI 상태의 우선순위에 따라 Back 요청을 처리합니다.
        /// Blocking Overlay는 Back 전달을 차단하고, Popup이 있으면 최상단 Popup을 닫으며,
        /// 그 외에는 Screen History의 이전 Screen을 복원합니다.
        /// </summary>
        /// <returns>Back 처리 시작 성공 또는 처리할 수 없는 원인을 포함하는 결과입니다.</returns>
        public Result Back()
        {
            CleanupRuntimeCollectionsAndRefreshInput();

            if (overlayOrder.HasBlockingOverlay(
                UIOverlayLayer.Topmost,
                instanceStore))
            {
                return Result.Failure(new ResultError(
                    UIErrorCodes.BackBlocked,
                    "Topmost Overlay가 현재 Back 전달을 차단하고 있습니다."));
            }

            if (popupStack.TryPeek(out UIId popupId))
            {
                if (!instanceStore.TryGet(popupId, out UIView popupInstance) ||
                    popupInstance is not UIPopup popup)
                {
                    return Result.Failure(new ResultError(
                        UIErrorCodes.NoBackTarget,
                        "Back으로 처리할 유효한 Popup을 찾을 수 없습니다."));
                }

                if (popup.IsTransitioning)
                {
                    return Result.Failure(new ResultError(
                        UIErrorCodes.Busy,
                        $"Popup '{popup.Id}'는 현재 {popup.State} 상태이므로 Back 요청을 처리할 수 없습니다."));
                }

                return Close(popup.Id);
            }

            if (overlayOrder.HasBlockingOverlay(
                UIOverlayLayer.Normal,
                instanceStore))
            {
                return Result.Failure(new ResultError(
                    UIErrorCodes.BackBlocked,
                    "Normal Overlay가 현재 Screen History로의 Back 전달을 차단하고 있습니다."));
            }

            if (screenNavigator.IsBusy)
            {
                return Result.Failure(new ResultError(
                    UIErrorCodes.Busy,
                    "Screen Navigation이 진행 중이므로 Back 요청을 처리할 수 없습니다."));
            }

            if (screenNavigator.HistoryCount == 0)
            {
                return Result.Failure(new ResultError(
                    UIErrorCodes.NoBackTarget,
                    "Back으로 닫을 Popup이나 복원할 Screen History가 없습니다."));
            }

            Result restoreResult = screenNavigator.RestorePrevious(this);

            if (restoreResult.IsSuccess)
            {
                RefreshInputState();
            }

            return restoreResult;
        }

        /// <summary>
        /// 현재 Screen을 History에 보존하는 Push 방식으로 지정한 Screen을 엽니다.
        /// 현재 Screen이 없다면 History를 변경하지 않고 대상 Screen을 최초 Screen으로 엽니다.
        /// </summary>
        /// <param name="id">열 Screen의 UI ID입니다.</param>
        /// <returns>Screen Navigation 시작 결과입니다.</returns>
        public Result<UIScreen> OpenScreen(UIId id)
        {
            return OpenScreen(id, UIScreenOpenMode.Push);
        }

        /// <summary>
        /// 지정한 Navigation 방식으로 Screen을 엽니다.
        /// Transition이 존재하는 경우 요청 성공은 Navigation이 정상적으로 시작되었음을 의미합니다.
        /// </summary>
        /// <param name="id">열 Screen의 UI ID입니다.</param>
        /// <param name="mode">적용할 Screen Navigation 방식입니다.</param>
        /// <returns>대상 Screen 또는 Navigation 시작 실패 정보를 포함하는 결과입니다.</returns>
        public Result<UIScreen> OpenScreen(UIId id, UIScreenOpenMode mode)
        {
            Result<UIScreen> result = screenNavigator.Open(
                this,
                id,
                mode);

            if (result.IsSuccess)
            {
                RefreshInputState();
            }

            return result;
        }

        /// <summary>
        /// 지정한 Popup을 열고 Popup Stack의 최상단에 추가합니다.
        /// Transition이 존재하는 경우 Popup은 Opening 상태로 Stack에 먼저 등록됩니다.
        /// </summary>
        /// <param name="id">열 Popup의 UI ID입니다.</param>
        /// <returns>Popup 열기 시작 결과입니다.</returns>
        public Result<UIPopup> OpenPopup(UIId id)
        {
            Result<UIPopup> openResult = OpenView<UIPopup>(
                id,
                RefreshInputState);

            if (openResult.IsFailure)
            {
                return Result<UIPopup>.Failure(openResult.Error);
            }

            UIPopup popup = openResult.Value;

            popup.transform.SetAsLastSibling();
            popupStack.Push(popup.Id);

            RefreshInputState();

            return Result<UIPopup>.Success(popup);
        }

        /// <summary>
        /// 지정한 Overlay를 엽니다.
        /// Overlay 설정에 따라 Normal 또는 Topmost Layer에 배치되며 Transition 중에도 열린 순서를 유지합니다.
        /// </summary>
        /// <param name="id">열 Overlay의 UI ID입니다.</param>
        /// <returns>Overlay 열기 시작 결과입니다.</returns>
        public Result<UIOverlay> OpenOverlay(UIId id)
        {
            overlayOrder.Cleanup(instanceStore);

            Result<UIOverlay> openResult = OpenView<UIOverlay>(
                id,
                RefreshInputState);

            if (openResult.IsFailure)
            {
                return Result<UIOverlay>.Failure(openResult.Error);
            }

            UIOverlay overlay = openResult.Value;

            overlay.transform.SetAsLastSibling();
            overlayOrder.Add(overlay.Id);

            RefreshInputState();

            return Result<UIOverlay>.Success(overlay);
        }

        /// <summary>
        /// 지정한 UI의 Runtime Instance를 닫습니다.
        /// Transition이 존재하는 Popup과 Overlay는 Closing 완료 전까지 Runtime 관리 목록에 유지됩니다.
        /// </summary>
        /// <param name="id">닫을 UI의 ID입니다.</param>
        /// <returns>닫기 시작 성공 또는 실패 정보를 포함하는 결과입니다.</returns>
        public Result Close(UIId id)
        {
            overlayOrder.Cleanup(instanceStore);

            if (id.IsEmpty)
            {
                return Result.Failure(new ResultError(
                    UIErrorCodes.InvalidId,
                    "UI ID는 비어 있을 수 없습니다."));
            }

            if (!TryGetCachedInstance(id, out UIView instance))
            {
                if (registry == null)
                {
                    return Result.Failure(new ResultError(
                        UIErrorCodes.MissingRegistry,
                        "UIController에 UIRegistry가 지정되지 않았습니다."));
                }

                Result<UIView> prefabResult = registry.Get(id);

                if (prefabResult.IsFailure)
                {
                    return Result.Failure(prefabResult.Error);
                }

                return Result.Failure(new ResultError(
                    UIErrorCodes.AlreadyClosed,
                    $"UI '{id}'는 이미 닫힌 상태입니다."));
            }

            Result closeResult = CloseView(
                instance,
                () =>
                {
                    if (instance is UIPopup)
                    {
                        popupStack.Remove(id);
                    }

                    if (instance is UIOverlay)
                    {
                        overlayOrder.Remove(id);
                    }

                    if (instance is UIScreen screen)
                    {
                        screenNavigator.HandleClosedScreen(screen);
                    }

                    RefreshInputState();
                });

            if (closeResult.IsFailure)
            {
                return closeResult;
            }

            RefreshInputState();

            return Result.Success();
        }

        /// <summary>
        /// 지정한 UI가 완전히 열린 상태인지 확인합니다.
        /// Opening 상태는 false로 처리합니다.
        /// </summary>
        /// <param name="id">확인할 UI ID입니다.</param>
        /// <returns>해당 UI의 Runtime Instance가 존재하고 Open 상태이면 true입니다.</returns>
        public bool IsOpen(UIId id)
        {
            if (id.IsEmpty)
            {
                return false;
            }

            if (!TryGetCachedInstance(id, out UIView instance))
            {
                return false;
            }

            return instance.IsOpen;
        }

        /// <summary>
        /// 지정한 UI ID의 Runtime Instance를 반환합니다.
        /// 아직 생성되지 않았다면 Registry의 Prefab을 해당 UI Layer에 생성하고 이후 요청에서 재사용합니다.
        /// </summary>
        /// <typeparam name="T">요청할 UIView 타입입니다.</typeparam>
        /// <param name="id">조회하거나 생성할 UI ID입니다.</param>
        /// <returns>Runtime Instance 또는 생성 실패 정보를 포함하는 결과입니다.</returns>
        public Result<T> GetOrCreate<T>(UIId id) where T : UIView
        {
            if (id.IsEmpty)
            {
                return Result<T>.Failure(new ResultError(
                    UIErrorCodes.InvalidId,
                    "UI ID는 비어 있을 수 없습니다."));
            }

            if (TryGetCachedInstance(id, out UIView cachedInstance))
            {
                if (cachedInstance is T cachedTypedInstance)
                {
                    return Result<T>.Success(cachedTypedInstance);
                }

                return Result<T>.Failure(new ResultError(
                    UIErrorCodes.InvalidType,
                    $"캐시된 UI '{id}'의 타입 '{cachedInstance.GetType().Name}'은 요청한 타입 '{typeof(T).Name}'과 호환되지 않습니다."));
            }

            if (registry == null)
            {
                return Result<T>.Failure(new ResultError(
                    UIErrorCodes.MissingRegistry,
                    "UIController에 UIRegistry가 지정되지 않았습니다."));
            }

            Result<UIView> prefabResult = registry.Get(id);

            if (prefabResult.IsFailure)
            {
                return Result<T>.Failure(prefabResult.Error);
            }

            UIView prefab = prefabResult.Value;

            if (prefab is not T)
            {
                return Result<T>.Failure(new ResultError(
                    UIErrorCodes.InvalidType,
                    $"UI '{id}'의 등록 타입 '{prefab.GetType().Name}'은 요청한 타입 '{typeof(T).Name}'과 호환되지 않습니다."));
            }

            Result<Transform> layerResult = ResolveLayer(prefab);

            if (layerResult.IsFailure)
            {
                return Result<T>.Failure(layerResult.Error);
            }

            UIView instance = Instantiate(
                prefab,
                layerResult.Value,
                false);

            CacheInstance(id, instance);

            return Result<T>.Success((T)instance);
        }

        internal int OpenOverlayCount
        {
            get
            {
                CleanupRuntimeCollectionsAndRefreshInput();
                return overlayOrder.Count;
            }
        }

        internal void SetRegistry(UIRegistry registry)
        {
            this.registry = registry;
        }

        internal void SetLayers(Transform screenLayer, Transform overlayLayer, Transform popupLayer, Transform topOverlayLayer)
        {
            this.screenLayer = screenLayer;
            this.overlayLayer = overlayLayer;
            this.popupLayer = popupLayer;
            this.topOverlayLayer = topOverlayLayer;
        }

        internal Result<T> PrepareViewForOpen<T>(UIId id) where T : UIView
        {
            Result<T> instanceResult = GetOrCreate<T>(id);

            if (instanceResult.IsFailure)
            {
                return Result<T>.Failure(instanceResult.Error);
            }

            T view = instanceResult.Value;

            Result validationResult = lifecycleCoordinator.ValidateOpen(view);

            if (validationResult.IsFailure)
            {
                return Result<T>.Failure(validationResult.Error);
            }

            return Result<T>.Success(view);
        }

        internal Result<T> OpenView<T>(UIId id, Action onCompleted = null) where T : UIView
        {
            Result<T> instanceResult = GetOrCreate<T>(id);

            if (instanceResult.IsFailure)
            {
                return Result<T>.Failure(instanceResult.Error);
            }

            T view = instanceResult.Value;

            Result openResult = lifecycleCoordinator.Open(
                this,
                view,
                onCompleted);

            if (openResult.IsFailure)
            {
                return Result<T>.Failure(openResult.Error);
            }

            return Result<T>.Success(view);
        }

        internal Result OpenPreparedView(UIView view, Action onCompleted = null)
        {
            return lifecycleCoordinator.Open(
                this,
                view,
                onCompleted);
        }

        internal Result CloseView(UIView view, Action onCompleted = null)
        {
            return lifecycleCoordinator.Close(
                this,
                view,
                onCompleted);
        }

        internal bool TryGetCachedInstance(UIId id, out UIView instance)
        {
            return instanceStore.TryGet(id, out instance);
        }

        internal void CacheInstance(UIId id, UIView instance)
        {
            instanceStore.Cache(id, instance);
        }

        internal bool RemoveCachedInstance(UIId id)
        {
            return instanceStore.Remove(id);
        }

        internal void ClearInstanceCache()
        {
            instanceStore.Clear();
        }

        internal void RefreshInputState()
        {
            inputCoordinator.Refresh(
                instanceStore,
                popupStack,
                overlayOrder,
                screenNavigator.CurrentScreen);
        }

        private void CleanupRuntimeCollectionsAndRefreshInput()
        {
            int popupCountBeforeCleanup = popupStack.Count;
            int overlayCountBeforeCleanup = overlayOrder.Count;

            popupStack.Cleanup(instanceStore);
            overlayOrder.Cleanup(instanceStore);

            bool collectionChanged =
                popupCountBeforeCleanup != popupStack.Count ||
                overlayCountBeforeCleanup != overlayOrder.Count;

            if (collectionChanged)
            {
                RefreshInputState();
            }
        }

        private Result<Transform> ResolveLayer(UIView prefab)
        {
            Transform layer;

            if (prefab is UIScreen)
            {
                layer = screenLayer;
            }
            else if (prefab is UIPopup)
            {
                layer = popupLayer;
            }
            else if (prefab is UIOverlay overlay)
            {
                layer = overlay.OverlayLayer == UIOverlayLayer.Topmost
                    ? topOverlayLayer
                    : overlayLayer;
            }
            else
            {
                return Result<Transform>.Failure(new ResultError(
                    UIErrorCodes.InvalidType,
                    $"UI '{prefab.Id}'의 타입 '{prefab.GetType().Name}'은 지원되는 Screen, Popup, Overlay 타입이 아닙니다."));
            }

            if (layer == null)
            {
                return Result<Transform>.Failure(new ResultError(
                    UIErrorCodes.MissingLayer,
                    $"UI '{prefab.Id}'을 배치할 Layer가 UIController에 지정되지 않았습니다."));
            }

            return Result<Transform>.Success(layer);
        }
    }
}