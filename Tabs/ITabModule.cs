using UnityEngine.UIElements;

public interface ITabModule
{
    string TabName { get; }

    // Build the tab UI into the provided root element
    void SetupUI(VisualElement root);

    // Called when the tab becomes active
    void OnTabActivated();

    // Called once per editor update (optional tick hook)
    void OnTabUpdate();
}
