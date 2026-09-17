using System;
using System.Collections;
using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UIScreenNavigationAsyncTests
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

        [UnityTest]
        public IEnumerator Reset_WithOpeningTransition_ClearsHistoryOnlyAfterTargetOpened()
        {
            UIScreen firstScreen = CreateScreenPrefab(
                "screen.first");

            UIScreen secondScreen = CreateScreenPrefab(
                "screen.second");

            UIScreen resetScreen = CreateTransitionScreenPrefab(
                "screen.reset",
                5,
                2);

            ConfigureRegistry(
                firstScreen,
                secondScreen,
                resetScreen);

            Result<UIScreen> firstResult = controller.OpenScreen(
                new UIId("screen.first"));

            Assert.That(firstResult.IsSuccess, Is.True);

            Result<UIScreen> secondResult = controller.OpenScreen(
                new UIId("screen.second"));

            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));

            Result<UIScreen> resetResult = controller.OpenScreen(
                new UIId("screen.reset"),
                UIScreenOpenMode.Reset);

            Assert.That(resetResult.IsSuccess, Is.True);
            Assert.That(resetResult.Value.State, Is.EqualTo(UIViewState.Opening));
            Assert.That(controller.CurrentScreen, Is.SameAs(resetResult.Value));
            Assert.That(controller.ScreenHistoryCount, Is.EqualTo(1));
            Assert.That(controller.IsBusy, Is.True);

            yield return WaitForState(
                resetResult.Value,
                UIViewState.Open);

            Assert.That(controller.CurrentScreen, Is.SameAs(resetResult.Value));
            Assert.That(controller.ScreenHistoryCount, Is.Zero);
            Assert.That(controller.IsBusy, Is.False);
        }

        [Test]
        public void OpenScreen_WithUnsupportedMode_ThrowsBeforeCreatingTargetInstance()
        {
            UIScreen screen = CreateScreenPrefab(
                "screen.target");

            ConfigureRegistry(screen);

            UIScreenOpenMode invalidMode = (UIScreenOpenMode)999;
            UIId targetId = new UIId("screen.target");

            Assert.That(
                controller.TryGetCachedInstance(
                    targetId,
                    out _),
                Is.False);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                controller.OpenScreen(
                    targetId,
                    invalidMode));

            Assert.That(
                controller.TryGetCachedInstance(
                    targetId,
                    out _),
                Is.False);

            Assert.That(controller.CurrentScreen, Is.Null);
            Assert.That(controller.ScreenHistoryCount, Is.Zero);
            Assert.That(controller.IsBusy, Is.False);
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

        private UIScreen CreateTransitionScreenPrefab(string id, int openingFrames, int closingFrames)
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