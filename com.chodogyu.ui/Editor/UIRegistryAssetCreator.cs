using System;
using System.IO;
using CDG.Core.Results;
using UnityEditor;
using UnityEngine;

namespace CDG.UI.Editor
{
    /// <summary>
    /// 지정한 프로젝트 Asset 경로에 새로운 UIRegistry Asset을 생성합니다.
    /// 기존 Asset을 덮어쓰지 않으며 유효하지 않은 경로는 생성 전에 차단합니다.
    /// </summary>
    internal static class UIRegistryAssetCreator
    {
        /// <summary>
        /// 지정한 경로에 빈 UIRegistry Asset을 생성합니다.
        /// 경로는 Assets 폴더 내부의 .asset 파일이어야 합니다.
        /// </summary>
        /// <param name="assetPath">생성할 Registry의 프로젝트 상대 Asset 경로입니다.</param>
        /// <returns>생성된 UIRegistry 또는 생성 실패 정보를 포함하는 결과입니다.</returns>
        internal static Result<UIRegistry> Create(string assetPath)
        {
            Result validationResult = ValidateAssetPath(assetPath);

            if (validationResult.IsFailure)
            {
                return Result<UIRegistry>.Failure(validationResult.Error);
            }

            if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
            {
                return Result<UIRegistry>.Failure(new ResultError(
                    UIEditorErrorCodes.AssetAlreadyExists,
                    $"경로 '{assetPath}'에 이미 Asset이 존재합니다."));
            }

            UIRegistry registry = ScriptableObject.CreateInstance<UIRegistry>();
            AssetDatabase.CreateAsset(registry, assetPath);
            AssetDatabase.SaveAssets();

            Selection.activeObject = registry;
            EditorGUIUtility.PingObject(registry);

            return Result<UIRegistry>.Success(registry);
        }

        private static Result ValidateAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return Result.Failure(new ResultError(
                    UIEditorErrorCodes.InvalidAssetPath,
                    "UIRegistry를 생성할 Asset 경로가 비어 있습니다."));
            }

            string normalizedPath = assetPath.Replace('\\', '/');

            if (!normalizedPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                !string.Equals(Path.GetExtension(normalizedPath), ".asset", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure(new ResultError(
                    UIEditorErrorCodes.InvalidAssetPath,
                    "UIRegistry 경로는 Assets 폴더 내부의 .asset 파일이어야 합니다."));
            }

            string directoryPath = Path.GetDirectoryName(normalizedPath)?.Replace('\\', '/');

            if (string.IsNullOrEmpty(directoryPath) || !AssetDatabase.IsValidFolder(directoryPath))
            {
                return Result.Failure(new ResultError(
                    UIEditorErrorCodes.InvalidAssetPath,
                    $"UIRegistry를 생성할 폴더가 존재하지 않습니다: '{directoryPath}'"));
            }

            return Result.Success();
        }
    }
}