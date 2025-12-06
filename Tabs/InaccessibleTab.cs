// InaccessibleTab.cs
using UnityEngine.UIElements;
using UVS.Modules;

public class InaccessibleTab : ITabModule
{
    public string TabName => "Hidden Parts";

    private EditorConsole console;

    public InaccessibleTab(EditorConsole c)
    {
        console = c;
    }

    public void SetupUI(VisualElement root)
    {
        root.Add(new Label("Manage sealed/internal parts here."));
    }

    public void OnTabActivated() { }
    public void OnTabUpdate()    { }
}
