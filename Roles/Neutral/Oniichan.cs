using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using static TownOfHostForE.Translator;
using System;

namespace TownOfHostForE.Roles.Neutral
{
    public sealed class Oniichan : RoleBase, IKiller
    {
        public enum OniichanState
        {
            Initial = 0,
            TargetKillGuard,
            MyBrotherKilled,
            Finish
        }
        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Oniichan),
                player => new Oniichan(player),
                CustomRoles.Oniichan,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                23300,
                SetupOptionItem,
                "ヤンデレ",
                "#C0C0C0",
                true,
                countType: CountTypes.Crew,
            introSound: () => ShipStatus.Instance.CommonTasks.Where(task => task.TaskType == TaskTypes.FixWiring).FirstOrDefault().MinigamePrefab.OpenSound
            );
        public Oniichan(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            killCool = KillCool.GetFloat();
            nowState = OniichanState.Initial;
            TargetId = 0;
            myBroId = 0;
        }

        //Option
        public static OptionItem KillCool;
        public static float killCool;

        public byte myBroId;
        public byte TargetId;

        public OniichanState nowState = OniichanState.Initial;



        private static void SetupOptionItem()
        {
            KillCool = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.KillCooldown, new(5, 100, 5), 30, false);
        }

        public override void Add()
        {
            var playerId = Player.PlayerId;

            //ターゲット割り当て
            if (!AmongUsClient.Instance.AmHost) return;

            List<PlayerControl> targetList = new();
            var targetRand = IRandom.Instance;
            foreach (var target in Main.AllPlayerControls)
            {
                if (playerId == target.PlayerId) continue;
                if (target.Is(CustomRoles.GM)) continue;

                targetList.Add(target);
            }
            var SelectedTarget = targetList[targetRand.Next(targetList.Count)];
            TargetId = SelectedTarget.PlayerId;

            //ちょっと手間だけどもう一回
            var MyBortherRand = IRandom.Instance;
            targetList.Clear();
            foreach (var target in Main.AllPlayerControls)
            {
                if (playerId == target.PlayerId) continue;
                if (TargetId == target.PlayerId) continue;
                if (target.Is(CustomRoles.GM)) continue;

                targetList.Add(target);
            }

            var SelectedBrother = targetList[MyBortherRand.Next(targetList.Count)];
            myBroId = SelectedBrother.PlayerId;

            SendRPC();
            Logger.Info($"{Player.GetNameWithRole()}:{SelectedTarget.GetNameWithRole()}", "OniiChan");
        }
        public void SendRPC()
        {
            if (!AmongUsClient.Instance.AmHost) return;

            using var sender = CreateSender(CustomRPC.OniichanTargetSync);
            sender.Writer.Write(TargetId);
            sender.Writer.Write(myBroId);
        }
        private void SendStateRPC()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            using var sender = CreateSender(CustomRPC.OniichanStateSync);
            sender.Writer.Write(Player.PlayerId);
            int sendState = (int)nowState;
            sender.Writer.Write(sendState);
        }
        public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
        {
            switch (rpcType)
            {
                case CustomRPC.OniichanTargetSync:
                    byte targetId = reader.ReadByte();
                    byte mybroId = reader.ReadByte();
                    myBroId = mybroId;
                    TargetId = targetId;
                    break;
                case CustomRPC.OniichanStateSync:
                    byte killerId = reader.ReadByte();
                    nowState = (OniichanState)Enum.ToObject(typeof(OniichanState),reader.ReadInt32());
                    break;
            }
        }
        public float CalculateKillCooldown() => killCool;
        public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
        {
            //seenが省略の場合seer
            seen ??= seer;

            return setTargetString(seen.PlayerId);
        }

        public override void OnStartMeeting()
        {
            bool reset = false;
            //お兄ちゃんをキルしてなければターゲット変更
            if ((nowState == OniichanState.Initial ||
                 nowState == OniichanState.TargetKillGuard) && !Utils.GetPlayerById(myBroId).IsAlive())
                 reset = resetMyBro();

            //あいつをキルしていなければターゲット変更
            if (nowState != OniichanState.Finish && !Utils.GetPlayerById(TargetId).IsAlive())
                reset = resetTarget();

            //更新があればRPCを贈る。
            if (AmongUsClient.Instance.AmHost && reset) SendRPC();

        }

        private bool resetTarget()
        {
            //ターゲット割り当て
            if (!AmongUsClient.Instance.AmHost) return false;

            List<PlayerControl> targetList = new();
            var rand = IRandom.Instance;
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (Player.PlayerId == target.PlayerId) continue;
                if (target.Is(CustomRoles.GM)) continue;
                //あいつに設定されていたら対象外
                if (myBroId == target.PlayerId) continue;

                targetList.Add(target);
            }
            var SelectedTarget = targetList[rand.Next(targetList.Count)];
            TargetId = SelectedTarget.PlayerId;
            return true;
        }
        private bool resetMyBro()
        {
            //ターゲット割り当て
            if (!AmongUsClient.Instance.AmHost) return false;

            List<PlayerControl> targetList = new();
            var rand = IRandom.Instance;
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (Player.PlayerId == target.PlayerId) continue;
                if (target.Is(CustomRoles.GM)) continue;
                //あいつに設定されていたら対象外
                if (TargetId == target.PlayerId) continue;

                targetList.Add(target);
            }
            var SelectedTarget = targetList[rand.Next(targetList.Count)];
            myBroId = SelectedTarget.PlayerId;
            return true;
        }

        private string setTargetString(byte targetId)
        {
            string returnString = "";
            if (targetId == myBroId)
            {
                returnString = "<color=#ff1919>♥♥</color>";
            }
            else if(targetId == TargetId)
            {
                returnString = "<color=#770000>〷</color>";
            }

            return returnString;
        }

        public bool CanUseImpostorVentButton() => false;
        public bool CanUseSabotageButton() => false;
        public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);

        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            //キル先がお嬢、シュレ猫でも問答無用でキルする。

            (var killer, var target) = info.AttemptTuple;
            switch (nowState)
            {
                case OniichanState.Initial:
                    if (target.PlayerId == TargetId)
                    {
                        nowState = OniichanState.TargetKillGuard;
                        Utils.NotifyRoles();
                    }
                    info.DoKill = false;
                    killer.SetKillCooldown();
                    break;
                case OniichanState.TargetKillGuard:
                    //対象がお兄ちゃんならそのままキルを通す
                    if (target.PlayerId == myBroId)
                    {
                        nowState = OniichanState.MyBrotherKilled;
                        info.DoKill = true;
                        Utils.NotifyRoles();
                    }
                    //お兄ちゃん以外ならキルできずにキルクール消費
                    else
                    {
                        killer.SetKillCooldown();
                        info.DoKill = false;
                    }
                    break;
                    //あいつならキル
                case OniichanState.MyBrotherKilled:
                    if (target.PlayerId == TargetId)
                    {
                        nowState = OniichanState.Finish;
                        info.DoKill = true;
                        Utils.NotifyRoles();
                    }
                    //あいつ以外ならキルできずにキルクール消費
                    else
                    {
                        killer.SetKillCooldown();
                        info.DoKill = false;
                    }
                    break;
            }
            SendStateRPC();
        }
        public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
        {
            string retText = "";
            if (GameStates.IsMeeting || isForMeeting) return retText;

            switch (nowState)
            {
                case OniichanState.Initial:
                    retText = string.Format(GetString("OniichanAitsu"));
                    break;
                case OniichanState.TargetKillGuard:
                    retText = string.Format(GetString("OniichanDoite"));
                    break;
                case OniichanState.MyBrotherKilled:
                    retText = string.Format(GetString("OniichanAitsuYaru"));
                    break;
                case OniichanState.Finish:
                    retText = string.Format(GetString("OniichanNande"));
                    break;
                default:
                    break;
            }
            return retText;
        }
        public override void OnMurderPlayerAsTarget(MurderInfo info)
        {
            Logger.Info($"{Player.GetRealName()}はヤンデレだった", nameof(Oniichan));
            if (CanWin())
            {
                MyState.DeathReason = CustomDeathReason.Suicide;
                Win();
            }
        }
        public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
        {
            if (exiled.PlayerId != Player.PlayerId)
            {
                return;
            }

            if (CanWin())
            {
                Win();
                DecidedWinner = true;
            }
        }
        public bool CanWin()
        {
            if (nowState == OniichanState.Finish)
                return true;
            return false;
        }
        public void Win()
        {
            foreach (var otherPlayer in Main.AllAlivePlayerControls)
            {
                if (otherPlayer.Is(CustomRoles.Oniichan))
                {
                    continue;
                }
                otherPlayer.SetRealKiller(Player);
                otherPlayer.RpcMurderPlayer(otherPlayer);
                var playerState = PlayerState.GetByPlayerId(otherPlayer.PlayerId);
                playerState.DeathReason = CustomDeathReason.Bombed;
                playerState.SetDead();
            }
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Oniichan);
            CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
        }

    }
}