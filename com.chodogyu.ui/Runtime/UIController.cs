using System.Collections.Generic;
using CDG.Core.Results;
using UnityEngine;

namespace CDG.UI
{
    /// <summary>
    /// Registry에 등록된 UI의 Runtime Instance와 화면 흐름을 관리하는 중심 Controller입니다.
    /// Screen, Popup, Overlay의 실제 Navigation 기능은 각 전용 단계에서 확장됩니다.
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