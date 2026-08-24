using System.Windows.Media;

namespace ControllerSessionManager.Tester.Models
{
    public sealed class ControllerVisualSchemeOption
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public Geometry PrimaryGeometry { get; set; }
        public Geometry SecondaryGeometry { get; set; }
        public double ThumbnailScale { get; set; }
    }
}
