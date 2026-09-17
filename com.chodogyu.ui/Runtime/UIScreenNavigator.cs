using System;
using System.Collections.Generic;
using CDG.Core.Results;

namespace CDG.UI
{
    /// <summary>
    /// 현재 Screen과 Screen History를 관리하고 Push, Replace, Reset 및 Back 복원을 수행합니다.
    /// Screen 전환 중에는 중복 Navigation을 차단하고 이전 Screen의 Closing 완료 후 다음 Screen을 엽니다.
    /// </summary>
    internal sealed class UIScreenNavigator
    {
        private readonly Stack<UIId> history = new Stack<UIId>();

        private UIScreen currentScreen;
        private bool isNavigating;

        internal UIScreen CurrentScreen => currentScreen;

        internal int HistoryCount => history.Count;

        internal bool IsBusy => isNavigating;

        internal Result<UIScreen> Open(UIController controller, UIId id, UIScreenOpenMode mode)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            if (isNavigating)
            {
                return Result<UIScreen>.Failure(new ResultError(
                    UIErrorCodes.Busy,
                    "Screen Navigation이 진행 중이므로 새로운 Screen 요청을 처리할 수 없습니다."));
            }

            Result<UIScreen> targetResult = controller.PrepareViewForOpen<UIScreen>(id);

            if (targetResult.IsFailure)
            {
                return Result<UIScreen>.Failure(targetResult.Error);
            }

            UIScreen targetScreen = targetResult.Value;

            switch (mode)
            {
                case UIScreenOpenMode.Push:
                    return Push(
                        controller,
                        targetScreen);

                case UIScreenOpenMode.Replace:
                    return Replace(
                        controller,
                        targetScreen);

                case UIScreenOpenMode.Reset:
                    return Reset(
                        controller,
                        targetScreen);

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "지원하지 않는 Screen Open Mode입니다.");
            }
        }

        internal Result RestorePrevious(UIController controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            if (isNavigating)
            {
                return Result.Failure(new ResultError(
                    UIErrorCodes.Busy,
                    "Screen Navigation이 진행 중이므로 Back 요청을 처리할 수 없습니다."));
            }

            if (history.Count == 0)
            {
                return Result.Failure(new ResultError(
                    UIErrorCodes.NoBackTarget,
                    "복원할 Screen History가 없습니다."));
            }

            UIId previousScreenId = history.Peek();

            Result<UIScreen> targetResult = controller.PrepareViewForOpen<UIScreen>(
                previousScreenId);

            if (targetResult.IsFailure)
            {
                return Result.Failure(targetResult.Error);
            }

            Result<UIScreen> navigationResult = BeginNavigation(
                controller,
                targetResult.Value,
                () => history.Pop());

            if (navigationResult.IsFailure)
            {
                return Result.Failure(navigationResult.Error);
            }

            return Result.Success();
        }

        internal void HandleClosedScreen(UIScreen screen)
        {
            if (screen == null)
            {
                return;
            }

            if (currentScreen == screen)
            {
                currentScreen = null;
            }
        }

        private Result<UIScreen> Push(UIController controller, UIScreen targetScreen)
        {
            UIScreen previousScreen = currentScreen;

            return BeginNavigation(
                controller,
                targetScreen,
                () =>
                {
                    if (previousScreen != null)
                    {
                        history.Push(previousScreen.Id);
                    }
                });
        }

        private Result<UIScreen> Replace(UIController controller, UIScreen targetScreen)
        {
            return BeginNavigation(
                controller,
                targetScreen,
                null);
        }

        private Result<UIScreen> Reset(UIController controller, UIScreen targetScreen)
        {
            return BeginNavigation(
                controller,
                targetScreen,
                history.Clear);
        }

        private Result<UIScreen> BeginNavigation(UIController controller, UIScreen targetScreen, Action onTargetStarted)
        {
            isNavigating = true;

            UIScreen previousScreen = currentScreen;

            if (previousScreen == null)
            {
                Result<UIScreen> startResult = StartTargetScreen(
                    controller,
                    targetScreen,
                    onTargetStarted);

                if (startResult.IsFailure)
                {
                    isNavigating = false;
                }

                return startResult;
            }

            bool synchronousCallbackInvoked = false;
            bool delayedStartFailed = false;
            ResultError delayedStartError = ResultError.None;

            Result closeResult = controller.CloseView(
                previousScreen,
                () =>
                {
                    synchronousCallbackInvoked = true;

                    if (currentScreen == previousScreen)
                    {
                        currentScreen = null;
                    }

                    Result<UIScreen> startResult = StartTargetScreen(
                        controller,
                        targetScreen,
                        onTargetStarted);

                    if (startResult.IsFailure)
                    {
                        delayedStartFailed = true;
                        delayedStartError = startResult.Error;

                        isNavigating = false;
                        controller.RefreshInputState();
                    }
                });

            if (closeResult.IsFailure)
            {
                isNavigating = false;

                return Result<UIScreen>.Failure(closeResult.Error);
            }

            if (synchronousCallbackInvoked && delayedStartFailed)
            {
                return Result<UIScreen>.Failure(delayedStartError);
            }

            return Result<UIScreen>.Success(targetScreen);
        }

        private Result<UIScreen> StartTargetScreen(UIController controller, UIScreen targetScreen, Action onTargetStarted)
        {
            currentScreen = targetScreen;

            bool startCallInProgress = true;
            bool completedDuringStartCall = false;

            Result openResult = controller.OpenPreparedView(
                targetScreen,
                () =>
                {
                    if (startCallInProgress)
                    {
                        completedDuringStartCall = true;
                        return;
                    }

                    CompleteNavigation(controller);
                });

            startCallInProgress = false;

            if (openResult.IsFailure)
            {
                if (currentScreen == targetScreen)
                {
                    currentScreen = null;
                }

                isNavigating = false;
                controller.RefreshInputState();

                return Result<UIScreen>.Failure(openResult.Error);
            }

            onTargetStarted?.Invoke();

            controller.RefreshInputState();

            if (completedDuringStartCall)
            {
                CompleteNavigation(controller);
            }

            return Result<UIScreen>.Success(targetScreen);
        }

        private void CompleteNavigation(UIController controller)
        {
            isNavigating = false;
            controller.RefreshInputState();
        }
    }
}