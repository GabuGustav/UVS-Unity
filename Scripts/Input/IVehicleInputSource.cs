public interface IVehicleInputSource
{
    bool IsActive { get; }
    VehicleInputState ReadInput();
}
