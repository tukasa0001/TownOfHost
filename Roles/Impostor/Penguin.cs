using UnityEngine;
using AmongUs.GameOptions;
using Hazel;

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
        MeetingKill = OptionMeetingKill.GetBool();
    }
    public override void OnDestroy()
    {
        AbductVictim = null;
    }

    static OptionItem OptionAbductTimerLimit;
    static OptionItem OptionMeetingKill;

    enum OptionName
    {
        PenguinAbductTimerLimit,
        PenguinMeetingKill,
    }

    private PlayerControl AbductVictim;
    private float AbductTimer;
    private float AbductTimerLimit;
    private bool stopCount;
    private bool MeetingKill;

    //拉致中にキルしそうになった相手の能力を使わせないための処置
    public bool IsKiller => AbductVictim == null;
    public static void SetupOptionItem()
    {
        OptionAbductTimerLimit = FloatOptionItem.Create(RoleInfo, 11, OptionName.PenguinAbductTimerLimit, new(5f, 20f, 1f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionMeetingKill = BooleanOptionItem.Create(RoleInfo, 12, OptionName.PenguinMeetingKill, false, false);
    }
    public override void Add()
    {
        AbductTimer = 255f;
        stopCount = false;
    }
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.ShapeshifterCooldown = AbductVictim != null ? AbductTimer : 255f;
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.PenguinSync);

        sender.Writer.Write(AbductVictim?.PlayerId ?? 255);
    }

    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.PenguinSync) return;

        var victim = reader.ReadByte();
        if (victim == 255)
        {
            AbductVictim = null;
            AbductTimer = 255f;
        }
        else
        {
            AbductVictim = Utils.GetPlayerById(victim);
            AbductTimer = AbductTimerLimit;
        }
    }
    void AddVictim(PlayerControl target)
    {
        PlayerState.GetByPlayerId(target.PlayerId).CanUseMovingPlatform = MyState.CanUseMovingPlatform = false;
        AbductVictim = target;
        AbductTimer = AbductTimerLimit;
        Player.SyncSettings();
        Player.RpcResetAbilityCooldown();
        SendRPC();
    }
    void RemoveVictim()
    {
        if (AbductVictim != null)
        {
            PlayerState.GetByPlayerId(AbductVictim.PlayerId).CanUseMovingPlatform = true;
            AbductVictim = null;
        }
        MyState.CanUseMovingPlatform = true;
        AbductTimer = 255f;
        Player.SyncSettings();
        Player.RpcResetAbilityCooldown();
        SendRPC();
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var target = info.AttemptTarget;
        if (AbductVictim != null)
        {
            if (target != AbductVictim)
            {
                //拉致中は拉致相手しか切れない
                Player.RpcMurderPlayer(AbductVictim);
                Player.ResetKillCooldown();
                info.DoKill = false;
            }
            RemoveVictim();
        }
        else
        {
            info.DoKill = false;
            AddVictim(target);
        }
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
    public override void OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        stopCount = true;
        // 時間切れ状態で会議を迎えたらはしご中でも構わずキルする
        if (AbductVictim != null && AbductTimer <= 0f)
        {
            Player.RpcMurderPlayer(AbductVictim);
        }
        if (MeetingKill)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (AbductVictim == null) return;
            Player.RpcMurderPlayer(AbductVictim);
            RemoveVictim();
        }
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
        if (!AmongUsClient.Instance.AmHost) return;
        if (!GameStates.IsInTask) return;

        if (!stopCount)
            AbductTimer -= Time.fixedDeltaTime;

        if (AbductVictim != null)
        {
            if (!Player.IsAlive() || !AbductVictim.IsAlive())
            {
                RemoveVictim();
                return;
            }
            if (AbductTimer <= 0f && !Player.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
            {
                // 先にIsDeadをtrueにする(はしごチェイス封じ)
                AbductVictim.Data.IsDead = true;
                GameData.Instance.SetDirty();
                // ペンギン自身がはしご上にいる場合，はしごを降りてからキルする
                if (!AbductVictim.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
                {
                    var abductVictim = AbductVictim;
                    _ = new LateTask(() =>
                    {
                        var sId = abductVictim.NetTransform.lastSequenceId + 5;
                        abductVictim.NetTransform.SnapTo(Player.transform.position, (ushort)sId);
                        Player.MurderPlayer(abductVictim);

                        var sender = CustomRpcSender.Create("PenguinMurder");
                        {
                            sender.AutoStartRpc(abductVictim.NetTransform.NetId, (byte)RpcCalls.SnapTo);
                            {
                                NetHelpers.WriteVector2(Player.transform.position, sender.stream);
                                sender.Write(abductVictim.NetTransform.lastSequenceId);
                            }
                            sender.EndRpc();
                            sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.MurderPlayer);
                            {
                                sender.WriteNetObject(abductVictim);
                            }
                            sender.EndRpc();
                        }
                        sender.SendMessage();
                    }, 0.3f, "PenguinMurder");
                    RemoveVictim();
                }
            }
            // はしごの上にいるプレイヤーにはSnapToRPCが効かずホストだけ挙動が変わるため，一律でテレポートを行わない
            else if (!AbductVictim.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
            {
                var position = Player.transform.position;
                if (Player.PlayerId != 0)
                {
                    RandomSpawn.TP(AbductVictim.NetTransform, position);
                }
                else
                {
                    _ = new LateTask(() =>
                    {
                        if (AbductVictim != null)
                            RandomSpawn.TP(AbductVictim.NetTransform, position);
                    }
                    , 0.25f, "");
                }
            }
        }
        else if (AbductTimer <= 100f)
        {
            AbductTimer = 255f;
            Player.RpcResetAbilityCooldown();
        }
    }
}
