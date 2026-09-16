using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UIPopupStackTests
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
        public void NewController_HasNoTopPopupOrPopupCount()
        {
            Assert.That(controller.TopPopup, Is.Null);
            Assert.That(controller.PopupCount, Is.Zero);
        }

        [Test]
        public void OpenPopup_FirstPopup_OpensAndBecomesTop()
        {
            UIPopup popup = CreatePrefab<UIPopup>("popup.first", true);
            ConfigureRegistry(popup);

            Result<UIPopup> result = controller.OpenPopup(
                new UIId("popup.first"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.State, Is.EqualTo(UIViewState.Open));
            Assert.That(result.Value.gameObject.activeSelf, Is.True);

            Assert.That(controller.TopPopup, Is.SameAs(result.Value));
            Assert.That(controller.PopupCount, Is.EqualTo(1));
        }

        [Test]
        public void OpenPopup_SecondPopup_BecomesTop()
        {
            UIPopup first = CreatePrefab<UIPopup>("popup.first", true);
            UIPopup second = CreatePrefab<UIPopup>("popup.second", true);
            ConfigureRegistry(first, second);

            Result<UIPopup> firstResult = controller.OpenPopup(
                new UIId("popup.first"));

            Result<UIPopup> secondResult = controller.OpenPopup(
                new UIId("popup.second"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);

            Assert.That(controller.TopPopup, Is.SameAs(secondResult.Value));
            Assert.That(controller.PopupCount, Is.EqualTo(2));
            Assert.That(firstResult.Value.IsOpen, Is.True);
            Assert.That(secondResult.Value.IsOpen, Is.True);
        }

        [Test]
        public void OpenPopup_PlacesInstanceUnderPopupLayer()
        {
            UIPopup popup = CreatePrefab<UIPopup>("popup.first", true);
            ConfigureRegistry(popup);

            Result<UIPopup> result = controller.OpenPopup(
                new UIId("popup.first"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.transform.parent, Is.SameAs(popupLayer));
        }

        [Test]
        public void OpenPopup_NewTopPopup_IsLastSibling()
        {
            UIPopup first = CreatePrefab<UIPopup>("popup.first", true);
            UIPopup second = CreatePrefab<UIPopup>("popup.second", true);
            ConfigureRegistry(first, second);

            Result<UIPopup> firstResult = controller.OpenPopup(
                new UIId("popup.first"));

            Result<UIPopup> secondResult = controller.OpenPopup(
                new UIId("popup.second"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);

            Assert.That(
                secondResult.Value.transform.GetSiblingIndex(),
                Is.EqualTo(popupLayer.childCount - 1));
        }

        [Test]
        public void OpenPopup_SamePopup_ReturnsAlreadyOpenWithoutChangingStack()
        {
            UIPopup popup = CreatePrefab<UIPopup>("popup.first", true);
            ConfigureRegistry(popup);

            Result<UIPopup> firstResult = controller.OpenPopup(
                new UIId("popup.first"));

            Assert.That(firstResult.IsSuccess, Is.True);

            UIPopup topBefore = controller.TopPopup;
            int countBefore = controller.PopupCount;

            Result<UIPopup> secondResult = controller.OpenPopup(
                new UIId("popup.first"));

            Assert.That(secondResult.IsFailure, Is.True);
            Assert.That(secondResult.Error.Code, Is.EqualTo(UIErrorCodes.AlreadyOpen));

            Assert.That(controller.TopPopup, Is.SameAs(topBefore));
            Assert.That(controller.PopupCount, Is.EqualTo(countBefore));
        }

        [Test]
        public void OpenPopup_MissingPopup_ReturnsNotFoundWithoutChangingStack()
        {
            UIPopup popup = CreatePrefab<UIPopup>("popup.first", true);
            ConfigureRegistry(popup);

            Result<UIPopup> result = controller.OpenPopup(
                new UIId("popup.missing"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.NotFound));

            Assert.That(controller.TopPopup, Is.Null);
            Assert.That(controller.PopupCount, Is.Zero);
        }

        [Test]
        public void OpenPopup_NonPopup_ReturnsInvalidTypeWithoutChangingStack()
        {
            UIScreen screen = CreatePrefab<UIScreen>("screen.main", true);
            ConfigureRegistry(screen);

            Result<UIPopup> result = controller.OpenPopup(
                new UIId("screen.main"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.InvalidType));

            Assert.That(controller.TopPopup, Is.Null);
            Assert.That(controller.PopupCount, Is.Zero);
        }

        [Test]
        public void OpenPopup_MissingCanvasGroup_ReturnsErrorWithoutChangingStack()
        {
            UIPopup popup = CreatePrefab<UIPopup>("popup.broken", false);
            ConfigureRegistry(popup);

            Result<UIPopup> result = controller.OpenPopup(
                new UIId("popup.broken"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.MissingCanvasGroup));

            Assert.That(controller.TopPopup, Is.Null);
            Assert.That(controller.PopupCount, Is.Zero);
        }

        [Test]
        public void OpenPopup_MissingPopupLayer_ReturnsErrorWithoutChangingStack()
        {
            UIPopup popup = CreatePrefab<UIPopup>("popup.first", true);
            ConfigureRegistry(popup);

            controller.SetLayers(
                screenLayer,
                overlayLayer,
                null,
                topOverlayLayer);

            Result<UIPopup> result = controller.OpenPopup(
                new UIId("popup.first"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.MissingLayer));

            Assert.That(controller.TopPopup, Is.Null);
            Assert.That(controller.PopupCount, Is.Zero);
        }

        [Test]
        public void OpenPopup_BusyPopup_ReturnsBusyWithoutChangingStack()
        {
            UIPopup popup = CreatePrefab<UIPopup>("popup.first", true);
            ConfigureRegistry(popup);

            Result<UIPopup> instanceResult = controller.GetOrCreate<UIPopup>(
                new UIId("popup.first"));

            Assert.That(instanceResult.IsSuccess, Is.True);

            instanceResult.Value.BeginOpening();

            Result<UIPopup> result = controller.OpenPopup(
                new UIId("popup.first"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.Busy));

            Assert.That(controller.TopPopup, Is.Null);
            Assert.That(controller.PopupCount, Is.Zero);
        }

        [Test]
        public void Close_TopPopup_RemovesItAndRevealsPreviousPopup()
        {
            UIPopup first = CreatePrefab<UIPopup>("popup.first", true);
            UIPopup second = CreatePrefab<UIPopup>("popup.second", true);
            ConfigureRegistry(first, second);

            Result<UIPopup> firstResult = controller.OpenPopup(
                new UIId("popup.first"));

            Result<UIPopup> secondResult = controller.OpenPopup(
                new UIId("popup.second"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);

            Result closeResult = controller.Close(
                new UIId("popup.second"));

            Assert.That(closeResult.IsSuccess, Is.True);
            Assert.That(secondResult.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(secondResult.Value.gameObject.activeSelf, Is.False);

            Assert.That(controller.TopPopup, Is.SameAs(firstResult.Value));
            Assert.That(controller.PopupCount, Is.EqualTo(1));
        }

        [Test]
        public void Close_MiddlePopup_RemovesOnlySpecifiedPopup()
        {
            UIPopup first = CreatePrefab<UIPopup>("popup.first", true);
            UIPopup second = CreatePrefab<UIPopup>("popup.second", true);
            UIPopup third = CreatePrefab<UIPopup>("popup.third", true);
            ConfigureRegistry(first, second, third);

            Result<UIPopup> firstResult = controller.OpenPopup(
                new UIId("popup.first"));

            Result<UIPopup> secondResult = controller.OpenPopup(
                new UIId("popup.second"));

            Result<UIPopup> thirdResult = controller.OpenPopup(
                new UIId("popup.third"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(thirdResult.IsSuccess, Is.True);

            Result closeResult = controller.Close(
                new UIId("popup.second"));

            Assert.That(closeResult.IsSuccess, Is.True);

            Assert.That(secondResult.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(firstResult.Value.State, Is.EqualTo(UIViewState.Open));
            Assert.That(thirdResult.Value.State, Is.EqualTo(UIViewState.Open));

            Assert.That(controller.TopPopup, Is.SameAs(thirdResult.Value));
            Assert.That(controller.PopupCount, Is.EqualTo(2));
        }

        [Test]
        public void Close_BottomPopup_RemovesItWithoutChangingTopPopup()
        {
            UIPopup first = CreatePrefab<UIPopup>("popup.first", true);
            UIPopup second = CreatePrefab<UIPopup>("popup.second", true);
            UIPopup third = CreatePrefab<UIPopup>("popup.third", true);
            ConfigureRegistry(first, second, third);

            Result<UIPopup> firstResult = controller.OpenPopup(
                new UIId("popup.first"));

            Result<UIPopup> secondResult = controller.OpenPopup(
                new UIId("popup.second"));

            Result<UIPopup> thirdResult = controller.OpenPopup(
                new UIId("popup.third"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(thirdResult.IsSuccess, Is.True);

            Result closeResult = controller.Close(
                new UIId("popup.first"));

            Assert.That(closeResult.IsSuccess, Is.True);

            Assert.That(controller.TopPopup, Is.SameAs(thirdResult.Value));
            Assert.That(controller.PopupCount, Is.EqualTo(2));
            Assert.That(secondResult.Value.IsOpen, Is.True);
            Assert.That(thirdResult.Value.IsOpen, Is.True);
        }

        [Test]
        public void Close_LastPopup_LeavesEmptyPopupStack()
        {
            UIPopup popup = CreatePrefab<UIPopup>("popup.first", true);
            ConfigureRegistry(popup);

            Result<UIPopup> openResult = controller.OpenPopup(
                new UIId("popup.first"));

            Assert.That(openResult.IsSuccess, Is.True);

            Result closeResult = controller.Close(
                new UIId("popup.first"));

            Assert.That(closeResult.IsSuccess, Is.True);
            Assert.That(controller.TopPopup, Is.Null);
            Assert.That(controller.PopupCount, Is.Zero);
        }

        [Test]
        public void Close_PopupMissingCanvasGroup_PreservesPopupStack()
        {
            UIPopup popup = CreatePrefab<UIPopup>("popup.first", true);
            ConfigureRegistry(popup);

            Result<UIPopup> openResult = controller.OpenPopup(
                new UIId("popup.first"));

            Assert.That(openResult.IsSuccess, Is.True);

            CanvasGroup canvasGroup = openResult.Value.CanvasGroup;
            Object.DestroyImmediate(canvasGroup);

            Result closeResult = controller.Close(
                new UIId("popup.first"));

            Assert.That(closeResult.IsFailure, Is.True);
            Assert.That(closeResult.Error.Code, Is.EqualTo(UIErrorCodes.MissingCanvasGroup));

            Assert.That(openResult.Value.State, Is.EqualTo(UIViewState.Open));
            Assert.That(controller.TopPopup, Is.SameAs(openResult.Value));
            Assert.That(controller.PopupCount, Is.EqualTo(1));
        }

        [Test]
        public void OpenPopup_AfterClose_ReusesCachedInstanceAndBecomesTop()
        {
            UIPopup first = CreatePrefab<UIPopup>("popup.first", true);
            UIPopup second = CreatePrefab<UIPopup>("popup.second", true);
            UIPopup third = CreatePrefab<UIPopup>("popup.third", true);
            ConfigureRegistry(first, second, third);

            Result<UIPopup> firstResult = controller.OpenPopup(
                new UIId("popup.first"));

            Result<UIPopup> secondResult = controller.OpenPopup(
                new UIId("popup.second"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);

            UIPopup cachedSecond = secondResult.Value;

            Result closeResult = controller.Close(
                new UIId("popup.second"));

            Assert.That(closeResult.IsSuccess, Is.True);

            Result<UIPopup> thirdResult = controller.OpenPopup(
                new UIId("popup.third"));

            Assert.That(thirdResult.IsSuccess, Is.True);

            Result<UIPopup> reopenedResult = controller.OpenPopup(
                new UIId("popup.second"));

            Assert.That(reopenedResult.IsSuccess, Is.True);
            Assert.That(reopenedResult.Value, Is.SameAs(cachedSecond));

            Assert.That(controller.TopPopup, Is.SameAs(cachedSecond));
            Assert.That(controller.PopupCount, Is.EqualTo(3));

            Assert.That(
                cachedSecond.transform.GetSiblingIndex(),
                Is.EqualTo(popupLayer.childCount - 1));
        }

        [Test]
        public void TopPopup_DestroyedTopPopup_CleansStackAndReturnsPreviousPopup()
        {
            UIPopup first = CreatePrefab<UIPopup>("popup.first", true);
            UIPopup second = CreatePrefab<UIPopup>("popup.second", true);
            ConfigureRegistry(first, second);

            Result<UIPopup> firstResult = controller.OpenPopup(
                new UIId("popup.first"));

            Result<UIPopup> secondResult = controller.OpenPopup(
                new UIId("popup.second"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);

            Object.DestroyImmediate(secondResult.Value.gameObject);

            Assert.That(controller.TopPopup, Is.SameAs(firstResult.Value));
            Assert.That(controller.PopupCount, Is.EqualTo(1));
        }

        [Test]
        public void PopupCount_DestroyedMiddlePopup_CleansStackAndPreservesTop()
        {
            UIPopup first = CreatePrefab<UIPopup>("popup.first", true);
            UIPopup second = CreatePrefab<UIPopup>("popup.second", true);
            UIPopup third = CreatePrefab<UIPopup>("popup.third", true);
            ConfigureRegistry(first, second, third);

            Result<UIPopup> firstResult = controller.OpenPopup(
                new UIId("popup.first"));

            Result<UIPopup> secondResult = controller.OpenPopup(
                new UIId("popup.second"));

            Result<UIPopup> thirdResult = controller.OpenPopup(
                new UIId("popup.third"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(thirdResult.IsSuccess, Is.True);

            Object.DestroyImmediate(secondResult.Value.gameObject);

            Assert.That(controller.PopupCount, Is.EqualTo(2));
            Assert.That(controller.TopPopup, Is.SameAs(thirdResult.Value));
        }

        [Test]
        public void TopPopup_ClosedPopupRemainingInStack_IsCleanedAutomatically()
        {
            UIPopup first = CreatePrefab<UIPopup>("popup.first", true);
            UIPopup second = CreatePrefab<UIPopup>("popup.second", true);
            ConfigureRegistry(first, second);

            Result<UIPopup> firstResult = controller.OpenPopup(
                new UIId("popup.first"));

            Result<UIPopup> secondResult = controller.OpenPopup(
                new UIId("popup.second"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);

            Result closeResult = controller.CloseView(secondResult.Value);

            Assert.That(closeResult.IsSuccess, Is.True);

            Assert.That(controller.TopPopup, Is.SameAs(firstResult.Value));
            Assert.That(controller.PopupCount, Is.EqualTo(1));
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