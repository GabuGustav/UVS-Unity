using UnityEngine.UIElements;

public class EngineTab : ITabModule
{
    public string TabName => "Engine";

    private EditorConsole console;
    private VehicleConfig config;

    public EngineTab(EditorConsole c, VehicleConfig cfg)
    {
        console = c;
        config  = cfg;
    }

    public void SetupUI(VisualElement root)
    {
        // Simple engine/drivetrain values
        root.Add(new FloatField("Max Torque (Nm)") { value = 400f });
        root.Add(new FloatField("Max RPM") { value = 6500f });
        root.Add(new FloatField("Idle RPM") { value = 800f });
        root.Add(new EnumField("Drivetrain Type", VehicleDrivetrain.RWD));

        console.Log("Engine tab UI built.");
    }

    public void OnTabActivated() { }
    public void OnTabUpdate() { }
}

public enum VehicleDrivetrain
{
    FWD,
    RWD,
    AWD
}
