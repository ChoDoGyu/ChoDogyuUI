using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UIBackRuntimeTests
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
        public void CanBack_RootScreenOnly_ReturnsFalse()
        {
            UIScreen screen = CreateScreenPrefab("screen.root");

            ConfigureRegistry(screen);

            Result<UIScreen> openResult = controller.OpenScreen(
                new UIId("screen.root"));

            Assert.That(openResult.IsSuccess, Is.True);
            Assert.That(controller.CanBack, Is.False);
        }

        [Test]
        public void CanBack_WithScreenHistory_ReturnsTrue()
        {
            UIScreen firstScreen = CreateScreenPrefab("screen.first");
            UIScreen secondScreen = CreateScreenPrefab("screen.second");

            ConfigureRegistry(firstScreen, secondScreen);

            Result<UIScreen> firstResult = controller.OpenScreen(
                new UIId("screen.first"));

            Result<UIScreen> secondResult = controller.OpenScreen(
                new UIId("screen.second"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));
            Assert.That(controller.CanBack, Is.True);
        }

        [Test]
        public void CanBack_WithOpenPopup_ReturnsTrue()
        {
            UIScreen screen = CreateScreenPrefab("screen.root");
            UIPopup popup = CreatePopupPrefab("popup.menu", true);

            ConfigureRegistry(screen, popup);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.root"));

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(popupResult.IsSuccess, Is.True);
            Assert.That(controller.CanBack, Is.True);
        }

        [Test]
        public void CanBack_BlockingTopmostOverlay_ReturnsFalse()
        {
            UIScreen firstScreen = CreateScreenPrefab("screen.first");
            UIScreen secondScreen = CreateScreenPrefab("screen.second");
            UIPopup popup = CreatePopupPrefab("popup.menu", true);

            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.loading",
                UIOverlayLayer.Topmost,
                true);

            ConfigureRegistry(
                firstScreen,
                secondScreen,
                popup,
                overlay);

            Assert.That(
                controller.OpenScreen(new UIId("screen.first")).IsSuccess,
                Is.True);

            Assert.That(
                controller.OpenScreen(new UIId("screen.second")).IsSuccess,
                Is.True);

            Assert.That(
                controller.OpenPopup(new UIId("popup.menu")).IsSuccess,
                Is.True);

            Assert.That(
                controller.OpenOverlay(new UIId("overlay.loading")).IsSuccess,
                Is.True);

            Assert.That(controller.CanBack, Is.False);
        }

        [Test]
        public void CanBack_NonBlockingTopmostOverlayWithPopup_ReturnsTrue()
        {
            UIScreen screen = CreateScreenPrefab("screen.root");
            UIPopup popup = CreatePopupPrefab("popup.menu", true);

            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.hud",
                UIOverlayLayer.Topmost,
                false);

            ConfigureRegistry(screen, popup, overlay);

            Assert.That(
                controller.OpenScreen(new UIId("screen.root")).IsSuccess,
                Is.True);

            Assert.That(
                controller.OpenPopup(new UIId("popup.menu")).IsSuccess,
                Is.True);

            Assert.That(
                controller.OpenOverlay(new UIId("overlay.hud")).IsSuccess,
                Is.True);

            Assert.That(controller.CanBack, Is.True);
        }

        [Test]
        public void CanBack_BlockingNormalOverlayWithoutPopup_ReturnsFalse()
        {
            UIScreen firstScreen = CreateScreenPrefab("screen.first");
            UIScreen secondScreen = CreateScreenPrefab("screen.second");

            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.blocker",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(
                firstScreen,
                secondScreen,
                overlay);

            Assert.That(
                controller.OpenScreen(new UIId("screen.first")).IsSuccess,
                Is.True);

            Assert.That(
                controller.OpenScreen(new UIId("screen.second")).IsSuccess,
                Is.True);

            Assert.That(
                controller.OpenOverlay(new UIId("overlay.blocker")).IsSuccess,
                Is.True);

            Assert.That(controller.CanBack, Is.False);
        }

        [Test]
        public void CanBack_BlockingNormalOverlayWithPopup_ReturnsTrue()
        {
            UIScreen firstScreen = CreateScreenPrefab("screen.first");
            UIScreen secondScreen = CreateScreenPrefab("screen.second");
            UIPopup popup = CreatePopupPrefab("popup.menu", true);

            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.blocker",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(
                firstScreen,
                secondScreen,
                popup,
                overlay);

            Assert.That(
                controller.OpenScreen(new UIId("screen.first")).IsSuccess,
                Is.True);

            Assert.That(
                controller.OpenScreen(new UIId("screen.second")).IsSuccess,
                Is.True);

            Assert.That(
                controller.OpenOverlay(new UIId("overlay.blocker")).IsSuccess,
                Is.True);

            Assert.That(
                controller.OpenPopup(new UIId("popup.menu")).IsSuccess,
                Is.True);

            Assert.That(controller.CanBack, Is.True);
        }

        [Test]
        public void CanBack_TransitioningPopup_ReturnsFalse()
        {
            UIScreen screen = CreateScreenPrefab("screen.root");
            UIPopup popupPrefab = CreatePopupPrefab("popup.menu", true);

            ConfigureRegistry(screen, popupPrefab);

            Assert.That(
                controller.OpenScreen(new UIId("screen.root")).IsSuccess,
                Is.True);

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(popupResult.IsSuccess, Is.True);

            popupResult.Value.BeginClosing();

            Assert.That(
                popupResult.Value.State,
                Is.EqualTo(UIViewState.Closing));

            Assert.That(controller.CanBack, Is.False);
        }

        [Test]
        public void Back_RootScreenOnly_ReturnsNoBackTarget()
        {
            UIScreen screen = CreateScreenPrefab("screen.root");

            ConfigureRegistry(screen);

            Result<UIScreen> openResult = controller.OpenScreen(
                new UIId("screen.root"));

            Assert.That(openResult.IsSuccess, Is.True);

            Result backResult = controller.Back();

            Assert.That(backResult.IsFailure, Is.True);
            Assert.That(
                backResult.Error.Code,
                Is.EqualTo(UIErrorCodes.NoBackTarget));

            Assert.That(controller.CurrentScreen, Is.SameAs(openResult.Value));
            Assert.That(controller.CurrentScreen.State, Is.EqualTo(UIViewState.Open));
        }

        [Test]
        public void Back_OpenPopup_ClosesTopPopup()
        {
            UIScreen screen = CreateScreenPrefab("screen.root");
            UIPopup popup = CreatePopupPrefab("popup.menu", true);

            ConfigureRegistry(screen, popup);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.root"));

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(popupResult.IsSuccess, Is.True);

            Result backResult = controller.Back();

            Assert.That(backResult.IsSuccess, Is.True);
            Assert.That(popupResult.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(controller.PopupCount, Is.EqualTo(0));
            Assert.That(controller.TopPopup, Is.Null);
            Assert.That(controller.CurrentScreen, Is.SameAs(screenResult.Value));
        }

        [Test]
        public void Back_MultiplePopups_ClosesInLifoOrder()
        {
            UIScreen screen = CreateScreenPrefab("screen.root");
            UIPopup lowerPopup = CreatePopupPrefab("popup.lower", true);
            UIPopup topPopup = CreatePopupPrefab("popup.top", true);

            ConfigureRegistry(
                screen,
                lowerPopup,
                topPopup);

            Assert.That(
                controller.OpenScreen(new UIId("screen.root")).IsSuccess,
                Is.True);

            Result<UIPopup> lowerResult = controller.OpenPopup(
                new UIId("popup.lower"));

            Result<UIPopup> topResult = controller.OpenPopup(
                new UIId("popup.top"));

            Assert.That(lowerResult.IsSuccess, Is.True);
            Assert.That(topResult.IsSuccess, Is.True);

            Result firstBackResult = controller.Back();

            Assert.That(firstBackResult.IsSuccess, Is.True);
            Assert.That(topResult.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(lowerResult.Value.State, Is.EqualTo(UIViewState.Open));
            Assert.That(controller.TopPopup, Is.SameAs(lowerResult.Value));
            Assert.That(controller.PopupCount, Is.EqualTo(1));

            Result secondBackResult = controller.Back();

            Assert.That(secondBackResult.IsSuccess, Is.True);
            Assert.That(lowerResult.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(controller.PopupCount, Is.EqualTo(0));
        }

        [Test]
        public void Back_BlockingTopmostOverlay_ReturnsBackBlockedAndKeepsPopupOpen()
        {
            UIScreen screen = CreateScreenPrefab("screen.root");
            UIPopup popup = CreatePopupPrefab("popup.menu", true);

            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.loading",
                UIOverlayLayer.Topmost,
                true);

            ConfigureRegistry(
                screen,
                popup,
                overlay);

            Assert.That(
                controller.OpenScreen(new UIId("screen.root")).IsSuccess,
                Is.True);

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(popupResult.IsSuccess, Is.True);

            Assert.That(
                controller.OpenOverlay(new UIId("overlay.loading")).IsSuccess,
                Is.True);

            Result backResult = controller.Back();

            Assert.That(backResult.IsFailure, Is.True);
            Assert.That(
                backResult.Error.Code,
                Is.EqualTo(UIErrorCodes.BackBlocked));

            Assert.That(popupResult.Value.State, Is.EqualTo(UIViewState.Open));
            Assert.That(controller.PopupCount, Is.EqualTo(1));
        }

        [Test]
        public void Back_BlockingTopmostOverlayBelowNonBlockingOverlay_StillReturnsBackBlocked()
        {
            UIScreen screen = CreateScreenPrefab("screen.root");
            UIPopup popup = CreatePopupPrefab("popup.menu", true);

            UIOverlay blockingOverlay = CreateOverlayPrefab(
                "overlay.blocking",
                UIOverlayLayer.Topmost,
                true);

            UIOverlay nonBlockingOverlay = CreateOverlayPrefab(
                "overlay.visual",
                UIOverlayLayer.Topmost,
                false);

            ConfigureRegistry(
                screen,
                popup,
                blockingOverlay,
                nonBlockingOverlay);

            Assert.That(
                controller.OpenScreen(new UIId("screen.root")).IsSuccess,
                Is.True);

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(popupResult.IsSuccess, Is.True);

            Assert.That(
                controller.OpenOverlay(new UIId("overlay.blocking")).IsSuccess,
                Is.True);

            Assert.That(
                controller.OpenOverlay(new UIId("overlay.visual")).IsSuccess,
                Is.True);

            Result backResult = controller.Back();

            Assert.That(backResult.IsFailure, Is.True);
            Assert.That(
                backResult.Error.Code,
                Is.EqualTo(UIErrorCodes.BackBlocked));

            Assert.That(popupResult.Value.State, Is.EqualTo(UIViewState.Open));
        }

        [Test]
        public void Back_BlockingNormalOverlayWithoutPopup_ReturnsBackBlocked()
        {
            UIScreen firstScreen = CreateScreenPrefab("screen.first");
            UIScreen secondScreen = CreateScreenPrefab("screen.second");

            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.blocker",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(
                firstScreen,
                secondScreen,
                overlay);

            Result<UIScreen> firstResult = controller.OpenScreen(
                new UIId("screen.first"));

            Result<UIScreen> secondResult = controller.OpenScreen(
                new UIId("screen.second"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);

            Assert.That(
                controller.OpenOverlay(new UIId("overlay.blocker")).IsSuccess,
                Is.True);

            Result backResult = controller.Back();

            Assert.That(backResult.IsFailure, Is.True);
            Assert.That(
                backResult.Error.Code,
                Is.EqualTo(UIErrorCodes.BackBlocked));

            Assert.That(controller.CurrentScreen, Is.SameAs(secondResult.Value));
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));
        }

        [Test]
        public void Back_BlockingNormalOverlayWithPopup_ClosesPopupBeforeBlockingScreenBack()
        {
            UIScreen firstScreen = CreateScreenPrefab("screen.first");
            UIScreen secondScreen = CreateScreenPrefab("screen.second");
            UIPopup popup = CreatePopupPrefab("popup.menu", true);

            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.blocker",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(
                firstScreen,
                secondScreen,
                popup,
                overlay);

            Assert.That(
                controller.OpenScreen(new UIId("screen.first")).IsSuccess,
                Is.True);

            Result<UIScreen> secondResult = controller.OpenScreen(
                new UIId("screen.second"));

            Assert.That(secondResult.IsSuccess, Is.True);

            Assert.That(
                controller.OpenOverlay(new UIId("overlay.blocker")).IsSuccess,
                Is.True);

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(popupResult.IsSuccess, Is.True);

            Result firstBackResult = controller.Back();

            Assert.That(firstBackResult.IsSuccess, Is.True);
            Assert.That(popupResult.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(controller.CurrentScreen, Is.SameAs(secondResult.Value));

            Result secondBackResult = controller.Back();

            Assert.That(secondBackResult.IsFailure, Is.True);
            Assert.That(
                secondBackResult.Error.Code,
                Is.EqualTo(UIErrorCodes.BackBlocked));

            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));
        }

        [Test]
        public void Back_ScreenHistory_RestoresPreviousScreen()
        {
            UIScreen firstScreen = CreateScreenPrefab("screen.first");
            UIScreen secondScreen = CreateScreenPrefab("screen.second");

            ConfigureRegistry(
                firstScreen,
                secondScreen);

            Result<UIScreen> firstResult = controller.OpenScreen(
                new UIId("screen.first"));

            Result<UIScreen> secondResult = controller.OpenScreen(
                new UIId("screen.second"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));

            Result backResult = controller.Back();

            Assert.That(backResult.IsSuccess, Is.True);
            Assert.That(controller.CurrentScreen, Is.SameAs(firstResult.Value));
            Assert.That(firstResult.Value.State, Is.EqualTo(UIViewState.Open));
            Assert.That(secondResult.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(0));
        }

        [Test]
        public void Back_ThreeScreens_RestoresHistoryInLifoOrder()
        {
            UIScreen firstScreen = CreateScreenPrefab("screen.first");
            UIScreen secondScreen = CreateScreenPrefab("screen.second");
            UIScreen thirdScreen = CreateScreenPrefab("screen.third");

            ConfigureRegistry(
                firstScreen,
                secondScreen,
                thirdScreen);

            Result<UIScreen> firstResult = controller.OpenScreen(
                new UIId("screen.first"));

            Result<UIScreen> secondResult = controller.OpenScreen(
                new UIId("screen.second"));

            Result<UIScreen> thirdResult = controller.OpenScreen(
                new UIId("screen.third"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(thirdResult.IsSuccess, Is.True);
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(2));

            Result firstBackResult = controller.Back();

            Assert.That(firstBackResult.IsSuccess, Is.True);
            Assert.That(controller.CurrentScreen, Is.SameAs(secondResult.Value));
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));

            Result secondBackResult = controller.Back();

            Assert.That(secondBackResult.IsSuccess, Is.True);
            Assert.That(controller.CurrentScreen, Is.SameAs(firstResult.Value));
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(0));

            Result thirdBackResult = controller.Back();

            Assert.That(thirdBackResult.IsFailure, Is.True);
            Assert.That(
                thirdBackResult.Error.Code,
                Is.EqualTo(UIErrorCodes.NoBackTarget));
        }

        [Test]
        public void Back_AfterReset_ReturnsNoBackTarget()
        {
            UIScreen firstScreen = CreateScreenPrefab("screen.first");
            UIScreen secondScreen = CreateScreenPrefab("screen.second");
            UIScreen resetScreen = CreateScreenPrefab("screen.reset");

            ConfigureRegistry(
                firstScreen,
                secondScreen,
                resetScreen);

            Assert.That(
                controller.OpenScreen(new UIId("screen.first")).IsSuccess,
                Is.True);

            Assert.That(
                controller.OpenScreen(new UIId("screen.second")).IsSuccess,
                Is.True);

            Result<UIScreen> resetResult = controller.OpenScreen(
                new UIId("screen.reset"),
                UIScreenOpenMode.Reset);

            Assert.That(resetResult.IsSuccess, Is.True);
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(0));
            Assert.That(controller.CanBack, Is.False);

            Result backResult = controller.Back();

            Assert.That(backResult.IsFailure, Is.True);
            Assert.That(
                backResult.Error.Code,
                Is.EqualTo(UIErrorCodes.NoBackTarget));

            Assert.That(controller.CurrentScreen, Is.SameAs(resetResult.Value));
        }

        [Test]
        public void Back_AfterReplace_PreservesExistingHistory()
        {
            UIScreen firstScreen = CreateScreenPrefab("screen.first");
            UIScreen secondScreen = CreateScreenPrefab("screen.second");
            UIScreen replacementScreen = CreateScreenPrefab("screen.replacement");

            ConfigureRegistry(
                firstScreen,
                secondScreen,
                replacementScreen);

            Result<UIScreen> firstResult = controller.OpenScreen(
                new UIId("screen.first"));

            Assert.That(firstResult.IsSuccess, Is.True);

            Assert.That(
                controller.OpenScreen(new UIId("screen.second")).IsSuccess,
                Is.True);

            Result<UIScreen> replacementResult = controller.OpenScreen(
                new UIId("screen.replacement"),
                UIScreenOpenMode.Replace);

            Assert.That(replacementResult.IsSuccess, Is.True);
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));

            Result backResult = controller.Back();

            Assert.That(backResult.IsSuccess, Is.True);
            Assert.That(controller.CurrentScreen, Is.SameAs(firstResult.Value));
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(0));
        }

        [Test]
        public void Back_AfterCurrentScreenClosed_RestoresPreviousScreen()
        {
            UIScreen firstScreen = CreateScreenPrefab("screen.first");
            UIScreen secondScreen = CreateScreenPrefab("screen.second");

            ConfigureRegistry(
                firstScreen,
                secondScreen);

            Result<UIScreen> firstResult = controller.OpenScreen(
                new UIId("screen.first"));

            Result<UIScreen> secondResult = controller.OpenScreen(
                new UIId("screen.second"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);

            Result closeResult = controller.Close(
                new UIId("screen.second"));

            Assert.That(closeResult.IsSuccess, Is.True);
            Assert.That(controller.CurrentScreen, Is.Null);
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));
            Assert.That(controller.CanBack, Is.True);

            Result backResult = controller.Back();

            Assert.That(backResult.IsSuccess, Is.True);
            Assert.That(controller.CurrentScreen, Is.SameAs(firstResult.Value));
            Assert.That(firstResult.Value.State, Is.EqualTo(UIViewState.Open));
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(0));
        }

        [Test]
        public void Back_BlockingPopup_RestoresScreenInput()
        {
            UIScreen screen = CreateScreenPrefab("screen.root");
            UIPopup popup = CreatePopupPrefab("popup.menu", true);

            ConfigureRegistry(
                screen,
                popup);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.root"));

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(popupResult.IsSuccess, Is.True);

            AssertInputEnabled(screenResult.Value, false);
            AssertInputEnabled(popupResult.Value, true);

            Result backResult = controller.Back();

            Assert.That(backResult.IsSuccess, Is.True);

            AssertInputEnabled(screenResult.Value, true);
            Assert.That(popupResult.Value.State, Is.EqualTo(UIViewState.Closed));
        }

        [Test]
        public void Back_TransitioningCurrentScreen_ReturnsBusyAndPreservesHistory()
        {
            UIScreen firstScreen = CreateScreenPrefab("screen.first");
            UIScreen secondScreen = CreateScreenPrefab("screen.second");

            ConfigureRegistry(
                firstScreen,
                secondScreen);

            Result<UIScreen> firstResult = controller.OpenScreen(
                new UIId("screen.first"));

            Result<UIScreen> secondResult = controller.OpenScreen(
                new UIId("screen.second"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));

            secondResult.Value.BeginClosing();

            Assert.That(
                secondResult.Value.State,
                Is.EqualTo(UIViewState.Closing));

            Assert.That(controller.CanBack, Is.False);

            Result backResult = controller.Back();

            Assert.That(backResult.IsFailure, Is.True);
            Assert.That(
                backResult.Error.Code,
                Is.EqualTo(UIErrorCodes.Busy));

            Assert.That(controller.CurrentScreen, Is.SameAs(secondResult.Value));
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));
            Assert.That(firstResult.Value.State, Is.EqualTo(UIViewState.Closed));
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

            UIScreen screen = gameObject.AddComponent<UIScreen>();
            screen.SetId(new UIId(id));

            gameObject.SetActive(false);

            return screen;
        }

        private UIPopup CreatePopupPrefab(string id, bool blocksInput)
        {
            GameObject gameObject = CreateGameObject("UIPopup Prefab");

            gameObject.AddComponent<CanvasGroup>();

            UIPopup popup = gameObject.AddComponent<UIPopup>();
            popup.SetId(new UIId(id));
            popup.SetBlocksInput(blocksInput);

            gameObject.SetActive(false);

            return popup;
        }

        private UIOverlay CreateOverlayPrefab(string id, UIOverlayLayer layer, bool blocksInput)
        {
            GameObject gameObject = CreateGameObject("UIOverlay Prefab");

            gameObject.AddComponent<CanvasGroup>();

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