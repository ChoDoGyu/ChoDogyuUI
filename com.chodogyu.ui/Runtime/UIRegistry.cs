using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CDG.Core.Results;
using UnityEngine;

namespace CDG.UI
{
    /// <summary>
    /// UIController가 생성하고 관리할 UIView Prefab 목록을 보관하는 Registry Asset입니다.
    /// 각 View는 자신의 UIId를 통해 식별되며, Registry의 데이터는 외부에서 직접 수정할 수 없습니다.
    /// </summary>
    public sealed class UIRegistry : ScriptableObject
    {
        [SerializeField]
        private List<UIView> prefabs = new List<UIView>();

        private List<UIView> readOnlyPrefabsSource;
        private ReadOnlyCollection<UIView> readOnlyPrefabs;

        private Dictionary<UIId, UIView> prefabsById;
        private ResultError lookupBuildError;

        /// <summary>
        /// Registry에 등록된 View Prefab 수를 반환합니다.
        /// </summary>
        public int Count => prefabs.Count;

        /// <summary>
        /// Registry에 등록된 View Prefab을 직렬화된 순서대로 제공합니다.
        /// 반환된 컬렉션을 통해 항목을 추가, 제거 또는 교체할 수 없습니다.
        /// </summary>
        public IReadOnlyList<UIView> Prefabs
        {
            get
            {
                if (!ReferenceEquals(readOnlyPrefabsSource, prefabs))
                {
                    readOnlyPrefabsSource = prefabs;
                    readOnlyPrefabs = prefabs.AsReadOnly();
                }

                return readOnlyPrefabs;
            }
        }

        /// <summary>
        /// 지정한 UI ID가 Registry에 등록되어 있는지 확인합니다.
        /// 유효하지 않은 ID이거나 Registry 구성이 잘못된 경우 false를 반환합니다.
        /// </summary>
        /// <param name="id">확인할 UI ID입니다.</param>
        /// <returns>정상적으로 등록된 Prefab이 존재하면 true입니다.</returns>
        public bool Contains(UIId id)
        {
            if (id.IsEmpty)
            {
                return false;
            }

            Result buildResult = EnsureLookup();

            if (buildResult.IsFailure)
            {
                return false;
            }

            return prefabsById.ContainsKey(id);
        }

        /// <summary>
        /// 지정한 UI ID의 Prefab을 조회합니다.
        /// 유효하지 않은 ID, 잘못된 Registry 구성 또는 미등록 ID는 false를 반환합니다.
        /// </summary>
        /// <param name="id">조회할 UI ID입니다.</param>
        /// <param name="prefab">조회에 성공한 경우 등록된 UIView Prefab입니다.</param>
        /// <returns>Prefab 조회에 성공하면 true입니다.</returns>
        public bool TryGet(UIId id, out UIView prefab)
        {
            prefab = null;

            if (id.IsEmpty)
            {
                return false;
            }

            Result buildResult = EnsureLookup();

            if (buildResult.IsFailure)
            {
                return false;
            }

            return prefabsById.TryGetValue(id, out prefab);
        }

        /// <summary>
        /// 지정한 UI ID의 Prefab을 조회하고 성공 또는 실패 결과를 반환합니다.
        /// 잘못된 ID, Registry 구성 오류, 미등록 ID를 서로 다른 오류 코드로 구분할 수 있습니다.
        /// </summary>
        /// <param name="id">조회할 UI ID입니다.</param>
        /// <returns>성공 시 등록된 UIView Prefab을 포함하고, 실패 시 오류 정보를 포함하는 결과입니다.</returns>
        public Result<UIView> Get(UIId id)
        {
            if (id.IsEmpty)
            {
                return Result<UIView>.Failure(new ResultError(
                    UIErrorCodes.InvalidId,
                    "UI ID는 비어 있을 수 없습니다."));
            }

            Result buildResult = EnsureLookup();

            if (buildResult.IsFailure)
            {
                return Result<UIView>.Failure(buildResult.Error);
            }

            if (!prefabsById.TryGetValue(id, out UIView prefab))
            {
                return Result<UIView>.Failure(new ResultError(
                    UIErrorCodes.NotFound,
                    $"Registry에 등록되지 않은 UI ID입니다: '{id}'"));
            }

            return Result<UIView>.Success(prefab);
        }

        /// <summary>
        /// 현재 Registry의 Prefab 전체를 지정한 목록으로 교체합니다.
        /// 새 목록은 먼저 독립된 스냅샷으로 생성하고 검증하며,
        /// 검증에 실패하면 기존 Registry 데이터는 변경하지 않습니다.
        /// </summary>
        /// <param name="source">Registry에 새로 등록할 UIView Prefab 목록입니다.</param>
        /// <returns>교체 작업의 성공 또는 실패 결과입니다.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/>가 null인 경우 발생합니다.
        /// </exception>
        internal Result ReplacePrefabs(IEnumerable<UIView> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            List<UIView> snapshot = new List<UIView>(source);
            Result<Dictionary<UIId, UIView>> lookupResult = BuildLookup(snapshot);

            if (lookupResult.IsFailure)
            {
                return Result.Failure(lookupResult.Error);
            }

            prefabs = snapshot;
            readOnlyPrefabsSource = null;
            readOnlyPrefabs = null;

            prefabsById = lookupResult.Value;
            lookupBuildError = null;

            return Result.Success();
        }

        private Result EnsureLookup()
        {
            if (prefabsById != null)
            {
                return lookupBuildError == null
                    ? Result.Success()
                    : Result.Failure(lookupBuildError);
            }

            Result<Dictionary<UIId, UIView>> lookupResult = BuildLookup(prefabs);

            if (lookupResult.IsFailure)
            {
                prefabsById = new Dictionary<UIId, UIView>();
                lookupBuildError = lookupResult.Error;

                return Result.Failure(lookupBuildError);
            }

            prefabsById = lookupResult.Value;
            lookupBuildError = null;

            return Result.Success();
        }

        private Result<Dictionary<UIId, UIView>> BuildLookup(IReadOnlyList<UIView> source)
        {
            Dictionary<UIId, UIView> lookup = new Dictionary<UIId, UIView>();

            for (int i = 0; i < source.Count; i++)
            {
                UIView prefab = source[i];

                if (prefab == null)
                {
                    return Result<Dictionary<UIId, UIView>>.Failure(new ResultError(
                        UIErrorCodes.InvalidRegistry,
                        $"Registry의 {i}번 항목에 Prefab이 지정되지 않았습니다."));
                }

                if (prefab.Id.IsEmpty)
                {
                    return Result<Dictionary<UIId, UIView>>.Failure(new ResultError(
                        UIErrorCodes.InvalidId,
                        $"Registry의 '{prefab.name}' Prefab에 유효한 UI ID가 지정되지 않았습니다."));
                }

                if (!lookup.TryAdd(prefab.Id, prefab))
                {
                    return Result<Dictionary<UIId, UIView>>.Failure(new ResultError(
                        UIErrorCodes.DuplicateId,
                        $"Registry에 중복된 UI ID가 등록되어 있습니다: '{prefab.Id}'"));
                }
            }

            return Result<Dictionary<UIId, UIView>>.Success(lookup);
        }

        private void InvalidateCaches()
        {
            readOnlyPrefabsSource = null;
            readOnlyPrefabs = null;
            prefabsById = null;
            lookupBuildError = null;
        }

        private void OnEnable()
        {
            InvalidateCaches();
        }

        private void OnValidate()
        {
            InvalidateCaches();
        }
    }
}