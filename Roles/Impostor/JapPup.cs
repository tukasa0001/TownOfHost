using System.Collections.Generic;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;
using Hazel;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Roles.Impostor
{
   public sealed class JapPup : RoleBase, IImpostor
    {
        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(JapPup),
            player => new JapPup(player),
            CustomRoles.JapPup,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            22600,
            SetupOptionItem,
            "人形術師"
        );
        public JapPup(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            japPupTargetKillTime = JapPupTargetKillTime.GetFloat();
            CanKillFlag = true;
            shapeTarget = NOT_SELECT;
            killedFlag = false;
            japPupTarget.Clear();

            //他視点用のMarkメソッド登録
            CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        }

        private static OptionItem JapPupTargetKillTime;

        public static RoleTypes RoleTypes;

        public static float japPupTargetKillTime;

        public static byte Target = new();
        public static bool CanSetTarget = new();
        public static bool CanKillFlag = new();

        public static Dictionary<byte, byte> japPupTarget = new();
        public static byte shapeTarget;

        readonly byte NOT_SELECT = 20;

        static bool killedFlag = false;


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
            /// <summary>
            /// ターゲットを削除する
            /// </summary>
            RemoveShapeTarget,
        }

        enum OptionName
        {
            JapPubTargetKillTime,
        }

        private static void SetupOptionItem()
        {
            JapPupTargetKillTime = FloatOptionItem.Create(RoleInfo, 10, OptionName.JapPubTargetKillTime, new(2f, 20f, 1f), 5f, false)
                .SetValueFormat(OptionFormat.Seconds);
        }

        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            if (!info.CanKill) return; //キル出来ない相手には無効
            var (killer, target) = info.AttemptTuple;

            //通常キルの場合
            if (CanKillFlag)
            {
                if (target.Is(CustomRoles.Bait) || target.Is(CustomRoles.AddBait)) return;
                if (info.IsFakeSuicide) return;

                info.DoKill = true;
            }
            //シェイプ中のキルの場合
            else
            {
                //変身相手と切る相手が同じ場合はそのままキルする。
                //シェイプ先が死んでいるなら通常キルにする。
                if (target.PlayerId == shapeTarget ||
                    !Utils.GetPlayerById(shapeTarget).IsAlive())
                {
                    if (target.Is(CustomRoles.Bait) || target.Is(CustomRoles.AddBait)) return;
                    if (info.IsFakeSuicide) return;

                    info.DoKill = true;
                }
                else
                {
                    SetTarget(target.PlayerId, shapeTarget);
                    japPupTarget.Add(target.PlayerId, shapeTarget);
                    killer.SetKillCooldown();

                    //噛まれたことが分かるように
                    target.RpcProtectedMurderPlayer();

                    //指定秒後にキルしてないと死ぬ
                    new LateTask(() =>
                    {
                        if (!killedFlag && GameStates.IsInTask)
                        {
                            var targetControl = Utils.GetPlayerById(target.PlayerId);
                            targetControl.RpcMurderPlayer(targetControl);
                            RemoveShapeTarget(target.PlayerId);
                            Utils.MarkEveryoneDirtySettings();
                            Utils.NotifyRoles();
                            Logger.Info("指定時間でキルできなかったため自爆","JapPup");
                        }
                        else
                        {
                            Logger.Info("難を逃れた", "JapPup");
                        }

                    }, japPupTargetKillTime, "JapPupTargetKill");
                    info.DoKill = false;
                }
            }
        }

        public override void OnShapeshift(PlayerControl target)
        {
            var shapeshifting = !Is(target);
            if (target == null) return;
            ResetFlag(true);
            //変身中
            if (shapeshifting)
            {
                shapeTarget = target.PlayerId;
                CanKillFlag = false;
            }
            //変身解除
            else
            {
                shapeTarget = NOT_SELECT;
                CanKillFlag = true;
            }
        }

        public override void OnStartMeeting()
        {
            ResetFlag();

            //ミーティング中、明けなどで人が死なないように
            killedFlag = true;
        }
        public override void AfterMeetingTasks()
        {
            var target = Utils.GetPlayerById(TargetId);
            if ((!Player.IsAlive() || !target.IsAlive()))
            {
                RemoveTarget();
            }
            Player.SyncSettings();
            //Player.RpcResetAbilityCooldown();
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
        private void SetTarget(byte targetId,byte killTargetId)
        {
            //CanSetTarget = false;
            TargetId = targetId;
            Logger.Info("ターゲットセット","JapPup");
            TargetArrow.Add(targetId, killTargetId);
            if (AmongUsClient.Instance.AmHost)
            {
                using var sender = CreateSender(CustomRPC.JapPupkillTargetSync);
                sender.Writer.Write((byte)TargetOperation.SetTarget);
                sender.Writer.Write(targetId);
                sender.Writer.Write(killTargetId);
            }
        }
        
        private void RemoveShapeTarget(byte targetId)
        {
            if (!japPupTarget.ContainsKey(targetId)) return;
            japPupTarget.Remove(targetId);
            if (AmongUsClient.Instance.AmHost)
            {
                using var sender = CreateSender(CustomRPC.JapPupkillTargetSync);
                sender.Writer.Write((byte)TargetOperation.RemoveShapeTarget);
                sender.Writer.Write(targetId);
                sender.Writer.Write(targetId);//ダミー
            }
        }

        public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
        {
            if (rpcType != CustomRPC.JapPupkillTargetSync) return;

            var operation = (TargetOperation)reader.ReadByte();

            byte targetId = reader.ReadByte();
            byte shapeTarget = reader.ReadByte();

            switch (operation)
            {
                case TargetOperation.RemoveTarget:
                    RemoveTarget();
                    break;
                case TargetOperation.RemoveShapeTarget:
                    japPupTarget.Remove(targetId);
                    break;
                case TargetOperation.SetTarget:
                    SetTarget(targetId, shapeTarget);
                    japPupTarget.Add(targetId, shapeTarget);
                    break;
                default: Logger.Warn($"不明なオペレーション: {operation}", nameof(JapPup)); break;
            }
        }
        public static string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            //seenが省略の場合seer
            seen ??= seer;

            if (isForMeeting) return "";
            //シーアもしくはシーンが死んでいたら処理しない。
            if (!seer.IsAlive() || !seen.IsAlive()) return "";
            //ターゲットが登録されていなかったら抜ける
            if (!japPupTarget.ContainsKey(seer.PlayerId)) return "";

            if (seer.PlayerId != seen.PlayerId) return "";
            var shapeTargetId = japPupTarget[seer.PlayerId];

            string targetSet = "\n" + Utils.GetPlayerById(shapeTargetId).name + "をやれ：" + TargetArrow.GetArrows(seer, shapeTargetId);

            //キラー自身がseenのとき
            return Utils.ColorString(RoleInfo.RoleColor, targetSet);
        }

        public override void OnFixedUpdate(PlayerControl pc)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (GameStates.IsLobby || GameStates.IsMeeting) return;
            if (pc == null) return;
            //ターゲットが入っていないなら実行しない
            if (japPupTarget.Count <= 0) return;

            foreach (var japPupData in japPupTarget)
            {
                byte targetId = japPupData.Key;
                byte shapeTarget = japPupData.Value;

                Vector3 targetPos = Vector3.zero;
                Vector3 shapeTargetPos = Vector3.zero;
                var KillRange = GameOptionsData.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];

                foreach (var target in Main.AllPlayerControls)
                {
                    if (target.PlayerId == targetId)
                    {
                        targetPos = target.transform.position;
                    }
                    else if (target.PlayerId == shapeTarget)
                    {
                        shapeTargetPos = target.transform.position;
                    }
                }

                float targetDistance = Vector2.Distance(targetPos, shapeTargetPos);

                if (targetDistance <= KillRange)
                {
                    var targetControl = Utils.GetPlayerById(targetId);
                    var shapeTargetControl = Utils.GetPlayerById(shapeTarget);

                    shapeTargetControl.SetRealKiller(targetControl);
                    targetControl.RpcMurderPlayer(shapeTargetControl);
                    killedFlag = true;
                    RemoveShapeTarget(targetId);
                    Utils.MarkEveryoneDirtySettings();
                    Utils.NotifyRoles();
                }
            }

        }

        private static void ResetFlag(bool checkMurder = false)
        {
            CanKillFlag = false;
            killedFlag = false;
            if (!checkMurder)
            {
                japPupTarget.Clear();
            }
        }

    }
}
