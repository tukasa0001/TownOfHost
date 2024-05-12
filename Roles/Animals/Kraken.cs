using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using TownOfHostForE.Roles.Core.Interfaces;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Neutral;
using static TownOfHostForE.Options;
using TownOfHostForE.Roles.Crewmate;

namespace TownOfHostForE.Roles.Animals
{
    public sealed class Kraken : RoleBase, IKiller, ISchrodingerCatOwner
    {

        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Kraken),
                player => new Kraken(player),
                CustomRoles.Kraken,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Animals,
                24950,
                SetupOptionItem,
                "クラーケン",
                "#FF8C00",
                true,
                countType: killNum == 0 ? CountTypes.Animals : CountTypes.Crew
            );
        public Kraken(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            targetId = byte.MaxValue;
            setShokusyuTarget = false;
            killCount = 0;
            killNum = OptionKillNum.GetInt();
        }
        public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Animals;

        private static OptionItem OptionKillCooldown;
        private static OptionItem OptionBlindTime;
        private static OptionItem OptionKillNum;
        private static OptionItem OptionDirectTargetKill;
        private static float KillCooldown;

        private byte targetId = byte.MaxValue;
        public static HashSet<byte> ShokusyuKun = new();
        private bool setShokusyuTarget = false;
        private int killCount = 0;
        private static int killNum = 0;

        enum OptionName
        {
            BlindTime,
            KrakenKillCount,
            KrakenDirectTargetKill

        }

        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            //視界が見えない時間
            OptionBlindTime = FloatOptionItem.Create(RoleInfo, 11, OptionName.BlindTime, new(2.5f, 60f, 2.5f), 10f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionKillNum = IntegerOptionItem.Create(RoleInfo, 12, OptionName.KrakenKillCount, new(0, 14, 1), 4, false)
                .SetValueFormat(OptionFormat.Players);
            OptionDirectTargetKill = BooleanOptionItem.Create(RoleInfo, 13, OptionName.KrakenDirectTargetKill, false, false);
        }

        public override void Add()
        {
            targetId = GetTarget();
        }

        /// <summary>
        /// ターゲット取得処理
        /// 引数が設定されている場合はそのIDは除外
        /// </summary>
        /// <param name="killId">キルされる人のID</param>
        /// <returns>ID　正しい取得が出来ない時はMaxで返却される。</returns>
        public byte GetTarget()
        {
            if (!Player.IsAlive()) return byte.MaxValue;

            List<PlayerControl> targetList = new();
            var targetRand = IRandom.Instance;
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (Player.PlayerId == target.PlayerId) continue;
                targetList.Add(target);
            }
            //抽選できない数(1VS1のときとか)は処理しない
            if (targetList.Count == 0) return byte.MaxValue;
            var SelectedTarget = targetList[targetRand.Next(targetList.Count)];
            return SelectedTarget.PlayerId;
        }

        public byte SetKillTarget()
        {
            List<PlayerControl> targetList = new();
            var targetRand = IRandom.Instance;
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (Player.PlayerId == target.PlayerId) continue;
                if (target.PlayerId == targetId) continue;
                targetList.Add(target);
            }
            if (targetList.Count == 0) return byte.MaxValue;
            var SelectedTarget = targetList[targetRand.Next(targetList.Count)];
            return SelectedTarget.PlayerId;
        }
        public override string GetProgressText(bool comms = false) => killNum == 0 ? "" : $"({killCount}/{killNum})";

        private void SetShokushuTarget(PlayerControl target)
        {
            setShokusyuTarget = true;
            //アドしたタイミングで視界が奪われるはず
            ShokusyuKun.Add(target.PlayerId);
            ReportDeadBodyPatch.CanReport[target.PlayerId] = false;
            target.MarkDirtySettings();
            _ = new LateTask(() =>
            {
                if(setShokusyuTarget)RemoveShokusyuSet(targetId);
            }, OptionBlindTime.GetFloat(), "Kraken Kill");
        }
        public override void OnStartMeeting()
        {
            RemoveShokusyuSet(targetId);
        }
        public override void AfterMeetingTasks()
        {
            targetId = GetTarget();
        }
        public void RemoveShokusyuSet(byte targetId)
        {
            if (targetId == byte.MaxValue) return;
            if (!ShokusyuKun.Contains(targetId)) return;
            var target = Utils.GetPlayerById(targetId);
            ReportDeadBodyPatch.CanReport[target.PlayerId] = true;
            target.MarkDirtySettings();
            ShokusyuKun.Remove(target.PlayerId);
            RPC.PlaySoundRPC(target.PlayerId, Sounds.TaskComplete);
            setShokusyuTarget = false;
        }


        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            if (Is(info.AttemptKiller) && !info.IsSuicide)
            {
                (var killer, var target) = info.AttemptTuple;

                killer.SetKillCooldown();

                var killTargetId = SetKillTarget();
                PlayerControl killTarget = null;


                //キルした相手を直接キルする場合
                if (OptionDirectTargetKill.GetBool())
                {
                    killTarget = target;
                }
                else if (killTargetId != byte.MaxValue)
                {
                    killTarget = Utils.GetPlayerById(killTargetId);
                }

                //ターゲットが決まってない(割り振れなかった)ときは素直にキル
                if (targetId == byte.MaxValue)
                {
                    info.DoKill = true;
                    return;
                }
                else
                {
                    info.DoKill = false;
                }

                //触手くんのコントロール確保
                var shokusyuBoy = Utils.GetPlayerById(targetId);
                //触手くん死んでるか判定
                if (!shokusyuBoy.IsAlive())
                {
                    //触手くんが死んでいるならキラーも死ぬ
                    //面白いから触手に殺してもらう
                    shokusyuBoy.RpcMurderPlayer(killer);
                    return;
                }


                //生きている触手くんへ各種処理
                SetShokushuTarget(shokusyuBoy);
                //触手くんによるキル
                shokusyuBoy.RpcMurderPlayer(killTarget);
                killCount++;
                Utils.NotifyRoles(SpecifySeer:Player);
                CheckWin();
            }
        }
        public static void ApplyGameOptionsByOther(byte id, IGameOptions opt)
        {
            if (ShokusyuKun.Contains(id))
            {
                opt.SetFloat(FloatOptionNames.CrewLightMod, 0);
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0);
                opt.SetVision(false);
            }
        }

        private void CheckWin()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (killNum == 0) return;
            if (killCount >= OptionKillNum.GetInt())
            {
                Vulture.AnimalsBomb(CustomDeathReason.Bombed);
                Vulture.AnimalsWin();
            }
        }
        public float CalculateKillCooldown() => KillCooldown;
        public bool CanUseImpostorVentButton() => false;
        public bool CanUseSabotageButton() => false;
        public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);
    }
}