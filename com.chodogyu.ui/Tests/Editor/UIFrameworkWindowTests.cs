using System.Linq;
using CDG.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CDG.UI.Tests.Editor
{
    public sealed class UIFrameworkWindowTests
    {
        [TearDown]
        public void TearDown()
        {
            UIFrameworkWindow[] windows =
                Resources.FindObjectsOfTypeAll<UIFrameworkWindow>();

            foreach (UIFrameworkWindow window in windows)
            {
                if (window != null)
                {
                    window.Close();
                }
            }
        }

        [Test]
        public void Open_CreatesWindowWithExpectedConfiguration()
        {
            UIFrameworkWindow.Open();

            UIFrameworkWindow window =
                Resources.FindObjectsOfTypeAll<UIFrameworkWindow>()
                    .Single();

            Assert.That(window, Is.Not.Null);
            Assert.That(
                window.titleContent.text,
                Is.EqualTo("CDG UI Framework"));

            Assert.That(
                window.minSize,
                Is.EqualTo(new Vector2(420f, 320f)));
        }

        [Test]
        public void MenuItem_OpensFrameworkWindow()
        {
            bool executed = EditorApplication.ExecuteMenuItem(
                "Tools/ChoDogyu/UI Framework/Open Window");

            Assert.That(executed, Is.True);

            UIFrameworkWindow[] windows =
                Resources.FindObjectsOfTypeAll<UIFrameworkWindow>();

            Assert.That(windows.Length, Is.EqualTo(1));
            Assert.That(windows[0], Is.Not.Null);
        }
    }
}