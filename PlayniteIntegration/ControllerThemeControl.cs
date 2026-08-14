using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Playnite.SDK.Controls;

namespace ControllerSessionManager.PlayniteIntegration
{
    public sealed class ControllerThemeControl : PluginUserControl
    {
        public ControllerThemeControl(ControllerThemeApi api, string elementName)
        {
            DataContext = api;
            var text = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var path = "StatusText";
            if (elementName == "ControllerCount")
            {
                path = "ConnectedCount";
            }
            else if (elementName == "PrimaryController")
            {
                path = "PrimaryControllerName";
            }

            text.SetBinding(TextBlock.TextProperty, new Binding(path));
            Content = text;
        }
    }
}

