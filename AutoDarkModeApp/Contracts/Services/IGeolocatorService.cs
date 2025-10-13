using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoDarkModeApp.Contracts.Services;
public interface IGeolocatorService
{
    Task<string?> GetRegionNameAsync(double longitude, double latitude);
}
