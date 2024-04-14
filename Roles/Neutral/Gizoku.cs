using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using MS.Internal.Xml.XPath;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using TownOfHostForE.Roles.Neutral;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace TownOfHostForE.Roles.Animals
{
    public sealed class Gizoku : RoleBase, IKiller, ISchrodingerCatOwner
    {

        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Gizoku),
                player => new Gizoku(player),
                CustomRoles.Gizoku,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                23200,
                SetupOptionItem,
                "義賊",
                "#C0C0C0",
                true,
                countType: CountTypes.Crew,
            introSound: () => ShipStatus.Instance.CommonTasks.Where(task => task.TaskType == TaskTypes.FixWiring).FirstOrDefault().MinigamePrefab.OpenSound
            );
        public Gizoku(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            gizokuRadius = GizokuRadius.GetFloat();
            vicGizokuMaxKill = VicGizokuMaxKill.GetInt();
            AlartFlag = true;
            AlartTarget = byte.MaxValue;
            shotLimit = 0;
            GizokuHolePosition = new();
            GizokuKillCount = new();
            gizokuHoleSetting = false;
        }
        public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Gizoku;

        //Option
        public static OptionItem GizokuRadius;
        public static OptionItem VicGizokuMaxKill;

        static float gizokuRadius = 1;
        static int vicGizokuMaxKill = 1;
        static bool AlartFlag = true;
        static byte AlartTarget = byte.MaxValue;
        static bool gizokuHoleSetting = false;
        int shotLimit = 0;

        static Vector3 GizokuHolePosition = new();
        static int GizokuKillCount = new();

        enum OptionName
        {
            GizokuRadius,
            VicGizokuMaxKill,
        }

        private static void SetupOptionItem()
        {
            GizokuRadius = FloatOptionItem.Create(RoleInfo, 10, OptionName.GizokuRadius, new(0.5f, 20f, 0.5f), 1f, false)
                .SetValueFormat(OptionFormat.Multiplier);
            VicGizokuMaxKill = IntegerOptionItem.Create(RoleInfo, 11, OptionName.VicGizokuMaxKill, new(1, 15, 1), 2, false)
                .SetValueFormat(OptionFormat.Players);
        }

        public override void Add()
        {
            var playerId = Player.PlayerId;
        }

        private void SendRPC()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            using var sender = CreateSender(CustomRPC.GizokuShotLimitSync);
            sender.Writer.Write(shotLimit);
            sender.Writer.Write(GizokuKillCount);
        }
        public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
        {
            if (rpcType != CustomRPC.GizokuShotLimitSync) return;

            shotLimit = reader.ReadInt32();
            GizokuKillCount = reader.ReadInt32();
        }

        public override void OnTouchPet(PlayerControl player)
        {
            Logger.Info($"Hole Set", "Gizoku");
            if (player == null || player.Data.IsDead) return;

            player.RpcProtectedMurderPlayer(); //設置が分かるように
            GizokuHolePosition = player.transform.position;
            gizokuHoleSetting = true;
            Utils.NotifyRoles();
        }
        public override string GetProgressText(bool comms = false) => Utils.ColorString(Color.yellow, $"({shotLimit}/{GizokuKillCount}/{vicGizokuMaxKill})");

        public bool CanUseImpostorVentButton() => true;
        public bool CanUseSabotageButton() => false;
        public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
        public bool CanUseKillButton()
            => Player.IsAlive()
            && shotLimit > 0;

        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            if (shotLimit == 0) return;

            (var killer, var target) = info.AttemptTuple;
            shotLimit--;
            if (target.Is(CustomRoleTypes.Impostor) ||
            target.IsAnimalsKiller() ||
            (target.IsNeutralKiller() && !target.GetCustomRole().IsDirectKillRole())
            )
            {
                GizokuKillCount++;
                winnerCheck(killer.PlayerId);
            }
            SendRPC();
            Utils.NotifyRoles();
        }
        public static void winnerCheck(byte playerId)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (GizokuKillCount >= vicGizokuMaxKill)
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    var playerState = PlayerState.GetByPlayerId(pc.PlayerId);
                    if (pc.Is(CustomRoles.Gizoku))
                    {
                        playerState.DeathReason = CustomDeathReason.Kill;
                    }
                    else if (!pc.Data.IsDead)
                    {
                        //生存者は爆死
                        pc.RpcMurderPlayer(pc);
                        playerState.DeathReason = CustomDeathReason.Bombed;
                        playerState.SetDead();
                    }
                }
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Gizoku);
                CustomWinnerHolder.WinnerIds.Add(playerId);
            }
        }
        public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);
        public override void OnFixedUpdate(PlayerControl pc)
        {
            if (GameStates.IsLobby) return;
            if (pc == null) return;
            if (!gizokuHoleSetting) return;
            if (shotLimit > 0)
            {
                AlartTarget = byte.MaxValue;
                AlartFlag = true;
                return;
            }
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (target != pc &&
                    (target.Is(CustomRoleTypes.Impostor) ||
                     target.IsAnimalsKiller() ||
                     target.IsNeutralKiller() ||
                     target.IsCrewKiller()))
                {
                    var dis = Vector2.Distance(GizokuHolePosition, target.transform.position);
                    if (dis <= gizokuRadius && AlartFlag && AlartTarget != target.PlayerId)
                    {
                        AlartTarget = target.PlayerId;
                        AlartFlag = false;
                        shotLimit++;
                        SendRPC();
                        gizokuHoleSetting = false;
                        pc.RpcProtectedMurderPlayer();//誰かが引っかかったことが分かるように
                        target.SetKillCooldown();

                        Utils.NotifyRoles();
                    }
                    else
                    {
                        AlartTarget = byte.MaxValue;
                        AlartFlag = true;
                    }
                }
            }
        }
    }
}