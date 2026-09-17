using CDG.Core.Results;
using CDG.UI.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CDG.UI.Tests.Editor
{
    public sealed class UIRootCreatorTests
    {
        private Scene testScene;

        [SetUp]
        public void SetUp()
        {
            testScene = EditorSceneManager.NewPreviewScene();
        }

        [TearDown]
        public void TearDown()
        {
            if (testScene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(testScene);
            }
        }

        [Test]
        public void Create_CreatesExpectedRootHierarchyAndComponents()
        {
            Result<UIController> result = UIRootCreator.Create(testScene);

            Assert.That(result.IsSuccess, Is.True);

            UIController controller = result.Value;
            GameObject rootObject = controller.gameObject;

            Assert.That(rootObject.scene, Is.EqualTo(testScene));
            Assert.That(rootObject.name, Is.EqualTo("CDG UI Root"));

            Canvas canvas = rootObject.GetComponent<Canvas>();
            GraphicRaycaster raycaster = rootObject.GetComponent<GraphicRaycaster>();

            Assert.That(canvas, Is.Not.Null);
            Assert.That(raycaster, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(rootObject.transform.childCount, Is.EqualTo(4));

            AssertLayer(rootObject.transform.GetChild(0), "Screen Layer", controller.ScreenLayer);
            AssertLayer(rootObject.transform.GetChild(1), "Overlay Layer", controller.OverlayLayer);
            AssertLayer(rootObject.transform.GetChild(2), "Popup Layer", controller.PopupLayer);
            AssertLayer(rootObject.transform.GetChild(3), "Top Overlay Layer", controller.TopOverlayLayer);
        }

        [Test]
        public void Create_DoesNotCreateEventSystem()
        {
            Result<UIController> result = UIRootCreator.Create(testScene);

            Assert.That(result.IsSuccess, Is.True);

            int eventSystemCount = 0;
            GameObject[] rootObjects = testScene.GetRootGameObjects();

            foreach (GameObject rootObject in rootObjects)
            {
                EventSystem[] eventSystems = rootObject.GetComponentsInChildren<EventSystem>(true);
                eventSystemCount += eventSystems.Length;
            }

            Assert.That(eventSystemCount, Is.Zero);
        }

        [Test]
        public void Create_WhenControllerAlreadyExists_ReturnsFailureWithoutDuplicateRoot()
        {
            Result<UIController> firstResult = UIRootCreator.Create(testScene);
            Result<UIController> secondResult = UIRootCreator.Create(testScene);

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsFailure, Is.True);
            Assert.That(secondResult.Error.Code, Is.EqualTo(UIEditorErrorCodes.RootAlreadyExists));

            int controllerCount = 0;
            GameObject[] rootObjects = testScene.GetRootGameObjects();

            foreach (GameObject rootObject in rootObjects)
            {
                UIController[] controllers = rootObject.GetComponentsInChildren<UIController>(true);
                controllerCount += controllers.Length;
            }

            Assert.That(controllerCount, Is.EqualTo(1));
        }

        private void AssertLayer(Transform layer, string expectedName, Transform expectedControllerLayer)
        {
            Assert.That(layer, Is.Not.Null);
            Assert.That(layer.name, Is.EqualTo(expectedName));
            Assert.That(layer, Is.SameAs(expectedControllerLayer));
            Assert.That(layer.gameObject.scene, Is.EqualTo(testScene));

            RectTransform rectTransform = layer as RectTransform;

            Assert.That(rectTransform, Is.Not.Null);
            Assert.That(rectTransform.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rectTransform.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rectTransform.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(rectTransform.offsetMax, Is.EqualTo(Vector2.zero));
        }
    }
}