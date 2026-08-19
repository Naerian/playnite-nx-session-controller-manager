namespace ControllerSessionManager.PlayniteIntegration
{
    internal static class ControllerConnectionIcons
    {
        public static string GetPathData(string connectionType)
        {
            switch (connectionType)
            {
                case "Wired":
                    return SvgIconGeometryLoader.GetPathData("usb.svg");
                case "Bluetooth":
                    return SvgIconGeometryLoader.GetPathData("bluetooth.svg");
                case "Wireless":
                case "WirelessReceiver":
                    return SvgIconGeometryLoader.GetPathData("wifi.svg");
                default:
                    return SvgIconGeometryLoader.GetPathData("help-circle.svg");
            }
        }
    }
}
