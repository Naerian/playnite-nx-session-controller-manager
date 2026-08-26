using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

internal static class ImportedVisualProfileCatalogSmoke
{
    private static string root;

    [STAThread]
    private static int Main(string[] args)
    {
        root = args.Length > 0 ? Path.GetFullPath(args[0]) : Environment.CurrentDirectory;
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        try
        {
            Run();
            Console.WriteLine("Imported visual profile catalog tests passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name).Name;
        if (string.Equals(name, "ControllerSessionManager", StringComparison.OrdinalIgnoreCase))
            return Assembly.LoadFrom(Path.Combine(root, "bin", "Release", "ControllerSessionManager.dll"));
        if (string.Equals(name, "Playnite.SDK", StringComparison.OrdinalIgnoreCase))
            return Assembly.LoadFrom(@"C:\Playnite\Playnite.SDK.dll");
        return null;
    }

    private static void Run()
    {
        var assembly = Assembly.LoadFrom(Path.Combine(root, "bin", "Release", "ControllerSessionManager.dll"));
        var snapshotType = assembly.GetType(
            "ControllerSessionManager.PlayniteIntegration.VisualProfileSnapshot", true);
        var catalogType = assembly.GetType(
            "ControllerSessionManager.PlayniteIntegration.ImportedVisualProfileCatalog", true);
        var notificationPresets = assembly.GetType(
            "ControllerSessionManager.PlayniteIntegration.NotificationStylePresets", true);
        var overlayPresets = assembly.GetType(
            "ControllerSessionManager.PlayniteIntegration.OverlayStylePresets", true);

        var testRoot = Path.Combine(root, "obj", "ImportedVisualProfileCatalogSmoke");
        if (Directory.Exists(testRoot)) Directory.Delete(testRoot, true);
        Directory.CreateDirectory(testRoot);
        var source = Path.Combine(testRoot, "source.json");
        var snapshot = Activator.CreateInstance(snapshotType);
        snapshotType.GetProperty("Version").SetValue(snapshot, 12, null);
        snapshotType.GetProperty("Name").SetValue(snapshot, "Imported smoke design", null);
        snapshotType.GetProperty("NotificationWidth").SetValue(snapshot, 731, null);
        snapshotType.GetProperty("OverlayScalePercent").SetValue(snapshot, 115, null);
        File.WriteAllText(source, new JavaScriptSerializer().Serialize(snapshot), new UTF8Encoding(false));

        catalogType.GetMethod("Configure").Invoke(null, new object[] { testRoot });
        var importedId = (string)catalogType.GetMethod("Import").Invoke(null, new object[] { source });
        Assert(importedId.StartsWith("Imported:", StringComparison.Ordinal), "Unexpected imported id.");
        Assert((bool)catalogType.GetMethod("Contains").Invoke(null, new object[] { importedId }),
            "Imported profile was not registered.");
        Assert((string)catalogType.GetMethod("GetName").Invoke(null, new object[] { importedId }) ==
            "Imported smoke design", "Imported profile name was not retained.");
        Assert(((string[])catalogType.GetMethod("GetIds").Invoke(null, null)).Length == 1,
            "Imported catalog did not expose exactly one profile.");
        Assert((string)notificationPresets.GetMethod("Normalize").Invoke(null, new object[] { importedId }) == importedId,
            "Notification presets rejected an imported profile id.");
        Assert((string)overlayPresets.GetMethod("Normalize").Invoke(null, new object[] { importedId }) == importedId,
            "Overlay presets rejected an imported profile id.");

        snapshotType.GetProperty("NotificationWidth").SetValue(snapshot, 812, null);
        File.WriteAllText(source, new JavaScriptSerializer().Serialize(snapshot), new UTF8Encoding(false));
        var overwrittenId = (string)catalogType.GetMethod("Import").Invoke(null, new object[] { source });
        Assert(overwrittenId == importedId &&
            ((string[])catalogType.GetMethod("GetIds").Invoke(null, null)).Length == 1,
            "Importing the same visual-profile name did not overwrite the existing design.");

        catalogType.GetMethod("Reload").Invoke(null, null);
        Assert((bool)catalogType.GetMethod("Contains").Invoke(null, new object[] { importedId }),
            "Imported profile did not survive catalog reload.");
        Assert((bool)catalogType.GetMethod("Delete").Invoke(null, new object[] { importedId }),
            "Imported profile could not be deleted.");
        Assert(!(bool)catalogType.GetMethod("Contains").Invoke(null, new object[] { importedId }),
            "Deleted profile remained in the catalog.");
        Assert(Directory.GetFiles(Path.Combine(testRoot, "VisualProfiles"), "*.pcvisual").Length == 0,
            "Deleting the imported profile left its persistent file behind.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
