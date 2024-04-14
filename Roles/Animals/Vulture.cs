using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmongUs.GameOptions;
using Hazel;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHostForE.Roles.Animals
{
    public sealed class Vulture : RoleBase
    {

        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Vulture),
                player => new Vulture(player),
                CustomRoles.Vulture,
                () => RoleTypes.Engineer,
                CustomRoleTypes.Animals,
                24200,
                SetupOptionItem,
                "バルチャー",
                "#FF8C00",
                countType: CountTypes.Crew,
            introSound: () => ShipStatus.Instance.CommonTasks.Where(task => task.TaskType == TaskTypes.FixWiring).FirstOrDefault().MinigamePrefab.OpenSound
            );
        public Vulture(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.ForRecompute
        )
        {
            eatCount = 0;
            IsComplete = false;
            ColorUtility.TryParseHtmlString("#FF8C00", out AnimalsColor);
        }
        enum OptionName
        {
            VictoryEatDeadBody,
        }

        //Option
        public static OptionItem VictoryEatDeadBody;
        private static HashSet<DeadBody> DeadBodyList = new();


        public static int eatCount = 0;
        public static bool IsComplete = false;

        //パラム
        private static Options.OverrideTasksData Tasks;

        Color AnimalsColor;


        private static void SetupOptionItem()
        {
            VictoryEatDeadBody = IntegerOptionItem.Create(RoleInfo, 10, OptionName.VictoryEatDeadBody, new(1, 15, 1), 3, false)
                .SetValueFormat(OptionFormat.Players);
            // 100-103を使用
            Tasks = Options.OverrideTasksData.Create(RoleInfo, 100);
        }

        public override void Add()
        {
            var playerId = Player.PlayerId;

            eatCount = 0;
            IsComplete = false;
            DeadBodyList.Clear();
            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : {eatCount}人食べる", "Vulture");
        }
        private void SendRPC()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            using var sender = CreateSender(CustomRPC.VultureSync);
            sender.Writer.Write(eatCount);
        }
        public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
        {
            if (rpcType != CustomRPC.VultureSync) return;

            eatCount = reader.ReadInt32();
        }

        public override bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
        {
            if (reporter != Player) return true;
            if (target == null) return true;

            eatCount++;
            DeadBody[] AllBody = Object.FindObjectsOfType<DeadBody>();

            var pos = reporter.transform.position;

            var seerTask = reporter.GetPlayerTaskState();

            foreach (var targetBody in AllBody)
            {
                var dis = Vector2.Distance(pos, targetBody.transform.position);
                if (dis > 5.0f) continue;
                TargetArrow.Remove(reporter.PlayerId, targetBody);
            }

            ReportDeadBodyPatch.CanReportByDeadBody[target.PlayerId] = false;
            SendRPC();
            CheckVultureWin(reporter.Data);
            Utils.NotifyRoles();
            return false;
        }
        private static void CheckVultureWin(GameData.PlayerInfo Vulture)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (eatCount >= VictoryEatDeadBody.GetInt())
            {
                AnimalsBomb(CustomDeathReason.Bombed);
                AnimalsWin();
            }
        }
        public static void AnimalsBomb(CustomDeathReason dr)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                var cRole = pc.GetCustomRole();
                var playerState = PlayerState.GetByPlayerId(pc.PlayerId);
                if (cRole.IsAnimals())
                {
                    playerState.DeathReason = CustomDeathReason.Kill;
                }
                else if (!pc.Data.IsDead)
                {
                    //生存者は爆死
                    pc.RpcMurderPlayer(pc);
                    playerState.DeathReason = CustomDeathReason.Kill;
                    playerState.SetDead();
                }
            }
            AnimalsWin();
        }
        public override string GetProgressText(bool comms = false) => Utils.ColorString(Color.yellow, $"({eatCount}/{VictoryEatDeadBody.GetInt()})");
        public static void AnimalsWin()
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Animals);
            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Vulture);
            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Coyote);
            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.ASchrodingerCat);
            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.AOjouSama);
            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Badger);
            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Leopard);
            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Braki);
            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.RedPanda);
            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Nyaoha);
        }
        public override bool OnCompleteTask()
        {
            bool update = false;
            if (!IsComplete && IsTaskFinished)
            {
                IsComplete = true;
                update = true;
            }
            if (update) Utils.NotifyRoles();
            return true;
        }

        public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        {
            //矢印表示する必要がなければ無し
            if (isForMeeting) return "";

            //seenが省略の場合seer
            seen ??= seer;

            //タスク終わってなければ無し
            if (!IsComplete) return "";

            var arrows = GetArrows();
            return arrows;
        }
        private string GetArrows()
        {

            DeadBody[] AllBody = Object.FindObjectsOfType<DeadBody>();
            DeadBody targetBody = null;

            foreach (var body in AllBody)
            {
                if (!DeadBodyList.Contains(body))
                {
                    DeadBodyList.Add(body);
                    targetBody = body;
                    TargetArrow.Add(Player.PlayerId, targetBody);
                    break;
                }
            }

            var sb = new StringBuilder(80);

            if (targetBody != null)
            {
                sb.Append(Utils.ColorString(AnimalsColor, TargetArrow.GetArrows(Player, targetBody)));
            }

            return sb.ToString();
        }

        //死体が生まれた時、発見される側の死体リストに追加
        public static void UpdateDeadBody()
        {
            if (!CustomRoles.Vulture.IsEnable()) return;

            DeadBody[] AllBody = Object.FindObjectsOfType<DeadBody>();
            DeadBody targetBody = null;

            foreach (var body in AllBody)
            {
                if (!DeadBodyList.Contains(body))
                {
                    DeadBodyList.Add(body);
                    targetBody = body;
                    break;
                }
            }

            foreach (var pc in Main.AllAlivePlayerControls)
            {
                var cRole = pc.GetCustomRole();
                if (cRole != CustomRoles.Vulture) continue;
                TargetArrow.Add(pc.PlayerId, targetBody);
            }

            Utils.NotifyRoles();
        }
    }
}
