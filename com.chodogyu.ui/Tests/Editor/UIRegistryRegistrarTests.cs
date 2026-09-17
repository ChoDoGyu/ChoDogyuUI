using CDG.Core.Results;
using CDG.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CDG.UI.Tests.Editor
{
    public sealed class UIRegistryRegistrarTests
    {
        private const string TestFolderPath = "Assets/CDG.UI.RegistryRegistrar.Tests";
        private const string RegistryPath = TestFolderPath + "/UIRegistry.asset";
        private const string ScreenPath = TestFolderPath + "/MainScreen.prefab";
        private const string OtherScreenPath = TestFolderPath + "/OtherScreen.prefab";

        [SetUp]
        public void SetUp()
        {
            DeleteTestFolder();

            if (!AssetDatabase.IsValidFolder(TestFolderPath))
            {
                AssetDatabase.CreateFolder("Assets", "CDG.UI.RegistryRegistrar.Tests");
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
        public void Register_AddsViewPrefabToRegistry()
        {
            UIRegistry registry = CreateRegistry();
            UIScreen screen = CreateScreen(ScreenPath, "screen.main");

            Result result = UIRegistryRegistrar.Register(registry, screen);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.Prefabs[0], Is.SameAs(screen));
        }

        [Test]
        public void Register_WhenDuplicateIdExists_ReturnsFailureWithoutChangingRegistry()
        {
            UIRegistry registry = CreateRegistry();
            UIScreen firstScreen = CreateScreen(ScreenPath, "screen.main");
            UIScreen secondScreen = CreateScreen(OtherScreenPath, "screen.main");

            Result firstResult = UIRegistryRegistrar.Register(registry, firstScreen);
            Result secondResult = UIRegistryRegistrar.Register(registry, secondScreen);

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsFailure, Is.True);
            Assert.That(secondResult.Error.Code, Is.EqualTo(UIErrorCodes.DuplicateId));

            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.Prefabs[0], Is.SameAs(firstScreen));
        }

        [Test]
        public void Register_WhenRegistryIsNotAsset_ReturnsInvalidRegistry()
        {
            UIRegistry registry = ScriptableObject.CreateInstance<UIRegistry>();
            UIScreen screen = CreateScreen(ScreenPath, "screen.main");

            try
            {
                Result result = UIRegistryRegistrar.Register(registry, screen);

                Assert.That(result.IsFailure, Is.True);
                Assert.That(result.Error.Code, Is.EqualTo(UIEditorErrorCodes.InvalidRegistry));
            }
            finally
            {
                Object.DestroyImmediate(registry);
            }
        }

        [Test]
        public void Register_WhenViewIsNotPrefabAsset_ReturnsInvalidViewPrefab()
        {
            UIRegistry registry = CreateRegistry();

            GameObject viewObject = new GameObject("Runtime Screen", typeof(RectTransform), typeof(CanvasGroup));
            UIScreen screen = viewObject.AddComponent<UIScreen>();

            try
            {
                Result result = UIRegistryRegistrar.Register(registry, screen);

                Assert.That(result.IsFailure, Is.True);
                Assert.That(result.Error.Code, Is.EqualTo(UIEditorErrorCodes.InvalidViewPrefab));
                Assert.That(registry.Count, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
            }
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