using System.Collections.Generic;
using AmongUs.GameOptions;
using TownOfHostForE.Roles.Core;
using UnityEngine;

using static TownOfHostForE.Options;

namespace TownOfHostForE.Roles.Crewmate
{
    public sealed class Tiikawa : RoleBase
    {
        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Tiikawa),
                player => new Tiikawa(player),
                CustomRoles.Tiikawa,
                () => RoleTypes.Crewmate,
                CustomRoleTypes.Crewmate,
                21500,
                SetupOptionItem,
                "ちいかわ"
            );

        public Tiikawa(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            CanKillFlag = false;
            TiikawaTasks = 0;
            rabbitUragiri = Parsent.GetInt();
            rabbitKillCool = Cooldown.GetFloat();
            tiikawaTaskDelay = TiikawaTime.GetInt();
        }


        public static TiikawaInitState state = new();

        public enum TiikawaInitState
        {
            Init,
            UE1,
            UE2,
            SITA1,
            SITA2,
            HIDARI1,
            MIGI1,
            HIDARI2,
            MIGI2,
            B,
            Finished
        }

        private static OptionItem Parsent;
        private static OptionItem Cooldown;
        private static OptionItem TiikawaTime;
        private static bool CanKillFlag = false;
        private static int TiikawaTasks = 0;
        private static int rabbitUragiri;
        private static float rabbitKillCool;
        private static int tiikawaTaskDelay;

        enum OptionName
        {
            TiikawaParcent,
            TiikawaTime,
        }

        private static void SetupOptionItem()
        {
            Parsent = IntegerOptionItem.Create(RoleInfo, 10, OptionName.TiikawaParcent, new(0, 100, 10), 50, false)
                .SetValueFormat(OptionFormat.Percent);
            Cooldown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, new(0f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            TiikawaTime = IntegerOptionItem.Create(RoleInfo, 12, OptionName.TiikawaTime, new(1, 60, 1), 10, false)
                .SetValueFormat(OptionFormat.Seconds);
        }

        public override void Add()
        {
            CanKillFlag = false;
            TiikawaTasks = 0;
        }


        public static void ChangeTiikawaRole()
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                var cRole = pc.GetCustomRole();
                if (cRole == CustomRoles.Tiikawa ||
                    cRole == CustomRoles.Hachiware ||
                    cRole == CustomRoles.Usagi ||
                    cRole == CustomRoles.IUsagi)
                {

                    System.Random rand = new System.Random();
                    int firstRand = rand.Next(3);

                    switch (firstRand)
                    {
                        case 0:
                            //ちいかわ以外だったらちいかわへ
                            if (cRole != CustomRoles.Tiikawa)
                            {
                                pc.RpcSetCustomRole(CustomRoles.Tiikawa);
                            }
                            break;
                        case 1:
                            //はちわれ以外だったらはちわれへ
                            if (cRole != CustomRoles.Hachiware)
                            {
                                pc.RpcSetCustomRole(CustomRoles.Hachiware);
                            }
                            break;
                        case 2:
                            //うさぎ以外だったらうさぎへ
                            if (cRole != CustomRoles.Usagi && cRole != CustomRoles.IUsagi)
                            {
                                //C#は同じランダム変数を二回使うと変な挙動になった気がするから
                                System.Random secondRand = new System.Random();
                                int NextRand = secondRand.Next(100);
                                //指定値以下ならマッドメイト兎
                                if (NextRand <= rabbitUragiri)
                                {
                                    pc.RpcSetCustomRole(CustomRoles.IUsagi);
                                }
                                else
                                {
                                    pc.RpcSetCustomRole(CustomRoles.Usagi);
                                }
                            }
                            break;
                        default:
                            Logger.Warn("ありえない選択に入ったようだ", "Tiikawa");
                            break;
                    }
                    Logger.Info("役職を変更したよ", "Tiikawa");
                }
            }

        }

        public static void FixedUpdate(PlayerControl player)
        {
            if (player == null) return;
            if (!player.IsAlive()) return;
            var playerId = player.PlayerId;
            var cRole = player.GetCustomRole();
            //対象の役職じゃない時はリターン
            if (cRole != CustomRoles.Tiikawa &&
                cRole != CustomRoles.Usagi &&
                cRole != CustomRoles.IUsagi)
                return;

            if (cRole == CustomRoles.Tiikawa)
            {
                //int pcTasks = player.Data.Tasks.Count;
                int pcTasks = player.GetPlayerTaskState().CompletedTasksCount;
                //ちいかわのタスク数と現在のタスク数に差異がある = タスクをやった
                if (TiikawaTasks != pcTasks)
                {
                    TiikawaStopSetting(player);
                    TiikawaTasks = pcTasks;
                }
            }

            //ホスト以外は兎処理しない
            if (!AmongUsClient.Instance.AmHost) return;
            if (GameStates.IsInTask && CanKillFlag && (cRole == CustomRoles.Usagi || cRole == CustomRoles.IUsagi))
            {
                Vector2 GSpos = player.transform.position;//うさぎの位置

                PlayerControl target = null;
                var KillRange = GameOptionsData.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];

                foreach (var pc in Main.AllAlivePlayerControls)
                {
                    if (pc != player)
                    {
                        float targetDistance = Vector2.Distance(GSpos, pc.transform.position);
                        if (targetDistance <= KillRange && player.CanMove && pc.CanMove)
                        {
                            target = pc;
                            break;
                        }

                    }
                }

                if (target != null)
                {
                    player.RpcResetAbilityCooldown();
                    target.SetRealKiller(player);
                    player.RpcMurderPlayer(target);
                    Utils.MarkEveryoneDirtySettings();
                    Utils.NotifyRoles();
                    KillCoolCheck(player.PlayerId);
                }

            }
        }

        public static void TiikawaStopSetting(PlayerControl tiikawa)
        {
            Logger.Info("ちいかわがタスクをしたらしい。", "Tiikawa");
            var tmpSpeed = Main.AllPlayerSpeed[tiikawa.PlayerId];
            Main.AllPlayerSpeed[tiikawa.PlayerId] = Main.MinSpeed;    //tmpSpeedで後ほど値を戻すので代入しています。
            tiikawa.MarkDirtySettings();

            int TrapTime = tiikawaTaskDelay;

            new LateTask(() =>
            {
                Main.AllPlayerSpeed[tiikawa.PlayerId] = Main.AllPlayerSpeed[tiikawa.PlayerId] - Main.MinSpeed + tmpSpeed;
                tiikawa.MarkDirtySettings();
            }, TrapTime, "Tiikawa Stop!");
        }
        public static void KillCoolCheck(byte playerId)
        {
            CanKillFlag = false;
            float EndTime = rabbitKillCool;
            var pc = Utils.GetPlayerById(playerId);

            new LateTask(() =>
            {
                //ミーティング中なら無視。
                if (GameStates.IsMeeting) return;
                pc.RpcProtectedMurderPlayer(); //キルクが開けてしまった事が分かるように
                CanKillFlag = true;
            }, EndTime, "TiikawaKillCoolEnd");
        }

        public static void MeetingEndCheck()
        {
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                var cRole = pc.GetCustomRole();
                if (cRole == CustomRoles.Usagi ||
                    cRole == CustomRoles.IUsagi)
                {
                    KillCoolCheck(pc.PlayerId);
                }

            }
        }
    }
}
