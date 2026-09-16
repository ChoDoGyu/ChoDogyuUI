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
        public void BeginOpening_SetsOpeningStateDisablesInputAndCallsHook()
        {
            gameObject.SetActive(false);

            view.BeginOpening();

            Assert.That(view.State, Is.EqualTo(UIViewState.Opening));
            Assert.That(view.IsOpen, Is.False);
            Assert.That(view.IsTransitioning, Is.True);

            Assert.That(canvasGroup.interactable, Is.False);
            Assert.That(canvasGroup.blocksRaycasts, Is.False);

            Assert.That(gameObject.activeSelf, Is.True);
            Assert.That(view.OpeningCount, Is.EqualTo(1));
        }

        [Test]
        public void CompleteOpening_SetsOpenStateEnablesInputAndCallsHook()
        {
            view.BeginOpening();

            view.CompleteOpening();

            Assert.That(view.State, Is.EqualTo(UIViewState.Open));
            Assert.That(view.IsOpen, Is.True);
            Assert.That(view.IsTransitioning, Is.False);

            Assert.That(canvasGroup.interactable, Is.True);
            Assert.That(canvasGroup.blocksRaycasts, Is.True);

            Assert.That(view.OpenedCount, Is.EqualTo(1));
        }

        [Test]
        public void BeginClosing_SetsClosingStateDisablesInputAndCallsHook()
        {
            view.BeginOpening();
            view.CompleteOpening();

            view.BeginClosing();

            Assert.That(view.State, Is.EqualTo(UIViewState.Closing));
            Assert.That(view.IsOpen, Is.False);
            Assert.That(view.IsTransitioning, Is.True);

            Assert.That(canvasGroup.interactable, Is.False);
            Assert.That(canvasGroup.blocksRaycasts, Is.False);

            Assert.That(view.ClosingCount, Is.EqualTo(1));
        }

        [Test]
        public void CompleteClosing_DisablesGameObjectSetsClosedStateAndCallsHook()
        {
            view.BeginOpening();
            view.CompleteOpening();
            view.BeginClosing();

            view.CompleteClosing();

            Assert.That(gameObject.activeSelf, Is.False);

            Assert.That(view.State, Is.EqualTo(UIViewState.Closed));
            Assert.That(view.IsOpen, Is.False);
            Assert.That(view.IsTransitioning, Is.False);

            Assert.That(view.ClosedCount, Is.EqualTo(1));
        }

        [Test]
        public void Lifecycle_CallsEachHookOnce()
        {
            gameObject.SetActive(false);

            view.BeginOpening();
            view.CompleteOpening();
            view.BeginClosing();
            view.CompleteClosing();

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