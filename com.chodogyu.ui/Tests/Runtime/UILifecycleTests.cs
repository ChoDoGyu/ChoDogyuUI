using System;
using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UILifecycleTests
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

            controller.SetRegistry(registry);
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
        public void OpenView_ClosedView_OpensAndActivates()
        {
            LifecycleScreen prefab = CreatePrefab<LifecycleScreen>("screen.main", true);
            ConfigureRegistry(prefab);

            Result<LifecycleScreen> result = controller.OpenView<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.State, Is.EqualTo(UIViewState.Open));
            Assert.That(result.Value.IsOpen, Is.True);
            Assert.That(result.Value.IsTransitioning, Is.False);
            Assert.That(result.Value.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void OpenView_EnablesCanvasGroupInput()
        {
            LifecycleScreen prefab = CreatePrefab<LifecycleScreen>("screen.main", true);
            ConfigureRegistry(prefab);

            Result<LifecycleScreen> result = controller.OpenView<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(result.IsSuccess, Is.True);

            CanvasGroup canvasGroup = result.Value.CanvasGroup;

            Assert.That(canvasGroup.interactable, Is.True);
            Assert.That(canvasGroup.blocksRaycasts, Is.True);
        }

        [Test]
        public void OpenView_CallsOpeningBeforeActivationAndOpenedAfterActivation()
        {
            LifecycleScreen prefab = CreatePrefab<LifecycleScreen>("screen.main", true);
            ConfigureRegistry(prefab);

            Result<LifecycleScreen> result = controller.OpenView<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(result.IsSuccess, Is.True);

            LifecycleScreen view = result.Value;

            Assert.That(view.OpeningCount, Is.EqualTo(1));
            Assert.That(view.OpenedCount, Is.EqualTo(1));

            Assert.That(view.WasActiveDuringOpening, Is.False);
            Assert.That(view.EnableCountDuringOpening, Is.Zero);

            Assert.That(view.WasActiveDuringOpened, Is.True);
            Assert.That(view.EnableCountDuringOpened, Is.EqualTo(1));
        }

        [Test]
        public void CloseView_OpenView_ClosesAndDeactivates()
        {
            LifecycleScreen prefab = CreatePrefab<LifecycleScreen>("screen.main", true);
            ConfigureRegistry(prefab);

            Result<LifecycleScreen> openResult = controller.OpenView<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(openResult.IsSuccess, Is.True);

            Result closeResult = controller.CloseView(openResult.Value);

            Assert.That(closeResult.IsSuccess, Is.True);
            Assert.That(openResult.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(openResult.Value.IsOpen, Is.False);
            Assert.That(openResult.Value.IsTransitioning, Is.False);
            Assert.That(openResult.Value.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void CloseView_DisablesCanvasGroupInput()
        {
            LifecycleScreen prefab = CreatePrefab<LifecycleScreen>("screen.main", true);
            ConfigureRegistry(prefab);

            Result<LifecycleScreen> openResult = controller.OpenView<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(openResult.IsSuccess, Is.True);

            CanvasGroup canvasGroup = openResult.Value.CanvasGroup;

            Result closeResult = controller.CloseView(openResult.Value);

            Assert.That(closeResult.IsSuccess, Is.True);
            Assert.That(canvasGroup.interactable, Is.False);
            Assert.That(canvasGroup.blocksRaycasts, Is.False);
        }

        [Test]
        public void CloseView_CallsClosingBeforeDeactivationAndClosedAfterDeactivation()
        {
            LifecycleScreen prefab = CreatePrefab<LifecycleScreen>("screen.main", true);
            ConfigureRegistry(prefab);

            Result<LifecycleScreen> openResult = controller.OpenView<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(openResult.IsSuccess, Is.True);

            LifecycleScreen view = openResult.Value;

            Result closeResult = controller.CloseView(view);

            Assert.That(closeResult.IsSuccess, Is.True);

            Assert.That(view.ClosingCount, Is.EqualTo(1));
            Assert.That(view.ClosedCount, Is.EqualTo(1));

            Assert.That(view.WasActiveDuringClosing, Is.True);
            Assert.That(view.DisableCountDuringClosing, Is.Zero);

            Assert.That(view.WasActiveDuringClosed, Is.False);
            Assert.That(view.DisableCountDuringClosed, Is.EqualTo(1));
        }

        [Test]
        public void OpenView_AlreadyOpen_ReturnsAlreadyOpen()
        {
            LifecycleScreen prefab = CreatePrefab<LifecycleScreen>("screen.main", true);
            ConfigureRegistry(prefab);

            Result<LifecycleScreen> firstResult = controller.OpenView<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(firstResult.IsSuccess, Is.True);

            Result<LifecycleScreen> secondResult = controller.OpenView<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(secondResult.IsFailure, Is.True);
            Assert.That(secondResult.Error.Code, Is.EqualTo(UIErrorCodes.AlreadyOpen));
        }

        [Test]
        public void CloseView_AlreadyClosed_ReturnsAlreadyClosed()
        {
            LifecycleScreen prefab = CreatePrefab<LifecycleScreen>("screen.main", true);
            ConfigureRegistry(prefab);

            Result<LifecycleScreen> instanceResult = controller.GetOrCreate<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(instanceResult.IsSuccess, Is.True);

            Result result = controller.CloseView(instanceResult.Value);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.AlreadyClosed));
        }

        [Test]
        public void OpenView_Opening_ReturnsBusy()
        {
            LifecycleScreen prefab = CreatePrefab<LifecycleScreen>("screen.main", true);
            ConfigureRegistry(prefab);

            Result<LifecycleScreen> instanceResult = controller.GetOrCreate<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(instanceResult.IsSuccess, Is.True);

            instanceResult.Value.BeginOpening();

            Result<LifecycleScreen> result = controller.OpenView<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.Busy));
        }

        [Test]
        public void OpenView_Closing_ReturnsBusy()
        {
            LifecycleScreen prefab = CreatePrefab<LifecycleScreen>("screen.main", true);
            ConfigureRegistry(prefab);

            Result<LifecycleScreen> openResult = controller.OpenView<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(openResult.IsSuccess, Is.True);

            openResult.Value.BeginClosing();

            Result<LifecycleScreen> result = controller.OpenView<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.Busy));
        }

        [Test]
        public void CloseView_Opening_ReturnsBusy()
        {
            LifecycleScreen prefab = CreatePrefab<LifecycleScreen>("screen.main", true);
            ConfigureRegistry(prefab);

            Result<LifecycleScreen> instanceResult = controller.GetOrCreate<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(instanceResult.IsSuccess, Is.True);

            instanceResult.Value.BeginOpening();

            Result result = controller.CloseView(instanceResult.Value);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.Busy));
        }

        [Test]
        public void CloseView_Closing_ReturnsBusy()
        {
            LifecycleScreen prefab = CreatePrefab<LifecycleScreen>("screen.main", true);
            ConfigureRegistry(prefab);

            Result<LifecycleScreen> openResult = controller.OpenView<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(openResult.IsSuccess, Is.True);

            openResult.Value.BeginClosing();

            Result result = controller.CloseView(openResult.Value);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.Busy));
        }

        [Test]
        public void OpenView_MissingCanvasGroup_ReturnsMissingCanvasGroupWithoutLifecycle()
        {
            LifecycleScreen prefab = CreatePrefab<LifecycleScreen>("screen.main", false);
            ConfigureRegistry(prefab);

            Result<LifecycleScreen> result = controller.OpenView<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.MissingCanvasGroup));

            Result<LifecycleScreen> instanceResult = controller.GetOrCreate<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(instanceResult.IsSuccess, Is.True);
            Assert.That(instanceResult.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(instanceResult.Value.gameObject.activeSelf, Is.False);
            Assert.That(instanceResult.Value.OpeningCount, Is.Zero);
            Assert.That(instanceResult.Value.OpenedCount, Is.Zero);
        }

        [Test]
        public void CloseView_MissingCanvasGroup_ReturnsMissingCanvasGroupWithoutClosing()
        {
            LifecycleScreen prefab = CreatePrefab<LifecycleScreen>("screen.main", true);
            ConfigureRegistry(prefab);

            Result<LifecycleScreen> openResult = controller.OpenView<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(openResult.IsSuccess, Is.True);

            CanvasGroup canvasGroup = openResult.Value.CanvasGroup;
            UnityEngine.Object.DestroyImmediate(canvasGroup);

            Result result = controller.CloseView(openResult.Value);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.MissingCanvasGroup));

            Assert.That(openResult.Value.State, Is.EqualTo(UIViewState.Open));
            Assert.That(openResult.Value.ClosingCount, Is.Zero);
            Assert.That(openResult.Value.ClosedCount, Is.Zero);
            Assert.That(openResult.Value.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void OpenView_AlreadyOpen_PrecedesMissingCanvasGroup()
        {
            LifecycleScreen prefab = CreatePrefab<LifecycleScreen>("screen.main", true);
            ConfigureRegistry(prefab);

            Result<LifecycleScreen> firstResult = controller.OpenView<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(firstResult.IsSuccess, Is.True);

            CanvasGroup canvasGroup = firstResult.Value.CanvasGroup;
            UnityEngine.Object.DestroyImmediate(canvasGroup);

            Result<LifecycleScreen> secondResult = controller.OpenView<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(secondResult.IsFailure, Is.True);
            Assert.That(secondResult.Error.Code, Is.EqualTo(UIErrorCodes.AlreadyOpen));
        }

        [Test]
        public void CloseView_AlreadyClosed_PrecedesMissingCanvasGroup()
        {
            LifecycleScreen prefab = CreatePrefab<LifecycleScreen>("screen.main", false);
            ConfigureRegistry(prefab);

            Result<LifecycleScreen> instanceResult = controller.GetOrCreate<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(instanceResult.IsSuccess, Is.True);

            Result result = controller.CloseView(instanceResult.Value);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.AlreadyClosed));
        }

        [Test]
        public void CloseView_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                controller.CloseView(null));
        }

        [Test]
        public void CloseThenOpen_ReusesSameCachedInstance()
        {
            LifecycleScreen prefab = CreatePrefab<LifecycleScreen>("screen.main", true);
            ConfigureRegistry(prefab);

            Result<LifecycleScreen> firstOpenResult = controller.OpenView<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(firstOpenResult.IsSuccess, Is.True);

            LifecycleScreen firstInstance = firstOpenResult.Value;

            Result closeResult = controller.CloseView(firstInstance);

            Assert.That(closeResult.IsSuccess, Is.True);

            Result<LifecycleScreen> secondOpenResult = controller.OpenView<LifecycleScreen>(
                new UIId("screen.main"));

            Assert.That(secondOpenResult.IsSuccess, Is.True);
            Assert.That(secondOpenResult.Value, Is.SameAs(firstInstance));
            Assert.That(secondOpenResult.Value.State, Is.EqualTo(UIViewState.Open));
        }

        [Test]
        public void IsOpen_TracksActualLifecycle()
        {
            LifecycleScreen prefab = CreatePrefab<LifecycleScreen>("screen.main", true);
            ConfigureRegistry(prefab);

            UIId id = new UIId("screen.main");

            Assert.That(controller.IsOpen(id), Is.False);

            Result<LifecycleScreen> openResult = controller.OpenView<LifecycleScreen>(id);

            Assert.That(openResult.IsSuccess, Is.True);
            Assert.That(controller.IsOpen(id), Is.True);

            Result closeResult = controller.CloseView(openResult.Value);

            Assert.That(closeResult.IsSuccess, Is.True);
            Assert.That(controller.IsOpen(id), Is.False);
        }

        private void ConfigureRegistry(params UIView[] prefabs)
        {
            Result result = registry.ReplacePrefabs(prefabs);

            Assert.That(result.IsSuccess, Is.True);
        }

        private T CreatePrefab<T>(string id, bool includeCanvasGroup) where T : UIView
        {
            GameObject gameObject = CreateGameObject($"{typeof(T).Name} Prefab");

            if (includeCanvasGroup)
            {
                gameObject.AddComponent<CanvasGroup>();
            }

            gameObject.SetActive(false);

            T view = gameObject.AddComponent<T>();
            view.SetId(new UIId(id));

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
            createdGameObjects.Add(layerObject);

            layerObject.transform.SetParent(parent, false);

            return layerObject.transform;
        }

        private sealed class LifecycleScreen : UIScreen
        {
            public int OpeningCount { get; private set; }

            public int OpenedCount { get; private set; }

            public int ClosingCount { get; private set; }

            public int ClosedCount { get; private set; }

            public int EnableCount { get; private set; }

            public int DisableCount { get; private set; }

            public bool WasActiveDuringOpening { get; private set; }

            public bool WasActiveDuringOpened { get; private set; }

            public bool WasActiveDuringClosing { get; private set; }

            public bool WasActiveDuringClosed { get; private set; }

            public int EnableCountDuringOpening { get; private set; }

            public int EnableCountDuringOpened { get; private set; }

            public int DisableCountDuringClosing { get; private set; }

            public int DisableCountDuringClosed { get; private set; }

            private void OnEnable()
            {
                EnableCount++;
            }

            private void OnDisable()
            {
                DisableCount++;
            }

            protected override void OnOpening()
            {
                OpeningCount++;
                WasActiveDuringOpening = gameObject.activeSelf;
                EnableCountDuringOpening = EnableCount;
            }

            protected override void OnOpened()
            {
                OpenedCount++;
                WasActiveDuringOpened = gameObject.activeSelf;
                EnableCountDuringOpened = EnableCount;
            }

            protected override void OnClosing()
            {
                ClosingCount++;
                WasActiveDuringClosing = gameObject.activeSelf;
                DisableCountDuringClosing = DisableCount;
            }

            protected override void OnClosed()
            {
                ClosedCount++;
                WasActiveDuringClosed = gameObject.activeSelf;
                DisableCountDuringClosed = DisableCount;
            }
        }
    }
}