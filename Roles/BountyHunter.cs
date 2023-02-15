using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;

using TownOfHost.Roles;
using static TownOfHost.Options;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public sealed class BountyHunter : RoleBase
    {
        public static readonly SimpleRoleInfo RoleInfo =
            new(
                CustomRoles.BountyHunter,
                RoleType.Impostor,
                1000,
                SetupOptionItem
            );

        private static OptionItem OptionTargetChangeTime;
        private static OptionItem OptionSuccessKillCooldown;
        private static OptionItem OptionFailureKillCooldown;
        private static OptionItem OptionShowTargetArrow;

        public BountyHunter(PlayerControl player)
        : base(
            player,
            false
        )
        {
            TargetChangeTime = OptionTargetChangeTime.GetFloat();
            SuccessKillCooldown = OptionSuccessKillCooldown.GetFloat();
            FailureKillCooldown = OptionFailureKillCooldown.GetFloat();
            ShowTargetArrow = OptionShowTargetArrow.GetBool();

            ChangeTimer = OptionTargetChangeTime.GetFloat();
        }

        private static float TargetChangeTime;
        private static float SuccessKillCooldown;
        private static float FailureKillCooldown;
        private static bool ShowTargetArrow;

        public PlayerControl Target;
        public float ChangeTimer;

        private static void SetupOptionItem()
        {
            var id = RoleInfo.ConfigId;
            var tab = RoleInfo.Tab;
            var roleName = RoleInfo.RoleName;
            SetupRoleOptions(id, tab, roleName);
            var parent = RoleInfo.RoleOption;
            OptionTargetChangeTime = FloatOptionItem.Create(id + 10, "BountyTargetChangeTime", new(10f, 900f, 2.5f), 60f, tab, false).SetParent(parent)
                .SetValueFormat(OptionFormat.Seconds);
            OptionSuccessKillCooldown = FloatOptionItem.Create(id + 11, "BountySuccessKillCooldown", new(0f, 180f, 2.5f), 2.5f, tab, false).SetParent(parent)
                .SetValueFormat(OptionFormat.Seconds);
            OptionFailureKillCooldown = FloatOptionItem.Create(id + 12, "BountyFailureKillCooldown", new(0f, 180f, 2.5f), 50f, tab, false).SetParent(parent)
                .SetValueFormat(OptionFormat.Seconds);
            OptionShowTargetArrow = BooleanOptionItem.Create(id + 13, "BountyShowTargetArrow", false, tab, false).SetParent(parent);
        }
        public override void Add()
        {
            if (AmongUsClient.Instance.AmHost)
                ResetTarget();
        }
        private static void SendRPC(byte targetId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetBountyTarget, SendOption.Reliable, -1);
            writer.Write(targetId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
        {
            if (rpcType != CustomRPC.SetBountyTarget) return;

            byte targetId = reader.ReadByte();

            Target = Utils.GetPlayerById(targetId);
            if (ShowTargetArrow) TargetArrow.Add(Player.PlayerId, targetId);
        }
        //public static void SetKillCooldown(byte id, float amount) => Main.AllPlayerKillCooldown[id] = amount;
        public override void ApplyGameOptions() => AURoleOptions.ShapeshifterCooldown = TargetChangeTime;

        public override IEnumerator<int> OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            if (GetTarget() == target)
            {//ターゲットをキルした場合
                Logger.Info($"{killer?.Data?.PlayerName}:ターゲットをキル", "BountyHunter");
                Main.AllPlayerKillCooldown[killer.PlayerId] = SuccessKillCooldown;
                killer.SyncSettings();//キルクール処理を同期
                ResetTarget();
            }
            else
            {
                Logger.Info($"{killer?.Data?.PlayerName}:ターゲット以外をキル", "BountyHunter");
                Main.AllPlayerKillCooldown[killer.PlayerId] = FailureKillCooldown;
                killer.SyncSettings();//キルクール処理を同期
            }
            yield break;
        }
        public override void OnFixedUpdate()
        {
            if (GameStates.IsInTask)
            {
                var player = Player;
                if (player.IsAlive())
                {
                    var targetId = GetTarget().PlayerId;
                    if (ChangeTimer >= TargetChangeTime)//時間経過でターゲットをリセットする処理
                    {
                        ResetTarget();//ターゲットの選びなおし
                        Utils.NotifyRoles(SpecifySeer: player);
                    }
                    if (ChangeTimer >= 0)
                        ChangeTimer += Time.fixedDeltaTime;

                    //BountyHunterのターゲット更新
                    if (Main.PlayerStates[targetId].IsDead)
                    {
                        ResetTarget();
                        Logger.Info($"{player.GetNameWithRole()}のターゲットが無効だったため、ターゲットを更新しました", "BountyHunter");
                        Utils.NotifyRoles(SpecifySeer: player);
                    }
                }
            }
        }
        public PlayerControl GetTarget()
        {
            if (Target == null)
                Target = ResetTarget();

            return Target;
        }
        public PlayerControl ResetTarget()
        {
            if (!AmongUsClient.Instance.AmHost) return null;

            var playerId = Player.PlayerId;

            ChangeTimer = 0f;

            Logger.Info($"{Player.GetNameWithRole()}:ターゲットリセット", "BountyHunter");
            Player.RpcResetAbilityCooldown(); ;//タイマー（変身クールダウン）のリセットと

            var cTargets = new List<PlayerControl>(Main.AllAlivePlayerControls.Where(pc => !pc.Is(RoleType.Impostor) && !pc.Is(CustomRoles.Egoist)));

            if (cTargets.Count() >= 2)
                cTargets.RemoveAll(x => x == Target); //前回のターゲットは除外

            if (cTargets.Count <= 0)
            {
                Logger.Warn("ターゲットの指定に失敗しました:ターゲット候補が存在しません", "BountyHunter");
                return null;
            }

            var rand = IRandom.Instance;
            var target = cTargets[rand.Next(0, cTargets.Count)];
            var targetId = target.PlayerId;
            Target = target;
            if (ShowTargetArrow) TargetArrow.Add(playerId, targetId);
            Logger.Info($"{Player.GetNameWithRole()}のターゲットを{target.GetNameWithRole()}に変更", "BountyHunter");

            //RPCによる同期
            SendRPC(targetId);
            return target;
        }
        public override string GetAbilityButtonText() => GetString("BountyHunterChangeButtonText");
        public override void AfterMeetingTasks()
        {
            if (!Main.PlayerStates[Player.PlayerId].IsDead)
            {
                Player.RpcResetAbilityCooldown();
                ChangeTimer = 0f;
            }
        }
        public string GetTargetText(bool hud)
        {
            var target = GetTarget();
            return target != null ? $"{(hud ? GetString("BountyCurrentTarget") : "Target")}:{Main.AllPlayerNames[target.PlayerId]}" : "";
        }
        public override string GetTargetArrow()
        {
            if (Target != null) return "";
            if (!ShowTargetArrow || GameStates.IsMeeting) return "";

            //seerがtarget自身でBountyHunterのとき、
            //矢印オプションがありミーティング以外で矢印表示
            return TargetArrow.GetArrows(Player, Target.PlayerId);
        }
    }
}
