using System;
using System.IO;
using System.Reflection;

internal static class DisplayResolutionScaleSmoke
{
    private static int Main(string[] args)
    {
        try
        {
            var root = Path.GetFullPath(args[0]);
            var assembly = Assembly.LoadFrom(Path.Combine(root, "bin", "Release",
                "ControllerSessionManager.OverlayHost.exe"));
            var type = assembly.GetType(
                "ControllerSessionManager.OverlayHost.DisplayResolutionScale", true);
            var fromPhysical = type.GetMethod("FromPhysicalHeight",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            AssertNear(Invoke(fromPhysical, 1080, 1.0), 1.0, "1080p @ 100% DPI");
            AssertNear(Invoke(fromPhysical, 2160, 1.0), 2.0, "4K @ 100% DPI");
            AssertNear(Invoke(fromPhysical, 2160, 2.0), 1.0, "4K @ 200% DPI");
            AssertNear(Invoke(fromPhysical, 1440, 1.0), 1440.0 / 1080.0, "1440p @ 100% DPI");
            AssertNear(Invoke(fromPhysical, 720, 1.0), 0.85, "720p clamps to minimum");
            AssertNear(Invoke(fromPhysical, 4320, 1.0), 2.5, "8K clamps to maximum");
            AssertNear(Invoke(fromPhysical, 0, 1.0), 1.0, "zero height falls back to 1");
            Console.WriteLine("DisplayResolutionScale smoke passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static double Invoke(MethodInfo method, int height, double dpi)
    {
        return (double)method.Invoke(null, new object[] { height, dpi });
    }

    private static void AssertNear(double actual, double expected, string label)
    {
        if (Math.Abs(actual - expected) > 0.001)
        {
            throw new Exception(string.Format("{0}: expected {1}, got {2}", label, expected, actual));
        }
    }
}
