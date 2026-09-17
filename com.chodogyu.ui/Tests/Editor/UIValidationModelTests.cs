using System;
using CDG.UI.Editor;
using NUnit.Framework;

namespace CDG.UI.Tests.Editor
{
    public sealed class UIValidationModelTests
    {
        [Test]
        public void Add_AddsIssueToReport()
        {
            UIValidationReport report = new UIValidationReport();
            UIValidationIssue issue = new UIValidationIssue(
                "TEST_WARNING",
                UIValidationSeverity.Warning,
                "테스트 경고입니다.");

            report.Add(issue);

            Assert.That(report.Count, Is.EqualTo(1));
            Assert.That(report.Issues[0], Is.SameAs(issue));
        }

        [Test]
        public void HasErrors_WhenErrorExists_ReturnsTrue()
        {
            UIValidationReport report = new UIValidationReport();

            report.Add(new UIValidationIssue(
                "TEST_WARNING",
                UIValidationSeverity.Warning,
                "테스트 경고입니다."));

            report.Add(new UIValidationIssue(
                "TEST_ERROR",
                UIValidationSeverity.Error,
                "테스트 오류입니다."));

            Assert.That(report.HasErrors, Is.True);
        }

        [Test]
        public void HasErrors_WhenOnlyWarningsExist_ReturnsFalse()
        {
            UIValidationReport report = new UIValidationReport();

            report.Add(new UIValidationIssue(
                "TEST_WARNING",
                UIValidationSeverity.Warning,
                "테스트 경고입니다."));

            Assert.That(report.HasErrors, Is.False);
        }

        [Test]
        public void Add_WhenIssueIsNull_ThrowsArgumentNullException()
        {
            UIValidationReport report = new UIValidationReport();

            Assert.Throws<ArgumentNullException>(() => report.Add(null));
        }
    }
}