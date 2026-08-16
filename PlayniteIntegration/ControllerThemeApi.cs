using System.Collections.Generic;
using System.Windows.Media;
using Playnite.SDK.Data;

namespace ControllerSessionManager.PlayniteIntegration
{
    public sealed class ControllerThemeApi : ObservableObject
    {
        private int connectedCount;
        private string primaryControllerName;
        private string statusText;
        private string primaryControllerIconGeometry;
        private string primaryControllerBatteryLabel;
        private string primaryControllerTooltip;
        private Brush primaryControllerBatteryBrush;
        private bool hasPrimaryControllerBattery;
        private bool usePrimaryControllerBatteryColor;

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

        public string PrimaryControllerIconGeometry
        {
            get { return primaryControllerIconGeometry; }
            private set { SetValue(ref primaryControllerIconGeometry, value); }
        }

        public string PrimaryControllerBatteryLabel
        {
            get { return primaryControllerBatteryLabel; }
            private set { SetValue(ref primaryControllerBatteryLabel, value); }
        }

        public Brush PrimaryControllerBatteryBrush
        {
            get { return primaryControllerBatteryBrush; }
            private set { SetValue(ref primaryControllerBatteryBrush, value); }
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

        public bool UsePrimaryControllerBatteryColor
        {
            get { return usePrimaryControllerBatteryColor; }
            private set { SetValue(ref usePrimaryControllerBatteryColor, value); }
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

        internal void UpdatePrimaryPresentation(string iconGeometry, string batteryLabel,
            Brush batteryBrush, bool hasBattery, bool useBatteryColor)
        {
            PrimaryControllerIconGeometry = iconGeometry;
            PrimaryControllerBatteryLabel = batteryLabel;
            PrimaryControllerBatteryBrush = batteryBrush;
            HasPrimaryControllerBattery = hasBattery;
            UsePrimaryControllerBatteryColor = hasBattery && useBatteryColor;
            PrimaryControllerTooltip = hasBattery && !string.IsNullOrWhiteSpace(batteryLabel)
                ? string.Format("{0}: {1}", PrimaryControllerName, batteryLabel)
                : PrimaryControllerName;
        }
    }
}
