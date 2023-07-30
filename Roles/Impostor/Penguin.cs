using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor;

class Penguin : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Penguin),
            player => new Penguin(player),
            CustomRoles.Penguin,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            3400,
            SetupOptionItem,
            "pe"
        );
    public Penguin(PlayerControl player)
        : base(RoleInfo, player)
    {
        AbductTimerLimit = OptionAbductTimerLimit.GetFloat();
    }
    public override void OnDestroy()
    {
        AbductVictim = null;
    }

    static OptionItem OptionAbductTimerLimit;

    enum OptionName
    {
        PenguinAbductTimerLimit,
    }

    private PlayerControl AbductVictim;
    private float AbductTimer;
    private float AbductTimerLimit;
    private bool stopCount;
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
        Player.SyncSettings();
        Player.RpcResetAbilityCooldown();
    }
    void RemoveVictim()
    {
        AbductVictim = null;
        AbductTimer = 255f;
        Player.SyncSettings();
        Player.RpcResetAbilityCooldown();
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
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
    }
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        RemoveVictim();
    }
    public bool OverrideKillButtonText(out string text)
    {
        if (AbductVictim != null)
        {
            text = GetString("KillButtonText");
        }
        else
        {
            text = GetString("PenguinKillButtonText");
        }
        return true;
    }
    public override string GetAbilityButtonText()
    {
        return GetString("PenguinTimerText");
    }
    public override bool CanUseAbilityButton()
    {
        return AbductVictim != null;
    }
    public override void OnStartMeeting()
    {
        stopCount = true;
    }
    public override void AfterMeetingTasks()
    {
        if (Main.NormalOptions.MapId == 4) return;

        //マップがエアシップ以外
        RestartAbduct();
    }
    public void OnSpawnAirship()
    {
        RestartAbduct();
    }
    public void RestartAbduct()
    {
        if (AbductVictim != null)
        {
            Player.SyncSettings();
            Player.RpcResetAbilityCooldown();
            stopCount = false;
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask) return;

        if (AbductVictim != null)
        {
            if (!stopCount)
                AbductTimer -= Time.fixedDeltaTime;
            if (!Player.IsAlive())
            {
                RemoveVictim();
                return;
            }
            if (AbductTimer <= 0f)
            {
                Player.RpcMurderPlayerV2(AbductVictim);
                RemoveVictim();
            }
            else
            {
                var position = Player.transform.position;
                if (Player.PlayerId != 0)
                    RandomSpawn.TP(AbductVictim.NetTransform, position);
                else
                    _ = new LateTask(() =>
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
