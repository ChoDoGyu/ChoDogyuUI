using System;
using System.Collections;
using CDG.Core.Results;
using UnityEngine;

namespace CDG.UI
{
    /// <summary>
    /// UIView의 Opening과 Closing Lifecycle을 실행하는 내부 Coordinator입니다.
    /// 상태 검증, Transition 실행, Lifecycle 완료 처리를 하나의 경로에서 관리합니다.
    /// </summary>
    internal sealed class UIViewLifecycleCoordinator
    {
        private int activeTransitionCount;

        internal bool IsBusy => activeTransitionCount > 0;

        internal Result ValidateOpen(UIView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

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

            return ValidateLifecycleView(view);
        }

        internal Result ValidateClose(UIView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

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

            return ValidateLifecycleView(view);
        }

        internal Result Open(MonoBehaviour coroutineHost, UIView view, Action onCompleted = null)
        {
            if (coroutineHost == null)
            {
                throw new ArgumentNullException(nameof(coroutineHost));
            }

            Result validationResult = ValidateOpen(view);

            if (validationResult.IsFailure)
            {
                return validationResult;
            }

            view.BeginOpening();

            UIViewTransition transition = view.Transition;

            if (transition == null)
            {
                view.CompleteOpening();
                onCompleted?.Invoke();

                return Result.Success();
            }

            activeTransitionCount++;

            coroutineHost.StartCoroutine(
                PlayOpening(
                    view,
                    transition,
                    onCompleted));

            return Result.Success();
        }

        internal Result Close(MonoBehaviour coroutineHost, UIView view, Action onCompleted = null)
        {
            if (coroutineHost == null)
            {
                throw new ArgumentNullException(nameof(coroutineHost));
            }

            Result validationResult = ValidateClose(view);

            if (validationResult.IsFailure)
            {
                return validationResult;
            }

            view.BeginClosing();

            UIViewTransition transition = view.Transition;

            if (transition == null)
            {
                view.CompleteClosing();
                onCompleted?.Invoke();

                return Result.Success();
            }

            activeTransitionCount++;

            coroutineHost.StartCoroutine(
                PlayClosing(
                    view,
                    transition,
                    onCompleted));

            return Result.Success();
        }

        private IEnumerator PlayOpening(UIView view, UIViewTransition transition, Action onCompleted)
        {
            try
            {
                yield return transition.PlayOpening(view.CanvasGroup);

                if (view != null)
                {
                    view.CompleteOpening();
                }
            }
            finally
            {
                activeTransitionCount = Math.Max(
                    0,
                    activeTransitionCount - 1);
            }

            onCompleted?.Invoke();
        }

        private IEnumerator PlayClosing(UIView view, UIViewTransition transition, Action onCompleted)
        {
            try
            {
                yield return transition.PlayClosing(view.CanvasGroup);

                if (view != null)
                {
                    view.CompleteClosing();
                }
            }
            finally
            {
                activeTransitionCount = Math.Max(
                    0,
                    activeTransitionCount - 1);
            }

            onCompleted?.Invoke();
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
    }
}