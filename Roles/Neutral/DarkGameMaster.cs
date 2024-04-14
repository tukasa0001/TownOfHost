using AmongUs.GameOptions;
using System;
using Il2CppSystem.Collections.Generic;
using System.Linq;
using TownOfHostForE;
using TownOfHostForE.Modules;
using TownOfHostForE.OneTimeAbillitys;
using TownOfHostForE.Patches;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using TownOfHostForE.Roles.Crewmate;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace TownOfHostForE.Roles.Neutral
{
    public sealed class DarkGameMaster : RoleBase, IKiller //, IAdditionalWinner
    {
        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
             SimpleRoleInfo.Create(
                typeof(DarkGameMaster),
                player => new DarkGameMaster(player),
                CustomRoles.DarkGameMaster,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                23500,
                SetupOptionItem,
                "運営者",
                "#47266e",
                 true,
                countType: CountTypes.Crew
            );
        public DarkGameMaster(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            deathGameJoinPlayerIds = new();

            deathGamePlayerCount = 2;
            IsDeathGameOwner = byte.MaxValue;

            dgClearLimit = OptionDGClearLimit.GetInt();
            dgWinAlivePlayers = OptionDGWinAlivePlayers.GetInt();

            CurrentKillCooldown = KillCooldown.GetFloat();

            //他視点用のMarkメソッド登録
            CustomRoleManager.MarkOthers.Add(GetMarkOthers);
            CustomRoleManager.OverriderOthers.Add(GetOverrideOthers);
        }
        enum OptionName
        {
            DGWinnerLimit,
            DGWinAlivePlayers,
        }

        //デスゲーム判定のリセットは本クラス内ではなくonGameStartedPatchで実施する
        public static bool IsDeathGameTime = false;
        public static byte IsDeathGameOwner = byte.MaxValue;

#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
        private static OptionItem OptionDGClearLimit;
        private static OptionItem OptionDGWinAlivePlayers;
        static OptionItem KillCooldown;
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。

        private int dgClearLimit;
        private int dgWinAlivePlayers;

        private int deathGamePlayerCount = 0;
        private int deathGameFesCount = 0;

        public float CurrentKillCooldown = 30;


        //key:killerid
        public static Dictionary<byte, HashSet<byte>> deathGameJoinPlayerIds = new();

        private static void SetupOptionItem()
        {
            KillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionDGClearLimit = IntegerOptionItem.Create(RoleInfo, 11, OptionName.DGWinnerLimit, new(0, 15, 1), 2, false).SetValueFormat(OptionFormat.Times);
            OptionDGWinAlivePlayers = IntegerOptionItem.Create(RoleInfo, 12, OptionName.DGWinAlivePlayers, new(1, 15, 1), 4, false).SetValueFormat(OptionFormat.Players);
        }

        public override void Add()
        {
            byte playerId = Player.PlayerId;
            HashSet<byte> inputTempHash = new();
            deathGameJoinPlayerIds.Add(playerId, inputTempHash);
            deathGameFesCount = 0;
        }

        public float CalculateKillCooldown() => CurrentKillCooldown;

        public void OnCheckMurderAsKiller(MurderInfo info)
        {

            (var killer, var target) = info.AttemptTuple;
            killer.RpcProtectedMurderPlayer(target);
            info.DoKill = false;
            if (deathGameJoinPlayerIds.ContainsKey(Player.PlayerId) == false) return;
            if (deathGameJoinPlayerIds[Player.PlayerId].Count >= deathGamePlayerCount) return;
            deathGameJoinPlayerIds[Player.PlayerId].Add(target.PlayerId);
            Utils.NotifyRoles();
        }

        public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
        {

            var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
            //投票者が自分か
            if (voterId != Player.PlayerId) return base.ModifyVote(voterId, sourceVotedForId, isIntentional);
            //投票されたのが自分か
            if (sourceVotedForId != Player.PlayerId) return base.ModifyVote(voterId, sourceVotedForId, isIntentional);

            //自投票
            if (deathGameJoinPlayerIds[Player.PlayerId].Count >= 2)
            {
                //開催
                IsDeathGameTime = true;
                IsDeathGameOwner = Player.PlayerId;
                deathGameFesCount++;

                votedForId = MeetingVoteManager.Skip;
            }

            return (votedForId, numVotes, doVote);
        }

        public override void OnStartMeeting()
        {
            CheckWin();
            EndDeathGame();
            CheckResetTarget();
            Utils.NotifyRoles();
        }
        //もしターゲットが死んでいた場合外すための処理
        private void CheckResetTarget()
        {
            HashSet<byte> removeId = new();
            foreach (byte id in deathGameJoinPlayerIds[Player.PlayerId])
            {
                var pc = Utils.GetPlayerById(id);
                if (pc.IsAlive() == false) removeId.Add(id);
            }

            foreach (byte id in removeId)
            {
                deathGameJoinPlayerIds[Player.PlayerId].Remove (id);
            }
        }

        public override void AfterMeetingTasks()
        {
            if (CheckAfterPlayers() == false) return;
            CheckSetPets();
            SetPetKills();
            if (IsDeathGameTime) Utils.NotifyRoles();
        }

        private bool CheckAfterPlayers()
        {
            //デスゲーム中にのみチェック
            if (!IsDeathGameTime) return false;

            HashSet<byte> removeId = new();
            foreach (byte id in deathGameJoinPlayerIds[Player.PlayerId])
            {
                var pc = Utils.GetPlayerById(id);
                if (pc.IsAlive() == false) removeId.Add(id);
            }

            //死者0でデスゲーム開催中なので良し
            if (removeId.Count == 0) return true;

            foreach (byte id in removeId)
            {
                deathGameJoinPlayerIds[Player.PlayerId].Remove(id);
            }

            //死者分ID削除してスタート
            return false;
        }

        public bool CanUseSabotageButton() => false;

        public static bool InDeathGamePenalty(PlayerControl player)
        {
            //デスゲーム中であるなら死んでもらう
            if (IsDeathGameTime)
            {
                if (player.IsAlive() == false) return false;

                PlayerState.GetByPlayerId(player.PlayerId).DeathReason = CustomDeathReason.deathGame;
                player.RpcMurderPlayer(player);
                return false;
            }

            return true;
        }

        public static string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            try
            {
                //seenが省略の場合seer
                seen ??= seer;

                if (seen == null || seer == null) return "";
                if (seer.PlayerId != seen.PlayerId) return "";
                //シーアもしくはシーンが死んでいたら処理しない。
                //if (!seer.IsAlive() || !seen.IsAlive()) return "";
                if (IsDeathGameTime == false || IsDeathGameOwner == byte.MaxValue) return "";
                //ターゲットが登録されていなかったら抜ける 全体側で処理
                if (CheckJoinPlayers(IsDeathGameOwner, seer.PlayerId) == false)　return "";

                byte otherTargetByte = GetOtherTargetId(seer.PlayerId);

                var otherTargetPlayerInfo = Utils.GetPlayerInfoById(otherTargetByte);

                string otherTargetName = Utils.ColorString(otherTargetPlayerInfo.Color,otherTargetPlayerInfo.PlayerName);

                //キラー自身がseenのとき
                return "\n" + otherTargetName + "をやらなければ生き残れない";

            }
            catch (Exception ex)
            {
                Logger.Info(ex.Message + "/" + ex.StackTrace,"DarkGameMaster");
                return "";
            }
        }
        public static string GetOverrideOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            try
            {
                if (isForMeeting) return "";
                //seenが省略の場合seer
                seen ??= seer;

                if (seen == null || seer == null) return "";
                if (seer.PlayerId != seen.PlayerId) return "";
                //シーアもしくはシーンが死んでいたら処理しない。
                if (IsDeathGameTime == false || IsDeathGameOwner == byte.MaxValue) return "";
                //ターゲットが登録されていなかったら抜ける
                if (CheckJoinPlayers(IsDeathGameOwner, seer.PlayerId) == false)
                {

                    return GetOtherViewString();
                }

                //キラー自身がseenのとき
                return "";

            }
            catch (Exception ex)
            {
                Logger.Info(ex.Message + "/" + ex.StackTrace, "DarkGameMaster");
                return "";
            }
        }

        private static string GetOtherViewString()
        {
            //もっと賢いやり方見つけたら(ry
            List<string> userNameList = new();

            foreach (var id in deathGameJoinPlayerIds[IsDeathGameOwner])
            {
                var pi = Utils.GetPlayerInfoById(id);
                userNameList.Add(Utils.ColorString(pi.Color,pi.PlayerName));
            }

            return "\n<size=120%>" + userNameList[0] + "</size>\n<size=80%>VS</size>\n<size=150%>" + userNameList[1] + "</size>";
        } 

        private static bool CheckJoinPlayers(byte killerId, byte targetId)
        {
            if (killerId == byte.MaxValue) return false;

            return deathGameJoinPlayerIds[killerId].Contains(targetId);
        }

        private void CheckSetPets()
        {
            if (IsDeathGameOwner == byte.MaxValue) return;
            if (!PetSettings.AllPetAssign) return;

            foreach (byte ids in deathGameJoinPlayerIds[IsDeathGameOwner])
            {
                if (PetSettings.petNotSetPlayerIds.Any(p => p == ids))
                {
                    var pc = Utils.GetPlayerById(ids);
                    pc.RpcSetPet(PetSettings.FREEPET_STRING);
                }
            }
        }
        public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
        {
            //seenが省略の場合seer
            seen ??= seer;
            if (seer.PlayerId != Player.PlayerId) return "";
            if (seer.PlayerId != seen.PlayerId) return "";
            if (deathGameJoinPlayerIds.ContainsKey(seer.PlayerId) == false) return "";

            return "\n" + SetGameMasterString();
        }

        private string SetGameMasterString()
        {
            string returnString = "";

            if (IsDeathGameTime)
            {
                returnString = "祭りは黙って見守るものさ";
            }
            //参加人数を満たしていたら
            else if (deathGameJoinPlayerIds[Player.PlayerId].Count == deathGamePlayerCount)
            {
                returnString = "祭りを始めろ！";
            }
            else
            {
                returnString = "後" + (deathGamePlayerCount - deathGameJoinPlayerIds[Player.PlayerId].Count) + "人指名しろ";
            }

            return returnString;
        }

        private void SetPetKills()
        {
            if (IsDeathGameOwner == byte.MaxValue) return;

            OneTimeAbilittyController.OneTimeAbility[] setAbility = new OneTimeAbilittyController.OneTimeAbility[1] { OneTimeAbilittyController.OneTimeAbility.petKill };
            PetKill.KillTargetSettings[] targetSetting = new PetKill.KillTargetSettings[1] { PetKill.KillTargetSettings.All };

            foreach (byte ids in deathGameJoinPlayerIds[IsDeathGameOwner])
            {
                var pc = Utils.GetPlayerById(ids);
                OneTimeAbilittyController.SetOneTimeAbility(pc, setAbility);
                byte[] tempId = new byte[1] { GetOtherTargetId(ids) };
                PetKill.SetPetKillsAbillity(pc, targetSetting, tempId);
            }
        }
        private static byte GetOtherTargetId(byte targetId)
        {
            byte returnByte = byte.MaxValue;

            foreach (byte otherId in deathGameJoinPlayerIds[IsDeathGameOwner])
            {
                if (otherId != targetId)
                {
                    returnByte = otherId;
                }
            }

            return returnByte;
        }

        private void EndDeathGame()
        {
            if (IsDeathGameTime == false) return;
            if (IsDeathGameOwner == byte.MaxValue) return;

            RemovePetAssign();

            IsDeathGameTime = false;
            deathGameJoinPlayerIds[Player.PlayerId].Clear();
        }

        private void RemovePetAssign()
        {
            if (!PetSettings.AllPetAssign) return;

            foreach (byte ids in deathGameJoinPlayerIds[IsDeathGameOwner])
            {
                if (PetSettings.petNotSetPlayerIds.Any(p => p == ids))
                {
                    var pc = Utils.GetPlayerById(ids);
                    pc.RpcSetPet("");
                }
            }
        }

        private void CheckWin()
        {
            //死んでるなら関係なし
            if (!Player.IsAlive()) return;
            //指定人数以上の場合は何もしない
            if (Main.AllAlivePlayerControls.Count() > dgWinAlivePlayers) return;
            //3回以上開催してないと勝利しない
            if (deathGameFesCount < dgClearLimit) return; 

            //条件を満たした
            Win();
        }
        private void Win()
        {
            foreach (var otherPlayer in Main.AllAlivePlayerControls)
            {
                if (otherPlayer.Is(CustomRoles.DarkGameMaster))
                {
                    continue;
                }
                otherPlayer.SetRealKiller(Player);
                otherPlayer.RpcMurderPlayer(otherPlayer);
                var playerState = PlayerState.GetByPlayerId(otherPlayer.PlayerId);
                playerState.DeathReason = CustomDeathReason.deathGame;
                playerState.SetDead();
            }
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.DeathGame);
            CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
        }
    }

}
