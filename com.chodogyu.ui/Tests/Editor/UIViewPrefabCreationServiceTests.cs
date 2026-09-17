using CDG.Core.Results;
using CDG.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CDG.UI.Tests.Editor
{
    public sealed class UIViewPrefabCreationServiceTests
    {
        private const string TestFolderPath = "Assets/CDG.UI.ViewCreationService.Tests";
        private const string RegistryPath = TestFolderPath + "/UIRegistry.asset";
        private const string ScreenPath = TestFolderPath + "/MainScreen.prefab";
        private const string OtherScreenPath = TestFolderPath + "/OtherScreen.prefab";

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TestFolderPath);

            if (!AssetDatabase.IsValidFolder(TestFolderPath))
            {
                AssetDatabase.CreateFolder("Assets", "CDG.UI.ViewCreationService.Tests");
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
        public void Create_WithRegistry_CreatesPrefabAndRegistersIt()
        {
            UIRegistry registry = CreateRegistry();

            Result<UIView> result = UIViewPrefabCreationService.Create(
                ScreenPath,
                UIViewPrefabType.Screen,
                new UIId("screen.main"),
                registry,
                false,
                UIOverlayLayer.Normal);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.TypeOf<UIScreen>());
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPath), Is.Not.Null);

            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.Prefabs[0], Is.SameAs(result.Value));
        }

        [Test]
        public void Create_WithoutRegistry_CreatesPrefabWithoutRegistration()
        {
            Result<UIView> result = UIViewPrefabCreationService.Create(
                ScreenPath,
                UIViewPrefabType.Screen,
                new UIId("screen.main"),
                null,
                false,
                UIOverlayLayer.Normal);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPath), Is.Not.Null);
        }

        [Test]
        public void Create_WhenRegistrationFails_DeletesCreatedPrefab()
        {
            UIRegistry registry = CreateRegistry();

            Result<UIView> firstResult = UIViewPrefabCreationService.Create(
                ScreenPath,
                UIViewPrefabType.Screen,
                new UIId("screen.main"),
                registry,
                false,
                UIOverlayLayer.Normal);

            Result<UIView> secondResult = UIViewPrefabCreationService.Create(
                OtherScreenPath,
                UIViewPrefabType.Screen,
                new UIId("screen.main"),
                registry,
                false,
                UIOverlayLayer.Normal);

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsFailure, Is.True);
            Assert.That(secondResult.Error.Code, Is.EqualTo(UIErrorCodes.DuplicateId));

            Assert.That(AssetDatabase.LoadMainAssetAtPath(OtherScreenPath), Is.Null);
            Assert.That(registry.Count, Is.EqualTo(1));
        }

        [Test]
        public void CreatePopup_AppliesBlockingInputSetting()
        {
            string popupPath = TestFolderPath + "/ConfirmPopup.prefab";

            Result<UIView> result = UIViewPrefabCreationService.Create(
                popupPath,
                UIViewPrefabType.Popup,
                new UIId("popup.confirm"),
                null,
                false,
                UIOverlayLayer.Normal);

            Assert.That(result.IsSuccess, Is.True);

            UIPopup popup = result.Value as UIPopup;

            Assert.That(popup, Is.Not.Null);
            Assert.That(popup.BlocksInput, Is.False);
        }

        [Test]
        public void CreateOverlay_AppliesOverlaySettings()
        {
            string overlayPath = TestFolderPath + "/LoadingOverlay.prefab";

            Result<UIView> result = UIViewPrefabCreationService.Create(
                overlayPath,
                UIViewPrefabType.Overlay,
                new UIId("overlay.loading"),
                null,
                true,
                UIOverlayLayer.Topmost);

            Assert.That(result.IsSuccess, Is.True);

            UIOverlay overlay = result.Value as UIOverlay;

            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay.BlocksInput, Is.True);
            Assert.That(overlay.OverlayLayer, Is.EqualTo(UIOverlayLayer.Topmost));
        }

        private UIRegistry CreateRegistry()
        {
            Result<UIRegistry> result = UIRegistryAssetCreator.Create(RegistryPath);

            Assert.That(result.IsSuccess, Is.True);
            return result.Value;
        }
    }
}