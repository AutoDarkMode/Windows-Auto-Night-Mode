namespace AutoDarkModeApp.Contracts.Services;

public interface IGeolocatorService
{
    Task<string?> GetRegionNameAsync(double longitude, double latitude);
}
