using NUnit.Framework;
using UnityEngine;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UIViewTypeTests
    {
        private GameObject gameObject;

        [TearDown]
        public void TearDown()
        {
            if (gameObject != null)
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void UIScreen_IsUIView()
        {
            UIScreen screen = CreateView<UIScreen>();

            Assert.That(screen, Is.InstanceOf<UIView>());
        }

        [Test]
        public void UIPopup_DefaultBlocksInput_ReturnsTrue()
        {
            UIPopup popup = CreateView<UIPopup>();

            Assert.That(popup.BlocksInput, Is.True);
        }

        [Test]
        public void UIOverlay_DefaultLayer_IsNormal()
        {
            UIOverlay overlay = CreateView<UIOverlay>();

            Assert.That(overlay.OverlayLayer, Is.EqualTo(UIOverlayLayer.Normal));
        }

        [Test]
        public void UIOverlay_DefaultBlocksInput_ReturnsFalse()
        {
            UIOverlay overlay = CreateView<UIOverlay>();

            Assert.That(overlay.BlocksInput, Is.False);
        }

        private T CreateView<T>() where T : UIView
        {
            gameObject = new GameObject(typeof(T).Name);
            gameObject.AddComponent<CanvasGroup>();

            return gameObject.AddComponent<T>();
        }
    }
}