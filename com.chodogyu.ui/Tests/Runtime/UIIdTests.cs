using NUnit.Framework;

namespace CDG.UI.Tests.Runtime
{
    public sealed class UIIdTests
    {
        [Test]
        public void Default_HasEmptyValue()
        {
            UIId id = default;

            Assert.That(id.Value, Is.EqualTo(string.Empty));
            Assert.That(id.IsEmpty, Is.True);
        }

        [Test]
        public void Constructor_StoresOriginalValue()
        {
            UIId id = new UIId("settings");

            Assert.That(id.Value, Is.EqualTo("settings"));
            Assert.That(id.IsEmpty, Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("   ")]
        public void IsEmpty_WithEmptyOrWhitespaceValue_ReturnsTrue(string value)
        {
            UIId id = new UIId(value);

            Assert.That(id.IsEmpty, Is.True);
        }

        [Test]
        public void Equals_WithSameValue_ReturnsTrue()
        {
            UIId first = new UIId("settings");
            UIId second = new UIId("settings");

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first == second, Is.True);
            Assert.That(first != second, Is.False);
        }

        [Test]
        public void Equals_WithDifferentValue_ReturnsFalse()
        {
            UIId first = new UIId("settings");
            UIId second = new UIId("inventory");

            Assert.That(first, Is.Not.EqualTo(second));
            Assert.That(first == second, Is.False);
            Assert.That(first != second, Is.True);
        }

        [Test]
        public void Equals_WithDifferentCase_ReturnsFalse()
        {
            UIId first = new UIId("settings");
            UIId second = new UIId("Settings");

            Assert.That(first, Is.Not.EqualTo(second));
        }

        [Test]
        public void SameValue_HasSameHashCode()
        {
            UIId first = new UIId("settings");
            UIId second = new UIId("settings");

            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }

        [Test]
        public void ToString_ReturnsValue()
        {
            UIId id = new UIId("settings");

            Assert.That(id.ToString(), Is.EqualTo("settings"));
        }
    }
}