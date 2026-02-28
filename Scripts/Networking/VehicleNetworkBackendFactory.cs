using System;

public static class VehicleNetworkBackendFactory
{
    public static IVehicleNetworkBackend Create(string backendId)
    {
        if (string.IsNullOrWhiteSpace(backendId))
            return new UVSLocalOnlyBackend();

        if (string.Equals(backendId, "ngo", StringComparison.OrdinalIgnoreCase))
            return new UVSNetcodeBackend();

        return new UVSLocalOnlyBackend();
    }
}
