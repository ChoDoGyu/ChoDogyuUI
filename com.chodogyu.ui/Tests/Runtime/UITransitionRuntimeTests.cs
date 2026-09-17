using System.Collections;
using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UITransitionRuntimeTests
    {
        private readonly List<GameObject> createdGameObjects = new List<GameObject>();

        private UIRegistry registry;
        private UIController controller;
        private Transform screenLayer;
        private Transform overlayLayer;
        private Transform popupLayer;
        private Transform topOverlayLayer;
        private float originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;

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
            Time.timeScale = originalTimeScale;

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
        public void IsBusy_ViewWithoutTransition_ReturnsFalseAfterImmediateLifecycle()
        {
            UIPopup popup = CreatePopupPrefab(
                "popup.instant",
                true);

            ConfigureRegistry(popup);

            Result<UIPopup> openResult = controller.OpenPopup(
                new UIId("popup.instant"));

            Assert.That(openResult.IsSuccess, Is.True);
            Assert.That(openResult.Value.State, Is.EqualTo(UIViewState.Open));
            Assert.That(controller.IsBusy, Is.False);

            Result closeResult = controller.Close(
                new UIId("popup.instant"));

            Assert.That(closeResult.IsSuccess, Is.True);
            Assert.That(openResult.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(controller.IsBusy, Is.False);
        }

        [UnityTest]
        public IEnumerator OpenPopup_WithTransition_StaysOpeningUntilCompletion()
        {
            UIPopup popup = CreatePopupPrefab(
                "popup.menu",
                true,
                3,
                3);

            ConfigureRegistry(popup);

            Result<UIPopup> openResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(openResult.IsSuccess, Is.True);
            Assert.That(openResult.Value.State, Is.EqualTo(UIViewState.Opening));
            Assert.That(controller.PopupCount, Is.EqualTo(1));
            Assert.That(controller.IsBusy, Is.True);
            AssertInputEnabled(openResult.Value, false);

            yield return WaitForState(
                openResult.Value,
                UIViewState.Open);

            Assert.That(controller.IsBusy, Is.False);
            Assert.That(controller.PopupCount, Is.EqualTo(1));
            AssertInputEnabled(openResult.Value, true);
        }

        [UnityTest]
        public IEnumerator ClosePopup_WithTransition_StaysTrackedAndBlockingUntilCompletion()
        {
            UIScreen screen = CreateScreenPrefab("screen.root");

            UIPopup popup = CreatePopupPrefab(
                "popup.menu",
                true,
                2,
                3);

            ConfigureRegistry(
                screen,
                popup);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.root"));

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(popupResult.IsSuccess, Is.True);

            yield return WaitForState(
                popupResult.Value,
                UIViewState.Open);

            AssertInputEnabled(screenResult.Value, false);

            Result closeResult = controller.Close(
                new UIId("popup.menu"));

            Assert.That(closeResult.IsSuccess, Is.True);
            Assert.That(popupResult.Value.State, Is.EqualTo(UIViewState.Closing));
            Assert.That(controller.PopupCount, Is.EqualTo(1));
            Assert.That(controller.TopPopup, Is.SameAs(popupResult.Value));
            Assert.That(controller.IsBusy, Is.True);
            AssertInputEnabled(screenResult.Value, false);

            yield return WaitForState(
                popupResult.Value,
                UIViewState.Closed);

            Assert.That(controller.PopupCount, Is.EqualTo(0));
            Assert.That(controller.TopPopup, Is.Null);
            Assert.That(controller.IsBusy, Is.False);
            AssertInputEnabled(screenResult.Value, true);
        }

        [UnityTest]
        public IEnumerator Back_WhilePopupClosing_ReturnsBusy()
        {
            UIPopup popup = CreatePopupPrefab(
                "popup.menu",
                true,
                2,
                4);

            ConfigureRegistry(popup);

            Result<UIPopup> openResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(openResult.IsSuccess, Is.True);

            yield return WaitForState(
                openResult.Value,
                UIViewState.Open);

            Result closeResult = controller.Close(
                new UIId("popup.menu"));

            Assert.That(closeResult.IsSuccess, Is.True);
            Assert.That(openResult.Value.State, Is.EqualTo(UIViewState.Closing));

            Result backResult = controller.Back();

            Assert.That(backResult.IsFailure, Is.True);
            Assert.That(
                backResult.Error.Code,
                Is.EqualTo(UIErrorCodes.Busy));

            yield return WaitForState(
                openResult.Value,
                UIViewState.Closed);
        }

        [UnityTest]
        public IEnumerator OpenBlockingOverlay_WithTransition_BlocksScreenDuringOpening()
        {
            UIScreen screen = CreateScreenPrefab("screen.root");

            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.loading",
                UIOverlayLayer.Normal,
                true,
                4,
                2);

            ConfigureRegistry(
                screen,
                overlay);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.root"));

            Result<UIOverlay> overlayResult = controller.OpenOverlay(
                new UIId("overlay.loading"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(overlayResult.IsSuccess, Is.True);

            Assert.That(
                overlayResult.Value.State,
                Is.EqualTo(UIViewState.Opening));

            Assert.That(controller.IsBusy, Is.True);
            AssertInputEnabled(overlayResult.Value, false);
            AssertInputEnabled(screenResult.Value, false);

            yield return WaitForState(
                overlayResult.Value,
                UIViewState.Open);

            Assert.That(controller.IsBusy, Is.False);
            AssertInputEnabled(overlayResult.Value, true);
            AssertInputEnabled(screenResult.Value, false);
        }

        [UnityTest]
        public IEnumerator CloseBlockingOverlay_WithTransition_BlocksScreenUntilCompletion()
        {
            UIScreen screen = CreateScreenPrefab("screen.root");

            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.loading",
                UIOverlayLayer.Normal,
                true,
                2,
                4);

            ConfigureRegistry(
                screen,
                overlay);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.root"));

            Result<UIOverlay> overlayResult = controller.OpenOverlay(
                new UIId("overlay.loading"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(overlayResult.IsSuccess, Is.True);

            yield return WaitForState(
                overlayResult.Value,
                UIViewState.Open);

            AssertInputEnabled(screenResult.Value, false);

            Result closeResult = controller.Close(
                new UIId("overlay.loading"));

            Assert.That(closeResult.IsSuccess, Is.True);
            Assert.That(
                overlayResult.Value.State,
                Is.EqualTo(UIViewState.Closing));

            Assert.That(controller.OpenOverlayCount, Is.EqualTo(1));
            AssertInputEnabled(screenResult.Value, false);

            yield return WaitForState(
                overlayResult.Value,
                UIViewState.Closed);

            Assert.That(controller.OpenOverlayCount, Is.EqualTo(0));
            AssertInputEnabled(screenResult.Value, true);
        }

        [UnityTest]
        public IEnumerator ScreenNavigation_WithTransitions_ClosesPreviousBeforeOpeningTarget()
        {
            UIScreen firstScreen = CreateScreenPrefab(
                "screen.first",
                2,
                4);

            UIScreen secondScreen = CreateScreenPrefab(
                "screen.second",
                4,
                2);

            ConfigureRegistry(
                firstScreen,
                secondScreen);

            Result<UIScreen> firstResult = controller.OpenScreen(
                new UIId("screen.first"));

            Assert.That(firstResult.IsSuccess, Is.True);

            yield return WaitForState(
                firstResult.Value,
                UIViewState.Open);

            Result<UIScreen> secondResult = controller.OpenScreen(
                new UIId("screen.second"));

            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(
                firstResult.Value.State,
                Is.EqualTo(UIViewState.Closing));

            Assert.That(
                secondResult.Value.State,
                Is.EqualTo(UIViewState.Closed));

            Assert.That(controller.CurrentScreen, Is.SameAs(firstResult.Value));
            Assert.That(controller.IsBusy, Is.True);

            yield return WaitForState(
                secondResult.Value,
                UIViewState.Opening);

            Assert.That(firstResult.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(controller.CurrentScreen, Is.SameAs(secondResult.Value));
            Assert.That(controller.IsBusy, Is.True);

            yield return WaitForState(
                secondResult.Value,
                UIViewState.Open);

            Assert.That(controller.IsBusy, Is.False);
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));
            Assert.That(controller.CurrentScreen, Is.SameAs(secondResult.Value));
        }

        [UnityTest]
        public IEnumerator OpenScreen_DuringNavigation_ReturnsBusy()
        {
            UIScreen firstScreen = CreateScreenPrefab(
                "screen.first",
                2,
                5);

            UIScreen secondScreen = CreateScreenPrefab(
                "screen.second",
                3,
                2);

            UIScreen thirdScreen = CreateScreenPrefab(
                "screen.third",
                2,
                2);

            ConfigureRegistry(
                firstScreen,
                secondScreen,
                thirdScreen);

            Result<UIScreen> firstResult = controller.OpenScreen(
                new UIId("screen.first"));

            Assert.That(firstResult.IsSuccess, Is.True);

            yield return WaitForState(
                firstResult.Value,
                UIViewState.Open);

            Result<UIScreen> secondResult = controller.OpenScreen(
                new UIId("screen.second"));

            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(controller.IsBusy, Is.True);

            Result<UIScreen> thirdResult = controller.OpenScreen(
                new UIId("screen.third"));

            Assert.That(thirdResult.IsFailure, Is.True);
            Assert.That(
                thirdResult.Error.Code,
                Is.EqualTo(UIErrorCodes.Busy));

            yield return WaitForState(
                secondResult.Value,
                UIViewState.Open);

            Assert.That(controller.CurrentScreen, Is.SameAs(secondResult.Value));
        }

        [UnityTest]
        public IEnumerator Back_DuringScreenNavigation_ReturnsBusy()
        {
            UIScreen firstScreen = CreateScreenPrefab(
                "screen.first",
                2,
                5);

            UIScreen secondScreen = CreateScreenPrefab(
                "screen.second",
                3,
                2);

            ConfigureRegistry(
                firstScreen,
                secondScreen);

            Result<UIScreen> firstResult = controller.OpenScreen(
                new UIId("screen.first"));

            Assert.That(firstResult.IsSuccess, Is.True);

            yield return WaitForState(
                firstResult.Value,
                UIViewState.Open);

            Result<UIScreen> secondResult = controller.OpenScreen(
                new UIId("screen.second"));

            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(controller.IsBusy, Is.True);

            Result backResult = controller.Back();

            Assert.That(backResult.IsFailure, Is.True);
            Assert.That(
                backResult.Error.Code,
                Is.EqualTo(UIErrorCodes.Busy));

            yield return WaitForState(
                secondResult.Value,
                UIViewState.Open);
        }

        [UnityTest]
        public IEnumerator Back_WithScreenTransitions_RestoresPreviousScreen()
        {
            UIScreen firstScreen = CreateScreenPrefab(
                "screen.first",
                2,
                3);

            UIScreen secondScreen = CreateScreenPrefab(
                "screen.second",
                2,
                3);

            ConfigureRegistry(
                firstScreen,
                secondScreen);

            Result<UIScreen> firstResult = controller.OpenScreen(
                new UIId("screen.first"));

            Assert.That(firstResult.IsSuccess, Is.True);

            yield return WaitForState(
                firstResult.Value,
                UIViewState.Open);

            Result<UIScreen> secondResult = controller.OpenScreen(
                new UIId("screen.second"));

            Assert.That(secondResult.IsSuccess, Is.True);

            yield return WaitForState(
                secondResult.Value,
                UIViewState.Open);

            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));

            Result backResult = controller.Back();

            Assert.That(backResult.IsSuccess, Is.True);
            Assert.That(
                secondResult.Value.State,
                Is.EqualTo(UIViewState.Closing));

            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));
            Assert.That(controller.IsBusy, Is.True);

            yield return WaitForState(
                firstResult.Value,
                UIViewState.Opening);

            Assert.That(secondResult.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(0));
            Assert.That(controller.CurrentScreen, Is.SameAs(firstResult.Value));

            yield return WaitForState(
                firstResult.Value,
                UIViewState.Open);

            Assert.That(controller.IsBusy, Is.False);
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator IsBusy_MultipleConcurrentTransitions_RemainsTrueUntilAllComplete()
        {
            UIPopup popup = CreatePopupPrefab(
                "popup.notice",
                false,
                2,
                2);

            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.visual",
                UIOverlayLayer.Topmost,
                false,
                6,
                2);

            ConfigureRegistry(
                popup,
                overlay);

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.notice"));

            Result<UIOverlay> overlayResult = controller.OpenOverlay(
                new UIId("overlay.visual"));

            Assert.That(popupResult.IsSuccess, Is.True);
            Assert.That(overlayResult.IsSuccess, Is.True);
            Assert.That(controller.IsBusy, Is.True);

            yield return WaitForState(
                popupResult.Value,
                UIViewState.Open);

            Assert.That(
                overlayResult.Value.State,
                Is.EqualTo(UIViewState.Opening));

            Assert.That(controller.IsBusy, Is.True);

            yield return WaitForState(
                overlayResult.Value,
                UIViewState.Open);

            Assert.That(controller.IsBusy, Is.False);
        }

        [UnityTest]
        public IEnumerator UIFadeTransition_ZeroDuration_SetsExpectedAlpha()
        {
            UIPopup popup = CreateFadePopupPrefab(
                "popup.fade",
                true,
                0f,
                0f);

            ConfigureRegistry(popup);

            Result<UIPopup> openResult = controller.OpenPopup(
                new UIId("popup.fade"));

            Assert.That(openResult.IsSuccess, Is.True);

            yield return WaitForState(
                openResult.Value,
                UIViewState.Open);

            CanvasGroup canvasGroup = openResult.Value.GetComponent<CanvasGroup>();

            Assert.That(canvasGroup.alpha, Is.EqualTo(1f).Within(0.0001f));

            Result closeResult = controller.Close(
                new UIId("popup.fade"));

            Assert.That(closeResult.IsSuccess, Is.True);

            yield return WaitForState(
                openResult.Value,
                UIViewState.Closed);

            Assert.That(canvasGroup.alpha, Is.EqualTo(0f).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator UIFadeTransition_TimeScaleZero_StillCompletes()
        {
            UIPopup popup = CreateFadePopupPrefab(
                "popup.pause",
                true,
                0.05f,
                0.05f);

            ConfigureRegistry(popup);

            Time.timeScale = 0f;

            Result<UIPopup> openResult = controller.OpenPopup(
                new UIId("popup.pause"));

            Assert.That(openResult.IsSuccess, Is.True);
            Assert.That(controller.IsBusy, Is.True);

            yield return WaitForState(
                openResult.Value,
                UIViewState.Open,
                120);

            Assert.That(controller.IsBusy, Is.False);

            CanvasGroup canvasGroup = openResult.Value.GetComponent<CanvasGroup>();

            Assert.That(canvasGroup.alpha, Is.EqualTo(1f).Within(0.0001f));

            Result closeResult = controller.Close(
                new UIId("popup.pause"));

            Assert.That(closeResult.IsSuccess, Is.True);

            yield return WaitForState(
                openResult.Value,
                UIViewState.Closed,
                120);

            Assert.That(canvasGroup.alpha, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(controller.IsBusy, Is.False);
        }

        private void ConfigureRegistry(params UIView[] prefabs)
        {
            Result result = registry.ReplacePrefabs(prefabs);

            Assert.That(result.IsSuccess, Is.True);
        }

        private UIScreen CreateScreenPrefab(string id)
        {
            GameObject gameObject = CreateViewGameObject("UIScreen Prefab");

            UIScreen screen = gameObject.AddComponent<UIScreen>();
            screen.SetId(new UIId(id));

            gameObject.SetActive(false);

            return screen;
        }

        private UIScreen CreateScreenPrefab(string id, int openingFrames, int closingFrames)
        {
            GameObject gameObject = CreateViewGameObject("UIScreen Prefab");

            UIScreen screen = gameObject.AddComponent<UIScreen>();
            screen.SetId(new UIId(id));

            TestFrameTransition transition = gameObject.AddComponent<TestFrameTransition>();
            transition.SetFrames(openingFrames, closingFrames);

            gameObject.SetActive(false);

            return screen;
        }

        private UIPopup CreatePopupPrefab(string id, bool blocksInput)
        {
            GameObject gameObject = CreateViewGameObject("UIPopup Prefab");

            UIPopup popup = gameObject.AddComponent<UIPopup>();
            popup.SetId(new UIId(id));
            popup.SetBlocksInput(blocksInput);

            gameObject.SetActive(false);

            return popup;
        }

        private UIPopup CreatePopupPrefab(string id, bool blocksInput, int openingFrames, int closingFrames)
        {
            GameObject gameObject = CreateViewGameObject("UIPopup Prefab");

            UIPopup popup = gameObject.AddComponent<UIPopup>();
            popup.SetId(new UIId(id));
            popup.SetBlocksInput(blocksInput);

            TestFrameTransition transition = gameObject.AddComponent<TestFrameTransition>();
            transition.SetFrames(openingFrames, closingFrames);

            gameObject.SetActive(false);

            return popup;
        }

        private UIPopup CreateFadePopupPrefab(string id, bool blocksInput, float openingDuration, float closingDuration)
        {
            GameObject gameObject = CreateViewGameObject("UIFade Popup Prefab");

            UIPopup popup = gameObject.AddComponent<UIPopup>();
            popup.SetId(new UIId(id));
            popup.SetBlocksInput(blocksInput);

            UIFadeTransition transition = gameObject.AddComponent<UIFadeTransition>();
            transition.SetDurations(
                openingDuration,
                closingDuration);

            gameObject.SetActive(false);

            return popup;
        }

        private UIOverlay CreateOverlayPrefab(string id, UIOverlayLayer layer, bool blocksInput, int openingFrames, int closingFrames)
        {
            GameObject gameObject = CreateViewGameObject("UIOverlay Prefab");

            UIOverlay overlay = gameObject.AddComponent<UIOverlay>();
            overlay.SetId(new UIId(id));
            overlay.SetOverlayLayer(layer);
            overlay.SetBlocksInput(blocksInput);

            TestFrameTransition transition = gameObject.AddComponent<TestFrameTransition>();
            transition.SetFrames(openingFrames, closingFrames);

            gameObject.SetActive(false);

            return overlay;
        }

        private IEnumerator WaitForState(UIView view, UIViewState expectedState, int maximumFrames = 60)
        {
            for (int frame = 0; frame < maximumFrames; frame++)
            {
                if (view != null &&
                    view.State == expectedState)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.That(view, Is.Not.Null);
            Assert.That(
                view.State,
                Is.EqualTo(expectedState),
                $"UI가 {maximumFrames} Frame 안에 {expectedState} 상태에 도달하지 못했습니다.");
        }

        private void AssertInputEnabled(UIView view, bool expected)
        {
            CanvasGroup canvasGroup = view.GetComponent<CanvasGroup>();

            Assert.That(canvasGroup, Is.Not.Null);
            Assert.That(canvasGroup.interactable, Is.EqualTo(expected));
            Assert.That(canvasGroup.blocksRaycasts, Is.EqualTo(expected));
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

        private sealed class TestFrameTransition : UIViewTransition
        {
            [SerializeField]
            private int openingFrames;

            [SerializeField]
            private int closingFrames;

            internal void SetFrames(int openingFrames, int closingFrames)
            {
                this.openingFrames = Mathf.Max(
                    0,
                    openingFrames);

                this.closingFrames = Mathf.Max(
                    0,
                    closingFrames);
            }

            protected override IEnumerator OnPlayOpening(CanvasGroup canvasGroup)
            {
                canvasGroup.alpha = 0f;

                for (int frame = 0; frame < openingFrames; frame++)
                {
                    yield return null;
                }

                canvasGroup.alpha = 1f;
            }

            protected override IEnumerator OnPlayClosing(CanvasGroup canvasGroup)
            {
                for (int frame = 0; frame < closingFrames; frame++)
                {
                    yield return null;
                }

                canvasGroup.alpha = 0f;
            }
        }
    }
}