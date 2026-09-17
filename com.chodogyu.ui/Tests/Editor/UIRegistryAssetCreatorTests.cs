using CDG.Core.Results;
using CDG.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CDG.UI.Tests.Editor
{
    public sealed class UIRegistryAssetCreatorTests
    {
        private const string TestFolderPath = "Assets/CDG.UI.Editor.Tests";
        private const string TestAssetPath = TestFolderPath + "/UIRegistry.asset";

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TestFolderPath);

            if (!AssetDatabase.IsValidFolder(TestFolderPath))
            {
                AssetDatabase.CreateFolder("Assets", "CDG.UI.Editor.Tests");
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
        public void Create_CreatesEmptyRegistryAsset()
        {
            Result<UIRegistry> result = UIRegistryAssetCreator.Create(TestAssetPath);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Not.Null);
            Assert.That(result.Value.Count, Is.Zero);

            UIRegistry loadedRegistry = AssetDatabase.LoadAssetAtPath<UIRegistry>(TestAssetPath);

            Assert.That(loadedRegistry, Is.Not.Null);
            Assert.That(loadedRegistry, Is.SameAs(result.Value));
        }

        [Test]
        public void Create_WhenAssetAlreadyExists_ReturnsFailureWithoutReplacingAsset()
        {
            Result<UIRegistry> firstResult = UIRegistryAssetCreator.Create(TestAssetPath);
            Result<UIRegistry> secondResult = UIRegistryAssetCreator.Create(TestAssetPath);

            Assert.That(firstResult.IsSuccess, Is.True);
            Assert.That(secondResult.IsFailure, Is.True);
            Assert.That(secondResult.Error.Code, Is.EqualTo(UIEditorErrorCodes.AssetAlreadyExists));

            UIRegistry loadedRegistry = AssetDatabase.LoadAssetAtPath<UIRegistry>(TestAssetPath);

            Assert.That(loadedRegistry, Is.SameAs(firstResult.Value));
        }

        [Test]
        public void Create_WhenPathIsOutsideAssets_ReturnsInvalidAssetPath()
        {
            Result<UIRegistry> result = UIRegistryAssetCreator.Create("UIRegistry.asset");

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIEditorErrorCodes.InvalidAssetPath));
        }

        [Test]
        public void Create_WhenFolderDoesNotExist_ReturnsInvalidAssetPath()
        {
            Result<UIRegistry> result = UIRegistryAssetCreator.Create("Assets/CDG.UI.Missing/UIRegistry.asset");

            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.Error.Code, Is.EqualTo(UIEditorErrorCodes.InvalidAssetPath));
        }
    }
}