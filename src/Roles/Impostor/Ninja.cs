using System.Collections.Generic;
using System.Linq;
using TownOfHost.Extensions;
using TownOfHost.RPC;
using TownOfHost.ReduxOptions;
using AmongUs.GameOptions;
using TownOfHost.Roles;
using TownOfHost.Interface;
using TownOfHost.Interface.Menus.CustomNameMenu;

namespace TownOfHost.Roles;

public class Ninja : Impostor
{
    private List<PlayerControl> playerList;
    public bool InKillMode;
    private bool PlayerTeleportsToNinja;
    public string CurrentSwitchMode;
    public NinjaMode Mode = NinjaMode.Killing;
    public NinjaMode previousMode = NinjaMode.Hunting;
    private readonly string[] killModes =
    {
        "Shapeshift", "Pet"
    };
    protected override void Setup(PlayerControl player) => playerList = new List<PlayerControl>();

    [RoleAction(RoleActionType.AttemptKill)]
    public override bool TryKill(PlayerControl target)
    {
        SyncOptions();
        if (InKillMode) return base.TryKill(target);
        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        if (result is InteractionResult.Halt) return false;

        playerList.Add(target);
        MyPlayer.RpcGuardAndKill(MyPlayer);
        return true;
    }

    [DynElement(UI.Misc)]
    private string CurrentMode() => Mode == NinjaMode.Hunting ? RoleColor.Colorize("(Hunting)") : RoleColor.Colorize("(Killing)");

    [RoleAction(RoleActionType.Shapeshift)]
    private void NinjaTargetCheck()
    {
        if (CurrentSwitchMode != "Shapeshift") return;
        InKillMode = false;
    }

    [RoleAction(RoleActionType.Unshapeshift)]
    private void NinjaUnShapeShift()
    {
        if (CurrentSwitchMode != "Shapeshift") return;
        InKillMode = true;
        foreach (PlayerControl player in new List<PlayerControl>(playerList))
        {
            if (player.Data.IsDead)
            {
                playerList.Remove(player);
                continue;
            }
            if (PlayerTeleportsToNinja)
            {
                Utils.Teleport(player.NetTransform, MyPlayer.transform.position);
                DTask.Schedule(() => MyPlayer.RpcMurderPlayer(player), 0.25f);
            }
            else
            {
                MyPlayer.RpcMurderPlayer(player);
            }
            playerList.Remove(player);
        }

        playerList.RemoveAll(p => p.Data.IsDead);
    }

    [RoleAction(RoleActionType.RoundStart)]
    public void EnterKillModeOnRoundStart(bool gameStart) => EnterKillMode(gameStart);

    [RoleAction(RoleActionType.OnPet)]
    public void SwitchMode()
    {
        if (CurrentSwitchMode != "Pet") return;
        "Swapping Ninja Mode".DebugLog();
        MyPlayer.name.DebugLog("My player: s");
        switch (Mode)
        {
            case NinjaMode.Killing:
                EnterHuntMode();
                break;
            case NinjaMode.Hunting:
                EnterKillMode();
                foreach (PlayerControl player in new List<PlayerControl>(playerList))
                {
                    if (player.Data.IsDead)
                    {
                        playerList.Remove(player);
                        continue;
                    }
                    if (PlayerTeleportsToNinja)
                    {
                        Utils.Teleport(player.NetTransform, MyPlayer.transform.position);
                        DTask.Schedule(() => MyPlayer.RpcMurderPlayer(player), 0.25f);
                    }
                    else
                    {
                        MyPlayer.RpcMurderPlayer(player);
                    }
                    playerList.Remove(player);
                }

                playerList.RemoveAll(p => p.Data.IsDead);
                break;
        }
    }

    [RoleAction(RoleActionType.RoundEnd)]
    private void NinjaClearTarget() => playerList.Clear();

    private void EnterKillMode(bool FirstTime = false)
    {
        InKillMode = true;
        if (FirstTime)
            RpcV2.Immediate((byte)MyPlayer.NetId, (byte)RpcCalls.SetPetStr).Write("pet_clank").Send(MyPlayer.GetClientId());
        Mode = NinjaMode.Killing;
        previousMode = NinjaMode.Hunting;
    }
    private void EnterHuntMode()
    {
        InKillMode = false;
        Mode = NinjaMode.Hunting;
        previousMode = NinjaMode.Killing;
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
        .VanillaRole(CurrentSwitchMode == "Shapeshift" ? RoleTypes.Shapeshifter : RoleTypes.Impostor);

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
        .AddSubOption(sub => sub
            .Name("Players Teleport to Ninja")
            .BindBool(v => PlayerTeleportsToNinja = v)
            .AddOnOffValues(false)
            .Build())
        .AddSubOption(sub => sub
            .Name("Ninja Switch Mode")
            .AddValues(-1, killModes)
            .Bind(v => CurrentSwitchMode = (string)v)
            .Build());

    public enum NinjaMode
    {
        Killing,
        Hunting
    }
}