using CDG.Core.Results;
using CDG.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CDG.UI.Tests.Editor
{
    public sealed class UIViewPrefabCreatorTests
    {
        private const string TestFolderPath = "Assets/CDG.UI.ViewPrefab.Tests";

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TestFolderPath);

            if (!AssetDatabase.IsValidFolder(TestFolderPath))
            {
                AssetDatabase.CreateFolder("Assets", "CDG.UI.ViewPrefab.Tests");
            }
        }

        [TearDown]
        public void TearDown()
        {
            Selection.activeObject = null;
            AssetDatabase.DeleteAsset(TestFolderPath);
            AssetDatabase.SaveAssets();
        }

        [Test]
        public void CreateScreen_CreatesExpectedPrefab()
        {
            string assetPath = TestFolderPath + "/MainScreen.prefab";
            Result<UIScreen> result = UIViewPrefabCreator.CreateScreen(assetPath, new UIId("screen.main"));

            Assert.That(result.IsSuccess, Is.True);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.activeSelf, Is.False);
            Assert.That(prefab.GetComponent<RectTransform>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<CanvasGroup>(), Is.Not.Null);

            UIScreen screen = prefab.GetComponent<UIScreen>();

            Assert.That(screen, Is.Not.Null);
            Assert.That(screen.Id, Is.EqualTo(new UIId("screen.main")));

            AssertFullStretch(prefab.GetComponent<RectTransform>());
        }

        [Test]
        public void CreatePopup_UsesBlockingInputByDefault()
        {
            string assetPath = TestFolderPath + "/ConfirmPopup.prefab";
            Result<UIPopup> result = UIViewPrefabCreator.CreatePopup(assetPath, new UIId("popup.confirm"));

            Assert.That(result.IsSuccess, Is.True);

            UIPopup popup = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath).GetComponent<UIPopup>();

            Assert.That(popup, Is.Not.Null);
            Assert.That(popup.BlocksInput, Is.True);
        }

        [Test]
        public void CreateOverlay_AppliesLayerAndBlockingConfiguration()
        {
            string assetPath = TestFolderPath + "/LoadingOverlay.prefab";

            Result<UIOverlay> result = UIViewPrefabCreator.CreateOverlay(
                assetPath,
                new UIId("overlay.loading"),
                UIOverlayLayer.Topmost,
                true);

            Assert.That(result.IsSuccess, Is.True);

            UIOverlay overlay = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath).GetComponent<UIOverlay>();

            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay.OverlayLayer, Is.EqualTo(UIOverlayLayer.Topmost));
            Assert.That(overlay.BlocksInput, Is.True);
        }

        [Test]
        public void CreateScreen_WhenIdIsEmpty_ReturnsFailureWithoutCreatingPrefab()
        {
            string assetPath = TestFolderPath + "/InvalidScreen.prefab";
            Result<UIScreen> result = UIViewPrefabCreator.CreateScreen(assetPath, default);

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIEditorErrorCodes.InvalidViewId));
            Assert.That(AssetDatabase.LoadMainAssetAtPath(assetPath), Is.Null);
        }

        [Test]
        public void CreateScreen_WhenAssetAlreadyExists_ReturnsFailureWithoutReplacingPrefab()
        {
            string assetPath = TestFolderPath + "/MainScreen.prefab";

            Result<UIScreen> firstResult = UIViewPrefabCreator.CreateScreen(assetPath, new UIId("screen.main"));
            Result<UIScreen> secondResult = UIViewPrefabCreator.CreateScreen(assetPath, new UIId("screen.other"));

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsFailure, Is.True);
            Assert.That(secondResult.Error.Code, Is.EqualTo(UIEditorErrorCodes.AssetAlreadyExists));

            UIScreen savedScreen = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath).GetComponent<UIScreen>();

            Assert.That(savedScreen.Id, Is.EqualTo(new UIId("screen.main")));
        }

        private void AssertFullStretch(RectTransform rectTransform)
        {
            Assert.That(rectTransform.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rectTransform.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rectTransform.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(rectTransform.offsetMax, Is.EqualTo(Vector2.zero));
        }
    }
}