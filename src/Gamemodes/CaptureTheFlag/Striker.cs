using System.Linq;
using AmongUs.GameOptions;
using TOHTOR.Extensions;
using TOHTOR.GUI;
using TOHTOR.Roles;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Options.Game;
using VentLib.Utilities;
using Impostor = TOHTOR.Roles.RoleGroups.Vanilla.Impostor;

namespace TOHTOR.Gamemodes.CaptureTheFlag;

public class Striker: Impostor
{
    private Cooldown gameTimer;
    [DynElement(UI.Cooldown)]
    private Cooldown speedCooldown;
    private float speedAdditive;
    private float duration;

    [DynElement(UI.Counter)]
    private string ShowGameTimer() => Color.white.Colorize($"{gameTimer}s");

    protected override void Setup(PlayerControl myPlayer)
    {
        this.gameTimer.Duration = CTFGamemode.GameDuration;
        this.gameTimer.Start();
    }

    [RoleAction(RoleActionType.Attack)]
    private void SendBackToSpawn(PlayerControl target)
    {
        int targetTeam = target.cosmetics.bodyMatProperties.ColorId;
        Utils.Teleport(target.NetTransform, CTFGamemode.SpawnLocations[targetTeam]);

        if (CTFGamemode.Carriers[targetTeam] == target.PlayerId) CTFGamemode.Carriers[targetTeam] = 255;

        GameOptionOverride[] overrides = { new(Override.PlayerSpeedMod, 0f) };
        target.GetCustomRole().SyncOptions(overrides);

        Async.Schedule(() => target.GetCustomRole().SyncOptions(), 3f);
    }

    [RoleAction(RoleActionType.SelfReportBody)]
    private void ReportBody(ActionHandle handle, GameData.PlayerInfo body)
    {
        int myTeam = MyPlayer.cosmetics.bodyMatProperties.ColorId;
        int otherTeam = myTeam == 0 ? 1 : 0;

        bool grabbedFlag = RoleUtils.GetPlayersWithinDistance(CTFGamemode.BodyLocations[otherTeam], 3f).Any(p => p.PlayerId == MyPlayer.PlayerId);
        if (grabbedFlag && CTFGamemode.Carriers[myTeam] == 255) CTFGamemode.GrabFlag(MyPlayer);

        Object.FindObjectsOfType<DeadBody>().FirstOrDefault(b => b.ParentId == body.PlayerId)!.Reported = false;
        handle.Cancel();
    }

    [RoleAction(RoleActionType.OnPet)]
    private void ActivateSpeedBoost()
    {
        if (speedCooldown.NotReady()) return;
        speedCooldown.Start();
        AddOverride(new(Override.PlayerSpeedMod, 5f));
        SyncOptions();
        Async.Schedule(() =>
        {
            RemoveOverride(Override.PlayerSpeedMod);
            SyncOptions();
        }, duration);
    }

    [RoleAction(RoleActionType.FixedUpdate)]
    private void PlayerFixedUpdate()
    {
        int myTeam = MyPlayer.cosmetics.bodyMatProperties.ColorId;
        int otherTeam = myTeam == 0 ? 1 : 0;
        bool inSpawn = RoleUtils.GetPlayersWithinDistance(CTFGamemode.SpawnLocations[myTeam], 2f).Any(p => p.PlayerId == MyPlayer.PlayerId);
        if (inSpawn && CTFGamemode.Carriers[myTeam] == MyPlayer.PlayerId) {
            CTFGamemode.TeamPoints[myTeam]++; CTFGamemode.Carriers[myTeam] = 255;
        }

        bool inEnemySpawn = RoleUtils.GetPlayersWithinDistance(CTFGamemode.BodyLocations[otherTeam], 0.5f).Any(p => p.PlayerId == MyPlayer.PlayerId);
        if (inEnemySpawn && CTFGamemode.Carriers[myTeam] == 255) CTFGamemode.GrabFlag(MyPlayer);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(CTFGamemode.CTFTab)
            .SubOption(sub => sub
                .Name("Kill Cooldown")
                .BindFloat(v => this.KillCooldown = v)
                .AddFloatRange(0.5f, 10f, 0.25f, 4, "s")
                .Build())
            .SubOption(sub => sub
                .Name("Speed Boost Additive")
                .AddFloatRange(0, 1.0f, 0.1f, 3, "x")
                .BindFloat(v => speedAdditive = v)
                .Build())
            .SubOption(sub => sub
                .Name("Speed Boost Duration")
                .AddFloatRange(0, 10, 0.25f, 6, "s")
                .BindFloat(v => duration = v)
                .Build())
            .SubOption(sub => sub
                .Name("Speed Boost Cooldown")
                .BindFloat(v => speedCooldown.Duration = v)
                .AddFloatRange(0, 120, 2.5f, 16, "s")
                .Build());

    protected override RoleModifier Modify(RoleModifier modifier) =>
        base.Modify(modifier).VanillaRole(RoleTypes.Impostor);
}