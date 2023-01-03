using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using TownOfHost.Interface.Menus.CustomNameMenu;
using TownOfHost.Modules;
using TownOfHost.ReduxOptions;
using UnityEngine;
using static TownOfHost.Translator;
using TownOfHost.Roles;
using TownOfHost.RPC;

namespace TownOfHost.Extensions;

public static class PlayerControlExtensions
{
    public static void RpcSetCustomRole(this PlayerControl player, CustomRole role)
    {
        player.GetPlayerPlus().DynamicName = DynamicName.For(player);
        CustomRoleManager.PlayersCustomRolesRedux[player.PlayerId] = role.Instantiate(player);
        // TODO: eventually add back subrole logic
        RpcV2.Immediate(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole)
            .Write(player.PlayerId)
            .WritePacked(CustomRoleManager.GetRoleId(role.GetType()))
            .Send();
    }
    public static void RpcSetCustomRole(byte playerId, CustomRoles role)
    {
        RpcV2.Immediate(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole)
            .Write(playerId)
            .WritePacked(CustomRoleManager.GetRoleId(role.GetType()))
            .Send();
    }

    public static void RpcExile(this PlayerControl player)
    {
        OldRPC.ExileAsync(player);
    }
    public static InnerNet.ClientData GetClient(this PlayerControl player)
    {
        var client = AmongUsClient.Instance.allClients.ToArray().FirstOrDefault(cd => cd.Character.PlayerId == player.PlayerId);
        return client;
    }
    public static int GetClientId(this PlayerControl player)
    {
        var client = player.GetClient();
        return client == null ? -1 : client.Id;
    }
    /// <summary>
    /// ※サブロールは取得できません。
    /// </summary>
    public static CustomRole GetCustomRole(this PlayerControl player)
    {
        if (player == null)
        {
            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            Logger.Warn(callerClassName + "." + callerMethodName + " Invalid Custom Role", "GetCustomRole");
            return CustomRoleManager.Static.Crewmate;
        }
        // var GetValue = Main.PlayerStates.TryGetValue(player.PlayerId, out var State);

        //  return GetValue ? State.MainRole : CustomRoles.Crewmate;
        CustomRole? role = CustomRoleManager.PlayersCustomRolesRedux.GetValueOrDefault(player.PlayerId);
        return role ?? (player.Data.Role == null ? CustomRoleManager.Default
            : player.Data.Role.Role switch
            {
                RoleTypes.Crewmate => CustomRoleManager.Static.Crewmate,
                RoleTypes.Engineer => CustomRoleManager.Static.Engineer,
                RoleTypes.Scientist => CustomRoleManager.Static.Scientist,
                /*RoleTypes.GuardianAngel => CustomRoleManager.Static.GuardianAngel,*/
                RoleTypes.Impostor => CustomRoleManager.Static.Impostor,
                RoleTypes.Shapeshifter => CustomRoleManager.Static.Morphling,
                _ => CustomRoleManager.Default,
            });
    }

    public static List<CustomRole> GetCustomSubRoles(this PlayerControl player)
    {
        if (player == null)
        {
            Logger.Warn("CustomSubRoleを取得しようとしましたが、対象がnullでした。", "getCustomSubRole");
            // new() { Roles.Subrole };
        }
        //  return Main.PlayerStates[player.PlayerId].SubRoles;
        return new();
    }

    public static void RpcSetRoleDesync(this PlayerControl player, RoleTypes role, int clientId)
    {
        //player: 名前の変更対象

        if (player == null) return;
        if (AmongUsClient.Instance.ClientId == clientId)
        {
            player.SetRole(role);
            return;
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetRole, Hazel.SendOption.Reliable, clientId);
        writer.Write((ushort)role);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    public static void RpcGuardAndKill(this PlayerControl killer, PlayerControl target = null, int colorId = 0)
    {
        if (target == null) target = killer;
        // Host
        if (killer.AmOwner)
        {
            killer.ProtectPlayer(target, colorId);
            killer.MurderPlayer(target);
        }
        // Other Clients
        if (killer.PlayerId != 0)
        {
            var sender = CustomRpcSender.Create("GuardAndKill Sender", SendOption.None);
            sender.StartMessage(killer.GetClientId());
            sender.StartRpc(killer.NetId, (byte)RpcCalls.ProtectPlayer)
                .WriteNetObject((InnerNetObject)target)
                .Write(colorId)
                .EndRpc();
            sender.StartRpc(killer.NetId, (byte)RpcCalls.MurderPlayer)
                .WriteNetObject((InnerNetObject)target)
                .EndRpc();
            sender.EndMessage();
            sender.SendMessage();
        }
    }
    public static void SetKillCooldown(this PlayerControl player, float time)
    {
        CustomRole role = player.GetCustomRole();
        if (!(role.IsImpostor() || player.IsNeutralKiller() || role is Arsonist or Sheriff)) return;
        if (player.AmOwner)
        {
            player.SetKillTimer(time);
        }
        else
        {
            Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
            player.MarkDirtySettings();
            player.RpcGuardAndKill();
        }
    }
    public static void RpcSpecificMurderPlayer(this PlayerControl killer, PlayerControl target = null)
    {
        if (target == null) target = killer;
        if (killer.AmOwner)
        {
            killer.MurderPlayer(target);
        }
        else
        {
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, killer.GetClientId());
            messageWriter.WriteNetObject(target);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
    }
    [Obsolete]
    public static void RpcSpecificProtectPlayer(this PlayerControl killer, PlayerControl target = null, int colorId = 0)
    {
        if (AmongUsClient.Instance.AmClient)
        {
            killer.ProtectPlayer(target, colorId);
        }
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.Reliable, killer.GetClientId());
        messageWriter.WriteNetObject(target);
        messageWriter.Write(colorId);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }
    public static void RpcResetAbilityCooldown(this PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return; //ホスト以外が実行しても何も起こさない
        Logger.Info($"アビリティクールダウンのリセット:{target.name}({target.PlayerId})", "RpcResetAbilityCooldown");
        if (PlayerControl.LocalPlayer == target)
        {
            //targetがホストだった場合
            PlayerControl.LocalPlayer.Data.Role.SetCooldown();
        }
        else
        {
            //targetがホスト以外だった場合
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(target.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.None, target.GetClientId());
            writer.WriteNetObject(target);
            writer.Write(0);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static void RpcDesyncRepairSystem(this PlayerControl target, SystemTypes systemType, int amount)
    {
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, target.GetClientId());
        messageWriter.Write((byte)systemType);
        messageWriter.WriteNetObject(target);
        messageWriter.Write((byte)amount);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }

    public static void MarkDirtySettings(this PlayerControl player)
    {
        PlayerGameOptionsSender.SetDirty(player.PlayerId);
    }
    public static TaskState GetPlayerTaskState(this PlayerControl player)
    {
        return Main.PlayerStates[player.PlayerId].GetTaskState();
    }

    public static T GetCustomRole<T>(this PlayerControl player) where T : CustomRole
    {
        return (T)player.GetCustomRole();
    }

    public static string GetDisplayRoleName(this PlayerControl player)
    {
        return Utils.GetDisplayRoleName(player.PlayerId);
    }
    public static string GetSubRoleName(this PlayerControl player)
    {
        var SubRoles = Main.PlayerStates[player.PlayerId].SubRoles;
        if (SubRoles.Count == 0) return "";
        var sb = new StringBuilder();
        foreach (var role in SubRoles)
        {
            if (role == CustomRoles.NotAssigned) continue;
            //   sb.Append($" + {Utils.GetRoleName(role)}");
        }

        return sb.ToString();
    }
    public static string GetAllRoleName(this PlayerControl player)
    {
        if (!player) return null;
        var text = Utils.GetRoleName(player.GetCustomRole());
        text += player.GetSubRoleName();
        return text;
    }
    public static string GetNameWithRole(this PlayerControl player)
    {
        return $"{player.GetRawName()}" + (GameStates.IsInGame ? $"({player?.GetAllRoleName()})" : "");
    }
    public static string GetRoleColorCode(this PlayerControl player)
    {
        return Utils.GetRoleColorCode(player.GetCustomRole());
    }
    public static Color GetRoleColor(this PlayerControl player)
    {
        return Utils.GetRoleColor(player.GetCustomRole());
    }
    public static void ResetPlayerCam(this PlayerControl pc, float delay = 0f)
    {
        if (pc == null || !AmongUsClient.Instance.AmHost || pc.AmOwner) return;

        var systemtypes = SystemTypes.Reactor;
        if (Main.NormalOptions.MapId == 2) systemtypes = SystemTypes.Laboratory;

        new DTask(() =>
        {
            pc.RpcDesyncRepairSystem(systemtypes, 128);
        }, 0f + delay, "Reactor Desync");

        new DTask(() =>
        {
            pc.RpcSpecificMurderPlayer();
        }, 0.2f + delay, "Murder To Reset Cam");

        new DTask(() =>
        {
            pc.RpcDesyncRepairSystem(systemtypes, 16);
            if (Main.NormalOptions.MapId == 4) //Airship用
                pc.RpcDesyncRepairSystem(systemtypes, 17);
        }, 0.4f + delay, "Fix Desync Reactor");
    }
    public static void ReactorFlash(this PlayerControl pc, float delay = 0f)
    {
        if (pc == null) return;
        int clientId = pc.GetClientId();
        // Logger.Info($"{pc}", "ReactorFlash");
        var systemtypes = SystemTypes.Reactor;
        if (Main.NormalOptions.MapId == 2) systemtypes = SystemTypes.Laboratory;
        float FlashDuration = StaticOptions.KillFlashDuration;

        pc.RpcDesyncRepairSystem(systemtypes, 128);

        new DTask(() =>
        {
            pc.RpcDesyncRepairSystem(systemtypes, 16);

            if (Main.NormalOptions.MapId == 4) //Airship用
                pc.RpcDesyncRepairSystem(systemtypes, 17);
        }, FlashDuration + delay, "Fix Desync Reactor");
    }

    public static string GetRealName(this PlayerControl player, bool isMeeting = false)
    {
        return isMeeting ? player?.Data?.PlayerName : player?.name;
    }
    public static bool CanUseKillButton(this PlayerControl pc)
    {
        return pc.GetCustomRole() is Impostor i && i.CanKill();
        /*return pc.GetCustomRole() switch
        {
            FireWorks f => f.CanKill(),
            Mafia m => ((Impostor)m).CanKill(),
            Mare m => m.CanKill(),
            Sheriff s => s.DesyncRole is RoleTypes.Impostor,
            Egoist or Jackal => true,
            _ => pc.Is(RoleType.Impostor),
        };*/
    }
    public static void RpcSetDousedPlayer(this PlayerControl player, PlayerControl target, bool isDoused)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDousedPlayer, SendOption.Reliable, -1);//RPCによる同期
        writer.Write(player.PlayerId);
        writer.Write(target.PlayerId);
        writer.Write(isDoused);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ResetKillCooldown(this PlayerControl player)
    {
        Main.AllPlayerKillCooldown[player.PlayerId] = 0;
        Logger.Warn("ResetKillCooldown not implemented yet", "RKC");
        //throw new NotImplementedException("haha");
    }

    public static void CanUseImpostorVent(this PlayerControl player)
        {

            switch (player.GetCustomRole())
            {
                case Amnesiac:
                case Sheriff:
                case Investigator:
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(false);
                    player.Data.Role.CanVent = false;
                    return;
                case Arsonist a:
                    bool canUse = a.CanVent() || (StaticOptions.TOuRArso);
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(canUse && !player.Data.IsDead);
                    player.Data.Role.CanVent = canUse;
                    return;
                case Juggernaut:
                    bool jug_canUse = StaticOptions.JuggerCanVent;
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(jug_canUse && !player.Data.IsDead);
                    player.Data.Role.CanVent = jug_canUse;
                    return;
                case Sidekick:
                case Jackal:
                    bool jackal_canUse = player.GetCustomRole().CanVent();
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(jackal_canUse && !player.Data.IsDead);
                    player.Data.Role.CanVent = jackal_canUse;
                    return;
                case Marksman:
                    bool marks_canUse = StaticOptions.MarksmanCanVent;
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(marks_canUse && !player.Data.IsDead);
                    player.Data.Role.CanVent = marks_canUse;
                    return;
                case PlagueBearer:
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(false);
                    player.Data.Role.CanVent = false;
                    return;
                case Pestilence:
                    bool pesti_CanUse = StaticOptions.PestiCanVent;
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(pesti_CanUse && !player.Data.IsDead);
                    player.Data.Role.CanVent = pesti_CanUse;
                    return;
                case Glitch:
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(!player.Data.IsDead);
                    player.Data.Role.CanVent = true;
                    return;
                case Werewolf:
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(!player.Data.IsDead);
                    player.Data.Role.CanVent = true;
                    return;
                case CorruptedSheriff:
                case Medusa:
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(!player.Data.IsDead);
                    player.Data.Role.CanVent = true;
                    return;
                case HexMaster:
                case CovenWitch:
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(Main.HasNecronomicon && !player.Data.IsDead);
                    player.Data.Role.CanVent = Main.HasNecronomicon;
                    break;
                case Janitor:
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(StaticOptions.STIgnoreVent && !player.Data.IsDead);
                    player.Data.Role.CanVent = StaticOptions.STIgnoreVent;
                    break;
            }
        }
    public static bool CanMakeMadmate(this PlayerControl player)
    {
        return StaticOptions.CanMakeMadmateCount > Main.SKMadmateNowCount
               && player != null
               && player.Data.Role.Role == RoleTypes.Shapeshifter
               && player.GetCustomRole().CanMakeMadmate();
    }
    public static void RpcExileV2(this PlayerControl player)
    {
        player.Exiled();
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, SendOption.None, -1);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcMurderPlayerV2(this PlayerControl killer, PlayerControl target)
    {
        if (target == null) target = killer;
        if (AmongUsClient.Instance.AmClient)
        {
            killer.MurderPlayer(target);
        }
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, -1);
        messageWriter.WriteNetObject(target);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        Utils.NotifyRoles();
    }
    public static void NoCheckStartMeeting(this PlayerControl reporter, GameData.PlayerInfo target)
    { /*サボタージュ中でも関係なしに会議を起こせるメソッド
            targetがnullの場合はボタンとなる*/
        MeetingRoomManager.Instance.AssignSelf(reporter, target);
        DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(reporter);
        reporter.RpcStartMeeting(target);
    }
    public static bool IsModClient(this PlayerControl player) => Main.playerVersion.ContainsKey(player.PlayerId);
    ///<summary>
    ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、戻り値を返します。
    ///</summary>
    ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
    ///<returns>GetPlayersInAbilityRangeSortedの戻り値</returns>
    public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, bool ignoreColliders = false) => GetPlayersInAbilityRangeSorted(player, pc => true, ignoreColliders);
    ///<summary>
    ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、predicateの条件に合わないものを除外して返します。
    ///</summary>
    ///<param name="predicate">リストに入れるプレイヤーの条件 このpredicateに入れてfalseを返すプレイヤーは除外されます。</param>
    ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
    ///<returns>GetPlayersInAbilityRangeSortedの戻り値から条件に合わないプレイヤーを除外したもの。</returns>
    public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, Predicate<PlayerControl> predicate, bool ignoreColliders = false)
    {
        var rangePlayersIL = RoleBehaviour.GetTempPlayerList();
        List<PlayerControl> rangePlayers = new();
        player.Data.Role.GetPlayersInAbilityRangeSorted(rangePlayersIL, ignoreColliders);
        foreach (var pc in rangePlayersIL)
        {
            if (predicate(pc)) rangePlayers.Add(pc);
        }
        return rangePlayers;
    }
    public static bool IsNeutralKiller(this PlayerControl player)
    {
        return
            player.GetCustomRole() is
                Egoist or
                Jackal;
    }
    // this is new
    public static bool KnowDeathReason(this PlayerControl seer, PlayerControl target)
       => (seer.Is(Doctor.Ref<Doctor>())
            || (seer.Is(Roles.RoleType.Madmate) && StaticOptions.MadmateCanSeeDeathReason)
            || (seer.Data.IsDead && StaticOptions.GhostCanSeeDeathReason))
           && target.Data.IsDead;
    public static string GetRoleInfo(this PlayerControl player, bool InfoLong = false)
    {
        var role = player.GetCustomRole();
        if (role.IsVanilla())
        {
            var blurb = role switch
            {
                Morphling => InfoLong ? StringNames.ShapeshifterBlurbLong : StringNames.ShapeshifterBlurb,
                Impostor => StringNames.ImpostorBlurb,
                Scientist => InfoLong ? StringNames.ScientistBlurbLong : StringNames.ScientistBlurb,
                Engineer => InfoLong ? StringNames.EngineerBlurbLong : StringNames.EngineerBlurb,
                GuardianAngel => InfoLong ? StringNames.GuardianAngelBlurbLong : StringNames.GuardianAngelBlurb,
                _ => StringNames.CrewmateBlurb,
            };
            return (InfoLong ? "\n" : "") + DestroyableSingleton<TranslationController>.Instance.GetString(blurb);
        }

        var text = role.ToString();

        var Prefix = "";
        if (!InfoLong)
            switch (role)
            {
                case Mafia:
                    Prefix = Utils.CanMafiaKill() ? "After" : "Before";
                    break;
                //    case EvilWatcher:
                //    case NiceWatcher:
                //      text = Watcher.ToString();
                //         break;
                case MadSnitch:
                case MadGuardian:
                    text = role.RoleName;
                    Prefix = player.GetPlayerTaskState().IsTaskFinished ? "" : "Before";
                    break;
            };
        return GetString($"{Prefix}{text}Info" + (InfoLong ? "Long" : ""));
    }

    //汎用
    public static bool Is(this PlayerControl target, CustomRole role) =>
        role.IsSubrole ? target.GetCustomSubRoles().Contains(role) : target.GetCustomRole() == role;
    public static bool Is(this PlayerControl target, Roles.RoleType type) { return target.GetCustomRole().GetRoleType() == type; }
    public static bool IsAlive(this PlayerControl target) => target != null && !target.Data.IsDead && !target.Data.Disconnected;

}