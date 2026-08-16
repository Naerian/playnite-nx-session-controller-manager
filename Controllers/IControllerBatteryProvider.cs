namespace ControllerSessionManager.Controllers
{
    public interface IControllerBatteryProvider
    {
        string Id { get; }
        bool Supports(ControllerMetadata controller);
        bool TryGetBatteryLevel(ControllerMetadata controller, out string level);
    }
}
