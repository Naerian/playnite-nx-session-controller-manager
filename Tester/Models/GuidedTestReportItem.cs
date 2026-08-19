namespace ControllerSessionManager.Tester.Models
{
    public sealed class GuidedTestReportItem
    {
        public string Label { get; set; }
        public bool IsPassed { get; set; }

        public string Glyph
        {
            get { return IsPassed ? "\uE73E" : "\uE711"; }
        }
    }
}
