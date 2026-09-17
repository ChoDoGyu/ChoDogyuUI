using System;
using CDG.Core.Results;
using CDG.UI.Editor;
using NUnit.Framework;

namespace CDG.UI.Tests.Editor
{
    public sealed class UIValidationFixTests
    {
        [Test]
        public void Constructor_WhenLabelIsEmpty_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new UIValidationFix(
                string.Empty,
                Result.Success));
        }

        [Test]
        public void Constructor_WhenApplyActionIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new UIValidationFix(
                "Fix",
                null));
        }

        [Test]
        public void Apply_ReturnsRegisteredActionResult()
        {
            UIValidationFix fix = new UIValidationFix(
                "Fix",
                Result.Success);

            Result result = fix.Apply();

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void ValidationIssue_WithFix_ReportsCanFix()
        {
            UIValidationFix fix = new UIValidationFix(
                "Fix",
                Result.Success);

            UIValidationIssue issue = new UIValidationIssue(
                "TEST",
                UIValidationSeverity.Warning,
                "테스트 문제입니다.",
                null,
                fix);

            Assert.That(issue.CanFix, Is.True);
            Assert.That(issue.Fix, Is.SameAs(fix));
        }
    }
}