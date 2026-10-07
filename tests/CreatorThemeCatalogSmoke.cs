using System;
using System.IO;
using System.Reflection;

internal static class CreatorThemeCatalogSmoke
{
    private static int Main(string[] args)
    {
        try
        {
            var root = Path.GetFullPath(args[0]);
            Assembly.LoadFrom(@"C:\Playnite\Playnite.SDK.dll");
            var assembly = Assembly.LoadFrom(Path.Combine(root, "bin", "Release",
                "ControllerSessionManager.dll"));
            var pluginRoot = Path.Combine(root, "obj", "CreatorPackPlugin");
            if (Directory.Exists(pluginRoot)) Directory.Delete(pluginRoot, true);
            var community = Path.Combine(pluginRoot, "CreatorThemes", "CommunityTest");
            Directory.CreateDirectory(Path.Combine(community, "Fonts"));
            Directory.CreateDirectory(Path.Combine(community, "Audio"));
            File.WriteAllBytes(Path.Combine(community, "Audio", "connected.wav"), new byte[] { 1, 2, 3 });
            File.WriteAllText(Path.Combine(community, "manifest.json"),
                "{\"Id\":\"community.test\",\"Name\":\"Community Test\",\"Author\":\"Test Author\",\"Version\":\"1.0.0\",\"DesktopThemeIds\":[\"desktop.test\"],\"FullscreenThemeIds\":[\"fullscreen.test\"],\"Fonts\":[{\"Id\":\"Main\",\"Name\":\"Community Font\",\"Family\":\"Community Font\",\"Folder\":\"Fonts\"}],\"Sounds\":{\"Connected\":\"Audio/connected.wav\"}}");
            File.WriteAllText(Path.Combine(community, "notification.json"),
                "{\"NotificationTextOrder\":\"MessageFirst\",\"NotificationBorderLeftThickness\":9,\"NotificationTitleFontFamily\":\"$font:Main\"}");
            var catalog = assembly.GetType(
                "ControllerSessionManager.PlayniteIntegration.CreatorThemeCatalog", true);
            var configureWithData = catalog.GetMethod("Configure", new[] { typeof(string), typeof(string) });
            var userData = Path.Combine(root, "obj", "CreatorPackData");
            if (Directory.Exists(userData)) Directory.Delete(userData, true);
            var downloaded = Path.Combine(userData, "CreatorThemes", "DownloadedTest");
            Directory.CreateDirectory(downloaded);
            File.WriteAllText(Path.Combine(downloaded, "manifest.json"),
                "{\"SchemaVersion\":1,\"Id\":\"downloaded.test\",\"Name\":\"Downloaded Test\",\"Author\":\"Test Author\",\"Version\":\"1.0.0\",\"MinimumPluginVersion\":\"1.0.0\"}");
            File.WriteAllText(Path.Combine(downloaded, "overlay.json"),
                "{\"OverlayScalePercent\":111}");
            configureWithData.Invoke(null, new object[] { pluginRoot, userData });
            var getIds = catalog.GetMethod("GetPresetIds");
            if (((string[])getIds.Invoke(null, new object[] { "notification" })).Length != 0 ||
                ((string[])getIds.Invoke(null, new object[] { "overlay" })).Length != 0)
                throw new Exception("Installed catalog folders must no longer appear as creator designs.");

            var loader = assembly.GetType(
                "ControllerSessionManager.PlayniteIntegration.CreatorThemePackLoader", true);
            var definition = LoadPack(loader, community);
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var definitions = catalog.GetField("Definitions", flags).GetValue(null);
            definitions.GetType().GetMethod("Add").Invoke(definitions, new[] { "community.test", definition });
            var settingsType = assembly.GetType(
                "ControllerSessionManager.PlayniteIntegration.ControllerSessionManagerSettings", true);
            var settings = Activator.CreateInstance(settingsType);
            var notificationPresets = assembly.GetType(
                "ControllerSessionManager.PlayniteIntegration.NotificationStylePresets", true);
            notificationPresets.GetMethod("Apply").Invoke(null, new[] { settings, "community.test" });
            if ((string)settingsType.GetProperty("NotificationTextOrder").GetValue(settings, null) !=
                "MessageFirst" || (int)settingsType.GetProperty("NotificationBorderLeftThickness")
                    .GetValue(settings, null) != 9 || (string)settingsType.GetProperty(
                    "DesktopNotificationTextOrder").GetValue(settings, null) != "MessageFirst" ||
                !((string)settingsType.GetProperty(
                    "NotificationTitleFontFamily").GetValue(settings, null)).StartsWith("ExternalFont|"))
                throw new Exception("A theme pack notification surface was not applied.");

            File.WriteAllText(Path.Combine(community, "overlay.json"),
                "{\"OverlayUseIndependentBorders\":true,\"OverlayBlockOrder\":\"Title,Instruction,Controller,Metadata,Status\",\"OverlayInstructionColor\":\"\",\"OverlayControllerIconColor\":\"\"}");
            definition = LoadPack(loader, community);
            definitions.GetType().GetMethod("set_Item").Invoke(definitions,
                new[] { "community.test", definition });
            var overlayPresets = assembly.GetType(
                "ControllerSessionManager.PlayniteIntegration.OverlayStylePresets", true);
            overlayPresets.GetMethod("Apply").Invoke(null, new[] { settings, "community.test" });
            if (!(bool)settingsType.GetProperty("OverlayUseIndependentBorders").GetValue(settings, null) ||
                (string)settingsType.GetProperty("OverlayBlockOrder").GetValue(settings, null) !=
                    "Title,Instruction,Controller,Metadata,Status")
                throw new Exception("A theme pack overlay surface was not applied.");
            var instructionColor = (string)settingsType.GetProperty("OverlayInstructionColor")
                .GetValue(settings, null);
            var iconColor = (string)settingsType.GetProperty("OverlayControllerIconColor")
                .GetValue(settings, null);
            if (string.IsNullOrWhiteSpace(instructionColor) || string.IsNullOrWhiteSpace(iconColor))
                throw new Exception("Empty pack colors must not wipe overlay defaults.");

            Console.WriteLine("Catalog folders stay hidden and theme pack application passed.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static object LoadPack(Type loader, string directory)
    {
        var tryLoad = loader.GetMethod("TryLoad");
        var loadArgs = new object[] { directory, null };
        if (!(bool)tryLoad.Invoke(null, loadArgs) || loadArgs[1] == null)
            throw new Exception("The theme pack folder was not loaded.");
        return loadArgs[1];
    }
}
