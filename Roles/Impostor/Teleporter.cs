using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using UnityEngine;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using static TownOfHostForE.Translator;
using TownOfHostForE.Modules;

using System.Linq;

namespace TownOfHostForE.Roles.Impostor
{
    public enum TereportState
    {
        Initial,
        Set,
        Active,
    }
    public sealed class Teleporter : RoleBase, IImpostor
    {
        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Teleporter),
            player => new Teleporter(player),
            CustomRoles.Teleporter,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            22500,
            SetupOptionItem,
            "テレポーター"
        );
        public Teleporter(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            tereportRecast = OptionTereportRecast.GetFloat();
            tereportRadius = OptionTereportRadius.GetFloat();
            shapeshiftTime = OptionShapeshiftTime.GetFloat();
            setWorpPosition.Clear();
            tereportTarget.Clear();
            deleteTereportTarget.Clear();
            nowState = TereportState.Initial;
            visualize = false;

            ImpostorsId.Clear();
            teleporters.Clear();
            var playerId = player.PlayerId;
            foreach (var target in Main.AllAlivePlayerControls)
            {
                var targetId = target.PlayerId;
                if (targetId != playerId && target.Is(CustomRoleTypes.Impostor))
                {
                    ImpostorsId.Add(targetId);
                }
            }

            if (OptionWatchGate.GetBool())
            {
                //他視点用のMarkメソッド登録
                CustomRoleManager.MarkOthers.Add(GetMarkOthers);
            }
        }


        static OptionItem OptionTereportRecast;
        static OptionItem OptionTereportRadius;
        static OptionItem OptionShapeshiftTime;
        static OptionItem OptionWatchGate;
        static OptionItem OptionResetGate;
        static OptionItem OptionOneSideGate;

        static float tereportRecast = 1;
        static float tereportRadius = 1;
        static float shapeshiftTime = 1;

        List<Vector3> setWorpPosition = new();
        List<byte> tereportTarget = new();
        List<byte> deleteTereportTarget = new();
        private static HashSet<byte> ImpostorsId = new(3);

        static bool visualize = false;

        static List<PlayerControl> teleporters = new();

        TereportState nowState = TereportState.Initial;

        enum OptionName
        {
            TereportRecast,
            TereportRadius,
            WatchGate,
            ResetGate,
            OneSideGate,
        }

        private static void SetupOptionItem()
        {
            OptionShapeshiftTime = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.ShapeshiftCooldown, new(1f, 20f, 0.5f), 2f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionTereportRadius = FloatOptionItem.Create(RoleInfo, 11, OptionName.TereportRadius, new(1f, 20f, 0.5f), 1.5f, false)
                .SetValueFormat(OptionFormat.Multiplier);
            OptionTereportRecast = FloatOptionItem.Create(RoleInfo, 12, OptionName.TereportRecast, new(5f, 100f, 5f), 10f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionWatchGate = BooleanOptionItem.Create(RoleInfo, 13, OptionName.WatchGate, false, false);
            OptionResetGate = BooleanOptionItem.Create(RoleInfo, 14, OptionName.ResetGate, false, false);
            OptionOneSideGate = BooleanOptionItem.Create(RoleInfo, 15, OptionName.OneSideGate, false, false);
        }


        public override void Add()
        {
            teleporters.Add(Player);
        }

        public override void ApplyGameOptions(IGameOptions opt)
        {
            AURoleOptions.ShapeshifterCooldown = shapeshiftTime;
            AURoleOptions.ShapeshifterDuration = 1f;
            AURoleOptions.ShapeshifterLeaveSkin = true;
        }

        public override void OnShapeshift(PlayerControl shapeTarget)
        {
            var shapeshifting = !Is(shapeTarget);
            if (!shapeshifting) return;
            if (setWorpPosition.Count() == 2)
            {
                //再設置不可の場合処理せず抜ける
                if (OptionOneSideGate.GetBool()) return;

                //最初に登録している箇所を削除
                setWorpPosition.RemoveAt(0);
            }
            setWorpPosition.Add(Player.transform.position);
            tereportTarget.Add(Player.PlayerId);
            foreach (var targetId in ImpostorsId)
            {
                SetTarget(targetId);
            }
            //SendRPC(Player.PlayerId);
            removeRecast();
            switch (nowState)
            {
                case TereportState.Initial:
                    nowState = TereportState.Set;
                    break;
                case TereportState.Set:
                    nowState = TereportState.Active;
                    break;
                //activeとエラー系は同じで何もしない
                case TereportState.Active:
                default:
                    break;
            }
            Utils.NotifyRoles();
        }

        public static string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            //seenが省略の場合seer
            seen ??= seer;
            if (GameStates.IsMeeting) return "";
            //シーアもしくはシーンが死んでいたら処理しない。
            if (!seer.IsAlive() || !seen.IsAlive()) return "";

            if (seer.PlayerId != seen.PlayerId) return "";

            if (!ImpostorsId.Contains(seer.PlayerId)) return "";
            //
            if (!visualize) return "";

            string targetSet = "";

            foreach (var ids  in teleporters)
            {
                targetSet = TargetArrow.GetArrowsP(ids, seer.PlayerId);
            }

            //キラー自身がseenのとき
            return Utils.ColorString(RoleInfo.RoleColor, targetSet);
        }
        private void SetTarget(byte targetId)
        {
            //開通した時が対象
            if (setWorpPosition.Count() != 2) return;

            //CanSetTarget = false;
            TargetArrow.Add(targetId, Player.PlayerId,Player.transform.position);
            visualize = true;
            //if (AmongUsClient.Instance.AmHost)
            //{
            //    using var sender = CreateSender(CustomRPC.JapPupkillTargetSync);
            //    sender.Writer.Write((byte)TargetOperation.SetTarget);
            //    sender.Writer.Write(targetId);
            //    sender.Writer.Write(killTargetId);
            //}
        }

        public override void OnFixedUpdate(PlayerControl pc)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (GameStates.IsLobby) return;
            if (pc == null) return;
            if (nowState != TereportState.Active) return;

            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (target.Is(CustomRoleTypes.Impostor))
                {
                    var dis = Vector2.Distance(setWorpPosition[0], target.transform.position);

                    //一方通行し出来ない場合は1つめの処理をしない
                    if (!OptionOneSideGate.GetBool())
                    {
                        if (dis <= tereportRadius && CheckTereportTarget(target.PlayerId))
                        {
                            SendRPC(target.PlayerId);
                            tereportTarget.Add(target.PlayerId);

                            //mod導入者限定で効果音再生

                            BGMSettings.PlaySoundSERPC("Teleport", target.PlayerId);

                            target.RpcSnapToForced(setWorpPosition[1]);
                            Utils.NotifyRoles();
                        }
                    }

                    //上でワープしてたらターゲットチェックで引っかかるからここを通らない
                    dis = Vector2.Distance(setWorpPosition[1], target.transform.position);
                    if (dis <= tereportRadius && CheckTereportTarget(target.PlayerId))
                    {
                        SendRPC(target.PlayerId);
                        tereportTarget.Add(target.PlayerId);

                        //mod導入者限定で効果音再生
                        BGMSettings.PlaySoundSERPC("Teleport", target.PlayerId);

                        target.RpcSnapToForced(setWorpPosition[0]);
                        Utils.NotifyRoles();
                    }

                    //一定時間経ったらワープできるように削除処理をする。
                    removeRecast();
                }
            }
        }

        private bool CheckTereportTarget(byte playerId)
        {
            foreach(var target in tereportTarget)
            {
                if (target == playerId) return false;
            }

            return true;
        }

        private void removeRecast()
        {
            foreach (var targetId in tereportTarget)
            {
                if (deleteTereportTarget.Contains(targetId)) continue;
                deleteTereportTarget.Add(targetId);
                new LateTask(() =>
                {
                    deleteTereportTarget.Remove(targetId);
                    tereportTarget.Remove(targetId);
                    Utils.NotifyRoles();
                }, tereportRecast, "Teleporter Recast");
            }
        }
        public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
        {
            string retText = "";

            switch (nowState)
            {
                case TereportState.Initial:
                case TereportState.Set:
                    retText = string.Format(GetString("TereporterSetPhase"));
                    break;
                case TereportState.Active:
                    if (CheckTereportTarget(seer.PlayerId))
                    {
                        retText = GetString("TereporterActivePhase");
                    }
                    else
                    {
                        retText = GetString("TereporterStandbyPhase");
                    }
                    break;
                default:
                    break;
            }
            return retText;
        }

        private void SendRPC(byte targetId)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            using var sender = CreateSender(CustomRPC.TeleportTargetSync);
            sender.Writer.Write(targetId);
        }
        public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
        {
            if (rpcType != CustomRPC.TeleportTargetSync) return;

            var targetId = reader.ReadByte();
            tereportTarget.Add(targetId);
            removeRecast();
        }

    }
}
