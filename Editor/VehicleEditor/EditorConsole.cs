using UnityEngine.UIElements;

public class EditorConsole
{
    private ScrollView view;

    public EditorConsole(ScrollView v)
    {
        view = v;
    }

    public void Log(string msg)
    {
        view.Add(new Label(msg));
    }

    public void Clear()
    {
        view.Clear();
    }
}
