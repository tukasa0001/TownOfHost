using Hazel;
using AmongUs.GameOptions;
using UnityEngine;
using System.Collections.Generic;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Roles.Neutral;

public sealed class OtakuPrincess : RoleBase, IKiller
{
    /// <summary>
    ///  20000:TOH4E役職
    ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
    ///    100:役職ID
    /// </summary>
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(OtakuPrincess),
            player => new OtakuPrincess(player),
            CustomRoles.OtakuPrincess,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            23100,
            SetupOptionItem,
            "姫",
            "#ff6be4",
            true,
            countType: CountTypes.Crew,
            assignInfo: new RoleAssignInfo(CustomRoles.OtakuPrincess, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            }
        );
    public OtakuPrincess(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        //Main.isLoversDead = false;
        //Main.LoversPlayers.Clear();
        killCool = KillCool.GetInt();
        isFirstSetting = false;
    }
    public static OptionItem PrinceMax;
    public static OptionItem KillCool;

    int princeMax;
    static int killCool;
    static bool isFirstSetting = false;

    Color HimeChanColor;

    enum OptionName
    {
        MaxOtakuCount,
    }

    private static void SetupOptionItem()
    {
        PrinceMax = IntegerOptionItem.Create(RoleInfo, 10, OptionName.MaxOtakuCount, new(1, 14, 1), 2, false);
        KillCool = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.KillCooldown, new(5, 100, 5), 30, false);
    }

    public override void Add()
    {
        var playerId = Player.PlayerId;
        princeMax = PrinceMax.GetInt();
        ColorUtility.TryParseHtmlString("#ff6be4", out HimeChanColor);

    }
    public float CalculateKillCooldown() => CanUseKillButton() ? killCool : 0f;
    public bool CanUseKillButton()
        => Player.IsAlive()
    && princeMax > 0;

    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? HimeChanColor : Color.gray, $"({princeMax})");
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (Is(info.AttemptKiller) && !info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;

            princeMax--;
            killer.RpcProtectedMurderPlayer(target);
            target.RpcProtectedMurderPlayer(target);
            Logger.Info($"{killer.GetNameWithRole()} : 恋人を作った", "OtakuPrincess");
            SendRPCShot();

            if (!isFirstSetting)
            {
                killer.RpcSetCustomRole(CustomRoles.Lovers);
                List<byte> playerIds = new ();
                playerIds.Add(killer.PlayerId);
                Main.LoversPlayersV2.Add(killer.PlayerId, playerIds);
                Main.isLoversLeaders.Add(killer.PlayerId);
                Main.isLoversDeadV2.Add(killer.PlayerId, false);
                isFirstSetting = true;
            }
            target.RpcSetCustomRole(CustomRoles.Lovers);

            if (CheckOtherLovers(target.PlayerId, out byte teamLeaderId))
            {
                Main.LoversPlayersV2[teamLeaderId].Remove(target.PlayerId);
            }
            Main.LoversPlayersV2[killer.PlayerId].Add(target.PlayerId);
            RPC.SyncLoversPlayers();
            killer.ResetKillCooldown();
            info.DoKill = false;
            //foreach (var targets in Main.LoversPlayersV2[killer.PlayerId]) { Logger.Info("ラバーズ：" +targets.name,"オタク"); }
            Utils.NotifyRoles();
        }
    }

    private bool CheckOtherLovers(byte targetId, out byte leader)
    {
        leader = byte.MaxValue;

        foreach (var list in Main.LoversPlayersV2)
        {
            if (list.Value.Contains(targetId))
            {
                leader = list.Key;
                return true;
            }
        }

        return false;
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = Translator.GetString("PlatonicLoverButtonText");
        return true;
    }
    private void SendRPCShot()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender(CustomRPC.SetPrincessShotLimit);
        sender.Writer.Write(princeMax);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType == CustomRPC.SetPrincessShotLimit)
        {
            princeMax = reader.ReadInt32();
        }
    }
}
