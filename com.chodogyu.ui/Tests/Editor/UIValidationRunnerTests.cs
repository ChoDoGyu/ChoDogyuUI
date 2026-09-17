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
    public sealed class UIValidationRunnerTests
    {
        private const string TestFolderPath = "Assets/CDG.UI.ValidationRunner.Tests";
        private const string RegistryPath = TestFolderPath + "/UIRegistry.asset";
        private const string ScreenPath = TestFolderPath + "/MainScreen.prefab";

        private Scene testScene;

        [SetUp]
        public void SetUp()
        {
            DeleteTestFolder();

            if (!AssetDatabase.IsValidFolder(TestFolderPath))
            {
                AssetDatabase.CreateFolder("Assets", "CDG.UI.ValidationRunner.Tests");
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
        public void ValidateRegistry_ValidConfiguration_ReturnsEmptyReport()
        {
            UIRegistry registry = CreateRegistry();
            UIScreen screen = CreateScreen();

            SetRegistryPrefabs(registry, screen);

            UIValidationReport report = UIValidationRunner.Validate(registry);

            Assert.That(report.Count, Is.Zero);
            Assert.That(report.HasErrors, Is.False);
        }

        [Test]
        public void ValidateRegistry_CollectsRegistryAndViewProblems()
        {
            UIRegistry registry = CreateRegistry();
            UIScreen screen = CreateInvalidScreenPrefab();

            SetRegistryPrefabs(registry, null, screen);

            UIValidationReport report = UIValidationRunner.Validate(registry);

            Assert.That(report.Count, Is.EqualTo(4));
            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.RegistryMissingPrefab));
            Assert.That(report.Issues[1].Code, Is.EqualTo(UIValidationCodes.RegistryBlankId));
            Assert.That(report.Issues[2].Code, Is.EqualTo(UIValidationCodes.ViewMissingCanvasGroup));
            Assert.That(report.Issues[3].Code, Is.EqualTo(UIValidationCodes.ViewRootActive));
        }

        [Test]
        public void ValidateController_WithValidRegistry_ValidatesWholeConfiguration()
        {
            UIController controller = CreateController();
            UIRegistry registry = CreateRegistry();
            UIScreen screen = CreateScreen();

            SetRegistryPrefabs(registry, screen);
            SetControllerRegistry(controller, registry);

            UIValidationReport report = UIValidationRunner.Validate(controller);

            Assert.That(report.Count, Is.Zero);
            Assert.That(report.HasErrors, Is.False);
        }

        [Test]
        public void ValidateController_WhenRegistryIsMissing_ReturnsControllerProblem()
        {
            UIController controller = CreateController();

            UIValidationReport report = UIValidationRunner.Validate(controller);

            Assert.That(report.Count, Is.EqualTo(1));
            Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.ControllerMissingRegistry));
        }

        [Test]
        public void ValidateController_WhenControllerIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => UIValidationRunner.Validate((UIController)null));
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

        private UIScreen CreateScreen()
        {
            Result<UIScreen> result = UIViewPrefabCreator.CreateScreen(
                ScreenPath,
                new UIId("screen.main"));

            Assert.That(result.IsSuccess, Is.True);
            return result.Value;
        }

        private UIScreen CreateInvalidScreenPrefab()
        {
            GameObject rootObject = new GameObject(
                "Invalid Screen",
                typeof(RectTransform));

            rootObject.AddComponent<UIScreen>();
            rootObject.SetActive(true);

            GameObject prefabObject = PrefabUtility.SaveAsPrefabAsset(rootObject, ScreenPath);
            UnityEngine.Object.DestroyImmediate(rootObject);

            Assert.That(prefabObject, Is.Not.Null);

            UIScreen screen = prefabObject.GetComponent<UIScreen>();

            Assert.That(screen, Is.Not.Null);
            return screen;
        }

        private void SetRegistryPrefabs(UIRegistry registry, params UIView[] prefabs)
        {
            SerializedObject serializedRegistry = new SerializedObject(registry);
            SerializedProperty prefabsProperty = serializedRegistry.FindProperty("prefabs");

            prefabsProperty.arraySize = prefabs.Length;

            for (int i = 0; i < prefabs.Length; i++)
            {
                prefabsProperty.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
            }

            serializedRegistry.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
        }

        private void SetControllerRegistry(UIController controller, UIRegistry registry)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty registryProperty = serializedController.FindProperty("registry");

            registryProperty.objectReferenceValue = registry;
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