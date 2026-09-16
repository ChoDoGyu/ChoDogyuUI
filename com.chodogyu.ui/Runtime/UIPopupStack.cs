using System.Collections.Generic;

namespace CDG.UI
{
    /// <summary>
    /// 열린 Popup의 ID를 LIFO 순서로 관리하는 내부 Stack입니다.
    /// 파괴되었거나 이미 닫힌 Popup 기록을 정리하고 현재 Popup 순서를 보존합니다.
    /// </summary>
    internal sealed class UIPopupStack
    {
        private readonly Stack<UIId> popupIds = new Stack<UIId>();

        internal int Count => popupIds.Count;

        internal void Push(UIId id)
        {
            popupIds.Push(id);
        }

        internal bool TryPeek(out UIId id)
        {
            if (popupIds.Count == 0)
            {
                id = default;
                return false;
            }

            id = popupIds.Peek();
            return true;
        }

        internal void Remove(UIId id)
        {
            if (popupIds.Count == 0)
            {
                return;
            }

            Stack<UIId> temporaryStack = new Stack<UIId>();

            while (popupIds.Count > 0)
            {
                UIId currentId = popupIds.Pop();

                if (currentId == id)
                {
                    break;
                }

                temporaryStack.Push(currentId);
            }

            while (temporaryStack.Count > 0)
            {
                popupIds.Push(temporaryStack.Pop());
            }
        }

        internal void Cleanup(UIInstanceStore instanceStore)
        {
            if (popupIds.Count == 0)
            {
                return;
            }

            Stack<UIId> temporaryStack = new Stack<UIId>();

            while (popupIds.Count > 0)
            {
                UIId id = popupIds.Pop();

                if (!instanceStore.TryGet(id, out UIView instance))
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
                popupIds.Push(temporaryStack.Pop());
            }
        }

        internal IEnumerable<UIId> EnumerateTopToBottom()
        {
            foreach (UIId id in popupIds)
            {
                yield return id;
            }
        }
    }
}