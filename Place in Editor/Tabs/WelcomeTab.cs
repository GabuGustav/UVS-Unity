using UnityEngine.UIElements;
using UVS.Modules;

public class WelcomeTab : ITabModule
{
    public string TabName => "Welcome";

    public void SetupUI(VisualElement root)
    {
        var label = new Label(
            "Welcome to the Vehicle Editor System!\n\n" +
            "Click any tab above to begin configuring your vehicle."
        );
        label.AddToClassList("welcome-label");
        root.Add(label);
    }

    public void OnTabActivated() { }
    public void OnTabUpdate()    { }
}
