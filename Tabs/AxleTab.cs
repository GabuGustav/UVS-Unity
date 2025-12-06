// AxleTab.cs
using UnityEngine.UIElements;
using UVS.Modules;

public class AxleTab : ITabModule
{
    public string TabName => "Axle";

    private EditorConsole console;

    public AxleTab(EditorConsole c)
    {
        console = c;
    }

    public void SetupUI(VisualElement root)
    {
        root.Add(new IntegerField("Axle Count"));
        root.Add(new Toggle("Driven Wheels"));
        root.Add(new Toggle("Turning Wheels"));
    }

    public void OnTabActivated() { }
    public void OnTabUpdate()    { }
}
