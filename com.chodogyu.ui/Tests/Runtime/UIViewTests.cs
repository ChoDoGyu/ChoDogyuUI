using NUnit.Framework;
using UnityEngine;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UIViewTests
    {
        private GameObject gameObject;
        private CanvasGroup canvasGroup;
        private TestUIView view;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("TestUIView");
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            view = gameObject.AddComponent<TestUIView>();
        }

        [TearDown]
        public void TearDown()
        {
            if (gameObject != null)
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void NewView_HasClosedState()
        {
            Assert.That(view.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(view.IsOpen, Is.False);
            Assert.That(view.IsTransitioning, Is.False);
        }

        [Test]
        public void SetId_UpdatesId()
        {
            UIId id = new UIId("test-view");

            view.SetId(id);

            Assert.That(view.Id, Is.EqualTo(id));
        }

        [Test]
        public void SetState_Open_UpdatesStateAndIsOpen()
        {
            view.SetState(UIViewState.Open);

            Assert.That(view.State, Is.EqualTo(UIViewState.Open));
            Assert.That(view.IsOpen, Is.True);
            Assert.That(view.IsTransitioning, Is.False);
        }

        [TestCase(UIViewState.Opening)]
        [TestCase(UIViewState.Closing)]
        public void SetState_TransitionState_IsTransitioning(UIViewState state)
        {
            view.SetState(state);

            Assert.That(view.State, Is.EqualTo(state));
            Assert.That(view.IsOpen, Is.False);
            Assert.That(view.IsTransitioning, Is.True);
        }

        [Test]
        public void CanvasGroup_ReturnsAttachedCanvasGroup()
        {
            Assert.That(view.CanvasGroup, Is.SameAs(canvasGroup));
        }

        [Test]
        public void InvokeLifecycle_CallsLifecycleHooks()
        {
            view.InvokeOpening();
            view.InvokeOpened();
            view.InvokeClosing();
            view.InvokeClosed();

            Assert.That(view.OpeningCount, Is.EqualTo(1));
            Assert.That(view.OpenedCount, Is.EqualTo(1));
            Assert.That(view.ClosingCount, Is.EqualTo(1));
            Assert.That(view.ClosedCount, Is.EqualTo(1));
        }

        private sealed class TestUIView : UIView
        {
            public int OpeningCount { get; private set; }

            public int OpenedCount { get; private set; }

            public int ClosingCount { get; private set; }

            public int ClosedCount { get; private set; }

            protected override void OnOpening()
            {
                OpeningCount++;
            }

            protected override void OnOpened()
            {
                OpenedCount++;
            }

            protected override void OnClosing()
            {
                ClosingCount++;
            }

            protected override void OnClosed()
            {
                ClosedCount++;
            }
        }
    }
}