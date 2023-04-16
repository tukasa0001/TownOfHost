using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor;
public sealed class Puppeteer : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Puppeteer),
            player => new Puppeteer(player),
            CustomRoles.Puppeteer,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            2000,
            null
        );
    public Puppeteer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Puppeteers.Add(this);

        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
    }
    public override void OnDestroy()
    {
        Puppeteers.Clear();
    }

    public static HashSet<Puppeteer> Puppeteers = new(3);
    /// <summary>
    /// Key: ターゲットのPlayerId, Value: パペッティア
    /// </summary>
    private static Dictionary<byte, Puppeteer> Puppets = new(15);

    public override void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (puppeteer, target) = info.AttemptTuple;

        Puppets[target.PlayerId] = this;
        puppeteer.SetKillCooldown();
        Utils.NotifyRoles(SpecifySeer: puppeteer);
        info.DoKill = false;
    }
    public override bool OnReportDeadBody(PlayerControl _, GameData.PlayerInfo __)
    {
        Puppets.Clear();

        return true;
    }
    public static void OnFixedUpdateOthers(PlayerControl puppet)
    {
        foreach (var thisClass in Puppeteers)
            thisClass.CheckPuppetKill(puppet);
    }
    private void CheckPuppetKill(PlayerControl puppet)
    {
        if (!Puppets.ContainsKey(puppet.PlayerId)) return;

        if (!puppet.IsAlive())
        {
            Puppets.Remove(puppet.PlayerId);
        }
        else
        {
            var puppetPos = puppet.transform.position;//puppetの位置
            Dictionary<PlayerControl, float> targetDistance = new();
            foreach (var pc in Main.AllAlivePlayerControls.ToArray())
            {
                if (pc.PlayerId != puppet.PlayerId && !pc.Is(CountTypes.Impostor))
                {
                    var dis = Vector2.Distance(puppetPos, pc.transform.position);
                    targetDistance.Add(pc, dis);
                }
            }
            if (targetDistance.Keys.Count <= 0) return;

            var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
            var target = min.Key;
            var KillRange = NormalGameOptionsV07.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
            if (min.Value <= KillRange && puppet.CanMove && target.CanMove)
            {
                RPC.PlaySoundRPC(Player.PlayerId, Sounds.KillSound);
                target.SetRealKiller(Player);
                puppet.RpcMurderPlayer(target);
                Utils.MarkEveryoneDirtySettings();
                Puppets.Remove(puppet.PlayerId);
                Utils.NotifyRoles();
            }
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (!(Puppets.ContainsValue(this) &&
            Puppets.ContainsKey(seen.PlayerId))) return "";

        return Utils.ColorString(RoleInfo.RoleColor, "◆");
    }
    public override string GetKillButtonText() => GetString("PuppeteerOperateButtonText");
}