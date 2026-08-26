using System;
using System.IO;
using System.Linq;
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
            var creatorRoot = Path.Combine(pluginRoot, "CreatorThemes");
            Directory.CreateDirectory(creatorRoot);
            var invalid = Path.Combine(creatorRoot, "BrokenPack");
            Directory.CreateDirectory(invalid);
            File.WriteAllText(Path.Combine(invalid, "manifest.json"), "{ definitely not json }");
            var community = Path.Combine(creatorRoot, "CommunityTest");
            Directory.CreateDirectory(community);
            Directory.CreateDirectory(Path.Combine(community, "Fonts"));
            Directory.CreateDirectory(Path.Combine(community, "Audio"));
            File.WriteAllBytes(Path.Combine(community, "Audio", "connected.wav"), new byte[] { 1, 2, 3 });
            File.WriteAllText(Path.Combine(community, "manifest.json"),
                "{\"Id\":\"community.test\",\"Name\":\"Community Test\",\"Author\":\"Test Author\",\"Version\":\"1.0.0\",\"DesktopThemeIds\":[\"desktop.test\"],\"FullscreenThemeIds\":[\"fullscreen.test\"],\"Fonts\":[{\"Id\":\"Main\",\"Name\":\"Community Font\",\"Family\":\"Community Font\",\"Folder\":\"Fonts\"}],\"Sounds\":{\"Connected\":\"Audio/connected.wav\"}}");
            File.WriteAllText(Path.Combine(community, "notification.json"),
                "{\"NotificationTextOrder\":\"MessageFirst\",\"NotificationBorderLeftThickness\":9,\"NotificationTitleFontFamily\":\"$font:Main\"}");
            var catalog = assembly.GetType(
                "ControllerSessionManager.PlayniteIntegration.CreatorThemeCatalog", true);
            catalog.GetMethod("Configure").Invoke(null, new object[] { pluginRoot });
            var ids = (string[])catalog.GetMethod("GetPresetIds").Invoke(null,
                new object[] { "notification" });
            if (!ids.Contains("community.test") || ids.Length != 1)
                throw new Exception("The test creator pack was not discovered.");
            var matchesTheme = catalog.GetMethod("MatchesTheme");
            if (!(bool)matchesTheme.Invoke(null, new object[] { "community.test", "desktop.test", false }) ||
                !(bool)matchesTheme.Invoke(null, new object[] { "community.test", "fullscreen.test", true }) ||
                (bool)matchesTheme.Invoke(null, new object[] { "community.test", "desktop.test", true }))
                throw new Exception("Creator theme ids were not matched by Playnite mode.");
            catalog.GetMethod("Configure").Invoke(null, new object[] { Path.Combine(root, "obj", "NoCreatorFiles") });
            if (((string[])catalog.GetMethod("GetPresetIds").Invoke(null,
                    new object[] { "notification" })).Length != 0)
                throw new Exception("Creator packs remained registered after configuring an empty directory.");
            catalog.GetMethod("Configure").Invoke(null, new object[] { pluginRoot });
            var soundKind = assembly.GetType(
                "ControllerSessionManager.PlayniteIntegration.NotificationSoundKind", true);
            var connected = Enum.Parse(soundKind, "Connected");
            var soundPath = (string)catalog.GetMethod("GetSoundPath").Invoke(null,
                new[] { (object)"community.test", connected });
            if (!File.Exists(soundPath)) throw new Exception("Creator sound was not resolved.");

            var settingsType = assembly.GetType(
                "ControllerSessionManager.PlayniteIntegration.ControllerSessionManagerSettings", true);
            var settings = Activator.CreateInstance(settingsType);
            var notificationPresets = assembly.GetType(
                "ControllerSessionManager.PlayniteIntegration.NotificationStylePresets", true);
            notificationPresets.GetMethod("Apply").Invoke(null, new[] { settings, "community.test" });
            if ((string)settingsType.GetProperty("NotificationTextOrder").GetValue(settings, null) !=
                "MessageFirst" || (int)settingsType.GetProperty("NotificationBorderLeftThickness")
                    .GetValue(settings, null) != 9 || !((string)settingsType.GetProperty(
                    "NotificationTitleFontFamily").GetValue(settings, null)).StartsWith("ExternalFont|"))
                throw new Exception("Community notification pack was not applied dynamically.");

            var overlayPresets = assembly.GetType(
                "ControllerSessionManager.PlayniteIntegration.OverlayStylePresets", true);
            File.WriteAllText(Path.Combine(community, "overlay.json"),
                "{\"OverlayUseIndependentBorders\":true,\"OverlayBlockOrder\":\"Title,Instruction,Controller,Metadata,Status\"}");
            catalog.GetMethod("Reload").Invoke(null, null);
            overlayPresets.GetMethod("Apply").Invoke(null, new[] { settings, "community.test" });
            if (!(bool)settingsType.GetProperty("OverlayUseIndependentBorders").GetValue(settings, null) ||
                (string)settingsType.GetProperty("OverlayBlockOrder").GetValue(settings, null) !=
                    "Title,Instruction,Controller,Metadata,Status")
                throw new Exception("The community overlay pack was not applied dynamically.");

            Console.WriteLine("Creator pack discovery and application passed.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
