using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UIRuntimeCleanupRecoveryTests
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
        public void Close_InvalidIdAfterDestroyedBlockingOverlay_RestoresScreenInput()
        {
            UIScreen screen = CreateScreenPrefab(
                "screen.main");

            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.blocking",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(
                screen,
                overlay);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.main"));

            Result<UIOverlay> overlayResult = controller.OpenOverlay(
                new UIId("overlay.blocking"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(overlayResult.IsSuccess, Is.True);

            AssertInputEnabled(
                screenResult.Value,
                false);

            Object.DestroyImmediate(
                overlayResult.Value.gameObject);

            Result closeResult = controller.Close(default);

            Assert.That(closeResult.IsFailure, Is.True);
            Assert.That(
                closeResult.Error.Code,
                Is.EqualTo(UIErrorCodes.InvalidId));

            AssertInputEnabled(
                screenResult.Value,
                true);
        }

        [Test]
        public void OpenOverlay_FailedRequestAfterDestroyedBlockingOverlay_RestoresScreenInput()
        {
            UIScreen screen = CreateScreenPrefab(
                "screen.main");

            UIOverlay blockingOverlay = CreateOverlayPrefab(
                "overlay.blocking",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(
                screen,
                blockingOverlay);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.main"));

            Result<UIOverlay> overlayResult = controller.OpenOverlay(
                new UIId("overlay.blocking"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(overlayResult.IsSuccess, Is.True);

            AssertInputEnabled(
                screenResult.Value,
                false);

            Object.DestroyImmediate(
                overlayResult.Value.gameObject);

            Result<UIOverlay> failedResult = controller.OpenOverlay(
                new UIId("overlay.missing"));

            Assert.That(failedResult.IsFailure, Is.True);
            Assert.That(
                failedResult.Error.Code,
                Is.EqualTo(UIErrorCodes.NotFound));

            AssertInputEnabled(
                screenResult.Value,
                true);
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
    }
}