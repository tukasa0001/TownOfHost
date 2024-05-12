using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Roles.Animals
{
    public sealed class RedPanda : RoleBase, IKiller
    {
        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(RedPanda),
                player => new RedPanda(player),
                CustomRoles.RedPanda,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Animals,
                24600,
                SetupOptionItem,
                "レッサーパンダ",
                "#FF8C00",
                true,
                countType: CountTypes.Crew,
            introSound: () => ShipStatus.Instance.CommonTasks.Where(task => task.TaskType == TaskTypes.FixWiring).FirstOrDefault().MinigamePrefab.OpenSound
            );
        public RedPanda(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            visionString = "";
            watchedAnimals = false;
            TargetId = byte.MaxValue;

            AnimalsId.Clear();
            var playerId = player.PlayerId;
            foreach (var target in Main.AllAlivePlayerControls)
            {
                var targetId = target.PlayerId;
                if (targetId != playerId && target.Is(CustomRoleTypes.Animals))
                {
                    AnimalsId.Add(targetId);
                }
            }

            //他視点用のMarkメソッド登録
            CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        }

        private bool watchedAnimals = false;
        private string visionString = "";
        private HashSet<byte> AnimalsId = new();
        private static byte TargetId = byte.MaxValue;
        private enum TargetOperation : byte
        {
            /// <summary>
            /// ターゲットを設定する
            /// </summary>
            SetTarget,
            /// <summary>
            /// 文字をを設定する
            /// </summary>
            SetString,
        }

        private static void SetupOptionItem()
        {
            Options.SetUpAddOnOptions(RoleInfo.ConfigId + 20, RoleInfo.RoleName, RoleInfo.Tab);
        }
        public bool CanUseImpostorVentButton() => true;
        public bool CanUseSabotageButton() => false;
        public override string GetProgressText(bool comms = false) => visionString;
        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            if (Is(info.AttemptKiller) && !info.IsSuicide)
            {
                (var killer, var target) = info.AttemptTuple;

                if (target.GetCustomRole().IsAnimals())
                {
                    visionString = "\n仲間を見つけた！";
                    Logger.Info($"仲間を見つけた！", "レッサーパンダ");
                    watchedAnimals = true;
                }
                else if (watchedAnimals)
                {
                    visionString = "\nこの人は僕らの敵になる...！";
                    TargetId = target.PlayerId;
                    SendTargetRPC();
                    Logger.Info($"敵をマーク！", "レッサーパンダ");
                }
                else
                {
                    visionString = "\n何の反応も返ってこない...";
                    Logger.Info($"人違いのようだ", "レッサーパンダ");
                }

                SendStringRPC();
                killer.RpcProtectedMurderPlayer(target);
                info.DoKill = false;

                Utils.NotifyRoles();
            }
        }

        public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (exiled.PlayerId != Player.PlayerId) return;
            RedPandaRejected();
        }

        public override void OnMurderPlayerAsTarget(MurderInfo info)
        {
            var (killer, target) = info.AttemptTuple;
            if (!AmongUsClient.Instance.AmHost) return;
            if (target.PlayerId != Player.PlayerId) return;

            RedPandaRejected();
        }

        private void RedPandaRejected()
        {
            //アニマルズを見つけてなければ発動しない
            if (!watchedAnimals) return;
            //自身が死ぬときアニマルズにアドオンを付与する。
            foreach (var animId in AnimalsId)
            {
                var pc = Utils.GetPlayerById(animId);
                SetSubRole(pc);
            }
            TargetId = byte.MaxValue;
            visionString = "";
            SendTargetRPC();
            SendStringRPC();
        }

        private void SetSubRole(PlayerControl pc)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                foreach (var Addon in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(x => x.IsAddOn()))
                {
                    if (Options.AddOnRoleOptions.TryGetValue((CustomRoles.RedPanda, Addon), out var option) && option.GetBool())
                    {
                        pc.RpcSetCustomRole(Addon);
                        CustomRoleManager.subRoleAdd(pc.PlayerId, Addon);
                    }
                }
                pc.SyncSettings();
                Utils.NotifyRoles();
                Logger.Info("付与","redpanda");
            }
        }
        public override void OnStartMeeting()
        {
            visionString = "";
            SendStringRPC();
        }

        public override void AfterMeetingTasks()
        {
            TargetId = byte.MaxValue;
            SendTargetRPC();
        }

        public static string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            //seenが省略の場合seer
            seen ??= seer;

            if (isForMeeting) return "";
            //シーアもしくはシーンが死んでいたら処理しない。
            if (!seer.IsAlive() || !seen.IsAlive()) return "";
            if (seer.PlayerId != seen.PlayerId) return "";
            if (TargetId == byte.MaxValue) return "";
            var cRole = seer.GetCustomRole();
            if (cRole == CustomRoles.RedPanda || !cRole.IsAnimals()) return "";

            string targetSet = "\n<size=75%>" + Utils.GetPlayerById(TargetId).name + "は僕らの敵だ！</size>";

            return Utils.ColorString(RoleInfo.RoleColor, targetSet);
        }
        private void SendTargetRPC()
        {
            if (AmongUsClient.Instance.AmHost)
            {
                using var sender = CreateSender(CustomRPC.RedPandaSync);
                sender.Writer.Write((byte)TargetOperation.SetTarget);
                sender.Writer.Write(TargetId);
            }
        }
        private void SendStringRPC()
        {
            if (AmongUsClient.Instance.AmHost)
            {
                using var sender = CreateSender(CustomRPC.RedPandaSync);
                sender.Writer.Write((byte)TargetOperation.SetString);
                sender.Writer.Write(visionString);
                sender.Writer.Write(watchedAnimals);
            }
        }
        public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
        {
            if (rpcType != CustomRPC.RedPandaSync) return;

            var operation = (TargetOperation)reader.ReadByte();

            if (operation == TargetOperation.SetTarget)
            {
                TargetId = reader.ReadByte();
            }
            else if (operation == TargetOperation.SetString)
            {
                visionString = reader.ReadString();
                watchedAnimals = reader.ReadBoolean();
            }
            else
            {
                //異常系
            }
        }
    }
}
