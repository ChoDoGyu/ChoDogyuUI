using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UIRuntimeCompositionTests
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
        public void OpenAllBlockingLayers_OnlyTopmostOverlayHasInput()
        {
            RuntimeViews views = OpenAllBlockingLayers();

            AssertInputEnabled(
                views.Screen,
                false);

            AssertInputEnabled(
                views.NormalOverlay,
                false);

            AssertInputEnabled(
                views.Popup,
                false);

            AssertInputEnabled(
                views.TopmostOverlay,
                true);
        }

        [Test]
        public void CloseTopmostOverlay_RestoresPopupInput()
        {
            RuntimeViews views = OpenAllBlockingLayers();

            Result closeResult = controller.Close(
                views.TopmostOverlay.Id);

            Assert.That(closeResult.IsSuccess, Is.True);

            AssertInputEnabled(
                views.Popup,
                true);

            AssertInputEnabled(
                views.NormalOverlay,
                false);

            AssertInputEnabled(
                views.Screen,
                false);
        }

        [Test]
        public void CloseTopmostOverlayAndPopup_RestoresNormalOverlayInput()
        {
            RuntimeViews views = OpenAllBlockingLayers();

            Assert.That(
                controller.Close(
                    views.TopmostOverlay.Id).IsSuccess,
                Is.True);

            Assert.That(
                controller.Close(
                    views.Popup.Id).IsSuccess,
                Is.True);

            AssertInputEnabled(
                views.NormalOverlay,
                true);

            AssertInputEnabled(
                views.Screen,
                false);
        }

        [Test]
        public void CloseAllBlockingLayers_RestoresScreenInput()
        {
            RuntimeViews views = OpenAllBlockingLayers();

            Assert.That(
                controller.Close(
                    views.TopmostOverlay.Id).IsSuccess,
                Is.True);

            Assert.That(
                controller.Close(
                    views.Popup.Id).IsSuccess,
                Is.True);

            Assert.That(
                controller.Close(
                    views.NormalOverlay.Id).IsSuccess,
                Is.True);

            AssertInputEnabled(
                views.Screen,
                true);

            Assert.That(controller.PopupCount, Is.Zero);
            Assert.That(controller.OpenOverlayCount, Is.Zero);
        }

        private RuntimeViews OpenAllBlockingLayers()
        {
            UIScreen screenPrefab = CreateScreenPrefab(
                "screen.main");

            UIOverlay normalOverlayPrefab = CreateOverlayPrefab(
                "overlay.normal",
                UIOverlayLayer.Normal,
                true);

            UIPopup popupPrefab = CreatePopupPrefab(
                "popup.menu",
                true);

            UIOverlay topmostOverlayPrefab = CreateOverlayPrefab(
                "overlay.topmost",
                UIOverlayLayer.Topmost,
                true);

            ConfigureRegistry(
                screenPrefab,
                normalOverlayPrefab,
                popupPrefab,
                topmostOverlayPrefab);

            Result<UIScreen> screenResult = controller.OpenScreen(
                new UIId("screen.main"));

            Result<UIOverlay> normalOverlayResult = controller.OpenOverlay(
                new UIId("overlay.normal"));

            Result<UIPopup> popupResult = controller.OpenPopup(
                new UIId("popup.menu"));

            Result<UIOverlay> topmostOverlayResult = controller.OpenOverlay(
                new UIId("overlay.topmost"));

            Assert.That(screenResult.IsSuccess, Is.True);
            Assert.That(normalOverlayResult.IsSuccess, Is.True);
            Assert.That(popupResult.IsSuccess, Is.True);
            Assert.That(topmostOverlayResult.IsSuccess, Is.True);

            return new RuntimeViews(
                screenResult.Value,
                normalOverlayResult.Value,
                popupResult.Value,
                topmostOverlayResult.Value);
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

        private sealed class RuntimeViews
        {
            internal RuntimeViews(UIScreen screen, UIOverlay normalOverlay, UIPopup popup, UIOverlay topmostOverlay)
            {
                Screen = screen;
                NormalOverlay = normalOverlay;
                Popup = popup;
                TopmostOverlay = topmostOverlay;
            }

            internal UIScreen Screen { get; }

            internal UIOverlay NormalOverlay { get; }

            internal UIPopup Popup { get; }

            internal UIOverlay TopmostOverlay { get; }
        }
    }
}