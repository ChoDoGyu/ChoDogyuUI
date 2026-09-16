using System.Collections.Generic;

namespace CDG.UI
{
    /// <summary>
    /// UI ID와 생성된 Runtime View Instance의 연결을 보관하는 내부 저장소입니다.
    /// 파괴된 Unity Object는 조회 시 자동으로 캐시에서 제거합니다.
    /// </summary>
    internal sealed class UIInstanceStore
    {
        private readonly Dictionary<UIId, UIView> instances = new Dictionary<UIId, UIView>();

        internal bool TryGet(UIId id, out UIView instance)
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

        internal void Cache(UIId id, UIView instance)
        {
            instances[id] = instance;
        }

        internal bool Remove(UIId id)
        {
            return instances.Remove(id);
        }

        internal void Clear()
        {
            instances.Clear();
        }
    }
}