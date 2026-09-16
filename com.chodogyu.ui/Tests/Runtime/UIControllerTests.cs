using System.Collections.Generic;
using System.Reflection;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UIControllerTests
    {
        private readonly List<GameObject> createdGameObjects = new List<GameObject>();

        private UIRegistry registry;
        private UIController controller;
        private Transform screenLayer;
        private Transform overlayLayer;
        private Transform popupLayer;
        private Transform topOverlayLayer;

        [SetUp]
        public void SetUp()
        {
            registry = ScriptableObject.CreateInstance<UIRegistry>();

            GameObject controllerObject = CreateGameObject("UIController");
            controller = controllerObject.AddComponent<UIController>();

            screenLayer = CreateLayer("Screen Layer", controllerObject.transform);
            overlayLayer = CreateLayer("Overlay Layer", controllerObject.transform);
            popupLayer = CreateLayer("Popup Layer", controllerObject.transform);
            topOverlayLayer = CreateLayer("Top Overlay Layer", controllerObject.transform);

            controller.SetLayers(
                screenLayer,
                overlayLayer,
                popupLayer,
                topOverlayLayer);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in createdGameObjects)
            {
                if (gameObject != null)
                {
                    Object.DestroyImmediate(gameObject);
                }
            }

            createdGameObjects.Clear();

            if (registry != null)
            {
                Object.DestroyImmediate(registry);
            }
        }

        [Test]
        public void SetRegistryAndLayers_UpdatesConfiguration()
        {
            controller.SetRegistry(registry);

            Assert.That(controller.Registry, Is.SameAs(registry));
            Assert.That(controller.ScreenLayer, Is.SameAs(screenLayer));
            Assert.That(controller.OverlayLayer, Is.SameAs(overlayLayer));
            Assert.That(controller.PopupLayer, Is.SameAs(popupLayer));
            Assert.That(controller.TopOverlayLayer, Is.SameAs(topOverlayLayer));
        }

        [Test]
        public void GetOrCreate_InvalidId_ReturnsInvalidId()
        {
            controller.SetRegistry(registry);

            Result<UIScreen> result = controller.GetOrCreate<UIScreen>(default);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.InvalidId));
        }

        [Test]
        public void GetOrCreate_MissingRegistry_ReturnsMissingRegistry()
        {
            Result<UIScreen> result = controller.GetOrCreate<UIScreen>(
                new UIId("screen.main"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.MissingRegistry));
        }

        [Test]
        public void GetOrCreate_MissingId_ReturnsNotFound()
        {
            controller.SetRegistry(registry);

            Result<UIScreen> result = controller.GetOrCreate<UIScreen>(
                new UIId("screen.missing"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.NotFound));
        }

        [Test]
        public void GetOrCreate_WrongRequestedType_ReturnsInvalidType()
        {
            UIScreen prefab = CreatePrefab<UIScreen>("screen.main");
            ConfigureRegistry(prefab);

            Result<UIPopup> result = controller.GetOrCreate<UIPopup>(
                new UIId("screen.main"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.InvalidType));
        }

        [Test]
        public void GetOrCreate_UnsupportedUIViewType_ReturnsInvalidType()
        {
            TestUnsupportedView prefab = CreatePrefab<TestUnsupportedView>("unsupported");
            ConfigureRegistry(prefab);

            Result<TestUnsupportedView> result =
                controller.GetOrCreate<TestUnsupportedView>(
                    new UIId("unsupported"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.InvalidType));
        }

        [Test]
        public void GetOrCreate_Screen_CreatesUnderScreenLayer()
        {
            UIScreen prefab = CreatePrefab<UIScreen>("screen.main");
            ConfigureRegistry(prefab);

            Result<UIScreen> result = controller.GetOrCreate<UIScreen>(
                new UIId("screen.main"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.transform.parent, Is.SameAs(screenLayer));
        }

        [Test]
        public void GetOrCreate_Popup_CreatesUnderPopupLayer()
        {
            UIPopup prefab = CreatePrefab<UIPopup>("popup.settings");
            ConfigureRegistry(prefab);

            Result<UIPopup> result = controller.GetOrCreate<UIPopup>(
                new UIId("popup.settings"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.transform.parent, Is.SameAs(popupLayer));
        }

        [Test]
        public void GetOrCreate_NormalOverlay_CreatesUnderOverlayLayer()
        {
            UIOverlay prefab = CreatePrefab<UIOverlay>("overlay.notice");
            ConfigureRegistry(prefab);

            Result<UIOverlay> result = controller.GetOrCreate<UIOverlay>(
                new UIId("overlay.notice"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.transform.parent, Is.SameAs(overlayLayer));
        }

        [Test]
        public void GetOrCreate_TopmostOverlay_CreatesUnderTopOverlayLayer()
        {
            UIOverlay prefab = CreatePrefab<UIOverlay>("overlay.loading");
            SetOverlayLayer(prefab, UIOverlayLayer.Topmost);
            ConfigureRegistry(prefab);

            Result<UIOverlay> result = controller.GetOrCreate<UIOverlay>(
                new UIId("overlay.loading"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.transform.parent, Is.SameAs(topOverlayLayer));
        }

        [Test]
        public void GetOrCreate_InactivePrefab_CreatesInactiveInstance()
        {
            UIScreen prefab = CreatePrefab<UIScreen>("screen.main");
            ConfigureRegistry(prefab);

            Result<UIScreen> result = controller.GetOrCreate<UIScreen>(
                new UIId("screen.main"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void GetOrCreate_SameId_ReturnsSameInstance()
        {
            UIScreen prefab = CreatePrefab<UIScreen>("screen.main");
            ConfigureRegistry(prefab);

            Result<UIScreen> firstResult = controller.GetOrCreate<UIScreen>(
                new UIId("screen.main"));

            Result<UIScreen> secondResult = controller.GetOrCreate<UIScreen>(
                new UIId("screen.main"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(secondResult.Value, Is.SameAs(firstResult.Value));
        }

        [Test]
        public void GetOrCreate_DestroyedCachedInstance_RecreatesInstance()
        {
            UIScreen prefab = CreatePrefab<UIScreen>("screen.main");
            ConfigureRegistry(prefab);

            Result<UIScreen> firstResult = controller.GetOrCreate<UIScreen>(
                new UIId("screen.main"));

            Assert.That(firstResult.IsSuccess, Is.True);

            UIScreen firstInstance = firstResult.Value;

            Object.DestroyImmediate(firstInstance.gameObject);

            Result<UIScreen> secondResult = controller.GetOrCreate<UIScreen>(
                new UIId("screen.main"));

            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(secondResult.Value, Is.Not.SameAs(firstInstance));
            Assert.That(secondResult.Value.transform.parent, Is.SameAs(screenLayer));
        }

        [Test]
        public void GetOrCreate_MissingRequiredLayer_ReturnsMissingLayer()
        {
            UIScreen prefab = CreatePrefab<UIScreen>("screen.main");
            ConfigureRegistry(prefab);

            controller.SetLayers(
                null,
                overlayLayer,
                popupLayer,
                topOverlayLayer);

            Result<UIScreen> result = controller.GetOrCreate<UIScreen>(
                new UIId("screen.main"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.MissingLayer));
        }

        [Test]
        public void GetOrCreate_AfterMissingLayerFixed_CanCreateInstance()
        {
            UIScreen prefab = CreatePrefab<UIScreen>("screen.main");
            ConfigureRegistry(prefab);

            controller.SetLayers(
                null,
                overlayLayer,
                popupLayer,
                topOverlayLayer);

            Result<UIScreen> failedResult = controller.GetOrCreate<UIScreen>(
                new UIId("screen.main"));

            Assert.That(failedResult.IsFailure, Is.True);

            controller.SetLayers(
                screenLayer,
                overlayLayer,
                popupLayer,
                topOverlayLayer);

            Result<UIScreen> successResult = controller.GetOrCreate<UIScreen>(
                new UIId("screen.main"));

            Assert.That(successResult.IsSuccess, Is.True);
            Assert.That(successResult.Value.transform.parent, Is.SameAs(screenLayer));
        }

        [Test]
        public void GetOrCreate_CachedInstanceRequestedAsWrongType_ReturnsInvalidType()
        {
            UIScreen prefab = CreatePrefab<UIScreen>("screen.main");
            ConfigureRegistry(prefab);

            Result<UIScreen> screenResult = controller.GetOrCreate<UIScreen>(
                new UIId("screen.main"));

            Assert.That(screenResult.IsSuccess, Is.True);

            Result<UIPopup> popupResult = controller.GetOrCreate<UIPopup>(
                new UIId("screen.main"));

            Assert.That(popupResult.IsFailure, Is.True);
            Assert.That(popupResult.Error.Code, Is.EqualTo(UIErrorCodes.InvalidType));
        }

        [Test]
        public void IsOpen_InvalidId_ReturnsFalse()
        {
            Assert.That(controller.IsOpen(default), Is.False);
        }

        [Test]
        public void IsOpen_UncreatedView_ReturnsFalse()
        {
            Assert.That(
                controller.IsOpen(new UIId("screen.main")),
                Is.False);
        }

        [Test]
        public void IsOpen_ClosedView_ReturnsFalse()
        {
            UIScreen prefab = CreatePrefab<UIScreen>("screen.main");
            ConfigureRegistry(prefab);

            Result<UIScreen> result = controller.GetOrCreate<UIScreen>(
                new UIId("screen.main"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(controller.IsOpen(new UIId("screen.main")), Is.False);
        }

        [Test]
        public void IsOpen_OpenView_ReturnsTrue()
        {
            UIScreen prefab = CreatePrefab<UIScreen>("screen.main");
            ConfigureRegistry(prefab);

            Result<UIScreen> result = controller.GetOrCreate<UIScreen>(
                new UIId("screen.main"));

            Assert.That(result.IsSuccess, Is.True);

            result.Value.SetState(UIViewState.Open);

            Assert.That(controller.IsOpen(new UIId("screen.main")), Is.True);
        }

        [Test]
        public void IsOpen_DestroyedCachedInstance_ReturnsFalse()
        {
            UIScreen prefab = CreatePrefab<UIScreen>("screen.main");
            ConfigureRegistry(prefab);

            Result<UIScreen> result = controller.GetOrCreate<UIScreen>(
                new UIId("screen.main"));

            Assert.That(result.IsSuccess, Is.True);

            result.Value.SetState(UIViewState.Open);
            Object.DestroyImmediate(result.Value.gameObject);

            Assert.That(controller.IsOpen(new UIId("screen.main")), Is.False);
        }

        [Test]
        public void RemoveCachedInstance_AfterCreation_NextRequestCreatesNewInstance()
        {
            UIScreen prefab = CreatePrefab<UIScreen>("screen.main");
            ConfigureRegistry(prefab);

            Result<UIScreen> firstResult = controller.GetOrCreate<UIScreen>(
                new UIId("screen.main"));

            Assert.That(firstResult.IsSuccess, Is.True);

            bool removed = controller.RemoveCachedInstance(
                new UIId("screen.main"));

            Assert.That(removed, Is.True);

            Result<UIScreen> secondResult = controller.GetOrCreate<UIScreen>(
                new UIId("screen.main"));

            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(secondResult.Value, Is.Not.SameAs(firstResult.Value));
        }

        [Test]
        public void ClearInstanceCache_DoesNotDestroyExistingInstance()
        {
            UIScreen prefab = CreatePrefab<UIScreen>("screen.main");
            ConfigureRegistry(prefab);

            Result<UIScreen> firstResult = controller.GetOrCreate<UIScreen>(
                new UIId("screen.main"));

            Assert.That(firstResult.IsSuccess, Is.True);

            UIScreen firstInstance = firstResult.Value;

            controller.ClearInstanceCache();

            Assert.That(firstInstance, Is.Not.Null);

            Result<UIScreen> secondResult = controller.GetOrCreate<UIScreen>(
                new UIId("screen.main"));

            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(secondResult.Value, Is.Not.SameAs(firstInstance));
        }

        private void ConfigureRegistry(params UIView[] prefabs)
        {
            Result result = registry.ReplacePrefabs(prefabs);

            Assert.That(result.IsSuccess, Is.True);

            controller.SetRegistry(registry);
        }

        private T CreatePrefab<T>(string id) where T : UIView
        {
            GameObject gameObject = CreateGameObject($"{typeof(T).Name} Prefab");
            gameObject.AddComponent<CanvasGroup>();

            T view = gameObject.AddComponent<T>();
            view.SetId(new UIId(id));

            gameObject.SetActive(false);

            return view;
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            createdGameObjects.Add(gameObject);

            return gameObject;
        }

        private Transform CreateLayer(string name, Transform parent)
        {
            GameObject layerObject = new GameObject(name, typeof(RectTransform));
            layerObject.transform.SetParent(parent, false);

            return layerObject.transform;
        }

        private void SetOverlayLayer(UIOverlay overlay, UIOverlayLayer layer)
        {
            FieldInfo field = typeof(UIOverlay).GetField(
                "overlayLayer",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);

            field.SetValue(overlay, layer);
        }

        private sealed class TestUnsupportedView : UIView
        {
        }
    }
}