using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using Il2CppSystem;
using TownOfHost.Extensions;
using TownOfHost.ReduxOptions;
using TownOfHost.Options;
using TownOfHost.RPC;

namespace TownOfHost.Roles;

public class Amnesiac : CustomRole
{
    private bool stealExactRole;

    [RoleAction(RoleActionType.AnyReportedBody)]
    public void AmnesiacRememberAction(PlayerControl reporter, GameData.PlayerInfo reported, ActionHandle handle)
    {
        Logger.Info($"Reporter: {reporter.GetRawName()} | Reported: {reported.GetNameWithRole()} | Self: {MyPlayer.GetRawName()}", "");

        if (reporter.PlayerId != MyPlayer.PlayerId) return;
        CustomRole newRole = reported.GetCustomRole();
        if (!stealExactRole)
        {
            if (newRole.SpecialType == SpecialType.NeutralKilling) { }
            else if (newRole.SpecialType == SpecialType.Neutral)
                newRole = CustomRoleManager.Static.Opportunist;
            else if (newRole.IsCrewmate())
                newRole = CustomRoleManager.Static.Sheriff;
            else
                newRole = Ref<Traitor>();
        }

        MyPlayer.GetNameWithRole().DebugLog("My Player");

        newRole = CustomRoleManager.PlayersCustomRolesRedux[MyPlayer.PlayerId] = newRole.Instantiate(MyPlayer);
        MessageWriter messageWriter = MessageWriter.Get(SendOption.Reliable); // Start message writer
        messageWriter.StartMessage(5); // Initial control-flow path for packet receival (Line 1352 InnerNetClient.cs) || This can be changed to "5" and remove line 20 to sync options to everybody
        messageWriter.Write(AmongUsClient.Instance.GameId); // Write 4 byte GameId
        messageWriter.WritePacked(MyPlayer.GetClientId()); // Target player ID

        messageWriter.StartMessage(1); // Second control-flow path specifically for changing game options
        messageWriter.WritePacked(DesyncOptions.GetTargetedClientId("GameData")); // Packed ID for game manager

        messageWriter.StartMessage(MyPlayer.PlayerId);
        MyPlayer.Data.Role.Role = RoleTypes.Impostor;
        MyPlayer.Data.Serialize(messageWriter);

        messageWriter.EndMessage(); // Finish message 1
        messageWriter.EndMessage(); // Finish message 2
        messageWriter.EndMessage(); // Finish message 3
        AmongUsClient.Instance.SendOrDisconnect(messageWriter); // Wrap up send
        messageWriter.Recycle(); // Recycle
        //Utils.NotifyRoles(true, MyPlayer);


        //MyPlayer.MyPhysics.RpcExitVent(2);

        Utils.SendMessage("You joined that person's team! They were " + newRole.RoleName + ".", MyPlayer.PlayerId);
        Utils.SendMessage("The Amnesiac stole your role! Because of this, your role has been reset to the default one.", reported.PlayerId);
        MyPlayer.GetNameWithRole().DebugLog("My Player");
        handle.Cancel();
    }

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
        .Tab(DefaultTabs.NeutralTab)
            .AddSubOption(sub => sub.Name("Steals Exact Role")
                .Bind(v => stealExactRole = (bool)v)
                .AddOnOffValues(false).Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) => roleModifier.RoleColor("#81DDFC");
}