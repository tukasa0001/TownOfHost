using System;
using System.Collections.Generic;

namespace TOHE;

public static class NameNotifyManager
{
    private static Dictionary<byte, (string, long)> Notice = new();
    public static void Reset() => Notice = new();
    public static bool Notifying(this PlayerControl pc) => Notice.ContainsKey(pc.PlayerId);
    public static void Notify(this PlayerControl pc, string text, float time)
    {
        Notice.Remove(pc.PlayerId);
        Notice.Add(pc.PlayerId, new (text, Utils.GetTimeStamp(DateTime.Now) + (long)time));
        Utils.NotifyRoles(pc);
        Logger.Info($"New name notify for {pc.GetNameWithRole().RemoveHtmlTags()}: {text} ({time}s)", "Name Notify");
    }
    public static void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask)
        {
            Notice = new();
            return;
        }
        if (Notice.ContainsKey(player.PlayerId) && Notice[player.PlayerId].Item2 < Utils.GetTimeStamp(DateTime.Now))
        {
            Notice.Remove(player.PlayerId);
            Utils.NotifyRoles(player);
        }
    }
    public static bool GetNameNotify(PlayerControl player, ref string name)
    {
        if (!Notice.ContainsKey(player.PlayerId)) return false;
        name = Notice[player.PlayerId].Item1;
        return true;
    }
}