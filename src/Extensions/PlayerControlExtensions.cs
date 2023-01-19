#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using TownOfHost.GUI;
using TownOfHost.Managers;
using TownOfHost.Options;
using UnityEngine;
using TownOfHost.Roles;
using TownOfHost.RPC;
using VentLib.Extensions;
using VentLib.Logging;
using VentLib.Utilities;

namespace TownOfHost.Extensions;

public static class PlayerControlExtensions
{
    public static bool IsHost(this PlayerControl player) =>
        AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == player.PlayerId;

    public static void RpcSetCustomRole(this PlayerControl player, CustomRole role)
    {
        player.GetPlayerPlus().DynamicName = DynamicName.For(player);
        CustomRoleManager.PlayersCustomRolesRedux[player.PlayerId] = role.Instantiate(player);
        // TODO: eventually add back subrole logic
        /*RpcV2.Immediate(PlayerControl.LocalPlayer.NetId, (byte)CustomRPCOLD.SetCustomRole)
            .Write(player.PlayerId)
            .WritePacked(CustomRoleManager.GetRoleId(role.GetType()))
            .Send();*/
    }

    public static ClientData? GetClient(this PlayerControl player)
    {
        var client = AmongUsClient.Instance.allClients.ToArray().FirstOrDefault(cd => cd.Character.PlayerId == player.PlayerId);
        return client;
    }

    public static void Trigger(this PlayerControl player, RoleActionType action, ref ActionHandle handle, params object[] parameters)
    {
        CustomRole role = player.GetCustomRole();
        List<Subrole> subroles = player.GetSubroles();
        role.Trigger(action, ref handle, parameters);
        if (handle is { IsCanceled: true }) return;
        foreach (Subrole subrole in subroles)
        {
            subrole.Trigger(action, ref handle, parameters);
            if (handle is { IsCanceled: true }) return;
        }
    }

    public static CustomRole GetCustomRole(this PlayerControl player)
    {
        if (player == null)
        {
            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string? callerClassName = callerMethod.DeclaringType.FullName;
            VentLogger.Warn(callerClassName + "." + callerMethodName + " Invalid Custom Role", "GetCustomRole");
            return CustomRoleManager.Static.Crewmate;
        }

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

    public static Subrole? GetSubrole(this PlayerControl player)
    {
        List<Subrole>? role = CustomRoleManager.PlayerSubroles.GetValueOrDefault(player.PlayerId);
        if (role == null || role.Count == 0) return null;
        return role[0];
    }

    public static T? GetSubrole<T>(this PlayerControl player) where T : Subrole
    {
        return (T?)player.GetSubrole()!;
    }

    public static List<Subrole> GetSubroles(this PlayerControl player)
    {
        return CustomRoleManager.PlayerSubroles.GetValueOrDefault(player.PlayerId, new List<Subrole>());
    }

    public static List<CustomRole> GetCustomSubRoles(this PlayerControl player)
    {
        if (player == null)
        {
            VentLogger.Warn("CustomSubRoleを取得しようとしましたが、対象がnullでした。", "getCustomSubRole");
            // new() { Roles.Subrole };
        }
        //  return Main.PlayerStates[player.PlayerId].SubRoles;
        return new();
    }

    public static string GetRoleName(this PlayerControl player) => player.GetCustomRole().RoleName;

    public static string GetRawName(this PlayerControl? player, bool isMeeting = false)
    {
        try { return player.GetDynamicName().RawName; }
        catch { return player.Data.PlayerName; }
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

    public static void RpcGuardAndKill(this PlayerControl killer, PlayerControl? target = null, int colorId = 0)
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
            TOHPlugin.AllPlayerKillCooldown[player.PlayerId] = time * 2;
            player.RpcGuardAndKill();
        }
    }
    public static void RpcSpecificMurderPlayer(this PlayerControl killer, PlayerControl? target = null)
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


    public static void RpcResetAbilityCooldown(this PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return; //ホスト以外が実行しても何も起こさない
        VentLogger.Old($"アビリティクールダウンのリセット:{target.name}({target.PlayerId})", "RpcResetAbilityCooldown");
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

    public static T GetCustomRole<T>(this PlayerControl player) where T : CustomRole
    {
        return (T)player.GetCustomRole();
    }

    public static string? GetAllRoleName(this PlayerControl player)
    {
        if (!player) return null;
        var text = Utils.GetRoleName(player.GetCustomRole());
        text += player.GetSubroles().Select(r => r.RoleName).StrJoin();
        return text;
    }

    public static string GetNameWithRole(this PlayerControl? player)
    {
        return $"{player.GetRawName()}" + (GameStates.IsInGame ? $"({player?.GetAllRoleName()})" : "");
    }

    public static Color GetRoleColor(this PlayerControl player)
    {
        return Utils.GetRoleColor(player.GetCustomRole());
    }
    public static void ResetPlayerCam(this PlayerControl pc, float delay = 0f)
    {
        if (pc == null || !AmongUsClient.Instance.AmHost || pc.AmOwner) return;

        var systemtypes = SystemTypes.Reactor;
        if (TOHPlugin.NormalOptions.MapId == 2) systemtypes = SystemTypes.Laboratory;

        Async.ScheduleInStep(() => pc.RpcDesyncRepairSystem(systemtypes, 128), 0f + delay);
        Async.ScheduleInStep(() => pc.RpcSpecificMurderPlayer(), 0.2f + delay);

        Async.ScheduleInStep(() => {
            pc.RpcDesyncRepairSystem(systemtypes, 16);
            if (TOHPlugin.NormalOptions.MapId == 4) //Airship用
                pc.RpcDesyncRepairSystem(systemtypes, 17);
        }, 0.4f + delay);
    }
    public static void ReactorFlash(this PlayerControl pc, float delay = 0f)
    {
        if (pc == null) return;
        var systemtypes = SystemTypes.Reactor;
        if (TOHPlugin.NormalOptions.MapId == 2) systemtypes = SystemTypes.Laboratory;
        float FlashDuration = StaticOptions.KillFlashDuration;

        pc.RpcDesyncRepairSystem(systemtypes, 128);



        new DTask(() =>
        {
            pc.RpcDesyncRepairSystem(systemtypes, 16);

            if (TOHPlugin.NormalOptions.MapId == 4) //Airship用
                pc.RpcDesyncRepairSystem(systemtypes, 17);
        }, FlashDuration + delay, "Fix Desync Reactor");
    }

    public static string? GetRealName(this PlayerControl? player, bool isMeeting = false)
    {
        return isMeeting ? player?.Data?.PlayerName : player?.name;
    }

    public static bool CanUseKillButton(this PlayerControl pc) => pc.GetCustomRole() is Impostor i && i.CanKill();

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
            case Traitor:
            case Medusa:
                DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(!player.Data.IsDead);
                player.Data.Role.CanVent = true;
                return;
            case HexMaster:
            case CovenWitch:
                DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(TOHPlugin.HasNecronomicon && !player.Data.IsDead);
                player.Data.Role.CanVent = TOHPlugin.HasNecronomicon;
                break;
            case Janitor:
                DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(StaticOptions.STIgnoreVent && !player.Data.IsDead);
                player.Data.Role.CanVent = StaticOptions.STIgnoreVent;
                break;
        }
    }
    public static bool CanMakeMadmate(this PlayerControl player)
    {
        return StaticOptions.CanMakeMadmateCount > TOHPlugin.SKMadmateNowCount
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
            killer.MurderPlayer(target);
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, -1);
        messageWriter.WriteNetObject(target);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }
    public static void NoCheckStartMeeting(this PlayerControl reporter, GameData.PlayerInfo target)
    { /*サボタージュ中でも関係なしに会議を起こせるメソッド
            targetがnullの場合はボタンとなる*/
        MeetingRoomManager.Instance.AssignSelf(reporter, target);
        DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(reporter);
        reporter.RpcStartMeeting(target);
    }
    public static bool IsModClient(this PlayerControl player) => TOHPlugin.playerVersion.ContainsKey(player.PlayerId);
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


    //汎用
    public static bool Is(this PlayerControl target, CustomRole role) =>
        role.IsSubrole ? target.GetCustomSubRoles().Contains(role) : target.GetCustomRole() == role;
    public static bool Is(this PlayerControl target, Roles.RoleType type) { return target.GetCustomRole().GetRoleType() == type; }
    public static bool IsAlive(this PlayerControl target) => target != null && !target.Data.IsDead && !target.Data.Disconnected;

}