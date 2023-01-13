using System.Linq;
using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.GUI;
using TownOfHost.Managers;
using TownOfHost.Options;
using UnityEngine;
using Convert = System.Convert;

namespace TownOfHost.Roles;

public class Sniper: Morphling
{
    private bool preciseShooting = true;
    [DynElement(UI.Cooldown)]
    private Cooldown loadBulletCooldown;
    private Cooldown killCooldown;

    private int totalBulletCount = 10;
    private int loadedBullets;
    private int maxLoadedBullets;
    private int sniperMode;
    private bool canBeVetted;

    private float realKillCooldown;
    private int currentBulletCount;
    private Vector2 lastDirection;

    [DynElement(UI.Counter)]
    private string BulletCountCounter() => RoleUtils.Counter(currentBulletCount, totalBulletCount);

    [DynElement(UI.Misc)]
    private string LoadedBulletDisplay() => Color.red.Colorize("▫".Repeat(loadedBullets));

    protected override void Setup(PlayerControl player)
    {
        currentBulletCount = totalBulletCount;
        realKillCooldown = GameOptionsManager.Instance.currentGameOptions.GetFloat(FloatOptionNames.KillCooldown);
        killCooldown.Duration = realKillCooldown;
    }

    [RoleAction(RoleActionType.AttemptKill)]
    private bool TryKill(PlayerControl target)
    {
        killCooldown.Start();
        return base.TryKill(target);
    }

    [RoleAction(RoleActionType.OnPet)]
    private void LoadBullet()
    {

        if (currentBulletCount == 0 || loadBulletCooldown.NotReady() || loadedBullets >= maxLoadedBullets || sniperMode == 0) return;
        loadedBullets++;
        currentBulletCount--;
        loadBulletCooldown.Start();
        GameOptionOverride[] killCooldown = { new(Override.KillCooldown, loadBulletCooldown.Duration * 2) };
        DesyncOptions.SendModifiedOptions(killCooldown, MyPlayer);
        MyPlayer.RpcGuardAndKill();
    }

    [RoleAction(RoleActionType.FixedUpdate)]
    private void SniperDirectionUpdate()
    {
        if (MyPlayer.MyPhysics.Velocity.magnitude != 0)
            lastDirection = MyPlayer.MyPhysics.Velocity;
    }

    [RoleAction(RoleActionType.Shapeshift)]
    private bool FireBullet(ActionHandle handle)
    {
        handle.Cancel();
        if (sniperMode == 1)
        {
            if (loadedBullets == 0 || killCooldown.NotReady() || loadBulletCooldown.NotReady()) return false;
            loadedBullets--;
        }
        else
        {
            if (currentBulletCount <= 0 || killCooldown.NotReady()) return false;
            currentBulletCount--;
        }

        Vector2 dir = lastDirection != null ? lastDirection : MyPlayer.MyPhysics.Velocity;
        bool killed = false;

        foreach (PlayerControl target in Game.GetAllPlayers().Where(p => p.PlayerId != MyPlayer.PlayerId && !p.GetCustomRole().IsAllied(MyPlayer)))
        {
            Vector3 targetPos = target.transform.position - MyPlayer.transform.position;
            Vector3 targetDirection = targetPos.normalized;
            float dotProduct = Vector3.Dot(dir, targetDirection);
            float error = !preciseShooting ? targetPos.magnitude : Vector3.Cross(dir, targetPos).magnitude;
            if (dotProduct < 0.98 || (error < 1.0 && preciseShooting)) continue;
            InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
            if (result == InteractionResult.Halt) return killed;
            /*PlayerStateOLD.SetDeathReason(target.PlayerId, PlayerStateOLD.DeathReason.Sniped);*/
            target.RpcMurderPlayer(target);
            MyPlayer.RpcGuardAndKill();
            killed = true;
        }

        float refundCooldown = realKillCooldown * 0.5f;
        GameOptionOverride[] modifiedCooldown = { new(Override.KillCooldown, refundCooldown) };
        DesyncOptions.SendModifiedOptions(modifiedCooldown, MyPlayer);
        killCooldown.Start(refundCooldown * 0.5f);

        DTask.Schedule(() => MyPlayer.RpcRevertShapeshift(true), 0.3f);
        DTask.Schedule(this.SyncOptions, 1f);

        return true;
    }

    [RoleInteraction(typeof(Veteran))]
    public InteractionResult VeteranSnipedInteraction(PlayerControl veteran)
    {
        if (!canBeVetted) return InteractionResult.Proceed;
        return veteran.GetCustomRole<Veteran>().TryKill(MyPlayer) ? InteractionResult.Halt : InteractionResult.Proceed;
    }



    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .AddSubOption(sub => sub
                .Name("Sniper Bullet Count")
                .Bind(v => totalBulletCount = (int)v)
                .AddValues(5, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)
                .Build())
            .AddSubOption(sub => sub
                .Name("Precise Shooting")
                .Bind(v => preciseShooting = (bool)v)
                .AddOnOffValues()
                .Build())
            .AddSubOption(sub => sub
                .Name("Can Be Vetted On Snipe")
                .Bind(v => canBeVetted = (bool)v)
                .AddOnOffValues()
                .Build())
            .AddSubOption(sub => sub
                .Name("Sniper Mode")
                .Bind(v => sniperMode = (int)v)
                .AddValue(v => v.Text("Normal Mode").Value(0).Build())
                .AddValue(v => v.Text("Load Bullet Mode").Value(1).Build())
                .ShowSubOptionsWhen(obj => SniperMode.LoadBullet == (SniperMode)obj)
                .AddSubOption(sub2 => sub2
                    .Name("Load Bullet Cooldown")
                    .Bind(v => loadBulletCooldown.Duration = Convert.ToSingle(v))
                    .AddValues(5, 5, 7.5, 10, 12.5, 15, 17.5, 20, 22.5, 25, 27.5, 30)
                    .Build())
                .AddSubOption(sub2 => sub2
                    .Name("Max Loaded Bullets")
                    .Bind(v => maxLoadedBullets = (int)v)
                    .AddValues(0, 1, 2, 3, 4, 5)
                    .Build())
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleName("Sniper");

    private enum SniperMode
    {
        Normal,
        LoadBullet,
    }
}