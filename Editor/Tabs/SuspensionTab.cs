using UnityEngine.UIElements;

public class SuspensionTab : ITabModule
{
    public string TabName => "Suspension";

    private EditorConsole console;
    private VehicleConfig config;

    public SuspensionTab(EditorConsole c, VehicleConfig cfg)
    {
        console = c;
        config  = cfg;
    }

    public void SetupUI(VisualElement root)
    {
        // Basic suspension settings per axle
        root.Add(new FloatField("Spring Force") { value = 35000f });
        root.Add(new FloatField("Damping") { value = 4500f });
        root.Add(new FloatField("Ride Height") { value = 0.3f });

        console.Log("Suspension tab UI built.");
    }

    public void OnTabActivated() { }
    public void OnTabUpdate() { }
}
