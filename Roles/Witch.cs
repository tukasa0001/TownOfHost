using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class Witch
    {
        public enum SwitchTrigger
        {
            Kill = 0,
            Vent = 1,
        };
        public static readonly string[] SwitchTriggerText =
        {
            "TriggerKill", "TriggerVent",
        };

        private static readonly int Id = 1500;
        public static List<byte> playerIdList = new();

        public static Dictionary<byte, bool> SpellMode = new();
        public static Dictionary<byte, List<byte>> SpelledPlayer = new();

        public static OptionItem ModeSwitchAction;
        public static SwitchTrigger NowSwitchTrigger;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Witch);
            ModeSwitchAction = StringOptionItem.Create(Id + 10, "WitchModeSwitchAction", SwitchTriggerText, 0, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Witch]);
        }
        public static void Init()
        {
            playerIdList = new();
            SpellMode = new();
            SpelledPlayer = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            SpellMode.Add(playerId, false);
            SpelledPlayer.Add(playerId, new());
            NowSwitchTrigger = (SwitchTrigger)ModeSwitchAction.GetValue();
        }
        public static bool IsEnable()
        {
            return playerIdList.Count > 0;
        }
        private static void SendRPC(bool doSpell, byte witchId, byte target = 255)
        {
            if (doSpell)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DoSpell, SendOption.Reliable, -1);
                writer.Write(witchId);
                writer.Write(target);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
            else
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKillOrSpell, SendOption.Reliable, -1);
                writer.Write(witchId);
                writer.Write(SpellMode[witchId]);
                AmongUsClient.Instance.FinishRpcImmediately(writer);

            }
        }

        public static void ReceiveRPC(MessageReader reader, bool doSpell)
        {
            if (doSpell)
            {
                var witch = reader.ReadByte();
                var spelledId = reader.ReadByte();
                if (spelledId != 255)
                {
                    SpelledPlayer[witch].Add(spelledId);
                }
                else
                {
                    SpelledPlayer[witch].Clear();
                }
            }
            else
            {
                byte playerId = reader.ReadByte();
                SpellMode[playerId] = reader.ReadBoolean();
            }
        }
        public static bool IsSpellMode(byte playerId)
        {
            return SpellMode[playerId];
        }
        public static void SwitchSpellMode(byte playerId, bool kill)
        {
            bool needSwitch = false;
            switch (NowSwitchTrigger)
            {
                case SwitchTrigger.Kill:
                    needSwitch = kill;
                    break;
                case SwitchTrigger.Vent:
                    needSwitch = !kill;
                    break;
            }
            if (needSwitch)
            {
                SpellMode[playerId] = !SpellMode[playerId];
                SendRPC(false, playerId);
                Utils.NotifyRoles(SpecifySeer: Utils.GetPlayerById(playerId));
            }
        }
        public static bool HaveSpelledPlayer()
        {
            foreach (var witch in playerIdList)
            {
                if (SpelledPlayer[witch].Count != 0)
                {
                    return true;
                }
            }
            return false;

        }
        public static bool IsSpelled(byte target)
        {
            foreach (var witch in playerIdList)
            {
                if (SpelledPlayer[witch].Contains(target))
                {
                    return true;
                }
            }
            return false;
        }
        public static void RemoveSpelledPlayer()
        {
            foreach (var witch in playerIdList)
            {
                SpelledPlayer[witch].Clear();
                SendRPC(true, witch);
            }
        }
        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            if (!IsSpellMode(killer.PlayerId))
            {
                SwitchSpellMode(killer.PlayerId, true);
                //キルモードなら通常処理に戻る
                return true;
            }

            if (!IsSpelled(target.PlayerId))
            {
                //キルクールの適正化
                Main.AllPlayerKillCooldown[killer.PlayerId] = Options.DefaultKillCooldown * 2;
                killer.CustomSyncSettings();
                killer.RpcGuardAndKill(target);

                SpelledPlayer[killer.PlayerId].Add(target.PlayerId);
                SendRPC(true, killer.PlayerId, target.PlayerId);
            }
            //スペルに失敗してもスイッチ判定
            SwitchSpellMode(killer.PlayerId, true);
            //キル処理終了させる
            return false;
        }
        public static void OnCheckForEndVoting(byte exiled)
        {
            foreach (var witch in playerIdList)
            {
                if (witch != exiled)
                {
                    var witchPc = Utils.GetPlayerById(witch);
                    foreach (var spelled in SpelledPlayer[witch])
                    {
                        if (!Main.PlayerStates[spelled].IsDead)
                        {
                            Utils.GetPlayerById(spelled).SetRealKiller(witchPc);
                            CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(spelled, PlayerState.DeathReason.Spell);
                        }
                    }
                }
                SendRPC(true, witch);
                SpelledPlayer[witch].Clear();
            }
        }
        public static void OnEnterVent(byte playerId)
        {
            if (NowSwitchTrigger is SwitchTrigger.Vent)
            {
                SwitchSpellMode(playerId, false);
            }
        }
        public static string GetSpelledMark(byte targetId, bool isMeeting)
        {
            if (isMeeting && IsEnable() && Witch.IsSpelled(targetId))
            {
                return "<color=#ff0000>†</color>";
            }
            return "";
        }
        public static string GetSpellModeText(PlayerControl witch, bool hud)
        {
            if (witch == null) return "";

            var str = new StringBuilder();
            if (hud)
            {
                str.Append(GetString("WitchCurrentMode") + ":");
            }
            else
            {
                str.Append("Mode:");
            }
            str.Append(IsSpellMode(witch.PlayerId) ? GetString("WitchModeSpell") : GetString("WitchModeKill"));
            return str.ToString();
        }
        public static void GetAbilityButtonText(HudManager hud)
        {
            if (IsSpellMode(PlayerControl.LocalPlayer.PlayerId))
            {
                hud.KillButton.OverrideText($"{GetString("WitchSpellButtonText")}");
            }
            else
            {
                hud.KillButton.OverrideText($"{GetString("KillButtonText")}");
            }
        }

        [HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
        class EnterVentPatch
        {
            public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
            {
                if (!AmongUsClient.Instance.AmHost) return;
                if (pc.Is(CustomRoles.Witch))
                {
                    Witch.OnEnterVent(pc.PlayerId);
                }
            }
        }
    }
}
