using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.GUI;
using TOHTOR.Options;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Logging;
using VentLib.Options.Game;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Roles;

[Localized("Roles.Vigilante")]
public class Vigilante: CustomRole
{

    [Localized("SelectPlayer")]
    private static string selectPlayerMsg = "You have selected to kill:";
    [Localized("SelectRole")]
    private static string selectRoleMsg = "You are guessing:";

    [Localized("VoteRoleInfo")]
    private static string voteRoleInfo =
        "To select a role, vote a player with the role over their name. If they have multiple roles over their name, voting that same player again will select the next role displayed.";
    [Localized("SkipToContinue")]
    private static string skipMsg = "Press \"Skip Vote\" to continue.";

    private List<CustomRole>[] roles = null!;
    private Optional<PlayerControl> playerSelected = Optional<PlayerControl>.Null();
    private VotingState votingState = VotingState.SelectingTarget;

    private int roleSelected;
    private byte lastPlayer = 255;


    [RoleAction(RoleActionType.RoundStart)]
    private void SetupRoleNames(bool isRoundOne)
    {
        if (!isRoundOne)
        {
            playerSelected = Optional<PlayerControl>.Null();
            votingState = VotingState.SelectingTarget;
            roleSelected = 0;
            lastPlayer = 255;
            return;
        }
        List<CustomRole> enabledRoles = Api.Roles.GetEnabledRoles().Sorted(r => r.RoleName).ToList();
        roles = new List<CustomRole>[PlayerControl.AllPlayerControls.Count];

        int evenD = Mathf.FloorToInt((float)enabledRoles.Count / roles.Length);
        int remainder = enabledRoles.Count % roles.Length;

        for (int i = 0; i < roles.Length; i++)
        {
            var list = roles[i] = new List<CustomRole>();
            for (int j = 0; j < evenD + Mathf.Clamp(remainder--, 0, Int32.MaxValue); j++) list.Add(enabledRoles.Pop(0));
        }

        List<PlayerControl> players = Game.GetAllPlayers().ToList();

        foreach (var tuple in roles.Indexed())
        {
            string name = tuple.item.Select(r => r.RoleColor.Colorize(r.RoleName)).Join();
            DynamicName dynName = players[tuple.index].GetDynamicName();

            dynName.AddRule(GameState.InMeeting, UI.Name, new DynamicString(name), MyPlayer.PlayerId);
            dynName.AddRule(GameState.InMeeting, UI.Role, new DynamicString(dynName.RawName), MyPlayer.PlayerId);
        }
    }

    [RoleAction(RoleActionType.MyVote)]
    private void VoteGuessing(Optional<PlayerControl> player, ActionHandle handle)
    {
        VentLogger.Debug($"{MyPlayer.GetNameWithRole()} Guessing State {votingState} | Target: {player.Map(p => p.GetNameWithRole())}", "Guesser");
        if (votingState is not VotingState.Finished) handle.Cancel();

        switch (votingState)
        {
            case VotingState.SelectingTarget:
                if (!player.Exists()) {
                    votingState = playerSelected.Exists() ? VotingState.SelectingRole : VotingState.Finished;
                    Utils.SendMessage(voteRoleInfo, MyPlayer.PlayerId);
                    break;
                }
                playerSelected = player;
                string targetPlayerMessage = $"{selectPlayerMsg} {player.Get().GetRawName()}\n{skipMsg}";
                Utils.SendMessage(targetPlayerMessage, MyPlayer.PlayerId);
                break;
            case VotingState.SelectingRole:
                if (!player.Exists()) {
                    votingState = VotingState.Finished;
                    TryAssassinate();
                    break;
                }

                byte lp = player.Get().PlayerId;
                List<CustomRole> catRoles = roles[lp];

                roleSelected = lp == lastPlayer ? roleSelected + 1 : 0;
                if (roleSelected >= catRoles.Count) roleSelected = 0;
                lastPlayer = lp;
                string targetRoleMessage = $"{selectRoleMsg} {catRoles[roleSelected].RoleName}\n{skipMsg}";
                Utils.SendMessage(targetRoleMessage, MyPlayer.PlayerId);
                break;
            case VotingState.Finished:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        VentLogger.Debug($"Updated Player State: {votingState}", "Guesser");
    }


    private void TryAssassinate()
    {
        VentLogger.Debug($"{MyPlayer.GetNameWithRole()} => {playerSelected.Map(ps => ps.GetNameWithRole())}", "TryAssassinate");
        try {
            List<CustomRole> catRoles = roles[lastPlayer];
            CustomRole selectedRole = catRoles[roleSelected];
            PlayerControl murderedPlayer = playerSelected.Get().GetCustomRole() == selectedRole ? playerSelected.Get() : MyPlayer;
            Game.GetAllPlayers().Do(p => p.RpcSpecificMurderPlayer(murderedPlayer));
        } catch (Exception exception) {
            VentLogger.Exception(exception, "Error Assassinating", "Guesser");
            Utils.SendMessage("An error has occured during assassination. Please report this to the host at the end of the game.", MyPlayer.PlayerId);
        }
    }


    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.CrewmateTab);

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier.RoleColor(Color.cyan);

    private enum VotingState
    {
        SelectingTarget,
        SelectingRole,
        Finished
    }
}