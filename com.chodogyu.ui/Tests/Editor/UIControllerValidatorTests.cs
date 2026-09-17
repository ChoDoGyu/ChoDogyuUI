using System;
using CDG.Core.Results;
using CDG.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CDG.UI.Tests.Editor
{
    public sealed class UIControllerValidatorTests
    {
        private const string TestFolderPath = "Assets/CDG.UI.ControllerValidator.Tests";
        private const string RegistryPath = TestFolderPath + "/UIRegistry.asset";

        private Scene testScene;

        [SetUp]
        public void SetUp()
        {
            DeleteTestFolder();

            if (!AssetDatabase.IsValidFolder(TestFolderPath))
            {
                AssetDatabase.CreateFolder("Assets", "CDG.UI.ControllerValidator.Tests");
            }

            testScene = EditorSceneManager.NewPreviewScene();
        }

        [TearDown]
        public void TearDown()
        {
            Selection.activeObject = null;

            if (testScene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(testScene);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            DeleteTestFolder();
        }

        [Test]
        public void Validate_ValidController_ReturnsEmptyReport()
        {
            UIController controller = CreateController();
            UIRegistry registry = CreateRegistry();

            SetRegistry(controller, registry);

            UIValidationReport report = UIControllerValidator.Validate(controller);

            Assert.That(report.Count, Is.Zero);
            Assert.That(report.HasErrors, Is.False);
        }

        [Test]
        public void Validate_WhenRegistryIsMissing_AddsMissingRegistryError()
        {
            UIController controller = CreateController();

            UIValidationReport report = UIControllerValidator.Validate(controller);

            Assert.That(report.Count, Is.EqualTo(1));
            Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.ControllerMissingRegistry));
            Assert.That(report.Issues[0].Severity, Is.EqualTo(UIValidationSeverity.Error));
            Assert.That(report.Issues[0].Context, Is.SameAs(controller));
        }

        [Test]
        public void Validate_WhenLayerIsMissing_AddsMissingLayerError()
        {
            UIController controller = CreateController();
            UIRegistry registry = CreateRegistry();

            SetRegistry(controller, registry);
            SetLayer(controller, "popupLayer", null);

            UIValidationReport report = UIControllerValidator.Validate(controller);

            Assert.That(report.Count, Is.EqualTo(1));
            Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.ControllerMissingLayer));
            Assert.That(report.Issues[0].Context, Is.SameAs(controller));
        }

        [Test]
        public void Validate_WhenMultipleLayersAreMissing_CollectsAllMissingLayerErrors()
        {
            UIController controller = CreateController();
            UIRegistry registry = CreateRegistry();

            SetRegistry(controller, registry);
            SetLayer(controller, "screenLayer", null);
            SetLayer(controller, "popupLayer", null);

            UIValidationReport report = UIControllerValidator.Validate(controller);

            Assert.That(report.Count, Is.EqualTo(2));
            Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.ControllerMissingLayer));
            Assert.That(report.Issues[1].Code, Is.EqualTo(UIValidationCodes.ControllerMissingLayer));
        }

        [Test]
        public void Validate_WhenLayersReferenceSameTransform_AddsDuplicateLayerError()
        {
            UIController controller = CreateController();
            UIRegistry registry = CreateRegistry();

            SetRegistry(controller, registry);
            SetLayer(controller, "popupLayer", controller.ScreenLayer);

            UIValidationReport report = UIControllerValidator.Validate(controller);

            Assert.That(report.Count, Is.EqualTo(1));
            Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.ControllerDuplicateLayer));
            Assert.That(report.Issues[0].Severity, Is.EqualTo(UIValidationSeverity.Error));
        }

        [Test]
        public void Validate_WhenRegistryAndLayerAreMissing_CollectsBothProblems()
        {
            UIController controller = CreateController();

            SetLayer(controller, "overlayLayer", null);

            UIValidationReport report = UIControllerValidator.Validate(controller);

            Assert.That(report.Count, Is.EqualTo(2));
            Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.ControllerMissingRegistry));
            Assert.That(report.Issues[1].Code, Is.EqualTo(UIValidationCodes.ControllerMissingLayer));
        }

        [Test]
        public void Validate_WhenControllerIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => UIControllerValidator.Validate(null));
        }

        private UIController CreateController()
        {
            Result<UIController> result = UIRootCreator.Create(testScene);

            Assert.That(result.IsSuccess, Is.True);
            return result.Value;
        }

        private UIRegistry CreateRegistry()
        {
            Result<UIRegistry> result = UIRegistryAssetCreator.Create(RegistryPath);

            Assert.That(result.IsSuccess, Is.True);
            return result.Value;
        }

        private void SetRegistry(UIController controller, UIRegistry registry)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty registryProperty = serializedController.FindProperty("registry");

            registryProperty.objectReferenceValue = registry;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SetLayer(UIController controller, string propertyName, Transform layer)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty layerProperty = serializedController.FindProperty(propertyName);

            layerProperty.objectReferenceValue = layer;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private void DeleteTestFolder()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (AssetDatabase.IsValidFolder(TestFolderPath))
            {
                AssetDatabase.DeleteAsset(TestFolderPath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }
    }
}