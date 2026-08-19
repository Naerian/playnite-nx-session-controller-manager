namespace ControllerSessionManager.Overlay
{
    /// <summary>
    /// Named-pipe toast and overlay commands carry Gamepads silhouette path data as base64.
    /// Default.svg alone is ~17 KB encoded; DualSense exceeds 26 KB. The old 16 KB line cap
    /// dropped every connect/disconnect toast and preview after the Lucide icons were replaced.
    /// </summary>
    internal static class OverlayIpcLimits
    {
        public const int MaxLineCharacters = 262144;
        public const int PipeBufferBytes = 65536;
    }
}
