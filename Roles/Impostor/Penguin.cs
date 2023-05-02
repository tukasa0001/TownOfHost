using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Impostor;

class Penguin : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
    new(
        typeof(Penguin),
        player => new Penguin(player),
        CustomRoles.Penguin,
        () => RoleTypes.Shapeshifter,
        CustomRoleTypes.Impostor,
        3100,
        SetupOptionItem
    );
    public Penguin(PlayerControl player)
        : base(RoleInfo, player)
    {
        AbductTimerLimit = OptionAbductTimerLimit.GetFloat();
        Penguins.Add(this);
    }
    public override void OnDestroy()
    {
        AbductVictim = null;
        Penguins.Remove(this);
    }

    static OptionItem OptionAbductTimerLimit;

    enum OptionName
    {
        PenguinAbductTimerLimit,
    }

    PlayerControl AbductVictim;
    float AbductTimer;
    float AbductTimerLimit;
    bool stopCount;
    static List<Penguin> Penguins = new();
    public static void SetupOptionItem()
    {
        OptionAbductTimerLimit = FloatOptionItem.Create(RoleInfo, 11, OptionName.PenguinAbductTimerLimit, new(5f, 20f, 1f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        AbductTimer = 255f;
        stopCount = false;
    }
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.ShapeshifterCooldown = AbductVictim != null ? AbductTimer : 255f;

    void AddVictim(PlayerControl target)
    {
        AbductVictim = target;
        AbductTimer = AbductTimerLimit;

    }
    void RemoveVictim()
    {
        AbductVictim = null;
        AbductTimer = 255f;
    }
    void AbductOrKill(PlayerControl target)
    {
    }
    public override void OnCheckMurderAsKiller(MurderInfo info)
    {
        var target = info.AttemptTarget;
        if (AbductVictim != null)
        {
            RemoveVictim();
        }
        else
        {
            info.DoKill = false;
            AddVictim(target);
        }
        Player.SyncSettings();
        Player.RpcResetAbilityCooldown();
    }
    public override void OnMurderPlayerAsKiller(MurderInfo info)
    {
        RemoveVictim();
    }
    public override string GetAbilityButtonText()
    {
        return base.GetAbilityButtonText();
    }
    public override void OnStartMeeting()
    {
        stopCount = true;
    }
    public void OnSpawn()
    {
        if (Main.NormalOptions.MapId == 4) return;

        //マップがエアシップ以外
        RestartAbduct();
    }
    public void OnSpawnAirship(byte penguinId)
    {
        RestartAbduct();
    }
    public void RestartAbduct()
    {
        foreach (var penguin in Penguins)
        {
            if (penguin.AbductVictim != null)
            {
                penguin.Player.SyncSettings();
                penguin.Player.RpcResetAbilityCooldown();
                penguin.stopCount = false;
            }
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask) return;

        if (!stopCount)
            AbductTimer -= Time.fixedDeltaTime;

        if (AbductVictim != null)
        {
            if (!Player.IsAlive())
            {
                RemoveVictim();
                return;
            }
            if (AbductTimer <= 0f)
                AbductOrKill(AbductVictim);
            else
            {
                var position = Player.transform.position;
                if (Player.PlayerId != 0)
                    RandomSpawn.TP(AbductVictim.NetTransform, position);
                else
                    new LateTask(() =>
                    RandomSpawn.TP(AbductVictim.NetTransform, position)
                    , 0.25f, "");
            }
        }
        else
            if (AbductTimer <= 100f)
        {
            AbductTimer = 255f;
            Player.RpcResetAbilityCooldown();
        }
    }
}
