namespace ControllerSessionManager.Tester.Models
{
    public sealed class ControllerVisualSchemeDefinition
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public double TestWidth { get; set; }
        public double TestHeight { get; set; }
        public double GuidedWidth { get; set; }
        public double GuidedHeight { get; set; }
        public string SvgFileName { get; set; }
        public string InteractiveLayoutKey { get; set; }
        public double ThumbnailScale { get; set; }

        public ControllerVisualSchemeOption ToOption()
        {
            var geometry = TesterSvgGeometryLoader.Load(SvgFileName);
            return new ControllerVisualSchemeOption
            {
                Key = Key,
                DisplayName = DisplayName,
                PrimaryGeometry = geometry.Primary,
                SecondaryGeometry = geometry.Secondary,
                ThumbnailScale = ThumbnailScale
            };
        }
    }
}
