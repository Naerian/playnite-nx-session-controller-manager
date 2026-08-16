namespace ControllerSessionManager.PlayniteIntegration
{
    public sealed class ControllerIconOption
    {
        private string geometryData;

        public string Id { get; set; }
        public string Name { get; set; }
        public string FileName { get; set; }

        public string GeometryData
        {
            get
            {
                if (geometryData == null && !string.IsNullOrWhiteSpace(FileName))
                {
                    geometryData = SvgIconGeometryLoader.GetPathData(FileName);
                }

                return geometryData;
            }
        }
    }
}
