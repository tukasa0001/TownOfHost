using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using MS.Internal.Xml.XPath;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using TownOfHostForE.Roles.Impostor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace TownOfHostForE.Roles.Animals
{
    public sealed class Badger : RoleBase
    {

        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Badger),
                player => new Badger(player),
                CustomRoles.Badger,
                () => RoleTypes.Crewmate,
                CustomRoleTypes.Animals,
                24350,
                SetupOptionItem,
                "アナグマ",
                "#FF8C00",
                countType: CountTypes.Crew,
            introSound: () => ShipStatus.Instance.CommonTasks.Where(task => task.TaskType == TaskTypes.FixWiring).FirstOrDefault().MinigamePrefab.OpenSound
            );
        public Badger(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            BadgerHoleSetting = new();
            BadgerHoleKillFlag = new();
            BadgerHolePosition = new();
            BadgerCount = new();
            badgerRadius = BadgerRadius.GetFloat();
            badgerHoleMaxStep = BadgerHoleMaxStep.GetInt();
            vicBadgerMaxKill = VicBadgerMaxKill.GetInt();
            BadgerHoleKillCount = new();
            AlartFlag = true;
            MeetingEndFlag = true;
            AlartTarget = byte.MaxValue;
        }

        //Option
        public static OptionItem BadgerRadius;
        public static OptionItem BadgerHoleMaxStep;
        public static OptionItem VicBadgerMaxKill;
        public static OptionItem BadgerMeetingEndTime;
        private static HashSet<DeadBody> DeadBodyList = new();

        static float badgerRadius = 1;
        static int badgerHoleMaxStep = 1;
        static int vicBadgerMaxKill = 1;
        static bool AlartFlag = true;
        static byte AlartTarget = 0;
        static bool MeetingEndFlag = true;
        static bool BadgerHoleSetting = false;
        static bool BadgerHoleKillFlag = false;

        static Vector3 BadgerHolePosition = new();
        static int BadgerCount = new();
        static int BadgerHoleKillCount = new();

        enum OptionName
        {
            BadgerRadius,
            BadgerHoleMaxStep,
            VicBadgerMaxKill,
            BadgerMeetingEndTime,
        }

        private static void SetupOptionItem()
        {
            BadgerRadius = FloatOptionItem.Create(RoleInfo, 10, OptionName.BadgerRadius, new(0.5f, 20f, 0.5f), 1f, false)
                .SetValueFormat(OptionFormat.Multiplier);
            BadgerHoleMaxStep = IntegerOptionItem.Create(RoleInfo, 11, OptionName.BadgerHoleMaxStep, new(1, 20, 1), 1, false)
                .SetValueFormat(OptionFormat.None);
            VicBadgerMaxKill = IntegerOptionItem.Create(RoleInfo, 12, OptionName.VicBadgerMaxKill, new(1, 15, 1), 3, false)
                .SetValueFormat(OptionFormat.Players);
            BadgerMeetingEndTime = FloatOptionItem.Create(RoleInfo, 13, OptionName.BadgerMeetingEndTime, new(1f, 30f, 0.5f), 15f, false)
                .SetValueFormat(OptionFormat.Seconds);
        }

        public override void Add()
        {
            var playerId = Player.PlayerId;

            BadgerHolePosition = new();
            BadgerCount = new();
            BadgerHoleKillCount = new();
            BadgerHoleSetting = false;
            BadgerHoleKillFlag = false;
        }

        public override void OnTouchPet(PlayerControl player)
        {
            Logger.Info($"Badger PetTouch", "Badger");
            if (player == null || player.Data.IsDead) return;

            player.RpcProtectedMurderPlayer(); //設置が分かるように
            BadgerHolePosition = player.transform.position;
            BadgerCount = 0;
            BadgerHoleSetting = true;
            SendRPC();
            Utils.NotifyRoles();
        }

        public override string GetProgressText(bool comms = false) => Utils.ColorString(Color.yellow, $"({BadgerCount}/{BadgerHoleKillCount}/{VicBadgerMaxKill.GetInt()})");

        public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        {
            //矢印表示する必要がなければ無し
            if (isForMeeting) return "";
            return "";
        }

        private void SendRPC()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            using var sender = CreateSender(CustomRPC.BadgerSync);
            sender.Writer.Write(BadgerCount);
            sender.Writer.Write(BadgerHoleKillCount);
        }
        public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
        {
            if (rpcType != CustomRPC.BadgerSync) return;

            BadgerCount = reader.ReadInt32();
            BadgerHoleKillCount = reader.ReadInt32();
        }

        public override void OnFixedUpdate(PlayerControl pc)
        {
            if (GameStates.IsLobby) return;
            if (pc == null) return;
            if (!BadgerHoleSetting) return;
            if (!MeetingEndFlag) return;

            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (target != pc)
                {
                    if (!pc.IsAlive() || !target.IsAlive())
                    {
                        AlartTarget = byte.MaxValue;
                        AlartFlag = true;
                        continue;
                    }

                    var dis = Vector2.Distance(BadgerHolePosition, target.transform.position);
                    if (BadgerCount < badgerHoleMaxStep)
                    {

                        if (dis <= badgerRadius && AlartFlag)
                        {
                            BadgerCount++;
                            AlartTarget = target.PlayerId;
                            AlartFlag = false;
                            BadgerHoleKillFlag = true;
                            SendRPC();
                            Utils.NotifyRoles();
                        }
                        //アラート対象が離れた場合
                        else if (AlartTarget == target.PlayerId && dis > badgerRadius)
                        {
                            AlartTarget = byte.MaxValue;
                            AlartFlag = true;
                        }
                    }
                    else if (BadgerHoleKillFlag && AlartTarget == target.PlayerId)
                    {
                        //穴開通
                        var NowLocation = target.transform.position;

                        target.RpcSnapToForced(new Vector2(50f, 50f));

                        float TrapTime = 2f; //2秒
                        BadgerHoleKillFlag = false;

                        new LateTask(() =>
                        {

                            var playerState = PlayerState.GetByPlayerId(target.PlayerId);
                            playerState.DeathReason = CustomDeathReason.Fall;
                            target.RpcMurderPlayer(target);
                            Utils.KillFlash(pc);
                            BadgerHoleKillCount++;
                            ReportDeadBodyPatch.CanReportByDeadBody[target.PlayerId] = false;
                            AlartTarget = byte.MaxValue;
                            AlartFlag = true;
                            if (BadgerHoleKillCount >= vicBadgerMaxKill)
                            {
                                Vulture.AnimalsBomb(CustomDeathReason.Fall);
                                Vulture.AnimalsWin();
                            }
                            Utils.NotifyRoles();
                            target.RpcSnapToForced(NowLocation);
                            SendRPC();

                        }, TrapTime, "Trapper BlockMove");
                    }
                }
            }
        }

        public static void MeetingEndCheck()
        {

            MeetingEndFlag = false;
            float EndTime = BadgerMeetingEndTime.GetFloat();

            new LateTask(() =>
            {
                MeetingEndFlag = true;
                foreach (var pc in Main.AllPlayerControls)
                {
                    var cRole = pc.GetCustomRole();
                    if (cRole == CustomRoles.Badger)
                    {
                        pc.RpcProtectedMurderPlayer(); //穴が反応することが分かるように
                    }
                }
            }, EndTime, "BadgerMettingEnd");
        }
    }
}