using System.Collections.Generic;
using System.Windows.Media;
using Playnite.SDK.Data;

namespace ControllerSessionManager.PlayniteIntegration
{
    /// <summary>
    /// Stable theme surface exposed via PluginSettings (SourceName ControllerSessionManager,
    /// SettingsRoot Theme). Theme authors compose freely; ContentControls are optional shortcuts.
    /// </summary>
    public sealed class ControllerThemeApi : ObservableObject
    {
        private int connectedCount;
        private string primaryControllerName;
        private string statusText;
        private string primaryControllerIconGeometry;
        private string topPanelIconGeometry;
        private string defaultIconGeometry;
        private string primaryControllerBatteryLabel;
        private string primaryControllerBatteryLevel;
        private string primaryControllerTooltip;
        private string topPanelControllerMode;
        private Brush primaryControllerBatteryBrush;
        private Brush primaryControllerIconBrush;
        private bool hasPrimaryControllerBattery;
        private bool usePrimaryControllerBatteryColor;
        private bool colorIconByBattery;
        private bool isTopPanelButtonVisible;

        public int ThemeApiVersion
        {
            get { return 1; }
        }

        public int ConnectedCount
        {
            get { return connectedCount; }
            private set { SetValue(ref connectedCount, value); }
        }

        public bool HasConnectedControllers
        {
            get { return ConnectedCount > 0; }
        }

        public string PrimaryControllerName
        {
            get { return primaryControllerName; }
            private set { SetValue(ref primaryControllerName, value); }
        }

        public string StatusText
        {
            get { return statusText; }
            private set { SetValue(ref statusText, value); }
        }

        /// <summary>Chosen profile icon of the primary controller (path geometry string).</summary>
        public string PrimaryControllerIconGeometry
        {
            get { return primaryControllerIconGeometry; }
            private set { SetValue(ref primaryControllerIconGeometry, value); }
        }

        /// <summary>
        /// Icon geometry that mirrors Desktop top-panel mode (Default pack icon vs primary profile).
        /// </summary>
        public string TopPanelIconGeometry
        {
            get { return topPanelIconGeometry; }
            private set { SetValue(ref topPanelIconGeometry, value); }
        }

        /// <summary>Fixed pack icon (gamepad-tester) for themes that do not want the profile silhouette.</summary>
        public string DefaultIconGeometry
        {
            get { return defaultIconGeometry; }
            private set { SetValue(ref defaultIconGeometry, value); }
        }

        public string PrimaryControllerBatteryLabel
        {
            get { return primaryControllerBatteryLabel; }
            private set { SetValue(ref primaryControllerBatteryLabel, value); }
        }

        /// <summary>Raw level key: Empty, Low, Medium, Full (empty when unknown).</summary>
        public string PrimaryControllerBatteryLevel
        {
            get { return primaryControllerBatteryLevel; }
            private set { SetValue(ref primaryControllerBatteryLevel, value); }
        }

        /// <summary>Always the level color when battery is known; use for dots / always-colored text.</summary>
        public Brush PrimaryControllerBatteryBrush
        {
            get { return primaryControllerBatteryBrush; }
            private set { SetValue(ref primaryControllerBatteryBrush, value); }
        }

        /// <summary>
        /// Brush for the controller icon after applying ColorTopPanelIndicatorByBattery.
        /// Null means the theme should keep its normal foreground (TargetNullValue).
        /// </summary>
        public Brush PrimaryControllerIconBrush
        {
            get { return primaryControllerIconBrush; }
            private set { SetValue(ref primaryControllerIconBrush, value); }
        }

        public bool HasPrimaryControllerBattery
        {
            get { return hasPrimaryControllerBattery; }
            private set { SetValue(ref hasPrimaryControllerBattery, value); }
        }

        public string PrimaryControllerTooltip
        {
            get { return primaryControllerTooltip; }
            private set { SetValue(ref primaryControllerTooltip, value); }
        }

        /// <summary>True when battery is known and the user enabled color-by-battery in settings.</summary>
        public bool UsePrimaryControllerBatteryColor
        {
            get { return usePrimaryControllerBatteryColor; }
            private set { SetValue(ref usePrimaryControllerBatteryColor, value); }
        }

        /// <summary>Mirrors ColorTopPanelIndicatorByBattery from plugin settings.</summary>
        public bool ColorIconByBattery
        {
            get { return colorIconByBattery; }
            private set { SetValue(ref colorIconByBattery, value); }
        }

        /// <summary>Hidden / Default / Primary — mirrors TopPanelControllerMode.</summary>
        public string TopPanelControllerMode
        {
            get { return topPanelControllerMode; }
            private set { SetValue(ref topPanelControllerMode, value); }
        }

        public bool IsTopPanelButtonVisible
        {
            get { return isTopPanelButtonVisible; }
            private set { SetValue(ref isTopPanelButtonVisible, value); }
        }

        internal void Update(int count, string primaryName, string text)
        {
            var hadControllers = HasConnectedControllers;
            ConnectedCount = count;
            PrimaryControllerName = primaryName;
            StatusText = text;
            if (hadControllers != HasConnectedControllers)
            {
                OnPropertyChanged("HasConnectedControllers");
            }
        }

        internal void UpdateSettingsMirrors(string topPanelMode, bool colorByBattery, bool topPanelVisible,
            string defaultIconGeometry)
        {
            TopPanelControllerMode = topPanelMode ?? ControllerSessionManagerSettings.TopPanelControllerModeHidden;
            ColorIconByBattery = colorByBattery;
            IsTopPanelButtonVisible = topPanelVisible;
            DefaultIconGeometry = defaultIconGeometry ?? string.Empty;
        }

        internal void UpdatePrimaryPresentation(string iconGeometry, string topPanelIconGeometry,
            string batteryLabel, string batteryLevel, Brush batteryBrush, bool hasBattery, bool useBatteryColor)
        {
            PrimaryControllerIconGeometry = iconGeometry;
            TopPanelIconGeometry = topPanelIconGeometry;
            PrimaryControllerBatteryLabel = batteryLabel;
            PrimaryControllerBatteryLevel = batteryLevel ?? string.Empty;
            PrimaryControllerBatteryBrush = batteryBrush;
            HasPrimaryControllerBattery = hasBattery;
            UsePrimaryControllerBatteryColor = hasBattery && useBatteryColor;
            PrimaryControllerIconBrush = UsePrimaryControllerBatteryColor ? batteryBrush : null;
            PrimaryControllerTooltip = hasBattery && !string.IsNullOrWhiteSpace(batteryLabel)
                ? string.Format("{0}: {1}", PrimaryControllerName, batteryLabel)
                : PrimaryControllerName;
        }
    }
}
