using System.Collections.Generic;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;
using Hazel;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Roles.Impostor
{
   public sealed class Cinderella : RoleBase, IImpostor
    {
        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Cinderella),
            player => new Cinderella(player),
            CustomRoles.Cinderella,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            22200,
            SetupOptionItem,
            "シンデレラ"
        );
        public Cinderella(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            cinderellaUpSpeed = CinderellaUpSpeed.GetFloat();
            cinderellaShapeshiftDuration = CinderellaShapeshiftDuration.GetFloat();
            CanKillFlag = false;
        }

        private static OptionItem CinderellaUpSpeed;
        public static OptionItem CinderellaShapeshiftDuration;

        public static RoleTypes RoleTypes;

        public static float cinderellaUpSpeed;
        public static float cinderellaShapeshiftDuration;

        public static byte Target = new();
        public static bool CanSetTarget = new();
        public static bool CanKillFlag = new();

        public static float CinderellaSpeed = 0f;

        public byte TargetId;
        private enum TargetOperation : byte
        {
            /// <summary>
            /// ターゲット再設定可能にする
            /// </summary>
            ReEnableTargeting,
            /// <summary>
            /// ターゲットを削除する
            /// </summary>
            RemoveTarget,
            /// <summary>
            /// ターゲットを設定する
            /// </summary>
            SetTarget,
        }

        enum OptionName
        {
            CinderellaUpSpeed,
            CinderellaShapeshiftDuration,
        }

        private static void SetupOptionItem()
        {
            CinderellaUpSpeed = FloatOptionItem.Create(RoleInfo, 10, OptionName.CinderellaUpSpeed, new(0.1f, 5.0f, 0.1f), 0.3f, false)
                .SetValueFormat(OptionFormat.Multiplier);
            CinderellaShapeshiftDuration = FloatOptionItem.Create(RoleInfo, 11, OptionName.CinderellaShapeshiftDuration, new(1, 1000, 1), 10, false);
        }

        public bool CanUseKillButton()
        {
            return Player.IsAlive() && CanKillFlag;
        }
        public override bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
        {
            //ミーティングに入るなら変身解除されるのでキルできなくする
            CanKillFlag = false;
            return true;
        }
        public override void OnShapeshift(PlayerControl target)
        {
            var shapeshifting = !Is(target);
            if (target == null) return;

            if (shapeshifting)
            {
                CanKillFlag = true;
                CinderellaSpeed = Main.AllPlayerSpeed[Player.PlayerId];
                Main.AllPlayerSpeed[Player.PlayerId] = cinderellaUpSpeed;
                SetTarget(target.PlayerId);
            }
            else
            {
                CanKillFlag = false;
                Main.AllPlayerSpeed[Player.PlayerId] = CinderellaSpeed;
                RemoveTarget();
            }

            Logger.Info($"{Player.GetNameWithRole()}のターゲットを{target.GetNameWithRole()}に設定", "CinderellaTarget");
            Player.MarkDirtySettings();
            Utils.NotifyRoles();
        }
        public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
        {
            if (rpcType != CustomRPC.SetCinderellaTarget) return;

            var operation = (TargetOperation)reader.ReadByte();

            switch (operation)
            {
                case TargetOperation.RemoveTarget: RemoveTarget(); break;
                case TargetOperation.SetTarget: SetTarget(reader.ReadByte()); break;
                default: Logger.Warn($"不明なオペレーション: {operation}", nameof(Cinderella)); break;
            }
        }
        public override void AfterMeetingTasks()
        {
            var target = Utils.GetPlayerById(TargetId);
            if ((!Player.IsAlive() || !target.IsAlive()) && CanKillFlag)
            {
                CanKillFlag = false;
                Main.AllPlayerSpeed[Player.PlayerId] = CinderellaSpeed;
                RemoveTarget();
            }
            Player.SyncSettings();
            Player.RpcResetAbilityCooldown();
        }

        public override void ApplyGameOptions(IGameOptions opt)
        {
            AURoleOptions.ShapeshifterDuration = cinderellaShapeshiftDuration;
        }
        private void RemoveTarget()
        {
            TargetId = byte.MaxValue;
            if (AmongUsClient.Instance.AmHost)
            {
                using var sender = CreateSender(CustomRPC.SetCinderellaTarget);
                sender.Writer.Write((byte)TargetOperation.RemoveTarget);
            }
        }
        private void SetTarget(byte targetId)
        {
            //CanSetTarget = false;
            TargetId = targetId;
            TargetArrow.Add(Player.PlayerId, targetId);
            if (AmongUsClient.Instance.AmHost)
            {
                using var sender = CreateSender(CustomRPC.SetCinderellaTarget);
                sender.Writer.Write((byte)TargetOperation.SetTarget);
                sender.Writer.Write(targetId);
            }
        }


        // 表示系の関数群
        public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool _ = false)
        {
            seen ??= seer;
            return TargetId == seen.PlayerId ? Utils.ColorString(Palette.ImpostorRed, "◀") : "";
        }
        public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        {
            seen ??= seer;
            if (isForMeeting) return "";

            return GetArrows(seen);
        }
        private string GetArrows(PlayerControl seen)
        {
            if (!Is(seen)) return "";

            var trackerId = Player.PlayerId;

            var sb = new StringBuilder(80);

            if (TargetId != byte.MaxValue)
            {
                sb.Append(Utils.ColorString(Color.white, TargetArrow.GetArrows(Player, TargetId)));
            }
            return sb.ToString();
        }
    }
}
