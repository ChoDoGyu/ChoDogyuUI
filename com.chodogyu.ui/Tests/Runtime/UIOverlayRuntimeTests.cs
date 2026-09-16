using System.Collections.Generic;
using CDG.Core.Results;
using NUnit.Framework;
using UnityEngine;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UIOverlayRuntimeTests
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
        public void NewController_HasNoOpenOverlays()
        {
            Assert.That(controller.OpenOverlayCount, Is.Zero);
        }

        [Test]
        public void OpenOverlay_Normal_OpensAndTracksOverlay()
        {
            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.hud",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(overlay);

            Result<UIOverlay> result = controller.OpenOverlay(
                new UIId("overlay.hud"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.State, Is.EqualTo(UIViewState.Open));
            Assert.That(result.Value.gameObject.activeSelf, Is.True);
            Assert.That(controller.OpenOverlayCount, Is.EqualTo(1));
        }

        [Test]
        public void OpenOverlay_Normal_UsesOverlayLayer()
        {
            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.hud",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(overlay);

            Result<UIOverlay> result = controller.OpenOverlay(
                new UIId("overlay.hud"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.transform.parent, Is.SameAs(overlayLayer));
        }

        [Test]
        public void OpenOverlay_Topmost_UsesTopOverlayLayer()
        {
            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.loading",
                UIOverlayLayer.Topmost,
                true);

            ConfigureRegistry(overlay);

            Result<UIOverlay> result = controller.OpenOverlay(
                new UIId("overlay.loading"));

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.transform.parent, Is.SameAs(topOverlayLayer));
        }

        [Test]
        public void OpenOverlay_SecondOverlayInSameLayer_BecomesLastSibling()
        {
            UIOverlay first = CreateOverlayPrefab(
                "overlay.first",
                UIOverlayLayer.Normal,
                true);

            UIOverlay second = CreateOverlayPrefab(
                "overlay.second",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(first, second);

            Result<UIOverlay> firstResult = controller.OpenOverlay(
                new UIId("overlay.first"));

            Result<UIOverlay> secondResult = controller.OpenOverlay(
                new UIId("overlay.second"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);

            Assert.That(
                secondResult.Value.transform.GetSiblingIndex(),
                Is.EqualTo(overlayLayer.childCount - 1));
        }

        [Test]
        public void OpenOverlay_NormalAndTopmost_TrackIndependently()
        {
            UIOverlay normal = CreateOverlayPrefab(
                "overlay.hud",
                UIOverlayLayer.Normal,
                true);

            UIOverlay topmost = CreateOverlayPrefab(
                "overlay.loading",
                UIOverlayLayer.Topmost,
                true);

            ConfigureRegistry(normal, topmost);

            Result<UIOverlay> normalResult = controller.OpenOverlay(
                new UIId("overlay.hud"));

            Result<UIOverlay> topmostResult = controller.OpenOverlay(
                new UIId("overlay.loading"));

            Assert.That(normalResult.IsSuccess, Is.True);
            Assert.That(topmostResult.IsSuccess, Is.True);

            Assert.That(normalResult.Value.transform.parent, Is.SameAs(overlayLayer));
            Assert.That(topmostResult.Value.transform.parent, Is.SameAs(topOverlayLayer));
            Assert.That(controller.OpenOverlayCount, Is.EqualTo(2));
        }

        [Test]
        public void OpenOverlay_SameOverlay_ReturnsAlreadyOpenWithoutChangingTracking()
        {
            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.hud",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(overlay);

            Result<UIOverlay> firstResult = controller.OpenOverlay(
                new UIId("overlay.hud"));

            Assert.That(firstResult.IsSuccess, Is.True);

            int countBefore = controller.OpenOverlayCount;

            Result<UIOverlay> secondResult = controller.OpenOverlay(
                new UIId("overlay.hud"));

            Assert.That(secondResult.IsFailure, Is.True);
            Assert.That(secondResult.Error.Code, Is.EqualTo(UIErrorCodes.AlreadyOpen));
            Assert.That(controller.OpenOverlayCount, Is.EqualTo(countBefore));
            Assert.That(firstResult.Value.IsOpen, Is.True);
        }

        [Test]
        public void OpenOverlay_MissingOverlay_ReturnsNotFoundWithoutTracking()
        {
            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.hud",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(overlay);

            Result<UIOverlay> result = controller.OpenOverlay(
                new UIId("overlay.missing"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.NotFound));
            Assert.That(controller.OpenOverlayCount, Is.Zero);
        }

        [Test]
        public void OpenOverlay_NonOverlay_ReturnsInvalidTypeWithoutTracking()
        {
            UIScreen screen = CreateViewPrefab<UIScreen>(
                "screen.main",
                true);

            ConfigureRegistry(screen);

            Result<UIOverlay> result = controller.OpenOverlay(
                new UIId("screen.main"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.InvalidType));
            Assert.That(controller.OpenOverlayCount, Is.Zero);
        }

        [Test]
        public void OpenOverlay_MissingCanvasGroup_ReturnsErrorWithoutTracking()
        {
            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.broken",
                UIOverlayLayer.Normal,
                false);

            ConfigureRegistry(overlay);

            Result<UIOverlay> result = controller.OpenOverlay(
                new UIId("overlay.broken"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.MissingCanvasGroup));
            Assert.That(controller.OpenOverlayCount, Is.Zero);
        }

        [Test]
        public void OpenOverlay_NormalMissingLayer_ReturnsMissingLayerWithoutTracking()
        {
            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.hud",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(overlay);

            controller.SetLayers(
                screenLayer,
                null,
                popupLayer,
                topOverlayLayer);

            Result<UIOverlay> result = controller.OpenOverlay(
                new UIId("overlay.hud"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.MissingLayer));
            Assert.That(controller.OpenOverlayCount, Is.Zero);
        }

        [Test]
        public void OpenOverlay_TopmostMissingLayer_ReturnsMissingLayerWithoutTracking()
        {
            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.loading",
                UIOverlayLayer.Topmost,
                true);

            ConfigureRegistry(overlay);

            controller.SetLayers(
                screenLayer,
                overlayLayer,
                popupLayer,
                null);

            Result<UIOverlay> result = controller.OpenOverlay(
                new UIId("overlay.loading"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.MissingLayer));
            Assert.That(controller.OpenOverlayCount, Is.Zero);
        }

        [Test]
        public void OpenOverlay_BusyOverlay_ReturnsBusyWithoutTracking()
        {
            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.hud",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(overlay);

            Result<UIOverlay> instanceResult = controller.GetOrCreate<UIOverlay>(
                new UIId("overlay.hud"));

            Assert.That(instanceResult.IsSuccess, Is.True);

            instanceResult.Value.BeginOpening();

            Result<UIOverlay> result = controller.OpenOverlay(
                new UIId("overlay.hud"));

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIErrorCodes.Busy));
            Assert.That(controller.OpenOverlayCount, Is.Zero);
        }

        [Test]
        public void Close_Overlay_RemovesItFromTracking()
        {
            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.hud",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(overlay);

            Result<UIOverlay> openResult = controller.OpenOverlay(
                new UIId("overlay.hud"));

            Assert.That(openResult.IsSuccess, Is.True);
            Assert.That(controller.OpenOverlayCount, Is.EqualTo(1));

            Result closeResult = controller.Close(
                new UIId("overlay.hud"));

            Assert.That(closeResult.IsSuccess, Is.True);
            Assert.That(openResult.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(openResult.Value.gameObject.activeSelf, Is.False);
            Assert.That(controller.OpenOverlayCount, Is.Zero);
        }

        [Test]
        public void Close_OneOfMultipleOverlays_RemovesOnlySpecifiedOverlay()
        {
            UIOverlay first = CreateOverlayPrefab(
                "overlay.first",
                UIOverlayLayer.Normal,
                true);

            UIOverlay second = CreateOverlayPrefab(
                "overlay.second",
                UIOverlayLayer.Topmost,
                true);

            UIOverlay third = CreateOverlayPrefab(
                "overlay.third",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(first, second, third);

            Result<UIOverlay> firstResult = controller.OpenOverlay(
                new UIId("overlay.first"));

            Result<UIOverlay> secondResult = controller.OpenOverlay(
                new UIId("overlay.second"));

            Result<UIOverlay> thirdResult = controller.OpenOverlay(
                new UIId("overlay.third"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(thirdResult.IsSuccess, Is.True);

            Result closeResult = controller.Close(
                new UIId("overlay.second"));

            Assert.That(closeResult.IsSuccess, Is.True);

            Assert.That(firstResult.Value.IsOpen, Is.True);
            Assert.That(secondResult.Value.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(thirdResult.Value.IsOpen, Is.True);

            Assert.That(controller.OpenOverlayCount, Is.EqualTo(2));
        }

        [Test]
        public void Close_OverlayMissingCanvasGroup_PreservesTracking()
        {
            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.hud",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(overlay);

            Result<UIOverlay> openResult = controller.OpenOverlay(
                new UIId("overlay.hud"));

            Assert.That(openResult.IsSuccess, Is.True);

            CanvasGroup canvasGroup = openResult.Value.CanvasGroup;
            Object.DestroyImmediate(canvasGroup);

            Result closeResult = controller.Close(
                new UIId("overlay.hud"));

            Assert.That(closeResult.IsFailure, Is.True);
            Assert.That(closeResult.Error.Code, Is.EqualTo(UIErrorCodes.MissingCanvasGroup));

            Assert.That(openResult.Value.State, Is.EqualTo(UIViewState.Open));
            Assert.That(controller.OpenOverlayCount, Is.EqualTo(1));
        }

        [Test]
        public void OpenOverlay_AfterClose_ReusesCachedInstanceAndTracksOnce()
        {
            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.hud",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(overlay);

            Result<UIOverlay> firstResult = controller.OpenOverlay(
                new UIId("overlay.hud"));

            Assert.That(firstResult.IsSuccess, Is.True);

            UIOverlay cachedOverlay = firstResult.Value;

            Result closeResult = controller.Close(
                new UIId("overlay.hud"));

            Assert.That(closeResult.IsSuccess, Is.True);
            Assert.That(controller.OpenOverlayCount, Is.Zero);

            Result<UIOverlay> secondResult = controller.OpenOverlay(
                new UIId("overlay.hud"));

            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(secondResult.Value, Is.SameAs(cachedOverlay));
            Assert.That(controller.OpenOverlayCount, Is.EqualTo(1));
        }

        [Test]
        public void OpenOverlay_DestroyedInstance_RecreatesWithoutDuplicateTracking()
        {
            UIOverlay overlay = CreateOverlayPrefab(
                "overlay.hud",
                UIOverlayLayer.Normal,
                true);

            ConfigureRegistry(overlay);

            Result<UIOverlay> firstResult = controller.OpenOverlay(
                new UIId("overlay.hud"));

            Assert.That(firstResult.IsSuccess, Is.True);

            UIOverlay destroyedInstance = firstResult.Value;

            Object.DestroyImmediate(destroyedInstance.gameObject);

            Result<UIOverlay> secondResult = controller.OpenOverlay(
                new UIId("overlay.hud"));

            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(secondResult.Value, Is.Not.SameAs(destroyedInstance));
            Assert.That(secondResult.Value.State, Is.EqualTo(UIViewState.Open));
            Assert.That(controller.OpenOverlayCount, Is.EqualTo(1));
        }

        [Test]
        public void OpenOverlayCount_DestroyedOverlay_CleansTracking()
        {
            UIOverlay first = CreateOverlayPrefab(
                "overlay.first",
                UIOverlayLayer.Normal,
                true);

            UIOverlay second = CreateOverlayPrefab(
                "overlay.second",
                UIOverlayLayer.Topmost,
                true);

            ConfigureRegistry(first, second);

            Result<UIOverlay> firstResult = controller.OpenOverlay(
                new UIId("overlay.first"));

            Result<UIOverlay> secondResult = controller.OpenOverlay(
                new UIId("overlay.second"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);
            Assert.That(controller.OpenOverlayCount, Is.EqualTo(2));

            Object.DestroyImmediate(secondResult.Value.gameObject);

            Assert.That(controller.OpenOverlayCount, Is.EqualTo(1));
            Assert.That(firstResult.Value.IsOpen, Is.True);
        }

        [Test]
        public void OpenOverlayCount_ClosedOverlayRemainingInOrder_CleansAutomatically()
        {
            UIOverlay first = CreateOverlayPrefab(
                "overlay.first",
                UIOverlayLayer.Normal,
                true);

            UIOverlay second = CreateOverlayPrefab(
                "overlay.second",
                UIOverlayLayer.Topmost,
                true);

            ConfigureRegistry(first, second);

            Result<UIOverlay> firstResult = controller.OpenOverlay(
                new UIId("overlay.first"));

            Result<UIOverlay> secondResult = controller.OpenOverlay(
                new UIId("overlay.second"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsSuccess, Is.True);

            Result closeResult = controller.CloseView(secondResult.Value);

            Assert.That(closeResult.IsSuccess, Is.True);

            Assert.That(controller.OpenOverlayCount, Is.EqualTo(1));
            Assert.That(firstResult.Value.IsOpen, Is.True);
        }

        private void ConfigureRegistry(params UIView[] prefabs)
        {
            Result result = registry.ReplacePrefabs(prefabs);

            Assert.That(result.IsSuccess, Is.True);
        }

        private UIOverlay CreateOverlayPrefab(string id, UIOverlayLayer layer, bool includeCanvasGroup)
        {
            GameObject gameObject = CreateGameObject("UIOverlay Prefab");

            if (includeCanvasGroup)
            {
                gameObject.AddComponent<CanvasGroup>();
            }

            gameObject.SetActive(false);

            UIOverlay overlay = gameObject.AddComponent<UIOverlay>();
            overlay.SetId(new UIId(id));
            overlay.SetOverlayLayer(layer);

            return overlay;
        }

        private T CreateViewPrefab<T>(string id, bool includeCanvasGroup) where T : UIView
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