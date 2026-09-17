using CDG.Core.Results;
using UnityEditor;

namespace CDG.UI.Editor
{
    /// <summary>
    /// UI View Prefab 생성과 선택적 Registry 등록을 하나의 작업으로 조율합니다.
    /// Registry 등록에 실패하면 생성한 Prefab을 삭제하여 부분 성공 상태가 남지 않도록 합니다.
    /// </summary>
    internal static class UIViewPrefabCreationService
    {
        /// <summary>
        /// 지정한 설정으로 View Prefab을 생성하고 Registry가 지정된 경우 자동으로 등록합니다.
        /// </summary>
        internal static Result<UIView> Create(
            string assetPath,
            UIViewPrefabType viewType,
            UIId id,
            UIRegistry registry,
            bool blocksInput,
            UIOverlayLayer overlayLayer)
        {
            Result<UIView> createResult = CreatePrefab(assetPath, viewType, id, blocksInput, overlayLayer);

            if (createResult.IsFailure)
            {
                return Result<UIView>.Failure(createResult.Error);
            }

            UIView viewPrefab = createResult.Value;

            if (registry == null)
            {
                return Result<UIView>.Success(viewPrefab);
            }

            Result registerResult = UIRegistryRegistrar.Register(registry, viewPrefab);

            if (registerResult.IsFailure)
            {
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.SaveAssets();

                return Result<UIView>.Failure(registerResult.Error);
            }

            return Result<UIView>.Success(viewPrefab);
        }

        private static Result<UIView> CreatePrefab(
            string assetPath,
            UIViewPrefabType viewType,
            UIId id,
            bool blocksInput,
            UIOverlayLayer overlayLayer)
        {
            switch (viewType)
            {
                case UIViewPrefabType.Screen:
                    {
                        Result<UIScreen> result = UIViewPrefabCreator.CreateScreen(assetPath, id);
                        return ConvertResult(result);
                    }

                case UIViewPrefabType.Popup:
                    {
                        Result<UIPopup> result = UIViewPrefabCreator.CreatePopup(assetPath, id, blocksInput);
                        return ConvertResult(result);
                    }

                case UIViewPrefabType.Overlay:
                    {
                        Result<UIOverlay> result = UIViewPrefabCreator.CreateOverlay(assetPath, id, overlayLayer, blocksInput);
                        return ConvertResult(result);
                    }

                default:
                    return Result<UIView>.Failure(new ResultError(
                        UIEditorErrorCodes.PrefabCreationFailed,
                        $"지원하지 않는 View Prefab 타입입니다: '{viewType}'"));
            }
        }

        private static Result<UIView> ConvertResult<T>(Result<T> result) where T : UIView
        {
            return result.IsSuccess
                ? Result<UIView>.Success(result.Value)
                : Result<UIView>.Failure(result.Error);
        }
    }
}