using System.Collections.Generic;
using CDG.Core.Results;
using UnityEditor;

namespace CDG.UI.Editor
{
    /// <summary>
    /// 기존 UIRegistry Asset에 UIView Prefab을 안전하게 등록합니다.
    /// 등록 전 Registry와 Prefab의 Asset 상태를 확인하고, 실패 시 기존 Registry 내용을 유지합니다.
    /// </summary>
    internal static class UIRegistryRegistrar
    {
        /// <summary>
        /// 지정한 View Prefab을 Registry의 마지막 항목으로 등록합니다.
        /// 중복 ID 등 Registry 규칙을 위반하면 등록하지 않고 실패 결과를 반환합니다.
        /// </summary>
        /// <param name="registry">View를 등록할 UIRegistry Asset입니다.</param>
        /// <param name="viewPrefab">등록할 UIView Prefab Asset입니다.</param>
        /// <returns>등록 성공 또는 실패 정보를 포함하는 결과입니다.</returns>
        internal static Result Register(UIRegistry registry, UIView viewPrefab)
        {
            Result validationResult = Validate(registry, viewPrefab);

            if (validationResult.IsFailure)
            {
                return validationResult;
            }

            List<UIView> prefabs = new List<UIView>(registry.Prefabs);
            prefabs.Add(viewPrefab);

            Result replaceResult = registry.ReplacePrefabs(prefabs);

            if (replaceResult.IsFailure)
            {
                return replaceResult;
            }

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            return Result.Success();
        }

        private static Result Validate(UIRegistry registry, UIView viewPrefab)
        {
            if (registry == null || !AssetDatabase.Contains(registry))
            {
                return Result.Failure(new ResultError(
                    UIEditorErrorCodes.InvalidRegistry,
                    "등록 대상 UIRegistry는 프로젝트에 저장된 Asset이어야 합니다."));
            }

            if (viewPrefab == null ||
                !AssetDatabase.Contains(viewPrefab) ||
                !PrefabUtility.IsPartOfPrefabAsset(viewPrefab.gameObject))
            {
                return Result.Failure(new ResultError(
                    UIEditorErrorCodes.InvalidViewPrefab,
                    "등록할 UIView는 프로젝트에 저장된 Prefab Asset이어야 합니다."));
            }

            return Result.Success();
        }
    }
}