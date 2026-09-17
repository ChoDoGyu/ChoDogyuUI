using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UIScreenRuntimeRecoveryTests
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
        public void OpenScreen_DestroyedCurrentSameId_RecreatesInstance()
        {
            UIScreen screen = CreateScreenPrefab(
                "screen.main");

            ConfigureRegistry(screen);

            Result<UIScreen> firstResult = controller.OpenScreen(
                new UIId("screen.main"));

            Assert.That(firstResult.IsSuccess, Is.True);

            UIScreen firstInstance = firstResult.Value;

            Object.DestroyImmediate(
                firstInstance.gameObject);

            Assert.That(
                controller.CurrentScreen == null,
                Is.True);

            Result<UIScreen> secondResult = controller.OpenScreen(
                new UIId("screen.main"));

            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(secondResult.Value, Is.Not.SameAs(firstInstance));
            Assert.That(secondResult.Value.State, Is.EqualTo(UIViewState.Open));
            Assert.That(secondResult.Value.gameObject.activeSelf, Is.True);
            Assert.That(controller.CurrentScreen, Is.SameAs(secondResult.Value));
            Assert.That(controller.ScreenHistoryCount, Is.Zero);
        }

        [Test]
        public void Back_DestroyedCurrentScreen_RestoresPreviousScreen()
        {
            UIScreen firstScreen = CreateScreenPrefab(
                "screen.first");

            UIScreen secondScreen = CreateScreenPrefab(
                "screen.second");

            ConfigureRegistry(
                firstScreen,
                secondScreen);

            Result<UIScreen> firstResult = controller.OpenScreen(
                new UIId("screen.first"));

            Assert.That(firstResult.IsSuccess, Is.True);

            Result<UIScreen> secondResult = controller.OpenScreen(
                new UIId("screen.second"));

            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));

            Object.DestroyImmediate(
                secondResult.Value.gameObject);

            Assert.That(
                controller.CurrentScreen == null,
                Is.True);

            Assert.That(controller.CanBack, Is.True);

            Result backResult = controller.Back();

            Assert.That(backResult.IsSuccess, Is.True);
            Assert.That(controller.CurrentScreen, Is.SameAs(firstResult.Value));
            Assert.That(firstResult.Value.State, Is.EqualTo(UIViewState.Open));
            Assert.That(firstResult.Value.gameObject.activeSelf, Is.True);
            Assert.That(controller.ScreenHistoryCount, Is.Zero);
        }

        [Test]
        public void Back_DestroyedHistoryScreen_RecreatesPreviousScreen()
        {
            UIScreen firstScreen = CreateScreenPrefab(
                "screen.first");

            UIScreen secondScreen = CreateScreenPrefab(
                "screen.second");

            ConfigureRegistry(
                firstScreen,
                secondScreen);

            Result<UIScreen> firstResult = controller.OpenScreen(
                new UIId("screen.first"));

            Assert.That(firstResult.IsSuccess, Is.True);

            UIScreen originalFirstInstance = firstResult.Value;

            Result<UIScreen> secondResult = controller.OpenScreen(
                new UIId("screen.second"));

            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(originalFirstInstance.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));

            Object.DestroyImmediate(
                originalFirstInstance.gameObject);

            Result backResult = controller.Back();

            Assert.That(backResult.IsSuccess, Is.True);
            Assert.That(controller.CurrentScreen, Is.Not.Null);
            Assert.That(controller.CurrentScreen, Is.Not.SameAs(originalFirstInstance));
            Assert.That(controller.CurrentScreen.Id, Is.EqualTo(new UIId("screen.first")));
            Assert.That(controller.CurrentScreen.State, Is.EqualTo(UIViewState.Open));
            Assert.That(controller.CurrentScreen.gameObject.activeSelf, Is.True);
            Assert.That(secondResult.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(controller.ScreenHistoryCount, Is.Zero);
        }

        [Test]
        public void OpenScreen_DestroyedCurrentScreen_DoesNotAddDestroyedScreenToHistory()
        {
            UIScreen firstScreen = CreateScreenPrefab(
                "screen.first");

            UIScreen secondScreen = CreateScreenPrefab(
                "screen.second");

            ConfigureRegistry(
                firstScreen,
                secondScreen);

            Result<UIScreen> firstResult = controller.OpenScreen(
                new UIId("screen.first"));

            Assert.That(firstResult.IsSuccess, Is.True);

            Object.DestroyImmediate(
                firstResult.Value.gameObject);

            Assert.That(
                controller.CurrentScreen == null,
                Is.True);

            Result<UIScreen> secondResult = controller.OpenScreen(
                new UIId("screen.second"));

            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(controller.CurrentScreen, Is.SameAs(secondResult.Value));
            Assert.That(secondResult.Value.State, Is.EqualTo(UIViewState.Open));
            Assert.That(controller.ScreenHistoryCount, Is.Zero);
            Assert.That(controller.CanBack, Is.False);
        }

        private void ConfigureRegistry(params UIView[] prefabs)
        {
            Result result = registry.ReplacePrefabs(prefabs);

            Assert.That(result.IsSuccess, Is.True);
        }

        private UIScreen CreateScreenPrefab(string id)
        {
            GameObject gameObject = CreateViewGameObject(
                "UIScreen Prefab");

            UIScreen screen = gameObject.AddComponent<UIScreen>();
            screen.SetId(new UIId(id));

            gameObject.SetActive(false);

            return screen;
        }

        private GameObject CreateViewGameObject(string name)
        {
            GameObject gameObject = CreateGameObject(name);
            gameObject.AddComponent<CanvasGroup>();

            return gameObject;
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            createdGameObjects.Add(gameObject);

            return gameObject;
        }

        private Transform CreateLayer(string name, Transform parent)
        {
            GameObject layerObject = new GameObject(
                name,
                typeof(RectTransform));

            createdGameObjects.Add(layerObject);

            layerObject.transform.SetParent(
                parent,
                false);

            return layerObject.transform;
        }
    }
}