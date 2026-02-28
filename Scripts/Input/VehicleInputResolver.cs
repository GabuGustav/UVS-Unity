using UnityEngine.InputSystem;

public static class VehicleInputResolver
{
    public static VehicleInputProfile GetProfile(VehicleConfig config)
    {
        if (config != null && config.inputProfileOverride != null)
            return config.inputProfileOverride;

        return VehicleInputProfile.GetDefault();
    }

    public static InputAction Resolve(InputAction current, InputActionReference reference)
    {
        return reference != null && reference.action != null ? reference.action : current;
    }
}
