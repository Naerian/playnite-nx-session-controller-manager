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
            var configureBundled = catalog.GetMethod("Configure", new[] { typeof(string) });
            var configureWithData = catalog.GetMethod("Configure", new[] { typeof(string), typeof(string) });
            configureBundled.Invoke(null, new object[] { pluginRoot });
            var ids = (string[])catalog.GetMethod("GetPresetIds").Invoke(null,
                new object[] { "notification" });
            if (!ids.Contains("community.test") || ids.Length != 1)
                throw new Exception("The test creator pack was not discovered.");
            var matchesTheme = catalog.GetMethod("MatchesTheme");
            if (!(bool)matchesTheme.Invoke(null, new object[] { "community.test", "desktop.test", false }) ||
                !(bool)matchesTheme.Invoke(null, new object[] { "community.test", "fullscreen.test", true }) ||
                (bool)matchesTheme.Invoke(null, new object[] { "community.test", "desktop.test", true }))
                throw new Exception("Creator theme ids were not matched by Playnite mode.");
            configureBundled.Invoke(null, new object[] { Path.Combine(root, "obj", "NoCreatorFiles") });
            if (((string[])catalog.GetMethod("GetPresetIds").Invoke(null,
                    new object[] { "notification" })).Length != 0)
                throw new Exception("Creator packs remained registered after configuring an empty directory.");
            configureBundled.Invoke(null, new object[] { pluginRoot });
            var soundKind = assembly.GetType(
                "ControllerSessionManager.PlayniteIntegration.NotificationSoundKind", true);
            var connected = Enum.Parse(soundKind, "Connected");
            var soundPath = (string)catalog.GetMethod("GetSoundPath").Invoke(null,
                new[] { (object)"community.test", connected });
            if (!File.Exists(soundPath)) throw new Exception("Creator sound was not resolved.");
            if (((string[])catalog.GetMethod("GetCompleteSoundPackIds").Invoke(null, null)).Length != 0)
                throw new Exception("An incomplete creator sound set was exposed as a selectable pack.");
            File.WriteAllBytes(Path.Combine(community, "Audio", "disconnected.wav"), new byte[] { 2 });
            File.WriteAllBytes(Path.Combine(community, "Audio", "low_battery.wav"), new byte[] { 3 });
            File.WriteAllBytes(Path.Combine(community, "Audio", "warning.wav"), new byte[] { 4 });
            File.WriteAllText(Path.Combine(community, "manifest.json"),
                "{\"Id\":\"community.test\",\"Name\":\"Community Test\",\"Author\":\"Test Author\",\"Version\":\"1.0.0\",\"DesktopThemeIds\":[\"desktop.test\"],\"FullscreenThemeIds\":[\"fullscreen.test\"],\"Fonts\":[{\"Id\":\"Main\",\"Name\":\"Community Font\",\"Family\":\"Community Font\",\"Folder\":\"Fonts\"}],\"Sounds\":{\"Connected\":\"Audio/connected.wav\",\"Disconnected\":\"Audio/disconnected.wav\",\"LowBattery\":\"Audio/low_battery.wav\",\"Warning\":\"Audio/warning.wav\"}}");
            catalog.GetMethod("Reload").Invoke(null, null);
            var soundPacks = (string[])catalog.GetMethod("GetCompleteSoundPackIds").Invoke(null, null);
            if (soundPacks.Length != 1 || soundPacks[0] != "creator:community.test" ||
                (string)catalog.GetMethod("GetSoundPackId").Invoke(null,
                    new object[] { "community.test" }) != "creator:community.test")
                throw new Exception("A complete creator sound set was not exposed as a selectable pack.");

            var settingsType = assembly.GetType(
                "ControllerSessionManager.PlayniteIntegration.ControllerSessionManagerSettings", true);
            var settings = Activator.CreateInstance(settingsType);
            settingsType.GetProperty("NotificationSoundPack").SetValue(
                settings, "creator:community.test", null);
            var audioType = assembly.GetType(
                "ControllerSessionManager.PlayniteIntegration.NotificationAudioService", true);
            var audio = Activator.CreateInstance(audioType, new object[] { null, root });
            var resolvedCreator = (string)audioType.GetMethod("ResolvePath",
                new[] { soundKind, settingsType }).Invoke(audio, new[] { connected, settings });
            if (resolvedCreator != soundPath)
                throw new Exception("The selected creator sound pack was not resolved.");
            var customPath = Path.Combine(community, "Audio", "custom.wav");
            File.WriteAllBytes(customPath, new byte[] { 9 });
            settingsType.GetProperty("CustomConnectedSoundPath").SetValue(settings, customPath, null);
            var resolvedCustom = (string)audioType.GetMethod("ResolvePath",
                new[] { soundKind, settingsType }).Invoke(audio, new[] { connected, settings });
            if (resolvedCustom != customPath)
                throw new Exception("A custom sound did not override the selected creator sound pack.");
            ((IDisposable)audio).Dispose();
            settingsType.GetProperty("CustomConnectedSoundPath").SetValue(settings, string.Empty, null);
            var notificationPresets = assembly.GetType(
                "ControllerSessionManager.PlayniteIntegration.NotificationStylePresets", true);
            notificationPresets.GetMethod("Apply").Invoke(null, new[] { settings, "community.test" });
            if ((string)settingsType.GetProperty("NotificationTextOrder").GetValue(settings, null) !=
                "MessageFirst" || (int)settingsType.GetProperty("NotificationBorderLeftThickness")
                    .GetValue(settings, null) != 9 || (string)settingsType.GetProperty(
                    "DesktopNotificationTextOrder").GetValue(settings, null) != "MessageFirst" ||
                !((string)settingsType.GetProperty(
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

            var userData = Path.Combine(root, "obj", "CreatorPackData");
            if (Directory.Exists(userData)) Directory.Delete(userData, true);
            var downloaded = Path.Combine(userData, "CreatorThemes", "DownloadedTest");
            Directory.CreateDirectory(downloaded);
            File.WriteAllText(Path.Combine(downloaded, "manifest.json"),
                "{\"SchemaVersion\":1,\"Id\":\"downloaded.test\",\"Name\":\"Downloaded Test\",\"Author\":\"Test Author\",\"Version\":\"1.0.0\",\"MinimumPluginVersion\":\"1.0.0\"}");
            File.WriteAllText(Path.Combine(downloaded, "overlay.json"),
                "{\"OverlayScalePercent\":111}");
            configureWithData.Invoke(null, new object[] { pluginRoot, userData });
            var overlayIds = (string[])catalog.GetMethod("GetPresetIds").Invoke(null,
                new object[] { "overlay" });
            if (!overlayIds.Contains("downloaded.test") || !overlayIds.Contains("community.test"))
                throw new Exception("Bundled and downloaded creator packs were not merged.");
            var isUserInstalled = catalog.GetMethod("IsUserInstalled");
            if ((bool)isUserInstalled.Invoke(null, new object[] { "downloaded.test" }) ||
                (bool)isUserInstalled.Invoke(null, new object[] { "community.test" }))
                throw new Exception("Catalog and bundled creator packs must not be treated as removable sideloads.");
            catalog.GetMethod("MarkSideloadOrigin").Invoke(null, new object[] { downloaded });
            if (!(bool)isUserInstalled.Invoke(null, new object[] { "downloaded.test" }))
                throw new Exception("A manually installed creator pack was not marked removable.");
            catalog.GetMethod("MarkCatalogOrigin").Invoke(null, new object[] { downloaded });
            if ((bool)isUserInstalled.Invoke(null, new object[] { "downloaded.test" }))
                throw new Exception("An official catalog pack remained removable after a catalog install.");
            catalog.GetMethod("MarkSideloadOrigin").Invoke(null, new object[] { downloaded });
            if (!(bool)matchesTheme.Invoke(null,
                    new object[] { "downloaded.test", "any.desktop.theme", false }) ||
                !(bool)matchesTheme.Invoke(null,
                    new object[] { "downloaded.test", "any.fullscreen.theme", true }))
                throw new Exception("Creator packs without a target theme must remain universal.");

            Console.WriteLine("Creator pack discovery, downloaded cache and application passed.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
