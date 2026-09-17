using System;
using System.IO;
using CDG.Core.Results;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CDG.UI.Editor
{
    /// <summary>
    /// UI Framework에서 사용할 Screen, Popup, Overlay Prefab을 생성합니다.
    /// 생성된 View는 Runtime Registry 사용 규칙에 맞게 기본 비활성 상태로 저장됩니다.
    /// </summary>
    internal static class UIViewPrefabCreator
    {
        /// <summary>
        /// 지정한 경로에 UIScreen Prefab을 생성합니다.
        /// </summary>
        internal static Result<UIScreen> CreateScreen(string assetPath, UIId id)
        {
            return CreateView<UIScreen>(assetPath, id, null);
        }

        /// <summary>
        /// 지정한 경로에 UIPopup Prefab을 생성합니다.
        /// Popup의 하위 UI 입력 차단 여부는 기본적으로 true입니다.
        /// </summary>
        internal static Result<UIPopup> CreatePopup(string assetPath, UIId id, bool blocksInput = true)
        {
            return CreateView<UIPopup>(assetPath, id, popup => popup.SetBlocksInput(blocksInput));
        }

        /// <summary>
        /// 지정한 경로에 UIOverlay Prefab을 생성합니다.
        /// Overlay의 표시 Layer와 하위 UI 입력 차단 여부를 함께 지정할 수 있습니다.
        /// </summary>
        internal static Result<UIOverlay> CreateOverlay(string assetPath, UIId id, UIOverlayLayer overlayLayer = UIOverlayLayer.Normal, bool blocksInput = false)
        {
            return CreateView<UIOverlay>(
                assetPath,
                id,
                overlay =>
                {
                    overlay.SetOverlayLayer(overlayLayer);
                    overlay.SetBlocksInput(blocksInput);
                });
        }

        private static Result<T> CreateView<T>(string assetPath, UIId id, Action<T> configure) where T : UIView
        {
            Result validationResult = Validate(assetPath, id);

            if (validationResult.IsFailure)
            {
                return Result<T>.Failure(validationResult.Error);
            }

            Scene previewScene = EditorSceneManager.NewPreviewScene();

            try
            {
                string prefabName = Path.GetFileNameWithoutExtension(assetPath);

                GameObject rootObject = new GameObject(
                    prefabName,
                    typeof(RectTransform),
                    typeof(CanvasGroup));

                SceneManager.MoveGameObjectToScene(rootObject, previewScene);

                RectTransform rectTransform = rootObject.GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.localScale = Vector3.one;

                T view = rootObject.AddComponent<T>();
                view.SetId(id);
                configure?.Invoke(view);

                rootObject.SetActive(false);

                GameObject prefabObject = PrefabUtility.SaveAsPrefabAsset(rootObject, assetPath);

                if (prefabObject == null)
                {
                    return Result<T>.Failure(new ResultError(
                        UIEditorErrorCodes.PrefabCreationFailed,
                        $"UI View Prefab을 생성하지 못했습니다: '{assetPath}'"));
                }

                T prefabView = prefabObject.GetComponent<T>();

                if (prefabView == null)
                {
                    return Result<T>.Failure(new ResultError(
                        UIEditorErrorCodes.PrefabCreationFailed,
                        $"생성된 Prefab에서 '{typeof(T).Name}' Component를 찾을 수 없습니다."));
                }

                AssetDatabase.SaveAssets();

                Selection.activeObject = prefabObject;
                EditorGUIUtility.PingObject(prefabObject);

                return Result<T>.Success(prefabView);
            }
            finally
            {
                if (previewScene.IsValid())
                {
                    EditorSceneManager.ClosePreviewScene(previewScene);
                }
            }
        }

        private static Result Validate(string assetPath, UIId id)
        {
            if (id.IsEmpty)
            {
                return Result.Failure(new ResultError(
                    UIEditorErrorCodes.InvalidViewId,
                    "View Prefab의 UI ID는 비어 있을 수 없습니다."));
            }

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return Result.Failure(new ResultError(
                    UIEditorErrorCodes.InvalidAssetPath,
                    "View Prefab을 생성할 Asset 경로가 비어 있습니다."));
            }

            string normalizedPath = assetPath.Replace('\\', '/');

            if (!normalizedPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                !string.Equals(Path.GetExtension(normalizedPath), ".prefab", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure(new ResultError(
                    UIEditorErrorCodes.InvalidAssetPath,
                    "View Prefab 경로는 Assets 폴더 내부의 .prefab 파일이어야 합니다."));
            }

            string directoryPath = Path.GetDirectoryName(normalizedPath)?.Replace('\\', '/');

            if (string.IsNullOrEmpty(directoryPath) || !AssetDatabase.IsValidFolder(directoryPath))
            {
                return Result.Failure(new ResultError(
                    UIEditorErrorCodes.InvalidAssetPath,
                    $"View Prefab을 생성할 폴더가 존재하지 않습니다: '{directoryPath}'"));
            }

            if (AssetDatabase.LoadMainAssetAtPath(normalizedPath) != null)
            {
                return Result.Failure(new ResultError(
                    UIEditorErrorCodes.AssetAlreadyExists,
                    $"경로 '{normalizedPath}'에 이미 Asset이 존재합니다."));
            }

            return Result.Success();
        }
    }
}