namespace ControllerSessionManager.Overlay
{
    /// <summary>
    /// Named-pipe toast and overlay commands carry Gamepads silhouette path data as base64.
    /// Keep enough headroom for two controller geometries plus presentation metadata in a
    /// disconnect overlay, including custom or future silhouettes that may be more detailed.
    /// </summary>
    internal static class OverlayIpcLimits
    {
        public const int MaxLineCharacters = 262144;
        public const int PipeBufferBytes = 65536;
    }
}
