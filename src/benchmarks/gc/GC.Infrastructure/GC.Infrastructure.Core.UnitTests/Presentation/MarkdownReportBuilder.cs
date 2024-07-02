using FluentAssertions;
using GC.Infrastructure.Core.Presentation;

namespace GC.Infrastructure.Core.UnitTests.Presentation
{
    [TestClass]
    public class MarkdownReportBuilderTests
    {
        [TestMethod]
        public void CopySectionFromMarkdown_CopySuccessful1_Successful()
        {
            string text = "# Section1\na\nb\nc\nd\n# Section 2";
            string details = MarkdownReportBuilder.CopySectionFromMarkDown(text, "Section1");
            details.Should().BeEquivalentTo("a\r\nb\r\nc\r\nd\r\n");
        }

        [TestMethod]
        public void CopySectionFromMarkdown_CopySuccessful2_Successful()
        {
            string text = @"# Summary
|  | Working Set (MB)|Private Memory (MB)|Requests/sec
|--- | ---|---|---|
JsonMin_Windows | -7.000000000000001% | -12% | -6%

# Incomplete Tests
";

            string details = MarkdownReportBuilder.CopySectionFromMarkDown(text, "Summary");
            details.Should().BeEquivalentTo("|  | Working Set (MB)|Private Memory (MB)|Requests/sec\r\n|--- | ---|---|---|\r\nJsonMin_Windows | -7.000000000000001% | -12% | -6%\r\n");
        }
    }
}
