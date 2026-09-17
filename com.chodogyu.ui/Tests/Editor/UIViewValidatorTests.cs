using System;
using CDG.Core.Results;
using CDG.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CDG.UI.Tests.Editor
{
    public sealed class UnsupportedUIViewForTests : UIView
    {
    }

    public sealed class UIViewValidatorTests
    {
        private const string TestFolderPath = "Assets/CDG.UI.ViewValidator.Tests";
        private const string ScreenPath = TestFolderPath + "/MainScreen.prefab";
        private const string InvalidScreenPath = TestFolderPath + "/InvalidScreen.prefab";
        private const string OverlayPath = TestFolderPath + "/TestOverlay.prefab";

        [SetUp]
        public void SetUp()
        {
            DeleteTestFolder();

            if (!AssetDatabase.IsValidFolder(TestFolderPath))
            {
                AssetDatabase.CreateFolder("Assets", "CDG.UI.ViewValidator.Tests");
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
        public void Validate_ValidView_ReturnsEmptyReport()
        {
            UIScreen screen = CreateScreen(ScreenPath);

            UIValidationReport report = UIViewValidator.Validate(screen);

            Assert.That(report.Count, Is.Zero);
            Assert.That(report.HasErrors, Is.False);
        }

        [Test]
        public void Validate_WhenCanvasGroupIsMissing_AddsMissingCanvasGroupError()
        {
            UIScreen screen = CreateManualScreenPrefab(
                InvalidScreenPath,
                false,
                false);

            UIValidationReport report = UIViewValidator.Validate(screen);

            Assert.That(report.Count, Is.EqualTo(1));
            Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.ViewMissingCanvasGroup));
            Assert.That(report.Issues[0].Severity, Is.EqualTo(UIValidationSeverity.Error));
            Assert.That(report.Issues[0].Context, Is.SameAs(screen));
        }

        [Test]
        public void Validate_WhenViewRootIsActive_AddsRootActiveError()
        {
            UIScreen screen = CreateManualScreenPrefab(
                InvalidScreenPath,
                true,
                true);

            UIValidationReport report = UIViewValidator.Validate(screen);

            Assert.That(report.Count, Is.EqualTo(1));
            Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.ViewRootActive));
            Assert.That(report.Issues[0].Context, Is.SameAs(screen));
        }

        [Test]
        public void Validate_WhenViewIsNotPrefabRoot_AddsNotPrefabRootError()
        {
            UIScreen screen = CreateNestedScreenPrefab(InvalidScreenPath);

            UIValidationReport report = UIViewValidator.Validate(screen);

            Assert.That(report.Count, Is.EqualTo(1));
            Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.ViewNotPrefabRoot));
            Assert.That(report.Issues[0].Context, Is.SameAs(screen));
        }

        [Test]
        public void Validate_WhenViewTypeIsUnsupported_AddsUnsupportedTypeError()
        {
            GameObject viewObject = new GameObject(
                "Unsupported View",
                typeof(RectTransform),
                typeof(CanvasGroup));

            UnsupportedUIViewForTests view = viewObject.AddComponent<UnsupportedUIViewForTests>();
            viewObject.SetActive(false);

            try
            {
                UIValidationReport report = UIViewValidator.Validate(view);

                Assert.That(report.Count, Is.EqualTo(1));
                Assert.That(report.HasErrors, Is.True);
                Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.ViewUnsupportedType));
                Assert.That(report.Issues[0].Context, Is.SameAs(view));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(viewObject);
            }
        }

        [Test]
        public void Validate_ValidOverlayLayer_ReturnsEmptyReport()
        {
            Result<UIOverlay> result = UIViewPrefabCreator.CreateOverlay(
                OverlayPath,
                new UIId("overlay.test"),
                UIOverlayLayer.Topmost);

            Assert.That(result.IsSuccess, Is.True);

            UIValidationReport report = UIViewValidator.Validate(result.Value);

            Assert.That(report.Count, Is.Zero);
        }

        [Test]
        public void Validate_WhenOverlayLayerIsInvalid_AddsInvalidLayerError()
        {
            Result<UIOverlay> result = UIViewPrefabCreator.CreateOverlay(
                OverlayPath,
                new UIId("overlay.test"));

            Assert.That(result.IsSuccess, Is.True);

            UIOverlay overlay = result.Value;
            SetOverlayLayerRaw(overlay, 99);

            UIValidationReport report = UIViewValidator.Validate(overlay);

            Assert.That(report.Count, Is.EqualTo(1));
            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues[0].Code, Is.EqualTo(UIValidationCodes.OverlayInvalidLayer));
            Assert.That(report.Issues[0].Context, Is.SameAs(overlay));
        }

        [Test]
        public void Validate_WhenViewIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => UIViewValidator.Validate(null));
        }

        private UIScreen CreateScreen(string assetPath)
        {
            Result<UIScreen> result = UIViewPrefabCreator.CreateScreen(
                assetPath,
                new UIId("screen.main"));

            Assert.That(result.IsSuccess, Is.True);
            return result.Value;
        }

        private UIScreen CreateManualScreenPrefab(string assetPath, bool includeCanvasGroup, bool active)
        {
            GameObject rootObject = includeCanvasGroup
                ? new GameObject("Invalid Screen", typeof(RectTransform), typeof(CanvasGroup))
                : new GameObject("Invalid Screen", typeof(RectTransform));

            rootObject.AddComponent<UIScreen>();
            rootObject.SetActive(active);

            GameObject prefabObject = PrefabUtility.SaveAsPrefabAsset(rootObject, assetPath);
            UnityEngine.Object.DestroyImmediate(rootObject);

            Assert.That(prefabObject, Is.Not.Null);

            UIScreen screen = prefabObject.GetComponent<UIScreen>();

            Assert.That(screen, Is.Not.Null);
            return screen;
        }

        private UIScreen CreateNestedScreenPrefab(string assetPath)
        {
            GameObject rootObject = new GameObject("Prefab Root", typeof(RectTransform));
            GameObject viewObject = new GameObject("Nested Screen", typeof(RectTransform), typeof(CanvasGroup));

            viewObject.transform.SetParent(rootObject.transform, false);
            viewObject.AddComponent<UIScreen>();

            rootObject.SetActive(false);
            viewObject.SetActive(false);

            GameObject prefabObject = PrefabUtility.SaveAsPrefabAsset(rootObject, assetPath);
            UnityEngine.Object.DestroyImmediate(rootObject);

            Assert.That(prefabObject, Is.Not.Null);

            UIScreen prefabScreen = prefabObject.GetComponentInChildren<UIScreen>(true);

            Assert.That(prefabScreen, Is.Not.Null);
            return prefabScreen;
        }

        private void SetOverlayLayerRaw(UIOverlay overlay, int value)
        {
            SerializedObject serializedOverlay = new SerializedObject(overlay);
            SerializedProperty layerProperty = serializedOverlay.FindProperty("overlayLayer");

            layerProperty.intValue = value;
            serializedOverlay.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(overlay);
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