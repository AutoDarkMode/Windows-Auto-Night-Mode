namespace AutoDarkModeApp.Contracts.Services;

public interface IGeolocatorService
{
    event EventHandler? LocationStatusChanged;

    Task<string?> GetRegionNameAsync(double longitude, double latitude);
}
