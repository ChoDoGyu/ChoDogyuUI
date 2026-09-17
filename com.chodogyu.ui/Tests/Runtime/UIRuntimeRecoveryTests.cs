using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UIRuntimeRecoveryTests
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
        public void PopupCount_DestroyedBlockingPopup_RestoresScreenInput()
        {
            UIScreen screen = CreateScreenPrefab("screen.main");
            UIPopup popup = CreatePopupPrefab("popup.blocking", true);

            ConfigureRegistry(
                screen,
                popup);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.main"));

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.blocking"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(popupResult.IsSuccess, Is.True);

            AssertInputEnabled(screenResult.Value, false);

            Object.DestroyImmediate(
                popupResult.Value.gameObject);

            Assert.That(controller.PopupCount, Is.Zero);
            AssertInputEnabled(screenResult.Value, true);
        }

        [Test]
        public void TopPopup_DestroyedBlockingTopPopup_RestoresPreviousPopupInput()
        {
            UIPopup lowerPopup = CreatePopupPrefab(
                "popup.lower",
                true);

            UIPopup upperPopup = CreatePopupPrefab(
                "popup.upper",
                true);

            ConfigureRegistry(
                lowerPopup,
                upperPopup);

            Result<UIPopup> lowerResult = controller.OpenPopup(
                new UIId("popup.lower"));

            Result<UIPopup> upperResult = controller.OpenPopup(
                new UIId("popup.upper"));

            Assert.That(lowerResult.IsSuccess, Is.True);
            Assert.That(upperResult.IsSuccess, Is.True);

            AssertInputEnabled(lowerResult.Value, false);
            AssertInputEnabled(upperResult.Value, true);

            Object.DestroyImmediate(
                upperResult.Value.gameObject);

            Assert.That(
                controller.TopPopup,
                Is.SameAs(lowerResult.Value));

            Assert.That(controller.PopupCount, Is.EqualTo(1));
            AssertInputEnabled(lowerResult.Value, true);
        }

        [Test]
        public void CanBack_DestroyedBlockingNormalOverlay_RestoresCurrentScreenInput()
        {
            UIScreen firstScreen = CreateScreenPrefab(
                "screen.first");

            UIScreen secondScreen = CreateScreenPrefab(
                "screen.second");

            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.blocking",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(
                firstScreen,
                secondScreen,
                overlay);

            Assert.That(
                controller.OpenScreen(
                    new UIId("screen.first")).IsSuccess,
                Is.True);

            Result<UIScreen> secondResult = controller.OpenScreen(
                new UIId("screen.second"));

            Result<UIOverlay> overlayResult = controller.OpenOverlay(
                new UIId("overlay.blocking"));

            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(overlayResult.IsSuccess, Is.True);

            AssertInputEnabled(secondResult.Value, false);

            Object.DestroyImmediate(
                overlayResult.Value.gameObject);

            Assert.That(controller.CanBack, Is.True);
            AssertInputEnabled(secondResult.Value, true);
        }

        [Test]
        public void CanBack_DestroyedBlockingTopmostOverlay_RestoresPopupInput()
        {
            UIScreen screen = CreateScreenPrefab(
                "screen.main");

            UIPopup popup = CreatePopupPrefab(
                "popup.menu",
                true);

            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.loading",
                UIOverlayLayer.Topmost,
                true);

            ConfigureRegistry(
                screen,
                popup,
                overlay);

            Assert.That(
                controller.OpenScreen(
                    new UIId("screen.main")).IsSuccess,
                Is.True);

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Result<UIOverlay> overlayResult = controller.OpenOverlay(
                new UIId("overlay.loading"));

            Assert.That(popupResult.IsSuccess, Is.True);
            Assert.That(overlayResult.IsSuccess, Is.True);

            AssertInputEnabled(popupResult.Value, false);

            Object.DestroyImmediate(
                overlayResult.Value.gameObject);

            Assert.That(controller.CanBack, Is.True);
            AssertInputEnabled(popupResult.Value, true);
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

        private UIPopup CreatePopupPrefab(string id, bool blocksInput)
        {
            GameObject gameObject = CreateViewGameObject(
                "UIPopup Prefab");

            UIPopup popup = gameObject.AddComponent<UIPopup>();
            popup.SetId(new UIId(id));
            popup.SetBlocksInput(blocksInput);

            gameObject.SetActive(false);

            return popup;
        }

        private UIOverlay CreateOverlayPrefab(string id, UIOverlayLayer layer, bool blocksInput)
        {
            GameObject gameObject = CreateViewGameObject(
                "UIOverlay Prefab");

            UIOverlay overlay = gameObject.AddComponent<UIOverlay>();
            overlay.SetId(new UIId(id));
            overlay.SetOverlayLayer(layer);
            overlay.SetBlocksInput(blocksInput);

            gameObject.SetActive(false);

            return overlay;
        }

        private void AssertInputEnabled(UIView view, bool expected)
        {
            CanvasGroup canvasGroup = view.GetComponent<CanvasGroup>();

            Assert.That(canvasGroup, Is.Not.Null);
            Assert.That(
                canvasGroup.interactable,
                Is.EqualTo(expected));

            Assert.That(
                canvasGroup.blocksRaycasts,
                Is.EqualTo(expected));
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