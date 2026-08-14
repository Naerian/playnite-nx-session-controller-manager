using System.Collections.Generic;
using Playnite.SDK.Data;

namespace ControllerSessionManager.PlayniteIntegration
{
    public sealed class ControllerThemeApi : ObservableObject
    {
        private int connectedCount;
        private string primaryControllerName;
        private string statusText;

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
    }
}
