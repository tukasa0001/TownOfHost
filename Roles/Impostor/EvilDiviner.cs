using AmongUs.GameOptions;
using System.Collections.Generic;
using Hazel;
using UnityEngine;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Roles.Impostor;
public sealed class EvilDiviner : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(EvilDiviner),
            player => new EvilDiviner(player),
            CustomRoles.EvilDiviner,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            20600,
            SetupOptionItem,
            "イビルディバイナー"
        );
    public EvilDiviner(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        DivinationMaxCount = OptionDivinationMaxCount.GetInt();

        DivinationTarget = new();
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionDivinationMaxCount;
    enum OptionName
    {
        EvilDivinerDivinationMaxCount,
    }
    private static float KillCooldown;
    private static int DivinationMaxCount;

    static int DivinationCount;
    private static Dictionary<byte, List<byte>> DivinationTarget = new();

    public static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionDivinationMaxCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.EvilDivinerDivinationMaxCount, new(1, 15, 1), 5, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add()
    {
        DivinationCount = DivinationMaxCount;
        DivinationTarget.TryAdd(Player.PlayerId, new());
        Player.AddDoubleTrigger();
    }
    private void SendRPC(byte playerId, byte targetId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender(CustomRPC.SetEvilDiviner);
        sender.Writer.Write(playerId);
        sender.Writer.Write(targetId);
    }

    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetEvilDiviner) return;

        byte playerId = reader.ReadByte();
        byte targetId = reader.ReadByte();

        if (DivinationTarget.ContainsKey(playerId))
            DivinationTarget[playerId].Add(targetId);
        else
            DivinationTarget.Add(playerId, new());
    }

    public float CalculateKillCooldown() => KillCooldown;
    public override string GetProgressText(bool comms = false) => Utils.ColorString(DivinationCount > 0 ? Palette.ImpostorRed : Color.gray, $"({DivinationCount})");

    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (DivinationTarget[Player.PlayerId].Contains(seen.PlayerId) && Player.IsAlive())
            enabled = true;
    }

    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (DivinationCount > 0)
        {
            return killer.CheckDoubleTrigger(target, () => { SetDivination(killer, target); });
        }
        else return true;
    }
    public static void SetDivination(PlayerControl killer, PlayerControl target)
    {
        var killerId = killer.PlayerId;
        var targetId = target.PlayerId;
        if (!DivinationTarget[killerId].Contains(targetId))
        {
            DivinationCount--;
            DivinationTarget[killerId].Add(targetId);
            Logger.Info($"{killer.GetNameWithRole()}：占った 占い先→{target.GetNameWithRole()} || 残り{DivinationCount}回", "EvilDiviner");
            Utils.NotifyRoles(SpecifySeer: killer);

            //キルクールの適正化
            killer.SetKillCooldown();
        }
    }
}