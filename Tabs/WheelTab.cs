// WheelTab.cs
using UnityEngine.UIElements;
using UVS.Modules;

public class WheelTab : ITabModule
{
    public string TabName => "Wheel";

    private EditorConsole console;

    public WheelTab(EditorConsole c)
    {
        console = c;
    }

    public void SetupUI(VisualElement root)
    {
        root.Add(new FloatField("Radius"));
        root.Add(new FloatField("Width"));
        root.Add(new FloatField("Friction Stiffness"));
        root.Add(new Toggle("Override Defaults"));
    }

    public void OnTabActivated() { }
    public void OnTabUpdate()    { }
}
