using System;
using CDG.Core.Results;
using CDG.UI.Editor;
using NUnit.Framework;
using UnityEditor;

namespace CDG.UI.Tests.Editor
{
    public sealed class UIRegistryValidatorTests
    {
        private const string TestFolderPath = "Assets/CDG.UI.RegistryValidator.Tests";
        private const string RegistryPath = TestFolderPath + "/UIRegistry.asset";
        private const string FirstScreenPath = TestFolderPath + "/FirstScreen.prefab";
        private const string SecondScreenPath = TestFolderPath + "/SecondScreen.prefab";

        [SetUp]
        public void SetUp()
        {
            DeleteTestFolder();

            if (!AssetDatabase.IsValidFolder(TestFolderPath))
            {
                AssetDatabase.CreateFolder("Assets", "CDG.UI.RegistryValidator.Tests");
            }
        }

        [TearDown]
        public void TearDown()
        {
            Selection.activeObject = null;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            DeleteTestFolder();
        }

        [Test]
        public void Validate_ValidRegistry_ReturnsEmptyReport()
        {
            UIRegistry registry = CreateRegistry();
            UIScreen screen = CreateScreen(FirstScreenPath, "screen.main");

            SetRegistryPrefabs(registry, screen);

            UIValidationReport report = UIRegistryValidator.Validate(registry);

            Assert.That(report.Count, Is.Zero);
            Assert.That(report.HasErrors, Is.False);
        }

        [Test]
        public void Validate_WhenPrefabIsMissing_AddsMissingPrefabError()
        {
            UIRegistry registry = CreateRegistry();

            SetRegistryPrefabs(registry, new UIView[] { null });

            UIValidationReport report = UIRegistryValidator.Validate(registry);

            Assert.That(report.Count, Is.EqualTo(1));
            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.RegistryMissingPrefab));
            Assert.That(report.Issues[0].Severity, Is.EqualTo(UIValidationSeverity.Error));
            Assert.That(report.Issues[0].Context, Is.SameAs(registry));
        }

        [Test]
        public void Validate_WhenViewIdIsBlank_AddsBlankIdError()
        {
            UIRegistry registry = CreateRegistry();
            UIScreen screen = CreateScreen(FirstScreenPath, "screen.main");

            SetViewId(screen, string.Empty);
            SetRegistryPrefabs(registry, screen);

            UIValidationReport report = UIRegistryValidator.Validate(registry);

            Assert.That(report.Count, Is.EqualTo(1));
            Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.RegistryBlankId));
            Assert.That(report.Issues[0].Context, Is.SameAs(screen));
        }

        [Test]
        public void Validate_WhenDuplicateIdsExist_AddsDuplicateIdError()
        {
            UIRegistry registry = CreateRegistry();
            UIScreen firstScreen = CreateScreen(FirstScreenPath, "screen.main");
            UIScreen secondScreen = CreateScreen(SecondScreenPath, "screen.other");

            SetViewId(secondScreen, "screen.main");
            SetRegistryPrefabs(registry, firstScreen, secondScreen);

            UIValidationReport report = UIRegistryValidator.Validate(registry);

            Assert.That(report.Count, Is.EqualTo(1));
            Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.RegistryDuplicateId));
            Assert.That(report.Issues[0].Context, Is.SameAs(secondScreen));
        }

        [Test]
        public void Validate_WhenMultipleProblemsExist_CollectsAllProblems()
        {
            UIRegistry registry = CreateRegistry();
            UIScreen blankScreen = CreateScreen(FirstScreenPath, "screen.blank");
            UIScreen duplicateScreen = CreateScreen(SecondScreenPath, "screen.duplicate");

            SetViewId(blankScreen, string.Empty);
            SetViewId(duplicateScreen, "screen.same");

            string thirdScreenPath = TestFolderPath + "/ThirdScreen.prefab";
            UIScreen thirdScreen = CreateScreen(thirdScreenPath, "screen.same");

            SetRegistryPrefabs(registry, null, blankScreen, duplicateScreen, thirdScreen);

            UIValidationReport report = UIRegistryValidator.Validate(registry);

            Assert.That(report.Count, Is.EqualTo(3));
            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.RegistryMissingPrefab));
            Assert.That(report.Issues[1].Code, Is.EqualTo(UIValidationCodes.RegistryBlankId));
            Assert.That(report.Issues[2].Code, Is.EqualTo(UIValidationCodes.RegistryDuplicateId));
        }

        [Test]
        public void Validate_WhenRegistryIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => UIRegistryValidator.Validate(null));
        }

        private UIRegistry CreateRegistry()
        {
            Result<UIRegistry> result = UIRegistryAssetCreator.Create(RegistryPath);

            Assert.That(result.IsSuccess, Is.True);
            return result.Value;
        }

        private UIScreen CreateScreen(string assetPath, string id)
        {
            Result<UIScreen> result = UIViewPrefabCreator.CreateScreen(assetPath, new UIId(id));

            Assert.That(result.IsSuccess, Is.True);
            return result.Value;
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

        private void SetViewId(UIView view, string id)
        {
            SerializedObject serializedView = new SerializedObject(view);
            SerializedProperty idProperty = serializedView.FindProperty("id");
            SerializedProperty valueProperty = idProperty.FindPropertyRelative("value");

            valueProperty.stringValue = id;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(view);
            AssetDatabase.SaveAssets();
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