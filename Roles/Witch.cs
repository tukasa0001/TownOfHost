using System.Collections.Generic;
using System.Text;
using Hazel;
using MS.Internal.Xml.XPath;
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
            ModeSwitchAction = OptionItem.Create(Id + 10, TabGroup.ImpostorRoles, Color.white, "WitchModeSwitchAction", SwitchTriggerText, SwitchTriggerText[0], Options.CustomRoleSpawnChances[CustomRoles.Witch]);
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
            NowSwitchTrigger = (SwitchTrigger)ModeSwitchAction.GetSelection();
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
            bool flag = false;
            switch (NowSwitchTrigger)
            {
                case SwitchTrigger.Kill:
                    flag = kill;
                    break;
                case SwitchTrigger.Vent:
                    flag = !kill;
                    break;
            }
            if (flag)
            {
                SpellMode[playerId] = !SpellMode[playerId];
                SendRPC(false, playerId);
                Utils.NotifyRoles();
            }
        }
        public static bool HaveSpelledPlayer()
        {
            if (!IsEnable())
            {
                return false;
            }

            var spelled = false;
            foreach (var witch in playerIdList)
            {
                if (SpelledPlayer[witch].Count != 0)
                {
                    spelled = true;
                }
            }
            return spelled;

        }
        public static bool IsSpelled(byte target)
        {
            if (!IsEnable())
            {
                return false;
            }

            var spelled = false;
            foreach (var witch in playerIdList)
            {
                if (SpelledPlayer[witch].Contains(target))
                {
                    spelled = true;
                }
            }
            return spelled;
        }
        public static void RemoveSpelledPlayer()
        {
            if (!IsEnable())
            {
                return;
            }

            foreach (var witch in playerIdList)
            {
                SpelledPlayer[witch].Clear();
                SendRPC(true, witch);
            }
        }
        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            var ret = false;
            if (!IsSpellMode(killer.PlayerId))
            {
                //キルモードなら通常処理に戻る
                ret = true;
            }
            else if (!IsSpelled(target.PlayerId))
            {
                Main.AllPlayerKillCooldown[killer.PlayerId] = Options.DefaultKillCooldown * 2;
                killer.CustomSyncSettings();//キルクール処理を同期
                killer.RpcGuardAndKill(target);
                SpelledPlayer[killer.PlayerId].Add(target.PlayerId);
                SendRPC(true, killer.PlayerId, target.PlayerId);
            }
            SwitchSpellMode(killer.PlayerId, true);
            return ret;
        }
        public static void OnCheckForEndVoting(byte exiled)
        {
            if (playerIdList.Contains(exiled))
            {
                SpelledPlayer[exiled].Clear();
            }
            foreach (var witch in playerIdList)
            {
                foreach (var spelled in SpelledPlayer[witch])
                {
                    if (!Main.PlayerStates[spelled].IsDead)
                    {
                        Main.AfterMeetingDeathPlayers.TryAdd(spelled, PlayerState.DeathReason.Spell);
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
            if (Witch.IsSpellMode(PlayerControl.LocalPlayer.PlayerId))
            {
                hud.KillButton.OverrideText($"{GetString("WitchSpellButtonText")}");
            }
            else
            {
                hud.KillButton.OverrideText($"{GetString("KillButtonText")}");
            }
        }
    }
}
