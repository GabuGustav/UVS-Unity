using System.Collections.Generic;

public static class RailBlockReservation
{
    private static readonly HashSet<string> Reserved = new();

    public static bool TryReserve(string blockId)
    {
        if (string.IsNullOrEmpty(blockId)) return false;
        if (Reserved.Contains(blockId)) return false;
        Reserved.Add(blockId);
        return true;
    }

    public static void Release(string blockId)
    {
        if (string.IsNullOrEmpty(blockId)) return;
        Reserved.Remove(blockId);
    }

    public static bool IsReserved(string blockId)
    {
        return !string.IsNullOrEmpty(blockId) && Reserved.Contains(blockId);
    }
}
