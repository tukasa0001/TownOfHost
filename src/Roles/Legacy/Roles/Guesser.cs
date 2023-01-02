using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;
using System;
using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.ReduxOptions;
using TownOfHost.Roles;
using TownOfHost.RPC;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class Guesser
    {
        static readonly int Id = 23424;
        static CustomOption CanShootAsNormalCrewmate;
        static CustomOption GuesserCanKillCount;
        static CustomOption CanKillMultipleTimes;
        public static CustomOption PirateGuessAmount;
        static List<byte> playerIdList = new();
        static Dictionary<byte, int> GuesserShootLimit;
        public static Dictionary<byte, bool> isEvilGuesserExiled;
        static Dictionary<int, CustomRole> RoleAndNumber;
        static Dictionary<int, CustomRole> RoleAndNumberPirate;
        static Dictionary<int, CustomRole> RoleAndNumberAss;
        static Dictionary<int, CustomRole> RoleAndNumberCoven;
        public static Dictionary<byte, bool> IsSkillUsed;
        static Dictionary<byte, bool> IsEvilGuesser;
        static Dictionary<byte, bool> IsNeutralGuesser;
        public static bool IsEvilGuesserMeeting;
        public static bool canGuess;
        public static Dictionary<byte, int> PirateGuess;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id + 21, CustomRoles.EvilGuesser, AmongUsExtensions.OptionType.Impostor);
            /*CanShootAsNormalCrewmate = CustomOption.Create(Id + 30130, Color.white, "CanShootAsNormalCrewmate", AmongUsExtensions.OptionType.Impostor, true, Options.CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
            GuesserCanKillCount = CustomOption.Create(Id + 30140, Color.white, "GuesserShootLimit", AmongUsExtensions.OptionType.Impostor, 1, 1, 15, 1, Options.CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
            CanKillMultipleTimes = CustomOption.Create(Id + 30150, Color.white, "CanKillMultipleTimes", AmongUsExtensions.OptionType.Impostor, false, Options.CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
            Options.SetupRoleOptions(Id + 20, CustomRoles.NiceGuesser, AmongUsExtensions.OptionType.Crewmate);
            Options.SetupRoleOptions(Id + 51, CustomRoles.Pirate, AmongUsExtensions.OptionType.Neutral);
            PirateGuessAmount = CustomOption.Create(Id + 30170, Color.white, "PirateGuessAmount", AmongUsExtensions.OptionType.Impostor, 3, 1, 10, 1, Options.CustomRoleSpawnChances[CustomRoles.Pirate]);*/
        }
        /*public static bool SetGuesserTeam(byte PlayerId = byte.MaxValue)//確定イビルゲッサーの人数とは別でイビルゲッサーかナイスゲッサーのどちらかに決める。
        {
            float EvilGuesserRate = EvilGuesserChance.GetFloat();
            IsEvilGuesser[PlayerId] = UnityEngine.Random.Range(1, 100) < EvilGuesserRate;
            return IsEvilGuesser[PlayerId];
        }
        public static bool SetOtherGuesserTeam(byte PlayerId = byte.MaxValue)//確定イビルゲッサーの人数とは別でイビルゲッサーかナイスゲッサーのどちらかに決める。
        {
            float NeutralGuesserRate = NeutralGuesserChance.GetFloat();
            IsNeutralGuesser[PlayerId] = UnityEngine.Random.Range(1, 100) < NeutralGuesserRate;
            return IsNeutralGuesser[PlayerId];
        }*/
        public static void Init()
        {
            playerIdList = new();
            GuesserShootLimit = new();
            isEvilGuesserExiled = new();
            RoleAndNumber = new();
            RoleAndNumberPirate = new();
            RoleAndNumberAss = new();
            RoleAndNumberCoven = new();
            IsSkillUsed = new();
            IsEvilGuesserMeeting = false;
            canGuess = true;
            PirateGuess = new();
            IsEvilGuesser = new();
            IsNeutralGuesser = new();
        }
        public static void Add(byte PlayerId)
        {
            playerIdList.Add(PlayerId);
            if (Utils.GetPlayerById(PlayerId).Is(CustomRoles.Pirate))
                GuesserShootLimit[PlayerId] = 99;
            else
                GuesserShootLimit[PlayerId] = GuesserCanKillCount.GetInt();
            isEvilGuesserExiled[PlayerId] = false;
            IsSkillUsed[PlayerId] = false;
            IsEvilGuesserMeeting = false;
        }
        public static bool IsEnable()
        {
            return playerIdList.Count > 0;
        }
        public static void SetRoleToGuesser(PlayerControl player)//ゲッサーをイビルとナイスに振り分ける
        {
            if (IsEvilGuesser[player.PlayerId]) CustomRoleManager.PlayersCustomRolesRedux[player.PlayerId] = EvilGuesser.Ref<EvilGuesser>();
            else if (IsNeutralGuesser[player.PlayerId]) CustomRoleManager.PlayersCustomRolesRedux[player.PlayerId] = Pirate.Ref<Pirate>();
            else CustomRoleManager.PlayersCustomRolesRedux[player.PlayerId] = NiceGuesser.Ref<NiceGuesser>();
        }
        public static void GuesserShoot(PlayerControl killer, string targetname, string targetrolenum)//ゲッサーが撃てるかどうかのチェック
        {
            if ((!killer.Is(CustomRoles.NiceGuesser) && !killer.Is(CustomRoles.EvilGuesser) && !killer.Is(CustomRoles.Pirate)) || killer.Data.IsDead || !AmongUsClient.Instance.IsGameStarted) return;
            if (killer.Is(CustomRoles.Pirate) && !canGuess) return;
            //死んでるやつとゲッサーじゃないやつ、ゲームが始まってない場合は引き返す
            if (killer.Is(CustomRoles.NiceGuesser) && IsEvilGuesserMeeting) return;//イビルゲッサー会議の最中はナイスゲッサーは打つな
            if (!CanKillMultipleTimes.GetBool() && IsSkillUsed[killer.PlayerId] && !IsEvilGuesserMeeting) if (!killer.Is(CustomRoles.Pirate)) return;
            if (targetname == "show")
            {
                SendShootChoices(killer.PlayerId);
                return;
            }
            foreach (var target in PlayerControl.AllPlayerControls)
            {
                if (targetname == $"{target.name}" && GuesserShootLimit[killer.PlayerId] != 0)//targetnameが人の名前で弾数が０じゃないなら続行
                {
                    //if (target.Data.IsDead) return;
                    var r = GetGuessingType(killer.GetCustomRole(), targetrolenum);
                    if (target.Data.IsDead) return;
                    if (target.GetCustomRole() == r /*TODO: subrole | target.GetCustomSubRole() == r*/)//当たっていた場合
                    {
                        if (killer.Is(CustomRoles.Pirate))
                            PirateGuess[killer.PlayerId]++;
                        if (!killer.Is(CustomRoles.Pirate))
                            if ((target.GetCustomRole() is Crewmate && !CanShootAsNormalCrewmate.GetBool()) || (target.GetCustomRole() is Egoist && killer.Is(CustomRoles.EvilGuesser))) return;
                        //クルー打ちが許可されていない場合とイビルゲッサーがエゴイストを打とうとしている場合はここで帰る
                        GuesserShootLimit[killer.PlayerId]--;
                        IsSkillUsed[killer.PlayerId] = true;
                        PlayerStateOLD.SetDeathReason(target.PlayerId, PlayerStateOLD.DeathReason.Kill);
                        target.RpcGuesserMurderPlayer(0f);//専用の殺し方
                        PlayerStateOLD.SetDeathReason(target.PlayerId, PlayerStateOLD.DeathReason.Kill);
                        if (PirateGuess[killer.PlayerId] == PirateGuessAmount.GetInt())
                        {
                            // pirate wins.
                            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                            writer.Write((byte)CustomWinner.Pirate);
                            writer.Write(killer.PlayerId);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                            OldRPC.PirateWin(killer.PlayerId);
                            //CheckAndEndGamePatch.ResetRoleAndEndGame(endReason, false);
                        }
                        return;
                    }
                    if (target.GetCustomRole() != r)//外していた場合
                    {
                        if (!killer.Is(CustomRoles.Pirate))
                        {
                            PlayerStateOLD.SetDeathReason(target.PlayerId, PlayerStateOLD.DeathReason.Misfire);
                            killer.RpcGuesserMurderPlayer(0f);
                            PlayerStateOLD.SetDeathReason(target.PlayerId, PlayerStateOLD.DeathReason.Misfire);
                        }
                        else { canGuess = false; Utils.SendMessage("You missguessed as Pirate. Because of this, instead of dying, your guessing powers have been removed for the rest of the meeting,.", killer.PlayerId); }
                        if (IsEvilGuesserMeeting)
                        {
                            IsEvilGuesserMeeting = false;
                            isEvilGuesserExiled[killer.PlayerId] = false;
                            MeetingHud.Instance.RpcClose();
                        }
                        return;
                    }
                }
            }
        }
        public static CustomRole GetGuessingType(CustomRole role, string targetrolenum)
        {
            switch (role)
            {
                case EvilGuesser:
                    RoleAndNumberAss.TryGetValue(int.Parse(targetrolenum), out var r);
                    return r;
                case NiceGuesser:
                    RoleAndNumber.TryGetValue(int.Parse(targetrolenum), out var re);
                    return re;
                case Pirate:
                    RoleAndNumberPirate.TryGetValue(int.Parse(targetrolenum), out var ree);
                    return ree;
            }
            if (role.IsCoven())
            {
                RoleAndNumberCoven.TryGetValue(int.Parse(targetrolenum), out var ree);
                return ree;
            }
            return Amnesiac.Ref<Amnesiac>();
        }
        public static bool CanGuess(this PlayerControl pc)
        {
            if (GameStates.IsLobby || !AmongUsClient.Instance.IsGameStarted) return false;
            switch (pc.GetRoleType())
            {
                case RoleType.Coven:
                    if (pc.Is(CustomRoles.Mimic) | !Main.HasNecronomicon) break;
                    return true;
            }
            switch (pc.GetCustomRole())
            {
                case EvilGuesser:
                case NiceGuesser:
                case Pirate:
                    return true;
                default:
                    return false;
            }
        }
        public static void GuesserShootByID(PlayerControl killer, string playerId, string targetrolenum)//ゲッサーが撃てるかどうかのチェック
        {
            if (!killer.CanGuess()) return;
            if (killer.Is(CustomRoles.Pirate) && !canGuess) return;
            //死んでるやつとゲッサーじゃないやつ、ゲームが始まってない場合は引き返す
            if (killer.Is(CustomRoles.NiceGuesser) && IsEvilGuesserMeeting) return;//イビルゲッサー会議の最中はナイスゲッサーは打つな
            if (!CanKillMultipleTimes.GetBool() && IsSkillUsed[killer.PlayerId] && !IsEvilGuesserMeeting) if (!killer.Is(CustomRoles.Pirate)) return;
            if (playerId == "show")
            {
                SendShootChoices(killer.PlayerId);
                SendShootID(killer.PlayerId);
                return;
            }
            if (!killer.Data.IsDead)
                foreach (var target in PlayerControl.AllPlayerControls)
                {
                    if (playerId == $"{target.PlayerId}" && GuesserShootLimit[killer.PlayerId] != 0)//targetnameが人の名前で弾数が０じゃないなら続行
                    {
                        var r = GetShootChoices(killer.GetCustomRole(), targetrolenum);
                        if (target.Data.IsDead) return;
                        if (target.GetCustomRole().Is(r) /*TODO: Subrole| target.GetCustomSubRole() == r*/)//当たっていた場合
                        {
                            if (killer.Is(CustomRoles.Pirate))
                            {
                                PirateGuess[killer.PlayerId]++;
                                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetPirateProgress, Hazel.SendOption.Reliable, -1);
                                writer.Write(killer.PlayerId);
                                writer.Write(PirateGuess[killer.PlayerId]);
                                AmongUsClient.Instance.FinishRpcImmediately(writer);
                            }
                            if (!killer.Is(CustomRoles.Pirate))
                                if ((target.GetCustomRole() is Crewmate && !CanShootAsNormalCrewmate.GetBool()) || (target.GetCustomRole() is Egoist && killer.Is(CustomRoles.EvilGuesser))) return;
                            //クルー打ちが許可されていない場合とイビルゲッサーがエゴイストを打とうとしている場合はここで帰る
                            GuesserShootLimit[killer.PlayerId]--;
                            IsSkillUsed[killer.PlayerId] = true;
                            PlayerStateOLD.SetDeathReason(target.PlayerId, PlayerStateOLD.DeathReason.Kill);
                            target.RpcGuesserMurderPlayer(0f);//専用の殺し方
                            if (PirateGuess[killer.PlayerId] == PirateGuessAmount.GetInt())
                            {
                                // pirate wins.
                                var endReason = TempData.LastDeathReason switch
                                {
                                    DeathReason.Exile => GameOverReason.ImpostorByVote,
                                    DeathReason.Kill => GameOverReason.ImpostorByKill,
                                    _ => GameOverReason.ImpostorByVote,
                                };
                                Main.WonPirateID = killer.PlayerId;
                                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                                writer.Write((byte)CustomWinner.Pirate);
                                writer.Write(killer.PlayerId);
                                AmongUsClient.Instance.FinishRpcImmediately(writer);
                                OldRPC.PirateWin(killer.PlayerId);
                                PirateEndGame(endReason, false);
                            }
                            return;
                        }
                        if (target.GetCustomRole() != r)//外していた場合
                        {
                            if (!killer.Is(CustomRoles.Pirate))
                            {
                                PlayerStateOLD.SetDeathReason(target.PlayerId, PlayerStateOLD.DeathReason.Misfire);
                                killer.RpcGuesserMurderPlayer(0f);
                            }
                            else { canGuess = false; Utils.SendMessage("You missguessed as Pirate. Because of this, instead of dying, your guessing powers have been removed for the rest of the meeting,.", killer.PlayerId); }
                            if (IsEvilGuesserMeeting)
                            {
                                IsEvilGuesserMeeting = false;
                                isEvilGuesserExiled[killer.PlayerId] = false;
                                MeetingHud.Instance.RpcClose();
                            }
                            return;
                        }
                    }
                }
        }
        public static CustomRole GetShootChoices(CustomRole role, string targetrolenum)
        {
            if (role.IsCoven())
            {
                RoleAndNumberCoven.TryGetValue(int.Parse(targetrolenum), out var nvm);
                return nvm;
            }
            switch (role)
            {
                case EvilGuesser:
                    RoleAndNumberAss.TryGetValue(int.Parse(targetrolenum), out var e);
                    return e;
                case NiceGuesser:
                    RoleAndNumber.TryGetValue(int.Parse(targetrolenum), out var n);
                    return n;
                case Pirate:
                    RoleAndNumberPirate.TryGetValue(int.Parse(targetrolenum), out var p);
                    return p;
                default:
                    RoleAndNumberAss.TryGetValue(int.Parse(targetrolenum), out var nvm);
                    return nvm;
            }
        }
        public static void SendShootChoices(byte PlayerId = byte.MaxValue)//番号と役職をチャットに表示
        {
            string text = "";
            if (RoleAndNumber.Count() == 0) return;
            var role = Utils.GetPlayerById(PlayerId).GetCustomRole();
            switch (role)
            {
                case EvilGuesser:
                    for (var n = 1; n <= RoleAndNumberAss.Count(); n++)
                    {
                        text += string.Format("{0}:{1}\n", RoleAndNumberAss[n], n);
                    }
                    break;
                case NiceGuesser:
                    for (var n = 1; n <= RoleAndNumber.Count(); n++)
                    {
                        text += string.Format("{0}:{1}\n", RoleAndNumber[n], n);
                    }
                    break;
                case Pirate:
                    for (var n = 1; n <= RoleAndNumberPirate.Count(); n++)
                    {
                        text += string.Format("{0}:{1}\n", RoleAndNumberPirate[n], n);
                    }
                    break;
            }
            Utils.SendMessage(text, PlayerId);
        }
        public static void SendShootID(byte PlayerId = byte.MaxValue)//番号と役職をチャットに表示
        {
            string text = "";
            List<PlayerControl> AllPlayers = new();
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                AllPlayers.Add(pc);
            }
            text += "All Players and their IDs:";
            foreach (var player in AllPlayers)
            {
                text += $"\n{player.GetRawName(true)} : {player.PlayerId}";
            }
            Utils.SendMessage(text, PlayerId);
        }
        public static void RpcClientGuess(this PlayerControl pc)
        {
            var amOwner = pc.AmOwner;
            var meetingHud = MeetingHud.Instance;
            var hudManager = DestroyableSingleton<HudManager>.Instance;
            SoundManager.Instance.PlaySound(pc.KillSfx, false, 0.8f);
            hudManager.KillOverlay.ShowKillAnimation(pc.Data, pc.Data);
            if (amOwner)
            {
                hudManager.ShadowQuad.gameObject.SetActive(false);
                pc.nameText().GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
                pc.RpcSetScanner(false);
                ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
                importantTextTask.transform.SetParent(AmongUsClient.Instance.transform, false);
                meetingHud.SetForegroundForDead();
            }
            PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(
                x => x.TargetPlayerId == pc.PlayerId
            );
            //pc.Die(DeathReason.Kill);
            if (voteArea == null) return;
            if (voteArea.DidVote) voteArea.UnsetVote();
            voteArea.AmDead = true;
            voteArea.Overlay.gameObject.SetActive(true);
            voteArea.Overlay.color = Color.white;
            voteArea.XMark.gameObject.SetActive(true);
            voteArea.XMark.transform.localScale = Vector3.one;
            foreach (var playerVoteArea in meetingHud.playerStates)
            {
                if (playerVoteArea.VotedFor != pc.PlayerId) continue;
                playerVoteArea.UnsetVote();
                var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
                if (!voteAreaPlayer.AmOwner) continue;
                meetingHud.ClearVote();
            }
        }
        public static void RpcGuesserMurderPlayer(this PlayerControl pc, float delay = 0f)//ゲッサー用の殺し方
        {
            string text = "";
            text += string.Format(GetString("KilledByGuesser"), pc.name);
            Main.unreportableBodies.Add(pc.PlayerId);
            Utils.SendMessage(text, byte.MaxValue);
            // DEATH STUFF //
            var amOwner = pc.AmOwner;
            pc.Data.IsDead = true;
            pc.RpcExileV2();
            //PlayerState.SetDeathReason(pc.PlayerId, PlayerState.DeathReason.Execution);
            PlayerStateOLD.SetDead(pc.PlayerId);
            var meetingHud = MeetingHud.Instance;
            var hudManager = DestroyableSingleton<HudManager>.Instance;
            SoundManager.Instance.PlaySound(pc.KillSfx, false, 0.8f);
            hudManager.KillOverlay.ShowKillAnimation(pc.Data, pc.Data);
            if (amOwner)
            {
                hudManager.ShadowQuad.gameObject.SetActive(false);
                pc.nameText().GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
                pc.RpcSetScanner(false);
                ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
                importantTextTask.transform.SetParent(AmongUsClient.Instance.transform, false);
                meetingHud.SetForegroundForDead();
            }
            PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(
                x => x.TargetPlayerId == pc.PlayerId
            );
            if (voteArea == null) return;
            if (voteArea.DidVote) voteArea.UnsetVote();
            voteArea.AmDead = true;
            voteArea.Overlay.gameObject.SetActive(true);
            voteArea.Overlay.color = Color.white;
            voteArea.XMark.gameObject.SetActive(true);
            voteArea.XMark.transform.localScale = Vector3.one;
            foreach (var playerVoteArea in meetingHud.playerStates)
            {
                if (playerVoteArea.VotedFor != pc.PlayerId) continue;
                playerVoteArea.UnsetVote();
                var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
                if (!voteAreaPlayer.AmOwner) continue;
                meetingHud.ClearVote();
            }
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.AssassinKill, Hazel.SendOption.Reliable, -1);
            writer.Write(pc.PlayerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SetRoleAndNumber()//役職を番号で管理
        {
            RoleAndNumber = new();
            RoleAndNumberPirate = new();
            RoleAndNumberAss = new();
            RoleAndNumberCoven = new();
            List<CustomRole> vigiList = new();
            List<CustomRole> pirateList = new();
            List<CustomRole> assassinList = new();
            List<CustomRole> covenList = new();
            List<CustomRole> revealed = new();
            var i = 1;
            var ie = 1;
            var iee = 1;
            var c = 1;
            foreach (var id in Main.rolesRevealedNextMeeting)
            {
                revealed.Add(Utils.GetPlayerById(id).GetCustomRole());
            }
            foreach (CustomRole role in CustomRoleManager.Roles)
            {
                if (!role.IsEnable() | revealed.Contains(role)) continue;
                if (role is Phantom) continue;
                if (role is Child && StaticOptions.ChildKnown) continue;
                //TODO: Subroles
                /*if (role.IsModifier())
                {
                    if (!role.IsCrewModifier() && role != CustomRoles.LoversRecode) continue;
                }*/
                if (/*TODO: Team logic!role.IsImpostorTeam() &&*/ role is not Egoist) assassinList.Add(role);
                if (role is not Pirate) pirateList.Add(role);
                if (!role.IsCrewmate()) vigiList.Add(role);
                if (!role.IsCoven()) covenList.Add(role);
            }
            vigiList = vigiList.OrderBy(a => Guid.NewGuid()).ToList();
            assassinList = assassinList.OrderBy(a => Guid.NewGuid()).ToList();
            pirateList = pirateList.OrderBy(a => Guid.NewGuid()).ToList();
            covenList = covenList.OrderBy(a => Guid.NewGuid()).ToList();
            foreach (var ro in vigiList)
            {
                RoleAndNumber.Add(i, ro);
                i++;
            }//番号とセットにする
            foreach (var ro in pirateList)
            {
                RoleAndNumberPirate.Add(ie, ro);
                ie++;
            }//番号とセットにする
            foreach (var ro in assassinList)
            {
                RoleAndNumberAss.Add(iee, ro);
                iee++;
            }//番号とセットにする
            foreach (var ro in covenList)
            {
                RoleAndNumberCoven.Add(c, ro);
                c++;
            }//番号とセットにする
        }
        public static void OpenGuesserMeeting()
        {
            foreach (var gu in playerIdList)
            {
                if (isEvilGuesserExiled[gu])//ゲッサーの中から吊られた奴がいないかどうかの確認
                {
                    string text = "";
                    Utils.GetPlayerById(gu).CmdReportDeadBody(null);//会議を起こす
                    IsEvilGuesserMeeting = true;
                    text += GetString("EvilGuesserMeeting");
                    Utils.SendMessage(text, byte.MaxValue);
                }
            }
        }
        private static void PirateEndGame(GameOverReason reason, bool showAd)
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                var LoseImpostorRole = Main.AliveImpostorCount == 0 ? pc.GetCustomRole() is Impostor : pc.Is(CustomRoles.Egoist);
                if (pc.Is(CustomRoles.Sheriff) || pc.Is(CustomRoles.Investigator) ||
                    (Main.currentWinner != CustomWinner.Arsonist && pc.GetCustomRole() is Arsonist || (Main.currentWinner != CustomWinner.Vulture && pc.Is(CustomRoles.Vulture)) || (Main.currentWinner != CustomWinner.Marksman && pc.Is(CustomRoles.Marksman)) || (Main.currentWinner != CustomWinner.Pirate && pc.Is(CustomRoles.Pirate)) ||
                    (Main.currentWinner != CustomWinner.Jackal && pc.Is(CustomRoles.Jackal)) || (Main.currentWinner != CustomWinner.BloodKnight && pc.Is(CustomRoles.BloodKnight)) || (Main.currentWinner != CustomWinner.Pestilence && pc.Is(CustomRoles.Pestilence)) || (Main.currentWinner != CustomWinner.Coven && pc.GetRoleType() == RoleType.Coven) ||
                    LoseImpostorRole || (Main.currentWinner != CustomWinner.Werewolf && pc.Is(CustomRoles.Werewolf)) || (Main.currentWinner != CustomWinner.TheGlitch && pc.Is(CustomRoles.TheGlitch))))
                {
                    pc.RpcSetRole(RoleTypes.GuardianAngel);
                }
                if (pc.Is(CustomRoles.Pirate))
                {
                    //pc.RpcSetCustomRole(CustomRoles.Impostor);
                    pc.RpcSetRole(RoleTypes.Impostor);
                }
            }
            new Work(() =>
            {
                GameManager.Instance.RpcEndGame(reason, showAd);
            }, 0.5f, "EndGameTask");
        }
    }
}
