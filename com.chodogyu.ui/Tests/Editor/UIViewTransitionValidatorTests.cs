using System;
using CDG.Core.Results;
using CDG.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CDG.UI.Tests.Editor
{
    public sealed class UIViewTransitionValidatorTests
    {
        private const string TestFolderPath = "Assets/CDG.UI.TransitionValidator.Tests";
        private const string ScreenPath = TestFolderPath + "/MainScreen.prefab";
        private const string ChildTransitionPath = TestFolderPath + "/ChildTransitionScreen.prefab";

        [SetUp]
        public void SetUp()
        {
            DeleteTestFolder();

            if (!AssetDatabase.IsValidFolder(TestFolderPath))
            {
                AssetDatabase.CreateFolder("Assets", "CDG.UI.TransitionValidator.Tests");
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
        public void Validate_ViewWithoutTransition_ReturnsEmptyReport()
        {
            UIScreen screen = CreateScreen(ScreenPath);

            UIValidationReport report = UIViewTransitionValidator.Validate(screen);

            Assert.That(report.Count, Is.Zero);
            Assert.That(report.HasErrors, Is.False);
        }

        [Test]
        public void Validate_ValidFadeTransition_ReturnsEmptyReport()
        {
            UIScreen screen = CreateScreen(ScreenPath);
            UIFadeTransition fadeTransition = AddFadeTransition(screen);

            SetFadeDurations(fadeTransition, 0.2f, 0.3f);

            UIValidationReport report = UIViewTransitionValidator.Validate(screen);

            Assert.That(report.Count, Is.Zero);
        }

        [Test]
        public void Validate_WhenFadeDurationsAreNegative_AddsWarnings()
        {
            UIScreen screen = CreateScreen(ScreenPath);
            UIFadeTransition fadeTransition = AddFadeTransition(screen);

            SetFadeDurations(fadeTransition, -0.1f, -0.2f);

            UIValidationReport report = UIViewTransitionValidator.Validate(screen);

            Assert.That(report.Count, Is.EqualTo(2));
            Assert.That(report.HasErrors, Is.False);

            Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.FadeOpeningDurationNegative));
            Assert.That(report.Issues[0].Severity, Is.EqualTo(UIValidationSeverity.Warning));
            Assert.That(report.Issues[0].Context, Is.SameAs(fadeTransition));

            Assert.That(report.Issues[1].Code, Is.EqualTo(UIValidationCodes.FadeClosingDurationNegative));
            Assert.That(report.Issues[1].Severity, Is.EqualTo(UIValidationSeverity.Warning));
            Assert.That(report.Issues[1].Context, Is.SameAs(fadeTransition));
        }

        [Test]
        public void Validate_WhenTransitionIsOnChild_AddsWarning()
        {
            UIScreen screen = CreateScreenWithChildTransition(ChildTransitionPath);

            UIValidationReport report = UIViewTransitionValidator.Validate(screen);

            Assert.That(report.Count, Is.EqualTo(1));
            Assert.That(report.HasErrors, Is.False);
            Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.TransitionNotOnViewRoot));
            Assert.That(report.Issues[0].Severity, Is.EqualTo(UIValidationSeverity.Warning));
        }

        [Test]
        public void Validate_WhenViewIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => UIViewTransitionValidator.Validate(null));
        }

        private UIScreen CreateScreen(string assetPath)
        {
            Result<UIScreen> result = UIViewPrefabCreator.CreateScreen(
                assetPath,
                new UIId("screen.main"));

            Assert.That(result.IsSuccess, Is.True);
            return result.Value;
        }

        private UIFadeTransition AddFadeTransition(UIScreen screen)
        {
            string assetPath = AssetDatabase.GetAssetPath(screen);

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

            try
            {
                UIFadeTransition fadeTransition = prefabRoot.AddComponent<UIFadeTransition>();

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            GameObject savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            UIFadeTransition savedTransition = savedPrefab.GetComponent<UIFadeTransition>();

            Assert.That(savedTransition, Is.Not.Null);
            return savedTransition;
        }

        private UIScreen CreateScreenWithChildTransition(string assetPath)
        {
            GameObject rootObject = new GameObject("Screen Root", typeof(RectTransform), typeof(CanvasGroup));
            UIScreen screen = rootObject.AddComponent<UIScreen>();

            GameObject childObject = new GameObject("Transition Child", typeof(RectTransform));
            childObject.transform.SetParent(rootObject.transform, false);
            childObject.AddComponent<UIFadeTransition>();

            rootObject.SetActive(false);

            GameObject prefabObject = PrefabUtility.SaveAsPrefabAsset(rootObject, assetPath);
            UnityEngine.Object.DestroyImmediate(rootObject);

            Assert.That(prefabObject, Is.Not.Null);

            UIScreen prefabScreen = prefabObject.GetComponent<UIScreen>();

            Assert.That(prefabScreen, Is.Not.Null);
            return prefabScreen;
        }

        private void SetFadeDurations(UIFadeTransition fadeTransition, float openingDuration, float closingDuration)
        {
            SerializedObject serializedTransition = new SerializedObject(fadeTransition);

            SerializedProperty openingProperty = serializedTransition.FindProperty("openingDuration");
            SerializedProperty closingProperty = serializedTransition.FindProperty("closingDuration");

            openingProperty.floatValue = openingDuration;
            closingProperty.floatValue = closingDuration;

            serializedTransition.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fadeTransition);
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