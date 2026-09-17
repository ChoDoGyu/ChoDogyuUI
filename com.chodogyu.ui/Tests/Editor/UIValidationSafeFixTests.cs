using CDG.Core.Results;
using CDG.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CDG.UI.Tests.Editor
{
    public sealed class UIValidationSafeFixTests
    {
        private const string TestFolderPath = "Assets/CDG.UI.ValidationSafeFix.Tests";
        private const string ScreenPath = TestFolderPath + "/Screen.prefab";
        private const string FadeScreenPath = TestFolderPath + "/FadeScreen.prefab";

        [SetUp]
        public void SetUp()
        {
            DeleteTestFolder();

            if (!AssetDatabase.IsValidFolder(TestFolderPath))
            {
                AssetDatabase.CreateFolder("Assets", "CDG.UI.ValidationSafeFix.Tests");
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
        public void MissingCanvasGroupFix_AddsCanvasGroup()
        {
            UIScreen screen = CreateManualScreenPrefab(
                ScreenPath,
                false,
                false);

            UIValidationReport report = UIViewValidator.Validate(screen);
            UIValidationIssue issue = report.Issues[0];

            Assert.That(issue.Code, Is.EqualTo(UIValidationCodes.ViewMissingCanvasGroup));
            Assert.That(issue.CanFix, Is.True);

            Result fixResult = issue.Fix.Apply();

            Assert.That(fixResult.IsSuccess, Is.True);

            GameObject fixedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPath);

            Assert.That(fixedPrefab.GetComponent<CanvasGroup>(), Is.Not.Null);

            UIValidationReport fixedReport = UIViewValidator.Validate(fixedPrefab.GetComponent<UIScreen>());

            Assert.That(fixedReport.Count, Is.Zero);
        }

        [Test]
        public void ActiveRootFix_DeactivatesPrefabRoot()
        {
            UIScreen screen = CreateManualScreenPrefab(
                ScreenPath,
                true,
                true);

            UIValidationReport report = UIViewValidator.Validate(screen);
            UIValidationIssue issue = report.Issues[0];

            Assert.That(issue.Code, Is.EqualTo(UIValidationCodes.ViewRootActive));
            Assert.That(issue.CanFix, Is.True);

            Result fixResult = issue.Fix.Apply();

            Assert.That(fixResult.IsSuccess, Is.True);

            GameObject fixedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPath);

            Assert.That(fixedPrefab.activeSelf, Is.False);

            UIValidationReport fixedReport = UIViewValidator.Validate(fixedPrefab.GetComponent<UIScreen>());

            Assert.That(fixedReport.Count, Is.Zero);
        }

        [Test]
        public void NegativeOpeningDurationFix_SetsOnlyOpeningDurationToZero()
        {
            UIFadeTransition transition = CreateFadeScreenPrefab(
                FadeScreenPath,
                -0.5f,
                0.75f);

            UIScreen screen = transition.GetComponent<UIScreen>();
            UIValidationReport report = UIViewTransitionValidator.Validate(screen);
            UIValidationIssue issue = report.Issues[0];

            Assert.That(issue.Code, Is.EqualTo(UIValidationCodes.FadeOpeningDurationNegative));
            Assert.That(issue.CanFix, Is.True);

            Result fixResult = issue.Fix.Apply();

            Assert.That(fixResult.IsSuccess, Is.True);

            GameObject fixedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FadeScreenPath);
            UIFadeTransition fixedTransition = fixedPrefab.GetComponent<UIFadeTransition>();

            Assert.That(fixedTransition.OpeningDuration, Is.EqualTo(0f));
            Assert.That(fixedTransition.ClosingDuration, Is.EqualTo(0.75f));
        }

        [Test]
        public void NegativeClosingDurationFix_SetsOnlyClosingDurationToZero()
        {
            UIFadeTransition transition = CreateFadeScreenPrefab(
                FadeScreenPath,
                0.5f,
                -0.75f);

            UIScreen screen = transition.GetComponent<UIScreen>();
            UIValidationReport report = UIViewTransitionValidator.Validate(screen);
            UIValidationIssue issue = report.Issues[0];

            Assert.That(issue.Code, Is.EqualTo(UIValidationCodes.FadeClosingDurationNegative));
            Assert.That(issue.CanFix, Is.True);

            Result fixResult = issue.Fix.Apply();

            Assert.That(fixResult.IsSuccess, Is.True);

            GameObject fixedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FadeScreenPath);
            UIFadeTransition fixedTransition = fixedPrefab.GetComponent<UIFadeTransition>();

            Assert.That(fixedTransition.OpeningDuration, Is.EqualTo(0.5f));
            Assert.That(fixedTransition.ClosingDuration, Is.EqualTo(0f));
        }

        [Test]
        public void NonPrefabViewProblem_DoesNotProvideSafeFix()
        {
            GameObject viewObject = new GameObject(
                "Runtime Screen",
                typeof(RectTransform));

            UIScreen screen = viewObject.AddComponent<UIScreen>();
            viewObject.SetActive(false);

            try
            {
                UIValidationReport report = UIViewValidator.Validate(screen);

                Assert.That(report.Count, Is.EqualTo(1));
                Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.ViewMissingCanvasGroup));
                Assert.That(report.Issues[0].CanFix, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(viewObject);
            }
        }

        private UIScreen CreateManualScreenPrefab(string assetPath, bool includeCanvasGroup, bool active)
        {
            GameObject rootObject = includeCanvasGroup
                ? new GameObject("Screen", typeof(RectTransform), typeof(CanvasGroup))
                : new GameObject("Screen", typeof(RectTransform));

            rootObject.AddComponent<UIScreen>();
            rootObject.SetActive(active);

            GameObject prefabObject = PrefabUtility.SaveAsPrefabAsset(rootObject, assetPath);
            UnityEngine.Object.DestroyImmediate(rootObject);

            Assert.That(prefabObject, Is.Not.Null);

            UIScreen screen = prefabObject.GetComponent<UIScreen>();

            Assert.That(screen, Is.Not.Null);
            return screen;
        }

        private UIFadeTransition CreateFadeScreenPrefab(string assetPath, float openingDuration, float closingDuration)
        {
            Result<UIScreen> createResult = UIViewPrefabCreator.CreateScreen(
                assetPath,
                new UIId("screen.fade"));

            Assert.That(createResult.IsSuccess, Is.True);

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

            try
            {
                UIFadeTransition transition = prefabRoot.AddComponent<UIFadeTransition>();
                SerializedObject serializedTransition = new SerializedObject(transition);

                serializedTransition.FindProperty("openingDuration").floatValue = openingDuration;
                serializedTransition.FindProperty("closingDuration").floatValue = closingDuration;
                serializedTransition.ApplyModifiedPropertiesWithoutUndo();

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);

                Assert.That(savedPrefab, Is.Not.Null);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            UIFadeTransition savedTransition = prefab.GetComponent<UIFadeTransition>();

            Assert.That(savedTransition, Is.Not.Null);
            return savedTransition;
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