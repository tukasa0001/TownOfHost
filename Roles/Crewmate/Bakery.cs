using System.Collections.Generic;
using Hazel;

using AmongUs.GameOptions;
using static TownOfHostForE.Translator;
using static TownOfHostForE.Utils;
using TownOfHostForE.Roles.Core;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class Bakery : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Bakery),
            player => new Bakery(player),
            CustomRoles.Bakery,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            40000,
            SetupOptionItem,
            "パン屋",
            "#b58428"
        );
    public Bakery(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => IsNeutral(player) ? HasTask.False : HasTask.True
    )
    {
        ChangeChances = OptionChangeChances.GetInt();

        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }
    public override void OnDestroy()
    {
        nBakeries.Clear();
        CustomRoleManager.MarkOthers.Remove(GetMarkOthers);
    }

    public static OptionItem OptionChangeChances;
    enum OptionName
    {
        BakeryChangeChances,
    }
    private static int ChangeChances;

    public PlayerControl PoisonPlayer;
    public static List<Bakery> nBakeries = new();

    private static void SetupOptionItem()
    {
        OptionChangeChances = FloatOptionItem.Create(RoleInfo, 10, OptionName.BakeryChangeChances, new(0, 100, 2), 10, false)
            .SetValueFormat(OptionFormat.Percent);
    }
    public override void Add()
    {
        nBakeries.Clear();
        PoisonPlayer = null;
    }
    private void SendRPC(byte targetId = byte.MaxValue)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender(CustomRPC.SetNBakryPoison);
        sender.Writer.Write(targetId);
    }
    
    private void NBarkySyncRPC(byte targetId = byte.MaxValue)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender(CustomRPC.BakryChangeSync);
        sender.Writer.Write(targetId);
    }

    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType == CustomRPC.SetNBakryPoison)
        {
            var targetId = reader.ReadByte();
            if (targetId != byte.MaxValue)
            {
                PoisonPlayer = GetPlayerById(targetId);
            }
            else
            {
                PoisonPlayer = null;
            }
        }
        else if (rpcType == CustomRPC.BakryChangeSync)
        {
            var targetId = reader.ReadByte();
            if (targetId == Player.PlayerId)
            {
                nBakeries.Add(this);
            }
        }
    }
    public static bool IsPoisoned(PlayerControl target = null)
    {
        foreach (var nBakery in nBakeries)
        {
            if (target == null && nBakery.PoisonPlayer != null) return true;
            if (target != null && nBakery.PoisonPlayer == target) return true;
        }
        return false;
    }
    public static bool IsNeutral(PlayerControl bakery)
    {
        foreach(var ba in nBakeries)
        {
            if (ba.Player == bakery)
                return true;
        }
        return false;
    }

    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        //第三パン屋じゃないなら関係ないので返す
        if (!nBakeries.Contains(this) || info.IsSuicide) return;

        PoisonPlayer = null;
        if (AmongUsClient.Instance.AmHost) SendRPC();
        Logger.Info($"{target.GetNameWithRole()}の配布毒パン回収", "NBakery");
    }

    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting && IsPoisoned(seen))
        {
            return ColorString(RoleInfo.RoleColor, "θ");
        }
        return string.Empty;
    }

    public override void OnStartMeeting()
    {
        //処理はホストのみ
        if (!AmongUsClient.Instance.AmHost) return;
        var PlayerId = Player.PlayerId;
        var BakeryTitle = $"<color={RoleInfo.RoleColorCode}>{GetString("PanAliveMessageTitle")}</color>";

        if (nBakeries.Contains(this) && Player.IsAlive())
        {
            if (PoisonPlayer.IsAlive())
                SendMessage(GetString("BakeryChangeNow"), title: BakeryTitle);
            else //配る予定だった人が死んでいたら
            {
                PoisonPlayer = null;
                if (AmongUsClient.Instance.AmHost) SendRPC();
                SendMessage(GetString("BakeryChangeNONE"), title: BakeryTitle);
            }
        }
        else if (Player.Is(CustomRoles.Bakery) && Player.IsAlive())
        {
            string panMessage = "";
            int chance = IRandom.Instance.Next(1, 101);
            if (chance <= ChangeChances)
            {
                panMessage = GetString("BakeryChange");
                nBakeries.Add(this);
                NBarkySyncRPC(Player.PlayerId);
            }
            else if (chance <= 77) panMessage = GetString("PanAlive");
            else if (chance <= 79) panMessage = GetString("PanAlive1");
            else if (chance <= 81) panMessage = GetString("PanAlive2");
            else if (chance <= 82) panMessage = GetString("PanAlive3");
            else if (chance <= 84) panMessage = GetString("PanAlive4");
            else if (chance <= 86) panMessage = GetString("PanAlive5");
            else if (chance <= 87) panMessage = GetString("PanAlive6");
            else if (chance <= 88) panMessage = GetString("PanAlive7");
            else if (chance <= 90) panMessage = GetString("PanAlive8");
            else if (chance <= 92) panMessage = GetString("PanAlive9");
            else if (chance <= 94) panMessage = GetString("PanAlive10");
            else if (chance <= 96) panMessage = GetString("PanAlive11");
            else if (chance <= 98)
            {
                List<PlayerControl> targetList = new();
                var rand = IRandom.Instance;
                foreach (var p in Main.AllAlivePlayerControls)
                {
                    if (p.Is(CustomRoles.Bakery)) continue;
                    targetList.Add(p);
                }
                var TargetPlayer = targetList[rand.Next(targetList.Count)];
                panMessage = string.Format(GetString("PanAlive12"), TargetPlayer.GetRealName());
            }
            else if (chance <= 100)
            {
                List<PlayerControl> targetList = new();
                var rand = IRandom.Instance;
                foreach (var p in Main.AllAlivePlayerControls)
                {
                    if (p.Is(CustomRoles.Bakery)) continue;
                    targetList.Add(p);
                }
                var TargetPlayer = targetList[rand.Next(targetList.Count)];
                panMessage = string.Format(GetString("PanAlive13"), TargetPlayer.GetRealName());
            }

            SendMessage(panMessage, title: BakeryTitle);
        }
    }
    public override void AfterMeetingTasks()
    {
        //処理はホストのみ
        if (!AmongUsClient.Instance.AmHost) return;
        var PlayerId = Player.PlayerId;
        //第三パン屋でない||死亡
        if (!nBakeries.Contains(this) || !Player.IsAlive()) return;

        if (Player.IsAlive() || MyState.DeathReason != CustomDeathReason.Vote)
        {   //吊られなかった時毒発動
            if (PoisonPlayer != null
                && !Main.AfterMeetingDeathPlayers.ContainsKey(PoisonPlayer.PlayerId))
            {
                PoisonPlayer.SetRealKiller(Player);
                MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Poisoning, PoisonPlayer.PlayerId);
            }
        }
        else //吊られた時のKey取り消し 以降のパンセットいらないので返す
        {
            PoisonPlayer = null;
            if (AmongUsClient.Instance.AmHost) SendRPC();
            return;
        }

        //次のターゲットを決めておく
        List<PlayerControl> targetList = new();
        var rand = IRandom.Instance;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (Player == pc) continue;
            if (Main.AfterMeetingDeathPlayers.ContainsKey(pc.PlayerId)) continue;
            targetList.Add(pc);
        }
        var PoisonedSelectPlayer = targetList[rand.Next(targetList.Count)];
        PoisonPlayer = PoisonedSelectPlayer;
        if (AmongUsClient.Instance.AmHost) SendRPC(PoisonPlayer.PlayerId);
        Logger.Info($"{Player.GetNameWithRole()}の次ターン配布先：{PoisonedSelectPlayer.GetNameWithRole()}", "NBakery");
    }
}