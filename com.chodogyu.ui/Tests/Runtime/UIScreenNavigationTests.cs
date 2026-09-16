using System;
using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UIScreenNavigationTests
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
        public void NewController_HasNoCurrentScreenOrHistory()
        {
            Assert.That(controller.CurrentScreen, Is.Null);
            Assert.That(controller.ScreenHistoryCount, Is.Zero);
        }

        [Test]
        public void OpenScreen_Default_FirstScreen_OpensAsCurrentWithoutHistory()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            ConfigureRegistry(main);

            Result<UIScreen> result = controller.OpenScreen(
                new UIId("screen.main"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(controller.CurrentScreen, Is.SameAs(result.Value));
            Assert.That(controller.CurrentScreen.Id, Is.EqualTo(new UIId("screen.main")));
            Assert.That(controller.CurrentScreen.State, Is.EqualTo(UIViewState.Open));
            Assert.That(controller.ScreenHistoryCount, Is.Zero);
        }

        [Test]
        public void OpenScreen_Default_SecondScreen_PushesPreviousToHistory()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            UIScreen inventory = CreatePrefab<UIScreen>("screen.inventory", true);
            ConfigureRegistry(main, inventory);

            Result<UIScreen> mainResult = controller.OpenScreen(
                new UIId("screen.main"));

            Assert.That(mainResult.IsSuccess, Is.True);

            Result<UIScreen> inventoryResult = controller.OpenScreen(
                new UIId("screen.inventory"));

            Assert.That(inventoryResult.IsSuccess, Is.True);
            Assert.That(controller.CurrentScreen, Is.SameAs(inventoryResult.Value));
            Assert.That(controller.CurrentScreen.Id, Is.EqualTo(new UIId("screen.inventory")));
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));
        }

        [Test]
        public void OpenScreen_Push_ClosesPreviousScreen()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            UIScreen inventory = CreatePrefab<UIScreen>("screen.inventory", true);
            ConfigureRegistry(main, inventory);

            Result<UIScreen> mainResult = controller.OpenScreen(
                new UIId("screen.main"),
                UIScreenOpenMode.Push);

            Assert.That(mainResult.IsSuccess, Is.True);

            UIScreen mainInstance = mainResult.Value;

            Result<UIScreen> inventoryResult = controller.OpenScreen(
                new UIId("screen.inventory"),
                UIScreenOpenMode.Push);

            Assert.That(inventoryResult.IsSuccess, Is.True);
            Assert.That(mainInstance.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(mainInstance.gameObject.activeSelf, Is.False);
            Assert.That(inventoryResult.Value.State, Is.EqualTo(UIViewState.Open));
        }

        [Test]
        public void OpenScreen_Push_MultipleScreens_IncreasesHistoryCount()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            UIScreen inventory = CreatePrefab<UIScreen>("screen.inventory", true);
            UIScreen settings = CreatePrefab<UIScreen>("screen.settings", true);
            ConfigureRegistry(main, inventory, settings);

            Assert.That(
                controller.OpenScreen(new UIId("screen.main")).IsSuccess,
                Is.True);

            Assert.That(
                controller.OpenScreen(new UIId("screen.inventory")).IsSuccess,
                Is.True);

            Assert.That(
                controller.OpenScreen(new UIId("screen.settings")).IsSuccess,
                Is.True);

            Assert.That(controller.CurrentScreen.Id, Is.EqualTo(new UIId("screen.settings")));
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(2));
        }

        [Test]
        public void OpenScreen_Push_SameCurrentScreen_ReturnsAlreadyOpenWithoutChangingNavigation()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            ConfigureRegistry(main);

            Result<UIScreen> firstResult = controller.OpenScreen(
                new UIId("screen.main"));

            Assert.That(firstResult.IsSuccess, Is.True);

            UIScreen currentBefore = controller.CurrentScreen;
            int historyBefore = controller.ScreenHistoryCount;

            Result<UIScreen> secondResult = controller.OpenScreen(
                new UIId("screen.main"));

            Assert.That(secondResult.IsFailure, Is.True);
            Assert.That(secondResult.Error.Code, Is.EqualTo(UIErrorCodes.AlreadyOpen));
            Assert.That(controller.CurrentScreen, Is.SameAs(currentBefore));
            Assert.That(controller.CurrentScreen.IsOpen, Is.True);
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(historyBefore));
        }

        [Test]
        public void OpenScreen_Push_MissingScreen_ReturnsNotFoundWithoutChangingNavigation()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            ConfigureRegistry(main);

            Result<UIScreen> mainResult = controller.OpenScreen(
                new UIId("screen.main"));

            Assert.That(mainResult.IsSuccess, Is.True);

            UIScreen currentBefore = controller.CurrentScreen;
            int historyBefore = controller.ScreenHistoryCount;

            Result<UIScreen> result = controller.OpenScreen(
                new UIId("screen.missing"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.NotFound));
            Assert.That(controller.CurrentScreen, Is.SameAs(currentBefore));
            Assert.That(controller.CurrentScreen.IsOpen, Is.True);
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(historyBefore));
        }

        [Test]
        public void OpenScreen_Push_NonScreen_ReturnsInvalidTypeWithoutChangingNavigation()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            UIPopup popup = CreatePrefab<UIPopup>("popup.settings", true);
            ConfigureRegistry(main, popup);

            Result<UIScreen> mainResult = controller.OpenScreen(
                new UIId("screen.main"));

            Assert.That(mainResult.IsSuccess, Is.True);

            UIScreen currentBefore = controller.CurrentScreen;

            Result<UIScreen> result = controller.OpenScreen(
                new UIId("popup.settings"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.InvalidType));
            Assert.That(controller.CurrentScreen, Is.SameAs(currentBefore));
            Assert.That(controller.CurrentScreen.IsOpen, Is.True);
            Assert.That(controller.ScreenHistoryCount, Is.Zero);
        }

        [Test]
        public void OpenScreen_Push_MissingCanvasGroup_ReturnsErrorWithoutChangingNavigation()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            UIScreen broken = CreatePrefab<UIScreen>("screen.broken", false);
            ConfigureRegistry(main, broken);

            Result<UIScreen> mainResult = controller.OpenScreen(
                new UIId("screen.main"));

            Assert.That(mainResult.IsSuccess, Is.True);

            UIScreen currentBefore = controller.CurrentScreen;

            Result<UIScreen> result = controller.OpenScreen(
                new UIId("screen.broken"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.MissingCanvasGroup));
            Assert.That(controller.CurrentScreen, Is.SameAs(currentBefore));
            Assert.That(controller.CurrentScreen.IsOpen, Is.True);
            Assert.That(controller.ScreenHistoryCount, Is.Zero);
        }

        [Test]
        public void OpenScreen_Push_CurrentScreenCannotClose_ReturnsErrorWithoutChangingHistoryOrTarget()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            UIScreen inventory = CreatePrefab<UIScreen>("screen.inventory", true);
            ConfigureRegistry(main, inventory);

            Result<UIScreen> mainResult = controller.OpenScreen(
                new UIId("screen.main"));

            Assert.That(mainResult.IsSuccess, Is.True);

            CanvasGroup mainCanvasGroup = mainResult.Value.CanvasGroup;
            UnityEngine.Object.DestroyImmediate(mainCanvasGroup);

            Result<UIScreen> result = controller.OpenScreen(
                new UIId("screen.inventory"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.MissingCanvasGroup));

            Assert.That(controller.CurrentScreen, Is.SameAs(mainResult.Value));
            Assert.That(controller.CurrentScreen.State, Is.EqualTo(UIViewState.Open));
            Assert.That(controller.ScreenHistoryCount, Is.Zero);

            Result<UIScreen> inventoryResult = controller.GetOrCreate<UIScreen>(
                new UIId("screen.inventory"));

            Assert.That(inventoryResult.IsSuccess, Is.True);
            Assert.That(inventoryResult.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(inventoryResult.Value.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void OpenScreen_Replace_FirstScreen_OpensWithoutHistory()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            ConfigureRegistry(main);

            Result<UIScreen> result = controller.OpenScreen(
                new UIId("screen.main"),
                UIScreenOpenMode.Replace);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(controller.CurrentScreen, Is.SameAs(result.Value));
            Assert.That(controller.CurrentScreen.Id, Is.EqualTo(new UIId("screen.main")));
            Assert.That(controller.ScreenHistoryCount, Is.Zero);
        }

        [Test]
        public void OpenScreen_Replace_DoesNotAddCurrentScreenToHistory()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            UIScreen inventory = CreatePrefab<UIScreen>("screen.inventory", true);
            ConfigureRegistry(main, inventory);

            Result<UIScreen> mainResult = controller.OpenScreen(
                new UIId("screen.main"));

            Assert.That(mainResult.IsSuccess, Is.True);

            Result<UIScreen> inventoryResult = controller.OpenScreen(
                new UIId("screen.inventory"),
                UIScreenOpenMode.Replace);

            Assert.That(inventoryResult.IsSuccess, Is.True);
            Assert.That(controller.CurrentScreen, Is.SameAs(inventoryResult.Value));
            Assert.That(controller.CurrentScreen.Id, Is.EqualTo(new UIId("screen.inventory")));
            Assert.That(controller.ScreenHistoryCount, Is.Zero);
        }

        [Test]
        public void OpenScreen_Replace_PreservesExistingHistoryCount()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            UIScreen inventory = CreatePrefab<UIScreen>("screen.inventory", true);
            UIScreen settings = CreatePrefab<UIScreen>("screen.settings", true);
            ConfigureRegistry(main, inventory, settings);

            Assert.That(
                controller.OpenScreen(new UIId("screen.main")).IsSuccess,
                Is.True);

            Assert.That(
                controller.OpenScreen(new UIId("screen.inventory")).IsSuccess,
                Is.True);

            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));

            Result<UIScreen> result = controller.OpenScreen(
                new UIId("screen.settings"),
                UIScreenOpenMode.Replace);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(controller.CurrentScreen.Id, Is.EqualTo(new UIId("screen.settings")));
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));
        }

        [Test]
        public void OpenScreen_Replace_ClosesPreviousScreen()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            UIScreen inventory = CreatePrefab<UIScreen>("screen.inventory", true);
            ConfigureRegistry(main, inventory);

            Result<UIScreen> mainResult = controller.OpenScreen(
                new UIId("screen.main"));

            Assert.That(mainResult.IsSuccess, Is.True);

            Result<UIScreen> inventoryResult = controller.OpenScreen(
                new UIId("screen.inventory"),
                UIScreenOpenMode.Replace);

            Assert.That(inventoryResult.IsSuccess, Is.True);
            Assert.That(mainResult.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(mainResult.Value.gameObject.activeSelf, Is.False);
            Assert.That(inventoryResult.Value.State, Is.EqualTo(UIViewState.Open));
        }

        [Test]
        public void OpenScreen_Reset_FirstScreen_OpensWithoutHistory()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            ConfigureRegistry(main);

            Result<UIScreen> result = controller.OpenScreen(
                new UIId("screen.main"),
                UIScreenOpenMode.Reset);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(controller.CurrentScreen, Is.SameAs(result.Value));
            Assert.That(controller.ScreenHistoryCount, Is.Zero);
        }

        [Test]
        public void OpenScreen_Reset_ClearsExistingHistory()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            UIScreen inventory = CreatePrefab<UIScreen>("screen.inventory", true);
            UIScreen settings = CreatePrefab<UIScreen>("screen.settings", true);
            UIScreen title = CreatePrefab<UIScreen>("screen.title", true);
            ConfigureRegistry(main, inventory, settings, title);

            Assert.That(
                controller.OpenScreen(new UIId("screen.main")).IsSuccess,
                Is.True);

            Assert.That(
                controller.OpenScreen(new UIId("screen.inventory")).IsSuccess,
                Is.True);

            Assert.That(
                controller.OpenScreen(new UIId("screen.settings")).IsSuccess,
                Is.True);

            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(2));

            Result<UIScreen> result = controller.OpenScreen(
                new UIId("screen.title"),
                UIScreenOpenMode.Reset);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(controller.CurrentScreen.Id, Is.EqualTo(new UIId("screen.title")));
            Assert.That(controller.ScreenHistoryCount, Is.Zero);
        }

        [Test]
        public void OpenScreen_Reset_FailedTarget_PreservesCurrentAndHistory()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            UIScreen inventory = CreatePrefab<UIScreen>("screen.inventory", true);
            UIScreen broken = CreatePrefab<UIScreen>("screen.broken", false);
            ConfigureRegistry(main, inventory, broken);

            Assert.That(
                controller.OpenScreen(new UIId("screen.main")).IsSuccess,
                Is.True);

            Result<UIScreen> inventoryResult = controller.OpenScreen(
                new UIId("screen.inventory"));

            Assert.That(inventoryResult.IsSuccess, Is.True);
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));

            UIScreen currentBefore = controller.CurrentScreen;

            Result<UIScreen> result = controller.OpenScreen(
                new UIId("screen.broken"),
                UIScreenOpenMode.Reset);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.MissingCanvasGroup));
            Assert.That(controller.CurrentScreen, Is.SameAs(currentBefore));
            Assert.That(controller.CurrentScreen.IsOpen, Is.True);
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));
        }

        [Test]
        public void OpenScreen_WithUnsupportedMode_ThrowsArgumentOutOfRangeException()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            ConfigureRegistry(main);

            UIScreenOpenMode invalidMode = (UIScreenOpenMode)999;

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                controller.OpenScreen(
                    new UIId("screen.main"),
                    invalidMode));

            Assert.That(controller.CurrentScreen, Is.Null);
            Assert.That(controller.ScreenHistoryCount, Is.Zero);
        }

        [Test]
        public void OpenScreen_ReopensCachedClosedScreenInstance()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            UIScreen inventory = CreatePrefab<UIScreen>("screen.inventory", true);
            ConfigureRegistry(main, inventory);

            Result<UIScreen> firstMainResult = controller.OpenScreen(
                new UIId("screen.main"));

            Assert.That(firstMainResult.IsSuccess, Is.True);

            UIScreen firstMainInstance = firstMainResult.Value;

            Result<UIScreen> inventoryResult = controller.OpenScreen(
                new UIId("screen.inventory"));

            Assert.That(inventoryResult.IsSuccess, Is.True);
            Assert.That(firstMainInstance.State, Is.EqualTo(UIViewState.Closed));

            Result<UIScreen> secondMainResult = controller.OpenScreen(
                new UIId("screen.main"),
                UIScreenOpenMode.Replace);

            Assert.That(secondMainResult.IsSuccess, Is.True);
            Assert.That(secondMainResult.Value, Is.SameAs(firstMainInstance));
            Assert.That(secondMainResult.Value.State, Is.EqualTo(UIViewState.Open));
            Assert.That(controller.CurrentScreen, Is.SameAs(firstMainInstance));
        }

        [Test]
        public void OpenScreen_TargetBusy_ReturnsBusyWithoutChangingCurrentScreen()
        {
            UIScreen main = CreatePrefab<UIScreen>("screen.main", true);
            UIScreen inventory = CreatePrefab<UIScreen>("screen.inventory", true);
            ConfigureRegistry(main, inventory);

            Result<UIScreen> mainResult = controller.OpenScreen(
                new UIId("screen.main"));

            Assert.That(mainResult.IsSuccess, Is.True);

            Result<UIScreen> inventoryResult = controller.GetOrCreate<UIScreen>(
                new UIId("screen.inventory"));

            Assert.That(inventoryResult.IsSuccess, Is.True);

            inventoryResult.Value.BeginOpening();

            Result<UIScreen> result = controller.OpenScreen(
                new UIId("screen.inventory"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.Busy));
            Assert.That(controller.CurrentScreen, Is.SameAs(mainResult.Value));
            Assert.That(controller.CurrentScreen.IsOpen, Is.True);
            Assert.That(controller.ScreenHistoryCount, Is.Zero);
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
    }
}