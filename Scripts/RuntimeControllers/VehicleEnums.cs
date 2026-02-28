namespace UVS.Shared
{
    public enum AmmoType
    {
        APFSDS,
        HEAT,
        HE,
        APHE,
        Canister,
        Smoke
    }

    public enum FlightMode
    {
        Hover,
        ForwardFlight,
        Transition,
        Auto
    }

    public enum ArmorType
    {
        RolledHomogeneousSteel,
        Composite,
        Reactive,
        ActiveProtection,
        CompositeReactive
    }

    public enum EquipmentType
    {
        Excavator,
        Bulldozer,
        Crane,
        WheelLoader,
        Backhoe,
        SkidSteer,
        DumpTruck,
        ConcretePump
    }

    public enum AttachmentType
    {
        StandardBucket,
        HeavyDutyBucket,
        RockBucket,
        HydraulicHammer,
        Grapple,
        Auger,
        Ripper,
        Blade,
        Hook,
        Forks
    }

    public enum TerrainType
    {
        HardSurface,
        SoftSoil,
        Rocky,
        Mixed,
        Swamp,
        Sand
    }

    public enum DeformationType
    {
        Mesh,
        Vertex,
        Texture,
        Hybrid
    }

    public enum DamageModel
    {
        Simple,
        Realistic,
        Advanced,
        Custom
    }

    public enum MaterialType
    {
        Steel,
        Aluminum,
        Composite,
        Titanium,
        CarbonFiber,
        Plastic,
        Wood
    }

    public enum RepairMethod
    {
        Welding,
        Replacement,
        Bonding,
        Patching,
        Magic
    }

    public enum DamageTexture
    {
        Scratches,
        Dents,
        Cracks,
        Burns,
        Rust,
        Custom
    }

    public enum ControlScheme
    {
        Standard,
        Advanced,
        Expert,
        Custom
    }
}
