using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

internal static class CreatorThemeUpdaterSmoke
{
    private static int Main(string[] args)
    {
        try
        {
            var root = Path.GetFullPath(args[0]);
            Assembly.LoadFrom(@"C:\Playnite\Playnite.SDK.dll");
            var assembly = Assembly.LoadFrom(Path.Combine(root, "bin", "Release",
                "ControllerSessionManager.dll"));
            var extension = File.ReadAllText(Path.Combine(root, "extension.yaml"));
            var manifestVersionMatch = Regex.Match(extension, @"(?m)^Version:\s*([^\s]+)\s*$");
            Version manifestVersion;
            if (!manifestVersionMatch.Success ||
                !Version.TryParse(manifestVersionMatch.Groups[1].Value, out manifestVersion) ||
                assembly.GetName().Version == null ||
                assembly.GetName().Version.ToString(3) != manifestVersion.ToString(3))
                throw new Exception("Assembly and extension manifest versions must match.");
            var updaterType = assembly.GetType(
                "ControllerSessionManager.PlayniteIntegration.CreatorThemeUpdater", true);
            var cache = Path.Combine(root, "obj", "CreatorThemeUpdaterSmoke", "CreatorThemes");
            if (Directory.Exists(Path.GetDirectoryName(cache)))
                Directory.Delete(Path.GetDirectoryName(cache), true);
            var constructor = updaterType.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(string), typeof(Version), typeof(string) }, null);
            if (constructor == null) throw new Exception("The test updater constructor was not found.");
            var catalogUrl = (string)updaterType.GetField("DefaultCatalogUrl",
                BindingFlags.Public | BindingFlags.Static).GetRawConstantValue();
            var updater = constructor.Invoke(new object[] { cache, assembly.GetName().Version, catalogUrl });
            var task = (System.Threading.Tasks.Task)updaterType.GetMethod("CheckForUpdatesAsync")
                .Invoke(updater, new object[] { CancellationToken.None });
            task.GetAwaiter().GetResult();
            var result = task.GetType().GetProperty("Result").GetValue(task, null);
            var resultType = result.GetType();
            if (!(bool)resultType.GetProperty("Succeeded").GetValue(result, null))
                throw new Exception((string)resultType.GetProperty("Error").GetValue(result, null));
            if (!File.Exists(Path.Combine(root, "obj", "CreatorThemeUpdaterSmoke",
                    "CreatorThemes.catalog.json")))
                throw new Exception("The verified remote catalog was not cached.");
            var installed = (int)resultType.GetProperty("Installed").GetValue(result, null);
            var manifest = Path.Combine(cache, "naerian.narianux", "manifest.json");
            if (installed != 1 || !File.Exists(manifest))
                throw new Exception("The compatible NarianUX package was not installed.");
            var secondTask = (System.Threading.Tasks.Task)updaterType.GetMethod("CheckForUpdatesAsync")
                .Invoke(updater, new object[] { CancellationToken.None });
            secondTask.GetAwaiter().GetResult();
            var secondResult = secondTask.GetType().GetProperty("Result").GetValue(secondTask, null);
            var secondType = secondResult.GetType();
            if (!(bool)secondType.GetProperty("Succeeded").GetValue(secondResult, null) ||
                !(bool)secondType.GetProperty("CatalogCurrent").GetValue(secondResult, null) ||
                (int)secondType.GetProperty("Installed").GetValue(secondResult, null) != 0 ||
                (int)secondType.GetProperty("Updated").GetValue(secondResult, null) != 0)
                throw new Exception("A second catalog check must keep the installed design unchanged.");
            Console.WriteLine("Remote creator-theme catalog and NarianUX installation passed.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
