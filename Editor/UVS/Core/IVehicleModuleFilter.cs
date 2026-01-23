namespace UVS.Editor.Core
{
    // Interface for vehicle module filters
    public interface IVehicleModuleFilter
    {
        // Check if the module is allowed for the given vehicle configuration
        bool IsModuleAllowed(VehicleConfig vehicleConfig, VehicleEditorModuleBase moduleBase);
    }

  
    }