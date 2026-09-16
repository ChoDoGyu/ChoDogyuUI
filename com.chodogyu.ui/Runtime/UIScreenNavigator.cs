using System;
using System.Collections.Generic;
using CDG.Core.Results;

namespace CDG.UI
{
    /// <summary>
    /// 현재 Screen과 Screen History를 관리하고 Push, Replace, Reset 및 Back 복원을 수행합니다.
    /// UIController를 통해 실제 View Lifecycle을 실행하며 Screen Navigation 상태만 전담합니다.
    /// </summary>
    internal sealed class UIScreenNavigator
    {
        private readonly Stack<UIId> history = new Stack<UIId>();

        private UIScreen currentScreen;

        internal UIScreen CurrentScreen => currentScreen;

        internal int HistoryCount => history.Count;

        internal Result<UIScreen> Open(UIController controller, UIId id, UIScreenOpenMode mode)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            switch (mode)
            {
                case UIScreenOpenMode.Push:
                    return Push(controller, id);

                case UIScreenOpenMode.Replace:
                    return Replace(controller, id);

                case UIScreenOpenMode.Reset:
                    return Reset(controller, id);

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

            if (history.Count == 0)
            {
                return Result.Failure(new ResultError(
                    UIErrorCodes.NoBackTarget,
                    "복원할 Screen History가 없습니다."));
            }

            UIId previousScreenId = history.Peek();

            Result<UIScreen> restoreResult = Replace(
                controller,
                previousScreenId);

            if (restoreResult.IsFailure)
            {
                return Result.Failure(restoreResult.Error);
            }

            history.Pop();

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

        private Result<UIScreen> Push(UIController controller, UIId id)
        {
            Result<UIScreen> targetResult = controller.PrepareViewForOpen<UIScreen>(id);

            if (targetResult.IsFailure)
            {
                return Result<UIScreen>.Failure(targetResult.Error);
            }

            UIScreen previousScreen = currentScreen;

            Result navigationResult = ChangeCurrentScreen(
                controller,
                id);

            if (navigationResult.IsFailure)
            {
                return Result<UIScreen>.Failure(navigationResult.Error);
            }

            if (previousScreen != null)
            {
                history.Push(previousScreen.Id);
            }

            return Result<UIScreen>.Success(currentScreen);
        }

        private Result<UIScreen> Replace(UIController controller, UIId id)
        {
            Result<UIScreen> targetResult = controller.PrepareViewForOpen<UIScreen>(id);

            if (targetResult.IsFailure)
            {
                return Result<UIScreen>.Failure(targetResult.Error);
            }

            Result navigationResult = ChangeCurrentScreen(
                controller,
                id);

            if (navigationResult.IsFailure)
            {
                return Result<UIScreen>.Failure(navigationResult.Error);
            }

            return Result<UIScreen>.Success(currentScreen);
        }

        private Result<UIScreen> Reset(UIController controller, UIId id)
        {
            Result<UIScreen> targetResult = controller.PrepareViewForOpen<UIScreen>(id);

            if (targetResult.IsFailure)
            {
                return Result<UIScreen>.Failure(targetResult.Error);
            }

            Result navigationResult = ChangeCurrentScreen(
                controller,
                id);

            if (navigationResult.IsFailure)
            {
                return Result<UIScreen>.Failure(navigationResult.Error);
            }

            history.Clear();

            return Result<UIScreen>.Success(currentScreen);
        }

        private Result ChangeCurrentScreen(UIController controller, UIId id)
        {
            UIScreen previousScreen = currentScreen;

            if (previousScreen != null)
            {
                Result closeResult = controller.CloseView(previousScreen);

                if (closeResult.IsFailure)
                {
                    return closeResult;
                }
            }

            Result<UIScreen> openResult = controller.OpenView<UIScreen>(id);

            if (openResult.IsFailure)
            {
                return Result.Failure(openResult.Error);
            }

            currentScreen = openResult.Value;

            return Result.Success();
        }
    }
}