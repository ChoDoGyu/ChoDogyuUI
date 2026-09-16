using System;
using System.Collections.Generic;
using CDG.Core.Results;
using UnityEngine;

namespace CDG.UI
{
    /// <summary>
    /// Registry에 등록된 UI의 Runtime Instance와 화면 흐름을 관리하는 중심 Controller입니다.
    /// Screen Navigation, Popup Stack, Overlay 관리 기능을 통해 UI 흐름을 일관된 방식으로 제어합니다.
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

        private readonly Dictionary<UIId, UIView> instances = new Dictionary<UIId, UIView>();
        private readonly Stack<UIId> screenHistory = new Stack<UIId>();
        private readonly Stack<UIId> popupStack = new Stack<UIId>();

        private UIScreen currentScreen;

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
        /// 현재 열려 있는 Screen을 반환합니다.
        /// 아직 Screen Navigation이 시작되지 않았거나 현재 Screen이 파괴된 경우 null일 수 있습니다.
        /// </summary>
        public UIScreen CurrentScreen => currentScreen;

        /// <summary>
        /// Back으로 복원할 수 있도록 보관 중인 이전 Screen의 개수를 반환합니다.
        /// History 컬렉션 자체는 외부에 노출하지 않습니다.
        /// </summary>
        public int ScreenHistoryCount => screenHistory.Count;

        /// <summary>
        /// 현재 Popup Stack의 최상단 Popup을 반환합니다.
        /// 유효한 Popup이 없으면 null을 반환하며, 파괴되거나 이미 닫힌 항목은 자동으로 Stack에서 정리합니다.
        /// </summary>
        public UIPopup TopPopup
        {
            get
            {
                CleanupPopupStack();

                if (popupStack.Count == 0)
                {
                    return null;
                }

                UIId topId = popupStack.Peek();

                return TryGetCachedInstance(topId, out UIView instance)
                    ? instance as UIPopup
                    : null;
            }
        }

        /// <summary>
        /// 현재 Popup Stack에 존재하는 유효한 Popup의 개수를 반환합니다.
        /// 파괴되거나 이미 닫힌 항목은 계산 전에 자동으로 정리합니다.
        /// </summary>
        public int PopupCount
        {
            get
            {
                CleanupPopupStack();
                return popupStack.Count;
            }
        }

        /// <summary>
        /// 현재 Screen을 History에 보존하는 Push 방식으로 지정한 Screen을 엽니다.
        /// 현재 Screen이 없다면 History를 변경하지 않고 대상 Screen을 최초 Screen으로 엽니다.
        /// </summary>
        /// <param name="id">열 Screen의 UI ID입니다.</param>
        /// <returns>열린 Screen 또는 Navigation 실패 정보를 포함하는 결과입니다.</returns>
        public Result<UIScreen> OpenScreen(UIId id)
        {
            return OpenScreen(id, UIScreenOpenMode.Push);
        }

        /// <summary>
        /// 지정한 Navigation 방식으로 Screen을 엽니다.
        /// Push는 현재 Screen을 History에 보존하고, Replace는 현재 History를 유지한 채 Screen만 교체하며,
        /// Reset은 대상 Screen이 정상적으로 열린 후 기존 History를 모두 제거합니다.
        /// </summary>
        /// <param name="id">열 Screen의 UI ID입니다.</param>
        /// <param name="mode">적용할 Screen Navigation 방식입니다.</param>
        /// <returns>열린 Screen 또는 Navigation 실패 정보를 포함하는 결과입니다.</returns>
        public Result<UIScreen> OpenScreen(UIId id, UIScreenOpenMode mode)
        {
            switch (mode)
            {
                case UIScreenOpenMode.Push:
                    return PushScreen(id);

                case UIScreenOpenMode.Replace:
                    return ReplaceScreen(id);

                case UIScreenOpenMode.Reset:
                    return ResetScreen(id);

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "지원하지 않는 Screen Open Mode입니다.");
            }
        }

        /// <summary>
        /// 지정한 Popup을 열고 Popup Stack의 최상단에 추가합니다.
        /// 이미 열린 Popup이거나 Lifecycle 전환 중인 Popup은 다시 열 수 없습니다.
        /// </summary>
        /// <param name="id">열 Popup의 UI ID입니다.</param>
        /// <returns>열린 Popup 또는 실패 정보를 포함하는 결과입니다.</returns>
        public Result<UIPopup> OpenPopup(UIId id)
        {
            Result<UIPopup> targetResult = PreparePopupForOpen(id);

            if (targetResult.IsFailure)
            {
                return Result<UIPopup>.Failure(targetResult.Error);
            }

            Result<UIPopup> openResult = OpenView<UIPopup>(id);

            if (openResult.IsFailure)
            {
                return Result<UIPopup>.Failure(openResult.Error);
            }

            UIPopup popup = openResult.Value;

            popup.transform.SetAsLastSibling();
            popupStack.Push(popup.Id);

            return Result<UIPopup>.Success(popup);
        }

        /// <summary>
        /// 지정한 UI의 Runtime Instance를 닫습니다.
        /// Popup을 닫으면 Popup Stack에서도 제거되며, 현재 Screen을 직접 닫는 경우 History를 자동 복원하지 않습니다.
        /// </summary>
        /// <param name="id">닫을 UI의 ID입니다.</param>
        /// <returns>닫기 성공 또는 실패 정보를 포함하는 결과입니다.</returns>
        public Result Close(UIId id)
        {
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

            Result closeResult = CloseView(instance);

            if (closeResult.IsFailure)
            {
                return closeResult;
            }

            if (instance is UIPopup)
            {
                RemovePopupFromStack(id);
            }

            if (instance is UIScreen screen && currentScreen == screen)
            {
                currentScreen = null;
            }

            return Result.Success();
        }

        /// <summary>
        /// 지정한 UI가 완전히 열린 상태인지 확인합니다.
        /// 아직 생성되지 않았거나 ID가 유효하지 않은 경우 false를 반환합니다.
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

            UIView instance = Instantiate(prefab, layerResult.Value, false);

            CacheInstance(id, instance);

            return Result<T>.Success((T)instance);
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

        internal Result<T> OpenView<T>(UIId id) where T : UIView
        {
            Result<T> instanceResult = GetOrCreate<T>(id);

            if (instanceResult.IsFailure)
            {
                return Result<T>.Failure(instanceResult.Error);
            }

            T view = instanceResult.Value;
            Result stateResult = ValidateOpenState(view);

            if (stateResult.IsFailure)
            {
                return Result<T>.Failure(stateResult.Error);
            }

            Result validationResult = ValidateLifecycleView(view);

            if (validationResult.IsFailure)
            {
                return Result<T>.Failure(validationResult.Error);
            }

            view.BeginOpening();
            view.CompleteOpening();

            return Result<T>.Success(view);
        }

        internal Result CloseView(UIView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            Result stateResult = ValidateCloseState(view);

            if (stateResult.IsFailure)
            {
                return stateResult;
            }

            Result validationResult = ValidateLifecycleView(view);

            if (validationResult.IsFailure)
            {
                return validationResult;
            }

            view.BeginClosing();
            view.CompleteClosing();

            return Result.Success();
        }

        internal bool TryGetCachedInstance(UIId id, out UIView instance)
        {
            if (!instances.TryGetValue(id, out instance))
            {
                return false;
            }

            if (instance != null)
            {
                return true;
            }

            instances.Remove(id);
            instance = null;

            return false;
        }

        internal void CacheInstance(UIId id, UIView instance)
        {
            instances[id] = instance;
        }

        internal bool RemoveCachedInstance(UIId id)
        {
            return instances.Remove(id);
        }

        internal void ClearInstanceCache()
        {
            instances.Clear();
        }

        private Result<UIScreen> PushScreen(UIId id)
        {
            Result<UIScreen> targetResult = PrepareScreenForOpen(id);

            if (targetResult.IsFailure)
            {
                return Result<UIScreen>.Failure(targetResult.Error);
            }

            UIScreen previousScreen = currentScreen;

            Result navigationResult = ChangeCurrentScreen(id);

            if (navigationResult.IsFailure)
            {
                return Result<UIScreen>.Failure(navigationResult.Error);
            }

            if (previousScreen != null)
            {
                screenHistory.Push(previousScreen.Id);
            }

            return Result<UIScreen>.Success(currentScreen);
        }

        private Result<UIScreen> ReplaceScreen(UIId id)
        {
            Result<UIScreen> targetResult = PrepareScreenForOpen(id);

            if (targetResult.IsFailure)
            {
                return Result<UIScreen>.Failure(targetResult.Error);
            }

            Result navigationResult = ChangeCurrentScreen(id);

            if (navigationResult.IsFailure)
            {
                return Result<UIScreen>.Failure(navigationResult.Error);
            }

            return Result<UIScreen>.Success(currentScreen);
        }

        private Result<UIScreen> ResetScreen(UIId id)
        {
            Result<UIScreen> targetResult = PrepareScreenForOpen(id);

            if (targetResult.IsFailure)
            {
                return Result<UIScreen>.Failure(targetResult.Error);
            }

            Result navigationResult = ChangeCurrentScreen(id);

            if (navigationResult.IsFailure)
            {
                return Result<UIScreen>.Failure(navigationResult.Error);
            }

            screenHistory.Clear();

            return Result<UIScreen>.Success(currentScreen);
        }

        private Result ChangeCurrentScreen(UIId id)
        {
            UIScreen previousScreen = currentScreen;

            if (previousScreen != null)
            {
                Result closeResult = CloseView(previousScreen);

                if (closeResult.IsFailure)
                {
                    return closeResult;
                }
            }

            Result<UIScreen> openResult = OpenView<UIScreen>(id);

            if (openResult.IsFailure)
            {
                return Result.Failure(openResult.Error);
            }

            currentScreen = openResult.Value;

            return Result.Success();
        }

        private Result<UIScreen> PrepareScreenForOpen(UIId id)
        {
            Result<UIScreen> instanceResult = GetOrCreate<UIScreen>(id);

            if (instanceResult.IsFailure)
            {
                return Result<UIScreen>.Failure(instanceResult.Error);
            }

            UIScreen screen = instanceResult.Value;

            Result stateResult = ValidateOpenState(screen);

            if (stateResult.IsFailure)
            {
                return Result<UIScreen>.Failure(stateResult.Error);
            }

            Result validationResult = ValidateLifecycleView(screen);

            if (validationResult.IsFailure)
            {
                return Result<UIScreen>.Failure(validationResult.Error);
            }

            return Result<UIScreen>.Success(screen);
        }

        private Result<UIPopup> PreparePopupForOpen(UIId id)
        {
            Result<UIPopup> instanceResult = GetOrCreate<UIPopup>(id);

            if (instanceResult.IsFailure)
            {
                return Result<UIPopup>.Failure(instanceResult.Error);
            }

            UIPopup popup = instanceResult.Value;

            Result stateResult = ValidateOpenState(popup);

            if (stateResult.IsFailure)
            {
                return Result<UIPopup>.Failure(stateResult.Error);
            }

            Result validationResult = ValidateLifecycleView(popup);

            if (validationResult.IsFailure)
            {
                return Result<UIPopup>.Failure(validationResult.Error);
            }

            return Result<UIPopup>.Success(popup);
        }

        private void RemovePopupFromStack(UIId id)
        {
            if (popupStack.Count == 0)
            {
                return;
            }

            Stack<UIId> temporaryStack = new Stack<UIId>();

            while (popupStack.Count > 0)
            {
                UIId currentId = popupStack.Pop();

                if (currentId == id)
                {
                    break;
                }

                temporaryStack.Push(currentId);
            }

            while (temporaryStack.Count > 0)
            {
                popupStack.Push(temporaryStack.Pop());
            }
        }

        private void CleanupPopupStack()
        {
            if (popupStack.Count == 0)
            {
                return;
            }

            Stack<UIId> temporaryStack = new Stack<UIId>();

            while (popupStack.Count > 0)
            {
                UIId id = popupStack.Pop();

                if (!TryGetCachedInstance(id, out UIView instance))
                {
                    continue;
                }

                if (instance is not UIPopup popup)
                {
                    continue;
                }

                if (popup.State == UIViewState.Closed)
                {
                    continue;
                }

                temporaryStack.Push(id);
            }

            while (temporaryStack.Count > 0)
            {
                popupStack.Push(temporaryStack.Pop());
            }
        }

        private Result ValidateOpenState(UIView view)
        {
            if (view.State == UIViewState.Open)
            {
                return Result.Failure(new ResultError(
                    UIErrorCodes.AlreadyOpen,
                    $"UI '{view.Id}'는 이미 열린 상태입니다."));
            }

            if (view.IsTransitioning)
            {
                return Result.Failure(new ResultError(
                    UIErrorCodes.Busy,
                    $"UI '{view.Id}'는 현재 {view.State} 상태이므로 Open 요청을 처리할 수 없습니다."));
            }

            return Result.Success();
        }

        private Result ValidateCloseState(UIView view)
        {
            if (view.State == UIViewState.Closed)
            {
                return Result.Failure(new ResultError(
                    UIErrorCodes.AlreadyClosed,
                    $"UI '{view.Id}'는 이미 닫힌 상태입니다."));
            }

            if (view.IsTransitioning)
            {
                return Result.Failure(new ResultError(
                    UIErrorCodes.Busy,
                    $"UI '{view.Id}'는 현재 {view.State} 상태이므로 Close 요청을 처리할 수 없습니다."));
            }

            return Result.Success();
        }

        private Result ValidateLifecycleView(UIView view)
        {
            if (view.CanvasGroup == null)
            {
                return Result.Failure(new ResultError(
                    UIErrorCodes.MissingCanvasGroup,
                    $"UI '{view.Id}'에 Lifecycle 및 입력 제어에 필요한 CanvasGroup이 없습니다."));
            }

            return Result.Success();
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