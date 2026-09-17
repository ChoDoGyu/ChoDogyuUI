using System.Collections;
using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UITransitionDestructionRecoveryTests
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

        [UnityTest]
        public IEnumerator Popup_DestroyedWhileOpening_ReleasesBusyAndCleansStack()
        {
            UIPopup popup = CreatePopupPrefab(
                "popup.menu",
                true,
                5,
                2);

            ConfigureRegistry(popup);

            Result<UIPopup> openResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(openResult.IsSuccess, Is.True);
            Assert.That(openResult.Value.State, Is.EqualTo(UIViewState.Opening));
            Assert.That(controller.IsBusy, Is.True);
            Assert.That(controller.PopupCount, Is.EqualTo(1));

            Object.DestroyImmediate(
                openResult.Value.gameObject);

            yield return WaitUntilNotBusy();

            Assert.That(controller.IsBusy, Is.False);
            Assert.That(controller.PopupCount, Is.Zero);
            Assert.That(controller.TopPopup, Is.Null);
        }

        [UnityTest]
        public IEnumerator Popup_DestroyedWhileClosing_ReleasesBusyAndRestoresScreenInput()
        {
            UIScreen screen = CreateScreenPrefab(
                "screen.main");

            UIPopup popup = CreatePopupPrefab(
                "popup.menu",
                true,
                2,
                5);

            ConfigureRegistry(
                screen,
                popup);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.main"));

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(popupResult.IsSuccess, Is.True);

            yield return WaitForState(
                popupResult.Value,
                UIViewState.Open);

            AssertInputEnabled(
                screenResult.Value,
                false);

            Result closeResult = controller.Close(
                new UIId("popup.menu"));

            Assert.That(closeResult.IsSuccess, Is.True);
            Assert.That(popupResult.Value.State, Is.EqualTo(UIViewState.Closing));
            Assert.That(controller.IsBusy, Is.True);

            Object.DestroyImmediate(
                popupResult.Value.gameObject);

            yield return WaitUntilNotBusy();

            Assert.That(controller.IsBusy, Is.False);
            Assert.That(controller.PopupCount, Is.Zero);
            Assert.That(controller.TopPopup, Is.Null);

            AssertInputEnabled(
                screenResult.Value,
                true);
        }

        [UnityTest]
        public IEnumerator Screen_DestroyedWhileOpening_ReleasesNavigationBusy()
        {
            UIScreen screen = CreateScreenPrefab(
                "screen.main",
                5,
                2);

            ConfigureRegistry(screen);

            Result<UIScreen> openResult = controller.OpenScreen(
                new UIId("screen.main"));

            Assert.That(openResult.IsSuccess, Is.True);
            Assert.That(openResult.Value.State, Is.EqualTo(UIViewState.Opening));
            Assert.That(controller.IsBusy, Is.True);

            Object.DestroyImmediate(
                openResult.Value.gameObject);

            yield return WaitUntilNotBusy();

            Assert.That(controller.IsBusy, Is.False);

            Assert.That(
                controller.CurrentScreen == null,
                Is.True);

            Assert.That(controller.ScreenHistoryCount, Is.Zero);
            Assert.That(controller.CanBack, Is.False);
        }

        [UnityTest]
        public IEnumerator PreviousScreen_DestroyedWhileClosing_TargetScreenStillOpens()
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
            Assert.That(firstResult.Value.State, Is.EqualTo(UIViewState.Closing));
            Assert.That(secondResult.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(controller.IsBusy, Is.True);

            Object.DestroyImmediate(
                firstResult.Value.gameObject);

            yield return WaitForState(
                secondResult.Value,
                UIViewState.Open);

            Assert.That(controller.CurrentScreen, Is.SameAs(secondResult.Value));
            Assert.That(secondResult.Value.State, Is.EqualTo(UIViewState.Open));
            Assert.That(controller.IsBusy, Is.False);

            Assert.That(
                controller.ScreenHistoryCount,
                Is.Zero);
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

        private UIScreen CreateScreenPrefab(string id, int openingFrames, int closingFrames)
        {
            GameObject gameObject = CreateViewGameObject(
                "Transition UIScreen Prefab");

            UIScreen screen = gameObject.AddComponent<UIScreen>();
            screen.SetId(new UIId(id));

            TestFrameTransition transition = gameObject.AddComponent<TestFrameTransition>();
            transition.SetFrames(
                openingFrames,
                closingFrames);

            gameObject.SetActive(false);

            return screen;
        }

        private UIPopup CreatePopupPrefab(string id, bool blocksInput, int openingFrames, int closingFrames)
        {
            GameObject gameObject = CreateViewGameObject(
                "Transition UIPopup Prefab");

            UIPopup popup = gameObject.AddComponent<UIPopup>();
            popup.SetId(new UIId(id));
            popup.SetBlocksInput(blocksInput);

            TestFrameTransition transition = gameObject.AddComponent<TestFrameTransition>();
            transition.SetFrames(
                openingFrames,
                closingFrames);

            gameObject.SetActive(false);

            return popup;
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

        private IEnumerator WaitUntilNotBusy(int maximumFrames = 60)
        {
            for (int frame = 0; frame < maximumFrames; frame++)
            {
                if (!controller.IsBusy)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.That(
                controller.IsBusy,
                Is.False,
                $"{maximumFrames} Frame 안에 UI Busy 상태가 해제되지 않았습니다.");
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
                for (int frame = 0; frame < openingFrames; frame++)
                {
                    yield return null;
                }
            }

            protected override IEnumerator OnPlayClosing(CanvasGroup canvasGroup)
            {
                for (int frame = 0; frame < closingFrames; frame++)
                {
                    yield return null;
                }
            }
        }
    }
}