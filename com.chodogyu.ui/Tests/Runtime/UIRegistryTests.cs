using System;
using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UIRegistryTests
    {
        private readonly List<GameObject> createdGameObjects = new List<GameObject>();
        private UIRegistry registry;

        [SetUp]
        public void SetUp()
        {
            registry = ScriptableObject.CreateInstance<UIRegistry>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in createdGameObjects)
            {
                if (gameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(gameObject);
                }
            }

            createdGameObjects.Clear();

            if (registry != null)
            {
                UnityEngine.Object.DestroyImmediate(registry);
            }
        }

        [Test]
        public void NewRegistry_IsEmpty()
        {
            Assert.That(registry.Count, Is.Zero);
            Assert.That(registry.Prefabs, Is.Empty);
        }

        [Test]
        public void ReplacePrefabs_ValidSource_ReplacesAllPrefabsInOrder()
        {
            UIScreen screen = CreateView<UIScreen>("screen.main");
            UIPopup popup = CreateView<UIPopup>("popup.settings");

            Result result = registry.ReplacePrefabs(new UIView[]
            {
                screen,
                popup
            });

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(registry.Count, Is.EqualTo(2));
            Assert.That(registry.Prefabs[0], Is.SameAs(screen));
            Assert.That(registry.Prefabs[1], Is.SameAs(popup));
        }

        [Test]
        public void ReplacePrefabs_EmptySource_ClearsRegistry()
        {
            UIScreen screen = CreateView<UIScreen>("screen.main");

            Result initialResult = registry.ReplacePrefabs(new UIView[]
            {
                screen
            });

            Assert.That(initialResult.IsSuccess, Is.True);
            Assert.That(registry.Count, Is.EqualTo(1));

            Result result = registry.ReplacePrefabs(Array.Empty<UIView>());

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(registry.Count, Is.Zero);
            Assert.That(registry.Prefabs, Is.Empty);
        }

        [Test]
        public void ReplacePrefabs_NullSource_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                registry.ReplacePrefabs(null));
        }

        [Test]
        public void ReplacePrefabs_NullPrefab_ReturnsInvalidRegistry()
        {
            Result result = registry.ReplacePrefabs(new UIView[]
            {
                null
            });

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.InvalidRegistry));
        }

        [Test]
        public void ReplacePrefabs_EmptyId_ReturnsInvalidId()
        {
            UIScreen screen = CreateView<UIScreen>(string.Empty);

            Result result = registry.ReplacePrefabs(new UIView[]
            {
                screen
            });

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.InvalidId));
        }

        [Test]
        public void ReplacePrefabs_DuplicateId_ReturnsDuplicateId()
        {
            UIScreen first = CreateView<UIScreen>("screen.main");
            UIPopup second = CreateView<UIPopup>("screen.main");

            Result result = registry.ReplacePrefabs(new UIView[]
            {
                first,
                second
            });

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.DuplicateId));
        }

        [Test]
        public void ReplacePrefabs_InvalidSource_PreservesExistingRegistry()
        {
            UIScreen original = CreateView<UIScreen>("screen.original");

            Result initialResult = registry.ReplacePrefabs(new UIView[]
            {
                original
            });

            Assert.That(initialResult.IsSuccess, Is.True);

            UIScreen first = CreateView<UIScreen>("screen.duplicate");
            UIPopup second = CreateView<UIPopup>("screen.duplicate");

            Result result = registry.ReplacePrefabs(new UIView[]
            {
                first,
                second
            });

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.DuplicateId));

            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.Prefabs[0], Is.SameAs(original));

            Result<UIView> lookupResult = registry.Get(new UIId("screen.original"));

            Assert.That(lookupResult.IsSuccess, Is.True);
            Assert.That(lookupResult.Value, Is.SameAs(original));
        }

        [Test]
        public void ReplacePrefabs_CreatesSnapshotIndependentFromSourceChanges()
        {
            UIScreen original = CreateView<UIScreen>("screen.main");

            List<UIView> source = new List<UIView>
            {
                original
            };

            Result result = registry.ReplacePrefabs(source);

            Assert.That(result.IsSuccess, Is.True);

            UIPopup addedLater = CreateView<UIPopup>("popup.settings");

            source[0] = addedLater;
            source.Add(original);

            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.Prefabs[0], Is.SameAs(original));
        }

        [Test]
        public void ReplacePrefabs_AfterPrefabsAccess_RebuildsReadOnlyView()
        {
            UIScreen first = CreateView<UIScreen>("screen.first");

            Result firstResult = registry.ReplacePrefabs(new UIView[]
            {
                first
            });

            Assert.That(firstResult.IsSuccess, Is.True);

            IReadOnlyList<UIView> firstView = registry.Prefabs;

            UIScreen second = CreateView<UIScreen>("screen.second");

            Result secondResult = registry.ReplacePrefabs(new UIView[]
            {
                second
            });

            Assert.That(secondResult.IsSuccess, Is.True);

            IReadOnlyList<UIView> secondView = registry.Prefabs;

            Assert.That(secondView, Is.Not.SameAs(firstView));

            Assert.That(firstView.Count, Is.EqualTo(1));
            Assert.That(firstView[0], Is.SameAs(first));

            Assert.That(secondView.Count, Is.EqualTo(1));
            Assert.That(secondView[0], Is.SameAs(second));
        }

        [Test]
        public void Contains_RegisteredId_ReturnsTrue()
        {
            UIScreen screen = CreateView<UIScreen>("screen.main");

            Result replaceResult = registry.ReplacePrefabs(new UIView[]
            {
                screen
            });

            Assert.That(replaceResult.IsSuccess, Is.True);

            bool result = registry.Contains(new UIId("screen.main"));

            Assert.That(result, Is.True);
        }

        [Test]
        public void Contains_InvalidOrMissingId_ReturnsFalse()
        {
            UIScreen screen = CreateView<UIScreen>("screen.main");

            Result replaceResult = registry.ReplacePrefabs(new UIView[]
            {
                screen
            });

            Assert.That(replaceResult.IsSuccess, Is.True);

            Assert.That(registry.Contains(default), Is.False);
            Assert.That(registry.Contains(new UIId("screen.missing")), Is.False);
        }

        [Test]
        public void TryGet_RegisteredId_ReturnsPrefab()
        {
            UIPopup popup = CreateView<UIPopup>("popup.settings");

            Result replaceResult = registry.ReplacePrefabs(new UIView[]
            {
                popup
            });

            Assert.That(replaceResult.IsSuccess, Is.True);

            bool result = registry.TryGet(new UIId("popup.settings"), out UIView prefab);

            Assert.That(result, Is.True);
            Assert.That(prefab, Is.SameAs(popup));
        }

        [Test]
        public void TryGet_MissingId_ReturnsFalseAndNull()
        {
            bool result = registry.TryGet(new UIId("popup.missing"), out UIView prefab);

            Assert.That(result, Is.False);
            Assert.That(prefab, Is.Null);
        }

        [Test]
        public void Get_RegisteredId_ReturnsSuccessWithPrefab()
        {
            UIOverlay overlay = CreateView<UIOverlay>("overlay.notice");

            Result replaceResult = registry.ReplacePrefabs(new UIView[]
            {
                overlay
            });

            Assert.That(replaceResult.IsSuccess, Is.True);

            Result<UIView> result = registry.Get(new UIId("overlay.notice"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.SameAs(overlay));
        }

        [Test]
        public void Get_InvalidId_ReturnsInvalidId()
        {
            Result<UIView> result = registry.Get(default);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.InvalidId));
        }

        [Test]
        public void Get_MissingId_ReturnsNotFound()
        {
            Result<UIView> result = registry.Get(new UIId("screen.missing"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.NotFound));
        }

        [Test]
        public void ReplacePrefabs_AfterLookup_UsesNewLookup()
        {
            UIScreen first = CreateView<UIScreen>("screen.first");

            Result firstReplaceResult = registry.ReplacePrefabs(new UIView[]
            {
                first
            });

            Assert.That(firstReplaceResult.IsSuccess, Is.True);

            Result<UIView> firstLookupResult = registry.Get(new UIId("screen.first"));

            Assert.That(firstLookupResult.IsSuccess, Is.True);
            Assert.That(firstLookupResult.Value, Is.SameAs(first));

            UIScreen second = CreateView<UIScreen>("screen.second");

            Result secondReplaceResult = registry.ReplacePrefabs(new UIView[]
            {
                second
            });

            Assert.That(secondReplaceResult.IsSuccess, Is.True);

            Result<UIView> oldLookupResult = registry.Get(new UIId("screen.first"));
            Result<UIView> newLookupResult = registry.Get(new UIId("screen.second"));

            Assert.That(oldLookupResult.IsFailure, Is.True);
            Assert.That(oldLookupResult.Error.Code, Is.EqualTo(UIErrorCodes.NotFound));

            Assert.That(newLookupResult.IsSuccess, Is.True);
            Assert.That(newLookupResult.Value, Is.SameAs(second));
        }

        [Test]
        public void Lookup_IsCaseSensitive()
        {
            UIScreen screen = CreateView<UIScreen>("screen.main");

            Result replaceResult = registry.ReplacePrefabs(new UIView[]
            {
                screen
            });

            Assert.That(replaceResult.IsSuccess, Is.True);

            Result<UIView> result = registry.Get(new UIId("Screen.Main"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.NotFound));
        }

        private T CreateView<T>(string id) where T : UIView
        {
            GameObject gameObject = new GameObject(typeof(T).Name);
            createdGameObjects.Add(gameObject);

            gameObject.AddComponent<CanvasGroup>();

            T view = gameObject.AddComponent<T>();
            view.SetId(new UIId(id));

            return view;
        }
    }
}