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
            var creatorRoot = Path.Combine(pluginRoot, "CreatorThemes");
            CopyDirectory(Path.Combine(root, "bin", "Release", "CreatorThemes"), creatorRoot);
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
            if (!ids.Contains("Aniki") || !ids.Contains("Helium") || !ids.Contains("community.test"))
                throw new Exception("Bundled packs were not discovered.");
            var matchesTheme = catalog.GetMethod("MatchesTheme");
            if (!(bool)matchesTheme.Invoke(null, new object[] { "community.test", "desktop.test", false }) ||
                !(bool)matchesTheme.Invoke(null, new object[] { "community.test", "fullscreen.test", true }) ||
                (bool)matchesTheme.Invoke(null, new object[] { "community.test", "desktop.test", true }))
                throw new Exception("Creator theme ids were not matched by Playnite mode.");
            if (!(bool)matchesTheme.Invoke(null, new object[] { "Aniki", "Aniki_ReMake_bb8728bd-ac83-4324-88b1-ee5c586527d1", true }) ||
                !(bool)matchesTheme.Invoke(null, new object[] { "Helium", "8b15c46a-90c2-4fe5-9ebb-1ab25ba7fcb1", false }))
                throw new Exception("Bundled creator themes were not matched to the installed Playnite themes.");
            catalog.GetMethod("Configure").Invoke(null, new object[] { Path.Combine(root, "obj", "NoCreatorFiles") });
            if (!(bool)matchesTheme.Invoke(null, new object[] { "Aniki", "Aniki_ReMake_bb8728bd-ac83-4324-88b1-ee5c586527d1", true }) ||
                !(bool)matchesTheme.Invoke(null, new object[] { "Helium", "8b15c46a-90c2-4fe5-9ebb-1ab25ba7fcb1", false }))
                throw new Exception("Built-in theme filtering failed without copied creator manifests.");
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
            notificationPresets.GetMethod("Apply").Invoke(null, new[] { settings, "Aniki" });
            if (!(bool)settingsType.GetProperty("NotificationUseStateBackgroundColors")
                    .GetValue(settings, null) ||
                !(bool)settingsType.GetProperty("NotificationUseStateBorderColors")
                    .GetValue(settings, null) ||
                (string)settingsType.GetProperty("NotificationConnectedBorderColor")
                    .GetValue(settings, null) != "#FFD6B16F" ||
                (string)settingsType.GetProperty("NotificationFontFamily").GetValue(settings, null) != "Exo 2")
                throw new Exception("Aniki notification pack was not applied from disk.");
            notificationPresets.GetMethod("Apply").Invoke(null, new[] { settings, "community.test" });
            if ((string)settingsType.GetProperty("NotificationTextOrder").GetValue(settings, null) !=
                "MessageFirst" || (int)settingsType.GetProperty("NotificationBorderLeftThickness")
                    .GetValue(settings, null) != 9 || !((string)settingsType.GetProperty(
                    "NotificationTitleFontFamily").GetValue(settings, null)).StartsWith("ExternalFont|"))
                throw new Exception("Community notification pack was not applied dynamically.");

            var overlayPresets = assembly.GetType(
                "ControllerSessionManager.PlayniteIntegration.OverlayStylePresets", true);
            overlayPresets.GetMethod("Apply").Invoke(null, new[] { settings, "Helium" });
            if (!(bool)settingsType.GetProperty("OverlayUseIndependentBorders").GetValue(settings, null) ||
                (string)settingsType.GetProperty("OverlayBlockOrder").GetValue(settings, null) !=
                    "Title,Instruction,Controller,Metadata,Status")
                throw new Exception("Helium overlay pack was not applied from disk.");

            Console.WriteLine("Creator pack discovery and application passed.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
        foreach (var directory in Directory.GetDirectories(source))
            CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
    }
}
