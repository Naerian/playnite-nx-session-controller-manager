using System.Collections.Generic;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace ControllerSessionManager.PlayniteIntegration
{
    public sealed class ControllerSessionManagerSettings : ObservableObject, ISettings
    {
        private ControllerSessionManagerPlugin plugin;
        private ControllerSessionManagerSettings editingClone;
        private bool enableMonitoring = true;
        private bool enableDebugLogging;
        private int reconciliationIntervalSeconds = 5;

        public ControllerSessionManagerSettings()
        {
        }

        internal ControllerSessionManagerSettings(ControllerSessionManagerPlugin sourcePlugin)
        {
            plugin = sourcePlugin;
            var saved = sourcePlugin.LoadPluginSettings<ControllerSessionManagerSettings>();
            if (saved != null)
            {
                CopyFrom(saved);
            }
        }

        public bool EnableMonitoring
        {
            get { return enableMonitoring; }
            set { SetValue(ref enableMonitoring, value); }
        }

        public bool EnableDebugLogging
        {
            get { return enableDebugLogging; }
            set { SetValue(ref enableDebugLogging, value); }
        }

        public int ReconciliationIntervalSeconds
        {
            get { return reconciliationIntervalSeconds; }
            set { SetValue(ref reconciliationIntervalSeconds, value); }
        }

        internal void Attach(ControllerSessionManagerPlugin sourcePlugin)
        {
            plugin = sourcePlugin;
        }

        public void BeginEdit()
        {
            editingClone = Clone();
            if (plugin != null)
            {
                plugin.RefreshControllers();
            }
        }

        public void CancelEdit()
        {
            if (editingClone != null)
            {
                CopyFrom(editingClone);
            }
        }

        public void EndEdit()
        {
            if (plugin != null)
            {
                plugin.SavePluginSettings(this);
                plugin.ApplySettings();
            }
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            if (ReconciliationIntervalSeconds < 2 || ReconciliationIntervalSeconds > 60)
            {
                errors.Add(plugin == null
                    ? "The reconciliation interval must be between 2 and 60 seconds."
                    : plugin.Loc("LOCCSM_ValidationInterval"));
            }

            return errors.Count == 0;
        }

        private ControllerSessionManagerSettings Clone()
        {
            return new ControllerSessionManagerSettings
            {
                EnableMonitoring = EnableMonitoring,
                EnableDebugLogging = EnableDebugLogging,
                ReconciliationIntervalSeconds = ReconciliationIntervalSeconds
            };
        }

        private void CopyFrom(ControllerSessionManagerSettings source)
        {
            EnableMonitoring = source.EnableMonitoring;
            EnableDebugLogging = source.EnableDebugLogging;
            ReconciliationIntervalSeconds = source.ReconciliationIntervalSeconds;
        }
    }
}

