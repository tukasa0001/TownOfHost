using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;
using System;
using InnerNet;
using TownOfHost.Extensions;
using TownOfHost.ReduxOptions;
using TownOfHost.Roles;
using TownOfHost.RPC;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class NecromancerOld
    {
        public static List<byte> playerIdList = new();
        public static CustomRole currentRole = Necromancer.Ref<Necromancer>();
        public static void Init()
        {
            playerIdList = new();
            currentRole = Necromancer.Ref<Necromancer>();
        }
        public static bool IsEnable()
        {
            return playerIdList.Count > 0;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);

            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);

            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : Necromancer Player", "Necromancer");
        }

        public static void OnReportBody(CustomRole role, PlayerControl player)
        {
            if (player.GetCustomRole() is Necromancer)
                switch (role)
                {
                    case PlagueBearer:
                        foreach (var ar in PlayerControl.AllPlayerControls)
                            if (!ar.Data.IsDead && !ar.GetCustomRole().IsCoven())
                                Main.isInfected.Add((player.PlayerId, ar.PlayerId), false);
                        currentRole = role;
                        break;
                    case Arsonist:
                        foreach (var ar in PlayerControl.AllPlayerControls)
                            if (!ar.Data.IsDead && !ar.GetCustomRole().IsCoven())
                                Main.isDoused.Add((player.PlayerId, ar.PlayerId), false);
                        currentRole = role;
                        break;
                    case Sheriff:
                    case CorruptedSheriff:
                        if (StaticOptions.NecroCanUseSheriff)
                            currentRole = role;
                        break;
                    default:
                        if (role.IsNeutralKilling() || role.IsImpostor())
                            if (role is not CrewPostor)
                            {
                                currentRole = role;
                                Utils.SendMessage("", player.PlayerId);
                            }
                        break;
                }
            Logger.Info($"{Utils.GetPlayerById(player.PlayerId)?.GetNameWithRole()} : Attempted to Take {role.ToString()}", "Necromancer");
        }
        public static void OnCheckMurder(PlayerControl necromancer, PlayerControl target)
        {
            bool skipVetCheck = Main.HasNecronomicon;
            /*switch (currentRole)
            {
                case Necromancer:
                    break;
                case Medusa:
                    if (Main.HasNecronomicon)
                    {
                        if (target.GetCustomRole() is Veteran && !Main.HasNecronomicon && Main.VetIsAlerted)
                        {
                            target.RpcMurderPlayer(necromancer);
                            break;
                        }
                        break;
                    }
                    else
                    {
                        break;
                    }
                case CovenWitch:
                    if (Main.HasNecronomicon)
                    {
                        Main.WitchedList[target.PlayerId] = 0;
                        if (target.GetCustomRole() is Veteran && !Main.HasNecronomicon && Main.VetIsAlerted)
                        {
                            target.RpcMurderPlayer(necromancer);
                            break;
                        }
                        break;
                    }
                    else
                    {
                        if (target.GetCustomRole() is Veteran && !Main.HasNecronomicon && Main.VetIsAlerted)
                        {
                            target.RpcMurderPlayer(necromancer);
                            break;
                        }
                        Main.WitchedList[target.PlayerId] = necromancer.PlayerId;
                        Main.AllPlayerKillCooldown[necromancer.PlayerId] = Options.CovenKillCooldown.GetFloat() * 2;
                        necromancer.CustomSyncSettings();
                        necromancer.RpcGuardAndKill(target);
                        break;
                    }
                    break;
                case Sidekick:
                case Jackal:

                    if (target.GetCustomRole() is Medusa && Main.IsGazing)
                    {
                        target.RpcMurderPlayer(necromancer);
                        new LateTask(() =>
                        {
                            Main.unreportableBodies.Add(necromancer.PlayerId);
                        }, Options.StoneReport.GetFloat(), "Medusa Stone Gazing");
                        break;
                    }
                    break;
                case Glitch:
                    if (target.GetCustomRole() is Veteran && !Main.HasNecronomicon && Main.VetIsAlerted && !Main.IsHackMode)
                    {
                        target.RpcMurderPlayer(necromancer);
                        break;
                    }
                    if (target.GetCustomRole() is Medusa && Main.IsGazing)
                    {
                        target.RpcMurderPlayer(necromancer);
                        new LateTask(() =>
                        {
                            Main.unreportableBodies.Add(necromancer.PlayerId);
                        }, Options.StoneReport.GetFloat(), "Medusa Stone Gazing");
                        break;
                    }
                    if (Main.IsHackMode && Main.CursedPlayers[necromancer.PlayerId] == necromancer)
                    { //Warlockが変身時以外にキルしたら、呪われる処理
                        Utils.CustomSyncAllSettings();
                        Main.CursedPlayers[necromancer.PlayerId] = target;
                        Main.WarlockTimer.Add(necromancer.PlayerId, 0f);
                        Main.isCurseAndKill[necromancer.PlayerId] = true;
                        necromancer.RpcGuardAndKill(target);
                        new LateTask(() =>
                        {
                            Main.CursedPlayers[necromancer.PlayerId] = necromancer;
                        }, Options.GlobalRoleBlockDuration.GetFloat(), "Glitch Hacking");
                        break;
                    }
                    if (!Main.IsHackMode)
                    {
                        if (target.GetCustomRole()Is(Pestilence))
                        {
                            target.RpcMurderPlayer(necromancer);
                            break;
                        }
                        necromancer.RpcMurderPlayer(target);
                        //necromancer.RpcGuardAndKill(target);
                        break;
                    }
                    if (Main.isCurseAndKill[necromancer.PlayerId]) necromancer.RpcGuardAndKill(target);
                    break;
                //break;
                case Ninja:
                    NinjaOld.KillCheck(necromancer, target);
                    break;
                case Werewolf:
                    if (Main.IsRampaged)
                    {
                        if (target.Is(Veteran) && !Main.HasNecronomicon && Main.VetIsAlerted)
                        {
                            target.RpcMurderPlayer(necromancer);
                            break;
                        }
                        if (target.Is(Pestilence))
                        {
                            target.RpcMurderPlayer(necromancer);
                            break;
                        }
                        if (target.Is(Medusa) && Main.IsGazing)
                        {
                            target.RpcMurderPlayer(necromancer);
                            new LateTask(() =>
                            {
                                Main.unreportableBodies.Add(necromancer.PlayerId);
                            }, Options.StoneReport.GetFloat(), "Medusa Stone Gazing");
                            break;
                        }
                        necromancer.RpcMurderPlayer(target);
                        break;
                    }
                    else
                    {
                        break;
                    }
                    break;
                case Amnesiac:
                    break;
                case Juggernaut:
                    //calculating next kill cooldown

                    if (target.Is(Medusa) && Main.IsGazing)
                    {
                        target.RpcMurderPlayer(necromancer);
                        new LateTask(() =>
                        {
                            Main.unreportableBodies.Add(necromancer.PlayerId);
                        }, Options.StoneReport.GetFloat(), "Medusa Stone Gazing");
                        break;
                    }
                    Main.JugKillAmounts++;
                    float DecreasedAmount = Main.JugKillAmounts * Options.JuggerDecrease.GetFloat();
                    Main.AllPlayerKillCooldown[necromancer.PlayerId] = Options.JuggerKillCooldown.GetFloat() - DecreasedAmount;
                    if (Main.AllPlayerKillCooldown[necromancer.PlayerId] < 1)
                        Main.AllPlayerKillCooldown[necromancer.PlayerId] = 1;
                    //after calculating make the kill happen ?
                    necromancer.CustomSyncSettings();
                    necromancer.RpcMurderPlayer(target);
                    break;
                    break;
                case BountyHunter: //キルが発生する前にここの処理をしないとバグる

                    if (target.Is(Medusa) && Main.IsGazing)
                    {
                        target.RpcMurderPlayer(necromancer);
                        new LateTask(() =>
                        {
                            Main.unreportableBodies.Add(necromancer.PlayerId);
                        }, Options.StoneReport.GetFloat(), "Medusa Stone Gazing");
                        break;
                    }
                    BountyHunterOld.OnCheckMurder(necromancer, target);
                    break;
                case SerialKiller:

                    if (target.Is(Medusa) && Main.IsGazing)
                    {
                        target.RpcMurderPlayer(necromancer);
                        new LateTask(() =>
                        {
                            Main.unreportableBodies.Add(necromancer.PlayerId);
                        }, Options.StoneReport.GetFloat(), "Medusa Stone Gazing");
                        break;
                    }
                    SerialKillerOld.OnCheckMurder(necromancer);
                    break;
                case Poisoner:
                case Vampire:

                    if (target.Is(Medusa) && Main.IsGazing)
                    {
                        target.RpcMurderPlayer(necromancer);
                        new LateTask(() =>
                        {
                            Main.unreportableBodies.Add(necromancer.PlayerId);
                        }, Options.StoneReport.GetFloat(), "Medusa Stone Gazing");
                        break;
                    }
                    if (!target.Is(Bait))
                    { //キルキャンセル&自爆処理
                        if (!target.Is(Bewilder))
                        {
                            Utils.CustomSyncAllSettings();
                            Main.AllPlayerKillCooldown[necromancer.PlayerId] = Options.DefaultKillCooldown * 2;
                            necromancer.CustomSyncSettings(); //負荷軽減のため、necromancerだけがCustomSyncSettingsを実行
                            necromancer.RpcGuardAndKill(target);
                            Main.BitPlayers.Add(target.PlayerId, (necromancer.PlayerId, 0f));
                            break;
                        }
                    }
                    else
                    {
                        if (Options.VampireBuff.GetBool()) //Vampire Buff will still make Vampire report but later.
                        {
                            Utils.CustomSyncAllSettings();
                            Main.AllPlayerKillCooldown[necromancer.PlayerId] = Options.DefaultKillCooldown * 2;
                            necromancer.CustomSyncSettings(); //負荷軽減のため、necromancerだけがCustomSyncSettingsを実行
                            necromancer.RpcGuardAndKill(target);
                            Main.BitPlayers.Add(target.PlayerId, (necromancer.PlayerId, 0f));
                            break;
                        }
                    }
                    break;
                case Warlock:

                    if (target.Is(Medusa) && Main.IsGazing)
                    {
                        target.RpcMurderPlayer(necromancer);
                        new LateTask(() =>
                        {
                            Main.unreportableBodies.Add(necromancer.PlayerId);
                        }, Options.StoneReport.GetFloat(), "Medusa Stone Gazing");
                        break;
                    }
                    if (!Main.CheckShapeshift[necromancer.PlayerId] && !Main.isCurseAndKill[necromancer.PlayerId])
                    { //Warlockが変身時以外にキルしたら、呪われる処理
                        Main.isCursed = true;
                        Utils.CustomSyncAllSettings();
                        necromancer.RpcGuardAndKill(target);
                        Main.CursedPlayers[necromancer.PlayerId] = target;
                        Main.WarlockTimer.Add(necromancer.PlayerId, 0f);
                        Main.isCurseAndKill[necromancer.PlayerId] = true;
                        break;
                    }
                    if (Main.CheckShapeshift[necromancer.PlayerId])
                    {//呪われてる人がいないくて変身してるときに通常キルになる
                        necromancer.RpcMurderPlayer(target);
                        necromancer.RpcGuardAndKill(target);
                        break;
                    }
                    if (Main.isCurseAndKill[necromancer.PlayerId]) necromancer.RpcGuardAndKill(target);
                    break;
                case Silencer:
                    //Silenced Player

                    if (target.Is(Medusa) && Main.IsGazing)
                    {
                        target.RpcMurderPlayer(necromancer);
                        new LateTask(() =>
                        {
                            Main.unreportableBodies.Add(necromancer.PlayerId);
                        }, Options.StoneReport.GetFloat(), "Medusa Stone Gazing");
                        break;
                    }
                    if (Main.SilencedPlayer.Count > 0)
                    {
                        necromancer.RpcMurderPlayer(target);
                        break;
                    }
                    else if (Main.SilencedPlayer.Count <= 0)
                    {
                        Main.firstKill.Add(necromancer.PlayerId);
                        necromancer.RpcGuardAndKill(target);
                        Main.SilencedPlayer.Add(target);
                        OldRPC.RpcDoSilence(target.PlayerId);
                        break;
                    }
                    if (!Main.firstKill.Contains(necromancer.PlayerId) && !Main.SilencedPlayer.Contains(target)) break;
                    break;
                case Witch:

                    if (target.Is(Medusa) && Main.IsGazing)
                    {
                        target.RpcMurderPlayer(necromancer);
                        new LateTask(() =>
                        {
                            Main.unreportableBodies.Add(necromancer.PlayerId);
                        }, Options.StoneReport.GetFloat(), "Medusa Stone Gazing");
                        break;
                    }
                    if (necromancer.IsSpellMode() && !Main.SpelledPlayer.Contains(target))
                    {
                        necromancer.RpcGuardAndKill(target);
                        Main.SpelledPlayer.Add(target);
                        OldRPC.RpcDoSpell(target.PlayerId);
                    }
                    Main.KillOrSpell[necromancer.PlayerId] = !necromancer.IsSpellMode();
                    Utils.NotifyRoles();
                    necromancer.SyncKillOrSpell();
                    if (!necromancer.IsSpellMode()) break;
                    break;
                case HexMaster:
                    if (target.Is(Veteran) && !Main.HasNecronomicon && Main.VetIsAlerted && Main.HexesThisRound != Options.MaxHexesPerRound.GetFloat())
                    {
                        target.RpcMurderPlayer(necromancer);
                        break;
                    }
                    Main.AllPlayerKillCooldown[necromancer.PlayerId] = 10f;
                    Utils.CustomSyncAllSettings();
                    if (!Main.isHexed[(necromancer.PlayerId, target.PlayerId)] && necromancer.IsHexMode() && Main.HexesThisRound != Options.MaxHexesPerRound.GetFloat())
                    {
                        necromancer.RpcGuardAndKill(target);
                        Main.HexesThisRound++;
                        Utils.NotifyRoles(SpecifySeer: necromancer);
                        Main.isHexed[(necromancer.PlayerId, target.PlayerId)] = true;//塗り完了
                    }
                    if (Main.HexesThisRound != Options.MaxHexesPerRound.GetFloat())
                        Main.KillOrSpell[necromancer.PlayerId] = !necromancer.IsHexMode();
                    Utils.NotifyRoles();
                    necromancer.SyncKillOrHex();
                    if (!necromancer.IsHexMode()) break;
                    //break;
                    if (!Main.HasNecronomicon && Main.HexesThisRound == Options.MaxHexesPerRound.GetFloat()) break;
                    break;
                case Puppeteer:

                    if (target.Is(Medusa) && Main.IsGazing)
                    {
                        target.RpcMurderPlayer(necromancer);
                        new LateTask(() =>
                        {
                            Main.unreportableBodies.Add(necromancer.PlayerId);
                        }, Options.StoneReport.GetFloat(), "Medusa Stone Gazing");
                        break;
                    }
                    Main.PuppeteerList[target.PlayerId] = necromancer.PlayerId;
                    Main.AllPlayerKillCooldown[necromancer.PlayerId] = Options.DefaultKillCooldown * 2;
                    necromancer.CustomSyncSettings(); //負荷軽減のため、necromancerだけがCustomSyncSettingsを実行
                    necromancer.RpcGuardAndKill(target);
                    break;
                case TimeThief:

                    if (target.Is(Medusa) && Main.IsGazing)
                    {
                        target.RpcMurderPlayer(necromancer);
                        new LateTask(() =>
                        {
                            Main.unreportableBodies.Add(necromancer.PlayerId);
                        }, Options.StoneReport.GetFloat(), "Medusa Stone Gazing");
                        break;
                    }
                    TimeThiefOld.OnCheckMurder(necromancer);
                    break;

                //==========マッドメイト系役職==========//

                //==========第三陣営役職==========//
                case Arsonist:

                    if (target.Is(Medusa) && Main.IsGazing)
                    {
                        target.RpcMurderPlayer(necromancer);
                        new LateTask(() =>
                        {
                            Main.unreportableBodies.Add(necromancer.PlayerId);
                        }, Options.StoneReport.GetFloat(), "Medusa Stone Gazing");
                        break;
                    }
                    Main.AllPlayerKillCooldown[necromancer.PlayerId] = 10f;
                    Utils.CustomSyncAllSettings();
                    if (!Main.isDoused[(necromancer.PlayerId, target.PlayerId)] && !Main.ArsonistTimer.ContainsKey(necromancer.PlayerId))
                    {
                        Main.ArsonistTimer.Add(necromancer.PlayerId, (target, 0f));
                        Utils.NotifyRoles(SpecifySeer: necromancer);
                        OldRPC.SetCurrentDousingTarget(necromancer.PlayerId, target.PlayerId);
                    }
                    break;

                //==========クルー役職==========//
                case PlagueBearer:

                    if (target.Is(Medusa) && Main.IsGazing)
                    {
                        target.RpcMurderPlayer(necromancer);
                        new LateTask(() =>
                        {
                            Main.unreportableBodies.Add(necromancer.PlayerId);
                        }, Options.StoneReport.GetFloat(), "Medusa Stone Gazing");
                        break;
                    }
                    Main.AllPlayerKillCooldown[necromancer.PlayerId] = 10f;
                    Utils.CustomSyncAllSettings();
                    if (!Main.isInfected[(necromancer.PlayerId, target.PlayerId)] && !Main.PlagueBearerTimer.ContainsKey(necromancer.PlayerId))
                    {
                        Main.PlagueBearerTimer.Add(necromancer.PlayerId, (target, 0f));
                        Utils.NotifyRoles(SpecifySeer: necromancer);
                        OldRPC.SetCurrentInfectingTarget(necromancer.PlayerId, target.PlayerId);
                        //Main.isInfected[(target.PlayerId, target.PlayerId)] = true;
                        //necromancer.RpcGuardAndKill(target);
                    }
                    break;
                case Sheriff:
                    skipVetCheck = true;
                    if (target.Is(Veteran) && !Main.HasNecronomicon && Main.VetIsAlerted && Options.CrewRolesVetted.GetBool())
                    {
                        target.RpcMurderPlayer(necromancer);
                        break;
                    }
                    if (target.Is(Medusa) && Main.IsGazing)
                    {
                        target.RpcMurderPlayer(necromancer);
                        new LateTask(() =>
                        {
                            Main.unreportableBodies.Add(necromancer.PlayerId);
                        }, Options.StoneReport.GetFloat(), "Medusa Stone Gazing");
                        break;
                    }
                    SheriffOld.OnCheckMurder(necromancer, target, Process: "RemoveShotLimit");
                    break;
                case BloodKnight:
                    if (target.Is(Medusa) && Main.IsGazing)
                    {
                        target.RpcMurderPlayer(necromancer);
                        new LateTask(() =>
                        {
                            Main.unreportableBodies.Add(necromancer.PlayerId);
                        }, Options.StoneReport.GetFloat(), "Medusa Stone Gazing");
                        break;
                    }
                    if (!Main.bkProtected)
                    {
                        Main.bkProtected = true;
                        new LateTask(() =>
                        {
                            Main.bkProtected = false;
                        }, Options.BKprotectDur.GetFloat(), "Blood Knight Duration");
                    }
                    break;
                default:
                    if (target.Is(Medusa) && Main.IsGazing)
                    {
                        target.RpcMurderPlayer(necromancer);
                        new LateTask(() =>
                        {
                            Main.unreportableBodies.Add(necromancer.PlayerId);
                        }, Options.StoneReport.GetFloat(), "Medusa Stone Gazing");
                        break;
                    }
                    break;
            }*/
            if (skipVetCheck) return;
            if (target.Is(CustomRoles.Veteran) && !Main.HasNecronomicon && Main.VetIsAlerted)
                target.RpcMurderPlayer(necromancer);
        }
        public static void OnShapeshiftCheck(this PlayerControl pc, bool shapeshifting)
        {

        }
        public static bool CanUseVent()
        {
            /*
            else if (pc.Object.Is(CustomRoles.Arsonist) && pc.Object.IsDouseDone() && !Options.TOuRArso.GetBool())
                canUse = couldUse = VentForTrigger = true;
            else if (pc.Object.Is(CustomRoles.Arsonist) && Options.TOuRArso.GetBool())
                canUse = couldUse = true;
            else if (pc.Object.Is(CustomRoles.Jackal) || pc.Object.Is(CustomRoles.Sidekick))
                canUse = couldUse = Options.JackalCanVent.GetBool();
            else if (pc.Object.Is(CustomRoles.Jester))
                canUse = couldUse = Options.JesterCanVent.GetBool();
            else if (pc.Object.Is(CustomRoles.Pestilence))
                canUse = couldUse = Options.PestiCanVent.GetBool();
            else if (pc.Object.Is(CustomRoles.Juggernaut))
                canUse = couldUse = Options.JuggerCanVent.GetBool();
            else if (pc.Object.Is(CustomRoles.BloodKnight))
                canUse = couldUse = Options.BKcanVent.GetBool();
            else if (pc.Object.Is(CustomRoles.TheGlitch))
            */
            switch (currentRole)
            {
                case Arsonist:
                    break;
                case Sidekick:
                case Jackal:
                    return currentRole.CanVent();
                case Pestilence:
                    return StaticOptions.PestiCanVent;
                case Juggernaut:
                    return StaticOptions.JuggerCanVent;
                case BloodKnight:
                    return StaticOptions.BKcanVent;

                case Necromancer:
                case Werewolf:
                case Medusa:
                case Glitch:
                case CorruptedSheriff:
                    return true;
            }
            if (currentRole.IsCoven() && currentRole is not Necromancer) return Main.HasNecronomicon;
            return false;
        }
        public static void OnUseVent(int ventId)
        {

        }
        public static string GetProgressText()
        {
            return "";
        }
    }
}