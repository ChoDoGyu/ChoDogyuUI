using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UIInputBlockingTests
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
        public void OpenScreen_WithoutBlockingView_EnablesScreenInput()
        {
            UIScreen screen = CreateScreenPrefab("screen.main");

            ConfigureRegistry(screen);

            Result<UIScreen> result = controller.OpenScreen(
                new UIId("screen.main"));

            Assert.That(result.IsSuccess, Is.True);

            AssertInputEnabled(result.Value, true);
        }

        [Test]
        public void OpenPopup_Blocking_DisablesScreenInput()
        {
            UIScreen screen = CreateScreenPrefab("screen.main");
            UIPopup popup = CreatePopupPrefab("popup.menu", true);

            ConfigureRegistry(screen, popup);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.main"));

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(popupResult.IsSuccess, Is.True);

            AssertInputEnabled(popupResult.Value, true);
            AssertInputEnabled(screenResult.Value, false);
        }

        [Test]
        public void OpenPopup_NonBlocking_KeepsScreenInputEnabled()
        {
            UIScreen screen = CreateScreenPrefab("screen.main");
            UIPopup popup = CreatePopupPrefab("popup.notice", false);

            ConfigureRegistry(screen, popup);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.main"));

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.notice"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(popupResult.IsSuccess, Is.True);

            AssertInputEnabled(popupResult.Value, true);
            AssertInputEnabled(screenResult.Value, true);
        }

        [Test]
        public void OpenPopup_BlockingTopPopup_DisablesLowerPopupAndScreen()
        {
            UIScreen screen = CreateScreenPrefab("screen.main");
            UIPopup lowerPopup = CreatePopupPrefab("popup.lower", false);
            UIPopup topPopup = CreatePopupPrefab("popup.top", true);

            ConfigureRegistry(screen, lowerPopup, topPopup);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.main"));

            Result<UIPopup> lowerResult = controller.OpenPopup(
                new UIId("popup.lower"));

            Result<UIPopup> topResult = controller.OpenPopup(
                new UIId("popup.top"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(lowerResult.IsSuccess, Is.True);
            Assert.That(topResult.IsSuccess, Is.True);

            AssertInputEnabled(topResult.Value, true);
            AssertInputEnabled(lowerResult.Value, false);
            AssertInputEnabled(screenResult.Value, false);
        }

        [Test]
        public void OpenPopup_NonBlockingTopPopup_AllowsBlockingLowerPopupInputButBlocksScreen()
        {
            UIScreen screen = CreateScreenPrefab("screen.main");
            UIPopup lowerPopup = CreatePopupPrefab("popup.lower", true);
            UIPopup topPopup = CreatePopupPrefab("popup.top", false);

            ConfigureRegistry(screen, lowerPopup, topPopup);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.main"));

            Result<UIPopup> lowerResult = controller.OpenPopup(
                new UIId("popup.lower"));

            Result<UIPopup> topResult = controller.OpenPopup(
                new UIId("popup.top"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(lowerResult.IsSuccess, Is.True);
            Assert.That(topResult.IsSuccess, Is.True);

            AssertInputEnabled(topResult.Value, true);
            AssertInputEnabled(lowerResult.Value, true);
            AssertInputEnabled(screenResult.Value, false);
        }

        [Test]
        public void Close_BlockingPopup_RestoresScreenInput()
        {
            UIScreen screen = CreateScreenPrefab("screen.main");
            UIPopup popup = CreatePopupPrefab("popup.menu", true);

            ConfigureRegistry(screen, popup);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.main"));

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(popupResult.IsSuccess, Is.True);
            AssertInputEnabled(screenResult.Value, false);

            Result closeResult = controller.Close(
                new UIId("popup.menu"));

            Assert.That(closeResult.IsSuccess, Is.True);
            AssertInputEnabled(screenResult.Value, true);
        }

        [Test]
        public void OpenNormalOverlay_Blocking_DisablesScreenInput()
        {
            UIScreen screen = CreateScreenPrefab("screen.main");
            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.blocker",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(screen, overlay);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.main"));

            Result<UIOverlay> overlayResult = controller.OpenOverlay(
                new UIId("overlay.blocker"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(overlayResult.IsSuccess, Is.True);

            AssertInputEnabled(overlayResult.Value, true);
            AssertInputEnabled(screenResult.Value, false);
        }

        [Test]
        public void OpenNormalOverlay_NonBlocking_KeepsScreenInputEnabled()
        {
            UIScreen screen = CreateScreenPrefab("screen.main");
            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.hud",
                UIOverlayLayer.Normal,
                false);

            ConfigureRegistry(screen, overlay);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.main"));

            Result<UIOverlay> overlayResult = controller.OpenOverlay(
                new UIId("overlay.hud"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(overlayResult.IsSuccess, Is.True);

            AssertInputEnabled(overlayResult.Value, true);
            AssertInputEnabled(screenResult.Value, true);
        }

        [Test]
        public void OpenPopup_AboveBlockingNormalOverlay_RemainsInteractive()
        {
            UIScreen screen = CreateScreenPrefab("screen.main");

            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.blocker",
                UIOverlayLayer.Normal,
                true);

            UIPopup popup = CreatePopupPrefab(
                "popup.notice",
                false);

            ConfigureRegistry(screen, overlay, popup);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.main"));

            Result<UIOverlay> overlayResult = controller.OpenOverlay(
                new UIId("overlay.blocker"));

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.notice"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(overlayResult.IsSuccess, Is.True);
            Assert.That(popupResult.IsSuccess, Is.True);

            AssertInputEnabled(popupResult.Value, true);
            AssertInputEnabled(overlayResult.Value, true);
            AssertInputEnabled(screenResult.Value, false);
        }

        [Test]
        public void OpenBlockingPopup_DisablesNormalOverlayAndScreen()
        {
            UIScreen screen = CreateScreenPrefab("screen.main");

            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.hud",
                UIOverlayLayer.Normal,
                false);

            UIPopup popup = CreatePopupPrefab(
                "popup.menu",
                true);

            ConfigureRegistry(screen, overlay, popup);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.main"));

            Result<UIOverlay> overlayResult = controller.OpenOverlay(
                new UIId("overlay.hud"));

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(overlayResult.IsSuccess, Is.True);
            Assert.That(popupResult.IsSuccess, Is.True);

            AssertInputEnabled(popupResult.Value, true);
            AssertInputEnabled(overlayResult.Value, false);
            AssertInputEnabled(screenResult.Value, false);
        }

        [Test]
        public void OpenTopmostOverlay_Blocking_DisablesAllLowerLayers()
        {
            UIScreen screen = CreateScreenPrefab("screen.main");

            UIOverlay normalOverlay = CreateOverlayPrefab(
                "overlay.normal",
                UIOverlayLayer.Normal,
                false);

            UIPopup popup = CreatePopupPrefab(
                "popup.menu",
                false);

            UIOverlay topOverlay = CreateOverlayPrefab(
                "overlay.top",
                UIOverlayLayer.Topmost,
                true);

            ConfigureRegistry(
                screen,
                normalOverlay,
                popup,
                topOverlay);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.main"));

            Result<UIOverlay> normalResult = controller.OpenOverlay(
                new UIId("overlay.normal"));

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Result<UIOverlay> topResult = controller.OpenOverlay(
                new UIId("overlay.top"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(normalResult.IsSuccess, Is.True);
            Assert.That(popupResult.IsSuccess, Is.True);
            Assert.That(topResult.IsSuccess, Is.True);

            AssertInputEnabled(topResult.Value, true);
            AssertInputEnabled(popupResult.Value, false);
            AssertInputEnabled(normalResult.Value, false);
            AssertInputEnabled(screenResult.Value, false);
        }

        [Test]
        public void OpenTopmostOverlay_NonBlocking_AllowsBlockingPopupToControlLowerInput()
        {
            UIScreen screen = CreateScreenPrefab("screen.main");

            UIOverlay normalOverlay = CreateOverlayPrefab(
                "overlay.normal",
                UIOverlayLayer.Normal,
                false);

            UIPopup popup = CreatePopupPrefab(
                "popup.menu",
                true);

            UIOverlay topOverlay = CreateOverlayPrefab(
                "overlay.top",
                UIOverlayLayer.Topmost,
                false);

            ConfigureRegistry(
                screen,
                normalOverlay,
                popup,
                topOverlay);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.main"));

            Result<UIOverlay> normalResult = controller.OpenOverlay(
                new UIId("overlay.normal"));

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Result<UIOverlay> topResult = controller.OpenOverlay(
                new UIId("overlay.top"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(normalResult.IsSuccess, Is.True);
            Assert.That(popupResult.IsSuccess, Is.True);
            Assert.That(topResult.IsSuccess, Is.True);

            AssertInputEnabled(topResult.Value, true);
            AssertInputEnabled(popupResult.Value, true);
            AssertInputEnabled(normalResult.Value, false);
            AssertInputEnabled(screenResult.Value, false);
        }

        [Test]
        public void OpenNormalOverlays_NonBlockingTopAllowsBlockingLowerOverlayInput()
        {
            UIScreen screen = CreateScreenPrefab("screen.main");

            UIOverlay lowerOverlay = CreateOverlayPrefab(
                "overlay.lower",
                UIOverlayLayer.Normal,
                true);

            UIOverlay topOverlay = CreateOverlayPrefab(
                "overlay.top",
                UIOverlayLayer.Normal,
                false);

            ConfigureRegistry(
                screen,
                lowerOverlay,
                topOverlay);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.main"));

            Result<UIOverlay> lowerResult = controller.OpenOverlay(
                new UIId("overlay.lower"));

            Result<UIOverlay> topResult = controller.OpenOverlay(
                new UIId("overlay.top"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(lowerResult.IsSuccess, Is.True);
            Assert.That(topResult.IsSuccess, Is.True);

            AssertInputEnabled(topResult.Value, true);
            AssertInputEnabled(lowerResult.Value, true);
            AssertInputEnabled(screenResult.Value, false);
        }

        [Test]
        public void Close_TopmostBlockingOverlay_RecalculatesRemainingInputState()
        {
            UIScreen screen = CreateScreenPrefab("screen.main");

            UIPopup popup = CreatePopupPrefab(
                "popup.notice",
                false);

            UIOverlay topOverlay = CreateOverlayPrefab(
                "overlay.top",
                UIOverlayLayer.Topmost,
                true);

            ConfigureRegistry(
                screen,
                popup,
                topOverlay);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.main"));

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.notice"));

            Result<UIOverlay> topResult = controller.OpenOverlay(
                new UIId("overlay.top"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(popupResult.IsSuccess, Is.True);
            Assert.That(topResult.IsSuccess, Is.True);

            AssertInputEnabled(popupResult.Value, false);
            AssertInputEnabled(screenResult.Value, false);

            Result closeResult = controller.Close(
                new UIId("overlay.top"));

            Assert.That(closeResult.IsSuccess, Is.True);

            AssertInputEnabled(popupResult.Value, true);
            AssertInputEnabled(screenResult.Value, true);
        }

        private void ConfigureRegistry(params UIView[] prefabs)
        {
            Result result = registry.ReplacePrefabs(prefabs);

            Assert.That(result.IsSuccess, Is.True);
        }

        private UIScreen CreateScreenPrefab(string id)
        {
            GameObject gameObject = CreateGameObject("UIScreen Prefab");

            gameObject.AddComponent<CanvasGroup>();
            gameObject.SetActive(false);

            UIScreen screen = gameObject.AddComponent<UIScreen>();
            screen.SetId(new UIId(id));

            return screen;
        }

        private UIPopup CreatePopupPrefab(string id, bool blocksInput)
        {
            GameObject gameObject = CreateGameObject("UIPopup Prefab");

            gameObject.AddComponent<CanvasGroup>();
            gameObject.SetActive(false);

            UIPopup popup = gameObject.AddComponent<UIPopup>();
            popup.SetId(new UIId(id));
            popup.SetBlocksInput(blocksInput);

            return popup;
        }

        private UIOverlay CreateOverlayPrefab(string id, UIOverlayLayer layer, bool blocksInput)
        {
            GameObject gameObject = CreateGameObject("UIOverlay Prefab");

            gameObject.AddComponent<CanvasGroup>();
            gameObject.SetActive(false);

            UIOverlay overlay = gameObject.AddComponent<UIOverlay>();
            overlay.SetId(new UIId(id));
            overlay.SetOverlayLayer(layer);
            overlay.SetBlocksInput(blocksInput);

            return overlay;
        }

        private void AssertInputEnabled(UIView view, bool expected)
        {
            CanvasGroup canvasGroup = view.GetComponent<CanvasGroup>();

            Assert.That(canvasGroup, Is.Not.Null);
            Assert.That(canvasGroup.interactable, Is.EqualTo(expected));
            Assert.That(canvasGroup.blocksRaycasts, Is.EqualTo(expected));
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