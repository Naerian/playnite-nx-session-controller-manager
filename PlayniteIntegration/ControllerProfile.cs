using System.Collections.Generic;

namespace ControllerSessionManager.PlayniteIntegration
{
    public sealed class ControllerProfile : ObservableObject
    {
        private string detectedName;
        private string customName;
        private string iconId;
        private string iconColor;

        public string HardwareId { get; set; }

        // Persisted in Desktop, where SDL enrichment is isolated from the Fullscreen process.
        // Fullscreen can then recover the friendly identity without making any SDL call.
        public int? LastKnownXInputSlot { get; set; }

        public string DetectedName
        {
            get { return detectedName; }
            set { SetValue(ref detectedName, value); }
        }

        public string CustomName
        {
            get { return customName; }
            set { SetValue(ref customName, value); }
        }

        public string IconId
        {
            get { return iconId; }
            set { SetValue(ref iconId, value); }
        }

        /// <summary>
        /// Optional silhouette tint (#AARRGGBB). Empty inherits appearance / theme defaults.
        /// </summary>
        public string IconColor
        {
            get { return iconColor; }
            set { SetValue(ref iconColor, value); }
        }
    }
}
