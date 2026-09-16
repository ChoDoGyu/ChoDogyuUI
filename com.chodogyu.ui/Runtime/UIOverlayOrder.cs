using System.Collections.Generic;

namespace CDG.UI
{
    /// <summary>
    /// 열린 Overlay의 순서를 관리하는 내부 컬렉션입니다.
    /// 파괴되었거나 이미 닫힌 Overlay 기록을 정리하며 같은 Layer에서는 나중에 열린 Overlay부터 조회할 수 있습니다.
    /// </summary>
    internal sealed class UIOverlayOrder
    {
        private readonly List<UIId> overlayIds = new List<UIId>();

        internal int Count => overlayIds.Count;

        internal void Add(UIId id)
        {
            overlayIds.Add(id);
        }

        internal void Remove(UIId id)
        {
            for (int i = overlayIds.Count - 1; i >= 0; i--)
            {
                if (overlayIds[i] == id)
                {
                    overlayIds.RemoveAt(i);
                }
            }
        }

        internal void Cleanup(UIInstanceStore instanceStore)
        {
            for (int i = overlayIds.Count - 1; i >= 0; i--)
            {
                UIId id = overlayIds[i];

                if (!instanceStore.TryGet(id, out UIView instance))
                {
                    overlayIds.RemoveAt(i);
                    continue;
                }

                if (instance is not UIOverlay overlay)
                {
                    overlayIds.RemoveAt(i);
                    continue;
                }

                if (overlay.State == UIViewState.Closed)
                {
                    overlayIds.RemoveAt(i);
                }
            }
        }

        internal bool HasBlockingOverlay(UIOverlayLayer layer, UIInstanceStore instanceStore)
        {
            Cleanup(instanceStore);

            for (int i = overlayIds.Count - 1; i >= 0; i--)
            {
                UIId id = overlayIds[i];

                if (!instanceStore.TryGet(id, out UIView instance))
                {
                    continue;
                }

                if (instance is not UIOverlay overlay)
                {
                    continue;
                }

                if (overlay.OverlayLayer != layer)
                {
                    continue;
                }

                if (overlay.BlocksInput)
                {
                    return true;
                }
            }

            return false;
        }

        internal IEnumerable<UIId> EnumerateTopToBottom(UIOverlayLayer layer, UIInstanceStore instanceStore)
        {
            for (int i = overlayIds.Count - 1; i >= 0; i--)
            {
                UIId id = overlayIds[i];

                if (!instanceStore.TryGet(id, out UIView instance))
                {
                    continue;
                }

                if (instance is not UIOverlay overlay)
                {
                    continue;
                }

                if (overlay.OverlayLayer != layer)
                {
                    continue;
                }

                yield return id;
            }
        }
    }
}