using System.Collections;
using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UITransitionFailureTests
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
        public IEnumerator OpenPopup_WhileOpening_ReturnsBusy()
        {
            UIPopup popup = CreateDelayedPopup(
                "popup.menu",
                4,
                2);

            ConfigureRegistry(popup);

            Result<UIPopup> firstResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(firstResult.Value.State, Is.EqualTo(UIViewState.Opening));

            Result<UIPopup> secondResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(secondResult.IsFailure, Is.True);
            Assert.That(
                secondResult.Error.Code,
                Is.EqualTo(UIErrorCodes.Busy));

            Assert.That(controller.PopupCount, Is.EqualTo(1));

            yield return WaitForState(
                firstResult.Value,
                UIViewState.Open);

            Assert.That(controller.IsBusy, Is.False);
        }

        [UnityTest]
        public IEnumerator ClosePopup_WhileOpening_ReturnsBusy()
        {
            UIPopup popup = CreateDelayedPopup(
                "popup.menu",
                4,
                2);

            ConfigureRegistry(popup);

            Result<UIPopup> openResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(openResult.IsSuccess, Is.True);
            Assert.That(openResult.Value.State, Is.EqualTo(UIViewState.Opening));

            Result closeResult = controller.Close(
                new UIId("popup.menu"));

            Assert.That(closeResult.IsFailure, Is.True);
            Assert.That(
                closeResult.Error.Code,
                Is.EqualTo(UIErrorCodes.Busy));

            Assert.That(controller.PopupCount, Is.EqualTo(1));

            yield return WaitForState(
                openResult.Value,
                UIViewState.Open);

            Assert.That(controller.IsBusy, Is.False);
        }

        [UnityTest]
        public IEnumerator ClosePopup_WhileClosing_ReturnsBusy()
        {
            UIPopup popup = CreateDelayedPopup(
                "popup.menu",
                2,
                4);

            ConfigureRegistry(popup);

            Result<UIPopup> openResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Assert.That(openResult.IsSuccess, Is.True);

            yield return WaitForState(
                openResult.Value,
                UIViewState.Open);

            Result firstCloseResult = controller.Close(
                new UIId("popup.menu"));

            Assert.That(firstCloseResult.IsSuccess, Is.True);
            Assert.That(openResult.Value.State, Is.EqualTo(UIViewState.Closing));

            Result secondCloseResult = controller.Close(
                new UIId("popup.menu"));

            Assert.That(secondCloseResult.IsFailure, Is.True);
            Assert.That(
                secondCloseResult.Error.Code,
                Is.EqualTo(UIErrorCodes.Busy));

            Assert.That(controller.PopupCount, Is.EqualTo(1));

            yield return WaitForState(
                openResult.Value,
                UIViewState.Closed);

            Assert.That(controller.PopupCount, Is.EqualTo(0));
            Assert.That(controller.IsBusy, Is.False);
        }

        [UnityTest]
        public IEnumerator NullRoutineTransition_Open_CompletesNormally()
        {
            UIPopup popup = CreateNullRoutinePopup(
                "popup.null");

            ConfigureRegistry(popup);

            Result<UIPopup> result = controller.OpenPopup(
                new UIId("popup.null"));

            Assert.That(result.IsSuccess, Is.True);

            yield return WaitForState(
                result.Value,
                UIViewState.Open);

            Assert.That(result.Value.IsOpen, Is.True);
            Assert.That(controller.IsBusy, Is.False);
            Assert.That(controller.PopupCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator NullRoutineTransition_Close_CompletesNormally()
        {
            UIPopup popup = CreateNullRoutinePopup(
                "popup.null");

            ConfigureRegistry(popup);

            Result<UIPopup> openResult = controller.OpenPopup(
                new UIId("popup.null"));

            Assert.That(openResult.IsSuccess, Is.True);

            yield return WaitForState(
                openResult.Value,
                UIViewState.Open);

            Result closeResult = controller.Close(
                new UIId("popup.null"));

            Assert.That(closeResult.IsSuccess, Is.True);

            yield return WaitForState(
                openResult.Value,
                UIViewState.Closed);

            Assert.That(controller.PopupCount, Is.EqualTo(0));
            Assert.That(controller.IsBusy, Is.False);
        }

        private void ConfigureRegistry(params UIView[] prefabs)
        {
            Result result = registry.ReplacePrefabs(prefabs);

            Assert.That(result.IsSuccess, Is.True);
        }

        private UIPopup CreateDelayedPopup(string id, int openingFrames, int closingFrames)
        {
            GameObject gameObject = CreateViewGameObject("Delayed Popup Prefab");

            UIPopup popup = gameObject.AddComponent<UIPopup>();
            popup.SetId(new UIId(id));
            popup.SetBlocksInput(true);

            TestFrameTransition transition = gameObject.AddComponent<TestFrameTransition>();
            transition.SetFrames(
                openingFrames,
                closingFrames);

            gameObject.SetActive(false);

            return popup;
        }

        private UIPopup CreateNullRoutinePopup(string id)
        {
            GameObject gameObject = CreateViewGameObject("Null Routine Popup Prefab");

            UIPopup popup = gameObject.AddComponent<UIPopup>();
            popup.SetId(new UIId(id));
            popup.SetBlocksInput(true);

            gameObject.AddComponent<NullRoutineTransition>();

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

        private sealed class NullRoutineTransition : UIViewTransition
        {
            protected override IEnumerator OnPlayOpening(CanvasGroup canvasGroup)
            {
                return null;
            }

            protected override IEnumerator OnPlayClosing(CanvasGroup canvasGroup)
            {
                return null;
            }
        }
    }
}