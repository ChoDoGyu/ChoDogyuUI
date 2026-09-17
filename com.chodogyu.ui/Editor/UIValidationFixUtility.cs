using System;
using CDG.Core.Results;
using UnityEditor;
using UnityEngine;

namespace CDG.UI.Editor
{
    /// <summary>
    /// Validation에서 확실하게 결정할 수 있는 Prefab 문제만 안전하게 수정합니다.
    /// 사용자의 이름, ID, 참조 선택 등이 필요한 문제는 자동 수정하지 않습니다.
    /// </summary>
    internal static class UIValidationFixUtility
    {
        internal static UIValidationFix CreateAddCanvasGroupFix(UIView view)
        {
            if (!TryGetRootPrefabPath(view, out string assetPath))
            {
                return null;
            }

            return new UIValidationFix(
                "Add CanvasGroup",
                () => ModifyPrefab(
                    assetPath,
                    prefabRoot =>
                    {
                        UIView prefabView = prefabRoot.GetComponent<UIView>();

                        if (prefabView == null)
                        {
                            return InvalidViewPrefab(assetPath);
                        }

                        if (prefabView.GetComponent<CanvasGroup>() == null)
                        {
                            prefabView.gameObject.AddComponent<CanvasGroup>();
                        }

                        return Result.Success();
                    }));
        }

        internal static UIValidationFix CreateDeactivateRootFix(UIView view)
        {
            if (!TryGetRootPrefabPath(view, out string assetPath))
            {
                return null;
            }

            return new UIValidationFix(
                "Deactivate View Root",
                () => ModifyPrefab(
                    assetPath,
                    prefabRoot =>
                    {
                        prefabRoot.SetActive(false);
                        return Result.Success();
                    }));
        }

        internal static UIValidationFix CreateClampOpeningDurationFix(UIFadeTransition transition)
        {
            if (!TryGetPrefabPath(transition, out string assetPath))
            {
                return null;
            }

            return new UIValidationFix(
                "Set Opening Duration to 0",
                () => SetFadeDuration(assetPath, "openingDuration"));
        }

        internal static UIValidationFix CreateClampClosingDurationFix(UIFadeTransition transition)
        {
            if (!TryGetPrefabPath(transition, out string assetPath))
            {
                return null;
            }

            return new UIValidationFix(
                "Set Closing Duration to 0",
                () => SetFadeDuration(assetPath, "closingDuration"));
        }

        private static Result SetFadeDuration(string assetPath, string propertyName)
        {
            return ModifyPrefab(
                assetPath,
                prefabRoot =>
                {
                    UIFadeTransition transition = prefabRoot.GetComponent<UIFadeTransition>();

                    if (transition == null)
                    {
                        return InvalidViewPrefab(assetPath);
                    }

                    SerializedObject serializedTransition = new SerializedObject(transition);
                    SerializedProperty durationProperty = serializedTransition.FindProperty(propertyName);

                    if (durationProperty == null)
                    {
                        return InvalidViewPrefab(assetPath);
                    }

                    durationProperty.floatValue = 0f;
                    serializedTransition.ApplyModifiedPropertiesWithoutUndo();

                    return Result.Success();
                });
        }

        private static Result ModifyPrefab(string assetPath, Func<GameObject, Result> modify)
        {
            if (string.IsNullOrEmpty(assetPath) ||
                AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) == null)
            {
                return InvalidViewPrefab(assetPath);
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

            try
            {
                Result modifyResult = modify(prefabRoot);

                if (modifyResult.IsFailure)
                {
                    return modifyResult;
                }

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);

                if (savedPrefab == null)
                {
                    return Result.Failure(new ResultError(
                        UIEditorErrorCodes.PrefabCreationFailed,
                        $"수정된 View Prefab을 저장하지 못했습니다: '{assetPath}'"));
                }

                AssetDatabase.SaveAssets();

                return Result.Success();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static bool TryGetRootPrefabPath(UIView view, out string assetPath)
        {
            assetPath = null;

            if (view == null ||
                view.transform.parent != null ||
                !PrefabUtility.IsPartOfPrefabAsset(view.gameObject))
            {
                return false;
            }

            assetPath = AssetDatabase.GetAssetPath(view);
            return !string.IsNullOrEmpty(assetPath);
        }

        private static bool TryGetPrefabPath(Component component, out string assetPath)
        {
            assetPath = null;

            if (component == null ||
                !PrefabUtility.IsPartOfPrefabAsset(component.gameObject))
            {
                return false;
            }

            assetPath = AssetDatabase.GetAssetPath(component);
            return !string.IsNullOrEmpty(assetPath);
        }

        private static Result InvalidViewPrefab(string assetPath)
        {
            return Result.Failure(new ResultError(
                UIEditorErrorCodes.InvalidViewPrefab,
                $"Safe Fix를 적용할 유효한 View Prefab을 찾을 수 없습니다: '{assetPath}'"));
        }
    }
}